namespace NonlinearFilters.Filters.Parameters
{
	public enum ImplementationType
	{
		Pixelwise = 0,
		Patchwise = 1
	}

	public class NonLocalMeansParameters : BaseFilterParameters
	{
		public int PatchRadius { get; set; }
		public int WindowRadius { get; set; }
		public double HParam { get; set; }
		public ImplementationType ImplementationType { get; set; }

		public NonLocalMeansParameters(
			int patchRadius = 1,
			int windowRadius = 7,
			double hParam = 0,
			ImplementationType implementationType = ImplementationType.Patchwise)
		{
			PatchRadius = patchRadius;
			WindowRadius = windowRadius;
			HParam = hParam;
			ImplementationType = implementationType;
		}

		public static NonLocalMeansParameters FromSigma(double sigma, ImplementationType type)
		{
			//src: https://www.ipol.im/pub/art/2011/bcm_nlm/article.pdf
			return sigma switch
			{
				<= 15 => new(1, 10, 0.4 * sigma, type),
				<= 30 => new(2, 10, 0.4 * sigma, type),
				<= 45 => new(3, 17, 0.35 * sigma, type),
				<= 75 => new(4, 17, 0.35 * sigma, type),
				<= 100 => new(5, 17, 0.30 * sigma, type),
				_ => throw new NotImplementedException()
			};
		}
	}
}
