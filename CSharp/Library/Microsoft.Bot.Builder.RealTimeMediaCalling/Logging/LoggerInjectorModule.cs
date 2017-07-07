using Autofac;
using Autofac.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.Logging
{
    /// <summary>
    /// Automatically injects Logger into relevant properties for objects instantiated by Autofac container
    /// </summary>
    public class LoggerInjectorModule<TLogger> : Module where TLogger:class 
    {
        private readonly TLogger _logger;

        public LoggerInjectorModule(TLogger logger)
        {
            _logger = logger;
        }
        
        protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry, IComponentRegistration registration)
        {
            //handles property injection
            registration.Activated += OnRegistrationActivation;
        }

        /// <summary>
        /// Injects the logger in the appropriate properties for each instantiated object
        /// </summary>
        void OnRegistrationActivation(object sender, ActivatedEventArgs<object> e)
        {
            foreach (var property in e.Instance.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(TLogger)
                    && property.CanWrite
                    && property.GetIndexParameters().Length == 0 //checks that this is not an indexed property
                )
                    property.SetValue(e.Instance, _logger);
            }
        }
    }
}
