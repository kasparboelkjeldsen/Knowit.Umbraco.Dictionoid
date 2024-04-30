namespace Knowit.Umbraco.Dictionoid.AiClients.Configurations;

public class AzureAiApiConfiguration : DictionoidConfiguration
{
    public static string SectionName = "Knowit.Umbraco.Dictionoid.AzureAiApi";
    public string ResourceId { get; set; } = string.Empty;
    public string DeploymentId { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-02-01";
}