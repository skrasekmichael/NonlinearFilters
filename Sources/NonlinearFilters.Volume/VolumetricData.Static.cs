using NonlinearFilters.Volume.NRRD;
using NonlinearFilters.Volume.VOL;

namespace NonlinearFilters.Volume
{
	public partial class VolumetricData
	{
		private static readonly BaseFileHandler[] FileHandlers = new BaseFileHandler[]
		{
			new VOLFileHandler(),
			new NRRDFileHandler()
		};

		public static readonly string[] Extsensions;
		public static readonly string FileFilter;

		static VolumetricData()
		{
			Extsensions = FileHandlers.Select(e => e.Extension).ToArray();
			FileFilter = string.Join('|', Extsensions.Select(ext => $"{ext}|*{ext}"));
		}

		public static bool FileIsVolume(string path)
		{
			var ext = Path.GetExtension(path);
			return Extsensions.Contains(ext);
		}

		private static BaseFileHandler GetFileHandler(string path)
		{
			var ext = Path.GetExtension(path);
			int index = Array.IndexOf(Extsensions, ext);
			if (index == -1)
				throw new ArgumentException($"'{ext}' is not supported extension.");
			return FileHandlers[index];
		}

		public static VolumetricData FromFile(string path) => GetFileHandler(path).Load(path);
		public static void SaveFile(VolumetricData data, string path) => GetFileHandler(path).Save(data, path);
	}
}
