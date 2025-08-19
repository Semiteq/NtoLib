using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps;

public interface IStepBuilder
{
    void InitializeStep();
    void SetAction(int actionId);
    IReadOnlyCollection<ColumnKey> NonNullKeys { get; }
    bool Supports(ColumnKey key);
    
    // --- Properties ---
    StepBuilder WithOptionalTarget(int? target);
    StepBuilder WithOptionalInitialValue(float? value);
    StepBuilder WithOptionalSetpoint(float? value);
    StepBuilder WithOptionalSpeed(float? value);
    StepBuilder WithOptionalDuration(float? value);
    StepBuilder WithOptionalComment(string comment);
    StepBuilder WithDeployDuration(DeployDuration duration);
    
    Step Build();
    StepBuilder WithProperty(ColumnKey key, object value, PropertyType type);
    StepBuilder WithOptionalDynamic(ColumnKey key, object value);
}