using NonlinearFilters.Extensions;
using OpenTK.Mathematics;
using System.Buffers.Binary;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace NonlinearFilters.Mathematics
{
	public class VolumetricImage
	{
		private const int intSize = 4;
		private const int floatSize = 4;
		private static readonly int sizeBorderRatio = 4 * intSize + 3 * floatSize;

		public Vector3i Size { get; private set; }
		public Vector3d Ratio { get; private set; }
		public int Border { get; private set; }
		public byte[] Data { get; private set; }

		private readonly int sizeYZ;

		public VolumetricImage(Vector3i size, Vector3d ratio, int border, byte[] data)
		{
			Size = size;
			Ratio = ratio;
			Border = border;
			Data = data;

			sizeYZ = size.Y * size.Z;
		}

		public VolumetricImage(Vector3i Size, Vector3d Ratio, int Border) : this(Size, Ratio, Border, new byte[Size.X * Size.Y * Size.Z]) { }

		public static VolumetricImage FromFile(string path)
		{
			using var stream = File.OpenRead(path);
			using var br = new BinaryReader(stream);

			var span = br.ReadBytes(sizeBorderRatio).AsSpan();

			int sizeX = BinaryPrimitives.ReadInt32BigEndian(span[..intSize]);
			int sizeY = BinaryPrimitives.ReadInt32BigEndian(span.Slice(intSize, intSize));
			int sizeZ = BinaryPrimitives.ReadInt32BigEndian(span.Slice(intSize * 2, intSize));

			int border = BinaryPrimitives.ReadInt32BigEndian(span.Slice(intSize * 3, intSize));

			float ratioX = BinaryPrimitives.ReadSingleBigEndian(span.Slice(intSize * 4, floatSize));
			float ratioY = BinaryPrimitives.ReadSingleBigEndian(span.Slice(intSize * 4 + floatSize, floatSize));
			float ratioZ = BinaryPrimitives.ReadSingleBigEndian(span.Slice(intSize * 4 + floatSize * 2, floatSize));

			var data = br.ReadBytes(sizeX * sizeY * sizeZ);
			return new VolumetricImage(new Vector3i(sizeX, sizeY, sizeZ), new Vector3d(ratioX, ratioY, ratioZ), border, data);
		}

		public unsafe void Save(string path)
		{
			using var stream = File.OpenWrite(path);
			using var bw = new BinaryWriter(stream);

			Span<byte> span = stackalloc byte[sizeBorderRatio];

			BinaryPrimitives.WriteInt32BigEndian(span, Size.X);
			BinaryPrimitives.WriteInt32BigEndian(span[intSize..], Size.Y);
			BinaryPrimitives.WriteInt32BigEndian(span[(intSize * 2)..], Size.Z);

			BinaryPrimitives.WriteInt32BigEndian(span[(intSize * 3)..], Border);

			BinaryPrimitives.WriteSingleBigEndian(span[(intSize * 4)..], (float)Ratio.X);
			BinaryPrimitives.WriteSingleBigEndian(span[(intSize * 4 + floatSize)..], (float)Ratio.Y);
			BinaryPrimitives.WriteSingleBigEndian(span[(intSize * 4 + floatSize * 2)..], (float)Ratio.Z);

			bw.Write(span);
			bw.Write(Data);
		}

		public int Coords2Index(int x, int y, int z) => x * sizeYZ + y * Size.Z + z;

		public byte this[int x, int y, int z]
		{
			get => Data[Coords2Index(x, y, z)];
			set => Data[Coords2Index(x, y, z)] = value;
		}

		public unsafe Bitmap Render()
		{
			var bmp = new Bitmap(Size.Y, Size.X, PixelFormat.Format32bppArgb);

			var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);

			byte* ptrOut = (byte*)data.Scan0.ToPointer();
			for (int x = Size.X - 1; x >= 0; x--)
			{
				for (int y = Size.Y - 1; y>= 0 ; y--)
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
