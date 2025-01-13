using System;
using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable
{
    internal class RecipeLineParser
    {
        RecipeLineFactory factory;

        public RecipeLineParser()
        { 
            factory = new RecipeLineFactory();
        }

        public RecipeLine Parse(string stringToParse)
        {
            string parsedString = stringToParse;

            List<string> cellStrings = new List<string>();

            for (int i = 0; i < Params.ColumnCount; i++)
            {
                int length = parsedString.IndexOf(';');
                string subString;
                if (length < 0)
                {
                    subString = parsedString;
                    parsedString = "";
                    cellStrings.Add(subString);
                    break;
                }
                else
                {
                    subString = parsedString.Substring(0, length);
                    parsedString = length + 1 <= parsedString.Length ? parsedString.Substring(length + 1) : "";
                }
                cellStrings.Add(subString);
            }

            string command = cellStrings[0];
            Int32.TryParse(cellStrings[1], out int number);
            float.TryParse(cellStrings[2], out float setpoint);
            float.TryParse(cellStrings[3], out float timeSetpoint);
            string cycleTime = cellStrings[4];
            string comment = cellStrings[5];

            return factory.NewLine(command,number, setpoint, timeSetpoint, comment);
        }
    }
}