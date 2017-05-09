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
using System.Collections.Generic;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Misc
{
    /// <summary>
    /// Helper class for serializing/deserializing
    /// </summary>
    public static class RealTimeMediaSerializer
    {
        private static readonly JsonSerializerSettings defaultSerializerSettings = GetSerializerSettings();
        private static readonly JsonSerializerSettings loggingSerializerSettings = GetSerializerSettings(Formatting.Indented);

        /// <summary>
        /// Serialize input object to string
        /// </summary>
        public static string SerializeToJson(object obj, bool forLogging = false)
        {
            return JsonConvert.SerializeObject(obj, forLogging ? loggingSerializerSettings : defaultSerializerSettings);
        }

        /// <summary>
        /// Serialize to JToken
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static JToken SerializeToJToken(Object obj)
        {
            return JToken.FromObject(obj, JsonSerializer.Create(defaultSerializerSettings));
        }

        /// <summary>
        /// Deserialize input string to object
        /// </summary>
        public static T DeserializeFromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, defaultSerializerSettings);
        }

        /// <summary>
        /// Deserialize from JToken
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jToken"></param>
        /// <returns></returns>
        public static T DeserializeFromJToken<T>(JToken jToken)
        {
            return jToken.ToObject<T>(JsonSerializer.Create(defaultSerializerSettings));
        }

        /// <summary>
        /// Returns default serializer settings.
        /// </summary>
        public static JsonSerializerSettings GetSerializerSettings(Formatting formatting = Formatting.None)
        {
            return new JsonSerializerSettings()
            {
                Formatting = formatting,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                Converters = new List<JsonConverter> { new StringEnumConverter { CamelCaseText = true }, new Contracts.RealTimeMediaActionConverter(), new RealTimeMediaOperationOutcomeConverter(), new NotificationConverter() },
            };
        }
    }
}
