using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using VoiceChatWPF.ViewModels;

namespace VoiceChatWPF.Views
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly VoiceChatViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new VoiceChatViewModel();
            DataContext = _viewModel;
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _viewModel.Dispose();
        }
    }
}