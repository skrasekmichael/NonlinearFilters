using NonlinearFilters.Volume;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace NonlinearFilters.APP.Models
{
	public class DataInput
	{
		public VolumetricData? Volume { get; private set; }
		public Image<Rgba32>? Image { get; private set; }

		public DataInput(VolumetricData volume)
		{
			Volume = volume;
			Image = null;
		}

		public DataInput(Image<Rgba32> image)
		{
			Image = image;
			Volume = null;
		}

		public override string ToString()
		{
			if (Volume is not null)
				return $"Volume {Volume.Size.X}x{Volume.Size.Y}x{Volume.Size.Z}";
			else
				return $"Image {Image!.Width}x{Image.Height}";
		}
	}
}
