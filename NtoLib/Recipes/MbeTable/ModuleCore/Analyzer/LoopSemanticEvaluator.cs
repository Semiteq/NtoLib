using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Loops;
using NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Warnings;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Analyzer;

/// <summary>
/// Applies semantic rules: iteration extraction, max depth detection, integrity flags.
/// </summary>
public sealed class LoopSemanticEvaluator
{
	private const int ForLoopActionId = (int)ServiceActions.ForLoop;
	private const int MaxDepth = 3;

	public LoopSemanticsResult Evaluate(LoopParseResult parseResult)
	{
		var reasons = new List<IReason>();
		var loopIntegrity = false;
		var maxDepthExceeded = false;
		var enriched = new List<LoopNode>();

		foreach (var node in parseResult.Nodes)
		{
			var status = node.Status;
			if (status == LoopStatus.Incomplete || status == LoopStatus.OrphanEnd)
			{
				loopIntegrity = true;
			}

			var effectiveIterations = node.IterationCountRaw <= 0 ? 1 : node.IterationCountRaw;

			if (node.NestingDepth > MaxDepth)
			{
				maxDepthExceeded = true;
				reasons.Add(new CoreForLoopMaxDepthExceededWarning(node.StartIndex, MaxDepth));
			}

			var enrichedNode = node with { EffectiveIterationCount = effectiveIterations };

			enriched.Add(enrichedNode);
		}

		// Semantic evaluator отвечает только за свои семантические причины.
		// Parse‑причины (parseResult.Reasons) добавляются на уровне RecipeAnalyzer.

		return new LoopSemanticsResult(enriched, reasons, loopIntegrity, maxDepthExceeded);
	}
}
