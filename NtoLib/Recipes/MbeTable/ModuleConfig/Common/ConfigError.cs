using System;

using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Common;

/// <summary>
/// Unified configuration error that carries section and contextual details.
/// </summary>
public sealed class ConfigError : Error
{
	public ConfigError(string message, string section, string context, Exception? cause = null)
		: base(message)
	{
		Section = section;
		Context = context;

		AddMeta("section", Section);
		AddMeta("context", Context);

		if (cause != null)
		{
			CausedBy(cause);
		}

		Error AddMeta(string key, object? value)
		{
			return WithMetadata(key, value?.ToString() ?? "null");
		}
	}

	public string Section { get; }
	public string Context { get; }

	public ConfigError WithDetail(string key, object? value)
	{
		WithMetadata(key, value?.ToString() ?? "null");

		return this;
	}

	public static ConfigError From(string section, string context, string message, Exception? cause = null)
	{
		return new(message, section, context, cause);
	}
}
