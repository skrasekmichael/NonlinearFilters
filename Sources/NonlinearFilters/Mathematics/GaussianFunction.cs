using OpenTK.Mathematics;

namespace NonlinearFilters.Mathematics
{
	public class GaussianFunction
	{
		private double expCoeff, coeff;

		public void Initalize(double sigma)
		{
			expCoeff = -0.5 / (sigma * sigma);
			coeff = 1 / (sigma * Math.Sqrt(2 * Math.PI));
		}

		public double Gauss(double x) => Math.Exp(expCoeff * x * x);
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
