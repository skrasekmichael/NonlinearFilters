namespace NonlinearFilters.Filters.Interfaces;

public interface IFilterProgressChanged
{
	public event EventHandler<double>? OnProgressChanged;
}
