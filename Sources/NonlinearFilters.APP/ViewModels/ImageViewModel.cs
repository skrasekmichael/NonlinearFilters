using NonlinearFilters.APP.Models;
using NonlinearFilters.APP.Services;
using NonlinearFilters.APP.Messages;
using NonlinearFilters.APP.Commands;
using NonlinearFilters.Extensions;
using System.Windows.Media.Imaging;
using System.Windows.Input;

namespace NonlinearFilters.APP.ViewModels
{
	public class ImageViewModel : BaseViewModel
	{
		public ICommand RenderVolumeCommand { get; }
		public ICommand SaveDataCommand { get; }

		private DataInput? _data;
		public DataInput? Data
		{
			get => _data;
			set
			{
				_data = value;
				if (_data is not null)
				{
					if (_data.Image is null)
						BitmapImage = _data.Volume!.Render().ToBitmapImage();
					else
						BitmapImage = _data.Image.ToBitmapImage();
				}
			}
		}

		private BitmapImage? _bitmapImage;
		public BitmapImage? BitmapImage
		{
			get => _bitmapImage;
			set
			{
				_bitmapImage = value;
				OnPropertyChanged();
			}
		}

		public ImageViewModel(Mediator mediator)
		{
			RenderVolumeCommand = new RelayCommand(() => 
				mediator.Send(new RenderVolumeMessage(Data!.Volume!)), () => Data is not null && Data.Volume is not null);

			SaveDataCommand = new RelayCommand(() => 
				mediator.Send(new SaveMessage(Data!)), () => Data is not null);
		}
	}
}
