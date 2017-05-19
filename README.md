# Real-time media calling with Skype

The Real-Time Media Platform for Bots adds a new dimension to how bots can interact with users by enabling real-time voice, video and screen sharing modalities. This provides the capability to build compelling and interactive entertainment, educational, and assistance bots. Users communicate with real-time media bots using Skype.

This is an advanced capability which allows the bot to send and receive voice and video content *frame by frame*. The bot has "raw" access to the voice, video and screen-sharing real-time modalities. For example, as the user speaks, the bot will receive 50 audio frames per second, with each frame containing 20 milliseconds (ms) of audio. The bot can perform real-time speech recognition as the audio frames are received, rather than having to wait for a recording after the user has stopped speaking. The bot can also send high-definition-resolution video to the user at 30 frames per second, along with audio.

The Real-Time Media Platform for Bots works with the Skype Calling Cloud to take care of call setup and media session establishment, enabling the bot to engage in a voice and (optionally) video conversation with a Skype caller. The platform provides a simple "socket"-like API for the bot to send and receive media, and handles the real-time encoding and decoding of media, using codecs such as SILK for audio and H.264 for video. A real-time media bot may also participate in Skype group video calls with multiple participants.

## Developer resources 

### SDKs

To develop a real-time media bot, you must install these NuGet packages within your Visual Studio project:

- [Bot Builder Real-Time Media Calling for .NET](https://www.nuget.org/packages?q=Bot.Builder.RealTimeMediaCalling)
- [Microsoft.Skype.Bots.Media .NET library](https://www.nuget.org/packages?q=Microsoft.Skype.Bots.Media)

### Get started


- **[Review the documentation](https://docs.microsoft.com/en-us/bot-framework/dotnet/bot-builder-dotnet-real-time-media-concepts)** to understand the concepts behind building real-time media bots.
- Check out the **[requirements](https://docs.microsoft.com/en-us/bot-framework/dotnet/bot-builder-dotnet-real-time-media-requirements)** for real-time media bots.
- Get started quickly with our **[samples](https://github.com/Microsoft/BotBuilder-RealTimeMediaCalling/tree/master/Samples)**.

## Contributing

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
