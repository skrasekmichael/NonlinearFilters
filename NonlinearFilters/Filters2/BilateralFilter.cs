using NonlinearFilters.Extensions;
using NonlinearFilters.Mathematics;
using OpenTK.Mathematics;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NonlinearFilters.Filters2
{
	public class BilateralFilter : BaseFilter
	{
		public double SpaceParam { get; }
		public double RangeParam { get; }

		private int[]? done;
		private readonly double size;
		private readonly int area;

		public BilateralFilter(ref Bitmap bmp, double spaceParam, double rangeParam) : base(ref bmp)
		{
			SpaceParam = spaceParam;
			RangeParam = rangeParam;
			area = (int)(2.5 * SpaceParam);
			size = 100.0 / (Bounds.Width * Bounds.Height);
		}

		public override unsafe Bitmap ApplyFilter(int cpuCount = 1, bool isGrayScale = true)
		{
			Action<Rectangle, IntPtr, IntPtr, int> FilterArea = isGrayScale ? FilterWindow : FilterWindowRGB;

			Rectangle bounds = new(Point.Empty, Bounds);
			Bitmap output = new(Bounds.Width, Bounds.Height);

			if (TargetBmp.PixelFormat != PixelFormat.Format32bppArgb)
				return output;

			BitmapData inputData = TargetBmp.LockBits(bounds, ImageLockMode.ReadOnly, TargetBmp.PixelFormat);
			BitmapData outputData = output.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			IntPtr inPtr = inputData.Scan0;
			IntPtr outPtr = outputData.Scan0;

			cpuCount = Math.Clamp(cpuCount, 1, Environment.ProcessorCount);
			done = new int[cpuCount];

			if (cpuCount == 1)
			{
				FilterArea(bounds, inPtr, outPtr, 0);
			}
			else
			{
				Rectangle[] windows = Split(cpuCount);

				Task[] tasks = new Task[cpuCount];
				for (int i = 0; i < cpuCount; i++)
				{
					int index = i; //save index into task scope
					tasks[index] = Task.Factory.StartNew(() => FilterArea(windows[index], inPtr, outPtr, index));
				}

				Task.WaitAll(tasks);
			}

			TargetBmp.UnlockBits(inputData);
			output.UnlockBits(outputData);
			return output;
		}

		private unsafe void FilterWindow(Rectangle window, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			for (int j = window.Y; j < window.Y + window.Height; j++)
			{
				for (int i = window.X; i < window.X + window.Width; i++)
				{
					Vector2i coords = new(i, j);

					double color = GetIntensityD(Coords2Ptr(inPtr, coords));
					double inten = InternalLoop(inPtr, coords, color, new(area, area));
					SetIntensity(Coords2Ptr(outPtr, coords), inten);

					done![index]++;
				}
				UpdateProgress();
			}
		}

		private unsafe double InternalLoop(byte* inPtr, Vector2i loc, double locor, Size window)
		{
			int startx = Math.Max(loc.X - window.Width / 2, 0);
			int starty = Math.Max(loc.Y - window.Height / 2, 0);

			int endx = Math.Min(loc.X + window.Width / 2, Bounds.Width);
			int endy = Math.Min(loc.Y + window.Height / 2, Bounds.Height);

			double sum = 0, wp = 0;
			for (int y = starty; y < endy; y++)
			{
				for (int x = startx; x < endx; x++)
				{
					Vector2i coords = new(x, y);
					double color = GetIntensityD(Coords2Ptr(inPtr, coords));
					double gs = MathExt.Gauss((coords - loc).EuclideanLength, SpaceParam);
					double fr = MathExt.Gauss(Math.Abs(color - locor), RangeParam);

					double w = gs * fr;
					wp += w;
					sum += w * color;
				}
			}
			return sum / wp;
		}

		private unsafe void FilterWindowRGB(Rectangle window, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			for (int j = window.Y; j < window.Y + window.Height; j++)
			{
				for (int i = window.X; i < window.X + window.Width; i++)
				{
					Vector2i coords = new(i, j);

					Vector4d color = GetColorD(Coords2Ptr(inPtr, coords));
					Vector4d newColor = InternalLoop(inPtr, coords, color, new(area, area));
					SetColor(Coords2Ptr(outPtr, coords), newColor);

					done![index]++;
				}
				UpdateProgress();
			}
		}

		private unsafe Vector4d InternalLoop(byte* inPtr, Vector2i loc, Vector4d locor, Size window)
		{
			int startx = Math.Max(loc.X - window.Width / 2, 0);
			int starty = Math.Max(loc.Y - window.Height / 2, 0);

			int endx = Math.Min(loc.X + window.Width / 2, Bounds.Width);
			int endy = Math.Min(loc.Y + window.Height / 2, Bounds.Height);

			Vector4d sum = Vector4d.Zero, wp = Vector4d.Zero;
			for (int y = starty; y < endy; y++)
			{
				for (int x = startx; x < endx; x++)
				{
					Vector2i coords = new(x, y);
					Vector4d color = GetColorD(Coords2Ptr(inPtr, coords));
					double gs = MathExt.Gauss((coords - loc).EuclideanLength, SpaceParam);
					Vector4d fr = MathExt.Gauss((color - locor).Abs(), RangeParam);

					Vector4d w = gs * fr;
					wp += w;
					sum += w * color;
				}
			}
			return sum.Div(wp);
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
