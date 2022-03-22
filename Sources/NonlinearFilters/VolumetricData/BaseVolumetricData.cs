using OpenTK.Mathematics;
using System.Drawing.Imaging;
using System.Drawing;

namespace NonlinearFilters.VolumetricData
{
	public abstract partial class BaseVolumetricData
	{
		public Vector3i Size { get; }
		public Vector3d Ratio { get; }
		public int Border { get; }
		public byte[] Data { get; }

		public BaseVolumetricData(Vector3i size, Vector3d ratio, int border, byte[] data)
		{
			Size = size;
			Ratio = ratio;
			Border = border;
			Data = data;
		}

		public BaseVolumetricData(Vector3i Size, Vector3d Ratio, int Border) : this(Size, Ratio, Border, new byte[Size.X * Size.Y * Size.Z]) { }

		public abstract int Coords2Index(int x, int y, int z);

		public byte this[int x, int y, int z]
		{
			get => Data[Coords2Index(x, y, z)];
			set => Data[Coords2Index(x, y, z)] = value;
		}

		public abstract BaseVolumetricData Create();

		public virtual unsafe Bitmap Render()
		{
			var bmp = new Bitmap(Size.X, Size.Y, PixelFormat.Format32bppArgb);

			var data = bmp.LockBits(new(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);

			byte* ptrOut = (byte*)data.Scan0.ToPointer();
			for (int y = Size.Y - 1; y >= 0; y--)
			{
				for (int x = Size.X - 1; x >= 0; x--)
				{
					byte val = 0;
					for (int z = Size.Z - 1; z >= 0; z--)
						val = Math.Max(val, this[x, y, z]);

					*ptrOut++ = val;
					*ptrOut++ = val;
					*ptrOut++ = val;
					*ptrOut++ = 255;
				}
			}

			bmp.UnlockBits(data);
			return bmp;
		}
	}
}
