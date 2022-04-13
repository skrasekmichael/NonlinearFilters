using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics.NonLocalMeansWeightingFunction;
using NonlinearFilters.Mathematics;
using NonlinearFilters.Volume;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace NonlinearFilters.Filters3D
{
	public class FastNonLocalMeansFilter3 : BaseFilter3<NonLocalMeansPatchParameters>
	{
		private long[,,]? integralImage = null;
		private readonly int sizeYZ;

		private BaseWeightingFunction? patchWeightinFunction;
		private readonly IntegralImageCreator integralImageCreator = new();

		public FastNonLocalMeansFilter3(ref VolumetricData input, NonLocalMeansPatchParameters parameters) : base(ref input, parameters)
		{
			sizeYZ = input.Size.Y * input.Size.Z;
		}

		public override VolumetricData ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, FilterBlock);

		protected override void InitalizeParams() { }

		protected override void InitalizeFilter()
		{
			integralImage = integralImageCreator.Create(Input);
		}

		protected override void PreCompute()
		{
			patchWeightinFunction = Parameters.Samples switch
			{
				> -1 => new SampledWeightingFunction(Parameters.HParam, Parameters.Samples),
				_ => new WeightingFunction(Parameters.HParam)
			};
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
										double gaussianWeightingFunction = patchWeightinFunction!.GetValue(currentPatch - centerPatch);

										normalizeFactor += gaussianWeightingFunction;
										weightedSum += input.Data[input.Coords2Index(x, y, z)] * gaussianWeightingFunction;
									}
								}
							}

							byte newIntensity = (byte)(weightedSum / normalizeFactor);
							output.Data[output.Coords2Index(cx, cy, cz)] = newIntensity;
							(*doneIndexPtr)++;
						}

						if (IsCanceled) return;
						UpdateProgress();
					}
				}
			}
		}

		private unsafe double PatchNeighborhood(int sx, int sy, int sz, int ex, int ey, int ez)
		{
			var voxelCount = (ex - sx) * (ey - sy) * (ez - sz);

			int sxgz = 1;
			int sygz = 1;
			int szgz = 1;
			int sxygz = 1;

			if (sx < 0)
			{
				sx = 0;
				sxgz = 0;
				sxygz = 0;
			}

			if (sy < 0)
			{
				sy = 0;
				sygz = 0;
				sxygz = 0;
			}

			if (sz < 0)
			{
				sz = 0;
				szgz = 0;
			}

			//coefficients for 3D array indexing
			int sxYZ = sx * sizeYZ;
			int exYZ = ex * sizeYZ;
			int syZ = sy * Input.Size.Z;
			int eyZ = ey * Input.Size.Z;

			fixed (long* ptr = integralImage)
			{
				//points on the block (neighborhood around center voxel)
				//set to zero if coords out of image (branchless), only start can be out of bounds
				var A = *(ptr + sxYZ + syZ + ez) * sxygz;
				var B = *(ptr + exYZ + syZ + ez) * sygz;
				var C = *(ptr + sxYZ + eyZ + ez) * sxgz;
				var D = *(ptr + exYZ + eyZ + ez);
				var E = *(ptr + sxYZ + syZ + sz) * sxygz * szgz;
				var F = *(ptr + exYZ + syZ + sz) * sygz * szgz;
				var G = *(ptr + sxYZ + eyZ + sz) * sxgz * szgz;
				var H = *(ptr + exYZ + eyZ + sz) * szgz;

				var blockVolume = D - H + G - C + F - E + A - B; //block volume
				return (double)blockVolume / voxelCount;
			}
		}
	}
}
