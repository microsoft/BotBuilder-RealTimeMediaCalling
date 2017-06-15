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
        private bool m_disposed;

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
            if (!m_disposed)
            {
                Marshal.FreeHGlobal(Data);
                Data = IntPtr.Zero;
            }

            m_disposed = true;           
        }
    }
}
