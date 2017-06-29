// ------------------------------------------------------------------------------------------------
// <copyright file="SpeechRecognitionStream.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace FrontEnd.Call
{
    /// <summary>
    /// Stream to allow reading and writing simultaneously from different threads/tasks.
    /// Most of the code for this class is from oxford source code, aka project Truman. 
    /// </summary>
    public class SpeechRecognitionStream : Stream
    {
        /// <summary>
        /// Queue of buffers to read from
        /// </summary>
        private readonly BlockingCollection<byte[]> _readQueue = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());

        /// <summary>
        /// Set to 1 when Stream has been disposed
        /// </summary>
        private int _disposed;

        /// <summary>
        /// The location of the read pointer on the current buff
        /// </summary>
        private int _currentReadBufferLocation;

        /// <summary>
        /// The current read buffer
        /// </summary>
        private byte[] _currentReadBuffer;

        /// <summary>
        /// Numbers of bytes in the readQueue
        /// </summary>
        private uint _pendingBytes;

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite => !AudioEnded;

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking. Always returns false.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Gets a value indicating whether MarkEndOfStream has been called or not
        /// </summary>
        public bool AudioEnded { get; private set; }

        /// <summary>
        /// Sets the length of the current stream. Not supported
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Marks the end of this stream
        /// </summary>
        public void MarkEndOfStream()
        {
            if (AudioEnded)
            {
                return;
            }

            try
            {
                _readQueue.Add(null);
                _readQueue.CompleteAdding();
            }
            catch (InvalidOperationException)
            {
                // The stream was already completed. Harmless. 
            }

            AudioEnded = true;
        }

        /// <summary>
        /// Gets the number of bytes available for reading
        /// </summary>
        /// <returns>Returns the bytes available to read</returns>
        public uint GetBytesAvailable()
        {
            return _pendingBytes;
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes
        /// read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {            
            DisposedCheck();

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentException("Offset cannot be negative", nameof(offset));
            }

            if (count < 0 || buffer.Length < offset + count)
            {
                throw new ArgumentException("Invalid value. Count = " + count + " Offset = " + offset + " Buffer length = " + buffer.Length, nameof(count));
            }

            if (count == 0)
            {
                return 0;
            }

            // If the read queue is complete, just return 0 to indicate EoS
            if (_currentReadBuffer == null)
            {
                if (_readQueue.IsCompleted)
                {
                    return 0;
                }

                // okay readQueue is not complete - let's read the next buffer
                _currentReadBuffer = TakeNextBuffer();
                _currentReadBufferLocation = 0;

                // we don't have any more data in the queue return 0 to indicate EoS
                if (_currentReadBuffer == null)
                {
                    return 0;
                }
            }

            // here the basic principle is if the user requests 1024 bytes
            // and there is say 256 bytes written to in the stream we will loop four times
            // while blocking in each chunk (at the TakeNextBuffer statement) for more bytes to arrive
            int bytesRead = 0;
            while (true)
            {
                // figure out the max # of bytes to read.
                // either it is the remaining bytes from the currentBuffer or the count of bytes requested
                // by the user, whichever is smaller
                int copyLength = Math.Min(_currentReadBuffer.Length - _currentReadBufferLocation, count);

                // copy it to the buffer from the currentReadBuffer
                Array.Copy(_currentReadBuffer, _currentReadBufferLocation, buffer, offset, copyLength);

                // increment the location based on the number of bytes copied
                _currentReadBufferLocation += copyLength;

                // increment offset by the number of bytes read
                offset += copyLength;

                // keep a total count of number of bytes read
                bytesRead += copyLength;

                // subtract from count the number of bytes read
                count -= copyLength;

                if (_currentReadBufferLocation >= _currentReadBuffer.Length)
                {
                    _currentReadBuffer = null;
                    _currentReadBufferLocation = 0;
                }

                // if count is 0 break out - we have satisfied the number of bytes requested by the user
                if (count == 0)
                {
                    break;
                }

                // get the bytes from the readQueue (this could potentially block)
                _currentReadBuffer = TakeNextBuffer();
                _currentReadBufferLocation = 0;                

                // TakeNextBuffer (the readQueue) returns null when we have no more bytes to add (see MarkEndOfStream)
                if (_currentReadBuffer == null)
                {
                    break;
                }
            }

            // add it to the total pendingBytes
            _pendingBytes -= (uint)bytesRead;

            // return the number of bytes read during this read request
            return bytesRead;
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position 
        /// within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            DisposedCheck();
            // If stream has been already closed then we can just discard any incoming buffer
            if (AudioEnded)
            {
                return;
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentException("Offset cannot be negative", nameof(offset));
            }

            if (count < 0 || buffer.Length < offset + count)
            {
                throw new ArgumentException("Invalid value", nameof(count));
            }

            if (count > 0)
            {
                // copy it, we don't want someone to hold on to a reference to the buffer array and modify it
                byte[] localBuffer = new byte[count];
                Buffer.BlockCopy(buffer, offset, localBuffer, 0, count);
                _readQueue.Add(localBuffer);

                _pendingBytes += (uint)localBuffer.Length;
            }
        }

        /// <summary>
        /// When we know that a buffer isn't going to be reused, allow the buffer to be posted directly, saving an extra allocation and copy
        /// </summary>
        /// <param name="buffer">Audio buffer that won't be rewritten over</param>
        public void Post(byte[] buffer)
        {
            DisposedCheck();

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            var len = buffer.Length;
            if (len > 0)
            {
                _readQueue.Add(buffer);
                _pendingBytes += (uint)len;
            }
        }

        /// <summary>
        /// Gets the length in bytes of the stream. Not supported.
        /// </summary>
        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Gets or sets the position within the current stream. Not supported.
        /// </summary>
        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Sets the position within the current stream. 
        /// </summary>
        /// <param name="offset">offset param</param>
        /// <param name="origin">origin param</param>
        /// <returns>long return</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device. Not supported.
        /// </summary>
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Disposes of the stream
        /// </summary>
        /// <param name="disposing">true if invoked from user code</param>
        protected override void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            // Can only perform managed operations if disposing is true
            if (disposing)
            {
                // ps 112121 - Ensure the reader is unblocked first
                MarkEndOfStream();
            }

            // Call the base dispose method
            base.Dispose(disposing);
        }

        /// <summary>
        /// Takes a buffer from the read queue.
        /// </summary>
        /// <returns>The next available buffer or null if the queue is disposed</returns>
        /// <remarks>
        /// The method blocks if a buffer is unavailable.
        /// </remarks>
        private byte[] TakeNextBuffer()
        {
            byte[] buffer = null;

            try
            {
                buffer = _readQueue.Take();
            }
            catch (ObjectDisposedException)
            {
                //// This exception will occur if the above Take occurs just after CompleteAdding 
                //// is called on the queue.
            }
            catch (InvalidOperationException)
            {
                //// This exception will occur if disposal occurs while a reader is blocked on the Take()
            }
            catch (ArgumentNullException)
            {
                //// This exception will occur if disposal occurs while a reader is blocked on the Take()
            }

            return buffer;
        }

        /// <summary>
        /// Check for the object being disposed
        /// </summary>
        private void DisposedCheck()
        {
            if (_disposed == 1)
            {
                throw new ObjectDisposedException("SpeechRecognitionStream");
            }
        }
    }
}