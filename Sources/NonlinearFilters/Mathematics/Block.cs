using OpenTK.Mathematics;

namespace NonlinearFilters.Mathematics
{
	public record Block(int X, int Y, int Z, int Width, int Height, int Depth)
	{
		public Block(Vector3i loc, Vector3i size) : this(loc.X, loc.Y, loc.Z, size.X, size.Y, size.Z)
		{
		}
	}
}
