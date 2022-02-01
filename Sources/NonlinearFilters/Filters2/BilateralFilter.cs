﻿using NonlinearFilters.Extensions;
using NonlinearFilters.Filters2.Parameters;
using NonlinearFilters.Mathematics;
using OpenTK.Mathematics;
using System.Drawing;

namespace NonlinearFilters.Filters2
{
	public class BilateralFilter : BaseFilter2<BilateralParameters>
	{
		private int radius;

		private readonly GaussFunction spaceGauss = new();
		private readonly GaussFunction rangeGauss = new();

		public BilateralFilter(ref Bitmap input, BilateralParameters parameters) : base(ref input, parameters)
		{
		}

		protected override void InitalizeParams()
		{
			radius = (int)(2.5 * Parameters.SpaceSigma);
			spaceGauss.Initalize(Parameters.SpaceSigma);
			rangeGauss.Initalize(Parameters.RangeSigma);
		}

		public override Bitmap ApplyFilter(int cpuCount = 1) => FilterArea(cpuCount, Parameters.GrayScale ? FilterWindow : FilterWindowRGB);

		private unsafe void FilterWindow(Rectangle window, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			for (int j = window.Y; j < window.Y + window.Height; j++)
			{
				for (int i = window.X; i < window.X + window.Width; i++)
				{
					Vector2i coords = new(i, j);
					double newIntesity = InternalLoop(inPtr, coords);
					SetIntensity(Coords2Ptr(outPtr, coords), newIntesity);

					doneCounts![index]++;
				}
				UpdateProgress();
			}
		}

		private unsafe double InternalLoop(byte* inPtr, Vector2i center)
		{
			int startx = Math.Max(center.X - radius + 1, 0);
			int starty = Math.Max(center.Y - radius + 1, 0);

			int endx = Math.Min(center.X + radius, Bounds.Width);
			int endy = Math.Min(center.Y + radius, Bounds.Height);

			double centerIntensity = GetIntensityD(Coords2Ptr(inPtr, center));

			double sum = 0, wp = 0;
			for (int y = starty; y < endy; y++)
			{
				for (int x = startx; x < endx; x++)
				{
					Vector2i coords = new(x, y);
					double distance = (coords - center).EuclideanLength;
					if (distance < radius)
					{
						double intesity = GetIntensityD(Coords2Ptr(inPtr, coords));
						double gs = spaceGauss.Gauss(distance);
						double fr = rangeGauss.Gauss(Math.Abs(intesity - centerIntensity));

						double w = gs * fr;
						sum += w * intesity;
						wp += w;
					}
				}
			}
			return sum / wp;
		}

		private unsafe void FilterWindowRGB(Rectangle window, IntPtr inputPtr, IntPtr outputPtr, int index)
		{
			byte* inPtr = (byte*)inputPtr.ToPointer();
			byte* outPtr = (byte*)outputPtr.ToPointer();

			for (int j = window.Y; j < window.Y + window.Height; j++)
			{
				for (int i = window.X; i < window.X + window.Width; i++)
				{
					Vector2i coords = new(i, j);
					Vector4d newColor = InternalLoopRGB(inPtr, coords);
					SetColor(Coords2Ptr(outPtr, coords), newColor);

					doneCounts![index]++;
				}
				UpdateProgress();
			}
		}

		private unsafe Vector4d InternalLoopRGB(byte* inPtr, Vector2i center)
		{
			int startx = Math.Max(center.X - radius, 0);
			int starty = Math.Max(center.Y - radius, 0);

			int endx = Math.Min(center.X + radius, Bounds.Width);
			int endy = Math.Min(center.Y + radius, Bounds.Height);

			Vector4d centerColor = GetColorD(Coords2Ptr(inPtr, center));

			Vector4d sum = Vector4d.Zero, wp = Vector4d.Zero;
			for (int y = starty; y < endy; y++)
			{
				for (int x = startx; x < endx; x++)
				{
					Vector2i coords = new(x, y);
					double distance = (coords - center).EuclideanLength;
					if (distance < radius)
					{
						Vector4d color = GetColorD(Coords2Ptr(inPtr, coords));
						double gs = spaceGauss.Gauss(distance);
						Vector4d fr = rangeGauss.Gauss((color - centerColor).Abs());

						Vector4d w = gs * fr;
						wp += w;
						sum += w * color;
					}
				}
			}
			return sum.Div(wp);
		}
	}
}
