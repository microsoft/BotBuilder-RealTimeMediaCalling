using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FrontEnd.Logging;
using FrontEnd.Media;
using Microsoft.Skype.Bots.Media;
using Microsoft.Skype.Internal.Bots.Media;

namespace FrontEnd
{

    internal static class Utilities
    {
        /// <summary>
        /// Extension for Task to execute the task in background and log any exception
        /// </summary>
        /// <param name="task"></param>
        /// <param name="description"></param>
        public static async void ForgetAndLogException(this Task task, string description = null)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                //ignore
                Log.Error(new CallerInfo(),
                    LogContext.FrontEnd,
                    "Caught an Exception running the task: {0} \n StackTrace: {1}", e.Message, e.StackTrace);
            }
        }

        public static List<VideoMediaBuffer> CreateVideoMediaBuffers(long currentTick)
        {
            List<VideoMediaBuffer> videoMediaBuffers = new List<VideoMediaBuffer>();
            int frameSize = GetFrameSize(VideoFormat.NV12_1280x720_30Fps);
            var referenceTime = currentTick;
            var packetSizeInMs = (long)((1000.0 / (double)VideoFormat.NV12_1280x720_30Fps.FrameRate) * 10000.0);

            using (FileStream fs = File.Open(Service.Instance.Configuration.VideoFileLocation, FileMode.Open))
            {
                byte[] bytesToRead = new byte[frameSize];

                while (fs.Read(bytesToRead, 0, bytesToRead.Length) >= frameSize)
                {
                    IntPtr unmanagedBuffer = Marshal.AllocHGlobal(frameSize);
                    Marshal.Copy(bytesToRead, 0, unmanagedBuffer, frameSize);
                    referenceTime += packetSizeInMs;
                    var videoSendBuffer = new VideoSendBuffer(unmanagedBuffer, (uint)frameSize,
                        VideoFormat.NV12_1280x720_30Fps, referenceTime);
                    videoMediaBuffers.Add(videoSendBuffer);
                }
            }

            Log.Info(
               new CallerInfo(),
               LogContext.Media,
               "created {0} VideoMediaBuffers", videoMediaBuffers.Count);
            return videoMediaBuffers;
        }

        public static List<AudioMediaBuffer> CreateAudioMediaBuffers(long currentTick)
        {
            var audioMediaBuffers = new List<AudioMediaBuffer>();
            var referenceTime = currentTick;

            using (FileStream fs = File.Open(Service.Instance.Configuration.AudioFileLocation, FileMode.Open))
            {
                byte[] bytesToRead = new byte[640];
                fs.Seek(44, SeekOrigin.Begin);
                while (fs.Read(bytesToRead, 0, bytesToRead.Length) >= 640) //20ms
                {
                    IntPtr unmanagedBuffer = Marshal.AllocHGlobal(640);
                    Marshal.Copy(bytesToRead, 0, unmanagedBuffer, 640);
                    referenceTime += 20 * 10000;
                    var audioBuffer = new AudioSendBuffer(unmanagedBuffer, 640, AudioFormat.Pcm16K,
                        referenceTime);
                    audioMediaBuffers.Add(audioBuffer);
                }
            }

            Log.Info(
                new CallerInfo(),
                LogContext.Media,
                "created {0} AudioMediaBuffers", audioMediaBuffers.Count);
            return audioMediaBuffers;
        }

        private static int GetFrameSize(VideoFormat videoFormat)
        {
            return (int)(videoFormat.Width * videoFormat.Height * Helper.GetBitsPerPixel(videoFormat.VideoColorFormat) / 8);
        }
    }
}
