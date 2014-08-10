using System;
using System.Net.Sockets;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace VoiceChatWPF.Audio
{
    public sealed class PlaybackEndpoint : IDisposable
    {
        private const int SampleSize = 960; //5ms 16bit 48khz Stereo
        public static int BufferedDuration;
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
        ///     Initialize ASIO and starts playing
        ///     If ASIO throws exception, use Wasapi
        /// </summary>
        private void InitializeAsio()
        {
            try
            {
                if (_asioOut != null) _asioOut.Dispose();
                _asioOut = new AsioOut(1);
                _asioOut.Init(_waveProvider);
                _asioOut.Play();
            }
            catch (Exception e)
            {
                InitializeWasapi();
            }
        }

        /// <summary>
        ///     Initialize Wasapi and starts playing
        /// </summary>
        private void InitializeWasapi()
        {
            if (_wasapiOut != null) _wasapiOut.Dispose();
            _wasapiOut = new WasapiOut(AudioClientShareMode.Shared, true, 500);
            _wasapiOut.Init(_waveProvider);
            _wasapiOut.Play();
        }

        /// <summary>
        ///     Initialize either ASIO or Wasapi
        /// </summary>
        private void InitilizeAudio()
        {
            _waveProvider.DiscardOnBufferOverflow = true;
            _waveProvider.BufferDuration = new TimeSpan(0, 0, 0, 0, 110);
            //if (AsioOut.isSupported())
                //InitializeAsio();
            //else
                InitializeWasapi();
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
            var bufferBytes = new byte[192000];
            int size;
            //Reads from Server as long as it doesn't return 0
            while ((size = serverSocket.Receive(bufferBytes)) != 0)
            {
                _waveProvider.AddSamples(bufferBytes, 0, size);

                BufferedDuration = _waveProvider.BufferedDuration.Milliseconds;
                if (BufferedDuration == _waveProvider.BufferDuration.Milliseconds)
                    _waveProvider.ClearBuffer();
            }

            serverSocket.Shutdown(SocketShutdown.Both);
            serverSocket.Close();
        }
    }
}