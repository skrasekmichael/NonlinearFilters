﻿using NonlinearFilters.Filters.Parameters;
using NonlinearFilters.Mathematics;
using NonlinearFilters.VolumetricData;

namespace NonlinearFilters.Filters3D;

public class BilateralFilter3 : BaseFilter3<BilateralParameters>
{
	private int radius, radius2;

	private readonly GaussianFunction spaceGauss = new();
	private readonly GaussianFunction rangeGauss = new();

	private readonly int[] border;

	public BilateralFilter3(ref BaseVolumetricData input, BilateralParameters parameters) : base(ref input, parameters)
	{
		border = new int[(input.Size.X + input.Size.Y + input.Size.Z) * 2];
	}

	protected override void InitalizeParams()
	{
		radius = Parameters.GetRadius();
		radius2 = radius * radius;

		spaceGauss.Initalize(Parameters.SpaceSigma);
		rangeGauss.Initalize(Parameters.RangeSigma);
	}

	protected override void InitalizeFilter()
	{
		var span = border.AsSpan();

		var borderXstart = span;
		var borderXend = borderXstart[Input.Size.X..];

		for (int i = 0; i < Input.Size.X; i++)
		{
			borderXstart[i] = Math.Max(i - radius, 0);
			borderXend[i] = Math.Min(i + radius, Input.Size.X - 1);
		}

		var borderYstart = borderXend[Input.Size.X..];
		var borderYend = borderYstart[Input.Size.Y..];

		for (int i = 0; i < Input.Size.Y; i++)
		{
			borderYstart[i] = Math.Max(i - radius, 0);
			borderYend[i] = Math.Min(i + radius, Input.Size.Y - 1);
		}

		var borderZstart = borderYend[Input.Size.Y..];
		var borderZend = borderZstart[Input.Size.Z..];

		for (int i = 0; i < Input.Size.Z; i++)
		{
			borderZstart[i] = Math.Max(i - radius, 0);
			borderZend[i] = Math.Min(i + radius, Input.Size.Z - 1);
		}
	}

	public override BaseVolumetricData ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, FilterBlock);

	private unsafe void FilterBlock(Block block, BaseVolumetricData input, BaseVolumetricData output, int index)
	{
		fixed (byte* ptrIn = input.Data)
		fixed (byte* ptrOut = output.Data)
		fixed (int* donePtr = doneCounts)
		fixed (int* ptrBorder = border)
		{
			int* doneIndexPtr = donePtr + index;

			int* ptrStartX = ptrBorder;
			int* ptrEndX = ptrStartX + input.Size.X;

			int* ptrStartY = ptrEndX + input.Size.X;
			int* ptrEndY = ptrStartY + input.Size.Y;

			int* ptrStartZ = ptrEndY + input.Size.Y;
			int* ptrEndZ = ptrStartZ + input.Size.Z;

			for (int cx = block.X; cx < block.X + block.Width; cx++)
			{
				int startx = *(ptrStartX + cx);
				int endx = *(ptrEndX + cx);

				for (int cy = block.Y; cy < block.Y + block.Height; cy++)
				{
					int starty = *(ptrStartY + cy);
					int endy = *(ptrEndY + cy);

					for (int cz = block.Z; cz < block.Z + block.Depth; cz++)
					{
						int startz = *(ptrStartZ + cz);
						int endz = *(ptrEndZ + cz);

						int dataIndex = input.Coords2Index(cx, cy, cz);
						byte centerIntensity = *(ptrIn + dataIndex);

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
										double gs = spaceGauss.Gauss(Math.Sqrt(d2));
										double fr = rangeGauss.Gauss(Math.Abs(intesity - centerIntensity));

										double weight = gs * fr;
										weightedSum += weight * intesity;
										normalzitaionFactor += weight;
									}
								}
							}
						}

						byte newIntesity = (byte)(weightedSum / normalzitaionFactor);

						*(ptrOut + dataIndex) = newIntesity;
						(*doneIndexPtr)++;
					}
					UpdateProgress();
				}
			}
		}
	}
}
