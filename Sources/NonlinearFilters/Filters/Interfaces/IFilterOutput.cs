using NonlinearFilters.Volume;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace NonlinearFilters.Filters.Interfaces;

public interface IFilterOutput<T> : IFilter
{
	public abstract T ApplyFilter(int cpuCount = 1);
}

public interface IFilter2Output : IFilterOutput<Image<Rgba32>> { }

public interface IFilter3Output : IFilterOutput<VolumetricData> { }

