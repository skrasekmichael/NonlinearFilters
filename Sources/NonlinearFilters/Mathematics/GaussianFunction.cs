using OpenTK.Mathematics;

namespace NonlinearFilters.Mathematics
{
	/// <summary>
	/// Class containing logic for Gaussian function
	/// </summary>
	public class GaussianFunction
	{
		private double expCoeff, coeff;

		public void Initalize(double sigma)
		{
			expCoeff = -0.5 / (sigma * sigma);
			coeff = 1 / (sigma * Math.Sqrt(2 * Math.PI));
		}

		/// <summary>
		/// Gaussian function
		/// </summary>
		/// <param name="x">x</param>
		/// <returns>Result from Gaussian function</returns>
		public double Gauss(double x) => Math.Exp(expCoeff * x * x);

		/// <summary>
		/// Gaussian function for squared input
		/// </summary>
		/// <param name="x2">Squared x</param>
		/// <returns>Result from Gaussian function</returns>
		public double Gauss2(double x2) => Math.Exp(expCoeff * x2);
		public double Gauss(double x, double mi) => Math.Exp(expCoeff * (mi - x) * (mi - x));

		public double Normal(double x) => coeff * Gauss(x);
		public double Normal(double x, double mi) => coeff * Gauss(x, mi);

		public Vector4d Gauss(Vector4d v)
		{
			Vector4d v2 = v * v;
			return new Vector4d(
				Math.Exp(expCoeff * v2.X),
				Math.Exp(expCoeff * v2.Y),
				Math.Exp(expCoeff * v2.Z),
				Math.Exp(expCoeff * v2.W)
			);
		}
	}
}
