#nullable enable
using System;
using System.Windows.Input;

namespace Beacon.Excel.Data.Presentation
{
    public sealed class DelegateCommand : ICommand
    {
        private readonly Func<bool>? _canExecute;
        private readonly Action _execute;

        public DelegateCommand(Action execute, Func<bool>? canExecute = null)
        {
            (this._execute, this._canExecute) = (execute, canExecute);
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add
            {
                if (this._canExecute != null)
                {
                    CommandManager.RequerySuggested += value;
                }
            }
            remove
            {
                if (this._canExecute != null)
                {
                    CommandManager.RequerySuggested -= value;
                }
            }
        }

        public bool CanExecute() => this._canExecute == null || this._canExecute();

        bool ICommand.CanExecute(object parameter) => this.CanExecute();

        public void Execute()
        {
            if (this.CanExecute())
            {
                this._execute();
            }
        }

        void ICommand.Execute(object parameter) => this.Execute();
    }
}