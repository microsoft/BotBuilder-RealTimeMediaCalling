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
    /// Creates an Audio Buffer for Send and also implements Dispose
    /// </summary>
    class AudioSendBuffer : AudioMediaBuffer
    {
        private int _disposed;

        public AudioSendBuffer(IntPtr data, long length, AudioFormat audioFormat, long timeStamp)
        {
            Data = data;
            Length = length;
            AudioFormat = audioFormat;
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
