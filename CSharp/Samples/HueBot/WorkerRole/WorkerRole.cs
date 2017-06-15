/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using FrontEnd.Logging;
using FrontEnd;

namespace WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {        
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        /// <summary>
        /// Keep the service running until OnStop is called 
        /// </summary>
        public override void Run()
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, "WorkerRole is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        /// <summary>
        /// Initialize and start the service when workerrole is started
        /// </summary>
        /// <returns></returns>
        public override bool OnStart()
        {

            try
            {
                // Wire up exception handling for unhandled exceptions (bugs).
                AppDomain.CurrentDomain.UnhandledException += this.OnAppDomainUnhandledException;
                TaskScheduler.UnobservedTaskException += this.OnUnobservedTaskException;

                // Set the maximum number of concurrent connections
                ServicePointManager.DefaultConnectionLimit = 12;
                AzureConfiguration.Instance.Initialize();

                // Create and start the environment-independent service.
                Service.Instance.Initialize(AzureConfiguration.Instance);
                Service.Instance.Start();

                bool result = base.OnStart();

                Log.Info(new CallerInfo(), LogContext.FrontEnd, "WorkerRole has been started");

                return result;
            }
            catch(Exception e)
            {
                Log.Error(new CallerInfo(), LogContext.FrontEnd, "Exception on startup: {0}", e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Cleanup when WorkerRole is stopped
        /// </summary>
        public override void OnStop()
        {
            try
            {
                Log.Info(new CallerInfo(), LogContext.FrontEnd, "WorkerRole is stopping");

                this.cancellationTokenSource.Cancel();
                this.runCompleteEvent.WaitOne();

                base.OnStop();

                Log.Info(new CallerInfo(), LogContext.FrontEnd, "WorkerRole has stopped");
            }
            catch (Exception e)
            {
                Log.Error(new CallerInfo(), LogContext.FrontEnd, "Exception on shutdown: {0}", e.ToString());
                throw;
            }
            finally
            {
                AppDomain.CurrentDomain.UnhandledException -= this.OnAppDomainUnhandledException;
                TaskScheduler.UnobservedTaskException -= this.OnUnobservedTaskException;
                Log.Flush();
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }
        }

        /// <summary>
        /// Log UnObservedTaskExceptions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Log.Error(new CallerInfo(), LogContext.FrontEnd, "Unobserved task exception: " + e.Exception.ToString());
        }

        /// <summary>
        /// Log any unhandled exceptions that are raised in the service
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error(new CallerInfo(), FrontEnd.Logging.LogContext.FrontEnd, "Unhandled exception: " + e.ExceptionObject.ToString());
            Log.Flush(); // process may or may not be terminating so flush log just in case.
        }
    }
}
