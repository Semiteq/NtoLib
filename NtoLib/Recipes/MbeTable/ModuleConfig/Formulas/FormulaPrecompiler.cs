using System;
using System.Collections.Generic;
using System.Linq;

using AngouriMath;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.Recipes.MbeTable.ModuleConfig.Errors;
using NtoLib.Recipes.MbeTable.ModuleCore.Formulas;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Formulas;

/// <summary>
/// Precompiles formulas from action definitions into compiled formula objects.
/// </summary>
public sealed class FormulaPrecompiler : IFormulaPrecompiler
{
    private static readonly StringComparer VariableNameComparer = StringComparer.OrdinalIgnoreCase;
    
    private readonly ILogger<FormulaPrecompiler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormulaPrecompiler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public FormulaPrecompiler(ILogger<FormulaPrecompiler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Precompiles formulas from action definitions.
    /// </summary>
    /// <param name="actions">The action definitions containing formulas to compile.</param>
    /// <returns>A dictionary mapping action IDs to compiled formulas.</returns>
    public Result<IReadOnlyDictionary<short, CompiledFormula>> Precompile(
        IReadOnlyDictionary<short, ActionDefinition> actions)
    {
        var compiled = new Dictionary<short, CompiledFormula>();
        var errors = new List<IError>();

        foreach (var action in actions.Values.Where(a => a.Formula != null))
        {
            var expression = action.Formula!.Expression ?? string.Empty;
            var recalcOrder = action.Formula!.RecalcOrder;

            var compilationResult = CompileFormula(expression, recalcOrder);
            if (compilationResult.IsFailed)
            {
                var configError = ConvertToConfigError(action, compilationResult.Errors);
                errors.Add(configError);
                continue;
            }

            compiled[action.Id] = compilationResult.Value;
        }

        return errors.Count > 0
            ? Result.Fail<IReadOnlyDictionary<short, CompiledFormula>>(errors)
            : compiled;
    }

    private Result<CompiledFormula> CompileFormula(string expression, IReadOnlyList<string> recalcOrder)
    {
        var validationResult = ValidateInput(expression, recalcOrder);
        if (validationResult.IsFailed)
            return validationResult.ToResult<CompiledFormula>();

        var parseResult = ParseExpression(expression);
        if (parseResult.IsFailed)
            return parseResult.ToResult<CompiledFormula>();

        var parsedExpression = parseResult.Value;
        var variables = ExtractVariables(parsedExpression);

        var structureValidationResult = ValidateFormulaStructure(variables, parsedExpression, recalcOrder);
        if (structureValidationResult.IsFailed)
            return structureValidationResult.ToResult<CompiledFormula>();

        return BuildCompiledFormula(parsedExpression, recalcOrder, variables);
    }

    private static Result ValidateInput(string expression, IReadOnlyList<string> recalcOrder)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return new ConfigFormulaEmptyExpressionError();

        if (recalcOrder.Count == 0)
            return new ConfigFormulaEmptyRecalcOrderError();

        return Result.Ok();
    }

    private Result<Entity> ParseExpression(string expression)
    {
        try
        {
            return (Entity)expression;
        }
        catch
        {
            _logger.LogError("Failed to parse formula expression: '{Expression}'", expression);
            return new ConfigFormulaInvalidExpressionError(expression);
        }
    }

    private static HashSet<string> ExtractVariables(Entity expression) 
        => expression.Vars.Select(v => v.Name).ToHashSet(VariableNameComparer);

    private static Result ValidateFormulaStructure(
        HashSet<string> variables,
        Entity parsedExpression,
        IReadOnlyList<string> recalcOrder)
    {
        var orderValidationResult = ValidateRecalcOrder(variables, recalcOrder);
        if (orderValidationResult.IsFailed)
            return orderValidationResult;

        var linearityResult = ValidateLinearity(variables, parsedExpression);
        return linearityResult;
    }

    private static Result ValidateRecalcOrder(HashSet<string> variables, IReadOnlyList<string> recalcOrder)
    {
        var variablesNotInRecalcOrder = variables
            .Where(v => !recalcOrder.Contains(v, VariableNameComparer))
            .ToList();
        
        if (variablesNotInRecalcOrder.Any())
            return new ConfigFormulaRecalcOrderMissingError(string.Join(", ", variablesNotInRecalcOrder));

        var orderVariablesNotInFormula = recalcOrder
            .Where(v => !variables.Contains(v, VariableNameComparer))
            .ToList();

        return orderVariablesNotInFormula.Any()
            ? new ConfigFormulaRecalcOrderExtraError(string.Join(", ", orderVariablesNotInFormula))
            : Result.Ok();
    }

    private static Result ValidateLinearity(HashSet<string> variables, Entity expression)
    {
        var nonLinearVariables = variables
            .Where(v => CountOccurrencesInExpression(expression, v) > 1)
            .ToList();
        
        return nonLinearVariables.Any()
            ? new ConfigFormulaNonLinearError()
            : Result.Ok();
    }

    private static int CountOccurrencesInExpression(Entity expression, string variableName)
    {
        var isMatchingVariable = expression is Entity.Variable v &&
                                 string.Equals(v.Name, variableName, StringComparison.OrdinalIgnoreCase);

        return isMatchingVariable
            ? 1
            : expression.DirectChildren.Sum(child => CountOccurrencesInExpression(child, variableName));
    }

    private Result<CompiledFormula> BuildCompiledFormula(
        Entity parsedExpression,
        IReadOnlyList<string> recalcOrder,
        HashSet<string> variables)
    {
        var solversResult = CompileSolvers(variables, parsedExpression);
        if (solversResult.IsFailed)
            return solversResult.ToResult<CompiledFormula>();

        return new CompiledFormula(
            recalcOrder,
            variables.ToList(),
            solversResult.Value);
    }

    private Result<Dictionary<string, Func<Dictionary<string, double>, double>>> CompileSolvers(
        HashSet<string> variables,
        Entity parsedExpression)
    {
        var solvers = new Dictionary<string, Func<Dictionary<string, double>, double>>(VariableNameComparer);

        foreach (var variable in variables)
        {
            var solverResult = CreateSolverForVariable(parsedExpression, variable, variables);
            if (solverResult.IsFailed)
                return solverResult.ToResult<Dictionary<string, Func<Dictionary<string, double>, double>>>();
            
            solvers[variable] = solverResult.Value;
        }

        return solvers;
    }

    private Result<Func<Dictionary<string, double>, double>> CreateSolverForVariable(
        Entity parsedExpression,
        string targetVariable,
        HashSet<string> allVariables)
    {
        try
        {
            var solved = parsedExpression.Solve(targetVariable);
            var extracted = ExtractSingleElementIfSet(solved);
            var simplified = extracted.Simplify();

            _logger.LogDebug(
                "Solved expression for variable '{TargetVariable}': {Expression}",
                targetVariable,
                simplified.Stringize());

            var solver = BuildSolverFunction(simplified, targetVariable, allVariables);
            return Result.Ok(solver);
        }
        catch (Exception ex)
        {
            return new ConfigFormulaComputationFailedError(ex.Message);
        }
    }

    private static Entity ExtractSingleElementIfSet(Entity expression)
    {
        if (expression is Entity.Set)
        {
            var elements = expression.DirectChildren;
            var count = elements.Count();
            if (count == 1)
                return elements.First();
        }

        return expression;
    }

    private static Func<Dictionary<string, double>, double> BuildSolverFunction(
        Entity solvedExpression,
        string targetVariable,
        HashSet<string> allVariables)
    {
        var otherVariables = GetOtherVariables(targetVariable, allVariables);

        return values =>
        {
            var substituted = SubstituteValues(solvedExpression, otherVariables, values)
                .Simplify();

            var expression = ExtractSingleElementIfSet(substituted);

            if (!expression.EvaluableNumerical)
                throw new InvalidOperationException($"Cannot evaluate expression for '{targetVariable}' as a number");

            var result = expression.EvalNumerical();
            return (double)result.RealPart;
        };
    }

    private static List<string> GetOtherVariables(string targetVariable, HashSet<string> allVariables)
    {
        return allVariables
            .Where(v => !string.Equals(v, targetVariable, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static Entity SubstituteValues(
        Entity expression,
        List<string> variables,
        Dictionary<string, double> values)
    {
        var result = expression;

        foreach (var variable in variables)
        {
            if (values.TryGetValue(variable, out var value))
                result = result.Substitute(variable, value);
        }

        return result;
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