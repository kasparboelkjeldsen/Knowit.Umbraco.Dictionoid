using Knowit.Umbraco.Dictionoid.Models;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;

namespace Knowit.Umbraco.Dictionoid.Database
{
    public class DictionoidHistoryRepository : IDictionoidHistoryRepository
    {
        private readonly IUmbracoDatabaseFactory _umbracoDatabaseFactory;

        public DictionoidHistoryRepository(IUmbracoDatabaseFactory umbracoDatabaseFactory)
        {
            _umbracoDatabaseFactory = umbracoDatabaseFactory;
        }

        public List<CachedDictionaryItem> GetEntireDictionary()
        {
            using var scope = _umbracoDatabaseFactory.CreateDatabase();
            var sql = @"SELECT i.[key], i.[pk], l.languageIsoCode, l.languageCultureName, d.[value]
				        FROM cmsLanguageText d
				        JOIN umbracoLanguage l ON d.languageId = l.id
				        JOIN cmsDictionary i ON i.id = d.uniqueId";

            return scope.Fetch<CachedDictionaryItem>(sql);
        }

        public string? GetValueByKeyAndLanguage(string key, string iso)
        {
            using var scope = _umbracoDatabaseFactory.CreateDatabase();
            var sql = @"SELECT d.[value]
                    FROM cmsLanguageText d 
                    JOIN umbracoLanguage l ON d.languageId = l.id 
                    JOIN cmsDictionary i ON i.id = d.uniqueId 
                    WHERE [key] = @key and languageIsoCode = @iso";

            var result = scope.Fetch<dynamic>(sql, new {key, iso}).FirstOrDefault();
            return result?.value;
        }

        public List<DictionoidHistory>? GetHistoryByKey(string key)
        {
            using var scope = _umbracoDatabaseFactory.CreateDatabase();
            var history =
                scope.Fetch<DictionoidHistory>("SELECT * FROM KnowitDictionoidHistory WHERE key = @key", new {key});
            return history;
        }

        public int ClearHistory()
        {
            using var scope = _umbracoDatabaseFactory.CreateDatabase();
            return scope.Execute("DELETE FROM KnowitDictionoidHistory");
        }

        public object Save(DictionoidHistory historyEntry)
        {
            using var scope = _umbracoDatabaseFactory.CreateDatabase();
            return scope.Insert("KnowitDictionoidHistory", "id", historyEntry);
        }
    }
}