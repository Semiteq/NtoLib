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

            string command = cellStrings[Params.CommandIndex] ?? throw new InvalidOperationException("Не удалось распарсить поле 'Действие'.");

            int number = 0;
            float setpoint = 0f, timeSetpoint = 0f;

            //Далее проверки на недействительные значения в файле рецепта.
            //Если сторока НЕ пустая и НЕ парсится - выбрасываем исключение.

            if (!string.IsNullOrEmpty(cellStrings[Params.NumberIndex]) && 
                !Int32.TryParse(cellStrings[Params.NumberIndex], out number))
            {
                throw new FormatException("Не удалось распарсить поле 'Номер'.");
            }

            if (!string.IsNullOrEmpty(cellStrings[Params.SetpointIndex]) &&
                !float.TryParse(cellStrings[Params.SetpointIndex], out setpoint))
            {
                throw new FormatException("Не удалось распарсить поле 'Задание'.");
            }

            if (!string.IsNullOrEmpty(cellStrings[Params.TimeSetpointIndex]) &&
                !float.TryParse(cellStrings[Params.TimeSetpointIndex], out timeSetpoint))
            {
                throw new FormatException("Не удалось распарсить поле 'Cкорость/Время'.");
            }
            
            string comment = cellStrings[Params.CommentIndex] ?? string.Empty;

            return factory.NewLine(command, number, setpoint, timeSetpoint, comment);
        }
    }
}