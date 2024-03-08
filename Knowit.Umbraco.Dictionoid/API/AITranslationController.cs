using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.BackOffice.Controllers;
using System.Text.Json;
using System.Text;
using Umbraco.Cms.Web.Common.Controllers;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using OpenAI_API.Chat;
using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Web.Common.Authorization;
using Org.BouncyCastle.Asn1;
using Microsoft.Extensions.Hosting;
using NPoco.fastJSON;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.NetworkInformation;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Infrastructure.Scoping;
using MimeKit.Cryptography;
using System.Globalization;
using Umbraco.Cms.Core.Cache;
using Umbraco.Extensions;
using static System.Formats.Asn1.AsnWriter;
using Umbraco.Cms.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Scoping;
using IScopeProvider = Umbraco.Cms.Infrastructure.Scoping.IScopeProvider;
using NPoco;
using Knowit.Umbraco.Dictionoid.DTO;
using Umbraco.Cms.Core.Models.Membership;

namespace Knowit.Umbraco.Dictionoid.API
{
    
    public class AITranslationController : UmbracoApiController
    {
        private readonly string? ApiKey = string.Empty;
        private readonly bool Cleanup = false;
        private const string Endpoint = "https://api.openai.com/v1/chat/completions";

        private readonly IUmbracoDatabaseFactory _umbracoDatabaseFactory;
        private readonly IAppPolicyCache _appCache;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
		private readonly ILocalizationService _localizationService;
		private readonly IDictionaryRepository _dictionaryRepository;
		private readonly IScopeProvider _scopeProvider;
		public AITranslationController(
			IUmbracoDatabaseFactory umbracoDatabaseFactory, 
			AppCaches appCaches, 
			IConfiguration configuration, 
			IWebHostEnvironment webHostEnvironment,
			ILocalizationService localizationService,
			IDictionaryRepository dictionaryRepository,
			IScopeProvider scopeProvider			
			) {
            _umbracoDatabaseFactory = umbracoDatabaseFactory;
            _appCache = appCaches.RuntimeCache;
            _configuration = configuration;
			_localizationService = localizationService;
			_dictionaryRepository = dictionaryRepository;
			_scopeProvider = scopeProvider;
			ApiKey = _configuration.GetValue<string>("Knowit.Umbraco.Dictionoid:ApiKey");

            var cleanup = _configuration.GetValue<bool?>("Knowit.Umbraco.Dictionoid:CleanupInBackoffice");
            
            if (cleanup.HasValue)
                Cleanup = cleanup.Value;

			_webHostEnvironment = webHostEnvironment;

		}

        [HttpGet("umbraco/backoffice/dictionoid/test")]
        public IActionResult Test()
        {
            return Ok("HEJ!");
        }

        [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
        [HttpPost("umbraco/backoffice/dictionoid/translate")]
        public async Task<IActionResult> Translate([FromBody] TranslationRequest request)
		{
			ChatResult result = await Dictionoid.GPTTranslate(request, ApiKey!);
			if (result != null && result.Choices.Count > 0)
				return Ok(result.Choices.First().Message.TextContent);
			else return Ok();
		}

		private List<dynamic>? CacheEntireDictionary()
		{
			return _appCache.GetCacheItem("allDictionaryItems", () =>
			{
				using (var scope = _umbracoDatabaseFactory.CreateDatabase())
				{
					var sql = @"
            SELECT i.[key], i.[pk], l.languageIsoCode, l.languageCultureName, d.[value]
            FROM cmsLanguageText d
            JOIN umbracoLanguage l ON d.languageId = l.id
            JOIN cmsDictionary i ON i.id = d.uniqueId
            "
					;

					return scope.Fetch<dynamic>(sql);
				}
			}, TimeSpan.FromMinutes(2));
		}

		[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
		[HttpGet("umbraco/backoffice/dictionoid/clearcache")]
        public async Task<IActionResult> ClearCache()
        {
            _appCache.ClearByKey("allDictionaryItems");
            return Ok();
        }

        [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
        [HttpGet("umbraco/backoffice/dictionoid/gettext")]
        public async Task<IActionResult> GetText(string key)
		{
			List<dynamic>? results = CacheEntireDictionary();

			var result = from res in results where res.key == key select res;

			return Ok(result);
		}

		[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
		[HttpGet("umbraco/backoffice/dictionoid/shouldcleanup")]
		public async Task<IActionResult> ShouldCleanup()
		{
			return Ok(Cleanup);
		}

		[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
		[HttpGet("umbraco/backoffice/dictionoid/cleanupinspect")]
        public async Task<IActionResult> CleanupInspect()
        {
            if (!Cleanup) 
				return Ok(false);
			_appCache.ClearByKey("allDictionaryItems");
			// Define the base directory for views.
			var viewsFolder = Path.Combine(_webHostEnvironment.ContentRootPath, "views");

			
			// Get all .cshtml files in the viewsFolder, including subdirectories.
			var cshtmlFiles = Directory.EnumerateFiles(viewsFolder, "*.cshtml", SearchOption.AllDirectories);

			// Cache the dictionary once outside the file loop
			var dictionary = CacheEntireDictionary();

			Dictionary<string, List<string>> changes = new Dictionary<string, List<string>>();

			foreach (var file in cshtmlFiles)
			{
				var content = System.IO.File.ReadAllText(file);
				bool modified = false;

				if (content.Contains("Umbraco.Dictionoid("))
				{
					// Updated pattern to account for the @await and escaped quotes
					string pattern = @"@await Umbraco\.Dictionoid\(\s*""((?:[^""\\]|\\.)*)""\s*,\s*""((?:[^""\\]|\\.)*)""\s*\)";

					var matches = Regex.Matches(content, pattern);

					foreach (Match match in matches)
					{
						if (match.Groups.Count == 3)
						{
							string value = match.Groups[1].Value;
							string key = match.Groups[2].Value;
							var item = dictionary!.FirstOrDefault(f => f.key == key);

							if (item != null)
							{
								modified = true;
								string replacement = $"@Umbraco.GetDictionaryValue(\"{key}\")";
								content = Regex.Replace(content, Regex.Escape(match.Value), replacement);

								if(!changes.ContainsKey(file))
								{
									changes.Add(file, new List<string>());
								}
								changes[file].Add(key);
							}
							else
							{
								using (var scope = _scopeProvider.CreateScope())
								{
									var languages = _localizationService.GetAllLanguages();
									var openAIContent = await Dictionoid.GetTranslationResult(value, languages, ApiKey);
									var success = Dictionoid.UpdateDictionaryItems(key, languages, _localizationService, _dictionaryRepository, openAIContent);

									if (success)
									{
										modified = true;
										string replacement = $"@Umbraco.GetDictionaryValue(\"{key}\")";
										content = Regex.Replace(content, Regex.Escape(match.Value), replacement);
										if (!changes.ContainsKey(file))
										{
											changes.Add(file, new List<string>());
										}
										changes[file].Add(key + " (created)");
									}
								}
							}
						}
					}

					if (modified)
					{
						System.IO.File.WriteAllText(file, content);
					}
				}
			}

			return Ok(changes);
        }

		[HttpGet("umbraco/backoffice/dictionoid/history")]
		public async Task<IActionResult> GetDictionidHistory(string key)
		{
			using(var scope = _scopeProvider.CreateScope())
			{
				var history = scope.Database.Fetch<DictionoidHistory>("select * from KnowitDictionoidHistory where key = @key", new { key = key });
				return Ok(history);
			}
		}

		[HttpGet("umbraco/backoffice/dictionoid/clearhistory")]
		public async Task<IActionResult> ClearHistory()
		{
			using (var scope = _umbracoDatabaseFactory.CreateDatabase())
			{
 				var affected = scope.Execute("delete from KnowitDictionoidHistory");
				return Ok(affected);
			}
		}
	}
    public class TranslationItem
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
    }

    public class TranslationRequest
    {
        public string? Color { get; set; }
        public string? DetectLanguage { get; set;}
        public List<TranslationItem>? Items { get; set; }
    }
}
