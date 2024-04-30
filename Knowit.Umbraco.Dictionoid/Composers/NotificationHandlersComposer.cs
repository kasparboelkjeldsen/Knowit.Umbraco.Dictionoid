using Knowit.Umbraco.Dictionoid.Notifications;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace Knowit.Umbraco.Dictionoid.Composers
{
    public class NotificationHandlersComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.AddNotificationAsyncHandler<DictionaryItemSavingNotification, DictionoidNotificationHandler>();
        }
    }
}