namespace NonlinearFilters.Filters.Parameters
{
	public record BilateralParameters(
		double SpaceSigma,
		double RangeSigma) : BaseFilterParameters
	{
	}
}
