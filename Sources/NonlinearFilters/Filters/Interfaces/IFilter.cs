namespace NonlinearFilters.Filters.Interfaces;

public interface IFilter
{
	public void Cancel();
}

public interface IFilter2 : IFilter, IFilter2Output { }

public interface IFilter3 : IFilter, IFilter3Output { }
