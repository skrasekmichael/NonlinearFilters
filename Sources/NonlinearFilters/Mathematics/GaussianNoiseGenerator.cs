using NonlinearFilters.Volume;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace NonlinearFilters.Mathematics
{
	/// <summary>
	/// Simple generator of additive Gaussian noise
	/// </summary>
	public class GaussianNoiseGenerator
	{
		private Random generator = new(420);
		private double sigma;

		public unsafe void Initalize(double sigma, int? generatorSeed = null)
		{
			this.sigma = sigma;
			generator = new Random(generatorSeed ?? DateTime.Now.Millisecond);
		}

		private double GetNext()
		{
			//https://stackoverflow.com/questions/218060/random-gaussian-variables
			double u1 = 1.0 - generator.NextDouble();
			double u2 = 1.0 - generator.NextDouble();
			return sigma * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
		}

		/// <summary>
		/// Generates additive Gaussian noise
		/// </summary>
		/// <param name="img">Input image</param>
		/// <returns>Noisy image</returns>
		/// <exception cref="Exception"></exception>
		public Image<Rgba32> ApplyForBitmap(ref Image<Rgba32> img)
		{
			var noisy = new Image<Rgba32>(img.Width, img.Height);

			if (!img.DangerousTryGetSinglePixelMemory(out var inputMemory) ||
				!noisy.DangerousTryGetSinglePixelMemory(out var outputMemory))
			{
				throw new Exception("Image is too large.");
			}

			var inputHandle = inputMemory.Pin();
			var outputHandle = outputMemory.Pin();

			unsafe
			{
				Apply((byte*)inputHandle.Pointer, (byte*)outputHandle.Pointer, inputMemory.Length, true);
			}

			inputHandle.Dispose();
			outputHandle.Dispose();

			return noisy;
		}

		/// <summary>
		/// Generates additive Gaussian noise
		/// </summary>
		/// <param name="volume">Input volumetric data</param>
		/// <returns>Noisy volumetric data</returns>
		public VolumetricData ApplyForVolume(VolumetricData volume)
		{
			var noisy = volume.Create();
			Apply(volume.Data, noisy.Data);
			return noisy;
		}

		public unsafe void Apply(byte[] input, byte[] output)
		{
			fixed (byte* ptrIn = input)
			fixed (byte* ptrOut = output)
			{
				Apply(ptrIn, ptrOut, input.Length, false);
			}
		}

		private unsafe void Apply(byte* ptrIn, byte* ptrOut, int size, bool grayScale)
		{
			byte* stop = ptrIn + size;

			if (grayScale)
			{
				while (ptrIn < stop)
				{
					double val = GetNext();
					byte newVal = (byte)Math.Clamp(*ptrIn + (int)val, 0, 255);

					*ptrOut++ = newVal;
					*ptrOut++ = newVal;
					*ptrOut++ = newVal;
					*ptrOut++ = 255;

					ptrIn += 4;
				}
			}
			else
			{
				while (ptrIn < stop)
				{
					double val = GetNext();
					*ptrOut = (byte)Math.Clamp(*ptrIn + (int)val, 0, 255);

					ptrIn++;
					ptrOut++;
				}
			}
		}
	}
}
