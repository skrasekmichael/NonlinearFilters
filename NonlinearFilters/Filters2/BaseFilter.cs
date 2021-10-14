using OpenTK.Mathematics;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace NonlinearFilters.Filters2
{
	public delegate void ProgressChanged(double percentage, object sender);

	public abstract class BaseFilter
	{
		public event ProgressChanged? OnProgressChanged;

		public Size Bounds { get; }

		protected Bitmap TargetBmp { get; }

		public BaseFilter(ref Bitmap input)
		{
			TargetBmp = input;
			Bounds = new Size(TargetBmp.Width, TargetBmp.Height);
		}

		protected unsafe Bitmap FilterArea(int cpuCount, Action<Rectangle, IntPtr, IntPtr, int> filterWindow)
		{
			Rectangle bounds = new(Point.Empty, Bounds);
			Bitmap output = new(Bounds.Width, Bounds.Height);

			if (TargetBmp.PixelFormat != PixelFormat.Format32bppArgb)
				return output;

			BitmapData inputData = TargetBmp.LockBits(bounds, ImageLockMode.ReadOnly, TargetBmp.PixelFormat);
			BitmapData outputData = output.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			IntPtr inPtr = inputData.Scan0;
			IntPtr outPtr = outputData.Scan0;

			cpuCount = Math.Clamp(cpuCount, 1, Environment.ProcessorCount);

			if (cpuCount == 1)
			{
				filterWindow(bounds, inPtr, outPtr, 0);
			}
			else
			{
				Rectangle[] windows = Split(cpuCount);

				Task[] tasks = new Task[cpuCount - 1];
				for (int i = 0; i < cpuCount - 1; i++)
				{
					int index = i; //save index into task scope
					tasks[index] = Task.Factory.StartNew(() => filterWindow(windows[index], inPtr, outPtr, index));
				}

				filterWindow(windows[cpuCount - 1], inPtr, outPtr, cpuCount - 1);
				Task.WaitAll(tasks);
			}

			TargetBmp.UnlockBits(inputData);
			output.UnlockBits(outputData);
			return output;
		}

		public abstract Bitmap ApplyFilter(int cpuCount = 1, bool isGrayScale = true);

		protected Rectangle[] Split(int count)
		{
			bool wh = Bounds.Width > Bounds.Height; //vertical/horizontal splits trough image
			int size = wh ? Bounds.Width : Bounds.Height;
			int len = (int)Math.Floor((double)size / count);
			int last = count - 1;

			Rectangle[] splits = new Rectangle[count];
			for (int i = 0; i < count - 1; i++)
				splits[i] = wh ? new(i * len, 0, len, Bounds.Height) : new(0, i * len, Bounds.Width, len);

			splits[last] = wh ? new(last * len, 0, len + (size % count), Bounds.Height) : new(0, last * len, Bounds.Width, len + (size % count));
			return splits;
		}

		protected unsafe double GetIntensityD(byte* ptr) => GetIntensity(ptr) / 255.0;
		protected unsafe byte GetIntensity(byte* ptr) => *ptr;

		protected unsafe Vector4d GetColorD(byte* ptr) => (Vector4d)GetColor(ptr) / 255.0;
		protected unsafe Vector4i GetColor(byte* ptr)
		{
			return new(
				*(ptr),
				*(ptr + 1),
				*(ptr + 2),
				*(ptr + 3)
			);
		}

		protected unsafe void SetIntensity(byte* ptr, double intensity) => SetIntensity(ptr, (int)(intensity * 255));
		protected unsafe void SetIntensity(byte* ptr, int intensity) => SetColor(ptr, (intensity, intensity, intensity, 255));
		protected unsafe void SetColor(byte* ptr, Vector4d color) => SetColor(ptr, (Vector4i)(color * 255.0));
		protected unsafe void SetColor(byte* ptr, Vector4i color) => SetColor(ptr, ((byte)color.X, (byte)color.Y, (byte)color.Z, (byte)color.W));
		protected unsafe void SetColor(byte* ptr, (byte R, byte G, byte B, byte A) color)
		{
			*ptr = color.R;
			*(ptr + 1) = color.G;
			*(ptr + 2) = color.B;
			*(ptr + 3) = color.A;
		}

		protected unsafe byte* Coords2Ptr(byte *ptr, Vector2i coords) => ptr + 4 * (coords.X + coords.Y * Bounds.Width);

		protected unsafe byte* Coords2Ptr(byte* ptr, int x, int y) => ptr + 4 * (x + y * Bounds.Width);

		protected void ChangeProgress(double percentage) => OnProgressChanged?.Invoke(percentage, this);
	}
}
