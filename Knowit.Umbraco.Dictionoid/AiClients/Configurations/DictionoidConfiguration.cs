namespace Knowit.Umbraco.Dictionoid.AiClients.Configurations;

public class DictionoidConfiguration
{
    public bool Enabled { get; set; } = false;
    public string? ApiKey { get; set; } = string.Empty;
    public bool CleanupAfterCreate { get; set; } = false;
    public bool CreateOnNotExist { get; set; } = false;
    public bool CleanupInBackoffice { get; set; } = false;
    public bool TrackHistory { get; set; } = false;

    public FrontendApi FrontendApi { get; set; } = new FrontendApi();
}

public class FrontendApi
{
    public bool Expose { get; set; } = false;
    public string Secret { get; set; } = string.Empty;
    public bool TranslateOnMissing { get; set; } = false;
}