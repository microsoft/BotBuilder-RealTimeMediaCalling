# Huebot sample 

Huebot sample shows how to
- integrate Bing Speech recognition service with Skype calls.
- loopback user's audio and video input.

## Description

Huebot listens for user’s audio and sends it to the recognition service for speech recognition. When it recognizes the colors – red, blue or green, it changes the hue of the user’s video to that color and loops it back to the user. The audio is loop backed too.
To try the sample, access token to Speech API is needed. You could sign up for a free trial [here](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/).

## Test the bot
Start an audio-video call to the bot. Your video should appear with a blue tint. Say colors – red/blue/green and the video should appear with the corresponding hue. You should also hear the bot echoing your audio.

## Deploy the bot sample
Prerequisites and instructions for deploying are [here](https://docs.microsoft.com/en-us/bot-framework/dotnet/bot-builder-dotnet-real-time-deploy-visual-studio). Update the configuration before deploying the sample per the instructions above.

### Update config

-	In app.config of the WorkerRole in the sample, replace $BotHandle$, $MicrosoftAppId$ and $BotSecret$ with values obtained during bot registration.

```xml
<appSettings>
    <!-- update these with your BotId, Microsoft App Id and your Microsoft App Password from your bot registration portal-->
    <add key="BotId" value="$BotHandle$" />
    <add key="MicrosoftAppId" value="$MicrosoftAppId$" />
    <add key="MicrosoftAppPassword" value="$BotSecret$" />
 </appSettings>
```
-	Substitute the $xxx$ in configuration with appropriate values in the config.
```xml
<Setting name="DefaultCertificate" value="$CertificateThumbprint$" />
```

## How it works
Instructions on how to build a bot can be found [here](https://docs.microsoft.com/en-us/bot-framework/dotnet/bot-builder-dotnet-real-time-audio-video-call-overview).

To recognize the speech, `MediaSession` in the sample creates a `SpeechClient` from `Microsoft.Bing.Speech` library and starts recognition.

```cs
using (_speechClient = new SpeechClient(preferences))
{
   _speechClient.SubscribeToRecognitionResult(this.OnRecognitionResult);
   await _speechClient.RecognizeAsync(new SpeechInput(_recognitionStream, requestMetadata), _recognitionCts.Token);
}
```

When the recognition completes – success or failure (timeouts like InitialSilenceTimeout/PhraseTimeout are considered failure), the `SpeechClient` is recreated and recognition is started again. This continues till the call is active. For efficiency and optimal use of resources, a voice activity detector could be used and the speech recognition could be started only when there is a voice activity.
