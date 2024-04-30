namespace Knowit.Umbraco.Dictionoid.AiClients.Configurations;

public class ChatGptApiConfiguration : DictionoidConfiguration
{
    public static string SectionName = "Knowit.Umbraco.Dictionoid.ChatGPT";
    public string ModelId { get; set; } = "gpt-4-turbo-preview";
}