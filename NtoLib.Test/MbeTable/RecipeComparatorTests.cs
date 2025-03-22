using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Test.MbeTable;

// Dummy interfaces/classes to simulate RecipeLine and its cells.
// В реальном проекте они уже должны быть определены.
public abstract class RecipeLine
{
    public abstract List<IDummyCell> Cells { get; }
}

// Minimal interface for a cell supporting value extraction.
public interface IDummyCell
{
    object GetValue();
}

public class DummyCellImpl : IDummyCell
{
    private readonly object _value;
    public DummyCellImpl(object value) => _value = value;
    public object GetValue() => _value;
}

// Dummy RecipeLine implementation used in tests.
public class DummyRecipeLine : RecipeLine
{
    private readonly List<IDummyCell> _cells;
    public DummyRecipeLine(List<IDummyCell> cells) => _cells = cells;
    public override List<IDummyCell> Cells => _cells;
}

// Dummy RecipeLineFactory implementation for testing.
// Фабрика создаёт DummyRecipeLine с 6 ячейками.
public static class RecipeLineFactory
{
    public static RecipeLine NewLine(string command, int number, float initialValue, float setpoint, float speed, float timeSetpoint, string comment)
    {
        // For testing we simulate cells with numeric values.
        var cells = new List<IDummyCell>
        {
            new DummyCellImpl(number),
            new DummyCellImpl(initialValue),
            new DummyCellImpl(setpoint),
            new DummyCellImpl(speed),
            new DummyCellImpl(timeSetpoint),
            new DummyCellImpl(0) // Comment cell simulated as 0
        };
        return new DummyRecipeLine(cells);
    }
    
    public static RecipeLine NewLine(int[] intData, int[] floatData, int[] boolData, int index)
    {
        // Create dummy cells from provided arrays (simplified for test)
        var cells = new List<IDummyCell>
        {
            new DummyCellImpl(intData.Length > 0 ? intData[0] : 0),
            new DummyCellImpl(intData.Length > 1 ? intData[1] : 0),
            new DummyCellImpl(floatData.Length > 0 ? floatData[0] : 0),
            new DummyCellImpl(floatData.Length > 1 ? floatData[1] : 0),
            new DummyCellImpl(floatData.Length > 2 ? floatData[2] : 0),
            new DummyCellImpl(floatData.Length > 3 ? floatData[3] : 0)
        };
        return new DummyRecipeLine(cells);
    }
}

[TestClass]
public class RecipeComparatorTests
{
    private RecipeLine CreateRecipeLine(params object[] values)
    {
        var cells = values.Select(v => new DummyCellImpl(v) as IDummyCell).ToList();
        return new DummyRecipeLine(cells);
    }
    
    // [TestMethod]
    // public void Compare_EqualRecipes_ReturnsTrue()
    // {
    //     // Create two recipes with identical lines.
    //     var line1 = CreateRecipeLine(1, 2, 3, 4);
    //     var line2 = CreateRecipeLine(10, 20, 30, 40);
    //     // Последняя строка – комментарий, считаем её одинаковой.
    //     var recipe1 = new List<RecipeLine> { line1, line2 };
    //     var recipe2 = new List<RecipeLine> { line1, line2 };
    //
    //     var comparator = new RecipeComparator();
    //     var result = comparator.Compare(recipe1, recipe2);
    //     Assert.IsTrue(result, "Equal recipes must return true.");
    // }
    //
    // [TestMethod]
    // public void Compare_DifferentRecipes_ReturnsFalse()
    // {
    //     var line1 = CreateRecipeLine(1, 2, 3, 4);
    //     var line2 = CreateRecipeLine(1, 2, 3, 5); // one cell different
    //     var recipe1 = new List<RecipeLine> { line1, line1 };
    //     var recipe2 = new List<RecipeLine> { line1, line2 };
    //
    //     var comparator = new RecipeComparator();
    //     var result = comparator.Compare(recipe1, recipe2);
    //     Assert.IsFalse(result, "Recipes with differences must return false.");
    // }
}