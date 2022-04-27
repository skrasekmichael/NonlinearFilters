using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.CompilerServices;

namespace NonlinearFilters.Volume
{
	/// <summary>
	/// Class representing volumetric data
	/// </summary>
	public partial class VolumetricData
	{
		public VolumeParams Parameters { get; }
		public Vector3i Size => Parameters.Size;
		public byte[] Data { get; }

		private readonly int sizeYZ;

		/// <summary>
		/// Initializes new instance of the <see cref="VolumetricData"/> class.
		/// </summary>
		/// <param name="parameters">Volumetric data parameters</param>
		/// <param name="data">Actual volumetric data</param>
		public VolumetricData(VolumeParams parameters, byte[] data)
		{
			Parameters = parameters;
			Data = data;

			sizeYZ = Size.Y * Size.Z;
		}

		/// <summary>
		/// Initializes new instance of the <see cref="VolumetricData"/> class.
		/// </summary>
		/// <param name="parameters">Volumetric data parameters</param>
		public VolumetricData(VolumeParams parameters) : this(parameters, new byte[parameters.Size.X * parameters.Size.Y * parameters.Size.Z]) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Coords2Index(int x, int y, int z) => x * sizeYZ + y * Size.Z + z;

		public byte this[int x, int y, int z]
		{
			get => Data[Coords2Index(x, y, z)];
			set => Data[Coords2Index(x, y, z)] = value;
		}

		/// <summary>
		/// Creates new instance of the <see cref="VolumetricData"/> class with same parameters.
		/// </summary>
		/// <returns>Volumetric data</returns>
		public VolumetricData Create() => new(Parameters);

		/// <summary>
		/// Simple direct volume rendering
		/// </summary>
		/// <returns>Image with rendered volumetric data</returns>
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
