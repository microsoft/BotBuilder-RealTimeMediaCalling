/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using FrontEnd.Logging;

namespace FrontEnd.Http
{
    /// <summary>
    /// Helper class to log HTTP requests and responses and to set the CorrelationID based on the X-Microsoft-Skype-Chain-ID header
    /// value of incoming HTTP requests from Skype platform.
    /// </summary>
    internal class LoggingMessageHandler : DelegatingHandler
    {
        public const string CidHeaderName = "X-Microsoft-Skype-Chain-ID";

        private readonly bool isIncomingMessageHandler;
        private readonly LogContext logContext;
        private string[] urlIgnorers;

        /// <summary>
        /// Create a new LoggingMessageHandler.
        /// </summary>
        public LoggingMessageHandler(bool isIncomingMessageHandler, LogContext logContext, string[] urlIgnorers = null)
        {
            this.isIncomingMessageHandler = isIncomingMessageHandler;
            this.logContext = logContext;
            this.urlIgnorers = urlIgnorers;
        }

        /// <summary>
        /// Log the request and response.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string requestCid;
            string responseCid;

            if (this.isIncomingMessageHandler)
            {
                requestCid = AdoptCorrelationId(request.Headers);
            }
            else
            {
                requestCid = SetCorrelationId(request.Headers);
            }

            bool ignore =
                this.urlIgnorers != null &&
                this.urlIgnorers.Any(ignorer => request.RequestUri.ToString().IndexOf(ignorer, StringComparison.OrdinalIgnoreCase) >= 0);

            if (ignore)
            {
                return await SendAndLogAsync(request, cancellationToken);
            }

            string localMessageId = Guid.NewGuid().ToString();
            string requestUriText = request.RequestUri.ToString();
            string requestHeadersText = GetHeadersText(request.Headers);

            if (request.Content != null)
            {
                requestHeadersText =
                    String.Join(
                        Environment.NewLine,
                        requestHeadersText,
                        GetHeadersText(request.Content.Headers));
            }

            string requestBodyText = await GetBodyText(request.Content);

            Log.Info(new CallerInfo(), logContext, "|| correlationId={0} || local.msgid={1} ||{2}{3}:: {4} {5}{6}{7}{8}{9}{10}$$END$$",
                requestCid, localMessageId,
                Environment.NewLine,
                this.isIncomingMessageHandler ? "Incoming" : "Outgoing",
                request.Method.ToString(),
                requestUriText,
                Environment.NewLine,
                requestHeadersText,
                Environment.NewLine,
                requestBodyText,
                Environment.NewLine);

            Stopwatch stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            Log.Info(
                new CallerInfo(),
                logContext,
                "{0} HTTP request with Local id={1} took {2}ms.",
                this.isIncomingMessageHandler ? "Incoming" : "Outgoing",
                localMessageId,
                stopwatch.ElapsedMilliseconds);

            if (this.isIncomingMessageHandler)
            {
                responseCid = SetCorrelationId(response.Headers);
            }
            else
            {
                responseCid = AdoptCorrelationId(response.Headers);
            }

            this.WarnIfDifferent(requestCid, responseCid);

            HttpStatusCode statusCode = response.StatusCode;

            string responseUriText = request.RequestUri.ToString();
            string responseHeadersText = GetHeadersText(response.Headers);

            if (response.Content != null)
            {
                responseHeadersText =
                    String.Join(
                        Environment.NewLine,
                        responseHeadersText,
                        GetHeadersText(response.Content.Headers));
            }

            string responseBodyText = await GetBodyText(response.Content);

            Log.Info(new CallerInfo(), logContext, "|| correlationId={0} || statuscode={1} || local.msgid={2} ||{3}Response to {4}:: {5} {6}{7}{8} {9}{10}{11}{12}{13}{14}$$END$$",
                CorrelationId.GetCurrentId(), statusCode, localMessageId,
               Environment.NewLine,
               this.isIncomingMessageHandler ? "incoming" : "outgoing",
                request.Method.ToString(),
                responseUriText,
                Environment.NewLine,
               ((int)response.StatusCode).ToString(),
                response.StatusCode.ToString(),
               Environment.NewLine,
                responseHeadersText,
               Environment.NewLine,
                responseBodyText,
                Environment.NewLine);

            return response;
        }

        private async Task<HttpResponseMessage> SendAndLogAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception e)
            {
                Log.Error(new CallerInfo(), LogContext.FrontEnd, "Exception occurred when calling SendAsync: {0}", e.ToString());
                throw;
            }
        }

        private void WarnIfDifferent(string requestCid, string responseCid)
        {
            if (string.IsNullOrWhiteSpace(requestCid) || string.IsNullOrWhiteSpace(responseCid))
            {
                return;
            }

            if (!string.Equals(requestCid, responseCid))
            {
                Log.Warning(
                    new CallerInfo(), LogContext.FrontEnd,
                    "The correlationId of the {0} request, {1}, is different from the {2} response, {3}.",
                    this.isIncomingMessageHandler ? "incoming" : "outgoing",
                    requestCid,
                    this.isIncomingMessageHandler ? "outgoing" : "outgoing",
                    responseCid);
            }
        }

        public static string GetHeadersText(HttpHeaders headers)
        {
            if (headers == null || !headers.Any())
            {
                return String.Empty;
            }

            List<string> headerTexts = new List<string>();

            foreach (KeyValuePair<string, IEnumerable<string>> h in headers)
            {
                headerTexts.Add(GetHeaderText(h));
            }

            return String.Join(Environment.NewLine, headerTexts);
        }

        private static string GetHeaderText(KeyValuePair<string, IEnumerable<string>> header)
        {
            return String.Format("{0}: {1}", header.Key, String.Join(",", header.Value));
        }

        private static string AdoptCorrelationId(HttpHeaders headers)
        {
            string correlationId = null;
            IEnumerable<string> correlationIdHeaderValues;
            if (headers.TryGetValues(CidHeaderName, out correlationIdHeaderValues))
            {
                correlationId = correlationIdHeaderValues.FirstOrDefault();
                CorrelationId.SetCurrentId(correlationId);
            }

            return correlationId;
        }

        private static string SetCorrelationId(HttpHeaders headers)
        {
            string correlationId = CorrelationId.GetCurrentId();
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                headers.Add(CidHeaderName, correlationId);
            }

            return correlationId;
        }

        public static async Task<string> GetBodyText(HttpContent content)
        {
            if (content == null)
            {
                return "(empty body)";
            }

            if (content.IsMimeMultipartContent())
            {
                Stream stream = await content.ReadAsStreamAsync();

                if (!stream.CanSeek)
                {
                    return "(cannot log body because HTTP stream cannot seek)";
                }

                StringBuilder multipartBodyBuilder = new StringBuilder();
                MultipartMemoryStreamProvider streamProvider = new MultipartMemoryStreamProvider();
                await content.ReadAsMultipartAsync<MultipartMemoryStreamProvider>(streamProvider, (int)stream.Length);

                try
                {
                    foreach (var multipartContent in streamProvider.Contents)
                    {
                        multipartBodyBuilder.AppendLine("-- beginning of multipart content --");

                        // Headers
                        string headerText = GetHeadersText(multipartContent.Headers);
                        multipartBodyBuilder.AppendLine(headerText);

                        // Body of message
                        string multipartBody = await multipartContent.ReadAsStringAsync();
                        string formattedJsonBody;

                        if (TryFormatJsonBody(multipartBody, out formattedJsonBody))
                        {
                            multipartBody = formattedJsonBody;
                        }

                        if (String.IsNullOrWhiteSpace(multipartBody))
                        {
                            multipartBodyBuilder.AppendLine("(empty body)");
                        }
                        else
                        {
                            multipartBodyBuilder.AppendLine(multipartBody);
                        }

                        multipartBodyBuilder.AppendLine("-- end of multipart content --");
                    }
                }
                finally
                {
                    // Reset the stream position so consumers of this class can re-read the multipart content.
                    stream.Position = 0;
                }

                return multipartBodyBuilder.ToString();
            }
            else
            {
                string body = await content.ReadAsStringAsync();

                string formattedJsonBody;
                if (TryFormatJsonBody(body, out formattedJsonBody))
                {
                    body = formattedJsonBody;
                }

                if (String.IsNullOrWhiteSpace(body))
                {
                    return "(empty body)";
                }

                return body;
            }
        }

        private static bool TryFormatJsonBody(string body, out string jsonBody)
        {
            jsonBody = null;

            if (String.IsNullOrWhiteSpace(body))
            {
                return false;
            }

            try
            {
                object parsedObject = JsonConvert.DeserializeObject(body);

                if (parsedObject == null)
                {
                    return false;
                }

                jsonBody = JsonConvert.SerializeObject(parsedObject, Formatting.Indented);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
