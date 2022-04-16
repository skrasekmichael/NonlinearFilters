using OpenTK.Mathematics;

namespace NonlinearFilters.Mathematics
{
	public readonly struct Block
	{
		public readonly int X, Y, Z, Width, Height, Depth;

		public Block(Vector3i loc, Vector3i size)
		{
			X = loc.X;
			Y = loc.Y;
			Z = loc.Z;
			Width = size.X;
			Height = size.Y;
			Depth = size.Z;
		}

		public Block(int x, int y, int z, int width, int height, int depth)
		{
			X = x;
			Y = y;
			Z = z;
			Width = width;
			Height = height;
			Depth = depth;
		}
	}
}
