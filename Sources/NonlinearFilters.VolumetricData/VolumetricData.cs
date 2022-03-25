using OpenTK.Mathematics;
using System.Drawing.Imaging;
using System.Drawing;

namespace NonlinearFilters.VolumetricData
{
	public partial class VolumetricData
	{
		public VolumeParams Parameters { get; }
		public Vector3i Size => Parameters.Size;
		public byte[] Data { get; }

		private readonly int sizeYZ;

		public VolumetricData(VolumeParams parameters, byte[] data)
		{
			Parameters = parameters;
			Data = data;

			sizeYZ = Size.Y * Size.Z;
		}

		public VolumetricData(VolumeParams parameters) : this(parameters, new byte[parameters.Size.X * parameters.Size.Y * parameters.Size.Z]) { }

		public virtual int Coords2Index(int x, int y, int z) => x * sizeYZ + y * Size.Z + z;

		public byte this[int x, int y, int z]
		{
			get => Data[Coords2Index(x, y, z)];
			set => Data[Coords2Index(x, y, z)] = value;
		}

		public VolumetricData Create() => new(Parameters);

		public virtual unsafe Bitmap Render()
		{
			var bmp = new Bitmap(Size.X, Size.Y);

			var data = bmp.LockBits(new(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

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
