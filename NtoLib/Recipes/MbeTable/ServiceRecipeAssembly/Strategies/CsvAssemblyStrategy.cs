using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.Errors;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ServiceCsv.Data;
using NtoLib.Recipes.MbeTable.ServiceCsv.Parsing;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Strategies;

/// <summary>
/// Assembly strategy for building Recipe from CSV data.
/// </summary>
public sealed class CsvAssemblyStrategy
{
    private readonly IActionRepository _actionRepository;
    private readonly PropertyDefinitionRegistry _propertyRegistry;
    private readonly IReadOnlyList<ColumnDefinition> _columns;
    private readonly ICsvHeaderBinder _headerBinder;

    public CsvAssemblyStrategy(
        IActionRepository actionRepository,
        PropertyDefinitionRegistry propertyRegistry,
        IReadOnlyList<ColumnDefinition> columns,
        ICsvHeaderBinder headerBinder)
    {
        _actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
        _propertyRegistry = propertyRegistry ?? throw new ArgumentNullException(nameof(propertyRegistry));
        _columns = columns ?? throw new ArgumentNullException(nameof(columns));
        _headerBinder = headerBinder ?? throw new ArgumentNullException(nameof(headerBinder));
    }

    public Result<Recipe> AssembleFromRawData(CsvRawData rawData)
    {
        if (rawData.Records.Count == 0)
        {
            return Result.Ok(new Recipe(ImmutableList<Step>.Empty));
        }
        
        var bindingResult = _headerBinder.Bind(rawData.Headers.ToArray(), new TableColumns(_columns));
        if (bindingResult.IsFailed)
        {
            return bindingResult.ToResult<Recipe>();
        }
        
        var binding = bindingResult.Value;
        var steps = new List<Step>();
        
        for (var rowIndex = 0; rowIndex < rawData.Records.Count; rowIndex++)
        {
            var record = rawData.Records[rowIndex];
            var stepResult = AssembleStep(record, binding, rowIndex + 2);
            
            if (stepResult.IsFailed)
            {
                return stepResult.ToResult<Recipe>();
            }
            
            steps.Add(stepResult.Value);
        }
        
        return Result.Ok(new Recipe(steps.ToImmutableList()));
    }

    private Result<Step> AssembleStep(string[] record, CsvHeaderBinder.Binding binding, int lineNumber)
    {
        var actionIdResult = ExtractActionId(record, binding);
        if (actionIdResult.IsFailed)
        {
            return Result.Fail<Step>(new Error($"Line {lineNumber}: Failed to extract action ID")
                .WithMetadata(nameof(Codes), Codes.CsvInvalidData)
                .CausedBy(actionIdResult.Errors));
        }
        
        var actionId = actionIdResult.Value;
        var actionResult = _actionRepository.GetResultActionDefinitionById(actionId);
        if (actionResult.IsFailed)
        {
            return Result.Fail<Step>(new Error($"Line {lineNumber}: Unknown action ID {actionId}")
                .WithMetadata(nameof(Codes), Codes.CoreActionNotFound)
                .CausedBy(actionResult.Errors));
        }
        
        var actionDefinition = actionResult.Value;
        var builder = new StepBuilder(actionDefinition, _propertyRegistry, _columns);
        
        foreach (var kvp in binding.FileIndexToColumn)
        {
            var fileIndex = kvp.Key;
            var columnDef = kvp.Value;
            
            if (columnDef.Key == MandatoryColumns.Action || columnDef.Key == MandatoryColumns.StepStartTime)
            {
                continue;
            }
            
            var rawValue = fileIndex < record.Length ? record[fileIndex] : string.Empty;
            
            if (!builder.Supports(columnDef.Key))
            {
                if (!string.IsNullOrWhiteSpace(rawValue))
                {
                    return Result.Fail<Step>(new Error($"Line {lineNumber}: Column '{columnDef.Code}' not applicable for action '{actionDefinition.Name}' but has value '{rawValue}'")
                        .WithMetadata(nameof(Codes), Codes.CsvInvalidData));
                }
                continue;
            }
            
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }
            
            var propertyDef = _propertyRegistry.GetPropertyDefinition(columnDef.PropertyTypeId);
            var parseResult = ParseValue(rawValue, propertyDef.SystemType);
            
            if (parseResult.IsFailed)
            {
                return Result.Fail<Step>(new Error($"Line {lineNumber}: Invalid value '{rawValue}' in column '{columnDef.Code}'")
                    .WithMetadata(nameof(Codes), Codes.PropertyConversionFailed)
                    .CausedBy(parseResult.Errors));
            }
            
            var setResult = builder.WithOptionalDynamic(columnDef.Key, parseResult.Value);
            if (setResult.IsFailed)
            {
                return setResult.ToResult<Step>();
            }
        }
        
        return Result.Ok(builder.Build());
    }

    private static Result<short> ExtractActionId(string[] record, CsvHeaderBinder.Binding binding)
    {
        var actionIndex = FindColumnIndex(binding, MandatoryColumns.Action);
        
        if (actionIndex < 0 || actionIndex >= record.Length)
        {
            return Result.Fail<short>(new Error("Action column not found or out of range")
                .WithMetadata(nameof(Codes), Codes.CsvInvalidData));
        }
        
        var actionValue = record[actionIndex];
        
        if (!short.TryParse(actionValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var actionId))
        {
            return Result.Fail<short>(new Error($"Invalid action ID: '{actionValue}'")
                .WithMetadata(nameof(Codes), Codes.PropertyConversionFailed));
        }
        
        return Result.Ok((short)actionId);
    }

    private static short FindColumnIndex(CsvHeaderBinder.Binding binding, ColumnIdentifier key)
    {
        foreach (var kvp in binding.FileIndexToColumn)
        {
            if (kvp.Value.Key == key)
            {
                return kvp.Key;
            }
        }
        
        return -1;
    }

    private static Result<object> ParseValue(string rawValue, Type targetType)
    {
        if (targetType == typeof(string))
        {
            return Result.Ok<object>(rawValue);
        }
        
        if (targetType == typeof(short))
        {
            if (short.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var shortValue))
            {
                return Result.Ok<object>(shortValue);
            }
            return Result.Fail(new Error("Value must be a valid integer").WithMetadata(nameof(Codes), Codes.PropertyConversionFailed));
        }
        
        if (targetType == typeof(float))
        {
            if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
            {
                return Result.Ok<object>(floatValue);
            }
            return Result.Fail(new Error("Value must be a valid floating-point number").WithMetadata(nameof(Codes), Codes.PropertyConversionFailed));
        }
        
        return Result.Fail(new Error($"Unsupported type: {targetType.Name}").WithMetadata(nameof(Codes), Codes.PropertyConversionFailed));
    }
}