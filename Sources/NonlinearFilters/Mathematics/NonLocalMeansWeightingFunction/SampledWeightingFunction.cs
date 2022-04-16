using NonlinearFilters.Filters.Parameters;

namespace NonlinearFilters.Mathematics.NonLocalMeansWeightingFunction
{
	public class SampledWeightingFunction : WeightingFunction
	{
		private readonly double[] sampledWeightingFunction;
		private readonly double normCoeff;

		public SampledWeightingFunction(NonLocalMeansParameters parameters, int patchSize) : base(parameters, patchSize)
		{
			long max = patchSize * 255 * 255;
			long samples = Math.Min(max, parameters.Samples);

			sampledWeightingFunction = new double[samples];

			double val = 0;
			double step = (double)max / samples;
			for (int i = 0; i < samples; i++)
			{
				sampledWeightingFunction[i] = base.GetValue((long)val);
				val += step;
			}

			normCoeff = 1 / step;
		}

		public override double GetValue(long patch)
		{
			//<0;max> => <0; samples> 
			int index = (int)(patch * normCoeff);
			return sampledWeightingFunction[index];
		}
	}
}
