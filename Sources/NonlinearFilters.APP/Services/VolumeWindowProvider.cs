﻿using NonlinearFilters.APP.Factories;
using NonlinearFilters.APP.VolumeRenderer;
using NonlinearFilters.Mathematics;
using System.Drawing;

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

		public void Render(VolumetricImage volume)
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

		public Bitmap CaptureVolumeWindow(VolumetricImage volume)
		{
			if (window is null || !window.Exists)
			{
				window = factory.Create();
				window.InitVolume(volume);
				window.Run();
				var bmp = window.Capture();
				window.Dispose();
				window.Close();
				return bmp;
			}

			return window.Capture();
		}
	}
}