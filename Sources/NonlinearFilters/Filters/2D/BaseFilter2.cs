using NonlinearFilters.Filters;
using NonlinearFilters.Filters.Interfaces;
using NonlinearFilters.Filters.Parameters;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using OpenTK.Mathematics;
using System.Runtime.CompilerServices;

namespace NonlinearFilters.Filters2D
{
	public abstract class BaseFilter2<TParameters> : BaseFilter<TParameters>, IFilter2 where TParameters : BaseFilterParameters
	{
		public Size Size { get; }
		public Size Bounds { get; private set; }
		protected Image<Rgba32> Input { get; }

		public BaseFilter2(ref Image<Rgba32> input, TParameters parameters) : base(parameters, 100.0 / (input.Width * input.Height))
		{
			Input = input;
			Size = new Size(Input.Width, Input.Height);
			Bounds = Size;
		}

		public abstract Image<Rgba32> ApplyFilter(int cpuCount = 1);

		protected unsafe Image<Rgba32> FilterArea(int cpuCount, Action<Rectangle, IntPtr, IntPtr, int> filterWindow)
		{
			cpuCount = Math.Clamp(cpuCount, 1, Environment.ProcessorCount);
			doneCounts = new int[cpuCount];

			if (!Initalized)
				Initalize();

			var output = new Image<Rgba32>(Size.Width, Size.Height);

			if (!Input.DangerousTryGetSinglePixelMemory(out var inputMemory) ||
				!output.DangerousTryGetSinglePixelMemory(out var outputMemory))
			{
				throw new Exception("Image is too large.");
			}

			var inputHandle = inputMemory.Pin();
			var outputHandle = outputMemory.Pin();

			if (Padding > 0)
			{
				Bounds = new Size(Size.Width + 2 * Padding, Size.Height + 2 * Padding);
				var inputData = dataPadder.CreatePadding((byte*)inputHandle.Pointer, Size, Padding);
				var outputData = new byte[Bounds.Width * Bounds.Height * 4];
				fixed (byte* inputPtr = inputData)
				fixed (byte* outputPtr = outputData)
				{
					var inPtr = new IntPtr(inputPtr);
					var outPtr = new IntPtr(outputPtr);

					if (!PreComputed)
					{
						ParameterPreCompute(Bounds, inPtr, outPtr);
						PreComputed = true;
					}

					BeforeFilter(inPtr, outPtr, cpuCount);
					ParallelFilter(inPtr, outPtr, cpuCount, filterWindow);
					dataPadder.RemovePaddding(outputPtr, (byte*)outputHandle.Pointer, Size, Padding);
				}
			}
			else
			{
				var inPtr = new IntPtr(inputHandle.Pointer);
				var outPtr = new IntPtr(outputHandle.Pointer);

				if (!PreComputed)
				{
					ParameterPreCompute(Bounds, inPtr, outPtr);
					PreComputed = true;
				}

				BeforeFilter(inPtr, outPtr, cpuCount);
				ParallelFilter(inPtr, outPtr, cpuCount, filterWindow);
			}

			inputHandle.Dispose();
			outputHandle.Dispose();
			return output;
		}

		private void ParallelFilter(IntPtr inPtr, IntPtr outPtr, int cpuCount, Action<Rectangle, IntPtr, IntPtr, int> filterWindow)
		{
			if (cpuCount == 1)
			{
				filterWindow(new(Padding, Padding, Size.Width, Size.Height), inPtr, outPtr, 0);
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
		}

		protected virtual unsafe void ParameterPreCompute(Size size, IntPtr inputPtr, IntPtr outputPtr) { }

		protected virtual unsafe void BeforeFilter(IntPtr inputPtr, IntPtr outputPtr, int cpuCount) { }

		protected Rectangle[] Split(int count)
		{
			//splits along Y axis trough image
			int sideSize = Size.Height;

			int windowSize = (int)Math.Floor((double)sideSize / count);
			int last = count - 1;

			var windows = new Rectangle[count];
			for (int i = 0; i < last; i++)
				windows[i] = new(Padding, Padding + i * windowSize, Size.Width, windowSize);

			int remaining = sideSize % count;
			windows[last] = new(Padding, Padding + last * windowSize, Size.Width, windowSize + remaining);

			return windows;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected unsafe byte GetIntensity(byte* ptr) => *ptr;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected unsafe Vector4i GetColor(byte* ptr)
		{
			return new(
				*ptr,
				*(ptr + 1),
				*(ptr + 2),
				*(ptr + 3)
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected unsafe void SetIntensity(byte* ptr, int intensity) => SetColor(ptr, (byte)intensity, (byte)intensity, (byte)intensity, 255);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected unsafe void SetColor(byte* ptr, Vector4i color) => SetColor(ptr, (byte)color.X, (byte)color.Y, (byte)color.Z, (byte)color.W);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected unsafe void SetColor(byte* ptr, byte R, byte G, byte B, byte A)
		{
			*ptr = R;
			*(ptr + 1) = G;
			*(ptr + 2) = B;
			*(ptr + 3) = A;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected unsafe byte* Coords2Ptr(byte* ptr, int x, int y) => ptr + 4 * (y * Bounds.Width + x);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected unsafe byte* Coords2Ptr(byte* ptr, int index) => ptr + 4 * index;
	}
}
