namespace NtoLib.MbeTable.ModuleCore.Analyzer;

public interface ILoopSemanticEvaluator
{
	LoopSemanticsResult Evaluate(LoopParseResult parseResult);
}
