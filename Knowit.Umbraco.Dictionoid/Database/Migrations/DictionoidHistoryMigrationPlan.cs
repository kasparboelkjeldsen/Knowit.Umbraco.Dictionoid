using Umbraco.Cms.Infrastructure.Migrations;

namespace Knowit.Umbraco.Dictionoid.Database.Migrations;

public class DictionoidHistoryMigrationPlan : MigrationPlan
{
    public DictionoidHistoryMigrationPlan() : base("DictionoidHistory")
    {
        From(string.Empty)
            .To<AddDictionoidHistoryTable>("dictionoid-history-db");
    }
}