namespace NonlinearFilters.Filters.Parameters
{
	/// <summary>
	/// Parameters for <see cref="Filters2D.BilateralFilter"/> and <see cref="Filters3D.BilateralFilter3"/>.
	/// </summary>
	public class BilateralParameters : BaseFilterParameters
	{
		/// <summary>
		/// Standard deviation of spatial Gaussian weighting function.
		/// </summary>
		public double SpaceSigma { get; set; }
		/// <summary>
		/// Standard deviation of range Gaussian weighting function.
		/// </summary>
		public double RangeSigma { get; set; }
		/// <summary>
		/// Defines radius of search area, if radius is less than 0, area is defined as 2.5 * SpaceSigma.
		/// </summary>
		public int Radius { get; set; }

		/// <summary>
		/// Initializes new instance of the <see cref="BilateralParameters"/> class.
		/// </summary>
		/// <param name="spaceSigma">Standard deviation of spatial Gaussian weighting function.</param>
		/// <param name="rangeSigma">Standard deviation of range Gaussian weighting function.</param>
		/// <param name="radius">Defines radius of search area, if radius is less than 0, area is defined as 2.5 * SpaceSigma.</param>
		public BilateralParameters(double spaceSigma, double rangeSigma, int radius = -1)
		{
			SpaceSigma = spaceSigma;
			RangeSigma = rangeSigma;
			Radius = radius;
		}

		/// <summary>
		/// Calculates radius for weighted average.
		/// </summary>
		/// <returns>Radius</returns>
		public int GetRadius()
		{
			if (Radius < 0)
				return (int)(2.5 * SpaceSigma);
			else
				return Radius;
		}
	}
}
