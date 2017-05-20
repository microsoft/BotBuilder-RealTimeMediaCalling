# AudioVideoPlayerBot sample

## Description

AudioVideoPlayerBot is a sample consuming the AudioVideoFramePlayer class.

The AudioVideoFramePlayer class can be used, if the app can provide a little ahead the list of buffers to stream.
This class will handle audio and video synchronization from the timestamps in the audio/video buffers.

## Test the bot

Once deployed, start an audio/video call with the bot, which will beging streaming audio/video from the file locations 
specified in the configuration.

## Deploy the bot sample
Prerequisites and instructions for deploying are [here](https://docs.microsoft.com/en-us/bot-framework/dotnet/bot-builder-dotnet-real-time-deploy-visual-studio). Update the configuration before deploying the sample per the instructions above.

## Update Config

-	In app.config of the WorkerRole, replace $BotHandle$, $MicrosoftAppId$ and $BotSecret$ with values obtained during bot registration.

```xml
<appSettings>
    <!-- update these with your BotId, Microsoft App Id and your Microsoft App Password-->
    <add key="BotId" value="$BotHandle$"/>
    <add key="MicrosoftAppId" value="$MicrosoftAppId$"/>
    <add key="MicrosoftAppPassword" value="$BotSecret$"/>
 </appSettings>
```

-	Substitute the $xxx$ in service configuration (ServiceConfiguration.Cloud.cscfg file) with appropriate values in the config.
```xml
<Setting name="DefaultCertificate" value="$CertificateThumbprint$" />
```

-   Modify the FileLocation values to point to the wav file for audio and YUV file for video
```xml
<Setting name="VideoFileLocation" value="$videoFilePath$" />
<Setting name="AudioFileLocation" value="$audioFilePath$" />
```

## How it works:

Once the audio/video sockets are created, and the send status event is raised on both sockets,
you can create the AudioVideoFramePlayer and attach it to the sockets:

```cs
// here we are setting the audio buffer size to 20ms and 1sec of minimum media content,
// once the enqueued video or audio length is under a second, the player will raise a LowOnFrameEvent
_audioVideoFramePlayer = new AudioVideoFramePlayer(_audioSocket, _videoSocket,
                    new AudioVideoFramePlayerSettings(new AudioSettings(20), new VideoSettings(), 1000));
					
// we subscribe to the LowOnFrameEvent
 _audioVideoFramePlayer.LowOnFrames += OnLowOnFrames;

// This will enqueue the audio and video buffers. The player will then stream audio and video in the background.
await _audioVideoFramePlayer.EnqueueBuffersAsync(_audioMediaBuffers, _videoMediaBuffers);


// un-subscribe to the LowOnFrameEvent
 _audioVideoFramePlayer.LowOnFrames -= OnLowOnFrames;

//shutdown the player to release all resources and free remaining enqueued buffers
await _audioVideoFramePlayer.ShutdownAsync();
```