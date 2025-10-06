using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Config.Domain.Columns;
using NtoLib.Recipes.MbeTable.Core.Entities;

namespace NtoLib.Recipes.MbeTable.Core.Services;

/// <summary>
/// Provides property state (Enabled/Readonly/Disabled) based on recipe data and column configuration.
/// </summary>
public sealed class PropertyStateProvider
{
    private readonly IReadOnlyList<ColumnDefinition> _columnsInConfig;
    
    public PropertyStateProvider(IReadOnlyList<ColumnDefinition> columnsInConfig)
    {
        _columnsInConfig = columnsInConfig ?? throw new ArgumentNullException(nameof(columnsInConfig));
    }

    /// <summary>
    /// Gets the state of a specific cell.
    /// </summary>
    /// <param name="step">Current step.</param>
    /// <param name="columnKey">Column identifier.</param>
    /// <returns>Property state.</returns>
    public PropertyState GetPropertyState(Step step, ColumnIdentifier columnKey)
    {
        // StepStartTime is always readonly
        if (columnKey == MandatoryColumns.StepStartTime)
            return PropertyState.Readonly;
        
        // Property doesn't exist for this action
        if (!step.Properties.TryGetValue(columnKey, out var propertyValue) || propertyValue == null)
            return PropertyState.Disabled;

        // Find column definition
        var columnDef = _columnsInConfig.FirstOrDefault(c => c.Key == columnKey);
        if (columnDef == null)
            return PropertyState.Disabled;

        return columnDef.ReadOnly 
            ? PropertyState.Readonly 
            : PropertyState.Enabled;
    }
}