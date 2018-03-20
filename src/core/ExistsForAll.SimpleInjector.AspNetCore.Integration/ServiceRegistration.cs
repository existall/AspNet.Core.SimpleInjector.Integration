using System;
using SimpleInjector;

namespace ExistsForAll.SimpleInjector.AspNetCore.Integration
{
	public class ServiceRegistration
	{
		public Type ServiceType { get; set; }

		public virtual Type ImplementingType { get; set; }

		public Func<object> FactoryExpression { get; set; }
		
		public string ServiceName { get; set; }

		public Lifestyle Lifestyle { get; set; }

		public object Value { get; set; }

		public Action RegistrationMethod { get; set; }
		
		public override int GetHashCode()
		{
			return this.ServiceType.GetHashCode() ^ this.ServiceName.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (!(obj is ServiceRegistration serviceRegistration) || ServiceName != serviceRegistration.ServiceName)
				return false;
			
			return this.ServiceType == serviceRegistration.ServiceType;
		}

		public override string ToString()
		{
			Lifestyle lifetime = Lifestyle;
			return string.Format("ServiceType: '{0}', ServiceName: '{1}', ImplementingType: '{2}', Lifetime: '{3}'", (object) this.ServiceType, (object) this.ServiceName, (object) this.ImplementingType, (object) ((lifetime != null ? lifetime.ToString() : (string) null) ?? "Transient"));
		}
	}
}