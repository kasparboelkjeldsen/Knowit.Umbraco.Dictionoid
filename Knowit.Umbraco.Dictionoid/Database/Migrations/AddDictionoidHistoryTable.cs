using Knowit.Umbraco.Dictionoid.Database;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Knowit.Umbraco.Dictionoid.Database.Migrations;

public class AddDictionoidHistoryTable : MigrationBase
{
    public AddDictionoidHistoryTable(IMigrationContext context)
        : base(context)
    {
    }

    protected override void Migrate()
    {
        if (TableExists("KnowitDictionoidHistory")) return;

        Create.Table<DictionoidHistory>().Do();
    }
}