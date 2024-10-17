using Knowit.Umbraco.Dictionoid.AiClients.Configurations;
using Knowit.Umbraco.Dictionoid.Notifications;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace Knowit.Umbraco.Dictionoid.Composers;

public class NotificationHandlersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        var chatGptEnabled = builder.Config[ChatGptApiConfiguration.SectionName + ":Enabled"]?.Equals("True") ?? false;
        var azureEnabled = builder.Config[AzureAiApiConfiguration.SectionName + ":Enabled"]?.Equals("True") ?? false;

        if (!azureEnabled && !chatGptEnabled)
        {
            return;
        }

        builder.AddNotificationAsyncHandler<DictionaryItemSavingNotification, DictionoidNotificationHandler>();
    }
}