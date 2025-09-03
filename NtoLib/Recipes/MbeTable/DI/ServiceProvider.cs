#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Config.Loaders;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain;
using NtoLib.Recipes.MbeTable.Core.Application.Services;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis.Rules;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Core.Domain.Steps;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Protocol;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Transport;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Utils;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Csv;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Validation;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;
using NtoLib.Recipes.MbeTable.Presentation.Status;
using NtoLib.Recipes.MbeTable.Presentation.Table.CellState;
using NtoLib.Recipes.MbeTable.StateMachine;
using NtoLib.Recipes.MbeTable.StateMachine.App;
using NtoLib.Recipes.MbeTable.StateMachine.Contracts;
using NtoLib.Recipes.MbeTable.StateMachine.ThreadDispatcher;

namespace NtoLib.Recipes.MbeTable.DI;

public static class MbeTableServiceConfigurator
{
    public static IServiceProvider ConfigureServices(MbeTableFB mbeTableFb)
    {
        if (mbeTableFb is null)
            throw new ArgumentNullException(nameof(mbeTableFb), "FbConnector cannot be null. Ensure that the VisualFBConnector is properly initialized.");

        var services = new ServiceCollection();

        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        var configurationLoader = new ConfigurationLoader(new TableSchemaLoader(), new ActionsLoader());
        var configResult = configurationLoader.LoadConfiguration(baseDirectory, "TableSchema.json", "ActionSchema.json");
        if (configResult.IsFailed)
        {
            throw new InvalidOperationException(
                $"Failed to load application configuration. Reasons: {string.Join("; ", configResult.Errors.Select(e => e.Message))}");
        }
        var appConfiguration = configResult.Value;

        Debug.Print("Schema and action configuration files loaded successfully.");

        ValidateConfigurationConsistency(mbeTableFb, appConfiguration);
        ValidateActionsColumnsAgainstSchema(appConfiguration);

        services.AddSingleton(mbeTableFb);
        services.AddSingleton(appConfiguration);
        services.AddSingleton(appConfiguration.Schema);

        services.AddSingleton<IPlcStateMonitor, PlcStateMonitor>();
        services.AddSingleton<IStatusManager, StatusManager>();
        services.AddSingleton<IActionRepository, ActionRepository>();
        services.AddSingleton<ILogger, DebugLogger>();
        services.AddSingleton<TableCellStateManager>();
        services.AddSingleton<IActionTargetProvider, ActionTargetProvider>();
        services.AddSingleton<IPlcRecipeStatusProvider, PlcRecipeStatusProvider>();
        services.AddSingleton<ICommunicationSettingsProvider, CommunicationSettingsProvider>();
        services.AddSingleton<AppStateMachine>();
        services.AddSingleton<AppStateUiProjector>();
        services.AddSingleton<IUiDispatcher, ImmediateUiDispatcher>();
        services.AddSingleton<TimerService>();
        services.AddSingleton<PropertyDefinitionRegistry>();
        services.AddSingleton<IComboboxDataProvider, ComboboxDataProvider>();
        services.AddSingleton<ITableSchemaLoader, TableSchemaLoader>();
        services.AddSingleton<IStepFactory, StepFactory>();
        services.AddSingleton<StepCalculationLogic>();
        services.AddSingleton<ICalculationRule, SmoothRampCalculationRule>();
        services.AddSingleton<StepPropertyCalculator>();
        services.AddSingleton<IRecipeLoopValidator, RecipeLoopValidator>();
        services.AddSingleton<IRecipeTimeCalculator, RecipeTimeCalculator>();
        services.AddSingleton<IRecipeEngine, RecipeEngine>();
        services.AddSingleton<IRecipeApplicationService, RecipeApplicationService>();
        services.AddSingleton<IStepViewModelFactory, StepViewModelFactory>();
        services.AddSingleton<RecipeViewModel>();
        services.AddSingleton<IRecipeFileReader, RecipeFileReader>();
        services.AddSingleton<IRecipeFileWriter, RecipeFileWriter>();
        services.AddSingleton<IRecipeSerializer, RecipeCsvSerializer>();
        services.AddSingleton<ICsvHelperFactory, CsvHelperFactory>();
        services.AddSingleton<ICsvStepMapper, CsvStepMapper>();
        services.AddSingleton<ICsvHeaderBinder, CsvHeaderBinder>();
        services.AddSingleton<RecipeFileMetadataSerializer>();
        services.AddSingleton<TargetAvailabilityValidator>();
        services.AddSingleton<IPlcRecipeSerializer, PlcRecipeSerializer>();
        services.AddSingleton<IRecipeComparator, RecipeComparatorV1>();
        services.AddSingleton<IRecipePlcSender, RecipePlcSender>();
        services.AddSingleton<PlcCapacityCalculator>();
        services.AddSingleton<IPlcProtocol, PlcProtocol>();
        services.AddSingleton<IModbusTransport, ModbusTransport>();
        services.AddSingleton<RecipeEffectsHandler>();

        services.AddTransient(_ => new OpenFileDialog
        {
            Filter = @"CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            AddExtension = true, Multiselect = false, Title = @"Выберите файл рецепта", RestoreDirectory = true
        });

        services.AddTransient(_ => new SaveFileDialog
        {
            Filter = @"CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            AddExtension = true, Title = @"Сохраните файл рецепта", RestoreDirectory = true
        });

        var serviceProvider = services.BuildServiceProvider();

        var stateMachine = serviceProvider.GetRequiredService<AppStateMachine>();
        var effectsHandler = serviceProvider.GetRequiredService<RecipeEffectsHandler>();
        stateMachine.InitializeEffects(effectsHandler);

        return serviceProvider;
    }

    private static void ValidateConfigurationConsistency(MbeTableFB fb, AppConfiguration appConfig)
    {
        var definedHardwareGroups = fb.GetDefinedGroupNames().ToHashSet(StringComparer.OrdinalIgnoreCase);

        var requiredTargetGroups = appConfig.Actions.Values
            .Where(a => !string.IsNullOrWhiteSpace(a.TargetGroup))
            .Where(a => a.Columns?.ContainsKey("action-target") == true)
            .Select(a => a.TargetGroup!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var requiredGroup in requiredTargetGroups)
        {
            if (!definedHardwareGroups.Contains(requiredGroup))
            {
                throw new InvalidOperationException(
                    "Configuration Error: ActionSchema.json requires a TargetGroup named " +
                    $"'{requiredGroup}', but no such group is defined in PinGroups.json. " +
                    "Please check for typos or add the corresponding group to the hardware configuration.");
            }
        }

        Debug.Print("Configuration consistency check passed.");
    }

    /// <summary>
    /// Ensures every column key mentioned in ActionSchema.json exists in TableSchema.json.
    /// Fails fast with a detailed message listing invalid keys per action.
    /// </summary>
    private static void ValidateActionsColumnsAgainstSchema(AppConfiguration appConfig)
    {
        var schemaKeys = appConfig.Schema.GetColumns()
            .Select(c => c.Key.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var invalidPerAction = appConfig.Actions.Values
            .Select(a => new
            {
                a.Id,
                a.Name,
                Invalid = a.Columns.Keys
                    .Where(k => !schemaKeys.Contains(k))
                    .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            })
            .Where(x => x.Invalid.Length > 0)
            .ToArray();

        if (invalidPerAction.Length > 0)
        {
            var details = string.Join("; ", invalidPerAction.Select(x =>
                $"actionId={x.Id} ('{x.Name}') invalid columns: [{string.Join(", ", x.Invalid)}]"));
            throw new InvalidOperationException(
                "Configuration Error: Some action columns do not exist in TableSchema.json. " + details);
        }
    }
}