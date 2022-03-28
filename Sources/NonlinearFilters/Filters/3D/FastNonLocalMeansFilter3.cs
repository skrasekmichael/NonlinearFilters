using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics;
using NonlinearFilters.Volume;
using OpenTK.Mathematics;

namespace NonlinearFilters.Filters3D
{
	public class FastNonLocalMeansFilter3 : BaseFilter3<FastNonLocalMeansParameters>
	{
		private long[,,]? integralImage = null;
		private double inverseParam;

		private readonly IntegralImageCreator integralImageCreator = new();

		public FastNonLocalMeansFilter3(ref VolumetricData input, FastNonLocalMeansParameters parameters) : base(ref input, parameters) { }

		public override VolumetricData ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, FilterBlock);

		protected override void InitalizeParams()
		{
			inverseParam = 1 / Parameters.HParam;
		}

		protected override void InitalizeFilter()
		{
			integralImage = integralImageCreator.Create(Input);
		}

		private unsafe void FilterBlock(Block block, VolumetricData input, VolumetricData output, int index)
		{
			int maxPatchX = input.Size.X - 1;
			int maxPatchY = input.Size.Y - 1;
			int maxPatchZ = input.Size.Z - 1;

			fixed (int* donePtr = doneCounts)
			{
				int* doneIndexPtr = donePtr + index;

				for (int cx = block.X; cx < block.X + block.Width; cx++)
				{
					int windowStartX = Math.Max(cx - Parameters.WindowRadius, 0);
					int windowEndX = Math.Min(cx + Parameters.WindowRadius, input.Size.X);

					int centerPatchStartX = Math.Max(cx - Parameters.PatchRadius, 0) - 1;
					int centerPatchEndX = Math.Min(cx + Parameters.PatchRadius, maxPatchX);

					for (int cy = block.Y; cy < block.Y + block.Height; cy++)
					{
						int windowStartY = Math.Max(cy - Parameters.WindowRadius, 0);
						int windowEndY = Math.Min(cy + Parameters.WindowRadius, input.Size.Y);

						int centerPatchStartY = Math.Max(cy - Parameters.PatchRadius, 0) - 1;
						int centerPatchEndY = Math.Min(cy + Parameters.PatchRadius, maxPatchY);

						for (int cz = block.Z; cz < block.Z + block.Depth; cz++)
						{
							int windowStartZ = Math.Max(cz - Parameters.WindowRadius, 0);
							int windowEndZ = Math.Min(cz + Parameters.WindowRadius, input.Size.Z);

							int centerPatchStartZ = Math.Max(cz - Parameters.PatchRadius, 0) - 1;
							int centerPatchEndZ = Math.Min(cz + Parameters.PatchRadius, maxPatchZ);

							double centerPatch = PatchNeighborhood(centerPatchStartX, centerPatchStartY, centerPatchStartZ, centerPatchEndX, centerPatchEndY, centerPatchEndZ);
							double normalizeFactor = 0;
							double weightedSum = 0;

							for (int x = windowStartX; x < windowEndX; x++)
							{
								int patchStartX = Math.Max(x - Parameters.PatchRadius, 0) - 1;
								int patchEndX = Math.Min(x + Parameters.PatchRadius, maxPatchX);

								for (int y = windowStartY; y < windowEndY; y++)
								{
									int patchStartY = Math.Max(y - Parameters.PatchRadius, 0) - 1;
									int patchEndY = Math.Min(y + Parameters.PatchRadius, maxPatchY);

									for (int z = windowStartZ; z < windowEndZ; z++)
									{
										int patchStartZ = Math.Max(z - Parameters.PatchRadius, 0) - 1;
										int patchEndZ = Math.Min(z + Parameters.PatchRadius, maxPatchZ);

										double currentPatch = PatchNeighborhood(patchStartX, patchStartY, patchStartZ, patchEndX, patchEndY, patchEndZ);
										double gaussianWeightingFunction = Math.Exp(
											-Math.Pow((currentPatch - centerPatch) * inverseParam, 2)
										);

										normalizeFactor += gaussianWeightingFunction;
										weightedSum += input[x, y, z] * gaussianWeightingFunction;
									}
								}
							}

							byte newIntensity = (byte)(weightedSum / normalizeFactor);
							output[cx, cy, cz] = newIntensity;
							(*doneIndexPtr)++;
						}
						UpdateProgress();
					}
				}
			}
		}

		private double PatchNeighborhood(int sx, int sy, int sz, int ex, int ey, int ez)
		{
			var voxelCount = (ex - sx) * (ey - sy) * (ez - sz);

			int sxgz = (sx >> 31) + 1;
			int sygz = (sy >> 31) + 1;
			int szgz = (sz >> 31) + 1;

			sx *= sxgz;
			sy *= sygz;
			sz *= szgz;

			//points on the block (neighborhood around center voxel)
			var A = integralImage![sx, sy, ez] * sxgz * sygz;
			var B = integralImage![ex, sy, ez] * sygz;
			var C = integralImage![sx, ey, ez] * sxgz;
			var D = integralImage![ex, ey, ez];
			var E = integralImage![sx, sy, sz] * sxgz * sygz * szgz;
			var F = integralImage![ex, sy, sz] * sygz * szgz;
			var G = integralImage![sx, ey, sz] * sxgz * szgz;
			var H = integralImage![ex, ey, sz] * szgz;

			var blockVolume = D - H + G - C + F - E + A - B; //block volume
			return (double)blockVolume / voxelCount;
		}
	}
}
