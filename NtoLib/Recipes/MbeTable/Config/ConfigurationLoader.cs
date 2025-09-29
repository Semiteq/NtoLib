#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Loaders;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Actions;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.PinGroups;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Properties;
using NtoLib.Recipes.MbeTable.Config.Yaml.Validators;
using NtoLib.Recipes.MbeTable.Core.Domain.Calculations;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;

namespace NtoLib.Recipes.MbeTable.Config;

/// <summary>
/// Entry point for loading and validating the entire application configuration from YAML files.
/// </summary>
public sealed class ConfigurationLoader : IConfigurationLoader
{
    private readonly IPropertyDefinitionLoader _propertyLoader;
    private readonly IPropertyDefsValidator _propertyValidator;
    private readonly IColumnDefsLoader _columnLoader;
    private readonly IColumnDefsValidator _columnValidator;
    private readonly IActionDefsLoader _actionLoader;
    private readonly IActionDefsValidator _actionValidator;
    private readonly IPinGroupDefsLoader _pinGroupsLoader;
    private readonly IPinGroupDefsValidator _pinGroupValidator;
    private readonly ICalculationOrderer _calculationOrderer;
    private readonly IFormulaParser _formulaParser;

    public ConfigurationLoader(
        IPropertyDefinitionLoader propertyLoader,
        IPropertyDefsValidator propertyValidator,
        IColumnDefsLoader columnLoader,
        IColumnDefsValidator columnValidator,
        IActionDefsLoader actionLoader,
        IActionDefsValidator actionValidator,
        IPinGroupDefsLoader pinGroupsLoader,
        IPinGroupDefsValidator pinGroupValidator,
        ICalculationOrderer calculationOrderer,
        IFormulaParser formulaParser)
    {
        _propertyLoader = propertyLoader;
        _propertyValidator = propertyValidator;
        _columnLoader = columnLoader;
        _columnValidator = columnValidator;
        _actionLoader = actionLoader;
        _actionValidator = actionValidator;
        _pinGroupsLoader = pinGroupsLoader;
        _pinGroupValidator = pinGroupValidator;
        _calculationOrderer = calculationOrderer;
        _formulaParser = formulaParser;
    }

    public Result<AppConfiguration> LoadConfiguration(ConfigFiles configFiles)
    {
        try
        {
            // --- 1. Load and Validate Property Definitions ---
            var propertyDefsResult = LoadAndValidatePropertyDefs(configFiles);
            if (propertyDefsResult.IsFailed) return propertyDefsResult.ToResult();
            var propertyRegistry = new PropertyDefinitionRegistry(propertyDefsResult.Value);

            // --- 2. Load and Validate Column Definitions ---
            var tableColumnsResult = LoadAndValidateColumnDefs(configFiles, propertyRegistry);
            if (tableColumnsResult.IsFailed) return tableColumnsResult.ToResult();
            var tableColumns = tableColumnsResult.Value;

            // --- 3. Validate Calculation Order ---
            var orderResult = _calculationOrderer.OrderAndValidate(tableColumns.GetColumns().ToList());
            if (orderResult.IsFailed) return orderResult.ToResult<AppConfiguration>();

            // --- 4. Load and Validate Action Definitions ---
            var actionsResult = LoadAndValidateActionDefs(configFiles, tableColumns);
            if (actionsResult.IsFailed) return actionsResult.ToResult();

            // --- 5. Load and Validate Pin Group Definitions ---
            var pinGroupsResult = LoadAndValidatePinGroups(configFiles);
            if (pinGroupsResult.IsFailed) return pinGroupsResult.ToResult();

            // --- 6. Assemble Final Configuration ---
            var appConfiguration = new AppConfiguration(
                propertyRegistry,
                tableColumns,
                actionsResult.Value,
                pinGroupsResult.Value,
                _calculationOrderer);

            return Result.Ok(appConfiguration);
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("A critical error occurred while loading configuration.").CausedBy(ex));
        }
    }

    private Result<IReadOnlyDictionary<string, IPropertyTypeDefinition>> LoadAndValidatePropertyDefs(ConfigFiles config)
    {
        var path = Path.Combine(config.BaseDirectory, config.PropertyDefsFileName);
        
        return _propertyLoader.Load(path)
            .Bind(dtos => _propertyValidator.Validate(dtos).ToResult(dtos))
            .Map(dtos => (IReadOnlyDictionary<string, IPropertyTypeDefinition>)dtos.ToDictionary(
                d => d.PropertyTypeId, CreatePropertyDefinition, StringComparer.OrdinalIgnoreCase));
    }
    
    private Result<TableColumns> LoadAndValidateColumnDefs(ConfigFiles config, PropertyDefinitionRegistry registry)
    {
        var path = Path.Combine(config.BaseDirectory, config.ColumnDefsFileName);
        
        return _columnLoader.LoadColumnDefs(path)
            .Bind(dtos => _columnValidator.Validate(dtos, registry).ToResult(dtos))
            .Map(dtos => new TableColumns(dtos.Select(ConvertToColumnDefinition).ToList()));
    }
    
    private Result<IReadOnlyDictionary<int, ActionDefinition>> LoadAndValidateActionDefs(ConfigFiles config, TableColumns tableColumns)
    {
        var path = Path.Combine(config.BaseDirectory, config.ActionsDefsFileName);

        return _actionLoader.LoadActions(path)
            .Bind(defs => _actionValidator.Validate(defs, tableColumns).ToResult(defs))
            .Map(defs => (IReadOnlyDictionary<int, ActionDefinition>)defs.ToDictionary(a => a.Id));
    }

    private Result<IReadOnlyList<PinGroupData>> LoadAndValidatePinGroups(ConfigFiles config)
    {
        var path = Path.Combine(config.BaseDirectory, config.PinGroupDefsFileName);
        
        // Pin groups are always loaded before this method, because scada calls pin init before the main DI container is created.
        // Here pins are already validated by PinGroupDefsValidator and inited. We load them into bundle to validate with other data in AppConfigurationValidator.
        // todo: remove second load from disk
        return _pinGroupsLoader.LoadPinGroups(path);
    }

    private ColumnDefinition ConvertToColumnDefinition(YamlColumnDefinition dto)
    {
        CalculationDefinition? calcDef = null;
        var readOnly = dto.BusinessLogic.ReadOnly;

        if (dto.BusinessLogic.Calculation != null)
        {
            var formula = dto.BusinessLogic.Calculation.Formula ?? string.Empty;
            var deps = _formulaParser.GetDependencies(formula);
            calcDef = new CalculationDefinition(formula, deps.ToList());
        }

        return new ColumnDefinition(
            Key: new ColumnIdentifier(dto.Key),
            PropertyTypeId: dto.BusinessLogic.PropertyTypeId,
            PlcMapping: dto.BusinessLogic.PlcMapping,

            Code: dto.Ui!.Code,
            UiName: dto.Ui.UiName,
            ColumnType: dto.Ui.ColumnType,
            Width: dto.Ui.Width,
            Alignment: dto.Ui.Alignment,
            ReadOnly: readOnly,
            Calculation: calcDef
        );
    }
    
    private static IPropertyTypeDefinition CreatePropertyDefinition(YamlPropertyDefinition dto)
    {
        // This method assumes validation has already passed.
        if (string.Equals(dto.PropertyTypeId, "Time", StringComparison.OrdinalIgnoreCase))
            return new DynamicTimeDefinition(dto);

        if (string.Equals(dto.PropertyTypeId, "Enum", StringComparison.OrdinalIgnoreCase))
            return new ConfigurableEnumDefinition(dto);

        var sysType = Type.GetType(dto.SystemType, throwOnError: true, ignoreCase: true)!;

        if (sysType == typeof(string))
            return new ConfigurableStringDefinition(dto);

        if (sysType == typeof(short) || sysType == typeof(float))
            return new ConfigurableNumericDefinition(dto);
        
        // This path should be unreachable if validation is correct.
        throw new NotSupportedException($"Unsupported SystemType '{dto.SystemType}' for '{dto.PropertyTypeId}'.");
    }
}