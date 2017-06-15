// -----------------------------------------------------------------------
// <copyright file="CognitiveServicesAuthorizationProvider.cs" company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// -----------------------------------------------------------------------

namespace FrontEnd.Http
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Bing.Speech;

    /// <summary>
    /// Cognitive Services Authorization Provider to contact bing speech services
    /// </summary>
    public sealed class CognitiveServicesAuthorizationProvider : IAuthorizationProvider
    {
        /// <summary>
        /// The fetch token URI
        /// </summary>
        private const string FetchTokenUri = "https://api.cognitive.microsoft.com/sts/v1.0";

        /// <summary>
        /// The subscription key
        /// </summary>
        private readonly string _subscriptionKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="CognitiveServicesAuthorizationProvider" /> class.
        /// </summary>
        /// <param name="subscriptionKey">The subscription identifier.</param>
        public CognitiveServicesAuthorizationProvider(string subscriptionKey)
        {
            if (subscriptionKey == null)
            {
                throw new ArgumentNullException(nameof(subscriptionKey));
            }

            if (string.IsNullOrWhiteSpace(subscriptionKey))
            {
                throw new ArgumentException(nameof(subscriptionKey));
            }

            _subscriptionKey = subscriptionKey;
        }

        /// <summary>
        /// Gets the authorization token asynchronously.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous read operation. The value of the string parameter contains the next the authorization token.
        /// </returns>
        /// <remarks>
        /// This method should always return a valid authorization token at the time it is called.
        /// </remarks>
        public Task<string> GetAuthorizationTokenAsync()
        {
            return FetchToken(FetchTokenUri, this._subscriptionKey);
        }

        /// <summary>
        /// Fetches the token.
        /// </summary>
        /// <param name="fetchUri">The fetch URI.</param>
        /// <param name="subscriptionKey">The subscription key.</param>
        /// <returns>An access token.</returns>
        private static async Task<string> FetchToken(string fetchUri, string subscriptionKey)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                var uriBuilder = new UriBuilder(fetchUri);
                uriBuilder.Path += "/issueToken";

                using (var result = await client.PostAsync(uriBuilder.Uri.AbsoluteUri, null).ConfigureAwait(false))
                {
                    return await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
        }
    }
}