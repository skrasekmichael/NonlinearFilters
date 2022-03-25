using SixLabors.ImageSharp;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.IO;

namespace NonlinearFilters.Extensions
{
	public static class BitmapExtensions
	{
		public static BitmapImage ToBitmapImage(this SixLabors.ImageSharp.Image img)
		{
			using var memory = new MemoryStream();
			img.SaveAsPng(memory);
			memory.Position = 0;

			var bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = memory;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			bitmapImage.Freeze();

			return bitmapImage;
		}
	}
}
