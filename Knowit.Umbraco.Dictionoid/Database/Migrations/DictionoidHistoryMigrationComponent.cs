using Knowit.Umbraco.Dictionoid.AiClients.Configurations;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Infrastructure.Scoping;

namespace Knowit.Umbraco.Dictionoid.Database.Migrations;

public class DictionoidHistoryMigrationComponent : IComponent
{
    private readonly IScopeProvider _scopeProvider;
    private readonly IMigrationPlanExecutor _migrationPlanExecutor;
    private readonly IKeyValueService _keyValueService;
    private readonly IRuntimeState _runtimeState;
    private readonly DictionoidConfiguration _configuration;

    public DictionoidHistoryMigrationComponent(IScopeProvider scopeProvider,
        IMigrationPlanExecutor migrationPlanExecutor, IKeyValueService keyValueService, IRuntimeState runtimeState,
        IOptions<DictionoidConfiguration> configuration)
    {
        _scopeProvider = scopeProvider;
        _migrationPlanExecutor = migrationPlanExecutor;
        _keyValueService = keyValueService;
        _runtimeState = runtimeState;
        _configuration = configuration.Value;
    }

    public void Initialize()
    {
        var shouldTrack = _configuration.TrackHistory;
        if (!shouldTrack) return;
        if (_runtimeState.Level < RuntimeLevel.Run)
        {
            return;
        }

        var plan = new DictionoidHistoryMigrationPlan();
        var upgrader = new Upgrader(plan);
        upgrader.Execute(_migrationPlanExecutor, _scopeProvider, _keyValueService);
    }

    public void Terminate()
    {
    }
}