namespace NonlinearFilters.APP.Factories;

public interface IFactory<TService>
{
	public TService Create();
}
