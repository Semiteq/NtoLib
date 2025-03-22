#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Actions;

namespace NtoLib.Recipes.MbeTable.RecipeLines
{
    public interface IRecipeFileReader
    {
        List<RecipeLine> Read();
    }

    public class RecipeFileReader : IRecipeFileReader
    {
        private readonly OpenFileDialog _openFileDialog;
        private readonly IStatusManager _statusManager;

        public RecipeFileReader(OpenFileDialog openFileDialog, IStatusManager statusManager)
        {
            _openFileDialog = openFileDialog;
            _statusManager = statusManager;
        }

        public List<RecipeLine> Read()
        {
            var recipeLineList = new List<RecipeLine>();

            try
            {
                // Use FileMode.Open to avoid creating a new file
                using var stream = new FileStream(_openFileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new StreamReader(stream);

                // Read header line and discard it
                if (reader.ReadLine() is null)
                {
                    _statusManager.WriteStatusMessage("Файл пуст.", true);
                    Debug.WriteLine("File is empty.");
                    return recipeLineList;
                }

                var lineNumber = 2; // Header is line 1
                string? line;
                while ((line = reader.ReadLine()) is not null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        lineNumber++;
                        continue;
                    }

                    var recipeLine = TryParseLine(line, lineNumber);
                    if (recipeLine is null)
                    {
                        // Abort processing on parsing error
                        return new List<RecipeLine>();
                    }

                    recipeLineList.Add(recipeLine);
                    lineNumber++;
                }
            }
            catch (Exception ex)
            {
                _statusManager.WriteStatusMessage($"Ошибка при загрузке файла: {ex.Message}", true);
                Debug.WriteLine($"File load error: {ex.Message}");
                return new List<RecipeLine>();
            }

            _statusManager.WriteStatusMessage($"Данные загружены из файла {_openFileDialog.FileName}", false);
            return recipeLineList;
        }

        private RecipeLine? TryParseLine(string line, int lineNumber)
        {
            var recipeLine = Parse(line);
            if (recipeLine is null)
            {
                _statusManager.WriteStatusMessage($"Ошибка при разборе строки {lineNumber}.", true);
                Debug.WriteLine($"Failed to parse line {lineNumber}.");
            }
            return recipeLine;
        }

        // Changed from static to instance method to use _statusManager for logging errors.
        private RecipeLine? Parse(string line)
        {
            var cellStrings = SplitLine(line, Params.ColumnCount);

            if (cellStrings.Count < Params.ColumnCount)
            {
                _statusManager.WriteStatusMessage("Недостаточно столбцов в строке.", true);
                Debug.WriteLine("Not enough columns in the line.");
                return null;
            }

            var command = ParseCommand(cellStrings[Params.ActionIndex]);
            if (command is null)
            {
                return null;
            }

            if (!TryParseInt(cellStrings[Params.ActionTargetIndex], "Объект", out var number))
            {
                return null;
            }
            if (!TryParseFloat(cellStrings[Params.InitialValueIndex], "Нач.значение", out var initialValue))
            {
                return null;
            }
            if (!TryParseFloat(cellStrings[Params.SetpointIndex], "Задание", out var setpoint))
            {
                return null;
            }
            if (!TryParseFloat(cellStrings[Params.SpeedIndex], "Скорость", out var speed))
            {
                return null;
            }
            if (!TryParseFloat(cellStrings[Params.TimeSetpointIndex], "Длительность", out var timeSetpoint))
            {
                return null;
            }

            var comment = cellStrings.Count > Params.CommentIndex ? cellStrings[Params.CommentIndex] ?? string.Empty : string.Empty;

            return RecipeLineFactory.NewLine(command, number, initialValue, setpoint, speed, timeSetpoint, comment);
        }

        /// <summary>
        /// Splits a semicolon-separated string into exactly columnCount cells.
        /// If there are fewer columns, empty strings are appended.
        /// </summary>
        private static List<string> SplitLine(string input, int columnCount)
        {
            // Split line without omitting empty entries
            var parts = input.Split(new[] { ';' }, StringSplitOptions.None);
            var cells = new List<string>(parts);
            // Append empty strings if necessary
            while (cells.Count < columnCount)
            {
                cells.Add(string.Empty);
            }
            return cells;
        }

        /// <summary>
        /// Parses the command from the given cell value.
        /// </summary>
        private string? ParseCommand(string commandCell)
        {
            if (int.TryParse(commandCell, out var commandId))
            {
                var command = ActionManager.GetActionNameById(commandId);
                if (command is not null)
                {
                    return command;
                }
            }
            _statusManager.WriteStatusMessage("Не удалось распарсить поле 'Действие'.", true);
            Debug.WriteLine($"Failed to parse 'Действие' field. Input: {commandCell}");
            return null;
        }

        /// <summary>
        /// Tries to parse an integer value; logs error if parsing fails.
        /// </summary>
        private bool TryParseInt(string input, string fieldName, out int value)
        {
            if (string.IsNullOrEmpty(input))
            {
                value = 0;
                return true;
            }
            if (int.TryParse(input, out value))
            {
                return true;
            }
            _statusManager.WriteStatusMessage($"Не удалось распарсить поле '{fieldName}'.", true);
            Debug.WriteLine($"Failed to parse integer for field '{fieldName}'. Input: {input}");
            return false;
        }

        /// <summary>
        /// Tries to parse a float value; logs error if parsing fails.
        /// </summary>
        private bool TryParseFloat(string input, string fieldName, out float value)
        {
            if (string.IsNullOrEmpty(input))
            {
                value = 0f;
                return true;
            }
            if (float.TryParse(input, out value))
            {
                return true;
            }
            _statusManager.WriteStatusMessage($"Не удалось распарсить поле '{fieldName}'.", true);
            Debug.WriteLine($"Failed to parse float for field '{fieldName}'. Input: {input}");
            return false;
        }
    }
}
