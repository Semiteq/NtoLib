using System;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties.Contracts;
using NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Properties.Definitions;

/// <summary>
/// Configurable string definition with optional MaxLength constraint.
/// </summary>
public sealed class ConfigurableStringDefinition : IPropertyTypeDefinition
{
	private const int DefaultMaxLength = 255;
	private readonly int _maxLength;

	public ConfigurableStringDefinition(YamlPropertyDefinition dto)
	{
		_maxLength = Math.Max(0, dto.MaxLength ?? DefaultMaxLength);
	}

	/// <inheritdoc/>
	public string Units => string.Empty;

	/// <inheritdoc/>
	public bool NonNegative => false;

	/// <inheritdoc/>
	public Result<object> GetNonNegativeValue(object value)
	{
		return value;
	}

	/// <inheritdoc/>
	public Type SystemType => typeof(string);

	/// <inheritdoc/>
	public FormatKind FormatKind => FormatKind.Numeric;

	/// <inheritdoc/>
	public object DefaultValue => string.Empty;

	/// <inheritdoc/>
	public Result TryValidate(object value)
	{
		var s = value?.ToString() ?? string.Empty;

		return s.Length > _maxLength
			? new CoreStringLengthExceededError(s.Length, _maxLength)
			: Result.Ok();
	}

	/// <inheritdoc/>
	public string FormatValue(object value)
	{
		return value?.ToString() ?? string.Empty;
	}

	/// <inheritdoc/>
	public Result<object> TryParse(string input)
	{
		return Result.Ok<object>(input ?? string.Empty);
	}
}
