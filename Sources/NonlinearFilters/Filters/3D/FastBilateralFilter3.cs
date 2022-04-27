using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics;
using NonlinearFilters.Volume;
using System.Runtime.CompilerServices;

namespace NonlinearFilters.Filters3D;

/// <summary>
/// Fast 3D bilateral filter
/// </summary>
public class FastBilateralFilter3 : BaseFilter3<BilateralParameters>
{
	private int radius, diameter, diameter2;

	private double[]? rangeGauss;
	private double[,,]? spaceGauss;
	private int[]? biasY;
	private int[,]? biasZ;

	private int[]? borderY, borderZ;
	private readonly int[] borderX;

	private readonly GaussianFunction gaussFunction = new();

	/// <summary>
	/// Initializes new instance of the <see cref="FastBilateralFilter3"/> class.
	/// </summary>
	/// <param name="input">Input volumetric data</param>
	/// <param name="parameters">Filter parameters</param>
	public FastBilateralFilter3(ref VolumetricData input, BilateralParameters parameters) : base(ref input, parameters)
	{
		borderX = new int[input.Size.X * 2];
	}

	protected override void InitalizeParams()
	{
		radius = Parameters.GetRadius();
		diameter = 2 * radius + 1;
		diameter2 = diameter * diameter;

		borderY = new int[Input.Size.Y + 2 * radius];
		borderZ = new int[Input.Size.Z + 2 * radius];

		rangeGauss = null;
		spaceGauss = null;
		biasY = null;
		biasZ = null;
	}

	protected override void InitalizeFilter()
	{
		var span = borderX.AsSpan();

		//precomputing border around axis X instead of using Min/Max
		var borderXstart = span;
		var borderXend = span[Input.Size.X..];

		for (int i = 0; i < Input.Size.X; i++)
		{
			borderXstart[i] = Math.Max(i - radius, 0);
			borderXend[i] = Math.Min(i + radius, Input.Size.X - 1);
		}

		//precomputing border around axis Y instead of using Min/Max
		for (int i = radius; i < borderY!.Length; i++)
			borderY[i] = Math.Min(i - radius, Input.Size.Y - 1);

		//precomputing border around axis Z instead of using Min/Max
		for (int i = radius; i < borderZ!.Length; i++)
			borderZ[i] = Math.Min(i - radius, Input.Size.Z - 1);
	}

	protected override void PreCompute()
	{
		int radius2 = radius * radius;

		//precompute range Gaussian function
		gaussFunction.Initalize(Parameters.RangeSigma);
		rangeGauss = new double[511];
		rangeGauss[255] = gaussFunction.Gauss(0);
		for (int i = 1; i < 256; i++)
		{
			rangeGauss[255 + i] = rangeGauss[255 - i] = gaussFunction.Gauss(i);
		}

		//precompute spatial Gaussian function
		gaussFunction.Initalize(Parameters.SpaceSigma);
		spaceGauss = new double[diameter, diameter, diameter];
		for (int x = 0; x <= radius; x++)
		{
			int x2 = x * x;
			for (int y = 0; y <= radius; y++)
			{
				int y2px2 = y * y + x2;
				for (int z = 0; z <= radius; z++)
				{
					int d2 = z * z + y2px2;
					if (d2 < radius2)
					{
						//coords transformation
						int rmx = radius - x, rpx = radius + x;
						int rmy = radius - y, rpy = radius + y;
						int rmz = radius - z, rpz = radius + z;

						double val = gaussFunction.Gauss2(d2);
						spaceGauss[rmx, rpy, rpz] = spaceGauss[rmx, rmy, rpz] = spaceGauss[rpx, rpy, rpz] =
						spaceGauss[rpx, rmy, rpz] = spaceGauss[rpx, rmy, rmz] = spaceGauss[rmx, rmy, rmz] =
						spaceGauss[rpx, rpy, rmz] = spaceGauss[rmx, rpy, rmz] = val;
					}
				}
			}
		}

		//precompute circle area of spatial Gaussian function
		biasY = new int[diameter];
		biasY[radius] = radius;
		for (int i = 1; i < radius; i++)
		{
			int bias = (int)Math.Round(Math.Sqrt(radius2 - i * i));
			biasY[radius - i] = biasY[radius + i] = bias;
		}

		//precompute sphere area of spatial Gaussian function
		biasZ = new int[diameter, diameter];
		for (int x = 0; x < radius; x++)
		{
			int x2 = x * x;
			int endy = (int)Math.Sqrt(radius2 - x2);
			for (int y = 0; y < endy; y++)
			{
				int bias = (int)Math.Round(Math.Sqrt(radius2 - x2 - y * y));
				biasZ[radius - x, radius - y] = biasZ[radius + x, radius + y] =
				biasZ[radius - x, radius + y] = biasZ[radius + x, radius - y] = bias;
			}
		}
	}

	public override VolumetricData ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, FilterBlock);

	/// <summary>
	/// Transforms coords into index from precomputed sphere area (<see cref="biasZ"/>)
	/// </summary>
	/// <param name="x">X</param>
	/// <param name="y">Y</param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int CoordsToBiasZ(int x, int y) => x * diameter + y;

	/// <summary>
	/// Transforms coords into index from precomputed spatial Gaussian function (<see cref="spaceGauss"/>)
	/// </summary>
	/// <param name="x">X</param>
	/// <param name="y">Y</param>
	/// <param name="z">Z</param>
	/// <returns>Index</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int CoordsToSpace(int x, int y, int z) => x * diameter2 + y * diameter + z;

	private unsafe void FilterBlock(Block block, VolumetricData input, VolumetricData output, int index)
	{
		fixed (int* donePtr = doneCounts)
		fixed (int* ptrBiasY = biasY, ptrBiasZ = biasZ)
		fixed (byte* ptrIn = input.Data, ptrOut = output.Data)
		fixed (double* ptrSpaceGauss = spaceGauss, ptrRangeGauss = rangeGauss)
		fixed (int* ptrBorX = borderX, ptrBorY = borderY, ptrBorZ = borderZ)
		{
			int* doneIndexPtr = donePtr + index;
			double* ptrIndexRangeGauss = ptrRangeGauss + 255;

			//precomputed borders pointers
			int* ptrStartX = ptrBorX;
			int* ptrEndX = ptrStartX + input.Size.X;
			int* ptrBorderY = ptrBorY + radius;
			int* ptrBorderZ = ptrBorZ + radius;

			//loop trough voxels in assigned area in the volumetric data
			for (int cx = block.X; cx < block.X + block.Width; cx++)
			{
				int rmcx = radius - cx;
				int startx = *(ptrStartX + cx); //Min(cx - radius, 0)
				int endx = *(ptrEndX + cx); //Max(cx + radius, Input.Size.X - 1)

				for (int cy = block.Y; cy < block.Y + block.Height; cy++)
				{
					int rmcy = radius - cy;

					int startCenterIndex = input.Coords2Index(cx, cy, block.Z);
					for (int cz = block.Z; cz < block.Z + block.Depth; cz++)
					{
						int rmcz = radius - cz;

						int centerDataIndex = startCenterIndex++;
						byte centerIntensity = *(ptrIn + centerDataIndex); //intensity at filtered voxel

						double* ptrIndexRangeGaussCentered = ptrIndexRangeGauss - centerIntensity;

						//loop trough area and compute weighted average
						double weightedSum = 0, normalizationFactor = 0;
						for (int x = startx; x <= endx; x++)
						{
							int tx = rmcx + x; //transformed x
			
							int biasY = *(ptrBiasY + tx); //bias along y axis

							int starty = *(ptrBorderY + cy - biasY); //Max(cy - biasY, 0)
							int endy = *(ptrBorderY + cy + biasY); //Min(cy + biasY, Input.Size.Y - 1)

							int biasZIndex = CoordsToBiasZ(tx, rmcy + starty);
							for (int y = starty; y <= endy; y++)
							{
								int ty = rmcy + y; //transformed y

								int biasZ = *(ptrBiasZ + biasZIndex++); //bias along z axis

								int startz = *(ptrBorderZ + cz - biasZ); //Max(cz - biasZ, 0)
								int endz = *(ptrBorderZ + cz + biasZ); //Min(cz + biasZ, Input.Size.Z - 1)

								int dataIndex = input.Coords2Index(x, y, startz); //index into Input volumetric data
								int spaceIndex = CoordsToSpace(tx, ty, rmcz + startz); //index into spatial Gaussian function
								for (int z = startz; z <= endz; z++)
								{
									byte intensity = *(ptrIn + dataIndex++); //getting voxel intensity and incrementing index

									double gs = *(ptrSpaceGauss + spaceIndex++); //reading from spatial Gaussian weighting function and incrementing index
									double fr = *(ptrIndexRangeGaussCentered + intensity); //reading from range Gaussian wighting function

									double weight = gs * fr;
									weightedSum += weight * intensity;
									normalizationFactor += weight;
								}
							}
						}

						byte newIntesity = (byte)(weightedSum / normalizationFactor);
						*(ptrOut + centerDataIndex) = newIntesity;
						(*doneIndexPtr)++; //storing progress
					}

					if (IsCanceled) return;
					UpdateProgress();
				}
			}
		}
	}
}
