using Knowit.Umbraco.Dictionoid.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common;

using Umbraco.Extensions;
using Newtonsoft.Json.Linq;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.Text.RegularExpressions;
using Knowit.Umbraco.Dictionoid.ServiceResolver;

namespace Knowit.Umbraco.Dictionoid.Helper
{
	public static class UmbracoHelperExtensions
	{
		/// <summary>
		/// Returns localized dictionary translation if it exists, and creates it when it doesn't.
		/// Will use OpenAI to translate into every language set up in Umbraco - results may vary.
		/// 
		/// Defaults to return "text" parameter if something goes wrong.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="text"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public async static Task<string?> Dictionoid(this UmbracoHelper helper, string text, string key)
		{
			if (string.IsNullOrEmpty(key)) return string.Empty;

			// check if key already exists, and use default behavior if it does
			var helperVal = helper.GetDictionaryValue(key);

			if (!string.IsNullOrEmpty(helperVal)) return helperVal;

			// time for crazy.
			// this is kinda dangerous, but how else do you get the required services in a static helper class?
			var services = GetRequiredServices();

			if (services == null || services.Value.Item1 == null) return text;

			var (localizationService, dictionaryRepository, scopeProvider, configuration, env, templating) = services.Value;

			// get configuration values
			var shouldCleanup = configuration.GetValue<bool?>("Knowit.Umbraco.Dictionoid:CleanupAfterCreate");
			var shouldCreate = configuration.GetValue<bool?>("Knowit.Umbraco.Dictionoid:CreateOnNotExist");

			// abort if we shouldn't make a new dictionary item
			if (!shouldCreate.HasValue || !shouldCreate.Value) return text;

			// get every language set up in Umbraco.
			var languages = localizationService.GetAllLanguages();

			// perform OpenAI translation
			var translationResult = await API.Dictionoid.GetTranslationResult(text, languages, configuration.GetValue<string>("Knowit.Umbraco.Dictionoid:ApiKey"));
			
			if (translationResult == null) return text;

			using (var scope = scopeProvider.CreateScope())
			{
				var success = API.Dictionoid.UpdateDictionaryItems(key, languages, localizationService, dictionaryRepository, translationResult);
				scope.Complete();
				if(shouldCleanup.HasValue && shouldCleanup.Value)
					DoCleanup(helper.AssignedContentItem.TemplateId, key, scopeProvider, templating, env);
				return success ? helper.GetDictionaryValue(key) : text;
			}
		}

		internal static void DoCleanup(int? templateId, string key, IScopeProvider scopeProvider, ITemplateRepository templateRepository, IWebHostEnvironment env)
		{
			using (var scope = scopeProvider.CreateScope())
			{
				var template = templateRepository.Get(templateId.Value);
				var path = (env.ContentRootPath + template.VirtualPath.Replace("/", "\\"));
				var text = System.IO.File.ReadAllText(path);

				// Define the regex pattern to match "@Umbraco.Dictionoid("SOMETEXT", "SOMEKEY")"
				string pattern = $@"Umbraco\.Dictionoid\(\s*""(.*?)"",\s*""{key}""\s*\)";

				// Replacement pattern now uses the "key" parameter
				string replacement = $"Umbraco.GetDictionaryValue(\"{key}\")";

				// Replace matched patterns in the input string
				string result = Regex.Replace(text, pattern, replacement);

				System.IO.File.WriteAllText(path, result);
			}
		}


		private static (ILocalizationService, IDictionaryRepository, IScopeProvider, IConfiguration, IWebHostEnvironment, ITemplateRepository)? GetRequiredServices()
		{
			var instance = ServiceProviderHelper.GetServiceProviderInstance();
			var localizationService = (ILocalizationService)instance.GetService(typeof(ILocalizationService));
			var dictionaryRepository = (IDictionaryRepository)instance.GetService(typeof(IDictionaryRepository));
			var scopeProvider = (IScopeProvider)instance.GetService(typeof(IScopeProvider));
			var configuration = (IConfiguration)instance.GetService(typeof(IConfiguration));
			var templateRepository = (ITemplateRepository)instance.GetService(typeof(ITemplateRepository));
			var env = (IWebHostEnvironment)instance.GetService(typeof(IWebHostEnvironment));

			return localizationService == null || dictionaryRepository == null || scopeProvider == null || configuration == null || templateRepository == null || env == null
				? (null, null, null, null, null, null)
				: (localizationService, dictionaryRepository, scopeProvider, configuration, env, templateRepository);
		}



	}

}
