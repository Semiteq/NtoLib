#nullable enable

using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;

namespace NtoLib.Recipes.MbeTable.Core.Application.Services;

/// <summary>
/// A factory for creating instances of <see cref="StepViewModel"/>.
/// </summary>
public sealed class StepViewModelFactory : IStepViewModelFactory
{
    private readonly IComboboxDataProvider _comboboxDataProvider;
    private readonly ILogger _debugLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StepViewModelFactory"/> class.
    /// </summary>
    /// <param name="comboboxDataProvider">A provider for combobox data sources.</param>
    /// <param name="debugLogger">The logger instance for debugging purposes.</param>
    public StepViewModelFactory(IComboboxDataProvider comboboxDataProvider, ILogger debugLogger)
    {
        _comboboxDataProvider = comboboxDataProvider ?? throw new ArgumentNullException(nameof(comboboxDataProvider));
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
    }

    /// <inheritdoc />
    public StepViewModel Create(Step step, int index, RecipeUpdateResult analysisResult, Action<int, ColumnIdentifier, object> updateCallback)
    {
        analysisResult.TimeResult.StepStartTimes.TryGetValue(index, out var startTime);

        var actionId = step.Properties[WellKnownColumns.Action]?.GetValue<int>();
        if (!actionId.HasValue)
        {
            var ex = new InvalidOperationException($"Step at index {index} does not have a valid action defined.");
            _debugLogger.LogException(ex, new { StepIndex = index });
            throw ex;
        }

        // This closure captures the actionId to provide enum options specific to the step's action.
        List<KeyValuePair<int, string>> EnumOptionsProvider(ColumnIdentifier key) => 
            _comboboxDataProvider.GetEnumOptions(actionId.Value, key.Value);

        return new StepViewModel(
            step,
            index,
            updateCallback,
            startTime,
            EnumOptionsProvider,
            _debugLogger
        );
    }
}