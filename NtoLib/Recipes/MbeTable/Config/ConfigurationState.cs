

using NtoLib.Recipes.MbeTable.Config.Domain;
using NtoLib.Recipes.MbeTable.Config.Sections;

namespace NtoLib.Recipes.MbeTable.Config;

/// <summary>
/// Immutable container for the loaded and validated application configuration.
/// Created once during initialization and reused across the application lifecycle.
/// </summary>
public sealed record ConfigurationState(
    ConfigurationSections Sections,
    AppConfiguration AppConfiguration);