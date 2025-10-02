#nullable enable

using System;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.State;

/// <summary>
/// Tracks the current executing line from PLC and provides row execution state.
/// </summary>
public sealed class RowExecutionStateProvider : IRowExecutionStateProvider
{
    private readonly IPlcRecipeStatusProvider _plcRecipeStatusProvider;
    private int _currentLineIndex;

    public event Action<int, int>? CurrentLineChanged;

    public int CurrentLineIndex => _currentLineIndex;

    public RowExecutionStateProvider(IPlcRecipeStatusProvider plcRecipeStatusProvider)
    {
        _plcRecipeStatusProvider = plcRecipeStatusProvider ?? throw new ArgumentNullException(nameof(plcRecipeStatusProvider));
        _plcRecipeStatusProvider.StatusChanged += OnPlcStatusChanged;
        
        var initialStatus = _plcRecipeStatusProvider.GetStatus();
        _currentLineIndex = initialStatus.CurrentLine;
    }

    public RowExecutionState GetState(int rowIndex)
    {
        if (_currentLineIndex < 0)
        {
            return RowExecutionState.Upcoming;
        }

        if (rowIndex < _currentLineIndex)
        {
            return RowExecutionState.Passed;
        }

        if (rowIndex == _currentLineIndex)
        {
            return RowExecutionState.Current;
        }

        return RowExecutionState.Upcoming;
    }

    private void OnPlcStatusChanged(PlcRecipeStatus status)
    {
        var oldIndex = _currentLineIndex;
        var newIndex = status.CurrentLine;

        if (oldIndex == newIndex)
        {
            return;
        }

        _currentLineIndex = newIndex;
        CurrentLineChanged?.Invoke(oldIndex, newIndex);
    }
}