using System.Collections.Generic;
using System.Linq;

using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Warnings;

public sealed class AssemblyColumnNotApplicableWarning : BilingualWarning
{
	public IReadOnlyList<Occurrence> Occurrences { get; }

	public AssemblyColumnNotApplicableWarning(IReadOnlyList<Occurrence> occurrences)
		: base(
			BuildEnglishMessage(occurrences),
			BuildRussianMessage(occurrences))
	{
		Occurrences = occurrences;
	}

	private static string BuildEnglishMessage(IReadOnlyList<Occurrence> occurrences)
	{
		var lines = string.Join(", ", occurrences.Select(o => o.LineNumber.ToString()));
		return $"Extra values in non-applicable columns ignored in {occurrences.Count} row(s). Lines: {lines}";
	}

	private static string BuildRussianMessage(IReadOnlyList<Occurrence> occurrences)
	{
		var lines = string.Join(", ", occurrences.Select(o => o.LineNumber.ToString()));
		return $"Найдены лишние поля в {occurrences.Count} строк(е/ах). Значения игнорированы. Строки: {lines}";
	}

	public sealed class Occurrence
	{
		public short ActionId { get; }
		public string ColumnCode { get; }
		public string Value { get; }
		public int LineNumber { get; }

		public Occurrence(short actionId, string columnCode, string value, int lineNumber)
		{
			ActionId = actionId;
			ColumnCode = columnCode;
			Value = value;
			LineNumber = lineNumber;
		}
	}
}
