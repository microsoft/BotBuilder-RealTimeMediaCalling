// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Bot Framework: http://botframework.com
// 
// Bot Builder SDK GitHub:
// https://github.com/Microsoft/BotBuilder
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Net.Http;
using Autofac;
using Microsoft.Bot.Builder.RealTimeMediaCalling.Logging;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    internal static class RealTimeMediaCallingScope
    {
        public static readonly object LifetimeScopeTag = typeof(RealTimeMediaCallingScope);

        public static ILifetimeScope BeginLifetimeScope(ILifetimeScope scope, HttpRequestMessage request = null)
        {
            var inner = scope.BeginLifetimeScope(LifetimeScopeTag);
            if (request != null)
            {
                inner.Resolve<HttpRequestMessage>(TypedParameter.From(request));
            }
            return inner;
        }
    }

    /// <summary>
    /// Autofac module for real-time media calling components.
    /// </summary>
    internal class RealTimeMediaCallingModule<TBotService, TCallService> : Module 
        where TBotService : IInternalRealTimeMediaBotService
        where TCallService : IInternalRealTimeMediaCallService
    {
        public readonly object LifetimeScopeTag;

        IRealTimeMediaLogger _logger;

        public RealTimeMediaCallingModule(object scopeTag, IRealTimeMediaLogger logger)
        {
            LifetimeScopeTag = scopeTag;
            _logger = logger ?? new TraceLogger();
        }

        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.RegisterModule(new LoggerInjectorModule<IRealTimeMediaLogger>(_logger));
            
            builder
               .Register((c, p) => p.TypedAs<HttpRequestMessage>())
               .AsSelf()
               .InstancePerMatchingLifetimeScope(LifetimeScopeTag);

            builder
               .RegisterType<RealTimeMediaCallingContext>()
               .AsSelf()
                .InstancePerMatchingLifetimeScope(LifetimeScopeTag);

            builder
                .RegisterType<TBotService>()
                .As<IInternalRealTimeMediaBotService>()
                .As<IRealTimeMediaBotService>()
                .SingleInstance();

            builder
                .Register((c, p) => p.TypedAs<RealTimeMediaCallServiceParameters>())
                .AsSelf()
                .InstancePerLifetimeScope();

            builder
                .RegisterType<TCallService>()
                .As<IInternalRealTimeMediaCallService>()
                .As<IRealTimeMediaCallService>()
                .InstancePerLifetimeScope();
        }
    }

    public sealed class RealTimeMediaCallingModule : Module
    {
        private readonly Module _innerModule;

        /// <summary>
        /// Initializes a new instance of the <see cref="RealTimeMediaCallingModule"/> class.
        /// </summary>
        /// <param name="scopeTag">The scope tag.</param>
        /// <param name="logger">The logger. if null, defaults to TraceLogger</param>
        public RealTimeMediaCallingModule(object scopeTag, IRealTimeMediaLogger logger)
        {
            _innerModule = new RealTimeMediaCallingModule<RealTimeMediaBotService, RealTimeMediaCallService>(scopeTag, logger);
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule(_innerModule);
            base.Load(builder);
        }
    }

    /// <summary>
    /// Module for real-media calling
    /// </summary>
    internal sealed class RealTimeMediaCallingModule_MakeBot : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder
                .Register((c, p) => p.TypedAs<IRealTimeMediaCallServiceSettings>())
                .AsSelf()
                .SingleInstance();

            builder
                .Register((c, p) => p.TypedAs<Func<IRealTimeMediaBotService, IRealTimeMediaBot>>())
                .AsSelf()
                .SingleInstance();

            builder
                .Register(c =>
                {
                    var make = c.ResolveOptional<Func<IRealTimeMediaBotService, IRealTimeMediaBot>>();
                    if (null == make)
                    {
                        return null;
                    }
                    var service = c.Resolve<IRealTimeMediaBotService>();
                    return make(service);
                })
                .As<IRealTimeMediaBot>()
                .SingleInstance();

            builder
                .Register((c, p) => p.TypedAs<Func<IRealTimeMediaCallService, IRealTimeMediaCall>>())
                .AsSelf()
                .SingleInstance();

            builder
                .Register((c, p) =>
                {
                    var make = c.ResolveOptional<Func<IRealTimeMediaCallService, IRealTimeMediaCall>>();
                    if (null == make)
                    {
                        return null;
                    }
                    var service = c.Resolve<IRealTimeMediaCallService>();
                    return make(service);
                })
                .As<IRealTimeMediaCall>();
        }

        /// <summary>
        /// Register the function to create a bot and to retrieve bot settings
        /// </summary>
        /// <param name="scope">The lifetime scope</param>
        /// <param name="settings">The real time media call service settings.</param>
        /// <param name="makeBot">The function to make a bot.</param>
        /// <param name="makeCall">The function to make a call.</param>
        public static void Register(ILifetimeScope scope, IRealTimeMediaCallServiceSettings settings, Func<IRealTimeMediaBotService, IRealTimeMediaBot> makeBot, Func<IRealTimeMediaCallService, IRealTimeMediaCall> makeCall)
        {
            if (null == settings)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (null == makeBot)
            {
                throw new ArgumentNullException(nameof(makeCall));
            }

            if (null == makeCall)
            {
                throw new ArgumentNullException(nameof(makeCall));
            }

            scope.Resolve<IRealTimeMediaCallServiceSettings>(TypedParameter.From(settings));
            scope.Resolve<Func<IRealTimeMediaBotService, IRealTimeMediaBot>>(TypedParameter.From(makeBot));
            scope.Resolve<Func<IRealTimeMediaCallService, IRealTimeMediaCall>>(TypedParameter.From(makeCall));
        }
    }
}
