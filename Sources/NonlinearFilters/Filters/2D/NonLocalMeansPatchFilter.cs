using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics.NonLocalMeansWeightingFunction;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace NonlinearFilters.Filters2D
{
	public class NonLocalMeansPatchFilter : BaseFilter2<NonLocalMeansPatchParameters>
	{
		private BaseWeightingFunction? patchWeightinFunction;

		public NonLocalMeansPatchFilter(ref Image<Rgba32> input, NonLocalMeansPatchParameters parameters) : base(ref input, parameters) { }

		protected override void InitalizeParams() { }

		public override Image<Rgba32> ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, FilterWindowPatchwise);

		protected override void PreCompute(Size size, IntPtr inputPtr, IntPtr outputPtr)
		{
			patchWeightinFunction = Parameters.Samples switch
			{
				> -1 => new SampledWeightingFunction(Parameters.HParam, Parameters.Samples),
				_ => new WeightingFunction(Parameters.HParam)
			};
		}

		private unsafe void FilterWindowPatchwise(Rectangle threadWindow, IntPtr inputPtr, IntPtr outputPtr, int index)
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
								double gaussianWeightingFunction = patchWeightinFunction!.GetValue(currentPatch - centerPatch);

								normalizeFactor += gaussianWeightingFunction;
								weightedSum += GetIntensity(Coords2Ptr(inPtr, x, y)) * gaussianWeightingFunction;
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
	}
}
