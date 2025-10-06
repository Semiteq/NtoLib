using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using NtoLib.Recipes.MbeTable.Config;

namespace NtoLib.Recipes.MbeTable;

public partial class MbeTableFB
{
    /// <summary>
    /// Returns all defined pin group names from configuration.
    /// </summary>
    /// <returns>Collection of group names.</returns>
    public IReadOnlyCollection<string> GetDefinedGroupNames()
    {
        var state = EnsureConfigurationLoaded();
        return state.AppConfiguration.PinGroupData
            .Select(g => g.GroupName)
            .ToArray();
    }

    /// <summary>
    /// Reads target values from specified pin group.
    /// </summary>
    /// <param name="groupName">Name of the pin group to read.</param>
    /// <returns>Dictionary mapping pin indices to their string values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when groupName is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when group is not found in configuration.</exception>
    public Dictionary<int, string> ReadTargets(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentNullException(nameof(groupName));
        
        var state = EnsureConfigurationLoaded();
        var pinGroup = state.AppConfiguration.PinGroupData
            .FirstOrDefault(g => string.Equals(g.GroupName, groupName, StringComparison.OrdinalIgnoreCase));

        return pinGroup == null 
            ? throw new InvalidOperationException($"Group '{groupName}' is not defined in PinGroupDefs.yaml.") 
            : ReadPinGroup(pinGroup.FirstPinId, pinGroup.PinQuantity);
    }

    /// <summary>
    /// Ensures configuration is loaded, using lazy initialization pattern.
    /// </summary>
    /// <returns>Loaded configuration state.</returns>
    private ConfigurationState EnsureConfigurationLoaded()
    {
        _configurationStateLazy ??= new Lazy<ConfigurationState>(LoadConfigurationInternal, LazyThreadSafetyMode.ExecutionAndPublication);
        return _configurationStateLazy.Value;
    }

    /// <summary>
    /// Internal method to load configuration from YAML files.
    /// </summary>
    /// <returns>Loaded and validated configuration state.</returns>
    private ConfigurationState LoadConfigurationInternal()
    {
        var configurationDirectory = Path.Combine(AppContext.BaseDirectory, ConfigFolderName);
        var facade = new ConfigurationBootstrapperFacade();

        return facade.LoadConfiguration(
            configurationDirectory,
            PropertyDefsFileName,
            ColumnDefsFileName,
            PinGroupDefsFileName,
            ActionsDefsFileName);
    }
}

