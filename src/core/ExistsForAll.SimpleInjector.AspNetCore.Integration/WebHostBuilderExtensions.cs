using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;

namespace ExistsForAll.SimpleInjector.AspNetCore.Integration
{
	public static class WebHostBuilderExtensions
	{
		public static IWebHostBuilder UseSimpleInjector(this IWebHostBuilder target,
			Action<ContainerOptions> action = null, Action<ServiceProviderFactoryOptions> options = null)
		{
			target.ConfigureServices(x =>
				x.AddSingleton<IServiceProviderFactory<Container>>(new SimpleInjectorServiceProviderFactory(action, options)));
			return target;
		}

		public static IWebHostBuilder UseSimpleInjector(this IWebHostBuilder target,
			Container container,
			Action<ServiceProviderFactoryOptions> options = null)
		{
			target.ConfigureServices(x =>
				x.AddSingleton<IServiceProviderFactory<Container>>(new SimpleInjectorServiceProviderFactory(container, options)));
			return target;
		}

		public static IWebHostBuilder UseSimpleInjectorWithViewSupport(this IWebHostBuilder target,
			Action<ContainerOptions> action = null, Action<ServiceProviderFactoryOptions> options = null)
		{
			target.ConfigureServices(x =>
				x.AddSingleton<IServiceProviderFactory<Container>>(
					new SimpleInjectorCompositeServiceProviderFactory(action, options)));
			return target;
		}

		public static IWebHostBuilder UseSimpleInjectorWithViewSupport(this IWebHostBuilder target,
			Container container,
			Action<ServiceProviderFactoryOptions> options = null)
		{
			target.ConfigureServices(x =>
				x.AddSingleton<IServiceProviderFactory<Container>>(
					new SimpleInjectorCompositeServiceProviderFactory(container, options)));
			return target;
		}
	}
}