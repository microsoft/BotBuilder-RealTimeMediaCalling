# MeetingJoinBot sample

MeetingJoinBot sample shows how to
- subscribe to roster notifications in a conference.
- subscribe to dominant speaker notifications in a conference through the Media Platform.
- subscribe to a participant's video in the conference and get access to her/his "raw/unencoded" video.
- initiate a join call to join an existing conversation


## Test the bot
Create a conference/conversation group and add `MeetingJoinBot` bot to the conference. Start an audio-video call on the conference and send a chat message to the `MeetingJoinBot` bot by mentioning the bot with an @. For example, send `@MeetingJoinBot hi`. The bot will reply back with a `Click here for video shots from the conference` message that has a link to the video shots from the conference. The link will show periodic video shots captured from the dominant speaker in the conference. The video shots are sent directly by the bot and are not stored anywhere. The link is available only as long as the call is active.  

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
