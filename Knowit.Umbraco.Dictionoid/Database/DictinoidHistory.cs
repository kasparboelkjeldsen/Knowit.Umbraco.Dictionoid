using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Knowit.Umbraco.Dictionoid.Database
{
    [TableName("KnowitDictionoidHistory")]
    [PrimaryKey("Id", AutoIncrement = true)]
    [ExplicitColumns]
    public class DictionoidHistory
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column("Id")]
        public int Id { get; set; } // auto-incremented

        [Column("Key")] public string Key { get; set; }
        [Column("LanguageIsoCode")] public string LanguageIsoCode { get; set; }
        [Column("LanguageCultureName")] public string LanguageCultureName { get; set; }
        [Column("Value")] public string Value { get; set; }
        [Column("Timestamp")] public DateTime Timestamp { get; set; }
    }
}