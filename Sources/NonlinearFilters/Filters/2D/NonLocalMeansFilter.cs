using NonlinearFilters.Mathematics.NonLocalMeansWeightingFunction;
using NonlinearFilters.Filters.Parameters;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace NonlinearFilters.Filters2D
{
	/// <summary>
	/// Implementation of 2D non-local means filter with almost no optimizations, this implementation is used for
	/// checking correct result of <see cref="FastNonLocalMeansFilter"/>
	/// </summary>
	public class NonLocalMeansFilter : BaseFilter2<NonLocalMeansParameters>
	{
		private double inverseCoeff;

		private WeightingFunction? weightingFunction;

		/// <summary>
		/// Initializes new instance of the <see cref="NonLocalMeansFilter"/> class.
		/// </summary>
		/// <param name="input">Input image data</param>
		/// <param name="parameters">Filter parameters</param>
		public NonLocalMeansFilter(ref Image<Rgba32> input, NonLocalMeansParameters parameters) : base(ref input, parameters) { }

		protected override void InitalizeParams()
		{
			double inversePatchArea = 1 / Math.Pow(2 * Parameters.PatchRadius + 1, 2);
			double inverseH2 = 1 / (Parameters.HParam * Parameters.HParam);
			inverseCoeff = inversePatchArea * inverseH2;
			Padding = Parameters.PatchRadius + Parameters.WindowRadius;
		}

		public override Image<Rgba32> ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, FilterWindow);

		protected override void ParameterPreCompute(Size size, IntPtr inputPtr, IntPtr outputPtr)
		{
			var patchSide = Parameters.PatchRadius * 2 + 1;
			var patchSize = patchSide * patchSide;
			weightingFunction = Parameters.Samples switch
			{
				> -1 => new SampledWeightingFunction(Parameters, patchSize),
				_ => new WeightingFunction(Parameters, patchSize)
			};
		}

		private unsafe void FilterWindow(Rectangle threadWindow, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			fixed (int* ptrDone = doneCounts)
			{
				int* ptrDoneIndex = ptrDone + index;

				for (int cy = threadWindow.Y; cy < threadWindow.Y + threadWindow.Height; cy++)
				{
					for (int cx = threadWindow.X; cx < threadWindow.X + threadWindow.Width; cx++)
					{
						double normalizeFactor = 0;
						double weightedSum = 0;

						for (int wy = cy - Parameters.WindowRadius; wy <= cy + Parameters.WindowRadius; wy++)
						{
							for (int wx = cx - Parameters.WindowRadius; wx <= cx + Parameters.WindowRadius; wx++)
							{
								var distance = GetPatchDistance(inPtr, wx, wy, cx, cy);
								double weight = weightingFunction!.GetValue(distance);

								normalizeFactor += weight;
								weightedSum += GetIntensity(Coords2Ptr(inPtr, wx, wy)) * weight;
							}
						}

						byte newIntensity = (byte)(weightedSum / normalizeFactor);
						SetIntensity(Coords2Ptr(outPtr, cx, cy), newIntensity);
						(*ptrDoneIndex)++;
					}

					if (IsCanceled) return;
					UpdateProgress();
				}
			}
		}

		private unsafe long GetPatchDistance(byte *inPtr, int px, int py, int cx, int cy)
		{
			long sum = 0;
			for (int y = -Parameters.PatchRadius; y <= Parameters.PatchRadius; y++)
			{
				int cyi = cy + y;
				int pyi = py + y;

				for (int x = -Parameters.PatchRadius; x <= Parameters.PatchRadius; x++)
				{
					int cxj = cx + x;
					int pxj = px + x;

					int diff = GetIntensity(Coords2Ptr(inPtr, pxj, pyi)) - GetIntensity(Coords2Ptr(inPtr, cxj, cyi));
					sum += diff * diff;
				}
			}
			return sum;
		}
	}
}
