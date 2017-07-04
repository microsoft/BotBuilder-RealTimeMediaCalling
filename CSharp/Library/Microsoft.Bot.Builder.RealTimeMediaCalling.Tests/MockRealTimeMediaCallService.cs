using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Microsoft.Skype.Bots.Media;
using Moq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.Tests
{
    internal class MockRealTimeMediaCallService : RealTimeMediaCallService
    {
        public MockRealTimeMediaCallService(RealTimeMediaCallServiceParameters parameters, IRealTimeMediaCallServiceSettings settings) 
            : base(parameters, settings)
        {
        }

        public override IRealTimeMediaSession CreateMediaSession(params NotificationType[] subscriptions)
        {
            var session = new Mock<IRealTimeMediaSession>();
            JObject mediaConfiguration;
            using (var writer = new JTokenWriter())
            {
                writer.WriteRaw("MediaConfiguration");
                mediaConfiguration = new JObject { { "Token", writer.Token } };
            }
            session.Setup(a => a.GetMediaConfiguration()).Returns(mediaConfiguration);

            var audioSocket = new Mock<IAudioSocket>();
            session.Setup(a => a.SetAudioSocket(It.IsAny<AudioSocketSettings>())).Returns(audioSocket.Object);
            session.Setup(a => a.AudioSocket).Returns(audioSocket.Object);

            var videoSocket = new Mock<IVideoSocket>();
            session.Setup(a => a.SetVideoSocket(It.IsAny<VideoSocketSettings>())).Returns(videoSocket.Object);
            session.Setup(a => a.VideoSocket).Returns(videoSocket.Object);

            return session.Object;
        }
    }

}
