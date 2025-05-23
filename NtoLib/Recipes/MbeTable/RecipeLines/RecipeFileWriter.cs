﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NtoLib.Recipes.MbeTable.Actions;

namespace NtoLib.Recipes.MbeTable.RecipeLines
{
    public interface IRecipeFileWriter
    {
        void Write(List<RecipeLine> recipeLines, string savedPath);
    }

    public class RecipeFileWriter : IRecipeFileWriter
    {
        private const string CsvSeparator = ";";

        public void Write(List<RecipeLine> recipeLines, string savedPath)
        {
            if (recipeLines == null) throw new ArgumentNullException(nameof(recipeLines));
            try
            {
                WriteToFile(recipeLines, savedPath);
            }
            catch (IOException ex)
            {
                throw new Exception($"Ошибка ввода/вывода при сохранении файла: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении: {ex.Message}");
            }
        }

        private void WriteToFile(List<RecipeLine> recipeLines, string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Create);
            using var writer = new StreamWriter(stream);

            WriteHeader(writer);
            WriteRecipeLines(writer, recipeLines);
        }

        private void WriteHeader(StreamWriter writer)
        {
            var headers = Params.ColumnNames;
            writer.WriteLine(string.Join(CsvSeparator, headers));
        }

        private void WriteRecipeLines(StreamWriter writer, List<RecipeLine> recipeLines)
        {
            foreach (var recipeLine in recipeLines)
            {
                var rowData = FormatRecipeLine(recipeLine);
                writer.WriteLine(string.Join(CsvSeparator, rowData));
            }
        }

        private static List<string> FormatRecipeLine(RecipeLine recipeLine)
        {
            var cells = recipeLine.Cells.ToList();
            var rowData = new List<string>();
            var currentCommand = cells[Params.ActionIndex].StringValue;
            var action = ActionManager.GetTargetAction(currentCommand);

            for (var i = 0; i < cells.Count; i++)
            {
                var cellValue = FormatCell(cells[i].StringValue, i, currentCommand, action);
                rowData.Add(cellValue);
            }

            return rowData;
        }

        private static string FormatCell(string value, int columnIndex, string command, ActionType action)
        {
            string formattedValue;

            if (columnIndex == Params.ActionIndex)
            {
                formattedValue = ActionManager.GetActionIdByCommand(value).ToString();
            }
            else if (columnIndex == Params.ActionTargetIndex && action != ActionType.Unspecified)
            {
                try
                {
                    formattedValue = ActionTarget.GetActionTypeByName(value, command).ToString();
                }
                catch (KeyNotFoundException)
                {
                    formattedValue = string.Empty;
                }
            }
            else
            {
                formattedValue = value;
            }

            return EscapeCsvValue(formattedValue);
        }

        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            if (value.Contains(CsvSeparator) || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }
    }
}