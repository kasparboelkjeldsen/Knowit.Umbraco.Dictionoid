using System.Text.RegularExpressions;
using Knowit.Umbraco.Dictionoid.AiClients;
using Knowit.Umbraco.Dictionoid.AiClients.Configurations;
using Knowit.Umbraco.Dictionoid.Database;
using Knowit.Umbraco.Dictionoid.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;
using File = System.IO.File;

namespace Knowit.Umbraco.Dictionoid.Services;

/**
 * FIXME: Refactor this class
 *
 * This is doing a lot of things, and it's not clear what the main responsibility is.
 * It's doing dictionary operations, translation, caching, history, and frontend token validation.
 * It's also doing file operations and cleanups.
 *
 * The class is also using a lot of magic strings, nested if statements, null checks and a lot of comments,
 * which is a code smell.
 *
 * Suggestions:
 * This class should be refactored into multiple easy to read classes, each with a single responsibility.
 *
 */

public class DictionoidService : IDictionoidService
{
	private const string CacheKey = "DictionoidCache";

	private readonly IScopeProvider _scopeProvider;
	private readonly ILocalizationService _localizationService;
	private readonly IDictionaryRepository _dictionaryRepository;
	private readonly DictionoidConfiguration _configuration;
	private readonly IAppPolicyCache _appCache;
	private readonly IAiClient _aiClient;
	private readonly IDictionoidHistoryRepository _historyRepository;

	public DictionoidService(ILocalizationService localizationService,
		IDictionaryRepository dictionaryRepository,
		IOptions<DictionoidConfiguration> configuration,
		AppCaches appCaches,
		IAiClient aiClient,
		IDictionoidHistoryRepository historyRepository,
		IScopeProvider scopeProvider)
	{
		_localizationService = localizationService;
		_dictionaryRepository = dictionaryRepository;
		_configuration = configuration.Value;
		_appCache = appCaches.RuntimeCache;
		_aiClient = aiClient;
		_historyRepository = historyRepository;
		_scopeProvider = scopeProvider;
	}

	#region Cleanups

	public bool ShouldCleanup() => _configuration.CleanupInBackoffice;

	public async Task<Dictionary<string, List<string>>?> CleanupInspect(string rootPath)
	{
		if (!_configuration.CleanupInBackoffice)
			return null;

		_appCache.ClearByKey(CacheKey);

		// Define the base directory for views.
		var viewsFolder = Path.Combine(rootPath, "views");

		// Get all .cshtml files in the viewsFolder, including subdirectories.
		var cshtmlFiles = Directory.EnumerateFiles(viewsFolder, "*.cshtml", SearchOption.AllDirectories);

		// Cache the dictionary once outside the file loop
		var dictionary = CacheEntireDictionary();

		var changes = new Dictionary<string, List<string>>();

		foreach (var file in cshtmlFiles)
		{
			var content = await File.ReadAllTextAsync(file);
			var modified = false;

			if (!content.Contains("Umbraco.Dictionoid(")) continue;

			// Updated pattern to account for the @await and escaped quotes
			var pattern = @"@await Umbraco\.Dictionoid\(\s*""((?:[^""\\]|\\.)*)""\s*,\s*""((?:[^""\\]|\\.)*)""\s*\)";

			var matches = Regex.Matches(content, pattern);

			foreach (Match match in matches)
			{
				if (match.Groups.Count != 3) continue;

				var value = match.Groups[1].Value;
				var key = match.Groups[2].Value;
				var item = dictionary!.FirstOrDefault(f => f.Key == key);
				var replacement = $"@Umbraco.GetDictionaryValue(\"{key}\")";

				if (item is not null)
				{
					modified = true;
					content = Regex.Replace(content, Regex.Escape(match.Value), replacement);

					if (!changes.ContainsKey(file))
						changes.Add(file, new List<string>());

					changes[file].Add(key);
					continue;
				}

				using var scope = _scopeProvider.CreateScope();
				var languages = _localizationService.GetAllLanguages().ToList();

				var openAiContent = _configuration.DisableAi ? "" : await GetTranslationResult(value, languages);

				var success = UpdateDictionaryItems(key, languages, _localizationService, _dictionaryRepository, openAiContent);

				if (!success) continue;

				modified = true;
				content = Regex.Replace(content, Regex.Escape(match.Value), replacement);

				if (!changes.ContainsKey(file))
					changes.Add(file, new List<string>());

				changes[file].Add(key + " (created)");
			}

			if (modified)
				await File.WriteAllTextAsync(file, content);
		}

		return changes;
	}

	#endregion

	#region Dictionary operations

	public async Task<GroupedResults?> GetItemGroupedResults(string key, string fallBack)
	{
		List<CachedDictionaryItem>? results = CacheEntireDictionary();
		var groupedResults = GetGroupedResults(key, results);

		if (groupedResults is not null || string.IsNullOrEmpty(fallBack))
			return groupedResults;

		if (!_configuration.FrontendApi.TranslateOnMissing)
		{
			return new GroupedResults
			{
				key = key,
				id = -1,
				translations = new List<Translation>()
				{
					new Translation()
					{
						lang = "",
						text = fallBack
					}
				}
			};
		}

		using var scope = _scopeProvider.CreateScope();
		var languages = _localizationService.GetAllLanguages().ToList();
		var openAiContent = await GetTranslationResult(fallBack, languages);
		UpdateDictionaryItems(key, languages, _localizationService, _dictionaryRepository, openAiContent);

		_appCache.ClearByKey(CacheKey);
		results = CacheEntireDictionary();

		return GetGroupedResults(key, results);

	}

	public List<GroupedResults> GetItemsGroupedResult(string keyStartsWith)
	{
		List<CachedDictionaryItem>? results = CacheEntireDictionary();

		return GroupedResultsListByKeyStartsWith(keyStartsWith, results);
	}

	public bool UpdateDictionaryItems(string key, List<ILanguage> languages,
		ILocalizationService localizationService, IDictionaryRepository dictionaryRepository, string content)
	{
		var jObject = JObject.Parse(content);

		if (!string.IsNullOrEmpty(key) && key.Contains("."))
			return UpdateNestedDictionaryItems(key, languages, localizationService, dictionaryRepository, jObject);

		return UpdateSingleDictionaryItem(key, languages, localizationService, dictionaryRepository, jObject);
	}

	#endregion

	#region Translation

	public async Task<string> Translate(TranslationRequest request)
	{
		try
		{
			var messages = _aiClient.BuildPrompt(request);

			var result = await _aiClient.TranslateAsync(messages);

			return result.Choices.First().Message.TextContent;
		}
		catch
		{
			return string.Empty;
		}
	}

	public async Task<string> GetTranslationResult(string text, IEnumerable<ILanguage> languages)
	{
		if (_configuration.DisableAi) return "";
		var request = CreateTranslationRequest(text, languages);
		return await Translate(request);
	}

	private TranslationRequest CreateTranslationRequest(string text, IEnumerable<ILanguage> languages) =>
		new TranslationRequest
		{
			Color = "",
			DetectLanguage = text,
			Items = languages.Select(s => new TranslationItem { Key = s.CultureName, Value = "" }).ToList()
		};

	#endregion

	#region Caching

	public void ClearCache() => _appCache.ClearByKey(CacheKey);

	public List<CachedDictionaryItem>? CacheEntireDictionary()
	{
		var items = _historyRepository.GetEntireDictionary();
		return _appCache.GetCacheItem(CacheKey, () => items, TimeSpan.FromMinutes(2));
	}

	public IEnumerable<CachedDictionaryItem>? GetText(string key) => CacheEntireDictionary()?
		.Where(res => res.Key == key);

	#endregion

	#region History

	public List<DictionoidHistory>? GetDictionoidHistory(string key) => _historyRepository.GetHistoryByKey(key);

	public int ClearHistory() => _historyRepository.ClearHistory();

	#endregion

	#region Frontend token validation

	public bool HasValidToken(HttpRequest request)
	{
		if (!_configuration.FrontendApi.Expose) return false;

		// FIXME: this authentication mechanism is easy to bypass.
		// it should be replaced with a proper authentication mechanism

		// Extract the bearer token from the Authorization header
		string authHeader = request.Headers["Authorization"];
		if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
			return false;

		// Remove "Bearer " from the start of the string to get the actual token
		var token = authHeader.Substring("Bearer ".Length).Trim();

		// Compare the extracted token with the FrontendKey
		return _configuration.FrontendApi.Secret == token;
	}

	#endregion

	#region private utilities

	private bool UpdateNestedDictionaryItems(string key, List<ILanguage> languages, ILocalizationService localizationService, IDictionaryRepository dictionaryRepository, JObject content)
	{
		var keys = key.Split('.');
		string buildKey = string.Empty;
		Guid? parent = null;
		bool success = false;

		foreach (var k in keys)
		{
			buildKey = CombineKeys(buildKey, k);
			var dictionaryItemExists = localizationService.GetDictionaryItemByKey(buildKey);

			if (dictionaryItemExists is null)
			{
				var newItem = CreateAndSaveDictionaryItem(buildKey, parent, languages, content, localizationService, dictionaryRepository);
				parent = newItem?.Key;
				success = newItem != null;
			}
			else
			{
				parent = dictionaryItemExists.Key;
			}
		}

		return success;
	}

	private bool UpdateSingleDictionaryItem(string key, IEnumerable<ILanguage> languages,
		ILocalizationService localizationService, IDictionaryRepository dictionaryRepository, JObject content)
	{
		var newItem = localizationService.CreateDictionaryItemWithIdentity(key, null);
		if (newItem is null) return false;

		UpdateDictionaryItemValues(newItem, languages, content, localizationService);
		dictionaryRepository.Save(newItem);
		return true;
	}

	private string CombineKeys(string buildKey, string keyPart) => string.IsNullOrEmpty(buildKey) ? keyPart : $"{buildKey}.{keyPart}";

	private GroupedResults? GetGroupedResults(string key, List<CachedDictionaryItem>? results) =>
		results?
			.Where(res => res.Key == key)
			.GroupBy(res => new { res.Key, res.Pk })
			.Select(g => new GroupedResults()
			{
				key = g.Key.Key,
				id = g.Key.Pk,
				translations = g.Select(res => new Translation() { lang = res.LanguageISOCode, text = res.Value })
					.ToList()
			}).FirstOrDefault();

	private List<GroupedResults> GroupedResultsListByKeyStartsWith(string keyStartsWith,
		List<CachedDictionaryItem>? results) =>
		results?
			.Where(res => res.Key.StartsWith(keyStartsWith))
			.GroupBy(res => new { res.Key, res.Pk })
			.Select(g => new GroupedResults
			{
				key = g.Key.Key,
				id = g.Key.Pk,
				translations = g.Select(res => new Translation() { lang = res.LanguageISOCode, text = res.Value })
					.ToList()
			}).ToList() ?? new List<GroupedResults>(); // This will create a list of the grouped result objects

	private IDictionaryItem CreateAndSaveDictionaryItem(string key, Guid? parent,
		IEnumerable<ILanguage> languages, JObject content, ILocalizationService localizationService,
		IDictionaryRepository dictionaryRepository)
	{
		var newItem = localizationService.CreateDictionaryItemWithIdentity(key, parent);
		UpdateDictionaryItemValues(newItem, languages, content, localizationService);
		dictionaryRepository.Save(newItem);
		return newItem;
	}

	private void UpdateDictionaryItemValues(IDictionaryItem item, IEnumerable<ILanguage> languages,
		JObject content, ILocalizationService localizationService)
	{
		foreach (var lang in languages)
		{
			var value = content["Items"]?.FirstOrDefault(i => i["Key"].ToString() == lang.CultureName)?["Value"]?.ToString();
			if (value != null)
				localizationService.AddOrUpdateDictionaryValue(item, lang, value);
		}
	}

	public bool IsAiDisabled()
	{
		var s = _configuration.DisableAi;
		return _configuration.DisableAi;
	}

	#endregion
}