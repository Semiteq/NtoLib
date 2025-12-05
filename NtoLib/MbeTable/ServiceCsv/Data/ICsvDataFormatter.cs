using FluentResults;

using NtoLib.MbeTable.ModuleCore.Entities;

namespace NtoLib.MbeTable.ServiceCsv.Data;

/// <summary>
/// Formats Recipe objects to CSV format.
/// </summary>
public interface ICsvDataFormatter
{
	/// <summary>
	/// Formats a recipe to CSV string representation.
	/// </summary>
	/// <param name="recipe">Recipe to format.</param>
	/// <returns>Result containing CSV string or error information.</returns>
	Result<string> FormatToCsv(Recipe recipe);
}
