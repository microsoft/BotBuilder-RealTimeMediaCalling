/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using FrontEnd.Logging;
using Owin;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Misc;

namespace FrontEnd.Http
{
    /// <summary>
    /// Initialize the httpConfiguration for OWIN
    /// </summary>
    public class CallEndpointStartup
    {
        /// <summary>
        /// Configuration settings like Auth, Routes for OWIN
        /// </summary>
        /// <param name="app"></param>

        public void Configuration(IAppBuilder app)
        {
            HttpConfiguration httpConfig = new HttpConfiguration();
            httpConfig.MapHttpAttributeRoutes();
            httpConfig.MessageHandlers.Add(new LoggingMessageHandler(isIncomingMessageHandler: true, logContext: LogContext.FrontEnd));
                      
            httpConfig.Services.Add(typeof(IExceptionLogger), new ExceptionLogger());
            httpConfig.Formatters.JsonFormatter.SerializerSettings = RealTimeMediaSerializer.GetSerializerSettings();
            httpConfig.EnsureInitialized();

            app.UseWebApi(httpConfig);
        }
    }
}
