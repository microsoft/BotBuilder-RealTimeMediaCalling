using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    /// <summary>
    /// An interface for a real time media bot.
    /// </summary>
    public interface IRealTimeMediaBot
    {
        /// <summary>
        /// The service for real time media bots.
        /// </summary>
        IRealTimeMediaBotService RealTimeMediaBotService { get; }
    }
}
