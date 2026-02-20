using System;
using System.Collections.Generic;

using NtoLib.Recipes.MbeTable.ModuleCore.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Properties;

/// <summary>
/// Registry of property type definitions keyed by string PropertyTypeId.
/// </summary>
public sealed class PropertyDefinitionRegistry
{
	private readonly IReadOnlyDictionary<string, IPropertyTypeDefinition> _definitions;

	/// <summary>
	/// Initializes a new instance with provided definitions.
	/// </summary>
	public PropertyDefinitionRegistry(IReadOnlyDictionary<string, IPropertyTypeDefinition> definitions)
	{
		_definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
	}

	/// <summary>
	/// Gets a property type definition by its string id.
	/// </summary>
	/// <param name="propertyTypeId">The string type id.</param>
	/// <returns>The type definition.</returns>
	/// <exception cref="KeyNotFoundException">When type id is missing after successful configuration load.</exception>
	public IPropertyTypeDefinition GetPropertyDefinition(string propertyTypeId)
	{
		if (_definitions.TryGetValue(propertyTypeId, out var d))
		{
			return d;
		}

		throw new KeyNotFoundException($"No definition registered for PropertyTypeId: '{propertyTypeId}'.");
	}
}
