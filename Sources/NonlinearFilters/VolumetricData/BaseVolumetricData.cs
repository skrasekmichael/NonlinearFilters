using OpenTK.Mathematics;
using System.Drawing.Imaging;
using System.Drawing;

namespace NonlinearFilters.VolumetricData
{
	public abstract partial class BaseVolumetricData
	{
		public abstract class BaseFileHandler
		{
			protected const int intSize = sizeof(int);
			protected const int floatSize = sizeof(float);

			public abstract BaseVolumetricData Load(string path);
			public abstract void Save(BaseVolumetricData data, string path);
		}

		public static readonly Lazy<VolumetricDataVolExt.FileHandler> VolFileHandler = new(() => new VolumetricDataVolExt.FileHandler());

		public static readonly string[] VolExtsension = new string[] { ".vol" };
		public static readonly string FileFilter;

		static BaseVolumetricData()
		{
			FileFilter = string.Join('|', VolExtsension.Select(ext => $"{ext}|*{ext}"));
		}

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

		public static bool FileIsVolume(string path)
		{
			var ext = Path.GetExtension(path);
			return VolExtsension.Contains(ext);
		}

		public static BaseVolumetricData FromFile(string path)
		{
			var ext = Path.GetExtension(path);
			return ext switch
			{
				".vol" => VolFileHandler.Value.Load(path),
				_ => throw new ArgumentException($"{ext} is not supported extension to load volumetric data.")
			};
		}

		public static void SaveFile(BaseVolumetricData data, string path)
		{
			var ext = Path.GetExtension(path);
			switch (ext)
			{
				case ".vol":
					VolFileHandler.Value.Save(data, path);
					break;
				default:
					throw new ArgumentException($"{ext} is not supported extension to save volumetric data.");
			};
		}
	}
}
