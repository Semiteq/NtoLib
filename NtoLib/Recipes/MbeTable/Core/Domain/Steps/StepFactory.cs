#nullable enable

using System;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps;

/// <inheritdoc />
public sealed class StepFactory : IStepFactory
{
    private readonly IActionRepository _actionRepository;
    private readonly PropertyDefinitionRegistry _registry;
    private readonly TableColumns _tableColumns;

    /// <summary>
    /// Initializes a new instance of the <see cref="StepFactory"/> class.
    /// </summary>
    /// <param name="actionRepository">The repository for accessing configured action definitions.</param>
    /// <param name="registry">The registry for property type definitions.</param>
    /// <param name="tableColumns">The schema of the recipe table.</param>
    public StepFactory(IActionRepository actionRepository, PropertyDefinitionRegistry registry, TableColumns tableColumns)
    {
        _actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _tableColumns = tableColumns ?? throw new ArgumentNullException(nameof(tableColumns));
    }

    /// <inheritdoc />
    public IStepBuilder ForAction(int actionId)
    {
        var actionDefinition = _actionRepository.GetActionById(actionId);
        return new StepBuilder(actionDefinition, _registry, _tableColumns);
    }
}