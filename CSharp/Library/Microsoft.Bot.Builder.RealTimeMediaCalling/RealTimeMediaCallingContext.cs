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
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Calling;
using Microsoft.Bot.Builder.Internals.Fibers;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    /// <summary>
    /// The context for this request. It parses the request into <see cref="ParsedCallingRequest"/>.
    /// </summary>
    public class RealTimeMediaCallingContext
    {
        /// <summary>
        /// The calling request.
        /// </summary>
        public readonly HttpRequestMessage Request;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>
        /// Automatically injected by Autofac DI
        /// </value>
        public IRealTimeMediaLogger Logger { get; set; }

        /// <summary>
        /// Creates a new instance of calling context. 
        /// </summary>
        /// <param name="request"> The calling request.</param>
        public RealTimeMediaCallingContext(HttpRequestMessage request) 
        {
            SetField.NotNull<HttpRequestMessage>(out this.Request, nameof(request), request);
        }

        /// <summary>
        /// Process the calling request and returns <see cref="ParsedCallingRequest"/>.
        /// </summary>
        /// <param name="callType"> The type of request.</param>
        /// <returns> The parsed request.</returns>
        public virtual async Task<ParsedCallingRequest> ProcessRequest(RealTimeMediaCallRequestType callType)
        {
            ParsedCallingRequest parsedRequest;
            switch (callType)
            {
                case RealTimeMediaCallRequestType.IncomingCall:
                case RealTimeMediaCallRequestType.CallingEvent:
                case RealTimeMediaCallRequestType.NotificationEvent:
                    parsedRequest = await ProcessRequestAsync();
                    break;
                default:
                    parsedRequest = CallingContext.GenerateParsedResults(HttpStatusCode.BadRequest, $"{callType} not accepted");
                    break;
            }
            parsedRequest.SkypeChainId = CallingContext.ExtractSkypeChainId(this.Request);
            return parsedRequest;
        }

        private async Task<ParsedCallingRequest> ProcessRequestAsync()
        {
            try
            {
                if (Request.Content == null)
                {
                    Logger.LogError("No content in the request");
                    return CallingContext.GenerateParsedResults(HttpStatusCode.BadRequest);
                }

                var content = await Request.Content.ReadAsStringAsync().ConfigureAwait(false);
                return CallingContext.GenerateParsedResults(HttpStatusCode.OK, content);
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to process the notification request, exception: {e}");
                return CallingContext.GenerateParsedResults(HttpStatusCode.InternalServerError, e.ToString());
            }
        }
    }
}
