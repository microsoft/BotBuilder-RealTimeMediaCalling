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
using System.Threading.Tasks;
using Microsoft.Bot.Builder.RealTimeMediaCalling.Events;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    /// <summary>
    /// Type of responses returned to the bot
    /// </summary>
    public enum ResponseType
    {
        /// <summary>
        /// Request was accepted or processed.
        /// </summary>
        Accepted,

        /// <summary>
        /// Request was semantically/syntactically invalid.
        /// </summary>
        BadRequest,

        /// <summary>
        /// The call for the incoming callback or notification is no longer active.
        /// </summary>
        NotFound
    }

    /// <summary>
    /// Stores the type of the response and the content
    /// </summary>
    public class ResponseResult
    {
        /// <summary>
        /// Type of the response
        /// </summary>
        public readonly ResponseType ResponseType;

        /// <summary>
        /// 
        /// </summary>
        public readonly string Content;

        /// <summary>
        /// Creates ResponseResult with the type of the response and the content
        /// </summary>
        public ResponseResult(ResponseType responseType, string content = null)
        {
            ResponseType = responseType;
            Content = content;
        }
    }

    /// <summary>
    /// Processes the incoming requests and invokes the appropriate handlers for the call
    /// </summary>
    public interface IRealTimeMediaBotService
    {
        /// <summary>
        /// Event raised when a new call is created.
        /// </summary>
        event Func<RealTimeMediaCallEvent, Task> OnCallCreated;

        /// <summary>
        /// Event raised when an existing call is requested.
        /// </summary>
        event Func<RealTimeMediaCallEvent, Task> OnCallEnded;

        /// <summary>
        /// Method for the bot to join an existing conversation
        /// </summary>
        Task JoinCall(JoinCallAppHostedMedia joinCallAppHostedMedia, string callId);

        /// <summary>
        /// Returns the list of all active call ids.
        /// </summary>
        IList<string> CallIds { get; }

        /// <summary>
        /// Returns the list of all active calls.
        /// </summary>
        IList<IRealTimeMediaCall> Calls { get; }

        /// <summary>
        /// Fetches the call for the given id.
        /// </summary>
        /// <param name="id">The ID of the call.</param>
        /// <returns>The real time media call, or null.</returns>
        IRealTimeMediaCall GetCallForId(string id);
    }

    internal interface IInternalRealTimeMediaBotService : IRealTimeMediaBotService
    {
        /// <summary>
        /// Processes incoming call request
        /// </summary>
        /// <param name="content">Content from the request</param>
        /// <param name="skypeChainId">X-Microsoft-Skype-Chain-Id header value used to associate calls across different services</param>
        /// <returns></returns>
        Task<ResponseResult> ProcessIncomingCallAsync(string content, string skypeChainId);

        /// <summary>
        /// Processes requests sent to the callback url
        /// </summary>
        /// <param name="content">Content from the request</param>
        /// <param name="skypeChainId">X-Microsoft-Skype-Chain-Id header value used to associate calls across different services</param>
        /// <returns></returns>
        Task<ResponseResult> ProcessCallbackAsync(string content , string skypeChainId);

        /// <summary>
        /// Processes requests sent to notification url
        /// </summary>
        /// <param name="content">Content from the request</param>
        /// <returns></returns>
        Task<ResponseResult> ProcessNotificationAsync(string content);
    }
}
