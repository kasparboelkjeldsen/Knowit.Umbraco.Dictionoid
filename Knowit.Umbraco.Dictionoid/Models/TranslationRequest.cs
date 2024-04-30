namespace Knowit.Umbraco.Dictionoid.Models;

public class TranslationRequest
{
    public string? Color { get; set; }
    public string? DetectLanguage { get; set; }
    public List<TranslationItem>? Items { get; set; }
}