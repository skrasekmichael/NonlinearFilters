using System.Buffers.Binary;

namespace NonlinearFilters.VolumetricData.VOL
{
	public class VOLFileHandler : BaseFileHandler
	{
		private const int sizeBorderRatio = 4 * intSize + 3 * floatSize;

		public override string Extension => ".vol";

		public override VolumetricData Load(string path)
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
			return new VolumetricData(new(new(sizeX, sizeY, sizeZ), new(ratioX, ratioY, ratioZ), border), data);
		}

		public override void Save(VolumetricData data, string path)
		{
			using var stream = File.OpenWrite(path);
			using var bw = new BinaryWriter(stream);

			Span<byte> span = stackalloc byte[sizeBorderRatio];

			BinaryPrimitives.WriteInt32BigEndian(span, data.Size.X);
			BinaryPrimitives.WriteInt32BigEndian(span[intSize..], data.Size.Y);
			BinaryPrimitives.WriteInt32BigEndian(span[(intSize * 2)..], data.Size.Z);

			BinaryPrimitives.WriteInt32BigEndian(span[(intSize * 3)..], data.Parameters.Border);

			BinaryPrimitives.WriteSingleBigEndian(span[(intSize * 4)..], (float)data.Parameters.Ratio.X);
			BinaryPrimitives.WriteSingleBigEndian(span[(intSize * 4 + floatSize)..], (float)data.Parameters.Ratio.Y);
			BinaryPrimitives.WriteSingleBigEndian(span[(intSize * 4 + floatSize * 2)..], (float)data.Parameters.Ratio.Z);

			bw.Write(span);
			bw.Write(data.Data);
		}
	}
}
