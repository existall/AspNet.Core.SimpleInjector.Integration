using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using SimpleInjector.Advanced;
using SimpleInjector.Integration.AspNetCore.Mvc;

namespace ExistsForAll.SimpleInjector.AspNetCore.Integration
{
	internal class SimpleInjectorServiceProviderFactory : IServiceProviderFactory<Container>
	{
		private readonly ServiceProviderFactoryOptions _serviceProviderFactoryOptions = new ServiceProviderFactoryOptions();

		private readonly Action<ContainerOptions> _action;
		private readonly Action<ServiceProviderFactoryOptions> _serviceProviderFactoryOptionsAction;

		public SimpleInjectorServiceProviderFactory(Action<ContainerOptions> action,
			Action<ServiceProviderFactoryOptions> serviceProviderFactoryOptionsAction)
		{
			_action = action;
			_serviceProviderFactoryOptionsAction = serviceProviderFactoryOptionsAction;
		}

		public SimpleInjectorServiceProviderFactory(Container container,
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
			//var serviceDescriptors = Services.Where(x => x.ServiceType.Name.Contains("ITele"));

			Container.Options.ConstructorResolutionBehavior =
				new MostResolvableParametersConstructorResolutionBehavior(Container);

			Services.UseSimpleInjectorAspNetRequestScoping(container);

			Container.Register<IServiceProvider>(() => new SimpleInjectorServiceProvider(Container), Lifestyle.Singleton);
			container.Register<IServiceScopeFactory>(() => new SimpleInjectorScopeFactory(container), Lifestyle.Singleton);

			RegisterServices(Services);

			var defaultServiceProvider1 = Services.BuildServiceProvider(_serviceProviderFactoryOptions.ValidateScope);

			var serviceDescriptors = Services.Where(x=>x.ServiceType.Name.Contains("Form"));

			container.ResolveUnregisteredType += Container_ResolveUnregisteredType;

			var service = defaultServiceProvider1.GetService(typeof(FormOptions));

			Container.Verify();


			_serviceProviderFactoryOptionsAction?.Invoke(_serviceProviderFactoryOptions);

			Services.UseSimpleInjectorAspNetRequestScoping(container);

			Services.AddSingleton<IControllerActivator>(new SimpleInjectorControllerActivator(container));

			if (_serviceProviderFactoryOptions.InclueViewComponents)
				Services.AddSingleton<IViewComponentActivator>(new SimpleInjectorViewComponentActivator(container));

			Services.EnableSimpleInjectorCrossWiring(container);

			var defaultServiceProvider = Services.BuildServiceProvider(_serviceProviderFactoryOptions.ValidateScope);

			container.RegisterMvcControllers(new ApplicationBuilder(defaultServiceProvider));

			if (_serviceProviderFactoryOptions.InclueViewComponents)
				container.RegisterMvcViewComponents(new ApplicationBuilder(defaultServiceProvider));

			container.ConfigureAutoCrossWiring(defaultServiceProvider, Services);

			return defaultServiceProvider;
		}

		private void Container_ResolveUnregisteredType(object sender, UnregisteredTypeEventArgs e)
		{
			if(e.Handled)
				return;

			var serviceType = e.UnregisteredServiceType;

			if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
			{
				var elementType = serviceType.GetGenericArguments().Single();
				var producer = Container.GetRegistration(elementType);

				if (producer == null)
					return;

				var castMethod = typeof(Enumerable)
					.GetMethod("Cast")
					.MakeGenericMethod(elementType);

				object stream = new[] { producer.GetInstance() }.Select(x => x);

				stream = castMethod.Invoke(null, new[] { stream });

				e.Register(producer.Lifestyle.CreateRegistration(serviceType, () => stream, Container));
			}

			var eUnregisteredServiceType = e.UnregisteredServiceType;
		}

		private void RegisterServices(IServiceCollection serviceCollection)
		{
			var list = new HashSet<Type>();

			var registrations = serviceCollection.Select(CreateServiceRegistration).ToArray();

			var groupedRegistrations = registrations.GroupBy(sr => sr.ServiceType);

			foreach (var groupedRegistration in groupedRegistrations)
			{
				if (groupedRegistration.Key.Name.Contains("IPostConfigureOptions"))
				{

				}

				if (!groupedRegistration.Key.GetTypeInfo().IsGenericTypeDefinition && groupedRegistration.Count() > 1)
				{
					Container.RegisterCollection(groupedRegistration.Key, groupedRegistration.Select(x => x.ServiceType));
					list.Add(groupedRegistration.Key);
				}
			}


			foreach (var registration in registrations.Where(x => !list.Contains(x.ServiceType)))
			{
				if (registration.ServiceType.Name.Contains("IPostConfigureOptions"))
				{

				}

				registration.RegistrationMethod.Invoke();
			}
		}


		private ServiceRegistration CreateServiceRegistration(ServiceDescriptor serviceDescriptor)
		{
			if (serviceDescriptor.ImplementationFactory != null)
			{
				return CreateServiceRegistrationForFactoryDelegate(serviceDescriptor);
			}

			if (serviceDescriptor.ImplementationInstance != null)
			{
				return CreateServiceRegistrationForInstance(serviceDescriptor);
			}

			return CreateServiceRegistrationServiceType(serviceDescriptor);
		}

		private ServiceRegistration CreateServiceRegistrationServiceType(ServiceDescriptor serviceDescriptor)
		{
			ServiceRegistration registration = CreateBasicServiceRegistration(serviceDescriptor);

			registration.ImplementingType = serviceDescriptor.ImplementationType;

			//var reg = registration.Lifestyle.CreateRegistration(registration.ImplementingType, Container);

			//Container.AddRegistration(registration.ServiceType,reg);

			registration.RegistrationMethod = () => Container.RegisterConditional(registration.ServiceType,
				registration.ImplementingType, registration.Lifestyle, c => !c.Handled);

			return registration;
		}

		private ServiceRegistration CreateServiceRegistrationForInstance(ServiceDescriptor serviceDescriptor)
		{
			var registration = CreateBasicServiceRegistration(serviceDescriptor);

			registration.Value = serviceDescriptor.ImplementationInstance;

			var reg = registration.Lifestyle.CreateRegistration(registration.ServiceType, () => registration.Value, Container);

			registration.RegistrationMethod = () => Container.AddRegistration(registration.ServiceType, reg);

			return registration;
		}

		private ServiceRegistration CreateServiceRegistrationForFactoryDelegate(ServiceDescriptor serviceDescriptor)
		{
			var registration = CreateBasicServiceRegistration(serviceDescriptor);

			registration.FactoryExpression = CreateTypedFactoryDelegate(serviceDescriptor);

			var t = registration.Lifestyle.CreateRegistration(registration.ServiceType, registration.FactoryExpression,
				Container);

			registration.RegistrationMethod = () => Container.AddRegistration(registration.ServiceType, t);

			return registration;
		}

		private static ServiceRegistration CreateBasicServiceRegistration(ServiceDescriptor serviceDescriptor)
		{
			var registration = new ServiceRegistration
			{
				Lifestyle = ResolveLifestyle(serviceDescriptor),
				ServiceType = serviceDescriptor.ServiceType,
				ServiceName = Guid.NewGuid().ToString()
			};

			return registration;
		}

		private static Lifestyle ResolveLifestyle(ServiceDescriptor serviceDescriptor)
		{
			switch (serviceDescriptor.Lifetime)
			{
				case ServiceLifetime.Singleton: return Lifestyle.Singleton;
				case ServiceLifetime.Scoped: return Lifestyle.Scoped;
				default: return Lifestyle.Transient;
			}
		}

		private static Func<object> CreateFactoryDelegate(ServiceDescriptor serviceDescriptor)
		{
			var openGenericMethod = typeof(SimpleInjectorServiceProviderFactory)
				.GetTypeInfo()
				.GetDeclaredMethod("CreateTypedFactoryDelegate");

			var closedGenericMethod = openGenericMethod.MakeGenericMethod(serviceDescriptor.ServiceType);

			return (Func<object>) closedGenericMethod.Invoke(null, new object[] {serviceDescriptor});
		}

		private Func<object> CreateTypedFactoryDelegate(ServiceDescriptor serviceDescriptor)
		{
			return () => serviceDescriptor.ImplementationFactory(Container.GetInstance<IServiceProvider>());
		}
	}

	public class MostResolvableParametersConstructorResolutionBehavior
		: IConstructorResolutionBehavior
	{
		private readonly Container container;

		public MostResolvableParametersConstructorResolutionBehavior(Container container)
		{
			this.container = container;
		}

		private bool IsCalledDuringRegistrationPhase => !container.IsLocked();

		public ConstructorInfo GetConstructor(Type implementationType)
		{
			var constructor = this.GetConstructors(implementationType).FirstOrDefault();
			if (constructor != null) return constructor;
			throw new ActivationException(BuildExceptionMessage(implementationType));
		}

		private IEnumerable<ConstructorInfo> GetConstructors(Type implementation)
		{

			if (implementation.Name.Contains("PostConfigureOptions"))
			{

			}

			var t = from ctor in implementation.GetConstructors()
				let parameters = ctor.GetParameters()
				where IsCalledDuringRegistrationPhase || implementation.GetConstructors().Length == 1 || ctor.GetParameters().All(CanBeResolved)
				orderby parameters.Length descending
				select ctor;

			return t;
		}


		private bool CanBeResolved(ParameterInfo parameter) =>
			this.GetInstanceProducerFor(new InjectionConsumerInfo(parameter)) != null;

		private InstanceProducer GetInstanceProducerFor(InjectionConsumerInfo i)
		{
			var instanceProducer = this.container.Options.DependencyInjectionBehavior.GetInstanceProducer(i, false);
			return instanceProducer;
		}
			

		private static string BuildExceptionMessage(Type type) =>
			!type.GetConstructors().Any()
				? TypeShouldHaveAtLeastOnePublicConstructor(type)
				: TypeShouldHaveConstructorWithResolvableTypes(type);

		private static string TypeShouldHaveAtLeastOnePublicConstructor(Type type) =>
			string.Format(CultureInfo.InvariantCulture,
				"For the container to be able to create {0}, it should contain at least " +
				"one public constructor.", type.ToFriendlyName());

		private static string TypeShouldHaveConstructorWithResolvableTypes(Type type) =>
			string.Format(CultureInfo.InvariantCulture,
				"For the container to be able to create {0}, it should contain a public " +
				"constructor that only contains parameters that can be resolved.",
				type.ToFriendlyName());
	}

	internal class SimpleInjectorServiceProvider : IServiceProvider
	{
		private readonly Container _container;

		public SimpleInjectorServiceProvider(Container container)
		{
			_container = container;
		}

		public object GetService(Type serviceType)
		{
			return _container.GetInstance(serviceType);
		}
	}
}