/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Skype.Bots.Media;

namespace FrontEnd.Media
{
    /// <summary>
    /// Creates a Video Buffer for Send and also implements Dispose
    /// </summary>
    class VideoSendBuffer : VideoMediaBuffer
    {
        private int _disposed;

        public VideoSendBuffer(IntPtr data, long length, VideoFormat videoformat, long timeStamp)
        {
            Data = data;
            Length = length;
            VideoFormat = videoformat;
            Timestamp = timeStamp;
        }

        protected override void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
            {
                Marshal.FreeHGlobal(Data);
                Data = IntPtr.Zero;
            }
        }
    }
}
