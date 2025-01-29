using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable
{
    internal class RecipeTimeLeft
    {
        private uint totalTimeLeft = 0; //data in milliseconds
        private uint lineTimeLeft = 0; //data in milliseconds

        public RecipeTimeLeft() { }

        public void CalculateTotalTime(List<RecipeLine> table)
        {
            uint sum = 0;

            foreach (var row in table)
            {
                var value = row.GetCells[Params.RecipeTimeIndex].GetValue();
                if (uint.TryParse(value.ToString(), out uint result))
                {
                    sum += result;
                }
            }
            totalTimeLeft = sum * 1000;
        }

        public void CalculateLineTime(List<RecipeLine> table, int rowNumber)
        {
            uint sum = 0;

            var value = table[rowNumber].GetCells[Params.RecipeTimeIndex].GetValue();
            if (uint.TryParse(value.ToString(), out uint result))
            {
                sum = result * 1000;
            }

            lineTimeLeft = sum * 1000;
        }
    }
}
