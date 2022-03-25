using NonlinearFilters.Volume;
using NonlinearFilters.APP.Models;
using NonlinearFilters.APP.Services;
using NonlinearFilters.APP.Messages;
using NonlinearFilters.APP.Commands;
using NonlinearFilters.Extensions;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;

namespace NonlinearFilters.APP.ViewModels
{
	public class DataViewModel : BaseViewModel
	{
		public ICommand CaptureVolumeWindowCommand { get; }
		public ICommand RenderVolumeCommand { get; }
		public ICommand SaveDataCommand { get; }
		public ICommand SaveBitmapCommand { get; }

		private DataInput? _data;
		public DataInput? Data
		{
			get => _data;
			set
			{
				_data = value;
				CaptureImage = null;

				if (_data is not null)
				{
					if (_data.Image is null)
					{
						var image = _data.Volume!.Render();
						DataImage = image.ToBitmapImage();
						image.Dispose();
					}
					else
						DataImage = _data.Image.ToBitmapImage();
				}

				SelectedTabIndex = 0;
				OnPropertyChanged();
			}
		}

		private BitmapImage? _dataImage;
		public BitmapImage? DataImage
		{
			get => _dataImage;
			set
			{
				_dataImage = value;
				OnPropertyChanged();
			}
		}

		private BitmapImage? _captureImage;
		public BitmapImage? CaptureImage
		{
			get => _captureImage;
			set
			{
				_captureImage = value;
				if (value is not null)
					SelectedTabIndex = 1;
				OnPropertyChanged();
			}
		}

		private int _selectedTabIndex;
		public int SelectedTabIndex
		{
			get => _selectedTabIndex;
			set
			{
				_selectedTabIndex = value;
				OnPropertyChanged();
			}
		}

		private bool IsVolume => Data?.Volume is not null;

		private readonly SaveFileDialog saveFileDialog = new();

		public DataViewModel(Mediator mediator, VolumeWindowProvider volumeWindowProvider)
		{
			RenderVolumeCommand = new RelayCommand(() =>
				mediator.Send(new RenderVolumeMessage(Data!.Volume!)), () => IsVolume);

			CaptureVolumeWindowCommand = new RelayCommand(() => {
				var img = volumeWindowProvider.CaptureVolumeWindow(Data!.Volume!);
				CaptureImage = img.ToBitmapImage();
				img.Dispose();
			}, () => IsVolume);

			SaveDataCommand = new RelayCommand(SaveData, () => Data is not null);

			SaveBitmapCommand = new RelayCommand<BitmapImage?>(bitmapImage =>
			{
				if (bitmapImage is null)
					return;

				saveFileDialog.Filter = ".png|*.png";
				if (saveFileDialog.ShowDialog() == true)
				{
					var encoder = new PngBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create(bitmapImage));

					using var stream = new FileStream(saveFileDialog.FileName, FileMode.Create);
					encoder.Save(stream);
				}
			});
		}

		private void SaveData()
		{
			Action<string> saveFunc;
			if (Data!.Volume is not null)
			{
				saveFileDialog.Filter = VolumetricData.FileFilter;
				saveFunc = path => VolumetricData.SaveFile(Data.Volume, path);
			}
			else
			{
				saveFileDialog.Filter = ".png|*.png";
				saveFunc = path => Data.Image!.Save(path, System.Drawing.Imaging.ImageFormat.Png);
			}

			if (saveFileDialog.ShowDialog() == true)
				saveFunc(saveFileDialog.FileName);
		}
	}
}
