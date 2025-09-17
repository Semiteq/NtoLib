#nullable enable

using System.Collections.Immutable;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Entities;

/// <summary>
/// Represents an immutable snapshot of an entire recipe.
/// </summary>
/// <param name="Steps">The immutable list of steps that make up the recipe.</param>
public record Recipe(IImmutableList<Step> Steps);