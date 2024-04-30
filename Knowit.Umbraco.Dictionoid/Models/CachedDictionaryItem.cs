namespace Knowit.Umbraco.Dictionoid.Models;

public class CachedDictionaryItem
{
    public string Key { get; set; }
    public int Pk { get; set; }
    public string LanguageISOCode { get; set; }
    public string LanguageCultureName { get; set; }
    public string Value { get; set; }
}