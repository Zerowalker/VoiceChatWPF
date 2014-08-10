using System;
using System.Threading;
using System.Windows.Input;

namespace VoiceChatWPF.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _action;
        private readonly SynchronizationContext _syncContext;

        private bool _canExecute;

        public RelayCommand(Action<object> action)
        {
            _canExecute = true;
            this._action = action;
            _syncContext = SynchronizationContext.Current;
        }

        public bool CanExecute
        {
            get { return _canExecute; }
            set
            {
                _canExecute = value;
                RaiseCanExecuteChanged();
            }
        }

        public event EventHandler CanExecuteChanged;

        bool ICommand.CanExecute(object parameter)
        {
            return _canExecute;
        }

        void ICommand.Execute(object parameter)
        {
            _action(parameter);
        }

        private void RaiseCanExecuteChanged()
        {
            EventHandler handler = CanExecuteChanged;
            if (handler == null) return;
            if (_syncContext != null)
                _syncContext.Post(_ => handler(this, EventArgs.Empty), null);
            else
                handler(this, EventArgs.Empty);
        }
    }
}