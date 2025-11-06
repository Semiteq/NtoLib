using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Common;

/// <summary>
/// Loader boundary that throws ConfigException on failure.
/// </summary>
public interface IConfigurationLoader
{
    AppConfiguration LoadConfiguration(string configurationDirectory, params string[] fileNames);
}