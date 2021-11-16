using NonlinearFilters.Mathematics;
using OpenTK.Mathematics;
using System.Diagnostics;
using System.Drawing;

namespace NonlinearFilters.Filters2
{
	public class FastBilateralFilter : BaseFilter
	{
		public double SpaceParam { get; }
		public double RangeParam { get; }

		private int[]? done;
		private readonly double size;
		private readonly int radius, diameter;

		private float[]? rangeGauss;
		private float[]? spaceGauss;
		private int[]? biasX;

		private readonly GaussFunction gaussFunction = new();

		public FastBilateralFilter(ref Bitmap bmp, double spaceParam, double rangeParam) : base(ref bmp)
		{
			SpaceParam = spaceParam;
			RangeParam = rangeParam;
			radius = (int)Math.Floor(2.5 * SpaceParam);
			diameter = radius + 1 + radius;
			size = 100.0 / (Bounds.Width * Bounds.Height);
		}

		public override Bitmap ApplyFilter(int cpuCount = 1, bool isGrayScale = true)
		{
			cpuCount = Math.Clamp(cpuCount, 1, Environment.ProcessorCount);
			done = new int[cpuCount];

			if (rangeGauss == null || spaceGauss == null)
				Initalize();

			return FilterArea(cpuCount, FilterWindow);
		}

		private int Coords2AreaIndex(int x, int y)
		{
			return y * diameter + x;
		}

		public unsafe void Initalize()
		{
			int radius2 = radius * radius;

			//precompute gauss function for range parameter
			gaussFunction.Initalize(RangeParam);
			rangeGauss = new float[512];
			for (int i = 0; i < 256; i++)
			{
				double x = i / 255.0;
				rangeGauss[256 + i] = rangeGauss[255 - i] = (float)gaussFunction.Gauss(x);
			}

			//precompute gauss function for space parameter
			gaussFunction.Initalize(SpaceParam);
			spaceGauss = new float[diameter * diameter];
			for (int y = 0; y < radius; y++)
			{
				for (int x = 0; x < radius; x++)
				{
					if (x * x + y * y < radius2)
					{
						var pos = new Vector2i(x, y);
						float val = (float)gaussFunction.Gauss(pos.EuclideanLength);

						int i1 = Coords2AreaIndex(x, y);
						int i2 = Coords2AreaIndex(radius + x, y);
						int i3 = Coords2AreaIndex(x, radius + y);
						int i4 = Coords2AreaIndex(radius + x, radius + y);

						spaceGauss[i1] = spaceGauss[i2] = spaceGauss[i3] = spaceGauss[i4] = val;
					}
				}
			}

			//precompute circle area of spatial gauss function
			biasX = new int[diameter];
			biasX[radius] = 0;
			for (int y = 0; y < radius; y++)
			{
				int bias = (int)Math.Sqrt(radius2 - y * y);
				biasX[radius - 1 - y] = biasX[radius + 1 + y] = bias;
			}
		}

		private unsafe void FilterWindow(Rectangle window, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			byte* windowPtrIn = Coords2Ptr(inPtr, window.X, window.Y);
			byte* windowPtrOut = Coords2Ptr(outPtr, window.X, window.Y);

			int windowNewLine = (Bounds.Width - window.Width) * 4;

			fixed (int* donePtr = done)
			fixed (int* biasPtr = biasX)
			fixed (float* spaceGaussPtr = spaceGauss)
			fixed (float* rangeGaussPtr = rangeGauss)
			{
				int* doneIndexPtr = donePtr + index;
				for (int py = window.Y; py < window.Y + window.Height; py++)
				{
					for (int px = window.X; px < window.X + window.Width; px++)
					{
						int starty = py - radius;
						int endy = py + radius;
						int len = endy - starty;
						int topEdgeOverflow = 0;

						if (starty < 0)
						{
							len -= -starty;
							topEdgeOverflow = diameter - len;
							starty = 0;
						}

						if (endy > Bounds.Height)
						{
							len -= endy - Bounds.Height;
							endy = Bounds.Height;
						}

						int centerIntensity = *windowPtrIn;
						int ci = 256 - centerIntensity;

						float* rangeGaussIndex = rangeGaussPtr + ci;
						float* spaceGaussIndex = spaceGaussPtr;
						spaceGaussIndex += topEdgeOverflow * diameter;

						int* biasPtrIndex = biasPtr + topEdgeOverflow;
						int y = starty;

						float weightedSum = 0, normilizeFactor = 0;
						for (int i = 0; i < len; i++)
						{
							int centerBias = *(biasPtrIndex + i);
							int edgeBias = radius - centerBias;
							int leftEdgeBias = edgeBias;
							int rightEdgeBias = edgeBias;

							int startx = px - centerBias;
							int endx = px + centerBias;

							if (startx < 0)
							{
								leftEdgeBias = -startx;
								startx = 0;
							}

							if (endx >= Bounds.Width)
							{
								rightEdgeBias = endx - Bounds.Width;
								endx = Bounds.Width;
							}

							spaceGaussIndex += leftEdgeBias;

							byte* ptrStart = Coords2Ptr(inPtr, startx, y++);
							byte* ptrStop = ptrStart + (endx - startx) * 4;
							for (byte* radiusPtrIn = ptrStart; radiusPtrIn < ptrStop; radiusPtrIn += 4)
							{
								int intensity = *radiusPtrIn;

								float gs = *spaceGaussIndex;
								float fr = *(rangeGaussIndex + intensity);

								float w = gs * fr;
								weightedSum += w * intensity;
								normilizeFactor += w;

								spaceGaussIndex++;
							}

							spaceGaussIndex += rightEdgeBias;
						}

						int newIntensity = (int)(weightedSum / normilizeFactor);
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

		private void UpdateProgress()
		{
			int sum = done![0];
			for (int i = 1; i < done.Length; i++)
				sum += done[i];
			ChangeProgress(sum * size);
		}
	}
}
