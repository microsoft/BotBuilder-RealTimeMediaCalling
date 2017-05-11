# MeetingScreenShots sample

MeetingScreenShots sample shows how to
- integrate chat modality with audio-video modality.
- subscribe to roster notifications in a conference.
- subscribe to dominant speaker notifications in a conference through the Media Platform.
- subscribe to a participant's video in the conference and get access to her/his "raw/unencoded" video.

## Description
When MeetingScreenShots bot is added to a conference, it captures the video frame of the dominant speaker at periodic intervals and sends the link to query this frame to the conference. Participants can click on this link and enjoy looking at the expressions of the dominant speaker at different moments of the conference.

## Test the bot
Create a conference/conversation group and add `MeetingScreenShots` bot to the conference. Start an audio-video call on the conference and send a chat message to the `MeetingScreenShots` bot by mentioning the bot with an @. For example, send `@MeetingScreenShots hi`. The bot will reply back with a `Click here for video shots from the conference` message that has a link to the video shots from the conference. The link will show periodic video shots captured from the dominant speaker in the conference. The video shots are sent directly by the bot and are not stored anywhere. The link is available only as long as the call is active.  

## Deploy the bot sample
Prerequisites and instructions for deploying are [here](https://docs.microsoft.com/en-us/bot-framework/dotnet/bot-builder-dotnet-real-time-deploy-visual-studio). Before the sample can be deployed, its configuration needs to be updated.

### Update Config
-	In app.config of the WorkerRole, replace $BotHandle$, $MicrosoftAppId$ and $BotSecret$ with values obtained during bot registration.

```xml
<appSettings>
    <!-- update these with your BotId, Microsoft App Id and your Microsoft App Password from your bot registration portal-->
    <add key="BotId" value="$BotHandle$" />
    <add key="MicrosoftAppId" value="$MicrosoftAppId$" />
    <add key="MicrosoftAppPassword" value="$BotSecret$" />
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

## Things to know
- A conversation group/conversation must be created with the bot before starting the audio-video call. Escalating to a conference from a 1:1 call with the bot is *not* supported.
- This sample can be run only from a single instance deployment. It shows how to link the chat thread and audio-video call, but it does not demonstrate routing of chat messages to the instance that handles audio-video call.
