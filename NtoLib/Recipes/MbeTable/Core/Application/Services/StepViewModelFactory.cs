using System;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;

namespace NtoLib.Recipes.MbeTable.Core.Application.Services;

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
            var ex = new InvalidOperationException($"Step №{index} does not have an action.");
            _debugLogger.LogException(ex);
            throw ex;
        }

        var availableTargets = _comboboxDataProvider.GetActionTargets(actionId.Value);

        return new StepViewModel(
            step,
            (key, val) => updateCallback(index, key, val),
            startTime,
            availableTargets,
            _debugLogger
        );
    }
}