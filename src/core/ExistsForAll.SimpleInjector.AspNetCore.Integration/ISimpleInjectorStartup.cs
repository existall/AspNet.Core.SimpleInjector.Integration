using SimpleInjector;

namespace ExistsForAll.SimpleInjector.AspNetCore.Integration
{
	public interface ISimpleInjectorStartup
	{
		void ConfigureContainer(Container container);
	}
}