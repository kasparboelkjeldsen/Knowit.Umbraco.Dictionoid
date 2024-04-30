using Knowit.Umbraco.Dictionoid.Models;

namespace Knowit.Umbraco.Dictionoid.Database;

public interface IDictionoidHistoryRepository
{
    List<DictionoidHistory>? GetHistoryByKey(string key);

    int ClearHistory();
    List<CachedDictionaryItem> GetEntireDictionary();
    string? GetValueByKeyAndLanguage(string key, string iso);
    object Save(DictionoidHistory historyEntry);
}