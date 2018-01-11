using System;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using SimpleInjector.Integration.AspNetCore.Mvc;

namespace ExistsForAll.SimpleInjector.AspNetCore.Integration
{
	internal class SimpleInjectorCompositeServiceProviderFactory : IServiceProviderFactory<Container>
	{
		private readonly Action<ContainerOptions> _action;
		private readonly Action<ServiceProviderFactoryOptions> _serviceProviderFactoryOptionsAction;
		private readonly ServiceProviderFactoryOptions _serviceProviderFactoryOptions = new ServiceProviderFactoryOptions();
		
		public SimpleInjectorCompositeServiceProviderFactory(Action<ContainerOptions> action,
			Action<ServiceProviderFactoryOptions> serviceProviderFactoryOptionsAction)
		{
			_action = action;
			_serviceProviderFactoryOptionsAction = serviceProviderFactoryOptionsAction;
		}

		public SimpleInjectorCompositeServiceProviderFactory(Container container,
			Action<ServiceProviderFactoryOptions> serviceProviderFactoryOptionsAction)
		{
			_serviceProviderFactoryOptionsAction = serviceProviderFactoryOptionsAction;
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
			_serviceProviderFactoryOptionsAction?.Invoke(_serviceProviderFactoryOptions);
			
			Services.UseSimpleInjectorAspNetRequestScoping(container);

			Services.AddSingleton<IControllerActivator>(new SimpleInjectorControllerActivator(container));

			Services.AddSingleton<IViewComponentActivator>(new SimpleInjectorViewComponentActivator(container));

			Services.EnableSimpleInjectorCrossWiring(container);

			var defaultServiceProvider = Services.BuildServiceProvider(_serviceProviderFactoryOptions.ValidateScope);

			container.RegisterMvcControllers(new ApplicationBuilder(defaultServiceProvider));

			if(_serviceProviderFactoryOptions.InclueViewComponents)
				container.RegisterMvcViewComponents(new ApplicationBuilder(defaultServiceProvider));

			var compositeServiceProvider = new CompositeServiceProvider(defaultServiceProvider, Container);

			var compositeServiceScopeFactory =
				new CompositeServiceScopeFactory(container, defaultServiceProvider.GetService<IServiceScopeFactory>());

			container.RegisterSingleton<IServiceScopeFactory>(compositeServiceScopeFactory);

			container.ConfigureAutoCrossWiring(defaultServiceProvider, Services);

			return compositeServiceProvider;
		}
	}
}