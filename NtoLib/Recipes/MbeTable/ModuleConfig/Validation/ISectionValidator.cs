using System.Collections.Generic;

using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Validation;

/// <summary>
/// Contract for validating a specific configuration section.
/// </summary>
/// <typeparam name="TDto">The DTO type for the section.</typeparam>
public interface ISectionValidator<in TDto> where TDto : class
{
	/// <summary>
	/// Validates a collection of DTOs according to business rules.
	/// </summary>
	/// <param name="items">The items to validate.</param>
	/// <returns>Result indicating success or validation errors.</returns>
	Result Validate(IReadOnlyList<TDto> items);
}
