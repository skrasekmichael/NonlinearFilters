using System.Drawing;
using static NonlinearFilters.Filters2.NonLocalMeansFilter;

namespace NonlinearFilters.Filters2
{
	public class FastNonLocalMeansFilter : BaseFilter
	{
		public double? HParam { get; private set; }

		private int[]? done;
		private int windowRadius, patchRadius;
		private double patchArea;
		private readonly double size;
		private readonly double sigma;

		private long[,] integralImage = null!;

		//src: https://www.ipol.im/pub/art/2011/bcm_nlm/article.pdf
		private readonly List<Parameters> grayscaleLookup = new()
		{
			new Parameters(15, 1, 10, 0.4),
			new Parameters(30, 2, 10, 0.4),
			new Parameters(45, 3, 17, 0.35),
			new Parameters(75, 4, 17, 0.35),
			new Parameters(100, 5, 17, 0.30)
		};

		public FastNonLocalMeansFilter(ref Bitmap input, double sigma) : base(ref input)
		{
			this.sigma = sigma;
			size = 100.0 / (Bounds.Width * Bounds.Height);
		}

		public override Bitmap ApplyFilter(int cpuCount = 1, bool isGrayScale = true)
		{
			cpuCount = Math.Clamp(cpuCount, 1, Environment.ProcessorCount);
			done = new int[cpuCount];

			foreach (var elem in grayscaleLookup)
			{
				if (sigma <= elem.MaxSigma)
				{
					windowRadius = elem.WindowRadius;
					patchRadius = elem.NeighborhoodPatchRadius;
					patchArea = Math.Pow(2 * patchRadius + 1, 2);
					HParam = sigma * elem.HParamCoeff;
					break;
				}
			}

			return FilterArea(cpuCount, FilterWindow);
		}

		private unsafe void FilterWindow(Rectangle threadWindow, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			fixed (long* ptr = integralImage)
			{
				for (int py = threadWindow.Y; py < threadWindow.Y + threadWindow.Height; py++)
				{
					int starty = Math.Max(py - windowRadius, 0);
					int endy = Math.Min(py + windowRadius, Bounds.Height);

					for (int px = threadWindow.X; px < threadWindow.X + threadWindow.Width; px++)
					{
						double centerPatch = PatchNeighborhood(px, py);
						double normalizeFactor = 0;
						double weightedSum = 0;

						int startx = Math.Max(px - windowRadius, 0);
						int endx = Math.Min(px + windowRadius, Bounds.Width);

						for (int y = starty; y < endy; y++)
						{
							for (int x = startx; x < endx; x++)
							{
								double currentPatch = PatchNeighborhood(x, y);
								double gaussianWeightingFunction = Math.Exp(
									-Math.Pow((currentPatch - centerPatch) / HParam!.Value, 2)
								);

								normalizeFactor += gaussianWeightingFunction;
								weightedSum += GetIntensity(Coords2Ptr(inPtr, x, y)) * gaussianWeightingFunction;
							}
						}

						double newIntensity = weightedSum / normalizeFactor;
						SetIntensity(Coords2Ptr(outPtr, px, py), (byte)newIntensity);
						done![index]++;
					}
					UpdateProgress();
				}
			}
		}

		private unsafe double PatchNeighborhood(int cx, int cy)
		{
			int startx = Math.Max(cx - patchRadius, 0);
			int starty = Math.Max(cy - patchRadius, 0);

			int endx = Math.Min(cx + patchRadius, Bounds.Width - 1);
			int endy = Math.Min(cy + patchRadius, Bounds.Height - 1);

			int lenx = endx + 1 - startx;
			int pixelCount = lenx * (endy + 1 - starty);

			long A = 0, B = 0, C = 0;
			long D = integralImage[endy, endx];

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

		protected override unsafe void PreCompute(Rectangle bounds, IntPtr inputPtr, IntPtr outputPtr)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();

			integralImage = new long[bounds.Height, bounds.Width];
			integralImage[0, 0] = *inPtr; //first cell

			//first row
			for (int x = 1; x < bounds.Width; x++)
			{
				byte intensity = GetIntensity(Coords2Ptr(inPtr, x, 0));
				integralImage[0, x] = integralImage[0, x - 1] + intensity;
			}

			//first column
			for (int y = 1; y < bounds.Height; y++)
			{
				byte intensity = GetIntensity(Coords2Ptr(inPtr, 0, y));
				integralImage[y, 0] = integralImage[y - 1, 0] + intensity;
			}

			for (int y = 1; y < bounds.Height; y++)
			{
				for (int x = 1; x < bounds.Width; x++)
				{
					long A = integralImage[y - 1, x - 1];
					long B = integralImage[y - 1, x];
					long C = integralImage[y, x - 1];

					byte intensity = GetIntensity(Coords2Ptr(inPtr, x, y));
					integralImage[y, x] = B + C - A + intensity;
				}
			}
		}

		private void UpdateProgress()
		{
			int sum = done![0];
			for (int i = 1; i < done.Length; i++)
				sum += done[i];
			ChangeProgress(sum * size);
		}
	}
}
