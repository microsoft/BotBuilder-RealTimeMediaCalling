/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using FrontEnd.Logging;

namespace FrontEnd.Http
{
    internal class ExceptionLogger : IExceptionLogger
    {
        public ExceptionLogger()
        {
        }

        public Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            Log.Error(new CallerInfo(), LogContext.FrontEnd, "Exception processing HTTP request. {0}", context.Exception.ToString());
            return Task.FromResult<object>(null);
        }
    }
}
