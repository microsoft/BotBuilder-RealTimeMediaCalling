# AudioVideoPlayerBot sample

## Description

AudioVideoPlayerBot is a sample consuming the AudioVideoFramePlayer class.

The AudioVideoFramePlayer class can be used, if the app can provide a little ahead the list of buffers to stream.
This class will handle audio and video synchronization from the timestamps in the audio/video buffers.

## Example:

Once the audio/video sockets are created, and the send status event is raised on both sockets,
you can create the AudioVideoFramePlayer and attach it to the sockets:

// here we are setting the audio buffer size to 20ms and 1sec of minimum media content,
// once the enqueued video or audio length is under a second, the player will raise a LowOnFrameEvent
_audioVideoFramePlayer = new AudioVideoFramePlayer(_audioSocket, _videoSocket,
                    new AudioVideoFramePlayerSettings(new AudioSettings(20), new VideoSettings(), 1000));
					
// we subscribe to the LowOnFrameEvent
 _audioVideoFramePlayer.LowOnFrames += OnLowInFrames;

// This will enqueue the audio and video buffers. The player will then stream audio and video in the background.
await _audioVideoFramePlayer.EnqueueBuffersAsync(_audioMediaBuffers, _videoMediaBuffers);


// un-subscribe to the LowOnFrameEvent
 _audioVideoFramePlayer.LowOnFrames -= OnLowInFrames;

//shutdown the player to release all resources and free remaining enqueued buffers
await _audioVideoFramePlayer.ShutdownAsync();

## Deploy the bot sample
Prerequisites and instructions for deploying are [here](https://docs.microsoft.com/en-us/bot-framework/dotnet/bot-builder-dotnet-real-time-deploy-visual-studio). Before the sample can be deployed, its configuration needs to be updated.

### Update Config
-	In app.config of the WorkerRole, replace $BotHandle$, $MicrosoftAppId$ and $BotSecret$ with values obtained during bot registration.
-   Modify the FileLocation values to point to the wav file for audio and YUV file for video

```xml
<appSettings>
    <!-- update these with your BotId, Microsoft App Id and your Microsoft App Password-->
    <add key="BotId" value="$BotHandle$"/>
    <add key="MicrosoftAppId" value="$MicrosoftAppId$"/>
    <add key="MicrosoftAppPassword" value="$BotSecret$"/>
    <add key="VideoFileLocation" value="FileLocation"/>
    <add key="AudioFileLocation" value="FileLocation"/>
 </appSettings>
```
-	Substitute the $xxx$ in service configuration (ServiceConfiguration.Cloud.cscfg file) with appropriate values in the config.
```xml
<Setting name="DefaultCertificate" value="$CertificateThumbprint$" />
```

## How it works
Instructions on how to build a bot can be found [here](https://docs.microsoft.com/en-us/bot-framework/dotnet/bot-builder-dotnet-real-time-audio-video-call-overview).

As mentioned [here](https://docs.microsoft.com/en-us/bot-framework/dotnet/bot-builder-dotnet-real-time-media-requirements), real-time media bots are different from chat bots as the real-time media bots are very stateful. The real-time media call is pinned to the virtual machine (VM) instance which accepted the incoming call and hence the subsequent requests for that call are sent to that VM using an `InstanceInputEnpoint`. A chat on the other hand could be handled by any instance in the deployment using constructs from the bot framework. So to integrate chat with the corresponding audio-video call, say make the audio-video call change state on receiving a chat message, the bot developer should route the chat message for that call to the appropriate instance handling the audio-video modality for the same call.

The Conversation.Id from the incoming Activity when a chat is received and the IncomingCall.ThreadId of RealTimeMediaIncomingCallEvent when an audio-video call is received could be used to link the chat with the corresponding audio-video call.

```cs
[HttpPost]
[Route(HttpRouteConstants.OnIncomingMessageRoute)]
public virtual async Task<HttpResponseMessage> Post([FromBody] Activity activity)
{
    ThreadId = activity.Conversation.Id; //use to link to the audio-video call
    return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
 }

internal class RealTimeMediaCall : IRealTimeMediaCall
{
  private Task OnIncomingCallReceived(RealTimeMediaIncomingCallEvent incomingCallEvent)
  {
      ThreadId = incomingCallEvent.IncomingCall.ThreadId; //use this to link to chat thread
      ...
  }
}
```