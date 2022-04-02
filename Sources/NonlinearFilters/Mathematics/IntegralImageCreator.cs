using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using NonlinearFilters.Volume;

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

		public long[,,] Create(VolumetricData data)
		{
			long[,,] integralImage = new long[data.Size.X, data.Size.Y, data.Size.Z];

			//first cell
			integralImage[0, 0, 0] = data[0, 0, 0];

			//x axis
			for (int x = 1; x < data.Size.X; x++)
				integralImage[x, 0, 0] = integralImage[x - 1, 0, 0] + data[x, 0, 0];

			//y axis
			for (int y = 1; y < data.Size.Y; y++)
				integralImage[0, y, 0] = integralImage[0, y - 1, 0] + data[0, y, 0];

			//z axis
			for (int z = 1; z < data.Size.Z; z++)
				integralImage[0, 0, z] = integralImage[0, 0, z - 1] + data[0, 0, z];

			//xy plane
			for (int x = 1; x < data.Size.X; x++)
			{
				for (int y = 1; y < data.Size.Y; y++)
				{
					var A = integralImage[x - 1, y - 1, 0];
					var B = integralImage[x - 1, y, 0];
					var C = integralImage[x, y - 1, 0];
					integralImage[x, y, 0] = B + C - A + data[x, y, 0];
				}
			}

			//xz plane
			for (int x = 1; x < data.Size.X; x++)
			{
				for (int z = 1; z < data.Size.Z; z++)
				{
					var A = integralImage[x - 1, 0, z - 1];
					var B = integralImage[x - 1, 0, z];
					var C = integralImage[x, 0, z - 1];
					integralImage[x, 0, z] = B + C - A + data[x, 0, z];
				}
			}

			//yz plane
			for (int y = 1; y < data.Size.Y; y++)
			{
				for (int z = 1; z < data.Size.Z; z++)
				{
					var A = integralImage[0, y - 1, z - 1];
					var B = integralImage[0, y - 1, z];
					var C = integralImage[0, y, z - 1];
					integralImage[0, y, z] = B + C - A + data[0, y, z];
				}
			}

			for (int x = 1; x < data.Size.X; x++)
			{
				for (int y = 1; y < data.Size.Y; y++)
				{
					for (int z = 1; z < data.Size.Z; z++)
					{
						//points on the cube in the integral image
						var A = integralImage[x - 1, y - 1, z];
						var B = integralImage[x, y - 1, z];
						var C = integralImage[x - 1, y, z];
						//D - trying to determine
						var E = integralImage[x - 1, y - 1, z - 1];
						var F = integralImage[x, y - 1, z - 1];
						var G = integralImage[x - 1, y, z - 1];
						var H = integralImage[x, y, z - 1];

						var volume = H + C - G + B - F - A + E; //cube volume except intensity at point D
						var D = volume + data[x, y, z]; //D

						if (A < E || F < E || G < E || B < A || C < A || B < F || C < G || H < G || H < F || D < B || D < C || D < H)
						{
							throw new Exception($"[{x}|{y}|{z}]\nA:{A}\nB:{B}\nC:{C}\nD:{D}\nE:{E}\nF:{F}\nG:{G}\nH:{H}");
						}

						integralImage[x, y, z] = D;
					}
				}
			}

			return integralImage;
		}
	}
}
