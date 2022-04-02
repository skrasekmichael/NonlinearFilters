namespace NonlinearFilters.Mathematics.NonLocalMeansWeightingFunction
{
	public class WeightingFunction : BaseWeightingFunction
	{
		public WeightingFunction(double param) : base(param) { }

		public override double GetValue(double patchDiff) => Math.Exp(-Math.Pow(patchDiff * inverseParam, 2));
	}
}
