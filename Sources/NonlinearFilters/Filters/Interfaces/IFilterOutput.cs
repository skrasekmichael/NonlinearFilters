using NonlinearFilters.Volume;
using System.Drawing;

namespace NonlinearFilters.Filters.Interfaces;

public interface IFilterOutput<T> : IFilter
{
	public abstract T ApplyFilter(int cpuCount = 1);
}

public interface IFilter2Output : IFilterOutput<Bitmap> { }

public interface IFilter3Output : IFilterOutput<VolumetricData> { }

