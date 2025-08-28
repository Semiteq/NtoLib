#nullable enable

using System;
using System.IO;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Loaders;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;

namespace NtoLib.Recipes.MbeTable.Config;

/// <inheritdoc />
public sealed class ConfigurationLoader : IConfigurationLoader
{
    private readonly ITableSchemaLoader _tableSchemaLoader;
    private readonly IActionsLoader _actionsLoader;

    public ConfigurationLoader(ITableSchemaLoader tableSchemaLoader, IActionsLoader actionsLoader)
    {
        _tableSchemaLoader = tableSchemaLoader ?? throw new ArgumentNullException(nameof(tableSchemaLoader));
        _actionsLoader = actionsLoader ?? throw new ArgumentNullException(nameof(actionsLoader));
    }

    /// <inheritdoc />
    public Result<AppConfiguration> LoadConfiguration(
        string baseDirectory, 
        string schemaConfigFileName, 
        string actionsConfigFileName)
    {
        try
        {
            // 1. Load Table Schema
            var tableSchemaPath = Path.Combine(baseDirectory, schemaConfigFileName);
            var schemaResult = _tableSchemaLoader.LoadSchema(tableSchemaPath);
            if (schemaResult.IsFailed)
                return Result.Fail("Failed to load table schema configuration.").WithErrors(schemaResult.Errors);
            
            var tableSchema = new TableSchema(schemaResult.Value);

            // 2. Load Actions Configuration
            var actionsConfigPath = Path.Combine(baseDirectory, actionsConfigFileName);
            var actionsResult = _actionsLoader.LoadActions(actionsConfigPath);
            if (actionsResult.IsFailed)
                return Result.Fail("Failed to load actions configuration.").WithErrors(actionsResult.Errors);
            
            // 3. Combine into AppConfiguration
            var appConfiguration = new AppConfiguration(tableSchema, actionsResult.Value);
            return Result.Ok(appConfiguration);
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("A critical error occurred while loading configuration.").CausedBy(ex));
        }
    }
}