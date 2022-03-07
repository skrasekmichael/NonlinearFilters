using NonlinearFilters.Filters.Parameters;
using System.Drawing;

namespace NonlinearFilters.Filters2D
{
	public class NonLocalMeansFilter : BaseFilter2<NonLocalMeansParameters>
	{
		private double patchArea;

		public NonLocalMeansFilter(ref Bitmap input, NonLocalMeansParameters parameters) : base(ref input, parameters)
		{
		}

		protected override void InitalizeParams()
		{
			patchArea = Math.Pow(2 * Parameters.PatchRadius + 1, 2);
		}

		public override Bitmap ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, Parameters.ImplementationType switch
		{
			ImplementationType.Patchwise => FilterWindowPatchwise,
			ImplementationType.Pixelwise => FilterWindowPixelwise,
			_ => throw new NotImplementedException()
		});

		private unsafe void FilterWindowPatchwise(Rectangle threadWindow, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			for (int py = threadWindow.Y; py < threadWindow.Y + threadWindow.Height; py++)
			{
				int starty = Math.Max(py - Parameters.WindowRadius, 0);
				int endy = Math.Min(py + Parameters.WindowRadius, Bounds.Height);

				for (int px = threadWindow.X; px < threadWindow.X + threadWindow.Width; px++)
				{
					double centerPatch = PatchNeighborhood(inPtr, px, py);
					double normalizeFactor = 0;
					double weightedSum = 0;

					int startx = Math.Max(px - Parameters.WindowRadius, 0);
					int endx = Math.Min(px + Parameters.WindowRadius, Bounds.Width);

					for (int y = starty; y < endy; y++)
					{
						for (int x = startx; x < endx; x++)
						{
							double currentPatch = PatchNeighborhood(inPtr, x, y);
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

		private unsafe double PatchNeighborhood(byte* inPtr, int cx, int cy)
		{
			int startx = Math.Max(cx - Parameters.PatchRadius, 0);
			int starty = Math.Max(cy - Parameters.PatchRadius, 0);

			int endx = Math.Min(cx + Parameters.PatchRadius, Bounds.Width - 1);
			int endy = Math.Min(cy + Parameters.PatchRadius, Bounds.Height - 1);

			int lenx = endx + 1 - startx;
			int pixelCount = lenx * (endy + 1 - starty);

			long pixelIntensitySum = 0;
			for (int y = starty; y <= endy; y++)
			{
				byte* start = Coords2Ptr(inPtr, startx, y);
				for (byte* ptr = start; ptr < start + 4 * lenx; ptr += 4)
				{
					pixelIntensitySum += *ptr;
				}
			}

			return (double)pixelIntensitySum / pixelCount;
		}

		private unsafe void FilterWindowPixelwise(Rectangle threadWindow, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			for (int py = threadWindow.Y; py < threadWindow.Y + threadWindow.Height; py++)
			{
				int starty = Math.Max(py - Parameters.WindowRadius, 0);
				int endy = Math.Min(py + Parameters.WindowRadius, Bounds.Height);

				for (int px = threadWindow.X; px < threadWindow.X + threadWindow.Width; px++)
				{
					double normalizeFactor = 0;
					double weightedSum = 0;

					int startx = Math.Max(px - Parameters.WindowRadius, 0);
					int endx = Math.Min(px + Parameters.WindowRadius, Bounds.Width);

					for (int y = starty; y < endy; y++)
					{
						for (int x = startx; x < endx; x++)
						{
							double weightedPatch = PixelNeighborhood(inPtr, x, y, px, py);

							normalizeFactor += weightedPatch;
							weightedSum += GetIntensity(Coords2Ptr(inPtr, x, y)) * weightedPatch;
						}
					}

					double newIntensity = weightedSum / normalizeFactor;
					SetIntensity(Coords2Ptr(outPtr, px, py), (byte)newIntensity);
					doneCounts![index]++;
				}
				UpdateProgress();
			}
		}

		private unsafe double PixelNeighborhood(byte *inPtr, int px, int py, int cx, int cy)
		{
			double sum = 0;
			for (int y = -Parameters.PatchRadius; y <= Parameters.PatchRadius; y++)
			{
				for (int x = -Parameters.PatchRadius; x <= Parameters.PatchRadius; x++)
				{
					int cxj = Math.Max(Math.Min(cx + x, Bounds.Width - 1), 0);
					int cyi = Math.Max(Math.Min(cy + y, Bounds.Height - 1), 0);
					int pxj = Math.Max(Math.Min(px + x, Bounds.Width - 1), 0);
					int pyi = Math.Max(Math.Min(py + y, Bounds.Height - 1), 0);

					byte centerIntesity = GetIntensity(Coords2Ptr(inPtr, cxj, cyi));
					byte currentIntensity = GetIntensity(Coords2Ptr(inPtr, pxj, pyi));

					sum += Math.Exp(
						-Math.Pow((currentIntensity - centerIntesity) / Parameters.HParam, 2)
					);
				}
			}
			return sum / patchArea;
		}
	}
}
