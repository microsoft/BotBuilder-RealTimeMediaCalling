/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System;
using System.Threading.Tasks;
using FrontEnd.Logging;
using Microsoft.Bot.Connector;

namespace FrontEnd.Http
{
    /// <summary>
    /// Helper to send chat messages
    /// </summary>
    internal class MessageSender
    { 
        /// <summary>
        /// Send chat message on a conversation thread with the url as the text message
        /// </summary>
        /// <param name="threadId">ThreadId for the conversation that the chat message needs to be sent</param>
        /// <param name="urlText">Url that needs to be send in the chat message</param>
        /// <returns></returns>
        public static async Task SendMessage(string threadId, string urlText)
        {
            try
            {
                if (MessagesController.MessagesServiceUrl == null)
                {
                    Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Bot has not received a chat message before. So it cannot send a message back");
                    return;
                }

                var connector = new ConnectorClient(new Uri(MessagesController.MessagesServiceUrl));
                IMessageActivity newMessage = Activity.CreateMessageActivity();
                newMessage.Type = ActivityTypes.Message;
                newMessage.From = MessagesController.BotAccount;
                newMessage.Conversation = new ConversationAccount(true, threadId);
                newMessage.Text = "[Click here for video shots from the conference](" + urlText + ")";
                await connector.Conversations.SendToConversationAsync((Activity)newMessage);
                Log.Info(new CallerInfo(), LogContext.FrontEnd, $"sent message");
            }
            catch(Exception ex)
            {
                Log.Error(new CallerInfo(), LogContext.FrontEnd, $"{ex}");
            }
        }
    }
}
