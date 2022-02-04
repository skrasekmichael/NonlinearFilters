using NonlinearFilters.Extensions;
using NonlinearFilters.Filters2.Parameters;
using NonlinearFilters.Mathematics;
using OpenTK.Mathematics;
using System.Drawing;

namespace NonlinearFilters.Filters2
{
	public class BilateralFilter : BaseFilter2<BilateralParameters>
	{
		private int radius, radius2;

		private readonly GaussFunction spaceGauss = new();
		private readonly GaussFunction rangeGauss = new();

		public BilateralFilter(ref Bitmap input, BilateralParameters parameters) : base(ref input, parameters)
		{
		}

		protected override void InitalizeParams()
		{
			radius = (int)(2.5 * Parameters.SpaceSigma);
			radius2 = radius * radius;

			spaceGauss.Initalize(Parameters.SpaceSigma);
			rangeGauss.Initalize(Parameters.RangeSigma);
		}

		public override Bitmap ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, Parameters.GrayScale ? FilterWindow : FilterWindowRGB);

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
				UpdateProgress();
			}
		}

		private unsafe int InternalLoop(byte* inPtr, int cx, int cy)
		{
			int startx = Math.Max(cx - radius, 0);
			int starty = Math.Max(cy - radius, 0);

			int endx = Math.Min(cx + radius, Bounds.Width - 1);
			int endy = Math.Min(cy + radius, Bounds.Height - 1);

			byte centerIntensity = GetIntensity(Coords2Ptr(inPtr, cx, cy));

			double sum = 0, wp = 0;
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

						double w = gs * fr;
						sum += w * intesity;
						wp += w;
					}
				}
			}
			return (int)(sum / wp);
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
				UpdateProgress();
			}
		}

		private unsafe Vector4i InternalLoopRGB(byte* inPtr, int cx, int cy)
		{
			int startx = Math.Max(cx - radius, 0);
			int starty = Math.Max(cy - radius, 0);

			int endx = Math.Min(cx + radius, Bounds.Width - 1);
			int endy = Math.Min(cy + radius, Bounds.Height - 1);

			Vector4i centerColor = GetColor(Coords2Ptr(inPtr, cx, cy));

			Vector4d sum = Vector4d.Zero, wp = Vector4d.Zero;
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

						Vector4d w = gs * fr;
						wp += w;
						sum += w * color;
					}
				}
			}

			return (Vector4i)sum.Div(wp);
		}
	}
}
