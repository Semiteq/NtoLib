using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Errors;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ServiceCsv.Data;
using NtoLib.Recipes.MbeTable.ServiceCsv.Errors;
using NtoLib.Recipes.MbeTable.ServiceCsv.Parsing;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Errors;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Strategies;

public sealed class CsvAssemblyStrategy
{
    private readonly IActionRepository _actionRepository;
    private readonly PropertyDefinitionRegistry _propertyRegistry;
    private readonly IReadOnlyList<ColumnDefinition> _columns;
    private readonly ICsvHeaderBinder _headerBinder;
    private readonly ILogger<CsvAssemblyStrategy> _logger;

    public CsvAssemblyStrategy(
        IActionRepository actionRepository,
        PropertyDefinitionRegistry propertyRegistry,
        IReadOnlyList<ColumnDefinition> columns,
        ICsvHeaderBinder headerBinder,
        ILogger<CsvAssemblyStrategy> logger)
    {
        _actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
        _propertyRegistry = propertyRegistry ?? throw new ArgumentNullException(nameof(propertyRegistry));
        _columns = columns ?? throw new ArgumentNullException(nameof(columns));
        _headerBinder = headerBinder ?? throw new ArgumentNullException(nameof(headerBinder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Result<Recipe> AssembleFromRawData(CsvRawData rawData)
    {
        if (rawData.Records.Count == 0)
        {
            return new Recipe(ImmutableList<Step>.Empty);
        }

        var bindingResult = _headerBinder.Bind(rawData.Headers.ToArray(), new TableColumns(_columns));
        if (bindingResult.IsFailed)
        {
            _logger.LogError("Header binding failed: {Errors}",
                string.Join("; ", bindingResult.Errors.Select(e => e.Message)));
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
                _logger.LogError("Step assembly failed at row {RowIndex}: {Errors}",
                    rowIndex + 2,
                    string.Join("; ", stepResult.Errors.Select(e => e.Message)));
                return stepResult.ToResult<Recipe>();
            }

            steps.Add(stepResult.Value);
        }

        return new Recipe(steps.ToImmutableList());
    }

    private Result<Step> AssembleStep(string[] record, CsvHeaderBinder.Binding binding, int lineNumber)
    {
        var actionIdResult = ExtractActionId(record, binding);
        if (actionIdResult.IsFailed)
            return Result.Fail(new CsvInvalidDataError("Failed to extract action ID", lineNumber))
                .WithErrors(actionIdResult.Errors);

        var actionId = actionIdResult.Value;
        var actionResult = _actionRepository.GetActionDefinitionById(actionId);
        if (actionResult.IsFailed)
            return new CoreActionNotFoundError(actionId);

        var actionDefinition = actionResult.Value;
        var createBuilderResult = StepBuilder.Create(actionDefinition, _propertyRegistry, _columns);
        if (createBuilderResult.IsFailed) return createBuilderResult.ToResult<Step>();
        var builder = createBuilderResult.Value;

        foreach (var kvp in binding.FileIndexToColumn)
        {
            var fileIndex = kvp.Key;
            var columnDef = kvp.Value;

            if (columnDef.Key == MandatoryColumns.Action || columnDef.Key == MandatoryColumns.StepStartTime)
                continue;

            var rawValue = fileIndex < record.Length ? record[fileIndex] : string.Empty;

            if (!builder.Supports(columnDef.Key))
            {
                if (!string.IsNullOrWhiteSpace(rawValue))
                    return new AssemblyColumnNotApplicableError(columnDef.Code, actionDefinition.Name, rawValue,
                        lineNumber);

                continue;
            }

            if (string.IsNullOrWhiteSpace(rawValue))
                continue;

            var propertyDef = _propertyRegistry.GetPropertyDefinition(columnDef.PropertyTypeId);
            var parseResult = ParseValue(rawValue, propertyDef.SystemType);

            if (parseResult.IsFailed)
                return Result.Fail(new CorePropertyConversionFailedError(rawValue, propertyDef.SystemType.Name))
                    .WithErrors(parseResult.Errors);

            var setResult = builder.WithOptionalDynamic(columnDef.Key, parseResult.Value);
            if (setResult.IsFailed)
                return setResult.ToResult<Step>();
        }

        return builder.Build();
    }

    private static Result<short> ExtractActionId(string[] record, CsvHeaderBinder.Binding binding)
    {
        var actionIndex = FindColumnIndex(binding, MandatoryColumns.Action);

        if (actionIndex < 0 || actionIndex >= record.Length)
            return new AssemblyActionColumnOutOfRangeError();

        var actionValue = record[actionIndex];

        if (!short.TryParse(actionValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var actionId))
            return new CorePropertyConversionFailedError(actionValue, "short");

        return actionId;
    }

    private static short FindColumnIndex(CsvHeaderBinder.Binding binding, ColumnIdentifier key)
    {
        foreach (var kvp in binding.FileIndexToColumn)
        {
            if (kvp.Value.Key == key)
                return kvp.Key;
        }

        return -1;
    }

    private static Result<object> ParseValue(string rawValue, Type targetType)
    {
        if (targetType == typeof(string))
            return rawValue;

        if (targetType == typeof(short))
        {
            if (short.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var shortValue))
                return shortValue;

            return new CorePropertyConversionFailedError(rawValue, "short");
        }

        if (targetType == typeof(float))
        {
            if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                return floatValue;

            return new CorePropertyConversionFailedError(rawValue, "float");
        }

        return new CsvInvalidDataError($"Unsupported type: {targetType.Name}");
    }
}