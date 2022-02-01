namespace NonlinearFilters.Filters2.Parameters
{
	public record BilateralParameters(
		double SpaceSigma,
		double RangeSigma) : BaseFilter2Parameters
	{
	}
}
