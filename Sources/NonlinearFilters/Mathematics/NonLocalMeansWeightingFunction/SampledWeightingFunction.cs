using NonlinearFilters.Filters.Parameters;

namespace NonlinearFilters.Mathematics.NonLocalMeansWeightingFunction
{
	/// <summary>
	/// Sampled weighting function for non-local means filter
	/// (<seealso cref="Filters2D.NonLocalMeansFilter"/>,
	/// <seealso cref="Filters2D.FastNonLocalMeansFilter"/>,
	/// <seealso cref="Filters3D.NonLocalMeansFilter3"/>,
	/// <seealso cref="Filters3D.FastNonLocalMeansFilter3"/>)
	/// </summary>
	public class SampledWeightingFunction : WeightingFunction
	{
		private readonly double[] sampledWeightingFunction;
		private readonly double normCoeff;

		/// <summary>
		/// Initializes new instance of the <see cref="SampledWeightingFunction"/> class.
		/// </summary>
		/// <param name="parameters">Filter parameters</param>
		/// <param name="patchSize">Size/Volume of patch</param>
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

		/// <summary>
		/// Get nearest value of weighting function
		/// </summary>
		/// <param name="patch">Squared Euclidean distance between patches</param>
		/// <returns>Sampled value of function</returns>
		public override double GetValue(long patch)
		{
			//<0;max> => <0; samples> 
			int index = (int)(patch * normCoeff);
			return sampledWeightingFunction[index];
		}
	}
}
