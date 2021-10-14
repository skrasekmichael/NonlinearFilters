using OpenTK.Mathematics;
using System;

namespace NonlinearFilters.Mathematics
{
	public class GaussFunction
	{
		private double expCoeff, coeff;

		public void Initalize(double sigma)
		{
			coeff = 1 / (sigma * Math.Sqrt(2 * Math.PI));
			expCoeff = -1 / (2 * sigma * sigma);
		}

		public double Gauss(double x)
		{
			return coeff * Math.Exp(expCoeff * x * x);
		}

		public Vector4d Gauss(Vector4d v)
		{
			Vector4d v2 = v * v;
			Vector4d u = new(
				Math.Exp(expCoeff * v2.X),
				Math.Exp(expCoeff * v2.Y),
				Math.Exp(expCoeff * v2.Z),
				Math.Exp(expCoeff * v2.W)
			);
			return coeff * u;
		}
	}
}
