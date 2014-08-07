using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using VoiceChatWPF.Annotations;
using VoiceChatWPF.Commands;
using VoiceChatWPF.Models;

namespace VoiceChatWPF.ViewModels
{

    internal class VoiceChatViewModel : INotifyPropertyChanged
    {
        public static int AudioIndex;
        public static string Adress = "127.0.0.1";
        public static int AudioBuffer;
        public static int BufferLength = 5;
        private readonly DispatcherTimer _audioTimer;
        private readonly ConnectionEndPoint _clientEndpoint;
        private readonly ListeningEndpoint _listeningEndpoint;
        private int _volumeSlider;

        public VoiceChatViewModel()
        {
            Model model = new Model();

            _listeningEndpoint = model.ListeningEndpoint;
               
                _listeningEndpoint.ButtonEvent += ConnectionHandlingOnRaiseCustomEvent;

                _clientEndpoint = model.ConnectionEndPoint;
                _clientEndpoint.ButtonEvent += ConnectionHandlingOnRaiseCustomEvent;
                ConnectCommand = new RelayCommand(param => _listeningEndpoint.Connect());
                DisconnectCommand = new RelayCommand(param => _listeningEndpoint.DropCall());


            _volumeSlider = SystemVolumeChanger.GetVolume();
            AudioDevicesFromCb = AudioDeviceEnumerator.GetAudioDevices();
            
            _audioTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(10)};
            _audioTimer.Tick += AudioTimerOnTick;
            _audioTimer.Start();
            Application.Current.Exit += CurrentOnExit;
        }




        //Connect and Disconnect click event
        public RelayCommand ConnectCommand { get; private set; }
        public RelayCommand DisconnectCommand { get; private set; }
        public RelayCommand AsioSettings { get; private set; }

        public TimeSpan AudioDuration { get; private set; }
        //Binds SelectedIndex from ComboBox to AudioIndex
        public int AudioIndexFromCb
        {
            get { return AudioIndex; }
            set
            {
                AudioIndex = value;
                OnPropertyChanged();
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

        //Binds AudioBuffer to ProgressBar (Buffered Duration in WaveProvider)
        public int AudioBufferToProgressBar
        {
            get { return AudioBuffer; }
            set
            {
                AudioBuffer = value;
                OnPropertyChanged();
            }
        }


        //Binds BufferLength to SendStream Bufferlength
        public int BufferLengthFromSlider
        {
            get { return BufferLength; }
            set
            {
                BufferLength = value;
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

        private void CurrentOnExit(object sender, ExitEventArgs exitEventArgs)
        {
         
            //if (_audioTimer != null) _audioTimer.Stop();
            //if (_clientEndpoint != null) _clientEndpoint.Dispose();
            //if (_listeningEndpoint != null) _listeningEndpoint.Dispose();
        }


        private void AudioTimerOnTick(object sender, EventArgs eventArgs)
        {
            //if (_serverEndpoint != null) AudioDuration = _serverEndpoint.ChatDuration.Elapsed;
        }

        private void ConnectionHandlingOnRaiseCustomEvent(object sender, CustomEventArgs eventArgs)
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
    }
}