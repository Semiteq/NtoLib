using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NtoLib.Recipes.MbeTable.Recipe;
using NtoLib.Recipes.MbeTable.Recipe.Actions.TableLines.ServiceActions;
using NtoLib.Recipes.MbeTable.Recipe.RecipeLineFactory;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.RecipeLines
{
    public interface IRecipeFileReader
    {
        List<Step> Read(string FilePath);
    }

    public class RecipeFileReader : IRecipeFileReader
    {
        private const char CsvSeparator = ';';

        public List<Step> Read(string FilePath)
        {
            if (!File.Exists(FilePath))
            {
                throw new Exception($"Файл не найден: {FilePath}");
            }

            try
            {
                var parsedRecipes = ParseFile(FilePath);

                if (!CheckRecipeCycles(parsedRecipes))
                    throw new Exception($"Ошибка синтаксиса For/EndFor");

                return parsedRecipes;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при загрузке файла: {ex.Message}");
            }
        }

        private List<Step> ParseFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath).Skip(1); // Skip header
            var result = new List<Step>();

            foreach (var (csvLine, index) in lines.Select((value, i) => (value, i)))
            {
                if (string.IsNullOrWhiteSpace(csvLine)) continue;

                try
                {
                    result.Add(ParseLine(csvLine));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Ошибка в строке {index + 1}/{lines.Count()}: {ex.Message}");
                }
            }

            return result;
        }

        private Step ParseLine(string csvLine)
        {
            var textLine = csvLine.Split(CsvSeparator)
                .Select(x => x.Trim())
                .ToArray();

            if (textLine.Length < TableSchema.Columns.Count)
            {
                throw new ArgumentException(
                    $"Недостаточно колонок. Ожидалось: {TableSchema.Columns.Count}, получено: {textLine.Length}");
            }

            return RecipeLineFactory.NewLine(
                ParseCommand(textLine, 0),

                ParseInt(textLine, 1),

                ParseFloat(textLine, 2),
                ParseFloat(textLine, 3),
                ParseFloat(textLine, 4),
                ParseFloat(textLine, 5),

                ParseString(textLine, 7)
            );
        }

        private string ParseCommand(string[] textLine, int index)
        {
            if (!int.TryParse(textLine[index], out var commandId))
            {
                throw new FormatException($"Некорректный формат действия: {textLine[index]}");
            }

            return ActionManager.GetActionNameById(commandId)
                   ?? throw new ArgumentException($"Неизвестный код действия: {commandId}");
        }

        private static int ParseInt(string[] textLine, int index) =>
            string.IsNullOrEmpty(textLine[index]) ? 0 :
            int.TryParse(textLine[index], out var result) ? result :
            throw new FormatException($"Некорректное значение поля '{Params.ColumnNames[index]}': {textLine}");

        private static float ParseFloat(string[] textLine, int index) =>
            string.IsNullOrEmpty(textLine[index]) ? 0f :
            float.TryParse(textLine[index], out var result) ? result :
            throw new FormatException($"Некорректное значение поля '{Params.ColumnNames[index]}': {textLine}");

        private static string ParseString(string[] textLine, int index) =>
            textLine.Length > index ? textLine[index] : string.Empty;

        private bool CheckRecipeCycles(List<Step> recipe)
        {
            var cycleDepth = 0;

            foreach (var line in recipe)
            {
                if (cycleDepth > Params.MaxLoopCount)
                    return false;

                cycleDepth += line switch
                {
                    ForLoop => 1,
                    EndForLoop when cycleDepth > 0 => -1,
                    EndForLoop => throw new InvalidOperationException("Найден EndFor без соответствующего For"),
                    _ => 0
                };

                line.TabulateLevel = cycleDepth;
            }

            return cycleDepth == 0;
        }
    }
}