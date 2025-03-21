﻿using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Actions;

namespace NtoLib.Recipes.MbeTable.RecipeLines
{
    internal class RecipeLineParser
    {
        public static RecipeLine Parse(string stringToParse)
        {
            var cellStrings = SplitLine(stringToParse, Params.ColumnCount);

            var command = ParseCommand(cellStrings[Params.ActionIndex]);

            var number = ParseIntOrThrow(cellStrings[Params.ActionTargetIndex], "Объект");
            var initialValue = ParseFloatOrThrow(cellStrings[Params.InitialValueIndex], "Нач.значение");
            var setpoint = ParseFloatOrThrow(cellStrings[Params.SetpointIndex], "Задание");
            var speed = ParseFloatOrThrow(cellStrings[Params.SpeedIndex], "Скорость");
            var timeSetpoint = ParseFloatOrThrow(cellStrings[Params.TimeSetpointIndex], "Длительность");

            var comment = cellStrings[Params.CommentIndex] ?? string.Empty;

            return RecipeLineFactory.NewLine(command, number, initialValue, setpoint, speed, timeSetpoint, comment);
        }

        /// <summary>
        /// Splits a semicolon-separated string into a list of cell values.
        /// </summary>
        private static List<string> SplitLine(string input, int columnCount)
        {
            var cells = new List<string>();
            var remaining = input;

            for (var i = 0; i < columnCount; i++)
            {
                var separatorIndex = remaining.IndexOf(';');
                if (separatorIndex < 0)
                {
                    cells.Add(remaining);
                    break;
                }

                cells.Add(remaining.Substring(0, separatorIndex));
                remaining = separatorIndex + 1 < remaining.Length ? remaining.Substring(separatorIndex + 1) : "";
            }

            return cells;
        }

        /// <summary>
        /// Parses the command name by its ID, throwing an exception if invalid.
        /// </summary>
        private static string ParseCommand(string commandCell)
        {
            return int.TryParse(commandCell, out var commandId) &&
                ActionManager.GetActionNameById(commandId) is string command
                ? command
                : throw new InvalidOperationException("Не удалось распарсить поле 'Действие'.");
        }

        /// <summary>
        /// Parses an integer value or throws an exception if invalid.
        /// </summary>
        private static int ParseIntOrThrow(string input, string fieldName)
        {
            return string.IsNullOrEmpty(input)
                ? 0
                : int.TryParse(input, out var value) ? value : throw new FormatException($"Не удалось распарсить поле '{fieldName}'.");
        }

        /// <summary>
        /// Parses a float value or throws an exception if invalid.
        /// </summary>
        private static float ParseFloatOrThrow(string input, string fieldName)
        {
            return string.IsNullOrEmpty(input)
                ? 0f
                : float.TryParse(input, out float value) ? value : throw new FormatException($"Не удалось распарсить поле '{fieldName}'.");
        }
    }
}
