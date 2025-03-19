#nullable enable
namespace NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime
{
    // Interface for processing line change events to allow unit testing.
    public interface ILineChangeProcessor
    {
        void Process(bool isRecipeActive, int currentLine, float expectedStepTime, ICountTimer? countdownTimer);
    }
}