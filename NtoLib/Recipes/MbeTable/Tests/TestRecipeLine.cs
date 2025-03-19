using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Tests
{
    // Dummy implementation for testing purposes
    public class TestRecipeLine : RecipeLine
    {
        public TestRecipeLine(string name) : base(name) { }
        public override ActionTime ActionTime => ActionTime.TimeSetpoint;
    }
}