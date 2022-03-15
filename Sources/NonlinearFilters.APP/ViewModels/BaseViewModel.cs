using NonlinearFilters.APP.ViewModels.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace NonlinearFilters.APP.ViewModels
{
	public abstract class BaseViewModel : IViewModel, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		protected BaseViewModel()
		{
			if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				LoadInDesignMode();
			}
		}

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public virtual void LoadInDesignMode() { }
	}
}
