using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.ComponentModel;
using System.Windows;
using VoiceChatWPF.Models;
using VoiceChatWPF.ViewModels;

namespace VoiceChatWPF.Views
{
 
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new VoiceChatViewModel();
        }
  
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //ConnectionHandling.CloseConnections();
        }
    }
}