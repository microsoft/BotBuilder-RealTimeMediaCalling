/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System;
using System.Runtime.Remoting.Messaging;

namespace FrontEnd.Logging
{
    internal class CorrelationId
    {
        private class Holder : MarshalByRefObject
        {
            public string Id;
        }

        internal const string LogicalDataName = "FrontEnd.Logging.CorrelationId";

        /// <summary>
        /// Sets the current correlation ID.  This is necessary to call in event handler callbacks because the event producer
        /// may not be aware of the call id.
        /// </summary>
        /// <param name="value"></param>
        public static void SetCurrentId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            Holder holder = CallContext.LogicalGetData(LogicalDataName) as Holder;
            if (holder == null)
            {
                CallContext.LogicalSetData(LogicalDataName, new Holder { Id = value });
            }
            else
            {
                try
                {
                    holder.Id = value;
                }
                catch (AppDomainUnloadedException)
                {
                    CallContext.LogicalSetData(LogicalDataName, new Holder { Id = value });
                }
            }
        }

        /// <summary>
        /// Gets the current correlation id.
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentId()
        {
            Holder holder = CallContext.LogicalGetData(LogicalDataName) as Holder;
            if (holder != null)
            {
                try
                {
                    return holder.Id;
                }
                catch (AppDomainUnloadedException)
                {
                    CallContext.FreeNamedDataSlot(LogicalDataName);
                    return null;
                }
            }

            return null;
        }
    }
}
