using System.Collections.Generic;
using System.Linq;
using FluentResults;
using Microsoft.Extensions.Logging;
using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.Recipes.MbeTable.ModuleCore.Formulas;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Formulas;

public sealed class FormulaPrecompiler : IFormulaPrecompiler
{
    private readonly ILogger<FormulaPrecompiler> _logger;

    public FormulaPrecompiler(ILogger<FormulaPrecompiler> logger)
    {
        _logger = logger;
    }

    public Result<IReadOnlyDictionary<short, CompiledFormula>> Precompile(
        IReadOnlyDictionary<short, ActionDefinition> actions)
    {
        var compiled = new Dictionary<short, CompiledFormula>();
        var errors = new List<IError>();

        foreach (var action in actions.Values.Where(a => a.Formula != null))
        {
            var expr = action.Formula!.Expression ?? string.Empty;
            var order = action.Formula!.RecalcOrder;

            var result = CompiledFormula.Create(expr, order, _logger);
            if (result.IsFailed)
            {
                var configError = ConvertToConfigError(action, result.Errors);
                errors.Add(configError);
                continue;
            }

            compiled[action.Id] = result.Value;
        }

        return errors.Count > 0
            ? Result.Fail<IReadOnlyDictionary<short, CompiledFormula>>(errors)
            : compiled;
    }

    private static ConfigError ConvertToConfigError(ActionDefinition action, IEnumerable<IError> innerErrors)
    {
        var innerErrorMessages = string.Join("; ", innerErrors.Select(e => e.Message));
        var context = $"ActionsDefs.yaml, ActionId={action.Id}, ActionName='{action.Name}'";

        var configError = new ConfigError(
                $"Failed to compile formula: {innerErrorMessages}",
                section: "ActionsDefs.yaml",
                context: context)
            .WithDetail("actionId", action.Id)
            .WithDetail("actionName", action.Name)
            .WithDetail("expression", action.Formula!.Expression ?? string.Empty);

        foreach (var innerError in innerErrors)
        {
            configError.CausedBy(innerError);
        }

        return configError;
    }
}