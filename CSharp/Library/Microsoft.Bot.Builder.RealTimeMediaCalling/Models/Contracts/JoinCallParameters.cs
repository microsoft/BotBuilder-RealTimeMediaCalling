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

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts
{
    /// <summary>
    /// Parameters required for joining a call.
    /// </summary>
    public class JoinCallParameters
    {
        /// <summary>
        /// Join call switching enum
        /// </summary>
        internal enum JoinCallMode
        {
            JoinToken,
            FiveParameterJoin
        }

        /// <summary>
        /// The join call mode used by these parameters.
        /// </summary>
        internal JoinCallMode JoinCallSwitch { get; }

        /// <summary>
        /// The ID of the conversation we are joining.
        /// </summary>
        public string CallLegId { get; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Conversation join token. This value defines the target group conversation
        /// to be joined.
        /// </summary>
        public string JoinToken { get; }

        /// <summary>
        /// TenantId passed in to identify a specific meeting
        /// to be joined.
        /// </summary>
        public Guid TenantId { get; }

        /// <summary>
        /// The id of the thread, for multiparty calls.
        /// </summary>
        public string ThreadId { get; }

        /// <summary>
        /// The id of the thread message, for multiparty calls.
        /// </summary>
        public string ThreadMessageId { get; }

        /// <summary>
        /// The Id of the organizer of the meeting to be joined
        /// </summary>
        public Guid OrganizerId { get; }

        /// <summary>
        /// Reply chain message id of the meeting to be joined
        /// </summary>
        public string ReplyChainMessageId { get; }

        /// <summary>
        /// Joins the conversation as a hidden entity
        /// </summary>
        public bool? Hidden { get; set; }

        /// <summary>
        /// Custom display name of the bot
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Custom ID of the bot
        /// </summary>
        public string JoinAsId { get; set; }

        /// <summary>
        /// Join the bot with this ID.
        /// <param name="threadId">ThreadId for the meeting to be joined</param>
        /// <param name="threadMessageId">threadMessageId for the meeting to be joined</param>
        /// <param name="tenantId">tenantId for the meeting to be joined</param>
        /// <param name="organizerId">organizerId for the meeting to be joined</param>
        /// <param name="replyChainMessageId">reply chaing message id for the meeting to be joined</param>
        /// </summary>
        //TODO remove joinToken once the other four parameters are supported on PMA side
        public JoinCallParameters(
            string threadId,
            string threadMessageId,
            Guid tenantId,
            Guid organizerId,
            string replyChainMessageId = null)
        { 
            JoinCallSwitch = JoinCallMode.FiveParameterJoin;

            ThreadId = threadId;
            ThreadMessageId = threadMessageId;
            OrganizerId = organizerId;
            ReplyChainMessageId = replyChainMessageId;
            TenantId = tenantId;
        }

        /// <summary>
        /// Join a call with the provided join token or a conversation
        /// url passed as join token.
        /// </summary>
        /// <param name="joinToken">The join token/conversation url</param>
        internal JoinCallParameters(string joinToken)
        {
            JoinCallSwitch = JoinCallMode.JoinToken;
            JoinToken = joinToken;
        }
    }
}
