using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Runtime.CompilerServices;

namespace NonlinearFilters.Filters2D
{
	public class FastBilateralFilter : BaseFilter2<BilateralParameters>
	{
		private int radius, diameter;

		private double[]? rangeGauss;
		private double[]? spaceGauss;
		private int[]? biasX;

		private readonly GaussianFunction gaussFunction = new();

		public FastBilateralFilter(ref Image<Rgba32> input, BilateralParameters parameters) : base(ref input, parameters) { }

		protected override void InitalizeParams()
		{
			radius = Parameters.GetRadius();
			diameter = 2 * radius + 1;
			Padding = radius;

			rangeGauss = null;
			spaceGauss = null;
			biasX = null;
		}

		public override Image<Rgba32> ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, FilterWindow);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int Coords2AreaIndex(int x, int y) => (radius - y) * diameter + radius - x;

		protected override unsafe void ParameterPreCompute(Size size, IntPtr inputPtr, IntPtr outputPtr)
		{
			int radius2 = radius * radius;

			//precompute gauss function for range sigma
			gaussFunction.Initalize(Parameters.RangeSigma);
			rangeGauss = new double[511];
			rangeGauss[255] = gaussFunction.Gauss(0);
			for (int i = 1; i < 256; i++)
			{
				rangeGauss[255 + i] = rangeGauss[255 - i] = gaussFunction.Gauss(i);
			}

			//precompute gauss function for space sigma
			gaussFunction.Initalize(Parameters.SpaceSigma);
			spaceGauss = new double[diameter * diameter];
			for (int y = 0; y <= radius; y++)
			{
				int y2 = y * y;
				for (int x = 0; x <= radius; x++)
				{
					int z2 = x * x + y2;
					if (z2 < radius2)
					{
						double val = gaussFunction.Gauss(Math.Sqrt(z2));

						int i1 = Coords2AreaIndex(x, y);
						int i2 = Coords2AreaIndex(-x, y);
						int i3 = Coords2AreaIndex(x, -y);
						int i4 = Coords2AreaIndex(-x, -y);

						spaceGauss[i1] = spaceGauss[i2] = spaceGauss[i3] = spaceGauss[i4] = val;
					}
				}
			}

			//precompute circle area of spatial gauss function
			biasX = new int[diameter];
			biasX[radius] = radius;
			for (int y = 1; y < radius; y++)
			{
				int bias = (int)Math.Round(Math.Sqrt(radius2 - y * y));
				biasX[radius - y] = biasX[radius + y] = bias;
			}
		}

		private unsafe void FilterWindow(Rectangle window, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			byte* windowPtrIn = Coords2Ptr(inPtr, window.X, window.Y);
			byte* windowPtrOut = Coords2Ptr(outPtr, window.X, window.Y);

			int windowNewLine = (Bounds.Width - window.Width) * 4;

			fixed (int* biasPtr = biasX)
			fixed (int* donePtr = doneCounts)
			fixed (double* spaceGaussPtr = spaceGauss)
			fixed (double* rangeGaussPtr = rangeGauss)
			{
				int* doneIndexPtr = donePtr + index;
				int* biasIndexPtr = biasPtr + radius;

				for (int py = window.Y; py < window.Y + window.Height; py++)
				{
					int starty = py - radius;
					int endy = py + radius;

					for (int px = window.X; px < window.X + window.Width; px++)
					{
						byte centerIntensity = GetIntensity(windowPtrIn);
						double* rangeGaussIndexPtr = rangeGaussPtr + 255 - centerIntensity;

						double weightedSum = 0, normalizationFactor = 0;
						for (int y = starty; y <= endy; y++)
						{
							int ty = y - py; //transformed y to circle area
							int bias = *(biasIndexPtr + ty);

							int startx = px - bias;
							int endx = px + bias;

							int tsx = startx - px; //transformed startx to circle area
							int spaceGaussIndex = Coords2AreaIndex(tsx, ty);

							byte* ptrStart = Coords2Ptr(inPtr, startx, y);
							byte* ptrStop = Coords2Ptr(inPtr, endx, y);

							for (byte* radiusPtr = ptrStart; radiusPtr <= ptrStop; radiusPtr += 4)
							{
								byte intensity = *radiusPtr;

								double gs = *(spaceGaussPtr + spaceGaussIndex);
								double fr = *(rangeGaussIndexPtr + intensity);

								double weight = gs * fr;
								weightedSum += weight * intensity;
								normalizationFactor += weight;

								spaceGaussIndex--;
							}
						}

						byte newIntensity = (byte)(weightedSum / normalizationFactor);
						SetIntensity(windowPtrOut, newIntensity);

						windowPtrIn += 4;
						windowPtrOut += 4;
						(*doneIndexPtr)++;
					}

					windowPtrIn += windowNewLine;
					windowPtrOut += windowNewLine;
					if (IsCanceled) return;
					UpdateProgress();
				}
			}
		}
	}
}
