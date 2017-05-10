using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using FrontEnd.Http;
using FrontEnd.Logging;

namespace FrontEnd.CallLogic
{
    public static class VideoImageViewer
    {
        /// <summary>
        /// Get the image for a particular call
        /// </summary>
        /// <param name="conversationResult"></param>
        /// <returns></returns>
        public static HttpResponseMessage GetVideoImageResponse(string callId)
        {
            RealTimeMediaCall mediaCall = RealTimeMediaCall.GetCallForCallId(callId);
            if (mediaCall == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            MediaSession mediaSession = mediaCall.MediaSession;

            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{callId}] Received request to retrieve image for call");
            Bitmap bitmap = mediaSession.CurrentVideoImage;
            if (bitmap == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new PushStreamContent((targetStream, httpContext, transportContext) =>
            {
                using (targetStream)
                {
                    bitmap.Save(targetStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
            }, new MediaTypeHeaderValue("image/jpeg"));

            response.Headers.Add("Cache-Control", "private,must-revalidate,post-check=1,pre-check=2,no-cache");

            string url = $"{Service.Instance.Configuration.AzureInstanceBaseUrl}/{HttpRouteConstants.CallSignalingRoutePrefix}/{HttpRouteConstants.Image}";
            url = url.Replace("{callid}", callId);
            response.Headers.Add("Refresh", $"3; url={url}");

            return response;
        }
    }
}
