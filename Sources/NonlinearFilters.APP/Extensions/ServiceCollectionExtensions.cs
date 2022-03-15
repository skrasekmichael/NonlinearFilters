using Microsoft.Extensions.DependencyInjection;
using NonlinearFilters.APP.Factories;

namespace NonlinearFilters.APP.Extensions
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddSingletonFactory<TService, TFactory>(this IServiceCollection services)
			where TFactory : class, IServiceFactory<TService> where TService : class
		{
			services.AddSingleton<TFactory>();

			return services.AddSingleton(serviceProvider =>
			{
				var factory = serviceProvider.GetRequiredService<TFactory>();
				return factory.Create(serviceProvider);
			});
		}

		public static IServiceCollection AddTransientFactory<TService, TFactory>(this IServiceCollection services)
			where TFactory : class, IServiceFactory<TService> where TService : class
		{
			services.AddSingleton<TFactory>();

			return services.AddTransient(serviceProvider =>
			{
				var factory = serviceProvider.GetRequiredService<TFactory>();
				return factory.Create(serviceProvider);
			});
		}

		public static IServiceCollection AddScopedFactory<TService, TFactory>(this IServiceCollection services)
			where TFactory : class, IServiceFactory<TService> where TService : class
		{
			services.AddSingleton<TFactory>();

			return services.AddScoped(serviceProvider =>
			{
				var factory = serviceProvider.GetRequiredService<TFactory>();
				return factory.Create(serviceProvider);
			});
		}
	}
}
