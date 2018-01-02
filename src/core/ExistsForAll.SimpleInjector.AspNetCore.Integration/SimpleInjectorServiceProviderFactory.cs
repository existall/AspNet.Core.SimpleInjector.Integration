using System;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using SimpleInjector.Integration.AspNetCore.Mvc;

namespace ExistsForAll.SimpleInjector.AspNetCore.Integration
{
	internal class SimpleInjectorServiceProviderFactory : IServiceProviderFactory<Container>
	{
		private readonly Action<ContainerOptions> _action;

		public SimpleInjectorServiceProviderFactory(Action<ContainerOptions> action)
		{
			_action = action;
		}

		public SimpleInjectorServiceProviderFactory(Container container)
		{
			Container = container;
		}

		private Container Container { get; set; }

		private IServiceCollection Services { get; set; }

		public Container CreateBuilder(IServiceCollection services)
		{
			Container = Container ?? new Container();
			_action?.Invoke(Container.Options);
			Services = services;
			return Container;
		}

		public IServiceProvider CreateServiceProvider(Container container)
		{
			Services.UseSimpleInjectorAspNetRequestScoping(container);

			Services.AddSingleton<IControllerActivator>(new SimpleInjectorControllerActivator(container));

			Services.AddSingleton<IViewComponentActivator>(new SimpleInjectorViewComponentActivator(container));

			Services.EnableSimpleInjectorCrossWiring(container);

			var defaultServiceProvider = Services.BuildServiceProvider();

			container.RegisterMvcControllers(new ApplicationBuilder(defaultServiceProvider));

			container.RegisterMvcViewComponents(new ApplicationBuilder(defaultServiceProvider));

			container.ConfigureAutoCrossWiring(defaultServiceProvider, Services);

			return defaultServiceProvider;
		}
	}
}