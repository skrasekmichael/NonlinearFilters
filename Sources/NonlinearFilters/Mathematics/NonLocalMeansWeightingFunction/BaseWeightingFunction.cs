namespace NonlinearFilters.Mathematics.NonLocalMeansWeightingFunction;

public abstract class BaseWeightingFunction
{
	protected readonly double inverseParam;

	public BaseWeightingFunction(double param)
	{
		inverseParam = 1 / param;
	}

	public abstract double GetValue(double patchDiff);
}
