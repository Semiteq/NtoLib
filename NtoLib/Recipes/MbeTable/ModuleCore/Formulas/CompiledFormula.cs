using System;
using System.Collections.Generic;
using System.Linq;

using AngouriMath;
using AngouriMath.Core.Exceptions;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Formulas;

public sealed class CompiledFormula
{
    private static readonly StringComparer VariableNameComparer = StringComparer.OrdinalIgnoreCase;

    private readonly IReadOnlyList<string> _recalcOrder;
    private readonly IReadOnlyList<string> _variables;
    private readonly IReadOnlyDictionary<string, Func<Dictionary<string, double>, double>> _solvers;

    private CompiledFormula(
        IReadOnlyList<string> recalcOrder,
        IReadOnlyList<string> variables,
        IReadOnlyDictionary<string, Func<Dictionary<string, double>, double>> solvers)
    {
        _recalcOrder = recalcOrder;
        _variables = variables;
        _solvers = solvers;
    }

    public static Result<CompiledFormula> Create(string expression, IReadOnlyList<string> recalcOrder, ILogger logger)
    {
        var validationResult = ValidateInput(expression, recalcOrder);
        if (validationResult.IsFailed)
            return validationResult;

        var compilationResult = CompileFormula(expression, recalcOrder, logger);
        return compilationResult;
    }

    public Result<IReadOnlyDictionary<string, double>> ApplyRecalculation(
        string changedVariable,
        IReadOnlyDictionary<string, double> currentValues)
    {
        var preparationResult = PrepareCalculation(changedVariable);
        if (preparationResult.IsFailed)
            return preparationResult.ToResult<IReadOnlyDictionary<string, double>>();

        var targetVariable = preparationResult.Value;

        var computationResult = ComputeTargetValue(targetVariable, currentValues);
        if (computationResult.IsFailed)
            return computationResult.ToResult<IReadOnlyDictionary<string, double>>();

        return CreateCalculationResult(changedVariable, currentValues[changedVariable], targetVariable,
            computationResult.Value);
    }

    private static Result<CompiledFormula> ValidateInput(string expression, IReadOnlyList<string> recalcOrder)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return Errors.FormulaEmptyExpression();

        if (recalcOrder.Count == 0)
            return Errors.FormulaEmptyRecalcOrder();

        return Result.Ok();
    }

    private static Result<CompiledFormula> CompileFormula(
        string expression,
        IReadOnlyList<string> recalcOrder,
        ILogger logger)
    {
        var parseResult = ParseExpression(expression, logger);
        if (parseResult.IsFailed)
            return parseResult.ToResult<CompiledFormula>();

        var parsedExpression = parseResult.Value;
        var variables = ExtractVariables(parsedExpression);

        var validationResult = ValidateFormulaStructure(variables, parsedExpression, recalcOrder);
        if (validationResult.IsFailed)
            return validationResult.ToResult<CompiledFormula>();

        return BuildCompiledFormula(parsedExpression, recalcOrder, variables, logger);
    }

    private static Result ValidateFormulaStructure(
        HashSet<string> variables,
        Entity parsedExpression,
        IReadOnlyList<string> recalcOrder)
    {
        var orderValidationResult = ValidateRecalcOrder(variables, recalcOrder);
        if (orderValidationResult.IsFailed) return orderValidationResult;

        var linearityResult = ValidateLinearity(variables, parsedExpression);
        return linearityResult;
    }

    private static Result<CompiledFormula> BuildCompiledFormula(
        Entity parsedExpression,
        IReadOnlyList<string> recalcOrder,
        HashSet<string> variables,
        ILogger logger)
    {
        var solversResult = CompileSolvers(variables, parsedExpression, logger);
        if (solversResult.IsFailed)
            return solversResult.ToResult<CompiledFormula>();

        return new CompiledFormula(
            recalcOrder,
            variables.ToList(),
            solversResult.Value);
    }

    private static Result<Entity> ParseExpression(string expression, ILogger logger)
    {
        try
        {
            Entity parsedExpression = expression;
            return Result.Ok(parsedExpression);
        }
        catch
        {
            logger.LogError("Failed to parse formula expression: '{Expression}'", expression);
            return Errors.FormulaInvalidExpression();
        }
    }

    private static HashSet<string> ExtractVariables(Entity expression) 
        => expression.Vars.Select(v => v.Name).ToHashSet(VariableNameComparer);
    

    private static Result ValidateRecalcOrder(HashSet<string> variables, IReadOnlyList<string> recalcOrder)
    {
        var variablesNotInRecalcOrder = variables.Where(v => !recalcOrder.Contains(v, VariableNameComparer)).ToList();
        if (variablesNotInRecalcOrder.Any())
            return Errors.FormulaRecalcOrderMissing(string.Join(", ", variablesNotInRecalcOrder));

        var orderVariablesNotInFormula = recalcOrder.Where(v => !variables.Contains(v, VariableNameComparer)).ToList();
        
        return orderVariablesNotInFormula.Any() 
            ? Errors.FormulaRecalcOrderExtra(string.Join(", ", orderVariablesNotInFormula)) 
            : Result.Ok();
    }

    private static Result ValidateLinearity(HashSet<string> variables, Entity expression)
    {
        var nonLinearVariables = variables.Where(v => CountOccurrencesInExpression(expression, v) > 1).ToList();
        return nonLinearVariables.Any()
            ? Errors.FormulaNonLinear()
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

    private static Result<Dictionary<string, Func<Dictionary<string, double>, double>>> CompileSolvers(
        HashSet<string> variables,
        Entity parsedExpression,
        ILogger logger)
    {
        var solvers = new Dictionary<string, Func<Dictionary<string, double>, double>>(VariableNameComparer);

        foreach (var variable in variables)
        {
            var solverResult = CreateSolverForVariable(parsedExpression, variable, variables, logger);
            if (solverResult.IsFailed)
                return solverResult.ToResult<Dictionary<string, Func<Dictionary<string, double>, double>>>();
            solvers[variable] = solverResult.Value;
        }

        return solvers;
    }

    private static Result<Func<Dictionary<string, double>, double>> CreateSolverForVariable(
        Entity parsedExpression,
        string targetVariable,
        HashSet<string> allVariables,
        ILogger logger)
    {
        try
        {
            var solved = parsedExpression.Solve(targetVariable);
            var extracted = ExtractSingleElementIfSet(solved);
            var simplified = extracted.Simplify();

            logger.LogDebug(
                "Solved expression for variable '{TargetVariable}': {Expression}",
                targetVariable,
                simplified.Stringize());

            var solver = BuildSolverFunction(simplified, targetVariable, allVariables);
            return Result.Ok(solver);
        }
        catch (Exception ex)
        {
            return Errors.FormulaComputationFailed(ex.Message);
        }
    }

    private static Entity ExtractSingleElementIfSet(Entity expression)
    {
        if (expression is Entity.Set)
        {
            var elems = expression.DirectChildren;
            var count = elems.Count();
            if (count == 1)
                return elems.First();
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

            var expr = ExtractSingleElementIfSet(substituted);

            if (!expr.EvaluableNumerical)
                throw new InvalidOperationException($"Cannot evaluate expression for '{targetVariable}' as a number");

            var result = expr.EvalNumerical();
            return (double)result.RealPart;
        };
    }

    private static List<string> GetOtherVariables(string targetVariable, HashSet<string> allVariables)
    {
        return allVariables.Where(v => !string.Equals(v, targetVariable, StringComparison.OrdinalIgnoreCase)).ToList();
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

    private Result<string> PrepareCalculation(
        string changedVariable)
    {
        var validationResult = EnsureVariableIsKnown(changedVariable);
        if (validationResult.IsFailed) 
            return validationResult.ToResult<string>();

        var targetVariable = DetermineTarget(changedVariable);
        if (targetVariable == null) 
            return Errors.FormulaTargetNotFound();

        return Result.Ok(targetVariable);
    }

    private Result EnsureVariableIsKnown(string variableName)
    {
        return _variables.Contains(variableName, VariableNameComparer)
            ? Result.Ok()
            : Errors.FormulaVariableUnknown(variableName);
    }

    private string? DetermineTarget(string changedVariable)
    {
        return _recalcOrder.FirstOrDefault(v => !string.Equals(v, changedVariable, StringComparison.OrdinalIgnoreCase));
    }

    private Result<double> ComputeTargetValue(string targetVariable, IReadOnlyDictionary<string, double> values)
    {
        if (!_solvers.TryGetValue(targetVariable, out var solver))
            return Errors.FormulaTargetNotFound();

        try
        {
            var calculatedValue = solver((Dictionary<string, double>)values);
            return ValidateCalculationResult(calculatedValue);
        }
        catch (CannotEvalException ex)
        {
            return Errors.FormulaComputationFailed(ex.Message);
        }
        catch (DivideByZeroException)
        {
            return Errors.FormulaDivisionByZero(targetVariable);
        }
        catch (Exception ex)
        {
            return Errors.FormulaComputationFailed(ex.Message);
        }
    }

    private static Result<double> ValidateCalculationResult(double value)
    {
        return double.IsNaN(value) || double.IsInfinity(value) 
            ? Errors.FormulaComputationFailed("result is NaN or Infinity") 
            : value;
    }

    private static Result<IReadOnlyDictionary<string, double>> CreateCalculationResult(
        string changedVariable,
        double changedValue,
        string targetVariable,
        double targetValue)
    {
        return Result.Ok<IReadOnlyDictionary<string, double>>(
            new Dictionary<string, double>(VariableNameComparer)
            {
                [changedVariable] = changedValue,
                [targetVariable] = targetValue
            });
    }
}