using NonlinearFilters.Mathematics.NonLocalMeansWeightingFunction;
using NonlinearFilters.Mathematics;
using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Volume;
using System.Runtime.CompilerServices;
using OpenTK.Mathematics;

namespace NonlinearFilters.Filters3D
{
	/// <summary>
	/// Fast 3D non-local means filter
	/// </summary>
	public class FastNonLocalMeansFilter3 : BaseFilter3<NonLocalMeansParameters>
	{
		//global indexing coeffs
		private int sizeYZ, sizeZ, reYZ, rsYZ, reZ, rsZ, rs;
		private double[] weightedSum = null!, normalizationFactor = null!;

		private WeightingFunction? weightingFunction;

		/// <summary>
		/// Initializes new instance of the <see cref="FastNonLocalMeansFilter3"/> class.
		/// </summary>
		/// <param name="input">Input volumetric data</param>
		/// <param name="parameters">Filter parameters</param>
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

			//global indexing coeffs
			sizeYZ = input.Size.Y * input.Size.Z;
			sizeZ = input.Size.Z;
			rs = Parameters.PatchRadius + 1;
			reYZ = Parameters.PatchRadius * sizeYZ;
			rsYZ = rs * sizeYZ;
			reZ = Parameters.PatchRadius * sizeZ;
			rsZ = rs * sizeZ;
		}

		private unsafe void FilterBlock(Block threadBlock, VolumetricData input, VolumetricData output, int index)
		{
			var txe = threadBlock.X + threadBlock.Width;
			var tye = threadBlock.Y + threadBlock.Height;
			var tze = threadBlock.Z + threadBlock.Depth;

			//allocate integral image for every thread
			var integral = new long[input.Size.X * input.Size.Y * input.Size.Z];

			double done = 0; //progress storage
			var windowDiameter = Parameters.WindowRadius * 2 + 1;
			double next = (double)threadBlock.Depth / (windowDiameter * windowDiameter * windowDiameter); //progress step

			//integral bounds
			int intStartX = threadBlock.X - Parameters.WindowRadius;
			int intStartY = threadBlock.Y - Parameters.WindowRadius;
			int intStartZ = threadBlock.Z - Parameters.WindowRadius;
			int intEndX = txe + Parameters.WindowRadius;
			int intEndY = tye + Parameters.WindowRadius;
			int intEndZ = tze + Parameters.WindowRadius;

			var intStart = new Vector3i(intStartX, intStartY, intStartZ);
			var intEnd = new Vector3i(intEndX, intEndY, intEndZ);

			//integral indexing coeffs
			int intSYZ = intStartX * sizeYZ;
			int intSZ = intStartY * sizeZ;

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
							CalculateIntegral(ptrInt, ptrIn, intStart, intEnd, intSYZ, intSZ, wx, wy, wz, wxYZ, wyZ);

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
										//squared Euclidean distance between patches
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
									*ptrDoneIndex = (int)done; //storing progress
								}
							}

							if (IsCanceled) goto cancel;
							UpdateProgress();
						}
					}
				}

			cancel:

				//final loop for normalizing values
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

		/// <summary>
		/// Calculates 3D integral image for Euclidien distance for patches moved 
		/// by vector [<paramref name="tx"/>, <paramref name="ty"/>, <paramref name="tz"/>].
		/// Calculates only required values defined in range <paramref name="start"/> to <paramref name="end"/>.
		/// </summary>
		/// <param name="ptrIntegral">Pointer to integral image</param>
		/// <param name="ptrIn">Pointer to volumetric data</param>
		/// <param name="start">Start point for required values</param>
		/// <param name="end">End point for required values</param>
		/// <param name="intSYZ">3D array indexing coeff - <paramref name="start"/>.X * <see cref="sizeYZ"/></param>
		/// <param name="intSZ">3D array indexing coeff - <paramref name="start"/>.Y * <see cref="sizeZ"/></param>
		/// <param name="tx">Transformed coord X</param>
		/// <param name="ty">Transformed coord Y</param>
		/// <param name="tz">Transformed coord Z</param>
		/// <param name="txYZ">3D array indexing coeff - <paramref name="tx"/> * <see cref="sizeYZ"/></param>
		/// <param name="tyZ">3D array indexing coeff - <paramref name="ty"/> * <see cref="sizeYZ"/></param>
		private unsafe void CalculateIntegral(long* ptrIntegral, byte *ptrIn, Vector3i start, Vector3i end, int intSYZ, int intSZ, int tx, int ty, int tz, int txYZ, int tyZ)
		{
			//indexing coeffs
			var intSptYZ = intSYZ + txYZ;
			var intSptZ = intSZ + tyZ;

			//first cell
			long diff = *(ptrIn + intSptYZ + intSptZ + start.Z + tz) - *(ptrIn + intSYZ + intSZ + start.Z);
			*(ptrIntegral + intSYZ + intSZ + start.Z) = diff * diff;

			//z axis
			for (int z = start.Z + 1; z <= end.Z; z++)
			{
				diff = *(ptrIn + intSptYZ + intSptZ + z + tz) - *(ptrIn + intSYZ + intSZ + z);
				*(ptrIntegral + intSYZ + intSZ + z) = *(ptrIntegral + intSYZ + intSZ + z - 1) + diff * diff;
			}

			for (int y = start.Y + 1; y <= end.Y; y++)
			{
				//indexing coeffs
				var yZ = y * sizeZ;
				var ym1Z = yZ - sizeZ;
				var yptZ = yZ + tyZ;

				//y axis
				diff = *(ptrIn + intSptYZ + yptZ + start.Z + tz) - *(ptrIn + intSYZ + yZ + start.Z);
				*(ptrIntegral + intSYZ + yZ + start.Z) = *(ptrIntegral + intSYZ + ym1Z + start.Z) + diff * diff;

				//yz plane
				for (int z = start.Z + 1; z < end.Z; z++)
				{
					diff = *(ptrIn + intSptYZ + yptZ + z + tz) - *(ptrIn + intSYZ + yZ + z);
					var A = *(ptrIntegral + intSYZ + ym1Z + z - 1);
					var B = *(ptrIntegral + intSYZ + yZ + z - 1);
					var C = *(ptrIntegral + intSYZ + ym1Z + z);
					*(ptrIntegral + intSYZ + yZ + z) = B + C - A + diff * diff;
				}
			}

			for (int x = start.X + 1; x <= end.X; x++)
			{
				//indexing coeffs
				var xYZ = x * sizeYZ;
				var xm1YZ = xYZ - sizeYZ;
				var xptYZ = xYZ + txYZ;

				//x axis
				diff = *(ptrIn + xptYZ + intSptZ + start.Z + tz) - *(ptrIn + xYZ + intSZ + start.Z);
				*(ptrIntegral + xYZ + intSZ + start.Z) = *(ptrIntegral + xm1YZ + intSZ + start.Z) + diff * diff;

				//xz plane
				for (int z = start.Z + 1; z <= end.Z; z++)
				{
					diff = *(ptrIn + xptYZ + intSptZ + z + tz) - *(ptrIn + xYZ + intSZ + z);
					var A = *(ptrIntegral + xm1YZ + intSZ + z - 1);
					var B = *(ptrIntegral + xYZ + intSZ + z - 1);
					var C = *(ptrIntegral + xm1YZ + intSZ + z);
					*(ptrIntegral + xYZ + intSZ + z) = B + C - A + diff * diff;
				}

				for (int y = start.Y + 1; y <= end.Y; y++)
				{
					//indexing coeffs
					var yZ = y * sizeZ;
					var ym1Z = yZ - sizeZ;
					var yptZ = yZ + tyZ;

					//xy plane
					diff = *(ptrIn + xptYZ + yptZ + start.Z + tz) - *(ptrIn + xYZ + yZ + start.Z);
					var A = *(ptrIntegral + xm1YZ + ym1Z + start.Z);
					var B = *(ptrIntegral + xYZ + ym1Z + start.Z);
					var C = *(ptrIntegral + xm1YZ + yZ + start.Z);
					*(ptrIntegral + xYZ + yZ + start.Z) = B + C - A + diff * diff;

					//rest of integral
					for (int z = start.Z + 1; z <= end.Z; z++)
					{
						diff = *(ptrIn + xptYZ + yptZ + z + tz) - *(ptrIn + xYZ + yZ + z);

						//points on cube in the integral image
						A = *(ptrIntegral + xm1YZ + ym1Z + z);
						B = *(ptrIntegral + xYZ + ym1Z + z);
						C = *(ptrIntegral + xm1YZ + yZ + z);
						var E = *(ptrIntegral + xm1YZ + ym1Z + z - 1);
						var F = *(ptrIntegral + xYZ + ym1Z + z - 1);
						var G = *(ptrIntegral + xm1YZ + yZ + z - 1);
						var H = *(ptrIntegral + xYZ + yZ + z - 1);

						*(ptrIntegral + xYZ + yZ + z) = H + C - G + B - F - A + E + diff * diff;
					}
				}
			}
		}

		/// <summary>
		/// Computes squared Euclidean distance using integral image
		/// </summary>
		/// <param name="ptrInt">Pointer to integral image</param>
		/// <param name="sxYZ">X coord of left plane points (A,C,E,G) * Depth * Height - 3D array indexing coeff</param>
		/// <param name="exYZ">X coord of right plane points (B,D,F,H) * Depth * Height - 3D array indexing coeff</param>
		/// <param name="syZ">Y coord of back plane points (A,B,E,F) * Depth - 3D array indexing coeff</param>
		/// <param name="eyZ">Y coord of front plane points (C,D,G,H) * Depth - 3D array indexing coeff</param>
		/// <param name="sz">Z coord of lower plane points (E,F,G,H)</param>
		/// <param name="ez">Z coord of upper plane points (A,B,C,D)</param>
		/// <returns>Squared Euclidean distance</returns>
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
