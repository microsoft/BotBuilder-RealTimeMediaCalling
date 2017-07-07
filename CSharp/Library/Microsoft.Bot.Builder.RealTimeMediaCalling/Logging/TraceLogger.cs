using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    /// <summary>
    /// Default, simple IRealtimeMediaLogger implementation that uses the .NET Trace class
    /// </summary>
    /// <seealso cref="Microsoft.Bot.Builder.RealTimeMediaCalling.IRealTimeMediaLogger" />
    public class TraceLogger : IRealTimeMediaLogger
    {
        void IRealTimeMediaLogger.LogError(string message)
        {
            Trace.TraceError(message);
        }

        void IRealTimeMediaLogger.LogError(string format, params object[] args)
        {
            Trace.TraceError(format, args);
        }

        void IRealTimeMediaLogger.LogInformation(string message)
        {
            Trace.TraceInformation(message);
        }

        void IRealTimeMediaLogger.LogInformation(string format, params object[] args)
        {
            Trace.TraceInformation(format, args);
        }

        void IRealTimeMediaLogger.LogWarning(string message)
        {
            Trace.TraceWarning(message);
        }

        void IRealTimeMediaLogger.LogWarning(string format, params object[] args)
        {
            Trace.TraceWarning(format, args);
        }
    }
}
