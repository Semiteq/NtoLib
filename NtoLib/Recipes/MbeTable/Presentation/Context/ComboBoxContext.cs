using System;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;

namespace NtoLib.Recipes.MbeTable.Presentation.Context;

/// <summary>
/// Concrete implementation of <see cref="IComboBoxContext"/>.
/// Registered as singleton in DI container.
/// </summary>
public sealed class ComboBoxContext : IComboBoxContext
{
    /// <inheritdoc />
    public ILogger Logger { get; }

    /// <inheritdoc />
    public RecipeViewModel RecipeViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComboBoxContext"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    /// <param name="recipeViewModel">RecipeViewModel for VirtualMode data access.</param>
    public ComboBoxContext(ILogger logger, RecipeViewModel recipeViewModel)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        RecipeViewModel = recipeViewModel ?? throw new ArgumentNullException(nameof(recipeViewModel));
    }
}