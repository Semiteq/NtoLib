using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NtoLib.Recipes.MbeTable.Recipe;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;

namespace NtoLib.Recipes.MbeTable.RecipeLines
{
    public interface IRecipeComparator
    {
        bool Compare(List<Step> recipe1, List<Step> recipe2);
    }

    public class RecipeComparator : IRecipeComparator
    {
        public bool Compare(List<Step> recipe1, List<Step> recipe2)
        {
            // If recipes have different row counts, they are not equal.
            if (recipe1.Count != recipe2.Count)
                return false;

            // Iterate over rows except the last one which is comment.
            for (var i = 0; i < recipe1.Count - 1; i++)
            {
                // var row1 = recipe1[i].Cells;
                // var row2 = recipe2[i].Cells;

                // If the number of cells in the rows differ, they are not equal.
                // if (row1.Count != row2.Count)
                //     return false;
                //
                // // Extract cell values for comparison.
                // var values1 = row1.Select(cell => cell.GetValue());
                // var values2 = row2.Select(cell => cell.GetValue());

                // Use SequenceEqual to compare the sequences.
                // if (!values1.SequenceEqual(values2))
                {
                    Debug.WriteLine($"Row {i} differs.");
                    return false;
                }
            }
            return true;
        }
    }
}
