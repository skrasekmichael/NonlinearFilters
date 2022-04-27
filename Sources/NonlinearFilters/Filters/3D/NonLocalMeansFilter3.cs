using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics.NonLocalMeansWeightingFunction;
using NonlinearFilters.Mathematics;
using NonlinearFilters.Volume;

namespace NonlinearFilters.Filters3D
{
	/// <summary>
	/// 3D non-local means filter
	/// </summary>
	public class NonLocalMeansFilter3 : BaseFilter3<NonLocalMeansParameters>
	{
		private WeightingFunction? weightingFunction;

		/// <summary>
		/// Initializes new instance of the <see cref="NonLocalMeansFilter3"/> class.
		/// </summary>
		/// <param name="input">Input volumetric data</param>
		/// <param name="parameters">Filter parameters</param>
		public NonLocalMeansFilter3(ref VolumetricData input, NonLocalMeansParameters parameters) : base(ref input, parameters) { }

		public override VolumetricData ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, FilterBlock);

		protected override void InitalizeParams()
		{
			Padding = Parameters.WindowRadius + Parameters.PatchRadius;
		}

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

		private unsafe void FilterBlock(Block block, VolumetricData input, VolumetricData output, int index)
		{
			fixed (int* donePtr = doneCounts)
			{
				int* doneIndexPtr = donePtr + index;

				for (int cx = block.X; cx < block.X + block.Width; cx++)
				{
					int windowStartX = cx - Parameters.WindowRadius;
					int windowEndX = cx + Parameters.WindowRadius;

					for (int cy = block.Y; cy < block.Y + block.Height; cy++)
					{
						int windowStartY = cy - Parameters.WindowRadius;
						int windowEndY = cy + Parameters.WindowRadius;

						for (int cz = block.Z; cz < block.Z + block.Depth; cz++)
						{
							int windowStartZ = cz - Parameters.WindowRadius;
							int windowEndZ = cz + Parameters.WindowRadius;

							double normalizationFactor = 0;
							double weightedSum = 0;

							for (int wx = windowStartX; wx <= windowEndX; wx++)
							{
								for (int wy = windowStartY; wy <= windowEndY; wy++)
								{
									for (int wz = windowStartZ; wz <= windowEndZ; wz++)
									{
										long distance = 0; //squared Euclidean distance
										for (int px = -Parameters.PatchRadius; px <= Parameters.PatchRadius; px++)
										{
											for (int py = -Parameters.PatchRadius; py <= Parameters.PatchRadius; py++)
											{
												for (int pz = -Parameters.PatchRadius; pz <= Parameters.PatchRadius; pz++)
												{
													var diff =
														input.Data[input.Coords2Index(wx + px, wy + py, wz + pz)] -
														input.Data[input.Coords2Index(cx + px, cy + py, cz + pz)];

													distance += diff * diff;
												}
											}
										}

										double weight = weightingFunction!.GetValue(distance);

										normalizationFactor += weight;
										weightedSum += input.Data[input.Coords2Index(wx, wy, wz)] * weight;
									}
								}
							}

							byte newIntensity = (byte)(weightedSum / normalizationFactor);
							output.Data[output.Coords2Index(cx, cy, cz)] = newIntensity;
							(*doneIndexPtr)++; //storing progress
						}

						if (IsCanceled) return;
						UpdateProgress();
					}
				}
			}
		}
	}
}
