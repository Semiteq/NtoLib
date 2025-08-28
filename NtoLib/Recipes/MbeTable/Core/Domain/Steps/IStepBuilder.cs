#nullable enable

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps;

/// <summary>
/// Defines a contract for building and configuring a <see cref="Step"/> instance.
/// The builder is stateful and is used to construct a step for a specific action.
/// </summary>
public interface IStepBuilder
{
    /// <summary>
    /// Initializes the step by setting all possible properties to a disabled state (null)
    /// and then activating the properties applicable to the current action with their default values.
    /// </summary>
    void InitializeStep();

    /// <summary>
    /// Gets a collection of column keys that are active (not null) for the current action.
    /// </summary>
    IReadOnlyCollection<ColumnIdentifier> NonNullKeys { get; }

    /// <summary>
    /// Checks whether a specific column is supported (applicable) for the current action.
    /// </summary>
    /// <param name="key">The column identifier to check.</param>
    /// <returns>True if the column is applicable; otherwise, false.</returns>
    bool Supports(ColumnIdentifier key);

    /// <summary>
    /// Sets the action target property if the action supports it.
    /// </summary>
    IStepBuilder WithOptionalTarget(int? target);
    
    /// <summary>
    /// Sets the initial value property if the action supports it.
    /// </summary>
    IStepBuilder WithOptionalInitialValue(float? value);

    /// <summary>
    /// Sets the setpoint property if the action supports it.
    /// </summary>
    IStepBuilder WithOptionalSetpoint(float? value);

    /// <summary>
    /// Sets the speed property if the action supports it.
    /// </summary>
    IStepBuilder WithOptionalSpeed(float? value);

    /// <summary>
    /// Sets the duration property if the action supports it.
    /// </summary>
    IStepBuilder WithOptionalDuration(float? value);

    /// <summary>
    /// Sets the comment property if the action supports it.
    /// </summary>
    IStepBuilder WithOptionalComment(string? comment);
    
    /// <summary>
    /// Constructs and returns the final immutable <see cref="Step"/> object.
    /// </summary>
    Step Build();

    /// <summary>
    /// Sets a property with a specific value and type, overriding any existing value.
    /// This method is for internal use when the type is known.
    /// </summary>
    IStepBuilder WithProperty(ColumnIdentifier key, object value, PropertyType type);
    
    /// <summary>
    /// Sets a property with a new value if the property is supported by the action.
    /// The property type is inferred from the existing property definition.
    /// </summary>
    IStepBuilder WithOptionalDynamic(ColumnIdentifier key, object value);
}