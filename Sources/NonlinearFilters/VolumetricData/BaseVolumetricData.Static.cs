namespace NonlinearFilters.VolumetricData
{
	public partial class BaseVolumetricData
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
