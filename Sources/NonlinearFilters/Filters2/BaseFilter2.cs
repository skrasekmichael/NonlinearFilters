using NonlinearFilters.Filters2.Parameters;
using OpenTK.Mathematics;
using System.Drawing;
using System.Drawing.Imaging;

namespace NonlinearFilters.Filters2
{
	public delegate void ProgressChanged(double percentage, object sender);

	public abstract class BaseFilter2<TParameters> where TParameters : BaseFilter2Parameters
	{
		public event ProgressChanged? OnProgressChanged;

		public Size Bounds { get; }
		protected Bitmap TargetBmp { get; }
		public TParameters Parameters { get; protected set; }
		protected bool Initalized { get; set; } = false;
		protected bool PreComputed { get; set; } = false;

		protected int[]? doneCounts = null;
		protected readonly double sizeCoeff;

		public BaseFilter2(ref Bitmap input, TParameters parameters)
		{
			TargetBmp = input;
			Parameters = parameters;
			InitalizeParams();

			Bounds = new Size(TargetBmp.Width, TargetBmp.Height);
			sizeCoeff = 100.0 / (Bounds.Width * Bounds.Height);
		}

		public void Initalize()
		{
			InitalizeFilter();
			Initalized = true;
		}

		protected virtual void InitalizeFilter() { }

		public void UpdateParameters(TParameters parameters)
		{
			Parameters = parameters;
			InitalizeParams();
			PreComputed = false;
		}

		protected abstract void InitalizeParams();

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
				return output;

			BitmapData inputData = TargetBmp.LockBits(bounds, ImageLockMode.ReadOnly, TargetBmp.PixelFormat);
			BitmapData outputData = output.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			IntPtr inPtr = inputData.Scan0;
			IntPtr outPtr = outputData.Scan0;

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
				var windows = Split(cpuCount);
				var tasks = new Task[cpuCount - 1];

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

		protected virtual unsafe void PreCompute(Rectangle bounds, IntPtr inputPtr, IntPtr outputPtr) { }

		protected Rectangle[] Split(int count)
		{
			bool isWide = Bounds.Width > Bounds.Height; //vertical/horizontal splits trough image
			int sideSize = isWide ? Bounds.Width : Bounds.Height;

			int windowSize = (int)Math.Floor((double)sideSize / count);
			int last = count - 1;

			var windows = new Rectangle[count];
			for (int i = 0; i < last; i++)
			{
				windows[i] = isWide switch
				{
					true => new(i * windowSize, 0, windowSize, Bounds.Height),
					false => new(0, i * windowSize, Bounds.Width, windowSize)
				};
			}

			int remaining = sideSize % count;
			windows[last] = isWide switch
			{
				true => new(last * windowSize, 0, windowSize + remaining, Bounds.Height),
				false => new(0, last * windowSize, Bounds.Width, windowSize + remaining)
			};

			return windows;
		}

		protected unsafe double GetIntensityD(byte* ptr) => GetIntensity(ptr) / 255.0;
		protected unsafe byte GetIntensity(byte* ptr) => *ptr;

		protected unsafe Vector4d GetColorD(byte* ptr) => (Vector4d)GetColor(ptr) / 255.0;
		protected unsafe Vector4i GetColor(byte* ptr)
		{
			return new(
				*ptr,
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

		protected virtual void UpdateProgress()
		{
			int sum = doneCounts![0];
			for (int i = 1; i < doneCounts.Length; i++)
				sum += doneCounts[i];
			ChangeProgress(sum * sizeCoeff);
		}
	}
}
