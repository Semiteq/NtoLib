using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Warnings;

public sealed class CsvRowCountMismatchWarning : BilingualWarning
{
    public int Expected { get; }
    public int Actual { get; }

    public CsvRowCountMismatchWarning(int expected, int actual)
        : base(
            $"Row count mismatch: expected {expected}, got {actual}",
            $"Несоответствие количества строк: ожидалось {expected}, получено {actual}")
    {
        Expected = expected;
        Actual = actual;
    }
}