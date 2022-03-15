using System.Windows.Input;
using MVVM = Microsoft.Toolkit.Mvvm.Input;

namespace NonlinearFilters.APP.Commands
{
	public class RelayCommand : ICommand
	{
		private readonly MVVM.RelayCommand relayCommand;

		public RelayCommand(Action execute, Func<bool>? canExecute = null)
		{
			relayCommand = canExecute is null ? new MVVM.RelayCommand(execute) : new MVVM.RelayCommand(execute, canExecute);
		}

		public bool CanExecute(object? parameter) => relayCommand.CanExecute(parameter);

		public void Execute(object? parameter) => relayCommand.Execute(parameter);

		public event EventHandler? CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}
	}

	public class RelayCommand<T> : ICommand
	{
		private readonly MVVM.RelayCommand<T> relayCommand;

		public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
		{
			relayCommand = canExecute is null ? new MVVM.RelayCommand<T>(execute) : new MVVM.RelayCommand<T>(execute, canExecute);
		}

		public bool CanExecute(object? parameter) => relayCommand.CanExecute(parameter);

		public void Execute(object? parameter) => relayCommand.Execute(parameter);

		public event EventHandler? CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}
	}
}
