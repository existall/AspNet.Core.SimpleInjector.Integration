namespace ExistsForAll.SimpleInjector.AspNetCore.Integration
{
	public class ServiceProviderFactoryOptions
	{
		public bool ValidateScope { get; set; } = false;
		public bool InclueViewComponents { get; set; } = true;
	}
}