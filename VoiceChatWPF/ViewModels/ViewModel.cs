using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using VoiceChatWPF.Annotations;
using VoiceChatWPF.Audio;
using VoiceChatWPF.Commands;
using VoiceChatWPF.Events;
using VoiceChatWPF.Models;
using VoiceChatWPF.Network;

namespace VoiceChatWPF.ViewModels
{

    internal class VoiceChatViewModel : INotifyPropertyChanged, IDisposable
    {
        private int audioIndex;
        public static string Adress = "127.0.0.1";
         private int bufferLength = 10;
        private readonly ConnectionEndPoint _clientEndpoint;
        private readonly ListeningEndpoint _listeningEndpoint;
        private int _volumeSlider;
        private readonly Model _model;
        public VoiceChatViewModel()
        {
            _model = new Model();
            
            _listeningEndpoint = _model.ListeningEndpoint;

            _listeningEndpoint.ButtonEvent += ConnectionHandlingOnRaiseCustomEvent;

            _clientEndpoint = _model.ConnectionEndPoint;
            _clientEndpoint.ButtonEvent += ConnectionHandlingOnRaiseCustomEvent;

            ConnectCommand = new RelayCommand(param => _listeningEndpoint.Connect());
            DisconnectCommand = new RelayCommand(param => _listeningEndpoint.DropCall()) {CanExecute = false};

           

            _volumeSlider = SystemVolumeChanger.GetVolume();
            AudioDevicesFromCb = AudioDeviceEnumerator.GetAudioDevices();

            _model.AudioTimer.Tick += AudioTimerOnTick;
        }


        private void AudioTimerOnTick(object sender, EventArgs eventArgs)
        {
            if (RecordEndPoint._audioSync != null &&
                RecordEndPoint._audioSync.Elapsed != AudioDuration) 
                AudioDuration = RecordEndPoint._audioSync.Elapsed;
            if (PlaybackEndpoint.BufferedDuration != CurrentBufferedAudio) 
            CurrentBufferedAudio = PlaybackEndpoint.BufferedDuration;
        }

        //Connect and Disconnect click event
        public RelayCommand ConnectCommand { get; private set; }
        public RelayCommand DisconnectCommand { get; private set; }
        public RelayCommand AsioSettings { get; private set; }

        public TimeSpan AudioDuration { get; private set; }
        //Binds SelectedIndex from ComboBox to AudioIndex
        public int AudioIndex
        {
            get { return audioIndex; }
            set
            {
                int device = value;
                if (AudioDevicesFromCb.Count-1 == value)
                    device = -1;
                _clientEndpoint.DeviceHandler(null, new AudioDeviceEvent(device, bufferLength));
                audioIndex = value;
                //OnPropertyChanged();
            }
        }


        //Binds Textbox Text to Adress
        public string IpFromText
        {
            get { return Adress; }
            set
            {
                Adress = value;
                OnPropertyChanged();
            }
        }

        //Binds AudioDevice List to ComboBox
        public ObservableCollection<string> AudioDevicesFromCb { get; private set; }

        //
        public int CurrentBufferedAudio
        { get; set; }


        //Binds BufferLength to SendStream Bufferlength
        public int BufferLength
        {
            get { return bufferLength; }
            set
            {
                _clientEndpoint.DeviceHandler(null, new AudioDeviceEvent(AudioIndex, value));
                bufferLength = value;
                OnPropertyChanged();
            }
        }

        //Binds Volume Slider to the Actual Volume function
        public int VolumeSliderControl
        {
            get { return _volumeSlider; }
            set
            {
                SystemVolumeChanger.SetVolume(value);
                _volumeSlider = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void ConnectionHandlingOnRaiseCustomEvent(object sender, ButtonHandlerEvent eventArgs)
        {
            if (DisconnectCommand != null) DisconnectCommand.CanExecute = eventArgs.GetDisconnectionStatus;
            if (ConnectCommand != null) ConnectCommand.CanExecute = eventArgs.GetConnectionStatus;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _model.Dispose();
        }
    }
}