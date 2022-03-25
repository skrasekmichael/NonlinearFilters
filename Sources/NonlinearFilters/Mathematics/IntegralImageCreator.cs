using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace NonlinearFilters.Mathematics
{
	public class IntegralImageCreator
	{
		public long[,] CreateGrayScale(IntPtr inputPtr, Size bounds) => GrayScale(bounds, inputPtr);

		public unsafe long[,] CreateGrayScale(Image<Rgba32> img)
		{
			if (!img.DangerousTryGetSinglePixelMemory(out var memory))
				throw new Exception("Image is too large.");

			var handler = memory.Pin();
			var integralImage = GrayScale(new(img.Width, img.Height), new IntPtr(handler.Pointer));
			handler.Dispose();
			return integralImage;
		}

		private unsafe long[,] GrayScale(Size bounds, IntPtr inputPtr)
		{
			byte* coords2ptr(byte* ptr, int x, int y) => ptr + 4 * (x + y * bounds.Width);

			byte* inPtr = (byte*)inputPtr.ToPointer();

			long[,] integralImage = new long[bounds.Height, bounds.Width];
			integralImage[0, 0] = *inPtr; //first cell

			//first row
			for (int x = 1; x < bounds.Width; x++)
			{
				byte intensity = *(coords2ptr(inPtr, x, 0));
				integralImage[0, x] = integralImage[0, x - 1] + intensity;
			}

			//first column
			for (int y = 1; y < bounds.Height; y++)
			{
				byte intensity = *(coords2ptr(inPtr, 0, y));
				integralImage[y, 0] = integralImage[y - 1, 0] + intensity;
			}

			for (int y = 1; y < bounds.Height; y++)
			{
				for (int x = 1; x < bounds.Width; x++)
				{
					long A = integralImage[y - 1, x - 1];
					long B = integralImage[y - 1, x];
					long C = integralImage[y, x - 1];

					byte intensity = *(coords2ptr(inPtr, x, y));
					integralImage[y, x] = B + C - A + intensity;
				}
			}

			return integralImage;
		}
	}
}
