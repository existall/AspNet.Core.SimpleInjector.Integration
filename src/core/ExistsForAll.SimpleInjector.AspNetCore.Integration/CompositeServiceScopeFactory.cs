using System;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;

namespace ExistsForAll.SimpleInjector.AspNetCore.Integration
{
	internal sealed class SimpleInjectorScopeFactory : IServiceScopeFactory
	{
		private readonly Container _container;

		public SimpleInjectorScopeFactory(Container container)
		{
			_container = container;
		}

		public IServiceScope CreateScope()
		{
			return new SimpleInjectorServiceScope(_container.GetInstance<IServiceProvider>());
		}

		private class SimpleInjectorServiceScope : IServiceScope
		{
			public SimpleInjectorServiceScope(IServiceProvider serviceProvider)
			{
				ServiceProvider = serviceProvider;
			}

			public void Dispose()
			{
				
			}

			public IServiceProvider ServiceProvider { get; }
		}
	}

	internal sealed class CompositeServiceScopeFactory : IServiceScopeFactory
	{
		private readonly Container _container;
		private readonly IServiceScopeFactory _defaultServiceScopeFactory;

		public CompositeServiceScopeFactory(Container container, IServiceScopeFactory defaultServiceScopeFactory)
		{
			_container = container;
			_defaultServiceScopeFactory = defaultServiceScopeFactory;
		}

		public IServiceScope CreateScope()
		{
			return new CompositeServiceScope(_container, _defaultServiceScopeFactory.CreateScope());
		}

		private class CompositeServiceScope : IServiceScope
		{
			private readonly IServiceScope _defaultServiceScope;

			public CompositeServiceScope(Container container, IServiceScope defaultServiceScope)
			{
				// for scoping we want to provide MS child container with simple injector.
				ServiceProvider = new CompositeServiceProvider(defaultServiceScope.ServiceProvider, container);
				_defaultServiceScope = defaultServiceScope;
			}

			public IServiceProvider ServiceProvider { get; }

			public void Dispose()
			{
				_defaultServiceScope.Dispose();
			}
		}
	}
}