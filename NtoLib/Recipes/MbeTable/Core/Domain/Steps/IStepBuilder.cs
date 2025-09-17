#nullable enable

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
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
    /// Sets a property with a new value if the property is supported by the action.
    /// The property type is inferred from the existing property definition. This is the primary method
    /// for setting property values.
    /// </summary>
    /// <param name="key">The identifier of the column to set.</param>
    /// <param name="value">The new value for the property.</param>
    /// <returns>The same builder instance for fluent chaining.</returns>
    IStepBuilder WithOptionalDynamic(ColumnIdentifier key, object value);

    /// <summary>
    /// Sets a property with a specific value and type, overriding any existing value.
    /// This method is for specific scenarios (e.g., deserialization) where the type is explicitly known.
    /// </summary>
    /// <param name="key">The identifier of the column to set.</param>
    /// <param name="value">The new value for the property.</param>
    /// <param name="type">The semantic property type.</param>
    /// <returns>The same builder instance for fluent chaining.</returns>
    IStepBuilder WithProperty(ColumnIdentifier key, object value, PropertyType type);

    /// <summary>
    /// Constructs and returns the final immutable <see cref="Step"/> object.
    /// </summary>
    /// <returns>A new <see cref="Step"/> instance.</returns>
    Step Build();
}