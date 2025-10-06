using System;
using System.Threading.Tasks;

namespace NtoLib.Recipes.MbeTable.Presentation;

/// <summary>
/// Orchestrates view, commands and application services (MVP root).
/// </summary>
public interface ITablePresenter : IDisposable
{
    void Initialize();
    Task LoadRecipeAsync();
    Task SaveRecipeAsync();
    Task SendRecipeAsync();
    Task ReceiveRecipeAsync();
    Task AddStepAfterCurrent();
    Task AddStepBeforeCurrent();
    Task RemoveCurrentStep();
}