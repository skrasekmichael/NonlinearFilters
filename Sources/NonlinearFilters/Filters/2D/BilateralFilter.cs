using NonlinearFilters.Extensions;
using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using OpenTK.Mathematics;

namespace NonlinearFilters.Filters2D
{
	public class BilateralFilter : BaseFilter2<BilateralParameters>
	{
		private int radius, radius2;

		private readonly GaussianFunction spaceGauss = new();
		private readonly GaussianFunction rangeGauss = new();

		public BilateralFilter(ref Image<Rgba32> input, BilateralParameters parameters) : base(ref input, parameters) { }

		protected override void InitalizeParams()
		{
			radius = Parameters.GetRadius();
			Padding = radius;
			radius2 = radius * radius;

			spaceGauss.Initalize(Parameters.SpaceSigma);
			rangeGauss.Initalize(Parameters.RangeSigma);
		}

		public override Image<Rgba32> ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, Parameters.GrayScale ? FilterWindow : FilterWindowRGB);

		private unsafe void FilterWindow(Rectangle window, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			for (int j = window.Y; j < window.Y + window.Height; j++)
			{
				for (int i = window.X; i < window.X + window.Width; i++)
				{
					int newIntesity = InternalLoop(inPtr, i, j);
					SetIntensity(Coords2Ptr(outPtr, i, j), newIntesity);
					doneCounts![index]++;
				}

				if (IsCanceled) return;
				UpdateProgress();
			}
		}

		private unsafe int InternalLoop(byte* inPtr, int cx, int cy)
		{
			int startx = cx - radius;
			int starty = cy - radius;

			int endx = cx + radius;
			int endy = cy + radius;

			byte centerIntensity = GetIntensity(Coords2Ptr(inPtr, cx, cy));

			double weightedSum = 0, normalizationFactor = 0;
			for (int y = starty; y <= endy; y++)
			{
				int dy = y - cy;
				int dy2 = dy * dy;

				for (int x = startx; x <= endx; x++)
				{
					int dx = x - cx;
					int dz2 = dx * dx + dy2;

					if (dz2 < radius2)
					{
						byte intesity = GetIntensity(Coords2Ptr(inPtr, x, y));
						double gs = spaceGauss.Gauss(Math.Sqrt(dz2));
						double fr = rangeGauss.Gauss(Math.Abs(intesity - centerIntensity));

						double weight = gs * fr;
						weightedSum += weight * intesity;
						normalizationFactor += weight;
					}
				}
			}
			return (int)(weightedSum / normalizationFactor);
		}

		private unsafe void FilterWindowRGB(Rectangle window, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			for (int j = window.Y; j < window.Y + window.Height; j++)
			{
				for (int i = window.X; i < window.X + window.Width; i++)
				{
					Vector4i newColor = InternalLoopRGB(inPtr, i, j);
					SetColor(Coords2Ptr(outPtr, i, j), newColor);
					doneCounts![index]++;
				}

				if (IsCanceled) return;
				UpdateProgress();
			}
		}

		private unsafe Vector4i InternalLoopRGB(byte* inPtr, int cx, int cy)
		{
			int startx = cx - radius;
			int starty = cy - radius;

			int endx = cx + radius;
			int endy = cy + radius;

			Vector4i centerColor = GetColor(Coords2Ptr(inPtr, cx, cy));

			Vector4d weightedSum = Vector4d.Zero, normalizationFactor = Vector4d.Zero;
			for (int y = starty; y <= endy; y++)
			{
				int dy = y - cy;
				int dy2 = dy * dy;

				for (int x = startx; x <= endx; x++)
				{
					int dx = x - cx;
					int dz2 = dx * dx + dy2;

					if (dz2 < radius2)
					{
						Vector4i color = GetColor(Coords2Ptr(inPtr, x, y));
						double gs = spaceGauss.Gauss(Math.Sqrt(dz2));
						Vector4d fr = rangeGauss.Gauss((color - centerColor).Abs());

						Vector4d weight = gs * fr;
						weightedSum += weight * color;
						normalizationFactor += weight;
					}
				}
			}

			return (Vector4i)weightedSum.Div(normalizationFactor);
		}
	}
}
