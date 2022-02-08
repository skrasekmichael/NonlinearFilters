using OpenTK.Mathematics;

namespace NonlinearFilters.Mathematics
{
	public class GaussFunction
	{
		private double expCoeff;

		public void Initalize(double sigma)
		{
			expCoeff = -1 / (2 * sigma * sigma);
		}

		public double Gauss(double x)
		{
			return Math.Exp(expCoeff * x * x);
		}

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
