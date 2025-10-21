using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;
using NtoLib.Recipes.MbeTable.ModuleConfig.Sections;

namespace NtoLib.Recipes.MbeTable.ModuleConfig;

/// <summary>
/// Immutable container for the loaded and validated application configuration.
/// Created once during initialization and reused across the application lifecycle.
/// </summary>
public sealed record ConfigurationState(
    ConfigurationSections Sections,
    AppConfiguration AppConfiguration);