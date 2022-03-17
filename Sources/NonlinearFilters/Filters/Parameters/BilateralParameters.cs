namespace NonlinearFilters.Filters.Parameters
{
	public class BilateralParameters : BaseFilterParameters
	{
		public double SpaceSigma { get; set; }
		public double RangeSigma { get; set; }
		public int Radius { get; set; }

		public BilateralParameters(double spaceSigma, double rangeSigma, int radius = -1)
		{
			SpaceSigma = spaceSigma;
			RangeSigma = rangeSigma;
			Radius = radius;
		}

		public int GetRadius()
		{
			if (Radius < 0)
				return (int)(2.5 * SpaceSigma);
			else
				return Radius;
		}
	}
}
