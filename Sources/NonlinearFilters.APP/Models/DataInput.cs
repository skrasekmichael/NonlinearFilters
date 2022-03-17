using NonlinearFilters.Mathematics;
using System.Drawing;

namespace NonlinearFilters.APP.Models
{
	public class DataInput
	{
		public VolumetricImage? Volume { get; private set; }
		public Bitmap? Image { get; private set; }

		public DataInput(VolumetricImage volume)
		{
			Volume = volume;
			Image = null;
		}

		public DataInput(Bitmap image)
		{
			Image = image;
			Volume = null;
		}

		public override string ToString()
		{
			if (Volume is not null)
				return $"Volume {Volume.Size.X}x{Volume.Size.Y}x{Volume.Size.Z}";
			else
				return $"Image {Image!.Size.Width}x{Image.Size.Height} {Image.PixelFormat}";
		}
	}
}
