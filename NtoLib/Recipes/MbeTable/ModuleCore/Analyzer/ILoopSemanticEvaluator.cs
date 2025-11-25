namespace NtoLib.Recipes.MbeTable.ModuleCore.Analyzer;

public interface ILoopSemanticEvaluator
{
	LoopSemanticsResult Evaluate(LoopParseResult parseResult);
}
