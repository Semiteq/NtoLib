#nullable enable

using FluentResults;
using NtoLib.Recipes.MbeTable.Composition;

namespace NtoLib.Recipes.MbeTable.Config;

/// <summary>
/// Defines a service for loading the application's configuration.
/// </summary>
public interface IConfigurationLoader
{
    /// <summary>
    /// Loads, parses, and validates configuration from external sources (e.g., JSON files).
    /// </summary>
    /// <returns>A <see cref="Result{T}"/> containing the <see cref="AppConfiguration"/> on success, or an error on failure.</returns>
    Result<AppConfiguration> LoadConfiguration(string baseDirectory, string schemaConfigFileName, string actionsConfigFileName);
}