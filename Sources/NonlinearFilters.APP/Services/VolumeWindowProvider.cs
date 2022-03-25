using NonlinearFilters.APP.Factories;
using NonlinearFilters.APP.VolumeRenderer;
using NonlinearFilters.Volume;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace NonlinearFilters.APP.Services
{
	public class VolumeWindowProvider
	{
		private readonly VolumeWindowFactory factory;

		private VolumeWindow? window = null;

		public VolumeWindowProvider(VolumeWindowFactory factory)
		{
			this.factory = factory;
		}

		public void Render(VolumetricData volume)
		{
			if (window is null || !window.Exists)
			{
				window = factory.Create();
				window.InitVolume(volume);
				window.Run();
				window.Dispose();
				return;
			}

			window.SetVolume(volume);
		}

		public Image<Rgb24> CaptureVolumeWindow(VolumetricData volume)
		{
			if (window is null || !window.Exists)
			{
				window = factory.Create();
				window.InitVolume(volume);
				window.Run();
				var img = window.Capture();
				window.Dispose();
				window.Close();
				return img;
			}

			return window.Capture();
		}
	}
}
