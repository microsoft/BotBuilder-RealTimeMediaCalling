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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    internal class RetryMessageHandler : DelegatingHandler
    {
        private const int RetryCount = 3;
        private static TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>
        /// Automatically injected by Autofac DI
        /// </value>
        public IRealTimeMediaLogger Logger { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return RunWithRetries(
                RetryCount,
                RetryDelay,
                async () => await base.SendAsync(request, cancellationToken),
                cancellationToken);
        }

        public async Task<HttpResponseMessage> RunWithRetries(
          int maxRetries,
          TimeSpan delay,
          Func<Task<HttpResponseMessage>> retryableMethod,
          CancellationToken cancelToken = default(CancellationToken))
        {
            try
            {
                var exceptions = new List<Exception>();

                var retryCount = 0;
                while (true)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        exceptions.Add(new OperationCanceledException());
                        throw new AggregateException(exceptions);
                    }

                    try
                    {
                        return await retryableMethod();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError("Error sending request " + e.ToString());
                        exceptions.Add(e);

                        if (++retryCount > maxRetries)
                        {
                            throw new AggregateException(exceptions);
                        }
                    }

                    Logger.LogInformation("Retrying request.. RetryCount" + retryCount);
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, cancelToken);
                    }
                }
            }
            catch (AggregateException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new AggregateException(e);
            }
        }
    }
}
