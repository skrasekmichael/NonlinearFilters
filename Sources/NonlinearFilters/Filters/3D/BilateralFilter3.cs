using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics;
using NonlinearFilters.Volume;

namespace NonlinearFilters.Filters3D;

/// <summary>
/// 3D Bilateral filter
/// </summary>
public class BilateralFilter3 : BaseFilter3<BilateralParameters>
{
	private int radius, radius2;

	private readonly GaussianFunction spaceGauss = new();
	private readonly GaussianFunction rangeGauss = new();

	/// <summary>
	/// Initializes new instance of the <see cref="BilateralFilter3"/> class.
	/// </summary>
	/// <param name="input">Input volumetric data</param>
	/// <param name="parameters">Filter parameters</param>
	public BilateralFilter3(ref VolumetricData input, BilateralParameters parameters) : base(ref input, parameters) { }

	protected override void InitalizeParams()
	{
		radius = Parameters.GetRadius();
		radius2 = radius * radius;
		Padding = radius;

		spaceGauss.Initalize(Parameters.SpaceSigma);
		rangeGauss.Initalize(Parameters.RangeSigma);
	}

	public override VolumetricData ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, FilterBlock);

	private unsafe void FilterBlock(Block block, VolumetricData input, VolumetricData output, int index)
	{
		fixed (byte* ptrIn = input.Data)
		fixed (byte* ptrOut = output.Data)
		fixed (int* donePtr = doneCounts)
		{
			int* doneIndexPtr = donePtr + index;

			for (int cx = block.X; cx < block.X + block.Width; cx++)
			{
				int startx = cx - radius;
				int endx = cx + radius;

				for (int cy = block.Y; cy < block.Y + block.Height; cy++)
				{
					int starty = cy - radius;
					int endy = cy + radius;

					for (int cz = block.Z; cz < block.Z + block.Depth; cz++)
					{
						int startz = cz - radius;
						int endz = cz + radius;

						int dataIndex = input.Coords2Index(cx, cy, cz);
						byte centerIntensity = *(ptrIn + dataIndex); //intensity at filtered voxel

						double weightedSum = 0, normalzitaionFactor = 0;
						for (int x = startx; x <= endx; x++)
						{
							int dx = x - cx;
							int dx2 = dx * dx;

							for (int y = starty; y <= endy; y++)
							{
								int dy = y - cy;
								int dy2pdx2 = dy * dy + dx2;

								for (int z = startz; z <= endz; z++)
								{
									int dz = z - cz;
									int d2 = dy2pdx2 + dz * dz;

									if (d2 < radius2)
									{
										byte intesity = input[x, y, z];
										double gs = spaceGauss.Gauss2(d2); //spatial Gaussian weighting function
										double fr = rangeGauss.Gauss(Math.Abs(intesity - centerIntensity)); //range Gaussian weighting function

										double weight = gs * fr;
										weightedSum += weight * intesity;
										normalzitaionFactor += weight;
									}
								}
							}
						}

						byte newIntesity = (byte)(weightedSum / normalzitaionFactor);

						*(ptrOut + dataIndex) = newIntesity;
						(*doneIndexPtr)++; //storing progress
					}

					if (IsCanceled) return;
					UpdateProgress();
				}
			}
		}
	}
}
