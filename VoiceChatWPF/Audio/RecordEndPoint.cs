using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using NAudio.Wave;
using VoiceChatWPF.Network;
using VoiceChatWPF.ViewModels;

namespace VoiceChatWPF.Models
{
    internal class RecordEndPoint : IDisposable
    {
        private const double BytePerMs = 192;
        private readonly WaveFormat _waveformat;
        public static readonly Stopwatch _audioSync = new Stopwatch();
        private readonly BlockingCollectionHandler _blockhand;
        private double _audioTimer;
        private WaveInEvent _sendStream;
        private WasapiLoopbackCapture _wasapiLoopback;
        private  int _deviceNumber;
        private  int _bufferLength = 10;
        private int _check;
        public RecordEndPoint(Socket tcpClient)
        {
            _waveformat = new WaveFormat(48000, 16, 2);
            InitializeSendStream();
            InitializeWasapiLoopCapture();
            _blockhand = new BlockingCollectionHandler(tcpClient);
            _blockhand.BeginProcessing();
        }
        /// <summary>
        /// Resets the Recording to update the Device/Buffer change
        /// </summary>
        /// <param name="device"></param>
        /// <param name="bufferLength"></param>
        public void Reinitialize( int device = 0, int bufferLength = 10)
        {
            if (_check == 1)
                Recording(false);
            else _check = 1;
            _bufferLength = bufferLength;
            _deviceNumber = device;
            InitializeSendStream();
            Recording(true);
        }
        public void Dispose()
        {
            _sendStream.DataAvailable -= SendStream_DataAvailable;
            _sendStream.Dispose();
            _wasapiLoopback.DataAvailable -= WasapiLoopbackOnDataAvailable;
            _wasapiLoopback.Dispose();
            if (_blockhand == null) return;
            _blockhand.Dispose();
            _audioSync.Stop();
        }

        /// <summary>
        /// Adds Silence if Audio Duration is inconsistent with StopWatch
        /// </summary>
        /// <param name="bytesRecorded"></param>
        private void SyncToStopWatch(int bytesRecorded)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_audioTimer == 0)
            {
                _audioSync.Reset();
                _audioSync.Start();
            }

            _audioTimer += (bytesRecorded / 192);
            long timeStamp = _audioSync.ElapsedMilliseconds;

            double millisecondsToInsert = timeStamp - _audioTimer;

            if (millisecondsToInsert >= 20)
            {
                var silence = new byte[(int)(BytePerMs * millisecondsToInsert)];
                _blockhand.BufferCollection.Add(silence);

                Console.WriteLine(@"{0} Milliseconds of Silence has been added", silence.Length);
                _audioTimer += millisecondsToInsert;
            }
        }

        /// <summary>
        ///     Sends each buffer to the Receiver
        ///     If an Error occurs, stop listening, recording and close ServerClient.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Audio Buffer</param>
        private void SendStream_DataAvailable(object sender, WaveInEventArgs e)
        {
            SyncToStopWatch(e.BytesRecorded);
            _blockhand.BufferCollection.Add(e.Buffer);
        }

        /// <summary>
        ///     Start/Stop (True/False) Recording With SendStream
        /// </summary>
        public void Recording(bool start, bool useWasapi = false)
        {
            if (_deviceNumber == -1)
                useWasapi = true;

            if (start && !useWasapi)
            {
                _sendStream.StartRecording();
            }
            else if (!start && !useWasapi)
            {
                _sendStream.StopRecording();
            }

            if (start && useWasapi)
            {
                _wasapiLoopback.StartRecording();
            }
            else if (!start)
            {
                _wasapiLoopback.StopRecording();
            }
        }



        /// <summary>
        ///     Convert 32bit Float to 16bit (Stereo)
        /// </summary>
        /// <param name="bytesRecorded">length of Buffer</param>
        /// <param name="buffer">The Buffer containing Audio Data</param>
        /// <returns></returns>
        private unsafe byte[] Float16Bit(int bytesRecorded, byte[] buffer)
        {
            var newArray16Bit = new byte[bytesRecorded/2];
            fixed (byte* sourcePtr = buffer)
            fixed (byte* targetPtr = newArray16Bit)
            {
                var sourceTyped = (float*) sourcePtr;
                var targetTyped = (short*) targetPtr;

                int count = bytesRecorded/4;
                for (int i = 0; i < count; i++)
                {
                    targetTyped[i] = (short) (sourceTyped[i]*short.MaxValue);
                }
            }
            return newArray16Bit;
        }

        private void WasapiLoopbackOnDataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] buffer = Float16Bit(e.BytesRecorded, e.Buffer);
            SyncToStopWatch(buffer.Length);
            _blockhand.BufferCollection.Add(buffer);
        }

        /// <summary>
        ///     Set Parameters to WasapiLoopCapture
        /// </summary>
        private void InitializeWasapiLoopCapture()
        {
            _wasapiLoopback = new WasapiLoopbackCapture();
            _wasapiLoopback.DataAvailable += WasapiLoopbackOnDataAvailable;
        }

        /// <summary>
        ///     Set Parameters to SendStream
        /// </summary>
        private void InitializeSendStream()
        {
            if (_sendStream != null) _sendStream.Dispose();
            _sendStream = new WaveInEvent();
                _sendStream.DataAvailable += SendStream_DataAvailable;
                _sendStream.WaveFormat = _waveformat;
                _sendStream.DeviceNumber = _deviceNumber;
                _sendStream.NumberOfBuffers = 1;
                _sendStream.BufferMilliseconds = _bufferLength;

        }
    }
}