namespace NonlinearFilters.CLI.Extensions
{
	public static class PathExtensions
	{
		public static void PathEnsureCreated(this string path)
		{
			var dir = Path.GetDirectoryName(path);
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir!);
		}
	}
}
