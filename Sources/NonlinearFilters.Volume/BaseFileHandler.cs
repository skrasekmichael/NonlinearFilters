namespace NonlinearFilters.Volume
{
	/// <summary>
	/// Base file handler for volumetric data, file handlers provides methods for loading and saving volumetric data.
	/// </summary>
	public abstract class BaseFileHandler
	{
		protected const int intSize = sizeof(int);
		protected const int floatSize = sizeof(float);

		public abstract string Extension { get; }

		public abstract VolumetricData Load(string path);
		public abstract void Save(VolumetricData data, string path);
	}
}