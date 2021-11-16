using OpenTK.Mathematics;
using System.Drawing;

namespace NonlinearFilters.Filters2
{
	public class NonLocalMeansFilter : BaseFilter
	{
		public double? HParam { get; private set; }

		private int[]? done;
		private int windowRadius, patchRadius;
		private readonly double size;
		private readonly double sigma;

		//src: https://www.ipol.im/pub/art/2011/bcm_nlm/article.pdf
		private readonly List<Parameters> grayscaleLookup = new()
		{
			(15, 1, 10, 0.4),
			(30, 2, 10, 0.4),
			(45, 3, 17, 0.35),
			(75, 4, 17, 0.35),
			(100, 5, 17, 0.30)
		};

		public NonLocalMeansFilter(ref Bitmap input, double sigma) : base(ref input)
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

			for (int py = threadWindow.Y; py < threadWindow.Y + threadWindow.Height; py++)
			{
				int starty = Math.Max(py - windowRadius, 0);
				int endy = Math.Min(py + windowRadius, Bounds.Height);

				for (int px = threadWindow.X; px < threadWindow.X + threadWindow.Width; px++)
				{
					double centerPatch = PatchNeighborhood(inPtr, px, py);
					double normalizeFactor = 0;
					double weightedSum = 0;

					int startx = Math.Max(px - windowRadius, 0);
					int endx = Math.Min(px + windowRadius, Bounds.Width);

					for (int y = starty; y < endy; y++)
					{
						for (int x = startx; x < endx; x++)
						{
							double currentPatch = PatchNeighborhood(inPtr, x, y);
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

		private unsafe double PatchNeighborhood(byte* inPtr, int cx, int cy)
		{
			int startx = Math.Max(cx - patchRadius, 0);
			int starty = Math.Max(cy - patchRadius, 0);

			int endx = Math.Min(cx + patchRadius, Bounds.Width);
			int endy = Math.Min(cy + patchRadius, Bounds.Height);

			int lenx = endx - startx;
			int pixelCount = lenx * (endy - starty);

			long pixelIntensitySum = 0;
			for (int y = starty; y < endy; y++)
			{
				byte* start = Coords2Ptr(inPtr, startx, y);
				for (byte* ptr = start; ptr < start + lenx; ptr += 4)
				{
					pixelIntensitySum += *ptr;
				}
			}

			return (double)pixelIntensitySum / pixelCount;
		}

		private void UpdateProgress()
		{
			int sum = done![0];
			for (int i = 1; i < done.Length; i++)
				sum += done[i];
			ChangeProgress(sum * size);
		}

		internal sealed record Parameters(int MaxSigma, int NeighborhoodPatchRadius, int WindowRadius, double HParamCoeff)
		{
			public static implicit operator (int MaxSigma, int NeighborhoodPatchRadius, int WindowRadius, double HParamCoeff)(Parameters value)
			{
				return (value.MaxSigma, value.NeighborhoodPatchRadius, value.WindowRadius, value.HParamCoeff);
			}

			public static implicit operator Parameters((int maxSigma, int neighborhoodPatchRadius, int windowRadius, double hParamCoeff) value)
			{
				return new Parameters(value.maxSigma, value.neighborhoodPatchRadius, value.windowRadius, value.hParamCoeff);
			}
		}
	}
}
