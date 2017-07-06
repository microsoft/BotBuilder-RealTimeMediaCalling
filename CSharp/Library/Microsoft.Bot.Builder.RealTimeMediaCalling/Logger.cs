using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    public static class Logger
    {
        public static IRealTimeMediaLogger LoggerInstance = new TraceLogger();

        public static void LogInformation(string message)
        {
            LoggerInstance.LogInformation(message);
        }

        public static void LogWarning(string message)
        {
            LoggerInstance.LogWarning(message);
        }

        public static void LogError(string message)
        {
            LoggerInstance.LogError(message);
        }

        public static void LogInformation(string format, params object[] args)
        {
            LoggerInstance.LogInformation(format, args);
        }

        public static void LogWarning(string format, params object[] args)
        {
            LoggerInstance.LogWarning(format, args);
        }

        public static void LogError(string format, params object[] args)
        {
            LoggerInstance.LogError(format, args);
        }
    }

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
