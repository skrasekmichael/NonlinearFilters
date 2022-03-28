using NonlinearFilters.Volume;
using NonlinearFilters.Filters.Interfaces;
using NonlinearFilters.Filters2D;
using NonlinearFilters.Filters3D;
using NonlinearFilters.APP.Services;
using NonlinearFilters.APP.Messages;
using NonlinearFilters.APP.Commands;
using NonlinearFilters.APP.Models;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows;
using Microsoft.Win32;

namespace NonlinearFilters.APP.ViewModels
{
	public class MainViewModel : BaseViewModel
	{
		public ICommand OpenFileCommand { get; }
		public ICommand CancelFilteringCommand { get; }
		public ICommand SelectFilter2Command { get; }
		public ICommand SelectFilter3Command { get; }

		public DataViewModel InputViewModel { get; }
		public DataViewModel OutputViewModel { get; }
		public FilterViewModel FilterViewModel { get; }

		private TimeSpan _duration = TimeSpan.Zero;
		public TimeSpan Duration
		{
			get => _duration;
			set
			{
				_duration = value;
				OnPropertyChanged();
			}
		}

		private double _progress = 0;
		public double Progress
		{
			get => _progress;
			set
			{
				_progress = value;
				OnPropertyChanged();
			}
		}

		private bool _isFiltering = false;
		public bool IsFiltering
		{
			get => _isFiltering;
			set
			{
				_isFiltering = value;
				OnPropertyChanged();
			}
		}

		public List<Type> Filters2 { get; } = new()
		{
			typeof(BilateralFilter),
			typeof(FastBilateralFilter),
			typeof(NonLocalMeansFilter),
			typeof(FastNonLocalMeansFilter)
		};

		public List<Type> Filters3 { get; } = new()
		{
			typeof(BilateralFilter3),
			typeof(FastBilateralFilter3)
		};

		private readonly Stopwatch watch = new();

		private readonly OpenFileDialog openFileDialog = new()
		{
			Filter = $".png|*.png|.jpg|*.jpg|{VolumetricData.FileFilter}",
			Multiselect = false
		};

		public MainViewModel(Mediator mediator, VolumeWindowProvider volumeWindowProvider,
			DataViewModel inputViewModel, DataViewModel outputViewModel, FilterViewModel filterViewModel)
		{
			InputViewModel = inputViewModel;
			OutputViewModel = outputViewModel;
			FilterViewModel = filterViewModel;

			OpenFileCommand = new RelayCommand(OpenFile, () => !IsFiltering);
			CancelFilteringCommand = new RelayCommand(CancelFiltering, () => IsFiltering);
			SelectFilter2Command = new RelayCommand<Type?>(SelectFilter, _ => !IsFiltering && InputViewModel.Data?.Image is not null);
			SelectFilter3Command = new RelayCommand<Type?>(SelectFilter, _ => !IsFiltering && InputViewModel.Data?.Volume is not null);

			mediator.Register<RenderVolumeMessage>(msg => volumeWindowProvider.Render(msg.Volume));
			mediator.Register<ApplyFilter2Message>(msg => ApplyFilter(msg.Filter, img => OutputViewModel.Data = new(img), msg.ProcessCount));
			mediator.Register<ApplyFilter3Message>(msg => ApplyFilter(msg.Filter, vol => OutputViewModel.Data = new(vol), msg.ProcessCount));
		}

		private void OpenFile()
		{
			if (openFileDialog.ShowDialog() == true)
			{
				try
				{
					InputViewModel.Data = DataInput.Load(openFileDialog.FileName);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Loading error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void CancelFiltering()
		{

		}

		private void SelectFilter(Type? filterType)
		{
			if (filterType is null)
				return;

			var filterCtor = filterType.GetConstructors().First();
			var paramCtor = filterCtor.GetParameters()[1].ParameterType.GetConstructors().First();
			var @params = paramCtor.GetParameters();

			var args = new object?[@params.Length];
			for (int i = 0; i < @params.Length; i++)
			{
				if (@params[i].HasDefaultValue)
					args[i] = @params[i].DefaultValue;
				else
				{
					var val = Activator.CreateInstance(@params[i].ParameterType);
					if (val is not null)
						args[i] = val;
				}
			}

			var paramInstance = paramCtor.Invoke(args);
			object input = InputViewModel.Data!.Volume as object ?? InputViewModel.Data.Image!;
			var filterInstance = filterCtor.Invoke(new object[] { input, paramInstance });

			if (filterInstance is IFilterProgressChanged filter)
				filter.OnProgressChanged += (_, percentage) => Progress = percentage;

			FilterViewModel.SetFilter((IFilter)filterInstance, paramInstance);
		}

		private void ApplyFilter<TOutput>(IFilterOutput<TOutput> filter, Action<TOutput> outputFunc, int processCount)
		{
			Task.Factory.StartNew(() =>
			{
				IsFiltering = true;

				watch.Restart();
				var output = filter.ApplyFilter(processCount);
				watch.Stop();

				outputFunc(output);
				Duration = watch.Elapsed;

				IsFiltering = false;
			});
		}
	}
}
