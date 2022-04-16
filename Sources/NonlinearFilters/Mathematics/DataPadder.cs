using NonlinearFilters.Volume;
using SixLabors.ImageSharp;

namespace NonlinearFilters.Mathematics
{
	public class DataPadder
	{
		public unsafe byte[] CreatePadding(byte* inPtr, Size bounds, int padding)
		{
			int paddedWidth = bounds.Width + 2 * padding;
			int paddedHeight = bounds.Height + 2 * padding;
			var data = new byte[paddedHeight * paddedWidth * 4];
			fixed (byte* dataPtr = data)
			{
				byte* ptr = dataPtr;
				for (int y = 0; y < paddedHeight; y++)
				{
					int py = Math.Clamp(y - padding, 0, bounds.Height - 1);
					for (int x = 0; x < paddedWidth; x++)
					{
						int px = Math.Clamp(x - padding, 0, bounds.Width - 1);
						var color = inPtr + 4 * (py * bounds.Width + px);
						*ptr++ = *color;
						*ptr++ = *(color + 1);
						*ptr++ = *(color + 2);
						*ptr++ = *(color + 3);
					}
				}
			}
			return data;
		}

		public unsafe void RemovePaddding(byte* paddedDataPtr, byte* outputDataPtr, Size size, int padding)
		{
			int paddedWidth = size.Width + 2 * padding;
			for (int y = padding; y < size.Height + padding; y++)
			{
				for (int x = padding; x < size.Width + padding; x++)
				{
					var color = paddedDataPtr + 4 * (y * paddedWidth + x);
					*outputDataPtr++ = *color++;
					*outputDataPtr++ = *color++;
					*outputDataPtr++ = *color;
					*outputDataPtr++ = 255;
				}
			}
		}

		public VolumetricData CreatePadding(VolumetricData vol, int padding)
		{
			int paddedWidth = vol.Size.X + 2 * padding;
			int paddedHeight = vol.Size.Y + 2 * padding;
			int paddedDepth = vol.Size.Z + 2 * padding;

			var data = new VolumetricData(new(new(paddedWidth, paddedHeight, paddedDepth), vol.Parameters.Ratio, vol.Parameters.Border), new byte[paddedWidth * paddedHeight * paddedDepth]);

			for (int x = 0; x < paddedWidth; x++)
			{
				int px = Math.Clamp(x - padding, 0, vol.Size.X - 1);
				for (int y = 0; y < paddedHeight; y++)
				{
					int py = Math.Clamp(y - padding, 0, vol.Size.Y - 1);
					for (int z = 0; z < paddedDepth; z++)
					{
						int pz = Math.Clamp(z - padding, 0, vol.Size.Z - 1);
						data[x, y, z] = vol[px, py, pz];
					}
				}
			}

			return data;
		}

		public unsafe void RemovePaddding(VolumetricData paddedData, VolumetricData outputData, int padding)
		{
			for (int x = 0; x < outputData.Size.X; x++)
			{
				for (int y = 0; y < outputData.Size.Y; y++)
				{
					for (int z = 0; z < outputData.Size.Z; z++)
						outputData[x, y, z] = paddedData[x + padding, y + padding, z + padding];
				}
			}
		}
	}
}
