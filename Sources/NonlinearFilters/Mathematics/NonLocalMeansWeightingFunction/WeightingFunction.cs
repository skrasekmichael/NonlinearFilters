using NonlinearFilters.Filters.Parameters;

namespace NonlinearFilters.Mathematics.NonLocalMeansWeightingFunction;

public class WeightingFunction
{
	protected readonly double inverseCoeff;

	public WeightingFunction(NonLocalMeansParameters parameters, int patchSize)
	{
		double param2 = parameters.HParam * parameters.HParam;
		inverseCoeff = -1 / (param2 * patchSize);
	}

	public virtual double GetValue(long patch) => Math.Exp(patch * inverseCoeff);
}
