

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Sections;

/// <summary>
/// Aggregate container holding all validated configuration sections.
/// Replaces the monolithic RawConfiguration.
/// </summary>
public sealed record ConfigurationSections(
    PropertyDefsSection PropertyDefs,
    ColumnDefsSection ColumnDefs,
    PinGroupDefsSection PinGroupDefs,
    ActionDefsSection ActionDefs);