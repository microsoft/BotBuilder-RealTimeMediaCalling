/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System;
using System.Runtime.InteropServices;
using FrontEnd.Logging;
using Microsoft.Skype.Bots.Media;

namespace FrontEnd.Media
{
    /// <summary>
    /// Creates a Video Buffer for Send and also implements Dispose
    /// </summary>
    class VideoSendBuffer : VideoMediaBuffer
    {
        private bool _disposed;

        public VideoSendBuffer(byte[] buffer, uint length, VideoFormat format)
        {
            IntPtr ptrToBuffer = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, ptrToBuffer, buffer.Length);

            Data = ptrToBuffer;
            Length = length;
            VideoFormat = format;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                Marshal.FreeHGlobal(Data);
                Data = IntPtr.Zero;
            }

            _disposed = true;
        }
    }
}
