using System.Net.Sockets;
using System.Windows.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Windows;

namespace VoiceChatWPF.Models
{
    public sealed class PlaybackEndpoint : IDisposable
    {
        private readonly BufferedWaveProvider _waveProvider = new BufferedWaveProvider(new WaveFormat(48000, 16, 2));
        private WasapiOut _wasapiOut;
        private AsioOut _asioOut;
        private const int SampleSize = 960; //5ms 16bit 48khz Stereo
       public PlaybackEndpoint()
       {
           InitilizeAudio();
       }
        /// <summary>
        ///     Set Parameters to SendStream and starts Playing wasapiOut
        /// </summary>
       private void InitilizeAudio()
        {
            try
            {
                _waveProvider.DiscardOnBufferOverflow = true;
                //if (AsioOut.isSupported())
                //{
                //    if (_asioOut != null) _asioOut.Dispose();
                //    _asioOut = new AsioOut(1);
                //    _asioOut.Init(_waveProvider);
                //    _asioOut.Play();

                //}
                //else
                {

                    _wasapiOut = new WasapiOut(AudioClientShareMode.Shared, true, 10);
                    _wasapiOut.Init(_waveProvider);
                    _wasapiOut.Play();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "InitilizeAudio", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

       /// <summary>
       ///     PlaybackLoop
       ///     Loops forever and adds received data to Waveprovider.
       ///     Exits when an Exception occurs (Closing connection)
       /// </summary>
       public void PlaybackLoop(ref Socket serverSocket)
       {
           _waveProvider.ClearBuffer();
           var bufferBytes = new byte[SampleSize];
           while (true)
           {
               serverSocket.Receive(bufferBytes);
               _waveProvider.AddSamples(bufferBytes,0,SampleSize);
           }
       }

        public void Dispose()
        {
            if (_wasapiOut != null) _wasapiOut.Dispose();
            if (_asioOut != null) _asioOut.Dispose();
        }
    }
}
