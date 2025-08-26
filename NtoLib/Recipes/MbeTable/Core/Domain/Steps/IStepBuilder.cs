using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps;

public interface IStepBuilder
{
    void InitializeStep();
    void SetAction(int actionId);
    IReadOnlyCollection<ColumnIdentifier> NonNullKeys { get; }
    bool Supports(ColumnIdentifier key);
    
    // --- Properties ---
    StepBuilder WithOptionalTarget(int? target);
    StepBuilder WithOptionalInitialValue(float? value);
    StepBuilder WithOptionalSetpoint(float? value);
    StepBuilder WithOptionalSpeed(float? value);
    StepBuilder WithOptionalDuration(float? value);
    StepBuilder WithOptionalComment(string comment);
    StepBuilder WithDeployDuration(DeployDuration duration);
    
    Step Build();
    StepBuilder WithProperty(ColumnIdentifier key, object value, PropertyType type);
    StepBuilder WithOptionalDynamic(ColumnIdentifier key, object value);
}