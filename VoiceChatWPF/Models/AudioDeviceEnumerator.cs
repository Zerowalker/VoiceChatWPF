using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Collections.ObjectModel;

namespace VoiceChatWPF
{
   static class AudioDeviceEnumerator
    {

        /// <summary>
        /// Get all Active Recording Devices
        /// Add them to AudioDevices(ComboBox)
        /// </summary>
        public static ObservableCollection<string> GetAudioDevices()
        {       int waveInDevice;
               ObservableCollection<string> audioDevices = new ObservableCollection<string>(); 
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            for (waveInDevice = 0; waveInDevice < WaveIn.DeviceCount; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                audioDevices.Add(deviceInfo.ProductName);
            }


            audioDevices.Add("Wasapi Loopback");
            return audioDevices;
        }
    }
}
