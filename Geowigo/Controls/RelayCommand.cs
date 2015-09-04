using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Geowigo.Controls
{
	/// <summary>
	/// A generic command which relays its call to an Action.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class RelayCommand<T> : ICommand where T : class
	{
		private Predicate<T> _CanExecute;
		private Action<T> _Execute;

		public event EventHandler CanExecuteChanged;

		public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
		{
			_CanExecute = canExecute;
			_Execute = execute;
		}
		
		public bool CanExecute(object parameter)
		{
			if (parameter is T)
			{
				if (_CanExecute != null)
				{
					return _CanExecute((T)parameter);
				}
				else
				{
					return true;
				}
			}
			else
			{
				return false;
			}
		}

		public bool CanExecute(T parameter)
		{
			if (_CanExecute == null)
			{
				return true;
			}

			return _CanExecute(parameter);
		}

		public void Execute(object parameter)
		{
			T param = parameter as T;
			if (param == null)
			{
				throw new ArgumentException("Argument is not a " + typeof(T).ToString(), "parameter");
			}

			_Execute(param);
		}

		public void Execute(T parameter)
		{
			_Execute(parameter);
		}
	}

	/// <summary>
	/// A generic command which relays its call to an Action.
	/// </summary>
	public class RelayCommand : ICommand
	{
		private Func<bool> _CanExecute;
		private Action _Execute;

        private bool? _LastCanExecuteValue;

		public event EventHandler CanExecuteChanged;

		public RelayCommand(Action execute)
		{
			_Execute = execute;
            _CanExecute = () => { return true; };
		}

        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _Execute = execute;
            _CanExecute = canExecute;
        }

		public bool CanExecute(object parameter)
		{
			bool canExecute = _CanExecute();
            bool valueChanged = canExecute != _LastCanExecuteValue;
            _LastCanExecuteValue = canExecute;

            if (valueChanged && CanExecuteChanged != null)
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }

            return canExecute;
		}

        public void RefreshCanExecute()
        {
            CanExecute(null);
        }

		public void Execute(object parameter)
		{
			_Execute();
		}
	}
}
