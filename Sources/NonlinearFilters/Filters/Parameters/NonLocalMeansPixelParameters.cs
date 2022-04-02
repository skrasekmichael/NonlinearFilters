namespace NonlinearFilters.Filters.Parameters
{
	public class NonLocalMeansPixelParameters : BaseFilterParameters
	{
		public int PatchRadius { get; set; }
		public int WindowRadius { get; set; }
		public double HParam { get; set; }

		public NonLocalMeansPixelParameters(int patchRadius = 1, int windowRadius = 7, double hParam = 0)
		{
			PatchRadius = patchRadius;
			WindowRadius = windowRadius;
			HParam = hParam;
		}

		public static NonLocalMeansPixelParameters FromSigma(double sigma)
		{
			//src: https://www.ipol.im/pub/art/2011/bcm_nlm/article.pdf
			return sigma switch
			{
				<= 15 => new(1, 10, 0.4 * sigma),
				<= 30 => new(2, 10, 0.4 * sigma),
				<= 45 => new(3, 17, 0.35 * sigma),
				<= 75 => new(4, 17, 0.35 * sigma),
				<= 100 => new(5, 17, 0.30 * sigma),
				_ => throw new NotImplementedException()
			};
		}
	}
}
