/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System;
using FrontEnd.Http;
using Microsoft.Owin.Hosting;
using Microsoft.Skype.Bots.Media;

namespace FrontEnd
{
    /// <summary>
    /// Service is the main entry point independent of Azure.  Anyone instantiating Service needs to first
    /// initialize the DependencyResolver.  Calling Start() on the Service starts the HTTP server that will
    /// listen for incoming Conversation requests from the Skype Platform.
    /// </summary>
    public class Service
    {
        private readonly object _syncLock = new object();
        private bool _initialized;

        private IDisposable _callHttpServer;
        private bool _started = false;
        public readonly string DefaultSendVideoFormat;
       
        public IConfiguration Configuration { get; private set; }

        public static readonly Service Instance = new Service();
     
        /// <summary>
        /// Instantiate a custom server (e.g. for testing).
        /// </summary>
        /// <param name="listeningUris">HTTP urls to listen for incoming call signaling requests.</param>
        /// <param name="callProcessor">The call processor instance.</param>
        public void Initialize(IConfiguration config)
        {
            lock (_syncLock)
            {
                if (_initialized)
                {
                    throw new InvalidOperationException("Service is already initialized");
                }
            }

            Configuration = config;

            MediaPlatform.Initialize(config.MediaPlatformSettings);

            _initialized = true;
        }

        /// <summary>
        /// Start the service.
        /// </summary>
        public void Start()
        {
            lock (_syncLock)
            {
                if (_started)
                {
                    throw new InvalidOperationException("The service is already started.");
                }
               
                // Start HTTP server for calls
                StartOptions callStartOptions = new StartOptions();
                foreach (Uri url in Configuration.CallControlListeningUrls)
                {
                    callStartOptions.Urls.Add(url.ToString());
                }

                this._callHttpServer = WebApp.Start(
                    callStartOptions,
                    (appBuilder) =>
                    {
                        var startup = new CallEndpointStartup();
                        startup.Configuration(appBuilder);
                    });
            
                _started = true;
            }
        }

        /// <summary>
        /// Stop the service.
        /// </summary>
        public void Stop()
        {
            lock (_syncLock)
            {
                if (!this._started)
                {
                    throw new InvalidOperationException("The service is already stopped.");
                }

                this._started = false;
            }

            this._callHttpServer.Dispose();
        }
    }
}
