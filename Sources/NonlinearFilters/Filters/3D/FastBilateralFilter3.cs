using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics;
using NonlinearFilters.Volume;
using System.Runtime.CompilerServices;

namespace NonlinearFilters.Filters3D;

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

		var borderXstart = span;
		var borderXend = span.Slice(Input.Size.X);

		for (int i = 0; i < Input.Size.X; i++)
		{
			borderXstart[i] = Math.Max(i - radius, 0);
			borderXend[i] = Math.Min(i + radius, Input.Size.X - 1);
		}

		for (int i = radius; i < borderY!.Length; i++)
			borderY[i] = Math.Min(i - radius, Input.Size.Y - 1);

		for (int i = radius; i < borderZ!.Length; i++)
			borderZ[i] = Math.Min(i - radius, Input.Size.Z - 1);
	}

	protected override void PreCompute()
	{
		int radius2 = radius * radius;

		//precompute gauss function for range sigma
		gaussFunction.Initalize(Parameters.RangeSigma);
		rangeGauss = new double[511];
		rangeGauss[255] = gaussFunction.Gauss(0);
		for (int i = 1; i < 256; i++)
		{
			rangeGauss[255 + i] = rangeGauss[255 - i] = gaussFunction.Gauss(i);
		}

		//precompute gauss function for space sigma
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
						int rmx = radius - x, rpx = radius + x;
						int rmy = radius - y, rpy = radius + y;
						int rmz = radius - z, rpz = radius + z;

						double val = gaussFunction.Gauss(Math.Sqrt(d2));
						spaceGauss[rmx, rpy, rpz] = spaceGauss[rmx, rmy, rpz] = spaceGauss[rpx, rpy, rpz] =
						spaceGauss[rpx, rmy, rpz] = spaceGauss[rpx, rmy, rmz] = spaceGauss[rmx, rmy, rmz] =
						spaceGauss[rpx, rpy, rmz] = spaceGauss[rmx, rpy, rmz] = val;
					}
				}
			}
		}

		//precompute circle area of spatial gauss function
		biasY = new int[diameter];
		biasY[radius] = radius;
		for (int i = 1; i < radius; i++)
		{
			int bias = (int)Math.Round(Math.Sqrt(radius2 - i * i));
			biasY[radius - i] = biasY[radius + i] = bias;
		}

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int CoordsToBiasZ(int x, int y) => x * diameter + y;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int CoordsToSpace(int x, int y, int z) => x * diameter2 + y * diameter + z;

	private unsafe void FilterBlock(Block block, VolumetricData input, VolumetricData output, int index)
	{
		fixed (int* donePtr = doneCounts)
		fixed (int* ptrBiasY = biasY)
		fixed (int* ptrBiasZ = biasZ)
		fixed (byte* ptrIn = input.Data)
		fixed (byte* ptrOut = output.Data)
		fixed (double* ptrSpaceGauss = spaceGauss)
		fixed (double* ptrRangeGauss = rangeGauss)
		fixed (int* ptrBorX = borderX)
		fixed (int* ptrBorY = borderY)
		fixed (int* ptrBorZ = borderZ)
		{
			int* doneIndexPtr = donePtr + index;
			double* ptrIndexRangeGauss = ptrRangeGauss + 255;

			int* ptrStartX = ptrBorX;
			int* ptrEndX = ptrStartX + input.Size.X;

			int* ptrBorderY = ptrBorY + radius;
			int* ptrBorderZ = ptrBorZ + radius;

			for (int cx = block.X; cx < block.X + block.Width; cx++)
			{
				int rmcx = radius - cx;
				int startx = *(ptrStartX + cx);
				int endx = *(ptrEndX + cx);

				for (int cy = block.Y; cy < block.Y + block.Height; cy++)
				{
					int rmcy = radius - cy;

					int startCenterIndex = input.Coords2Index(cx, cy, block.Z);
					for (int cz = block.Z; cz < block.Z + block.Depth; cz++)
					{
						int rmcz = radius - cz;

						int centerDataIndex = startCenterIndex++;
						byte centerIntensity = *(ptrIn + centerDataIndex);

						double* ptrIndexRangeGaussCentered = ptrIndexRangeGauss - centerIntensity;

						double weightedSum = 0, normalizationFactor = 0;
						for (int x = startx; x <= endx; x++)
						{
							int tx = rmcx + x;
			
							int biasY = *(ptrBiasY + tx);

							int starty = *(ptrBorderY + cy - biasY);
							int endy = *(ptrBorderY + cy + biasY);

							int biasZIndex = CoordsToBiasZ(tx, rmcy + starty);
							for (int y = starty; y <= endy; y++)
							{
								int ty = rmcy + y;

								int biasZ = *(ptrBiasZ + biasZIndex++);

								int startz = *(ptrBorderZ + cz - biasZ);
								int endz = *(ptrBorderZ + cz + biasZ);

								int dataIndex = input.Coords2Index(x, y, startz);
								int spaceIndex = CoordsToSpace(tx, ty, rmcz + startz);
								for (int z = startz; z <= endz; z++)
								{
									byte intensity = *(ptrIn + dataIndex++);

									double gs = *(ptrSpaceGauss + spaceIndex++);
									double fr = *(ptrIndexRangeGaussCentered + intensity);

									double weight = gs * fr;
									weightedSum += weight * intensity;
									normalizationFactor += weight;
								}
							}
						}

						byte newIntesity = (byte)(weightedSum / normalizationFactor);
						*(ptrOut + centerDataIndex) = newIntesity;
						(*doneIndexPtr)++;
					}

					if (IsCanceled) return;
					UpdateProgress();
				}
			}
		}
	}
}
