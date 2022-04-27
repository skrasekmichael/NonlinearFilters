using NonlinearFilters.Filters.Parameters;

namespace NonlinearFilters.Mathematics.NonLocalMeansWeightingFunction;

/// <summary>
/// Weighting function for non-local means filter 
/// (<seealso cref="Filters2D.NonLocalMeansFilter"/>,
/// <seealso cref="Filters2D.FastNonLocalMeansFilter"/>,
/// <seealso cref="Filters3D.NonLocalMeansFilter3"/>,
/// <seealso cref="Filters3D.FastNonLocalMeansFilter3"/>)
/// </summary>
public class WeightingFunction
{
	protected readonly double inverseCoeff;

	/// <summary>
	/// Initializes new instance of the <see cref="WeightingFunction"/> class.
	/// </summary>
	/// <param name="parameters">Filter parameters</param>
	/// <param name="patchSize">Size/Volume of patch</param>
	public WeightingFunction(NonLocalMeansParameters parameters, int patchSize)
	{
		double param2 = parameters.HParam * parameters.HParam;
		inverseCoeff = -1 / (param2 * patchSize);
	}

	/// <summary>
	/// Calculates value using weighting function
	/// </summary>
	/// <param name="patch">Squared Euclidean distance between patches</param>
	/// <returns>Value of function</returns>
	public virtual double GetValue(long patch) => Math.Exp(patch * inverseCoeff);
}
