// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Bot Framework: http://botframework.com
// 
// Bot Builder SDK GitHub:
// https://github.com/Microsoft/BotBuilder
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    internal class LoggingMessageHandler : DelegatingHandler
    {
        /// <summary>
        /// Log the request and response.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string requestUriText = request.RequestUri.ToString();
            string requestHeadersText = GetHeadersText(request.Headers);
            string requestBodyText = await GetBodyText(request.Content).ConfigureAwait(false);

            Trace.TraceInformation($"Method: {request.Method.ToString()}, Uri {requestUriText}, Headers { requestHeadersText}, Body { requestBodyText}");
            HttpResponseMessage response = await SendAndLogAsync(request, cancellationToken).ConfigureAwait(false);
            string responseHeadersText = GetHeadersText(response.Headers);

            if (response.Content != null)
            {
                responseHeadersText =
                    String.Join(
                        Environment.NewLine,
                        responseHeadersText,
                        GetHeadersText(response.Content.Headers));
            }

            string responseBodyText = await GetBodyText(response.Content).ConfigureAwait(false);
            Trace.TraceInformation($"Response: {responseBodyText}, Headers {responseHeadersText}");
            return response;
        }

        private async Task<HttpResponseMessage> SendAndLogAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Trace.TraceError("Exception occurred when calling SendAsync: {0}", e.ToString());
                throw;
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

        public static async Task<string> GetBodyText(HttpContent content)
        {
            if (content == null)
            {
                return "(empty body)";
            }
            string body = await content.ReadAsStringAsync().ConfigureAwait(false);

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
