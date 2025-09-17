#nullable enable

using FluentResults;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Validators;

/// <summary>
/// Defines a contract for performing high-level validation of the entire application configuration.
/// </summary>
public interface IAppConfigurationValidator
{
    /// <summary>
    /// Validates the consistency and integrity between different parts of the application configuration.
    /// </summary>
    /// <param name="config">The fully loaded application configuration object.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Result Validate(AppConfiguration config);
}