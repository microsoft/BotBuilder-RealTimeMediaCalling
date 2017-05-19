/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FrontEnd;
using FrontEnd.Http;
using FrontEnd.Logging;
using Microsoft.Azure;
using Microsoft.Skype.Bots.Media;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace WorkerRole
{
    /// <summary>
    /// Reads the configuration from service configuration
    /// </summary>
    internal class AzureConfiguration : IConfiguration
    {
        #region Fields
        private const string DefaultEndpointKey = "DefaultEndpoint";
        private const string InstanceCallControlEndpointKey = "InstanceCallControlEndpoint";
        private const string InstanceMediaControlEndpointKey = "InstanceMediaControlEndpoint";
        private const string ServiceDnsNameKey = "ServiceDnsName";
        private const string DefaultCertificateKey = "DefaultCertificate";
        private const string MicrosoftAppIdKey = "MicrosoftAppId";
        private const string MicrosoftAppPasswordKey = "MicrosoftAppPassword";
        private const string DefaultMicrosoftAppIdValue = "$MicrosoftAppId$";
        private const string DefaultMicrosoftAppPasswordValue = "$BotSecret$";

        //Prefix of the InstanceId from the RoleEnvironment 
        private const string InstanceIdToken = "in_";

        private static readonly AzureConfiguration s_Configuration = new AzureConfiguration();

        /// <summary>
        /// DomainNameLabel in NetworkConfiguration in .cscfg  <PublicIP name="instancePublicIP" domainNameLabel="pip"/>
        /// If the below changes, please change in the cscfg as well
        /// </summary>
        public const string DomainNameLabel = "pip";

        /// <summary>
        /// localPort specified in <InputEndpoint name="DefaultCallControlEndpoint" protocol="tcp" port="443" localPort="9440" />
        /// in .csdef. This is needed for running in emulator. Currently only messaging can be debugged in the emulator. 
        /// Media debugging in emulator will be supported in future releases.
        /// </summary>
        private const int DefaultPort = 9440;
        #endregion

        #region Properties
        public string ServiceDnsName { get; private set; }

        public IEnumerable<Uri> CallControlListeningUrls { get; private set; }

        public Uri CallControlCallbackUrl { get; private set; }

        public Uri NotificationCallbackUrl { get; private set; }

        public Uri AzureInstanceBaseUrl { get; private set; }

        public MediaPlatformSettings MediaPlatformSettings { get; private set; }

        public string MicrosoftAppId { get; private set; }

        public static AzureConfiguration Instance { get { return s_Configuration; } }

        #endregion

        #region Public Methods
        private AzureConfiguration()
        {
        }

        /// <summary>
        /// Initialize from serviceConfig
        /// </summary>
        public void Initialize()
        {
            // Collect config values from Azure config.
            TraceEndpointInfo();
            ServiceDnsName = GetString(ServiceDnsNameKey);
            X509Certificate2 defaultCertificate = GetCertificateFromStore(DefaultCertificateKey);

            RoleInstanceEndpoint instanceCallControlEndpoint = RoleEnvironment.IsEmulated ? null : GetEndpoint(InstanceCallControlEndpointKey);
            RoleInstanceEndpoint defaultEndpoint = GetEndpoint(DefaultEndpointKey);
            RoleInstanceEndpoint mediaControlEndpoint = RoleEnvironment.IsEmulated ? null : GetEndpoint(InstanceMediaControlEndpointKey);

            int instanceCallControlInternalPort = RoleEnvironment.IsEmulated ? DefaultPort : instanceCallControlEndpoint.IPEndpoint.Port;
            string instanceCallControlInternalIpAddress = RoleEnvironment.IsEmulated
                                                          ? IPAddress.Loopback.ToString()
                                                          : instanceCallControlEndpoint.IPEndpoint.Address.ToString();

            int instanceCallControlPublicPort = RoleEnvironment.IsEmulated ? DefaultPort : instanceCallControlEndpoint.PublicIPEndpoint.Port;
            int mediaInstanceInternalPort = RoleEnvironment.IsEmulated ? 8445 : mediaControlEndpoint.IPEndpoint.Port;
            int mediaInstancePublicPort = RoleEnvironment.IsEmulated ? 20100 : mediaControlEndpoint.PublicIPEndpoint.Port;

            string instanceCallControlIpEndpoint = string.Format("{0}:{1}", instanceCallControlInternalIpAddress, instanceCallControlInternalPort);

            MicrosoftAppId = ConfigurationManager.AppSettings[MicrosoftAppIdKey];
            if (string.IsNullOrEmpty(MicrosoftAppId) || string.Equals(MicrosoftAppId, DefaultMicrosoftAppIdValue))
            {
                throw new ConfigurationException("MicrosoftAppId", "Update app.config in WorkerRole with AppId from the bot registration portal");
            }

            string microsoftAppPassword = ConfigurationManager.AppSettings[MicrosoftAppPasswordKey];
            if (string.IsNullOrEmpty(microsoftAppPassword) || string.Equals(microsoftAppPassword, DefaultMicrosoftAppPasswordValue))
            {
                throw new ConfigurationException("MicrosoftAppPassword", "Update app.config in WorkerRole with BotSecret from the bot registration portal");
            }

            // Create structured config objects for service.
            CallControlCallbackUrl = new Uri(string.Format(
                "https://{0}:{1}/{2}/{3}/",
                ServiceDnsName,
                instanceCallControlPublicPort,
                HttpRouteConstants.CallSignalingRoutePrefix,
                HttpRouteConstants.OnCallbackRoute));

            NotificationCallbackUrl = new Uri(string.Format(
                "https://{0}:{1}/{2}/{3}/",
                ServiceDnsName,
                instanceCallControlPublicPort,
                HttpRouteConstants.CallSignalingRoutePrefix,
                HttpRouteConstants.OnNotificationRoute));

            AzureInstanceBaseUrl = new Uri(string.Format(
                "https://{0}:{1}/",
                ServiceDnsName,
                instanceCallControlPublicPort));

            TraceConfigValue("CallControlCallbackUri", CallControlCallbackUrl);
            List<Uri> controlListenUris = new List<Uri>();
            
            if (RoleEnvironment.IsEmulated)
            {
                controlListenUris.Add(new Uri("https://" + defaultEndpoint.IPEndpoint.Address + ":" + DefaultPort + "/"));
            }
            else            
            {
                controlListenUris.Add(new Uri("https://" + instanceCallControlIpEndpoint + "/"));
                controlListenUris.Add(new Uri("https://" + defaultEndpoint.IPEndpoint + "/"));
            };
            CallControlListeningUrls = controlListenUris;

            foreach (Uri uri in CallControlListeningUrls)
            {
                TraceConfigValue("Call control listening Uri", uri);
            }

           IPAddress publicInstanceIpAddress = RoleEnvironment.IsEmulated
                                                ? IPAddress.Loopback
                                                : GetInstancePublicIpAddress(ServiceDnsName);

            MediaPlatformSettings = new MediaPlatformSettings()
            {
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings()
                {
                    CertificateThumbprint = defaultCertificate.Thumbprint,
                    InstanceInternalPort = mediaInstanceInternalPort,
                    InstancePublicIPAddress = publicInstanceIpAddress,
                    InstancePublicPort = mediaInstancePublicPort,
                    ServiceFqdn = ServiceDnsName
                },

                ApplicationId = MicrosoftAppId
            };
        }

        /// <summary>
        /// Dispose the configuration
        /// </summary>
        public void Dispose()
        {
        }
        #endregion

        #region Helper methods
        private static void TraceEndpointInfo()
        {
            string[] endpoints = RoleEnvironment.IsEmulated
                ? new string[] { DefaultEndpointKey }
                : new string[] { DefaultEndpointKey, InstanceMediaControlEndpointKey };

            foreach (string endpointName in endpoints)
            {
                RoleInstanceEndpoint endpoint = GetEndpoint(endpointName);
                StringBuilder info = new StringBuilder();
                info.AppendFormat("Internal=https://{0}, ", endpoint.IPEndpoint);
                string publicInfo = endpoint.PublicIPEndpoint == null ? "-" : endpoint.PublicIPEndpoint.Port.ToString();
                info.AppendFormat("PublicPort={0}", publicInfo);
                TraceConfigValue(endpointName, info);
            }
        }

        private static void TraceConfigValue(string key, object value)
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"{key} ->{value}");         
        }

        private static RoleInstanceEndpoint GetEndpoint(string name)
        {
            RoleInstanceEndpoint endpoint;
            if (!RoleEnvironment.CurrentRoleInstance.InstanceEndpoints.TryGetValue(name, out endpoint))
            {
                throw new ConfigurationException(name, "No endpoint with name '{0}' was found.", name);
            }

            return endpoint;
        }

        private static string GetString(string key, bool allowEmpty = false)
        {
            string s = CloudConfigurationManager.GetSetting(key);

            TraceConfigValue(key, s);

            if (!allowEmpty && string.IsNullOrWhiteSpace(s))
            {
                throw new ConfigurationException(key, "The configuration value is null or empty.");
            }

            return s;
        }

        private static List<string> GetStringList(string key)
        {
            return GetString(key).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private static X509Certificate2 GetCertificateFromStore(string key)
        {
            string thumbprint = GetString(key);

            X509Certificate2 cert;

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            try
            {
                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
                if (certs.Count != 1)
                {
                    throw new ConfigurationException(key, "No certificate with thumbprint {0} was found in the machine store.", thumbprint);
                }

                cert = certs[0];
            }
            finally
            {
                store.Close();
            }

            return cert;
        }

        /// <summary>
        /// Get the PIP for this instance
        /// </summary>
        /// <returns></returns>
        private static IPAddress GetInstancePublicIpAddress(string publicFqdn)
        {           
            int instanceNumber;
            //get the instanceId for the current instance. It will be of the form  XXMediaBotRole_IN_0. Look for IN_ and then extract the number after it
            //Assumption: in_<instanceNumber> will the be the last in the instanceId
            string instanceId = RoleEnvironment.CurrentRoleInstance.Id;
            int instanceIdIndex = instanceId.IndexOf(InstanceIdToken, StringComparison.OrdinalIgnoreCase);
            if (!Int32.TryParse(instanceId.Substring(instanceIdIndex + InstanceIdToken.Length), out instanceNumber))
            {
                Log.Error(new CallerInfo(), LogContext.FrontEnd, "Couldn't extract Instance index from {0}", instanceId);
                throw new Exception("Couldn't extract Instance index from " + instanceId);
            }

            //for example: instance0 for fooservice.cloudapp.net will have hostname as pip.0.fooservice.cloudapp.net
            string instanceHostName = DomainNameLabel + "." + instanceNumber + "." + publicFqdn;
            IPAddress[] instanceAddresses = Dns.GetHostEntry(instanceHostName).AddressList;
            if(instanceAddresses.Length == 0)
            {
                throw new InvalidOperationException("Could not resolve the PIP hostname. Please make sure that PIP is properly configured for the service");
            }
            return instanceAddresses[0];
        }
        #endregion
    }

    /// <summary>
    /// Exception thrown when the configuration is not correct
    /// </summary>
    internal sealed class ConfigurationException : Exception
    {
        internal ConfigurationException(string parameter, string message, params object[] args)
            : base(string.Format(message, args))
        {
            Parameter = parameter;
        }

        public string Parameter { get; private set; }

        public override string Message
        {
            get
            {
                return string.Format(
                    "Parameter name: {0}\r\n{1}",
                    Parameter,
                    base.Message);
            }
        }

        public override string ToString()
        {
            return string.Format("Parameter name: {0}\r\n{1}", Parameter, base.ToString());
        }
    }
}
