using NonlinearFilters.Volume;
using System.Drawing;
using System.Drawing.Imaging;

namespace NonlinearFilters.Mathematics
{
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

		public Bitmap ApplyForBitmap(ref Bitmap bmp)
		{
			var noisy = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);

			var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

			var inData = bmp.LockBits(rect, ImageLockMode.ReadOnly, noisy.PixelFormat);
			var outData = noisy.LockBits(rect, ImageLockMode.WriteOnly, noisy.PixelFormat);

			unsafe
			{
				Apply((byte*)inData.Scan0.ToPointer(), (byte*)outData.Scan0.ToPointer(), inData.Stride * inData.Height, true);
			}

			noisy.UnlockBits(outData);
			bmp.UnlockBits(inData);

			return noisy;
		}

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
