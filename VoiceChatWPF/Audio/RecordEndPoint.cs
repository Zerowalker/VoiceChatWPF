using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Windows;
using NAudio.Wave;
using VoiceChatWPF.Network;
using VoiceChatWPF.ViewModels;

namespace VoiceChatWPF.Models
{
    internal class RecordEndPoint : IDisposable
    {
        private const double BytePerMs = 192;
        public readonly WaveFormat Waveformat;
        private readonly Stopwatch _audioSync = new Stopwatch();
        private readonly BlockingCollectionHandler _blockhand;
        private double _audioTimer;
        private WaveInEvent _sendStream;
        private WasapiLoopbackCapture _wasapiLoopback;

        public RecordEndPoint(Socket tcpClient)
        {
            Waveformat = new WaveFormat(48000, 16, 2);
            InitializeSendStream();
            InitializeWasapiLoopCapture();
            _blockhand = new BlockingCollectionHandler(tcpClient);
            _blockhand.BeginProcessing();
        }

        public void Dispose()
        {
            _sendStream.DataAvailable -= SendStream_DataAvailable;
            _sendStream.Dispose();
            _wasapiLoopback.DataAvailable -= WasapiLoopbackOnDataAvailable;
            _wasapiLoopback.Dispose();
            if (_blockhand == null) return;
            _blockhand.BufferCollection.Add(new byte[0]);
            _blockhand.Dispose();
        }

        /// <summary>
        ///     Sends each buffer to the Receiver
        ///     If an Error occurs, stop listening, recording and close ServerClient.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Audio Buffer</param>
        internal void SendStream_DataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                //if (_audioTimer == 0)
                //{
                //    _audioSync.Reset();
                //    _audioSync.Start();
                //}

                //_audioTimer += (e.BytesRecorded/192);
                //long timeStamp = _audioSync.ElapsedMilliseconds;

                //double millisecondsToInsert = timeStamp - _audioTimer;
                ////Console.WriteLine(e.BytesRecorded +"\n"+ e.Buffer.Length);
                //if (millisecondsToInsert >= 10)
                //{
                //    var silence = new byte[(int) (BytePerMs*millisecondsToInsert)];
                //    _blockhand.BufferCollection.Add(e.Buffer);

                //    Console.WriteLine(@"{0} Milliseconds of Silence has been added", silence.Length);
                //    _audioTimer += millisecondsToInsert;
                //}

                _blockhand.BufferCollection.Add(e.Buffer);
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException)
                    return;
                MessageBox.Show(ex.Message, "SendStream_DataAvailable", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Start/Stop (True/False) Recording With SendStream
        /// </summary>
        public void Recording(bool start, bool useWasapi = false)
        {
            if (start && !useWasapi)
                _sendStream.StartRecording();
            else if (!start && !useWasapi)
                _sendStream.StopRecording();
            else if (start)
                _wasapiLoopback.StartRecording();
            else
                _wasapiLoopback.StopRecording();
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
        ///     Convert 32bit Float to 16bit (Stereo)
        /// </summary>
        /// <param name="bytesRecorded">length of Buffer</param>
        /// <param name="buffer">The Buffer containing Audio Data</param>
        /// <returns></returns>
        private static unsafe byte[] Float16Bit(int bytesRecorded, byte[] buffer)
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
            _blockhand.BufferCollection.Add(buffer);
        }

        /// <summary>
        ///     Set Parameters to SendStream
        /// </summary>
        private void InitializeSendStream()
        {
            try
            {
                _sendStream = new WaveInEvent();
                _sendStream.DataAvailable += SendStream_DataAvailable;
                _sendStream.WaveFormat = Waveformat;
                _sendStream.DeviceNumber = VoiceChatViewModel.AudioIndex;
                _sendStream.NumberOfBuffers = 2;
                _sendStream.BufferMilliseconds = VoiceChatViewModel.BufferLength;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "InitilizeAudio", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}