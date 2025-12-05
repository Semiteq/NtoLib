using System.Collections.Generic;

using FluentResults;

namespace NtoLib.MbeTable.ModuleConfig.Common;

/// <summary>
/// Generic contract for loading and validating YAML sections.
/// </summary>
/// <typeparam name="TDto">The DTO type for the section.</typeparam>
public interface IFileLoader<TDto> where TDto : class
{
	/// <summary>
	/// Loads and validates a YAML section from file.
	/// </summary>
	/// <param name="filePath">Path to the YAML file.</param>
	/// <returns>Result containing the loaded items or errors.</returns>
	Result<IReadOnlyList<TDto>> Load(string filePath);
}
