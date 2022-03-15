using NonlinearFilters.APP.Commands;
using NonlinearFilters.APP.Messages;
using NonlinearFilters.APP.Models;
using NonlinearFilters.APP.Services;
using NonlinearFilters.Filters.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace NonlinearFilters.APP.ViewModels
{
	public class FilterViewModel : BaseViewModel
	{
		public ICommand ApplyFilterCommand { get; }
		public ICommand CancelCommand { get; }

		public ObservableCollection<FilterParameter> Parameters { get; } = new();

		private IFilter? _filter;
		public IFilter? Filter
		{
			get => _filter;
			set
			{
				_filter = value;
				OnPropertyChanged();
			}
		}

		private int _processCount = Environment.ProcessorCount - 1;
		public int ProcessCount
		{
			get => _processCount;
			set
			{
				_processCount = value;
				OnPropertyChanged();
			}
		}

		private object? parameterInstance;

		public FilterViewModel(Mediator mediator)
		{
			ApplyFilterCommand = new RelayCommand(() =>
			{
				//updating parameters
				foreach (var param in Parameters)
					param.Property.SetValue(parameterInstance, param.Value);

				if (Filter is IFilter2 filter2)
					mediator.Send<ApplyFilter2Message>(new(filter2, ProcessCount));
				else if (Filter is IFilter3 filter3)
					mediator.Send<ApplyFilter3Message>(new(filter3, ProcessCount));

				Filter = null;
			});

			CancelCommand = new RelayCommand(() => Filter = null);
		}

		public void SetFilter(IFilter filter, object parameterInstance)
		{
			Filter = filter;
			this.parameterInstance = parameterInstance;

			Parameters.Clear();

			var properties = parameterInstance.GetType().GetProperties();
			foreach (var property in properties)
			{
				var val = property.GetValue(parameterInstance);
				if (val is not null)
					Parameters.Add(new(property.Name, val, property));
			}
		}
	}
}
