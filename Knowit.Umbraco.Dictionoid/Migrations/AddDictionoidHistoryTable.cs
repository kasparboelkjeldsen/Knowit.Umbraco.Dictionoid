using Knowit.Umbraco.Dictionoid.DTO;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Infrastructure.Runtime;
using Umbraco.Cms.Infrastructure.Scoping;
using IScopeProvider = Umbraco.Cms.Infrastructure.Scoping.IScopeProvider;

namespace Knowit.Umbraco.Dictionoid.Migrations
{
	public class DictionoidHistoryMigrationComposer : ComponentComposer<DictionoidHistoryMigrationComponent> { }
	public class DictionoidHistoryMigrationComponent : IComponent
	{
		private readonly IScopeProvider _scopeProvider;
		private readonly IMigrationPlanExecutor _migrationPlanExecutor;
		private readonly IKeyValueService _keyValueService;
		private readonly IRuntimeState _runtimeState;
		private readonly IConfiguration _configuration;
		public DictionoidHistoryMigrationComponent(IScopeProvider scopeProvider, IMigrationPlanExecutor migrationPlanExecutor, IKeyValueService keyValueService, IRuntimeState runtimeState, IConfiguration configuration)
		{
			_scopeProvider = scopeProvider;
			_migrationPlanExecutor = migrationPlanExecutor;
			_keyValueService = keyValueService;
			_runtimeState = runtimeState;
			_configuration = configuration;
		}

		public void Initialize()
		{
			var shouldTrack = _configuration.GetValue<bool?>("Knowit.Umbraco.Dictionoid:TrackHistory");
			if (!shouldTrack.HasValue || !shouldTrack.Value) return;
			if (_runtimeState.Level < RuntimeLevel.Run)
			{
				return;
			}
			var plan = new DictionoidHistoryMigrationPlan();
			var upgrader = new Upgrader(plan);
			upgrader.Execute(_migrationPlanExecutor, _scopeProvider, _keyValueService);
		}

		public void Terminate() { }
	}
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

	public class DictionoidHistoryMigrationPlan : MigrationPlan
	{
		public DictionoidHistoryMigrationPlan() : base("DictionoidHistory")
		{
			From(string.Empty)
				.To<AddDictionoidHistoryTable>("dictionoid-history-db");
		}
	}
}
