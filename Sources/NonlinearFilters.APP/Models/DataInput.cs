using NonlinearFilters.Volume;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace NonlinearFilters.APP.Models
{
	public class DataInput
	{
		public VolumetricData? Volume { get; }
		public Image<Rgba32>? Image { get; }

		public DataInput(VolumetricData volume)
		{
			Volume = volume;
		}

		public DataInput(Image<Rgba32> image)
		{
			Image = image;
		}

		public override string ToString()
		{
			if (Volume is not null)
				return $"Volume {Volume.Size.X}x{Volume.Size.Y}x{Volume.Size.Z}";
			else
				return $"Image {Image!.Width}x{Image.Height}";
		}

		public static DataInput Load(string path) => VolumetricData.FileIsVolume(path) switch
		{
			true => new(VolumetricData.FromFile(path)),
			false => new(SixLabors.ImageSharp.Image.Load<Rgba32>(path))
		};
	}
}
