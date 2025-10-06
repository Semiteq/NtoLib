namespace NtoLib.Recipes.MbeTable.ModbusTCP.Configuration;

/// <summary>
/// Comparison tolerance for floating-point values.
/// </summary>
/// <param name="Epsilon">Maximum allowed absolute difference between two values.</param>
public sealed record ComparatorOptions(double Epsilon = 1e-5);