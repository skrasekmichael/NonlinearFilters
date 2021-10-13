using NonlinearFilters.Mathematics;
using OpenTK.Mathematics;
using System;
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
		private int[]? diffX;

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
						Vector2i pos = new(x, y);
						float val = (float)gaussFunction.Gauss(pos.EuclideanLength);

						int i1 = Coords2AreaIndex(radius + x, radius + y);
						int i2 = Coords2AreaIndex(radius - x, radius + y);
						int i3 = Coords2AreaIndex(radius + x, radius - y);
						int i4 = Coords2AreaIndex(radius - x, radius - y);

						spaceGauss[i1] = spaceGauss[i2] = spaceGauss[i3] = spaceGauss[i4] = val;
					}
				}
			}

			//precompute circle area of spatial gauss function
			diffX = new int[radius * 2];
			for (int y = 0; y < radius; y++)
			{
				int diff = (int)Math.Sqrt(radius2 - y * y);
				diffX[radius - 1 - y] = diffX[radius - 1 + y] = diff;
			}
		}

		private unsafe void FilterWindow(Rectangle window, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			for (int py = window.Y; py < window.Y + window.Height; py++)
			{
				for (int px = window.X; px < window.X + window.Width; px++)
				{
					int starty = Math.Max(py - radius + 1, 0);
					int endy = Math.Min(py + radius, Bounds.Height);
					int centerIntensity = GetIntensity(Coords2Ptr(inPtr, px, py));
					int len = endy - starty;

					int cx = radius - px;
					int cy = radius - py;
					int ci = 256 - centerIntensity;

					float sum = 0, wp = 0;
					for (int i = 0; i < len; i++)
					{
						int diff = diffX![i];
						int startx = Math.Max(px - diff + 1, 0);
						int endx = Math.Min(px + diff, Bounds.Width);
						int y = starty + i;

						for (int x = startx; x < endx; x++)
						{
							int intensity = GetIntensity(Coords2Ptr(inPtr, x, y));

							float gs = spaceGauss![Coords2AreaIndex(cx + x, cy + y)];
							float fr = rangeGauss![ci + intensity];

							float w = gs * fr;
							sum += w * intensity;
							wp += w;
						}
					}

					int newIntensity = (int)(sum / wp);
					SetIntensity(Coords2Ptr(outPtr, px, py), newIntensity);

					done![index]++;
				}
				UpdateProgress();
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
