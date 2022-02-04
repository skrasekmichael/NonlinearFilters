using NonlinearFilters.Filters2.Parameters;
using NonlinearFilters.Mathematics;
using OpenTK.Mathematics;
using System.Drawing;

namespace NonlinearFilters.Filters2
{
	public class FastBilateralFilter : BaseFilter2<BilateralParameters>
	{
		private int radius, diameter;

		private double[]? rangeGauss;
		private double[]? spaceGauss;
		private int[]? biasX;

		private readonly GaussFunction gaussFunction = new();

		public FastBilateralFilter(ref Bitmap input, BilateralParameters parameters) : base(ref input, parameters)
		{
		}

		protected override void InitalizeParams()
		{
			radius = (int)(2.5 * Parameters.SpaceSigma);
			diameter = 2 * radius + 1;

			rangeGauss = null;
			spaceGauss = null;
			biasX = null;
		}

		public override Bitmap ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, FilterWindow);

		private int Coords2AreaIndex(int x, int y) => (radius - y) * diameter + radius - x;

		protected override unsafe void PreCompute(Rectangle bounds, IntPtr inputPtr, IntPtr outputPtr)
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
			biasX[radius] = 0;
			for (int y = 0; y < radius; y++)
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
					for (int px = window.X; px < window.X + window.Width; px++)
					{
						int starty = Math.Max(py - radius, 0);
						int endy = Math.Min(py + radius, Bounds.Height - 1);

						byte centerIntensity = GetIntensity(windowPtrIn);
						double* rangeGaussIndexPtr = rangeGaussPtr + 255 - centerIntensity;

						double sum = 0, wp = 0;
						for (int y = starty; y <= endy; y++)
						{
							int ty = y - py; //transformed y to circle area
							int bias = *(biasIndexPtr + ty);

							int startx = Math.Max(px - bias, 0);
							int endx = Math.Min(px + bias, Bounds.Width - 1);

							int tsx = startx - px; //transformed start x to circle area
							int spaceGaussIndex = Coords2AreaIndex(tsx, ty);

							byte* ptrStart = Coords2Ptr(inPtr, startx, y);
							byte* ptrStop = Coords2Ptr(inPtr, endx, y);

							for (byte* radiusPtr = ptrStart; radiusPtr <= ptrStop; radiusPtr += 4)
							{
								byte intesity = *radiusPtr;

								double gs = *(spaceGaussPtr + spaceGaussIndex);
								double fr = *(rangeGaussIndexPtr + intesity);

								double w = gs * fr;
								sum += w * intesity;
								wp += w;

								spaceGaussIndex--;
							}
						}

						byte newIntensity = (byte)(sum / wp);
						SetIntensity(windowPtrOut, newIntensity);

						windowPtrIn += 4;
						windowPtrOut += 4;
						(*doneIndexPtr)++;
					}

					windowPtrIn += windowNewLine;
					windowPtrOut += windowNewLine;
					UpdateProgress();
				}
			}
		}
	}
}
