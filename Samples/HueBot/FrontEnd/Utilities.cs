using System;
using System.Threading.Tasks;
using FrontEnd.Logging;

namespace FrontEnd
{
    internal static class Utilities
    {
        /// <summary>
        /// Extension for Task to execute the task in background and log any exception
        /// </summary>
        /// <param name="task"></param>
        /// <param name="description"></param>
        public static async void ForgetAndLogException(this Task task, string description = null)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                //ignore
                Log.Error(new CallerInfo(),
                    LogContext.FrontEnd,
                    "Caught an Exception running the task: {0} \n StackTrace: {1}", e.Message, e.StackTrace);
            }
        }
    }
}
