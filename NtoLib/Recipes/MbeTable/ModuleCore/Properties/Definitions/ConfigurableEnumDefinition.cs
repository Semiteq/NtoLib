using System;
using System.Globalization;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties.Contracts;
using NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Properties.Definitions;

public sealed class ConfigurableEnumDefinition : IPropertyTypeDefinition
{
	public ConfigurableEnumDefinition(YamlPropertyDefinition dto)
	{
		Units = dto.Units;
	}

	/// <inheritdoc/>
	public string Units { get; }

	/// <inheritdoc/>
	public Type SystemType => typeof(short);

	/// <inheritdoc/>
	public FormatKind FormatKind => FormatKind.Numeric;

	/// <inheritdoc/>
	public object DefaultValue => (short)0;

	/// <inheritdoc/>
	public bool NonNegative => false;

	/// <inheritdoc/>
	public Result<object> GetNonNegativeValue(object value)
	{
		return value;
	}

	/// <inheritdoc/>
	public Result TryValidate(object value)
	{
		return value is short
				? Result.Ok()
				: new CorePropertyValidationFailedError("value must be Int16");
	}

	/// <inheritdoc/>
	public string FormatValue(object value)
	{
		return value.ToString();
	}

	/// <inheritdoc/>
	public Result<object> TryParse(string input)
	{
		const NumberStyles NumberStyles = NumberStyles.Integer;
		var invariantCulture = CultureInfo.InvariantCulture;

		return short.TryParse(input, NumberStyles, invariantCulture, out var s)
			? Result.Ok<object>(s)
			: new CorePropertyConversionFailedError(input, "Int16");
	}
}
