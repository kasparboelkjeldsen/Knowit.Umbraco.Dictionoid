using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Infrastructure.Scoping;
using Microsoft.AspNetCore.Hosting;
using System.Text.RegularExpressions;
using Knowit.Umbraco.Dictionoid.AiClients.Configurations;
using Knowit.Umbraco.Dictionoid.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Web.Common.DependencyInjection;

namespace Knowit.Umbraco.Dictionoid.Extensions
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
        public static async Task<string?> Dictionoid(this UmbracoHelper helper, string text, string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            // check if key already exists, and use default behavior if it does
            var helperVal = helper.GetDictionaryValue(key);

            if (!string.IsNullOrEmpty(helperVal)) return helperVal;

            // time for crazy.
            // this is kinda dangerous, but how else do you get the required services in a static helper class?
            var services = GetRequiredServices();

            if (services is null) return text;

            var (localizationService,
                dictionaryRepository,
                scopeProvider,
                configuration,
                env,
                templating,
                dictionoidService) = services.Value;

            // get configuration values
            var shouldCleanup = configuration!.CleanupAfterCreate;
            var shouldCreate = configuration.CreateOnNotExist;

            // abort if we shouldn't make a new dictionary item
            if (!shouldCreate) return text;

            // get every language set up in Umbraco.
            var languages = localizationService.GetAllLanguages().ToList();

            // perform OpenAI translation
            var translationResult = await dictionoidService.GetTranslationResult(text, languages);

            if (string.IsNullOrEmpty(translationResult)) return text;

            using var scope = scopeProvider.CreateScope();
            var success = dictionoidService.UpdateDictionaryItems(key, languages, localizationService,
                dictionaryRepository, translationResult);
            scope.Complete();
            if (shouldCleanup)
                DoCleanup(helper.AssignedContentItem.TemplateId, key, scopeProvider, templating, env);
            return success ? helper.GetDictionaryValue(key) : text;
        }

        private static void DoCleanup(int? templateId, string key, IScopeProvider scopeProvider,
            ITemplateRepository templateRepository, IWebHostEnvironment env)
        {
            using var scope = scopeProvider.CreateScope();
            var template = templateRepository.Get(templateId.Value);
            var path = (env.ContentRootPath + template.VirtualPath.Replace("/", "\\"));
            var text = File.ReadAllText(path);

            // Define the regex pattern to match "@Umbraco.Dictionoid("SOMETEXT", "SOMEKEY")"
            string pattern = $@"Umbraco\.Dictionoid\(\s*""(.*?)"",\s*""{key}""\s*\)";

            // Replacement pattern now uses the "key" parameter
            string replacement = $"Umbraco.GetDictionaryValue(\"{key}\")";

            // Replace matched patterns in the input string
            string result = Regex.Replace(text, pattern, replacement);

            File.WriteAllText(path, result);
        }


        private static (ILocalizationService, IDictionaryRepository, IScopeProvider, DictionoidConfiguration?, IWebHostEnvironment, ITemplateRepository, IDictionoidService)? GetRequiredServices()
        {
            var instance = StaticServiceProvider.Instance;
            var localizationService = instance.GetServices<ILocalizationService>().FirstOrDefault();
            var dictionaryRepository = instance.GetServices<IDictionaryRepository>().FirstOrDefault();
            var scopeProvider = instance.GetServices<IScopeProvider>().FirstOrDefault();
            var configuration = instance.GetServices<IOptions<DictionoidConfiguration>>().FirstOrDefault()?.Value;
            var templateRepository = instance.GetServices<ITemplateRepository>().FirstOrDefault();
            var env = instance.GetServices<IWebHostEnvironment>().FirstOrDefault();
            var dictionoidService = instance.GetServices<IDictionoidService>().FirstOrDefault();

            var result = (localizationService, dictionaryRepository, scopeProvider, configuration, env, templateRepository, dictionoidService);
            var hasNull = result.GetType().GetProperties().Any(p => p.GetValue(result) is null);

            return hasNull ? null : result!;
        }
    }
}