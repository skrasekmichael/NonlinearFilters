namespace NonlinearFilters.Filters2.Parameters
{
	public enum ImplementationType { Patchwise, Pixelwise }

	public record NonLocalMeansParameters(
		int PatchRadius,
		int WindowRadius,
		double HParam,
		ImplementationType ImplementationType = ImplementationType.Patchwise) : BaseFilter2Parameters
	{

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
