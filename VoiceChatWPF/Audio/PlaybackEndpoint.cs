using System;
using System.Net.Sockets;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace VoiceChatWPF.Models
{
    public sealed class PlaybackEndpoint : IDisposable
    {
        private const int SampleSize = 960; //5ms 16bit 48khz Stereo
        private readonly BufferedWaveProvider _waveProvider = new BufferedWaveProvider(new WaveFormat(48000, 16, 2));
        private AsioOut _asioOut;
        private WasapiOut _wasapiOut;

        public PlaybackEndpoint()
        {
            InitilizeAudio();
        }

        public void Dispose()
        {
            if (_wasapiOut != null) _wasapiOut.Dispose();
            if (_asioOut != null) _asioOut.Dispose();
        }

        /// <summary>
        ///     Set Parameters to SendStream and starts Playing wasapiOut
        /// </summary>
        private void InitilizeAudio()
        {
            try
            {
                _waveProvider.DiscardOnBufferOverflow = true;
                _waveProvider.BufferDuration = new TimeSpan(0, 0, 0, 0, 120);
                //if (AsioOut.isSupported())
                //{
                //    if (_asioOut != null) _asioOut.Dispose();
                //    _asioOut = new AsioOut(1);
                //    _asioOut.Init(_waveProvider);
                //    _asioOut.Play();

                //}
                //else
                {
                    if (_wasapiOut != null) _wasapiOut.Dispose();
                    _wasapiOut = new WasapiOut(AudioClientShareMode.Shared, true, 10);
                    _wasapiOut.Init(_waveProvider);
                    _wasapiOut.Play();
                }
            }
            catch (Exception e)
            {
            }
        }

        /// <summary>
        ///     PlaybackLoop
        ///     Loops forever and adds received data to Waveprovider.
        ///     Exits when an Exception occurs (Closing connection)
        /// </summary>
        public void PlaybackLoop(Socket serverSocket)
        {
            serverSocket.NoDelay = true;
            _waveProvider.ClearBuffer();
            var bufferBytes = new byte[SampleSize];
            //Reads from Server as long as it doesn't return 0
            for (int i = 1; i != 0; i = serverSocket.Receive(bufferBytes))
            {
                _waveProvider.AddSamples(bufferBytes, 0, SampleSize);
            }
            serverSocket.Shutdown(SocketShutdown.Both);
            serverSocket.Close();
        }
    }
}