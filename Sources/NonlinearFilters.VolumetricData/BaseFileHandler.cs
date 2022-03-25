namespace NonlinearFilters.VolumetricData
{
	public abstract class BaseFileHandler
	{
		protected const int intSize = sizeof(int);
		protected const int floatSize = sizeof(float);

		public abstract string Extension { get; }

		public abstract VolumetricData Load(string path);
		public abstract void Save(VolumetricData data, string path);
	}
}