namespace NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime
{
    public interface ILineChangeManager
    {
        void Process(bool isRecipeActive, int currentLine, float expectedStepTime);
    }
}