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
    /// Creates an Audio Buffer for Send and also implements Dispose
    /// </summary>
    class AudioSendBuffer : AudioMediaBuffer
    {
        private bool _disposed;

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        public AudioSendBuffer(AudioMediaBuffer mediaBuffer, AudioFormat format, ulong timeStamp)
        {
            IntPtr unmanagedBuffer = Marshal.AllocHGlobal((int)mediaBuffer.Length);
            CopyMemory(unmanagedBuffer, mediaBuffer.Data, (uint)mediaBuffer.Length);

            Data = unmanagedBuffer;
            Length = mediaBuffer.Length;
            AudioFormat = format;
            Timestamp = (long)timeStamp;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {              
                Marshal.FreeHGlobal(Data);
            }

            _disposed = true;
        }
    }
}
