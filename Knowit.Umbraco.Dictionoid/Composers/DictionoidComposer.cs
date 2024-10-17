using Knowit.Umbraco.Dictionoid.AiClients;
using Knowit.Umbraco.Dictionoid.AiClients.Configurations;
using Knowit.Umbraco.Dictionoid.Database;
using Knowit.Umbraco.Dictionoid.Services;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Knowit.Umbraco.Dictionoid.Composers;

public class DictionoidComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        var chatGptEnabled = builder.Config[ChatGptApiConfiguration.SectionName + ":Enabled"]?.Equals("True") ?? false;
        var azureEnabled = builder.Config[AzureAiApiConfiguration.SectionName + ":Enabled"]?.Equals("True") ?? false;

        if (!azureEnabled && !chatGptEnabled)
        {
            return;
        }

        if (azureEnabled && chatGptEnabled)
        {
            throw new InvalidOperationException("Both Azure and ChatGPT AI services cannot be enabled at the same time.");
        }

        if (chatGptEnabled)
        {
            builder.Services.Configure<ChatGptApiConfiguration>(
                builder.Config.GetSection(ChatGptApiConfiguration.SectionName));

            builder.Services.Configure<DictionoidConfiguration>(
                builder.Config.GetSection(ChatGptApiConfiguration.SectionName));

            builder.Services.AddSingleton<IAiClient, ChatGptClient>();
        }

        else
        {
            builder.Services.Configure<AzureAiApiConfiguration>(
                builder.Config.GetSection(AzureAiApiConfiguration.SectionName));

            builder.Services.Configure<DictionoidConfiguration>(
                builder.Config.GetSection(AzureAiApiConfiguration.SectionName));

            builder.Services.AddSingleton<IAiClient, AzureAiClient>();
        }

        builder.Services.AddSingleton<IDictionoidHistoryRepository, DictionoidHistoryRepository>();
        builder.Services.AddSingleton<IDictionoidService, DictionoidService>();
    }
}