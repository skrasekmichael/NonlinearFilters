namespace NonlinearFilters.Filters.Parameters
{
	public class NonLocalMeansParameters : BaseFilterParameters
	{
		/// <summary>
		/// Defines size of Patch.
		/// </summary>
		public int PatchRadius { get; set; }
		/// <summary>
		/// Defines size of window for searching similar patches.
		/// </summary>
		public int WindowRadius { get; set; }
		/// <summary>
		/// Parameter controlling weighting function.
		/// </summary>
		public double HParam { get; set; }
		/// <summary>
		/// Number of samples for sampling weighting function, weighting function won't be sampled if number is less than 0.
		/// </summary>
		public int Samples { get; set; }

		/// <summary>
		/// Initializes new instance of the <see cref="NonLocalMeansParameters"/> class.
		/// </summary>
		/// <param name="patchRadius">Defines size of Patch.</param>
		/// <param name="windowRadius">Defines size of window for searching similar patches.</param>
		/// <param name="hParam">Parameter controlling weighting function.</param>
		/// <param name="samples">Number of samples for sampling weighting function, weighting function won't be sampled if number is less than 0.</param>
		public NonLocalMeansParameters(int patchRadius = 1, int windowRadius = 7, double hParam = 0, int samples = -1)
		{
			PatchRadius = patchRadius;
			WindowRadius = windowRadius;
			HParam = hParam;
			Samples = samples;
		}
	}
}
