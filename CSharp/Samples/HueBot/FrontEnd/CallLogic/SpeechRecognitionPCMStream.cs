// ------------------------------------------------------------------------------------------------
// <copyright file="SpeechRecognitionPcmStream.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ------------------------------------------------------------------------------------------------

using System.IO;

namespace FrontEnd.Call
{
    /// <summary>
    /// This class creates stream with wav header included. Mainly used for
    /// adding wav header in stream received from speech service. 
    /// </summary>
    public class SpeechRecognitionPcmStream : SpeechRecognitionStream
    {
        private readonly short _compressionCode;
        private readonly short _numberOfChannels;
        private readonly int _sampleRate;
        private readonly int _avgBytesPerSecond;

        public SpeechRecognitionPcmStream(int samplingRate)
            : this(samplingRate, 0)
        {
        }

        public SpeechRecognitionPcmStream(int samplingRate, long length)
        {
            _compressionCode = 0x01;               //PCM
            _numberOfChannels = 0x01;              //No Stereo
            _sampleRate = samplingRate;
            _avgBytesPerSecond = samplingRate * 2;

            InitializeHeader(length);
        }

        private void WriteRiffChunk(BinaryWriter bw)
        {
            bw.Write(0x46464952); //'RIFF'
            bw.Write(50);    //a 0sec wav file is atleast 58 bytes
            bw.Write(0x45564157); //'WAVE'
        }

        private void WriteFmtChunk(BinaryWriter bw)
        {
            bw.Write(0x20746D66);   //'fmt '

            bw.Write(16);         //16 bytes of format. We produce no 'extra format info'

            bw.Write(_compressionCode);            //2bytes
            bw.Write(_numberOfChannels);    //2bytes
            bw.Write(_sampleRate);                 //4bytes
            bw.Write(_avgBytesPerSecond);   //4bytes
            bw.Write((short)2);                               //alignment
            bw.Write((short)16);                       //significant bits per sample
        }

        private void WriteFactChunk(BinaryWriter bw)
        {
            bw.Write(0x74636166);          //'fact' chunk ID
            bw.Write(4);                   //4 byte Fact Chunk size
            bw.Write(0);                   //4 byte chunk data. 
        }

        private void WriteDataChunk(BinaryWriter bw, long length)
        {
            bw.Write(0x61746164);        //'data' chunk ID
            bw.Write((int)length);                          //initially, we have no data, so we set the chunk size to 0
        }

        private void InitializeHeader(long length)
        {
            var bw = new BinaryWriter(this);

            WriteRiffChunk(bw);
            WriteFmtChunk(bw);
            WriteFactChunk(bw);
            WriteDataChunk(bw, length);
        }
    }
}
