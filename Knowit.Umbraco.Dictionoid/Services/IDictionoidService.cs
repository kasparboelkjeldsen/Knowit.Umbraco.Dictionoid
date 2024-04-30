using Knowit.Umbraco.Dictionoid.Database;
using Knowit.Umbraco.Dictionoid.Models;
using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Services;

namespace Knowit.Umbraco.Dictionoid.Services;

public interface IDictionoidService
{
    Task<GroupedResults?> GetItemGroupedResults(string key, string fallBack);
    List<GroupedResults> GetItemsGroupedResult(string keyStartsWith);

    bool HasValidToken(HttpRequest request);
    Task<string> Translate(TranslationRequest request);
    void ClearCache();
    IEnumerable<CachedDictionaryItem>? GetText(string key);
    bool ShouldCleanup();
    Task<Dictionary<string, List<string>>?> CleanupInspect(string rootPath);
    List<DictionoidHistory>? GetDictionoidHistory(string key);
    int ClearHistory();
    Task<string> GetTranslationResult(string text, IEnumerable<ILanguage> languages);

    bool UpdateDictionaryItems(string key, List<ILanguage> languages,
        ILocalizationService localizationService, IDictionaryRepository dictionaryRepository, string content);
    List<CachedDictionaryItem>? CacheEntireDictionary();
}