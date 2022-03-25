using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NonlinearFilters.Volume
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

		public virtual Image<L8> Render()
		{
			var image = new Image<L8>(Size.X, Size.Y);
			image.ProcessPixelRows(accessor =>
			{
				for (int y = 0; y < accessor.Height; y++)
				{
					var row = accessor.GetRowSpan(y);
					for (int x = 0; x < row.Length; x++)
					{
						byte val = 0;
						for (int z = 0; z < Size.Z; z++)
							val = Math.Max(val, this[x, y, z]);
						row[x] = new L8(val);
					}
				}
			});
			return image;
		}
	}
}
