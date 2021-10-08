using OpenTK.Mathematics;
using System;

namespace NonlinearFilters.Mathematics
{
	public static class MathExt
	{
		public static double Gauss(double x, double sigma) => Math.Exp(-0.5 * (x * x / (sigma * sigma))) / (sigma * Math.Sqrt(2 * Math.PI));

		public static Vector4d Gauss(Vector4d v, double sigma)
		{
			double a = (sigma * Math.Sqrt(2 * Math.PI));
			double sigma2 = sigma * sigma;
			Vector4d v2 = v * v;
			Vector4d u = new(
				Math.Exp(-0.5 * (v2.X / sigma2)),
				Math.Exp(-0.5 * (v2.Y / sigma2)),
				Math.Exp(-0.5 * (v2.Z / sigma2)),
				Math.Exp(-0.5 * (v2.W / sigma2))
			);
			return u / a;
		}
	}
}
