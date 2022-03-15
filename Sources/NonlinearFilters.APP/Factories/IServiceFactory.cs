namespace NonlinearFilters.APP.Factories;

public interface IServiceFactory<TService>
{
	public TService Create(IServiceProvider serviceProvider);
}
