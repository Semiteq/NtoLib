using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.Actions.TableLines;

namespace NtoLib.Recipes.MbeTable.RecipeLines
{
    public interface IRecipeFileReader
    {
        List<RecipeLine> Read();
    }

    public class RecipeFileReader : IRecipeFileReader
    {
        private const char CsvSeparator = ';';
        private readonly OpenFileDialog _openFileDialog;
        private readonly IStatusManager _statusManager;

        public RecipeFileReader(OpenFileDialog openFileDialog, IStatusManager statusManager)
        {
            _openFileDialog = openFileDialog ?? throw new ArgumentNullException(nameof(openFileDialog));
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
        }

        public List<RecipeLine> Read()
        {
            if (!File.Exists(_openFileDialog.FileName))
            {
                throw new Exception($"Файл не найден: {_openFileDialog.FileName}");
            }

            try
            {
                var parsedRecipes = ParseFile(_openFileDialog.FileName);

                if (!CheckRecipeCycles(parsedRecipes))
                    throw new Exception($"Ошибка синтаксиса For/EndFor");

                return parsedRecipes;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при загрузке файла: {ex.Message}");
            }
        }

        private List<RecipeLine> ParseFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath).Skip(1); // Skip header
            var result = new List<RecipeLine>();

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

            _statusManager.WriteStatusMessage($"Данные загружены из файла {filePath}", false);
            return result;
        }

        private RecipeLine ParseLine(string csvLine)
        {
            var textLine = csvLine.Split(CsvSeparator)
                .Select(x => x.Trim())
                .ToArray();

            if (textLine.Length < Params.ColumnCount)
            {
                throw new ArgumentException(
                    $"Недостаточно колонок. Ожидалось: {Params.ColumnCount}, получено: {textLine.Length}");
            }

            return RecipeLineFactory.NewLine(
                ParseCommand(textLine, Params.ActionIndex),

                ParseInt(textLine, Params.ActionTargetIndex),

                ParseFloat(textLine, Params.InitialValueIndex),
                ParseFloat(textLine, Params.SetpointIndex),
                ParseFloat(textLine, Params.SpeedIndex),
                ParseFloat(textLine, Params.TimeSetpointIndex),

                ParseString(textLine, Params.CommentIndex)
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

        private bool CheckRecipeCycles(List<RecipeLine> recipe)
        {
            var cycleDepth = 0;

            foreach (var line in recipe)
            {
                if (cycleDepth > Params.MaxLoopCount)
                    return false;

                cycleDepth += line switch
                {
                    For_Loop => 1,
                    EndFor_Loop when cycleDepth > 0 => -1,
                    EndFor_Loop => throw new InvalidOperationException("Найден EndFor без соответствующего For"),
                    _ => 0
                };

                line.TabulateLevel = cycleDepth;
            }

            return cycleDepth == 0;
        }
    }
}