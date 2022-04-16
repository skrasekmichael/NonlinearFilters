using NonlinearFilters.Mathematics.NonLocalMeansWeightingFunction;
using NonlinearFilters.Mathematics;
using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Volume;
using System.Runtime.CompilerServices;

namespace NonlinearFilters.Filters3D
{
	public class FastNonLocalMeansFilter3 : BaseFilter3<NonLocalMeansParameters>
	{
		//indexing coeffs
		private int sizeYZ, sizeZ, intStart, intEndX, intEndY, intEndZ, intSYZ, intSZ, reYZ, rsYZ, reZ, rsZ, rs;
		private double[] weightedSum = null!, normalizationFactor = null!;

		private WeightingFunction? weightingFunction;

		public FastNonLocalMeansFilter3(ref VolumetricData input, NonLocalMeansParameters parameters) : base(ref input, parameters) { }

		protected override void InitalizeParams()
		{
			Padding = Parameters.PatchRadius + Parameters.WindowRadius;
		}

		public override VolumetricData ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, FilterBlock);

		protected override void PreCompute()
		{
			var patchSide = Parameters.PatchRadius * 2 + 1;
			var patchSize = patchSide * patchSide * patchSide;
			weightingFunction = Parameters.Samples switch
			{
				> -1 => new SampledWeightingFunction(Parameters, patchSize),
				_ => new WeightingFunction(Parameters, patchSize)
			};
		}

		protected override void BeforeFilter(VolumetricData input, VolumetricData output, int cpuCount)
		{
			normalizationFactor = new double[input.Size.X * input.Size.Y * input.Size.Z];
			weightedSum = new double[input.Size.X * input.Size.Y * input.Size.Z];

			//indexing coeffs
			sizeYZ = input.Size.Y * input.Size.Z;
			sizeZ = input.Size.Z;
			rs = Parameters.PatchRadius + 1;
			reYZ = Parameters.PatchRadius * sizeYZ;
			rsYZ = rs * sizeYZ;
			reZ = Parameters.PatchRadius * sizeZ;
			rsZ = rs * sizeZ;

			//integral bounds coords
			intStart = Parameters.WindowRadius;
			intEndX = input.Size.X - intStart;
			intEndY = input.Size.Y - intStart;
			intEndZ = input.Size.Z - intStart;

			//integral indexing coeffs
			intSYZ = intStart * sizeYZ;
			intSZ = intStart * sizeZ;
		}

		private unsafe void FilterBlock(Block threadBlock, VolumetricData input, VolumetricData output, int index)
		{
			var txe = threadBlock.X + threadBlock.Width;
			var tye = threadBlock.Y + threadBlock.Height;
			var tze = threadBlock.Z + threadBlock.Depth;

			var integral = new long[input.Size.X * input.Size.Y * input.Size.Z];

			double done = 0;
			var windowDiameter = Parameters.WindowRadius * 2 + 1;
			double next = (double)threadBlock.Depth / (windowDiameter * windowDiameter * windowDiameter);

			fixed (int* ptrDone = doneCounts)
			fixed (long* ptrInt = integral)
			fixed (byte* ptrIn = input.Data)
			fixed (double* ptrNF = normalizationFactor, ptrWS = weightedSum)
			{
				int* ptrDoneIndex = ptrDone + index;

				for (int wx = -Parameters.WindowRadius; wx <= Parameters.WindowRadius; wx++)
				{
					//indexing coeffs
					var wxYZ = wx * sizeYZ;

					for (int wy = -Parameters.WindowRadius; wy <= Parameters.WindowRadius; wy++)
					{
						//indexing coeffs
						var wyZ = wy * sizeZ;

						for (int wz = -Parameters.WindowRadius; wz <= Parameters.WindowRadius; wz++)
						{
							CalculateIntegral(ptrInt, ptrIn, wx, wy, wz, wxYZ, wyZ);

							for (int cx = threadBlock.X; cx < txe; cx++)
							{
								//indexing coeffs
								var cxYZ = cx * sizeYZ;
								var sxYZ = cxYZ - rsYZ;
								var exYZ = cxYZ + reYZ;

								for (int cy = threadBlock.Y; cy < tye; cy++)
								{
									//indexing coeffs
									var cyZ = cy * sizeZ;
									var syZ = cyZ - rsZ;
									var eyZ = cyZ + reZ;

									for (int cz = threadBlock.Z; cz < tze; cz++)
									{
										var distance = GetPatchDistance(ptrInt, sxYZ, exYZ, syZ, eyZ,
											cz - rs,
											cz + Parameters.PatchRadius
										);

										var weight = weightingFunction!.GetValue(distance);

										var dataIndex = cxYZ + cyZ + cz;
										*(ptrNF + dataIndex) += weight;
										*(ptrWS + dataIndex) += weight * *(ptrIn + dataIndex + wxYZ + wyZ + wz);
									}
									done += next;
									*ptrDoneIndex = (int)done;
								}
							}

							if (IsCanceled) goto cancel;
							UpdateProgress();
						}
					}
				}

			cancel:

				for (int x = threadBlock.X; x < txe; x++)
				{
					for (int y = threadBlock.Y; y < tye; y++)
					{
						for (int z = threadBlock.Z; z < tze; z++)
						{
							var dataIndex = output.Coords2Index(x, y, z);
							var intensity = *(ptrWS + dataIndex) / *(ptrNF + dataIndex);
							output.Data[dataIndex] = (byte)intensity;
						}
					}
				}
			}
		}

		private unsafe void CalculateIntegral(long* ptrInt, byte *ptrIn, int tx, int ty, int tz, int txYZ, int tyZ)
		{
			//indexing coeffs
			var intSptYZ = intSYZ + txYZ;
			var intSptZ = intSZ + tyZ;

			//first cell
			long diff = *(ptrIn + intSptYZ + intSptZ + intStart + tz) - *(ptrIn + intSYZ + intSZ + intStart);
			*(ptrInt + intSYZ + intSZ + intStart) = diff * diff;

			//z axis
			for (int z = intStart + 1; z < intEndZ; z++)
			{
				diff = *(ptrIn + intSptYZ + intSptZ + z + tz) - *(ptrIn + intSYZ + intSZ + z);
				*(ptrInt + intSYZ + intSZ + z) = *(ptrInt + intSYZ + intSZ + z - 1) + diff * diff;
			}

			for (int y = intStart + 1; y < intEndY; y++)
			{
				//indexing coeffs
				var yZ = y * sizeZ;
				var ym1Z = yZ - sizeZ;
				var yptZ = yZ + tyZ;

				//y axis
				diff = *(ptrIn + intSptYZ + yptZ + intStart + tz) - *(ptrIn + intSYZ + yZ + intStart);
				*(ptrInt + intSYZ + yZ + intStart) = *(ptrInt + intSYZ + ym1Z + intStart) + diff * diff;

				//yz plane
				for (int z = intStart + 1; z < intEndZ; z++)
				{
					diff = *(ptrIn + intSptYZ + yptZ + z + tz) - *(ptrIn + intSYZ + yZ + z);
					var A = *(ptrInt + intSYZ + ym1Z + z - 1);
					var B = *(ptrInt + intSYZ + yZ + z - 1);
					var C = *(ptrInt + intSYZ + ym1Z + z);
					*(ptrInt + intSYZ + yZ + z) = B + C - A + diff * diff;
				}
			}

			for (int x = intStart + 1; x < intEndX; x++)
			{
				//indexing coeffs
				var xYZ = x * sizeYZ;
				var xm1YZ = xYZ - sizeYZ;
				var xptYZ = xYZ + txYZ;

				//x axis
				diff = *(ptrIn + xptYZ + intSptZ + intStart + tz) - *(ptrIn + xYZ + intSZ + intStart);
				*(ptrInt + xYZ + intSZ + intStart) = *(ptrInt + xm1YZ + intSZ + intStart) + diff * diff;

				//xz plane
				for (int z = intStart + 1; z < intEndZ; z++)
				{
					diff = *(ptrIn + xptYZ + intSptZ + z + tz) - *(ptrIn + xYZ + intSZ + z);
					var A = *(ptrInt + xm1YZ + intSZ + z - 1);
					var B = *(ptrInt + xYZ + intSZ + z - 1);
					var C = *(ptrInt + xm1YZ + intSZ + z);
					*(ptrInt + xYZ + intSZ + z) = B + C - A + diff * diff;
				}

				for (int y = intStart + 1; y < intEndY; y++)
				{
					//indexing coeffs
					var yZ = y * sizeZ;
					var ym1Z = yZ - sizeZ;
					var yptZ = yZ + tyZ;

					//xy plane
					diff = *(ptrIn + xptYZ + yptZ + intStart + tz) - *(ptrIn + xYZ + yZ + intStart);
					var A = *(ptrInt + xm1YZ + ym1Z + intStart);
					var B = *(ptrInt + xYZ + ym1Z + intStart);
					var C = *(ptrInt + xm1YZ + yZ + intStart);
					*(ptrInt + xYZ + yZ + intStart) = B + C - A + diff * diff;

					//rest of integral
					for (int z = intStart + 1; z < intEndZ; z++)
					{
						diff = *(ptrIn + xptYZ + yptZ + z + tz) - *(ptrIn + xYZ + yZ + z);

						//points on cube in the integral image
						A = *(ptrInt + xm1YZ + ym1Z + z);
						B = *(ptrInt + xYZ + ym1Z + z);
						C = *(ptrInt + xm1YZ + yZ + z);
						var E = *(ptrInt + xm1YZ + ym1Z + z - 1);
						var F = *(ptrInt + xYZ + ym1Z + z - 1);
						var G = *(ptrInt + xm1YZ + yZ + z - 1);
						var H = *(ptrInt + xYZ + yZ + z - 1);

						*(ptrInt + xYZ + yZ + z) = H + C - G + B - F - A + E + diff * diff;
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe long GetPatchDistance(long* ptrInt, int sxYZ, int exYZ, int syZ, int eyZ, int sz, int ez)
		{
			//points on the block (neighborhood around center voxel)
			var A = *(ptrInt + sxYZ + syZ + ez);
			var B = *(ptrInt + exYZ + syZ + ez);
			var C = *(ptrInt + sxYZ + eyZ + ez);
			var D = *(ptrInt + exYZ + eyZ + ez);
			var E = *(ptrInt + sxYZ + syZ + sz);
			var F = *(ptrInt + exYZ + syZ + sz);
			var G = *(ptrInt + sxYZ + eyZ + sz);
			var H = *(ptrInt + exYZ + eyZ + sz);

			return D - H + G - C + F - E + A - B;
		}
	}
}
