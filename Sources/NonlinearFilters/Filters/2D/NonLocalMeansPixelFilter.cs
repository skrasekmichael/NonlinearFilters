using NonlinearFilters.Filters.Parameters;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace NonlinearFilters.Filters2D
{
	public class NonLocalMeansPixelFilter : BaseFilter2<NonLocalMeansPixelParameters>
	{
		private double patchArea;
		private readonly double[] pixelWeightingFunction = new double[255 * 2 + 1];

		public NonLocalMeansPixelFilter(ref Image<Rgba32> input, NonLocalMeansPixelParameters parameters) : base(ref input, parameters) { }

		protected override void InitalizeParams()
		{
			patchArea = Math.Pow(2 * Parameters.PatchRadius + 1, 2);
		}

		public override Image<Rgba32> ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, FilterWindowPixelwise);

		protected override void PreCompute(Size size, IntPtr inputPtr, IntPtr outputPtr)
		{
			double inverseParam = 1 / Parameters.HParam;
			for (int i = -255; i <= 255; i++)
				pixelWeightingFunction[i + 255] = Math.Exp(-Math.Pow(i * inverseParam, 2));
		}

		private unsafe void FilterWindowPixelwise(Rectangle threadWindow, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			fixed (int* ptrDone = doneCounts)
			{
				int* ptrDoneIndex = ptrDone + index;

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

						byte newIntensity = (byte)(weightedSum / normalizeFactor);
						SetIntensity(Coords2Ptr(outPtr, px, py), newIntensity);
						(*ptrDoneIndex)++;
					}

					if (IsCanceled) return;
					UpdateProgress();
				}
			}
		}

		private unsafe double PixelNeighborhood(byte *inPtr, int px, int py, int cx, int cy)
		{
			double sum = 0;
			for (int y = -Parameters.PatchRadius; y <= Parameters.PatchRadius; y++)
			{
				for (int x = -Parameters.PatchRadius; x <= Parameters.PatchRadius; x++)
				{
					int cxj = Math.Clamp(cx + x, 0, Bounds.Width - 1);
					int cyi = Math.Clamp(cy + y, 0, Bounds.Height - 1);
					int pxj = Math.Clamp(px + x, 0, Bounds.Width - 1);
					int pyi = Math.Clamp(py + y, 0, Bounds.Height - 1);

					byte centerIntesity = GetIntensity(Coords2Ptr(inPtr, cxj, cyi));
					byte currentIntensity = GetIntensity(Coords2Ptr(inPtr, pxj, pyi));

					sum += pixelWeightingFunction[currentIntensity - centerIntesity + 255];
				}
			}
			return sum / patchArea;
		}
	}
}
