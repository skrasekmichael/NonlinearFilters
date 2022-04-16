namespace NonlinearFilters.Filters.Parameters
{
	public class NonLocalMeansParameters : BaseFilterParameters
	{
		public int PatchRadius { get; set; }
		public int WindowRadius { get; set; }
		public double HParam { get; set; }
		public int Samples { get; set; }

		public NonLocalMeansParameters(int patchRadius = 1, int windowRadius = 7, double hParam = 0, int samples = -1)
		{
			PatchRadius = patchRadius;
			WindowRadius = windowRadius;
			HParam = hParam;
			Samples = samples;
		}
	}
}
