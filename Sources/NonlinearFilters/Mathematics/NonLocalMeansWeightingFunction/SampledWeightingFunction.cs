namespace NonlinearFilters.Mathematics.NonLocalMeansWeightingFunction
{
	public class SampledWeightingFunction : BaseWeightingFunction
	{
		private readonly double[] sampledWeightingFunction;
		private readonly double normCoeff;

		public SampledWeightingFunction(double param, int samples = 511) : base(param)
		{
			sampledWeightingFunction = new double[samples];

			double val = -255;
			double step = 510.0 / samples;
			for (int i = 0; i < samples; i++)
			{
				sampledWeightingFunction[i] = Math.Exp(-Math.Pow(val * inverseParam, 2));
				val += step;
			}

			normCoeff = (samples - 1) / 510.0;
		}

		public override double GetValue(double patchDiff)
		{
			//<-255;255> => <0; samples>
			int index = (int)Math.Round((patchDiff + 255) * normCoeff);
			return sampledWeightingFunction[index];
		}
	}
}
