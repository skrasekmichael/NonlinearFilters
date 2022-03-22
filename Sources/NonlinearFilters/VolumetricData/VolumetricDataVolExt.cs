using OpenTK.Mathematics;
using System.Buffers.Binary;
using System.Drawing;

namespace NonlinearFilters.VolumetricData
{
	public class VolumetricDataVolExt : BaseVolumetricData
	{
		public class FileHandler : BaseFileHandler
		{
			private const int sizeBorderRatio = 4 * intSize + 3 * floatSize;

			public override BaseVolumetricData Load(string path)
			{
				using var stream = File.OpenRead(path);
				using var br = new BinaryReader(stream);

				var span = br.ReadBytes(sizeBorderRatio).AsSpan();

				int sizeX = BinaryPrimitives.ReadInt32BigEndian(span);
				int sizeY = BinaryPrimitives.ReadInt32BigEndian(span[intSize..]);
				int sizeZ = BinaryPrimitives.ReadInt32BigEndian(span[(intSize * 2)..]);

				int border = BinaryPrimitives.ReadInt32BigEndian(span[(intSize * 3)..]);

				float ratioX = BinaryPrimitives.ReadSingleBigEndian(span[(intSize * 4)..]);
				float ratioY = BinaryPrimitives.ReadSingleBigEndian(span[(intSize * 4 + floatSize)..]);
				float ratioZ = BinaryPrimitives.ReadSingleBigEndian(span[(intSize * 4 + floatSize * 2)..]);

				var data = br.ReadBytes(sizeX * sizeY * sizeZ);
				return new VolumetricDataVolExt(new(sizeX, sizeY, sizeZ), new(ratioX, ratioY, ratioZ), border, data);
			}

			public override void Save(BaseVolumetricData data, string path)
			{
				using var stream = File.OpenWrite(path);
				using var bw = new BinaryWriter(stream);

				Span<byte> span = stackalloc byte[sizeBorderRatio];

				BinaryPrimitives.WriteInt32BigEndian(span, data.Size.X);
				BinaryPrimitives.WriteInt32BigEndian(span[intSize..], data.Size.Y);
				BinaryPrimitives.WriteInt32BigEndian(span[(intSize * 2)..], data.Size.Z);

				BinaryPrimitives.WriteInt32BigEndian(span[(intSize * 3)..], data.Border);

				BinaryPrimitives.WriteSingleBigEndian(span[(intSize * 4)..], (float)data.Ratio.X);
				BinaryPrimitives.WriteSingleBigEndian(span[(intSize * 4 + floatSize)..], (float)data.Ratio.Y);
				BinaryPrimitives.WriteSingleBigEndian(span[(intSize * 4 + floatSize * 2)..], (float)data.Ratio.Z);

				bw.Write(span);
				bw.Write(data.Data);
			}
		}

		private readonly int sizeYZ;

		public VolumetricDataVolExt(Vector3i size, Vector3d ratio, int border) : base(size, ratio, border)
		{
			sizeYZ = size.Y * size.Z;
		}

		public VolumetricDataVolExt(Vector3i size, Vector3d ratio, int border, byte[] data) : base(size, ratio, border, data)
		{
			sizeYZ = size.Y * size.Z;
		}

		public override BaseVolumetricData Create() => new VolumetricDataVolExt(Size, Ratio, Border);

		public override int Coords2Index(int x, int y, int z) => x * sizeYZ + y * Size.Z + z;

		public override Bitmap Render()
		{
			var bmp = base.Render();
			bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
			return bmp;
		}
	}
}
