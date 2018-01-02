using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;

namespace ExistsForAll.SimpleInjector.AspNetCore.Integration
{
	public static class WebHostBuilderExtensions
	{
		public static IWebHostBuilder UseSimpleInjector(this IWebHostBuilder target, Action<ContainerOptions> action)
		{
			target.ConfigureServices(x =>
				x.AddSingleton<IServiceProviderFactory<Container>>(new SimpleInjectorServiceProviderFactory(action)));
			return target;
		}

		public static IWebHostBuilder UseSimpleInjector(this IWebHostBuilder target, Container container)
		{
			target.ConfigureServices(x =>
				x.AddSingleton<IServiceProviderFactory<Container>>(new SimpleInjectorServiceProviderFactory(container)));
			return target;
		}

		public static IWebHostBuilder UseSimpleInjectorWithViewSupport(this IWebHostBuilder target, Action<ContainerOptions> action)
		{
			target.ConfigureServices(x =>
				x.AddSingleton<IServiceProviderFactory<Container>>(new SimpleInjectorCompositeServiceProviderFactory(action)));
			return target;
		}

		public static IWebHostBuilder UseSimpleInjectorWithViewSupport(this IWebHostBuilder target, Container container)
		{
			target.ConfigureServices(x =>
				x.AddSingleton<IServiceProviderFactory<Container>>(new SimpleInjectorCompositeServiceProviderFactory(container)));
			return target;
		}
	}
}
