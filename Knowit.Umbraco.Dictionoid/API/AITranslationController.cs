using Knowit.Umbraco.Dictionoid.DTO;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OpenAI_API.Chat;
using System.Text.RegularExpressions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;
using IScopeProvider = Umbraco.Cms.Infrastructure.Scoping.IScopeProvider;

namespace Knowit.Umbraco.Dictionoid.API
{

	public class AITranslationController : UmbracoApiController
    {
        private readonly string? ApiKey = string.Empty;
        private readonly bool Cleanup = false;
		private readonly bool ExposeApi = false;
		private readonly string? FrontendKey = string.Empty;
		private readonly bool FrontendTranslateOnMissing = false;
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
            var expose = _configuration.GetValue<bool?>("Knowit.Umbraco.Dictionoid:FrontendApi:Expose");
            if (cleanup.HasValue)
                Cleanup = cleanup.Value;

			if (expose.HasValue)
			{
				ExposeApi = expose.Value;
				FrontendKey = _configuration.GetValue<string?>("Knowit.Umbraco.Dictionoid:FrontendApi:Secret");
                var translateOnMissing = _configuration.GetValue<bool?>("Knowit.Umbraco.Dictionoid:FrontendApi:TranslateOnMissing");
				if(translateOnMissing.HasValue)
				{
					FrontendTranslateOnMissing = translateOnMissing.Value;
                 }
            }

			_webHostEnvironment = webHostEnvironment;

		}

		[HttpGet("umbraco/api/dictionoid/item")]
		public async Task<IActionResult> Item(string key, string fallBack)
        {
            if (!ExposeApi) return NotFound();

            // Extract the bearer token from the Authorization header
            string authHeader = HttpContext.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return BadRequest("Missing or invalid authorization header.");

            // Remove "Bearer " from the start of the string to get the actual token
            var token = authHeader.Substring("Bearer ".Length).Trim();

            // Compare the extracted token with the FrontendKey
            if (FrontendKey != token)
                return BadRequest("Invalid token.");

            List<dynamic>? results = CacheEntireDictionary();
            dynamic? groupedResults = Dictionoid.GetGroupedResults(key, results);

            if (groupedResults == null && !string.IsNullOrEmpty(fallBack))
            {
                if (!FrontendTranslateOnMissing)
                    return Ok(new
                    {
                        key = key,
                        id = -1,
                        transations = new dynamic[]
                            {
                                new {
                                    lang = "",
                                    text = fallBack
                                }
                            }
                    });
                else
                {
                    using (var scope = _scopeProvider.CreateScope())
                    {
                        var languages = _localizationService.GetAllLanguages();
                        var openAIContent = await Dictionoid.GetTranslationResult(fallBack, languages, ApiKey);
                        var success = Dictionoid.UpdateDictionaryItems(key, languages, _localizationService, _dictionaryRepository, openAIContent);
                    }

                    _appCache.ClearByKey("allDictionaryItems");
                    results = CacheEntireDictionary();

                    groupedResults = Dictionoid.GetGroupedResults(key, results);
                    return Ok(groupedResults ?? null);
                }
            }
            // If no results are found for the given key, return an empty structure
            else return Ok(groupedResults ?? null);
        }

        

        [HttpGet("umbraco/api/dictionoid/items")]
        public IActionResult Items(string keyStartsWith)
        {
            if (!ExposeApi) return NotFound();

            // Extract the bearer token from the Authorization header
            string authHeader = HttpContext.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return BadRequest("Missing or invalid authorization header.");

            // Remove "Bearer " from the start of the string to get the actual token
            var token = authHeader.Substring("Bearer ".Length).Trim();

            // Compare the extracted token with the FrontendKey
            if (FrontendKey != token)
                return BadRequest("Invalid token.");

            List<dynamic>? results = CacheEntireDictionary();

            // Filter and group the results where keys start with the provided argument
            var groupedResults = results
                .Where(res => res.key != null && res.key.StartsWith(keyStartsWith))
                .GroupBy(res => new { res.key, res.pk })
                .Select(g => new {
                    key = g.Key.key,
                    id = g.Key.pk,
                    translations = g.Select(res => new { lang = res.languageISOCode, text = res.value }).ToList()
                }).ToList(); // This will create a list of the grouped result objects

            // If no results are found that match the criteria, return an empty list
            return Ok(groupedResults.Any() ? groupedResults : new List<object>());
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

		[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
		[HttpGet("umbraco/backoffice/dictionoid/history")]
		public async Task<IActionResult> GetDictionidHistory(string key)
		{
			using(var scope = _scopeProvider.CreateScope())
			{
				var history = scope.Database.Fetch<DictionoidHistory>("select * from KnowitDictionoidHistory where key = @key", new { key = key });
				return Ok(history);
			}
		}
		[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
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
