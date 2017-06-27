using Microsoft.Skype.Bots.Media;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    public interface IRealTimeMediaSession
    {
        string Id { get; }

        string CorrelationId { get; }

        IAudioSocket AudioSocket { get; }

        IReadOnlyList<IVideoSocket> VideoSockets { get; }
        
        IVideoSocket VbssSocket { get; }

        IAudioSocket AddAudioSocket(AudioSocketSettings settings);

        IVideoSocket AddVideoSocket(VideoSocketSettings settings);

        IVideoSocket AddVbssSocket(VideoSocketSettings settings);

        JObject MediaConfiguration { get; }
    }
}
