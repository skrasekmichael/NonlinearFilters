using NonlinearFilters.Mathematics;
using System.Drawing;

namespace NonlinearFilters.APP.Models
{
	public class DataInput
	{
		public VolumetricImage? Volume { get; private set; }
		public Bitmap? Image { get; private set; }

		public DataInput(VolumetricImage volume)
		{
			Volume = volume;
			Image = null;
		}

		public DataInput(Bitmap image)
		{
			Image = image;
			Volume = null;
		}
	}
}
