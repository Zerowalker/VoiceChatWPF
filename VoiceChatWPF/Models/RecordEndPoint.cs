using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Windows;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace VoiceChatWPF.Models
{
    internal class RecordEndPoint : IDisposable
    {
        private const double BytePerMs = 192;
        private double _audioTimer;
        private readonly Stopwatch _audioSync = new Stopwatch();
        public readonly WaveFormat Waveformat;
        private readonly BlockingCollectionHandler _blockhand;
        public WaveInEvent SendStream;
        private readonly Socket _tcpClient;

        public RecordEndPoint(Socket tcpClient)
        {
            _tcpClient = tcpClient;
            Waveformat = new WaveFormat(48000, 16, 2);
            InitilizeSendStream();
            _blockhand = new BlockingCollectionHandler(tcpClient);
            _blockhand.BeginProcessing();
        }

        public void Dispose()
        {
            SendStream.DataAvailable -= SendStream_DataAvailable;
            SendStream.Dispose();
            if (_blockhand != null)
            {
                try
                {
                    _blockhand.BufferCollection.Add(new byte[0]);
                    _blockhand.Dispose();
                }
                catch (Exception)
                {
                }
            }

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
        /// Start Recording With SendStream
        /// </summary>
        public void StartRecording()
        {
                   SendStream.StartRecording();
        }
        /// <summary>
        ///     Set Parameters to SendStream and starts Playing wasapiOut
        /// </summary>
        private void InitilizeSendStream()
        {
            try
            {
                SendStream = new WaveInEvent();
                SendStream.DataAvailable += SendStream_DataAvailable;
                SendStream.WaveFormat = Waveformat;
                SendStream.DeviceNumber = VoiceChatViewModel.AudioIndex;
                SendStream.NumberOfBuffers = 2;
                SendStream.BufferMilliseconds = VoiceChatViewModel.BufferLength;
         
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "InitilizeAudio", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}