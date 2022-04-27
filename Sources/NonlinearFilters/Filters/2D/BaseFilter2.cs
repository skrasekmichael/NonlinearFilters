using NonlinearFilters.Filters;
using NonlinearFilters.Filters.Interfaces;
using NonlinearFilters.Filters.Parameters;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using OpenTK.Mathematics;
using System.Runtime.CompilerServices;

namespace NonlinearFilters.Filters2D
{
	/// <summary>
	/// Base class for 2D filters
	/// </summary>
	/// <typeparam name="TParameters">Filter parameters</typeparam>
	public abstract class BaseFilter2<TParameters> : BaseFilter<TParameters>, IFilter2 where TParameters : BaseFilterParameters
	{
		/// <summary>
		/// Size of image
		/// </summary>
		public Size Size { get; }
		/// <summary>
		/// Size of image with padding
		/// </summary>
		public Size Bounds { get; private set; }
		/// <summary>
		/// Input image data
		/// </summary>
		protected Image<Rgba32> Input { get; }

		/// <summary>
		/// Initializes new instance of the <see cref="BaseFilter2{TParameters}"/> class.
		/// </summary>
		/// <param name="input">Input image data</param>
		/// <param name="parameters">Filter parameters</param>
		public BaseFilter2(ref Image<Rgba32> input, TParameters parameters) : base(parameters, 100.0 / (input.Width * input.Height))
		{
			Input = input;
			Size = new Size(Input.Width, Input.Height);
			Bounds = Size;
		}

		/// <summary>
		/// Applies filter on input image
		/// </summary>
		/// <param name="cpuCount">Number of processors for parallel filtering</param>
		/// <returns>Filtered image</returns>
		public abstract Image<Rgba32> ApplyFilter(int cpuCount = 1);

		/// <summary>
		/// Filters input image
		/// </summary>
		/// <param name="cpuCount">Number of processors for parallel filtering</param>
		/// <param name="filterWindow">Delegate for filtering area in image data</param>
		/// <returns>Filtered image</returns>
		/// <exception cref="Exception"></exception>
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

			if (Padding > 0) //if filter require data with padding
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
				//filtering data without padding
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

		/// <summary>
		/// Runs filter on <paramref name="cpuCount"/> threads. Runs synchronously if <paramref name="cpuCount"/> = 1
		/// </summary>
		/// <param name="inPtr">Pointer to input image data</param>
		/// <param name="outPtr">Pointer to output image data</param>
		/// <param name="cpuCount">Number of processors for parallel filtering</param>
		/// <param name="filterWindow">Delegate for filtering area in image data</param>
		private void ParallelFilter(IntPtr inPtr, IntPtr outPtr, int cpuCount, Action<Rectangle, IntPtr, IntPtr, int> filterWindow)
		{
			if (cpuCount == 1)
			{
				filterWindow(new(Padding, Padding, Size.Width, Size.Height), inPtr, outPtr, 0);
			}
			else
			{
				//parallel filtering
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

		/// <summary>
		/// Precomputes algorithm parameters for filtering  (depending on input parameters).
		/// </summary>
		/// <param name="size">Image data size</param>
		/// <param name="inputPtr">Pointer to input image data</param>
		/// <param name="outputPtr">Pointer to output image data</param>
		protected virtual unsafe void ParameterPreCompute(Size size, IntPtr inputPtr, IntPtr outputPtr) { }

		/// <summary>
		/// Runs synchronously before parallel filter.
		/// </summary>
		/// <param name="inputPtr">Pointer to input image data</param>
		/// <param name="outputPtr">Pointer to output image data</param>
		/// <param name="cpuCount">Number of processors for parallel filtering</param>
		protected virtual unsafe void BeforeFilter(IntPtr inputPtr, IntPtr outputPtr, int cpuCount) { }

		/// <summary>
		/// Splits input image data for parallel filtering.
		/// </summary>
		/// <param name="count">Number of windows</param>
		/// <returns>Array of windows representing coordinates and sizes of rectangle in image data</returns>
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

		/// <summary>
		/// Returns intensity of pixel at <paramref name="ptr"/>
		/// </summary>
		/// <param name="ptr">Pointer to pixel in image data</param>
		/// <returns>Intensity of pixel</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected unsafe byte GetIntensity(byte* ptr) => *ptr;

		/// <summary>
		/// Returns color of pixel at <paramref name="ptr"/>
		/// </summary>
		/// <param name="ptr">Pointer to pixel in image data</param>
		/// <returns>Color of pixel in RGBA format</returns>
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

		/// <summary>
		/// Sets intesity of pixel stored at address pointed by <paramref name="ptr"/>
		/// </summary>
		/// <param name="ptr">Pointer to pixel in image</param>
		/// <param name="intensity">New intensity</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected unsafe void SetIntensity(byte* ptr, int intensity) => SetColor(ptr, (byte)intensity, (byte)intensity, (byte)intensity, 255);

		/// <summary>
		/// Sets color of pixel stored at address pointed by <paramref name="ptr"/>
		/// </summary>
		/// <param name="ptr">Pointer to pixel in image</param>
		/// <param name="color">New color in RGBA format</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected unsafe void SetColor(byte* ptr, Vector4i color) => SetColor(ptr, (byte)color.X, (byte)color.Y, (byte)color.Z, (byte)color.W);

		/// <summary>
		/// Sets color of pixel stored at address pointed by <paramref name="ptr"/>
		/// </summary>
		/// <param name="ptr">Pointer to pixel in image</param>
		/// <param name="R">Red</param>
		/// <param name="G">Green</param>
		/// <param name="B">Blue</param>
		/// <param name="A">Alpha</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected unsafe void SetColor(byte* ptr, byte R, byte G, byte B, byte A)
		{
			*ptr = R;
			*(ptr + 1) = G;
			*(ptr + 2) = B;
			*(ptr + 3) = A;
		}

		/// <summary>
		/// Transforms [<paramref name="x"/>, <paramref name="y"/>] coordinates into pointer to pixel in image data
		/// </summary>
		/// <param name="ptr">Pointer to image data</param>
		/// <param name="x">X</param>
		/// <param name="y">Y</param>
		/// <returns>Pointer at pixel in image data</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected unsafe byte* Coords2Ptr(byte* ptr, int x, int y) => ptr + 4 * (y * Bounds.Width + x);

		/// <summary>
		/// Transforms <paramref name="index"/> into pointer to pixel in image data
		/// </summary>
		/// <param name="ptr">Pointer to image data</param>
		/// <param name="index">Index of pixel</param>
		/// <returns>Pointer at pixel in image data</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected unsafe byte* Coords2Ptr(byte* ptr, int index) => ptr + 4 * index;
	}
}
