/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FrontEnd.Logging
{
    /// <summary>
    /// Different contexts for which log statements are produced.  Each of these contexts
    /// has a corresponding TraceSource entry in the WorkerRole's app.config file.
    /// </summary>
    public enum LogContext
    {
        FrontEnd,
        Media
    }

    /// <summary>
    /// Wrapper class for logging.  This class provides a common mechanism for logging throughout the application.
    /// </summary>
    public static class Log
    {
        private static readonly Dictionary<LogContext, TraceSource> traceSources = new Dictionary<LogContext, TraceSource>();

        static Log()
        {
            foreach (LogContext context in Enum.GetValues(typeof(LogContext)))
            {
                traceSources[context] = new TraceSource(context.ToString());
            }
        }

        /// <summary>
        /// Checks if Verbose method is on
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool IsVerboseOn(LogContext context)
        {
            TraceSource traceSource = traceSources[context];
            return traceSource.Switch.Level >= SourceLevels.Verbose || traceSource.Switch.Level == SourceLevels.All;
        }

        /// <summary>
        /// Verbose logging of the message
        /// </summary>
        /// <param name="callerInfo"></param>
        /// <param name="context"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Verbose(CallerInfo callerInfo, LogContext context, string format, params object[] args)
        {
            Log.Write(TraceEventType.Verbose, callerInfo, context, format, args);
        }

        /// <summary>
        /// Info level logging of the message
        /// </summary>
        /// <param name="callerInfo"></param>
        /// <param name="context"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Info(CallerInfo callerInfo, LogContext context, string format, params object[] args)
        {
            Log.Write(TraceEventType.Information, callerInfo, context, format, args);
        }

        /// <summary>
        /// Warning level logging of the message
        /// </summary>
        /// <param name="callerInfo"></param>
        /// <param name="context"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Warning(CallerInfo callerInfo, LogContext context, string format, params object[] args)
        {
            Log.Write(TraceEventType.Warning, callerInfo, context, format, args);
        }

        /// <summary>
        /// Error level logging of the message
        /// </summary>
        /// <param name="callerInfo"></param>
        /// <param name="context"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Error(CallerInfo callerInfo, LogContext context, string format, params object[] args)
        {
            Log.Write(TraceEventType.Error, callerInfo, context, format, args);
        }

        /// <summary>
        /// Flush the log trace sources
        /// </summary>
        public static void Flush()
        {
            foreach (TraceSource traceSource in traceSources.Values)
            {
                traceSource.Flush();
            }
        }

        private static void Write(TraceEventType level, CallerInfo callerInfo, LogContext context, string format, params object[] args)
        {
            try
            {
                string correlationId = CorrelationId.GetCurrentId() ?? "-";
                string callerInfoString = (callerInfo == null) ? "-" : callerInfo.ToString();
                string tracePrefix = "[" + correlationId + " " + callerInfoString + "] ";
                if (args.Length == 0)
                {
                    traceSources[context].TraceEvent(level, 0, tracePrefix + format);
                }
                else
                {
                    traceSources[context].TraceEvent(level, 0, string.Format(tracePrefix + format, args));
                }

            }catch(Exception ex)
            {
                Trace.TraceError("Error in Log.cs" + ex.ToString());
            }
        }
    }
}
