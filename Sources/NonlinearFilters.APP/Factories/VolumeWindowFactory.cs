using NonlinearFilters.APP.VolumeRenderer;

namespace NonlinearFilters.APP.Factories
{
	public class VolumeWindowFactory : IFactory<VolumeWindow>
	{
		public VolumeWindow Create()
		{
			var gameWindowSettings = new OpenTK.Windowing.Desktop.GameWindowSettings()
			{
				RenderFrequency = 60,
			};

			var nativeWindowSettings = new OpenTK.Windowing.Desktop.NativeWindowSettings()
			{
				Title = "Volume renderer",
				Size = new(800, 600)
			};

			return new VolumeWindow(gameWindowSettings, nativeWindowSettings);
		}
	}
}
