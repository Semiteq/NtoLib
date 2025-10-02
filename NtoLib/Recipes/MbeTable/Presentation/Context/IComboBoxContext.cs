using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;

namespace NtoLib.Recipes.MbeTable.Presentation.Context;

/// <summary>
/// Provides dependencies required by ComboBox cells operating in VirtualMode.
/// Replaces static state pattern with DI-managed context.
/// </summary>
public interface IComboBoxContext
{
    /// <summary>
    /// Logger for diagnostic messages and structured errors.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// RecipeViewModel for accessing StepViewModels by row index in VirtualMode.
    /// </summary>
    RecipeViewModel RecipeViewModel { get; }
}