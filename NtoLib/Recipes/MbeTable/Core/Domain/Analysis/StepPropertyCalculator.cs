#nullable enable
using System;
using System.Data;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Calculations;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis;

public sealed class StepPropertyCalculator
{
    private readonly TableColumns _tableColumns;
    private readonly IActionRepository _actionRepository;
    private readonly PropertyDefinitionRegistry _propertyRegistry;
    private readonly ICalculationOrderer _calculationOrderer;
    private readonly IFormulaParser _formulaParser;
    private readonly ILogger _logger;

    public StepPropertyCalculator(
        TableColumns tableColumns,
        IActionRepository actionRepository,
        PropertyDefinitionRegistry propertyRegistry,
        ICalculationOrderer calculationOrderer,
        IFormulaParser formulaParser,
        ILogger logger)
    {
        _tableColumns = tableColumns;
        _actionRepository = actionRepository;
        _propertyRegistry = propertyRegistry;
        _calculationOrderer = calculationOrderer;
        _formulaParser = formulaParser;
        _logger = logger;
    }

    /// <summary>
    /// Applies user change and recalculates all calculated columns (if possible).
    /// On formula error returns original step (logging error).
    /// </summary>
    public Result<Step> CalculateDependencies(
        Step currentStep,
        ColumnIdentifier triggerKey,
        StepProperty newTriggerProperty)
    {
        try
        {
            var updatedProps = currentStep.Properties.SetItem(triggerKey, newTriggerProperty);
            
            return Result.Ok(currentStep with { Properties = updatedProps });
            
            var calcColumns = _calculationOrderer.GetCalculatedColumnsInOrder();
            if (calcColumns.Count == 0)
                return Result.Ok(currentStep with { Properties = updatedProps });

            var nameMap = _calculationOrderer.GetColumnKeyToDataTableNameMap();

            using var table = new DataTable("calc");
            // Create columns (string type for simplicity; we convert manually)
            foreach (var kv in nameMap)
            {
                if (!table.Columns.Contains(kv.Value))
                    table.Columns.Add(kv.Value, typeof(double));
            }
            var row = table.NewRow();
            table.Rows.Add(row);

            // Preload existing numeric values
            foreach (var kv in updatedProps)
            {
                if (!nameMap.TryGetValue(kv.Key, out var colName)) continue;
                if (kv.Value == null) continue;

                try
                {
                    var def = _propertyRegistry.GetDefinition(kv.Value.PropertyTypeId);
                    var obj = kv.Value.GetValueAsObject();
                    if (def.SystemType == typeof(int))
                        row[colName] = Convert.ToDouble((int)obj);
                    else if (def.SystemType == typeof(float))
                        row[colName] = Convert.ToDouble((float)obj);
                    else if (def.SystemType == typeof(bool))
                        row[colName] = ((bool)obj) ? 1d : 0d;
                    else
                        continue; // strings не кладём — формулы для них не предполагаются
                }
                catch
                {
                    // Игнорируем нечисловые
                }
            }

            var propsMutable = updatedProps.ToBuilder();

            foreach (var column in calcColumns)
            {
                if (column.Calculation == null) continue;
                var deps = column.Calculation.DependencyKeys;

                // Проверка присутствия зависимостей (и значений)
                var missing = deps.Any(d =>
                    !updatedProps.TryGetValue(d, out var sp) || sp == null);
                if (missing)
                {
                    continue; // пропускаем вычисление для этой строки
                }

                // Формула → DataTable syntax
                string dtFormula;
                try
                {
                    dtFormula = _formulaParser.ConvertFormulaToDataTableSyntax(
                        column.Calculation.Formula,
                        nameMap);
                }
                catch (Exception ex)
                {
                    _logger.Log($"Formula conversion failed for '{column.Key.Value}': {ex.Message}");
                    return Result.Ok(currentStep); // возвращаем старое
                }

                object rawResult;
                try
                {
                    rawResult = table.Compute(dtFormula, string.Empty);
                }
                catch (Exception ex)
                {
                    _logger.Log($"Formula evaluation failed for '{column.Key.Value}': {ex.Message}");
                    return Result.Ok(currentStep);
                }

                if (rawResult is DBNull)
                    continue;

                double dbl;
                try
                {
                    dbl = Convert.ToDouble(rawResult);
                    if (double.IsNaN(dbl) || double.IsInfinity(dbl))
                    {
                        _logger.Log($"Formula result invalid (NaN/Inf) for '{column.Key.Value}'.");
                        return Result.Ok(currentStep);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log($"Result conversion failed for '{column.Key.Value}': {ex.Message}");
                    return Result.Ok(currentStep);
                }

                // Приведение к типу свойства
                try
                {
                    var def = _propertyRegistry.GetDefinition(column.PropertyTypeId);
                    object typedValue = def.SystemType == typeof(int)
                        ? (object)(int)Math.Round(dbl)
                        : def.SystemType == typeof(float)
                            ? (object)(float)dbl
                            : def.SystemType == typeof(bool)
                                ? (object)(Math.Abs(dbl) > 1e-9)
                                : (object)dbl.ToString(); // fallback

                    // Обновляем DataRow для каскадных зависимостей
                    if (nameMap.TryGetValue(column.Key, out var colName))
                        row[colName] = def.SystemType == typeof(bool)
                            ? ((bool)typedValue ? 1d : 0d)
                            : Convert.ToDouble(typedValue);

                    var existing = propsMutable[column.Key];
                    StepProperty newProp;
                    if (existing == null)
                    {
                        newProp = new StepProperty(typedValue, column.PropertyTypeId, _propertyRegistry);
                    }
                    else
                    {
                        var upd = existing.WithValue(typedValue);
                        if (upd.IsFailed)
                        {
                            _logger.Log($"Validation failed for computed '{column.Key.Value}': {upd.Errors.First().Message}");
                            return Result.Ok(currentStep);
                        }
                        newProp = upd.Value;
                    }

                    propsMutable[column.Key] = newProp;
                }
                catch (Exception ex)
                {
                    _logger.Log($"Applying computed value failed for '{column.Key.Value}': {ex.Message}");
                    return Result.Ok(currentStep);
                }
            }

            return Result.Ok(currentStep with { Properties = propsMutable.ToImmutable() });
        }
        catch (Exception ex)
        {
            _logger.Log($"Unexpected calculation exception: {ex.Message}");
            return Result.Ok(currentStep);
        }
    }
}