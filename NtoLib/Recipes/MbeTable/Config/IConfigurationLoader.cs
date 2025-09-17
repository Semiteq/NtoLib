#nullable enable

using FluentResults;

namespace NtoLib.Recipes.MbeTable.Config;

/// <summary>
/// Defines a service for loading the application's configuration.
/// </summary>
public interface IConfigurationLoader
{
    Result<AppConfiguration> LoadConfiguration(ConfigFiles configFiles
        );
}