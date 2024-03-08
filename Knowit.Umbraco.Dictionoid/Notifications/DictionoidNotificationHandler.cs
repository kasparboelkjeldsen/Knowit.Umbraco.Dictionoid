using Knowit.Umbraco.Dictionoid.DTO;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.Persistence;

namespace Knowit.Umbraco.Dictionoid.Notifications
{
	public class NotificationHandlersComposer : IComposer
	{
		public void Compose(IUmbracoBuilder builder)
		{
			builder.AddNotificationAsyncHandler<DictionaryItemSavingNotification, DictionoidNotificationHandler>();
		}
	}
	public class DictionoidNotificationHandler : INotificationAsyncHandler<DictionaryItemSavingNotification>
	{
		private readonly IUmbracoDatabaseFactory _umbracoDatabaseFactory;
		private readonly IConfiguration _configuration;
		public DictionoidNotificationHandler(IUmbracoDatabaseFactory umbracoDatabaseFactory, IConfiguration configuration)
		{
			_umbracoDatabaseFactory = umbracoDatabaseFactory;
			_configuration = configuration;
		}


		public Task HandleAsync(DictionaryItemSavingNotification notification, CancellationToken cancellationToken)
		{
			var shouldTrack = _configuration.GetValue<bool?>("Knowit.Umbraco.Dictionoid:TrackHistory");
			if (!shouldTrack.HasValue || !shouldTrack.Value) return Task.CompletedTask;
			var now = DateTime.Now;
			using (var db = _umbracoDatabaseFactory.CreateDatabase())
			{
				foreach (var entity in notification.SavedEntities)
				{
					List<DictionoidHistory> entries = new List<DictionoidHistory>();

					foreach (var translation in entity.Translations)
					{
						try
						{
							var dyn = db.Fetch<dynamic>(@"
SELECT d.[value]
FROM cmsLanguageText d
JOIN umbracoLanguage l ON d.languageId = l.id
JOIN cmsDictionary i ON i.id = d.uniqueId
where [key] = @key
and languageIsoCode = @iso
", new { key = entity.ItemKey, iso = translation.Language.IsoCode }).FirstOrDefault();
						
						if (dyn == null) continue;

						var historyEntry = new DictionoidHistory
						{
							Key = entity.ItemKey,
							LanguageIsoCode = translation.Language.IsoCode,
							LanguageCultureName = translation.Language.CultureName,
							Value = dyn.value,
							Timestamp = now,
						};
						
							db.Insert("KnowitDictionoidHistory", "id", historyEntry);
						}
						catch (Exception e)
						{
							// todo log failure
						}
					}
				}
			}
			return Task.CompletedTask;
		}
	}
}
