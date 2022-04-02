using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics.NonLocalMeansWeightingFunction;
using NonlinearFilters.Mathematics;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace NonlinearFilters.Filters2D
{
	public class FastNonLocalMeansFilter : BaseFilter2<NonLocalMeansPatchParameters>
	{
		private long[,]? integralImage = null;

		private BaseWeightingFunction? patchWeightinFunction;
		private readonly IntegralImageCreator integralImageCreator = new();

		public FastNonLocalMeansFilter(ref Image<Rgba32> input, NonLocalMeansPatchParameters parameters) : base(ref input, parameters) { }

		protected override void InitalizeParams() { }

		public override Image<Rgba32> ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, FilterWindow);

		protected override void PreCompute(Size size, IntPtr inputPtr, IntPtr outputPtr)
		{
			patchWeightinFunction = Parameters.Samples switch
			{
				> -1 => new SampledWeightingFunction(Parameters.HParam, Parameters.Samples),
				_ => new WeightingFunction(Parameters.HParam)
			};
		}

		private unsafe void FilterWindow(Rectangle threadWindow, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			int patchMaxWidth = Bounds.Width - 1;
			int patchMaxHeight = Bounds.Height - 1;

			int threadWindowNewLine = (Bounds.Width - threadWindow.Width) * 4;
			byte* threadWindowPtrOut = Coords2Ptr(outPtr, threadWindow.X, threadWindow.Y);

			fixed (int* ptrDone = doneCounts)
			{
				int* ptrDoneIndex = ptrDone + index;

				for (int py = threadWindow.Y; py < threadWindow.Y + threadWindow.Height; py++)
				{
					int starty = Math.Max(py - Parameters.WindowRadius, 0);
					int endy = Math.Min(py + Parameters.WindowRadius, Bounds.Height);

					int centerPatchStartY = Math.Max(py - Parameters.PatchRadius, 0) - 1;
					int centerPatchEndY = Math.Min(py + Parameters.PatchRadius, patchMaxHeight);

					for (int px = threadWindow.X; px < threadWindow.X + threadWindow.Width; px++)
					{
						int startx = Math.Max(px - Parameters.WindowRadius, 0);
						int endx = Math.Min(px + Parameters.WindowRadius, Bounds.Width);

						int centerPatchStartX = Math.Max(px - Parameters.PatchRadius, 0) - 1;
						int centerPatchEndX = Math.Min(px + Parameters.PatchRadius, patchMaxWidth);

						double centerPatch = PatchNeighborhood(centerPatchStartX, centerPatchStartY, centerPatchEndX, centerPatchEndY);
						double normalizeFactor = 0;
						double weightedSum = 0;

						byte* windowPtrIn = Coords2Ptr(inPtr, startx, starty);
						int windowNewLine = (Bounds.Width - (endx - startx)) * 4;

						for (int y = starty; y < endy; y++)
						{
							int patchStartY = Math.Max(y - Parameters.PatchRadius, 0) - 1;
							int patchEndY = Math.Min(y + Parameters.PatchRadius, patchMaxHeight);

							for (int x = startx; x < endx; x++)
							{
								int patchStartX = Math.Max(x - Parameters.PatchRadius, 0) - 1;
								int patchEndX = Math.Min(x + Parameters.PatchRadius, patchMaxWidth);

								double currentPatch = PatchNeighborhood(patchStartX, patchStartY, patchEndX, patchEndY);
								double gaussianWeightingFunction = patchWeightinFunction!.GetValue(currentPatch - centerPatch);

								normalizeFactor += gaussianWeightingFunction;
								weightedSum += *windowPtrIn * gaussianWeightingFunction;
								windowPtrIn += 4;
							}
							windowPtrIn += windowNewLine;
						}

						byte newIntensity = (byte)(weightedSum / normalizeFactor);
						SetIntensity(threadWindowPtrOut, newIntensity);

						threadWindowPtrOut += 4;
						(*ptrDoneIndex)++;
					}

					threadWindowPtrOut += threadWindowNewLine;
					if (IsCanceled) return;
					UpdateProgress();
				}
			}
		}

		private unsafe double PatchNeighborhood(int sx, int sy, int ex, int ey)
		{
			int pixelCount = (ex - sx) * (ey - sy);

			long A = 0, B = 0, C = 0;
			long D = integralImage![ey, ex];

			if (sy >= 0)
			{
				B = integralImage[sy, ex];
				if (sx >= 0)
				{
					C = integralImage[ey, sx];
					A = integralImage[sy, sx];
				}
			}
			else if (sx >= 0)
				C = integralImage[ey, sx];

			long pixelIntensitySum = D + A - B - C;
			return (double)pixelIntensitySum / pixelCount;
		}

		protected override void InitalizeFilter()
		{
			integralImage = integralImageCreator.CreateGrayScale(Input);
		}
	}
}
