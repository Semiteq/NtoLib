using System.Collections.Generic;
using System.Linq;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.Recipes.MbeTable.ModuleConfig.Errors;
using NtoLib.Recipes.MbeTable.ModuleCore.Formulas;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Formulas;

// Default implementation of formula precompilation.
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
                var reason = string.Join("; ", result.Errors.Select(e => e.Message));
                errors.Add(new FormulaCompilationError(
                    message: $"Failed to compile formula: {reason}.",
                    actionId: action.Id,
                    actionName: action.Name,
                    expression: expr));
                continue;
            }
            
            compiled[action.Id] = result.Value;
        }

        return errors.Count > 0
            ? Result.Fail<IReadOnlyDictionary<short, CompiledFormula>>(errors)
            : compiled;
    }
}