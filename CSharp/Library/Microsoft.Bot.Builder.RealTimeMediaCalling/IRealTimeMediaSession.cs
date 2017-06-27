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

        IRealTimeAudioSocket AudioSocket { get; }

        IReadOnlyList<IRealTimeVideoSocket> VideoSockets { get; }
        
        IRealTimeVideoSocket VbssSocket { get; }

        IRealTimeAudioSocket AddAudioSocket(AudioSocketSettings settings);

        IRealTimeVideoSocket AddVideoSocket(VideoSocketSettings settings);

        IRealTimeVideoSocket AddVbssSocket(VideoSocketSettings settings);

        JObject MediaConfiguration { get; }
    }

    internal interface IInternalRealTimeMediaSession : IRealTimeMediaSession
    {
        new string CorrelationId { get; set; }
    }

    /// <summary>
    /// Interface wrapper for IAudioSocket
    /// </summary>
    public interface IRealTimeAudioSocket
    {
        /// <summary>
        /// If the application has configured the AudioSocket to receive media, this
        /// event is raised each time a packet of audio media is received.
        /// Once the application has consumed the buffer, it must call the buffer's
        /// Dispose() method.
        /// The application must be able to handle at least 50 incoming audio buffers
        /// per second.
        /// Events are serialized, so only one event at a time is raised to the app.
        /// </summary>
        event EventHandler<AudioMediaReceivedEventArgs> AudioMediaReceived;

        /// <summary>
        /// If the application has configured the AudioSocket to send media, this
        /// event is raised to inform the application when it may begin sending
        /// media and when it should stop. The application cannot send media before
        /// receiving a MediaSendStatusChanged event indicating the SendStatus is
        /// Started.
        /// </summary>
        event EventHandler<AudioSendStatusChangedEventArgs> AudioSendStatusChanged;

        /// <summary>
        /// This event is raised when there is a change in the dominant speaker in the conference.
        /// If there is no dominant speaker in the conference, the CurrentDominantSpeaker argument in the event will have the value None (0xFFFFFFFF).
        /// </summary>
        event EventHandler<DominantSpeakerChangedEventArgs> DominantSpeakerChanged;

        /// <summary>
        /// This event is raised when the DTMF tone is received. ToneId enum in the event arguments indicates the tone value.
        /// </summary>
        event EventHandler<ToneReceivedEventArgs> ToneReceived;

        /// <summary>
        /// Allows the application to send a packet of audio media if the application
        /// has configured the AudioSocket to send media.
        /// The application should be sending about 50 packets of audio media per
        /// second; each buffer containing 20 milliseconds worth of audio content.
        /// The application must create a concrete class which derives from the
        /// AudioMediaBuffer abstract class. The buffer object passed to the Send
        /// method is still potentially in-use after the method returns to the
        /// caller. The application must not free the buffer's data until the
        /// the buffer object's Dispose() method is invoked by the Media Platform.
        /// </summary>
        /// <param name="buffer">AudioMediaBuffer to send.</param>
        void Send(AudioMediaBuffer buffer);
    }

    /// <summary>
    /// Interface wrapper for IVideoSocket
    /// </summary>
    public interface IRealTimeVideoSocket
    {
        /// <summary>
        /// The 0-based ID of the socket. This socket ID is useful to identify a socket in a
        /// multiview (ie. more than 1 video socket) call. The same ID is used in the event
        /// args of the VideoMediaReceived and VideoSendStatusChanged events that this class
        ///  may raise. The socket ID property will be present in both single view and multiview
        /// cases. The ID maps to the order in which the video sockets are provided to the
        /// CreateMediaConfiguration API.
        /// Eg., if the collection of IVideoSocket objects in the CreateMediaConfiguration API contains
        /// {socketA, socketB, socketC}, the sockets will have the ID mapping of: 0 for socketA,
        /// 1 for socketB and 2 for socketC.
        /// Before the call to CreateMediaConfiguration, the SocketId has a value of -1.
        /// </summary>
        int SocketId { get; }

        /// <summary>
        /// If the application has configured the VideoSocket to receive media, this
        /// event is raised each time a packet of video media is received.
        /// Once the application has consumed the buffer, it must call the buffer's
        /// Dispose() method.
        /// The application should be prepared to handle approximately 30 incoming
        /// video buffers per second.
        /// Events are serialized, so only one event at a time is raised to the app.
        /// </summary>
        event EventHandler<VideoMediaReceivedEventArgs> VideoMediaReceived;

        /// <summary>
        /// If the application has configured the VideoSocket to send media, this
        /// event is raised to inform the application when it may begin sending
        /// media and when it should stop. The application cannot send media before
        /// receiving a VideoMediaSendStatusChanged event indicating the SendStatus is
        /// Active, such media will be discarded.
        /// </summary>
        event EventHandler<VideoSendStatusChangedEventArgs> VideoSendStatusChanged;

        /// <summary>
        /// Allows the application to send a packet of video media if the application
        /// has configured the VideoSocket to send media.
        /// The application should be sending about 30 video frame buffers/second.
        /// The application must create a concrete class which derives from the
        /// VideoMediaBuffer abstract class. The buffer object passed to the Send
        /// method is still potentially in-use after the method returns to the
        /// caller. The application must not free the buffer's data until the
        /// the buffer object's Dispose() method is invoked by the Media Platform.
        /// </summary>
        /// <param name="buffer">VideoMediaBuffer to send.</param>
        void Send(VideoMediaBuffer buffer);
    }
}
