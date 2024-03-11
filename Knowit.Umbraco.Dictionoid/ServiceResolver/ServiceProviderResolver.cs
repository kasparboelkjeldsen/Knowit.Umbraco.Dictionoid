using System.Reflection;
using Umbraco.Cms.Web.Common.DependencyInjection;

namespace Knowit.Umbraco.Dictionoid.ServiceResolver
{
	/// <summary>
	/// StaticServiceProvider has been moved between Umbraco 11 and 12, so we need to do ugly things to ensure we can get it in both versions
	/// </summary>
	internal static class ServiceProviderHelper
	{
		internal static IServiceProvider GetServiceProviderInstance()
		{
			var serviceProviderInstance = GetServiceProviderFromNamespace("Umbraco.Cms.Core.DependencyInjection.StaticServiceProvider")
									   ?? GetServiceProviderFromNamespace("Umbraco.Cms.Web.Common.DependencyInjection.StaticServiceProvider");

			if (serviceProviderInstance == null)
			{
				throw new InvalidOperationException("StaticServiceProvider.Instance could not be found in any known namespaces.");
			}

			return serviceProviderInstance;
		}

		private static IServiceProvider GetServiceProviderFromNamespace(string typeFullName)
		{
			var assembly = AppDomain.CurrentDomain.GetAssemblies()
			.FirstOrDefault(a => a.GetTypes().Any(t => t.FullName == typeFullName));

			if (assembly == null)
			{
				return null;
			}
			var type = assembly.GetType(typeFullName);
			if (type == null)
			{
				return null;  // Type not found, possibly due to different Umbraco version
			}

			var instanceProperty = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
			if (instanceProperty == null)
			{
				return null;  // Property not found, unexpected, should consider logging or handling
			}

			return instanceProperty.GetValue(null) as IServiceProvider;
		}
	}
}
