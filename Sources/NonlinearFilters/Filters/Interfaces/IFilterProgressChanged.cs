namespace NonlinearFilters.Filters.Interfaces;

public delegate void ProgressChanged(double percentage, object sender);

public interface IFilterProgressChanged
{
	public event ProgressChanged? OnProgressChanged;
}
