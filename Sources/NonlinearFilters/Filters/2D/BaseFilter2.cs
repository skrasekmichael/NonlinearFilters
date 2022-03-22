using NonlinearFilters.Filters;
using NonlinearFilters.Filters.Interfaces;
using NonlinearFilters.Filters.Parameters;
using OpenTK.Mathematics;
using System.Drawing;
using System.Drawing.Imaging;

namespace NonlinearFilters.Filters2D
{
	public abstract class BaseFilter2<TParameters> : BaseFilter<TParameters>, IFilter2 where TParameters : BaseFilterParameters
	{
		public Size Bounds { get; }
		protected Bitmap TargetBmp { get; }

		public BaseFilter2(ref Bitmap input, TParameters parameters) : base(parameters, 100.0 / (input.Width * input.Height))
		{
			TargetBmp = input;
			Bounds = new Size(TargetBmp.Width, TargetBmp.Height);
		}

		public abstract Bitmap ApplyFilter(int cpuCount = 1);

		protected unsafe Bitmap FilterArea(int cpuCount, Action<Rectangle, IntPtr, IntPtr, int> filterWindow)
		{
			cpuCount = Math.Clamp(cpuCount, 1, Environment.ProcessorCount);
			doneCounts = new int[cpuCount];

			if (!Initalized)
				Initalize();

			var bounds = new Rectangle(Point.Empty, Bounds);
			var output = new Bitmap(Bounds.Width, Bounds.Height);

			if (TargetBmp.PixelFormat != PixelFormat.Format32bppArgb)
				throw new ArgumentException($"{TargetBmp.PixelFormat} is not supported.");

			var inputData = TargetBmp.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			var outputData = output.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			var inPtr = inputData.Scan0;
			var outPtr = outputData.Scan0;

			if (!PreComputed)
			{
				PreCompute(bounds, inPtr, outPtr);
				PreComputed = true;
			}

			if (cpuCount == 1)
			{
				filterWindow(bounds, inPtr, outPtr, 0);
			}
			else
			{
				int last = cpuCount - 1;
				var windows = Split(cpuCount);
				var tasks = new Task[last];

				for (int i = 0; i < last; i++)
				{
					int index = i; //save index into task scope
					tasks[index] = Task.Factory.StartNew(() => filterWindow(windows[index], inPtr, outPtr, index));
				}

				filterWindow(windows[last], inPtr, outPtr, last);
				Task.WaitAll(tasks);
			}

			TargetBmp.UnlockBits(inputData);
			output.UnlockBits(outputData);
			return output;
		}

		protected virtual unsafe void PreCompute(Rectangle bounds, IntPtr inputPtr, IntPtr outputPtr) { }

		protected Rectangle[] Split(int count)
		{
			//splits along Y axis trough image
			int sideSize = Bounds.Height;

			int windowSize = (int)Math.Floor((double)sideSize / count);
			int last = count - 1;

			var windows = new Rectangle[count];
			for (int i = 0; i < last; i++)
				windows[i] = new(0, i * windowSize, Bounds.Width, windowSize);

			int remaining = sideSize % count;
			windows[last] = new(0, last * windowSize, Bounds.Width, windowSize + remaining);

			return windows;
		}

		protected unsafe byte GetIntensity(byte* ptr) => *ptr;

		protected unsafe Vector4i GetColor(byte* ptr)
		{
			return new(
				*ptr,
				*(ptr + 1),
				*(ptr + 2),
				*(ptr + 3)
			);
		}

		protected unsafe void SetIntensity(byte* ptr, int intensity) => SetColor(ptr, (intensity, intensity, intensity, 255));
		protected unsafe void SetColor(byte* ptr, Vector4i color) => SetColor(ptr, ((byte)color.X, (byte)color.Y, (byte)color.Z, (byte)color.W));
		protected unsafe void SetColor(byte* ptr, (byte R, byte G, byte B, byte A) color)
		{
			*ptr = color.R;
			*(ptr + 1) = color.G;
			*(ptr + 2) = color.B;
			*(ptr + 3) = color.A;
		}

		protected unsafe byte* Coords2Ptr(byte* ptr, int x, int y) => ptr + 4 * (x + y * Bounds.Width);
	}
}
