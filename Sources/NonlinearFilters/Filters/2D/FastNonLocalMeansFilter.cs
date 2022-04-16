using NonlinearFilters.Mathematics.NonLocalMeansWeightingFunction;
using NonlinearFilters.Mathematics;
using NonlinearFilters.Filters.Parameters;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace NonlinearFilters.Filters2D
{
	public class FastNonLocalMeansFilter : BaseFilter2<NonLocalMeansParameters>
	{
		private double[,] weightedSum = null!, normalizationFactor = null!;

		private WeightingFunction? weightingFunction;

		public FastNonLocalMeansFilter(ref Image<Rgba32> input, NonLocalMeansParameters parameters) : base(ref input, parameters) { }

		protected override void InitalizeParams()
		{
			Padding = Parameters.PatchRadius + Parameters.WindowRadius;
		}

		public override Image<Rgba32> ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, FilterWindow);

		protected override unsafe void ParameterPreCompute(Size size, IntPtr inputPtr, IntPtr outputPtr)
		{
			var patchSide = Parameters.PatchRadius * 2 + 1;
			var patchSize = patchSide * patchSide;
			weightingFunction = Parameters.Samples switch
			{
				> -1 => new SampledWeightingFunction(Parameters, patchSize),
				_ => new WeightingFunction(Parameters, patchSize)
			};
		}

		protected override void BeforeFilter(IntPtr inputPtr, IntPtr outputPtr, int cpuCount)
		{
			normalizationFactor = new double[Bounds.Height, Bounds.Width];
			weightedSum = new double[Bounds.Height, Bounds.Width];
		}

		private unsafe void FilterWindow(Rectangle threadWindow, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			var integral = new long[Bounds.Height * Bounds.Width];

			double done = 0;
			int windowDiameter = Parameters.WindowRadius * 2 + 1;
			double next = 1.0 / (windowDiameter * windowDiameter);

			var intStart = Parameters.WindowRadius;
			var intEndX = Bounds.Width - intStart;
			var intEndY = Bounds.Height - intStart;
			var intSW = intStart * Bounds.Width;

			fixed (int* ptrDone = doneCounts)
			fixed (long* ptrInt = integral)
			{
				int* ptrDoneIndex = ptrDone + index;

				for (int wy = -Parameters.WindowRadius; wy <= Parameters.WindowRadius; wy++)
				{
					for (int wx = -Parameters.WindowRadius; wx <= Parameters.WindowRadius; wx++)
					{
						#region calculating integral image

						//first cell
						var diff = *Coords2Ptr(inPtr, intStart, intStart) - *Coords2Ptr(inPtr, intStart + wx, intStart + wy);
						*(ptrInt + intSW + intStart) = diff * diff;

						//first row
						for (int x = intStart + 1; x < intEndX; x++)
						{
							diff = *Coords2Ptr(inPtr, x, intStart) - *Coords2Ptr(inPtr, x + wx, intStart + wy);
							*(ptrInt + intSW + x) = *(ptrInt + intSW + x - 1) + diff * diff;
						}

						for (int y = intStart + 1; y < intEndY; y++)
						{
							//first cell in row (first column)
							diff = *Coords2Ptr(inPtr, intStart, y) - *Coords2Ptr(inPtr, intStart + wx, y + wy);
							*(ptrInt + y * Bounds.Width + intStart) = *(ptrInt + (y - 1) * Bounds.Width + intStart) + diff * diff;

							//rest of cells in row
							var ym1W = (y - 1) * Bounds.Width;
							var yW = y * Bounds.Width;
							for (int x = intStart + 1; x < intEndX; x++)
							{
								diff = *Coords2Ptr(inPtr, x, y) - *Coords2Ptr(inPtr, x + wx, y + wy);
								long A = *(ptrInt + ym1W + x - 1);
								long B = *(ptrInt + ym1W + x);
								long C = *(ptrInt + yW + x - 1);
								*(ptrInt + yW + x) = B + C - A + diff * diff;
							}
						}

						#endregion

						for (int cy = threadWindow.Y; cy < threadWindow.Y + threadWindow.Height; cy++)
						{
							var cymrW = (cy - Parameters.PatchRadius - 1) * Bounds.Width;
							var cyprW = (cy + Parameters.PatchRadius) * Bounds.Width;

							for (int cx = threadWindow.X; cx < threadWindow.X + threadWindow.Width; cx++)
							{
								long D = *(ptrInt + cyprW + cx + Parameters.PatchRadius);
								long B = *(ptrInt + cymrW + cx + Parameters.PatchRadius);
								long A = *(ptrInt + cymrW + cx - Parameters.PatchRadius - 1);
								long C = *(ptrInt + cyprW + cx - Parameters.PatchRadius - 1);
								long distance = D - B - C + A;


								var weight = weightingFunction!.GetValue(distance);

								normalizationFactor[cy, cx] += weight;
								weightedSum[cy, cx] += weight * *Coords2Ptr(inPtr, cx + wx, cy + wy);

								done += next;
								*ptrDoneIndex = (int)done;
							}
						}

						if (IsCanceled) goto cancel;
						UpdateProgress();
					}
				}

			cancel:

				for (int y = threadWindow.Y; y < threadWindow.Y + threadWindow.Height; y++)
				{
					for (int x = threadWindow.X; x < threadWindow.X + threadWindow.Width; x++)
					{
						var intensity = weightedSum[y, x] / normalizationFactor[y, x];
						SetIntensity(Coords2Ptr(outPtr, x, y), (byte)intensity);
					}
				}
			}
		}
	}
}
