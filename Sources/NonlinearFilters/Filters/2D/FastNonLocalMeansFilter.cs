using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace NonlinearFilters.Filters2D
{
	public class FastNonLocalMeansFilter : BaseFilter2<FastNonLocalMeansParameters>
	{
		private long[,]? integralImage = null;

		private readonly IntegralImageCreator integralImageCreator = new();

		public FastNonLocalMeansFilter(ref Image<Rgba32> input, FastNonLocalMeansParameters parameters) : base(ref input, parameters) { }

		protected override void InitalizeParams() { }

		public override Image<Rgba32> ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, FilterWindow);

		private unsafe void FilterWindow(Rectangle threadWindow, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			fixed (long* ptr = integralImage)
			{
				for (int py = threadWindow.Y; py < threadWindow.Y + threadWindow.Height; py++)
				{
					int starty = Math.Max(py - Parameters.WindowRadius, 0);
					int endy = Math.Min(py + Parameters.WindowRadius, Bounds.Height);

					for (int px = threadWindow.X; px < threadWindow.X + threadWindow.Width; px++)
					{
						double centerPatch = PatchNeighborhood(px, py);
						double normalizeFactor = 0;
						double weightedSum = 0;

						int startx = Math.Max(px - Parameters.WindowRadius, 0);
						int endx = Math.Min(px + Parameters.WindowRadius, Bounds.Width);

						for (int y = starty; y < endy; y++)
						{
							for (int x = startx; x < endx; x++)
							{
								double currentPatch = PatchNeighborhood(x, y);
								double gaussianWeightingFunction = Math.Exp(
									-Math.Pow((currentPatch - centerPatch) / Parameters.HParam, 2)
								);

								normalizeFactor += gaussianWeightingFunction;
								weightedSum += GetIntensity(Coords2Ptr(inPtr, x, y)) * gaussianWeightingFunction;
							}
						}

						double newIntensity = weightedSum / normalizeFactor;
						SetIntensity(Coords2Ptr(outPtr, px, py), (byte)newIntensity);
						doneCounts![index]++;
					}
					UpdateProgress();
				}
			}
		}

		private unsafe double PatchNeighborhood(int cx, int cy)
		{
			int startx = Math.Max(cx - Parameters.PatchRadius, 0);
			int starty = Math.Max(cy - Parameters.PatchRadius, 0);

			int endx = Math.Min(cx + Parameters.PatchRadius, Bounds.Width - 1);
			int endy = Math.Min(cy + Parameters.PatchRadius, Bounds.Height - 1);

			int lenx = endx + 1 - startx;
			int pixelCount = lenx * (endy + 1 - starty);

			long A = 0, B = 0, C = 0;
			long D = integralImage![endy, endx];

			if (starty > 0)
			{
				B = integralImage[starty - 1, endx];
				if (startx > 0)
				{
					C = integralImage[endy, startx - 1];
					A = integralImage[starty - 1, startx - 1];
				}
			}
			else if (startx > 0)
				C = integralImage[endy, startx - 1];

			long pixelIntensitySum = D + A - B - C;
			return (double)pixelIntensitySum / pixelCount;
		}

		protected override void InitalizeFilter()
		{
			integralImage = integralImageCreator.CreateGrayScale(Input);
		}
	}
}
