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

        // Pass actionId as parameter instead of capturing it in closure
        List<KeyValuePair<int, string>> EnumOptionsProvider(int currentActionId, ColumnIdentifier key) => 
            _comboboxDataProvider.GetEnumOptions(currentActionId, key.Value);

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