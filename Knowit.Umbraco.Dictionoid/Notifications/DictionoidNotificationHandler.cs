using Knowit.Umbraco.Dictionoid.AiClients.Configurations;
using Knowit.Umbraco.Dictionoid.Database;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;

namespace Knowit.Umbraco.Dictionoid.Notifications;

public class DictionoidNotificationHandler : INotificationAsyncHandler<DictionaryItemSavingNotification>
{
    private readonly DictionoidConfiguration _configuration;
    private readonly IDictionoidHistoryRepository _historyRepository;
    private readonly IUmbracoDatabaseFactory _umbracoDatabaseFactory;

    public DictionoidNotificationHandler(IOptions<DictionoidConfiguration> configuration,
        IDictionoidHistoryRepository historyRepository, IUmbracoDatabaseFactory umbracoDatabaseFactory)
    {
        _historyRepository = historyRepository;
        _umbracoDatabaseFactory = umbracoDatabaseFactory;
        _configuration = configuration.Value;
    }

    public Task HandleAsync(DictionaryItemSavingNotification notification, CancellationToken cancellationToken)
    {
        var shouldTrack = _configuration.TrackHistory;
        if (!shouldTrack) return Task.CompletedTask;

        var now = DateTime.Now;

        foreach (var entity in notification.SavedEntities)
        {
            foreach (var translation in entity.Translations)
            {
                try
                {
                    var value = _historyRepository.GetValueByKeyAndLanguage(entity.ItemKey, translation.Language.IsoCode);

                    if (value == null) continue;

                    var historyEntry = new DictionoidHistory
                    {
                        Key = entity.ItemKey,
                        LanguageIsoCode = translation.Language.IsoCode,
                        LanguageCultureName = translation.Language.CultureName,
                        Value = value,
                        Timestamp = now,
                    };

                    _historyRepository.Save(historyEntry);
                }
                catch
                {
                    // todo log failure
                }
            }
        }

        return Task.CompletedTask;
    }
}