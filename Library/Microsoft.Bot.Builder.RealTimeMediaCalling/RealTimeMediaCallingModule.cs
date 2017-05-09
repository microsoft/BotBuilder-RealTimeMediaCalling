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

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    /// <summary>
    /// Autofac module for real-time media calling components.
    /// </summary>
    internal sealed class RealTimeMediaCallingModule : Module
    {
        public static readonly object LifetimeScopeTag = typeof(RealTimeMediaCallingModule);

        public static ILifetimeScope BeginLifetimeScope(ILifetimeScope scope, HttpRequestMessage request)
        {
            var inner = scope.BeginLifetimeScope(LifetimeScopeTag);
            inner.Resolve<HttpRequestMessage>(TypedParameter.From(request));
            return inner;
        }

        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder
               .Register((c, p) => p.TypedAs<HttpRequestMessage>())
               .AsSelf()
               .InstancePerMatchingLifetimeScope(LifetimeScopeTag);

            builder
               .RegisterType<RealTimeMediaCallingContext>()
               .AsSelf()
               .InstancePerMatchingLifetimeScope(LifetimeScopeTag);
         
            builder
                .Register(c => new RealTimeCallProcessor(c.Resolve<IRealTimeMediaCallServiceSettings>(), c.Resolve<Func<IRealTimeMediaCallService, IRealTimeMediaCall>>()))
                .AsSelf()
                .As<IRealTimeCallProcessor>()
                .SingleInstance();

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

            builder.RegisterModule(new RealTimeMediaCallingModule());

            builder
                .Register((c, p) => p.TypedAs<Func<IRealTimeMediaCallService, IRealTimeMediaCall>>())
                .AsSelf()
                .SingleInstance();

            builder
                .Register((c, p) => p.TypedAs<IRealTimeMediaCallServiceSettings>())
                .AsSelf()
                .SingleInstance();            
        }

        /// <summary>
        /// Register the function to create a bot and to retrieve bot settings
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="makeCallingBot"></param>
        /// <param name="RealTimeMediaCallingSettings"></param>
        public static void Register(ILifetimeScope scope, Func<IRealTimeMediaCallService, IRealTimeMediaCall> makeCallingBot, IRealTimeMediaCallServiceSettings RealTimeMediaCallingSettings)
        {            
            scope.Resolve<Func<IRealTimeMediaCallService, IRealTimeMediaCall>>(TypedParameter.From(makeCallingBot));
            scope.Resolve<IRealTimeMediaCallServiceSettings>(TypedParameter.From(RealTimeMediaCallingSettings));
        }
    }
}
