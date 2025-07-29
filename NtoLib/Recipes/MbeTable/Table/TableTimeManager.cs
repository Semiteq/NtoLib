using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Table
{
    // public class TableTimeManager
    // {
    //     private readonly IReadOnlyList<Step> _recipeData;
    //     private readonly UpdateBatcher _updateBatcher;
    //     private readonly ConcurrentDictionary<int, TimeSpan> _lineTotalTimeCache;
    //     private readonly ConcurrentDictionary<int, TimeSpan> _lineStartTimeCache;
    //     private TimeSpan? _recipeTotalTimeCache;
    //     private string _setpointName = TableSchema.Columns[3].UiName;
    //
    //     public TimeSpan TotalTime { get; private set; } = TimeSpan.Zero;
    //
    //     public TableTimeManager(IReadOnlyList<Step> tableData, UpdateBatcher updateBatcher)
    //     {
    //         _recipeData = tableData ?? throw new ArgumentNullException(nameof(tableData));
    //         _updateBatcher = updateBatcher ?? throw new ArgumentNullException(nameof(updateBatcher));
    //         _lineTotalTimeCache = new ConcurrentDictionary<int, TimeSpan>();
    //         _lineStartTimeCache = new ConcurrentDictionary<int, TimeSpan>();
    //     }
    //
    //     /// <summary>
    //     /// Получает общее время выполнения строки рецепта
    //     /// </summary>
    //     /// <param name="lineIndex">Индекс строки</param>
    //     /// <returns>Время выполнения</returns>
    //     public TimeSpan GetLineTotalTime(int lineIndex)
    //     {
    //         if (_recipeData == null || lineIndex < 0 || lineIndex >= _recipeData.Count)
    //             return TimeSpan.Zero;
    //
    //         // Используем кэш, если значение уже рассчитано
    //         if (_lineTotalTimeCache.TryGetValue(lineIndex, out var cachedTime))
    //             return cachedTime;
    //
    //         TimeSpan executionTime = CalculateLineTotalTime(lineIndex);
    //         _lineTotalTimeCache[lineIndex] = executionTime;
    //         
    //         return executionTime;
    //     }
    //
    //     /// <summary>
    //     /// Рассчитывает общее время выполнения строки рецепта
    //     /// </summary>
    //     private TimeSpan CalculateLineTotalTime(int lineIndex)
    //     {
    //         TimeSpan executionTime = TimeSpan.Zero;
    //         Stack<(int startIndex, int iterations)> loopStack = new();
    //
    //         for (int i = 0; i <= lineIndex; i++)
    //         {
    //             var line = _recipeData[i];
    //
    //             if (line is ForLoop forLoop)
    //             {
    //                 loopStack.Push((i, forLoop.GetCell("_setpointName").IntValue));
    //             }
    //             else if (line is EndForLoop)
    //             {
    //                 if (loopStack.Count > 0)
    //                     loopStack.Pop();
    //             }
    //             else if (line.DeployDuration == DeployDuration.TimeSetpoint)
    //             {
    //                 var duration = TimeSpan.FromSeconds(line.TryGetProperty(TableSchema.GetColumnByKey("duration").UiName).FloatValue);
    //                 executionTime += duration;
    //
    //                 if (loopStack.Count > 0)
    //                 {
    //                     var top = loopStack.Peek();
    //                     executionTime += TimeSpan.FromTicks(duration.Ticks * (top.iterations - 1));
    //                 }
    //             }
    //         }
    //
    //         return executionTime;
    //     }
    //
    //     /// <summary>
    //     /// Получает общее время выполнения всего рецепта
    //     /// </summary>
    //     /// <returns>Общее время выполнения рецепта</returns>
    //     public TimeSpan GetRecipeTotalTime()
    //     {
    //         if (_recipeData == null || _recipeData.Count == 0)
    //             return TimeSpan.Zero;
    //             
    //         // Используем кэш, если значение уже рассчитано
    //         if (_recipeTotalTimeCache.HasValue)
    //             return _recipeTotalTimeCache.Value;
    //
    //         TimeSpan totalTime = CalculateRecipeTotalTime();
    //         _recipeTotalTimeCache = totalTime;
    //         
    //         return totalTime;
    //     }
    //
    //     /// <summary>
    //     /// Рассчитывает общее время выполнения рецепта
    //     /// </summary>
    //     private TimeSpan CalculateRecipeTotalTime()
    //     {
    //         TimeSpan totalTime = TimeSpan.Zero;
    //         Stack<(int startIndex, int iterations, TimeSpan loopTime)> loopStack = new();
    //
    //         for (int i = 0; i < _recipeData.Count; i++)
    //         {
    //             var line = _recipeData[i];
    //
    //             if (line is ForLoop forLoop)
    //             {
    //                 loopStack.Push((i, forLoop.GetCell("_setpointName").IntValue, totalTime));
    //             }
    //             else if (line is EndForLoop)
    //             {
    //                 if (loopStack.Count > 0)
    //                 {
    //                     var (_, iterations, loopStartTime) = loopStack.Pop();
    //                     var singleLoopTime = totalTime - loopStartTime;
    //                     totalTime += TimeSpan.FromTicks(singleLoopTime.Ticks * (iterations - 1));
    //                 }
    //             }
    //             else if (line.DeployDuration == DeployDuration.TimeSetpoint)
    //             {
    //                 totalTime += TimeSpan.FromSeconds(line.DurationProperty);
    //             }
    //
    //             // Обновляем время в текущем цикле (если находимся внутри цикла)
    //             if (loopStack.Count > 0)
    //             {
    //                 var top = loopStack.Pop();
    //                 loopStack.Push((top.startIndex, top.iterations, totalTime));
    //             }
    //         }
    //
    //         return totalTime;
    //     }
    //
    //     /// <summary>
    //     /// Получает время начала выполнения строки рецепта
    //     /// </summary>
    //     /// <param name="lineIndex">Индекс строки</param>
    //     /// <returns>Время начала выполнения</returns>
    //     public TimeSpan GetLineStartTime(int lineIndex)
    //     {
    //         if (_recipeData == null || lineIndex < 0 || lineIndex >= _recipeData.Count)
    //             return TimeSpan.Zero;
    //
    //         // Используем кэш, если значение уже рассчитано
    //         if (_lineStartTimeCache.TryGetValue(lineIndex, out var cachedTime))
    //             return cachedTime;
    //
    //         TimeSpan startTime = CalculateLineStartTime(lineIndex);
    //         _lineStartTimeCache[lineIndex] = startTime;
    //         
    //         return startTime;
    //     }
    //
    //     /// <summary>
    //     /// Рассчитывает время начала выполнения строки рецепта
    //     /// </summary>
    //     private TimeSpan CalculateLineStartTime(int lineIndex)
    //     {
    //         TimeSpan startTime = TimeSpan.Zero;
    //         Stack<(int startIndex, int iterations)> loopStack = new();
    //
    //         for (int i = 0; i < lineIndex; i++)
    //         {
    //             var line = _recipeData[i];
    //
    //             if (line is ForLoop forLoop)
    //             {
    //                 loopStack.Push((i, forLoop.GetCell("_setpointName").IntValue));
    //             }
    //             else if (line is EndForLoop)
    //             {
    //                 if (loopStack.Count > 0)
    //                     loopStack.Pop();
    //             }
    //             else if (line.DeployDuration == DeployDuration.TimeSetpoint)
    //             {
    //                 var duration = TimeSpan.FromSeconds(line.DurationProperty);
    //                 startTime += duration;
    //
    //                 if (loopStack.Count > 0)
    //                 {
    //                     var top = loopStack.Peek();
    //                     startTime += TimeSpan.FromTicks(duration.Ticks * (top.iterations - 1));
    //                 }
    //             }
    //         }
    //
    //         return startTime;
    //     }
    //
    //     /// <summary>
    //     /// Пересчитывает время выполнения для каждой строки рецепта и обновляет UI
    //     /// </summary>
    //     public void Recalculate()
    //     {
    //         // Сбрасываем кэш при пересчете
    //         _lineTotalTimeCache.Clear();
    //         _lineStartTimeCache.Clear();
    //         _recipeTotalTimeCache = null;
    //
    //         if (_recipeData is null || _recipeData.Count == 0)
    //         {
    //             TotalTime = TimeSpan.Zero;
    //             return;
    //         }
    //
    //         var accumulatedTime = TimeSpan.Zero;
    //         var cellUpdates = new List<(int rowIndex, float time)>(_recipeData.Count);
    //         
    //         // Инициализируем время первой строки
    //         _recipeData[0].StartTimeProperty = 0f;
    //         cellUpdates.Add((0, 0f));
    //
    //         // Расчет времени для каждой строки
    //         for (var rowIndex = 0; rowIndex < _recipeData.Count; rowIndex++)
    //         {
    //             var recipeLine = _recipeData[rowIndex];
    //             var lineTime = GetLineTime(recipeLine, rowIndex);
    //             accumulatedTime += lineTime;
    //
    //             if (rowIndex < _recipeData.Count - 1)
    //             {
    //                 // Записываем время начала в объект Step и подготавливаем обновление UI
    //                 float startTimeSeconds = (float)accumulatedTime.TotalSeconds;
    //                 _recipeData[rowIndex + 1].StartTimeProperty = startTimeSeconds;
    //                 cellUpdates.Add((rowIndex + 1, startTimeSeconds));
    //             }
    //         }
    //
    //         // Пакетное обновление UI для повышения производительности
    //         foreach (var (rowIndex, time) in cellUpdates)
    //         {
    //             _updateBatcher.UpdateCell(rowIndex, Params.StepBeginTimeIndex, time);
    //         }
    //
    //         TotalTime = accumulatedTime;
    //     }
    //
    //     // Вычисляет время для одной строки рецепта
    //     private TimeSpan GetLineTime(Step step, int rowIndex)
    //     {
    //         return step switch
    //         {
    //             EndForLoop endLoop => TimeSpan.FromSeconds(CalculateCycleTime(endLoop, rowIndex)),
    //             _ when step.DeployDuration == DeployDuration.TimeSetpoint => TimeSpan.FromSeconds(step.DurationProperty),
    //             _ => TimeSpan.Zero
    //         };
    //     }
    //
    //     // Вычисляет время цикла для строки EndFor_Loop
    //     private float CalculateCycleTime(EndForLoop endLoop, int rowIndex)
    //     {
    //         var cycleStartIndex = FindCycleStart(rowIndex);
    //         return cycleStartIndex == -1
    //             ? 0f
    //             : (endLoop.StartTimeProperty - _recipeData[cycleStartIndex].StartTimeProperty) * (_recipeData[cycleStartIndex].SetpointProperty - 1);
    //     }
    //
    //     // Находит начальный индекс цикла For_Loop для данного EndFor_Loop
    //     private int FindCycleStart(int endIndex)
    //     {
    //         var tabulateLevel = 1;
    //         for (var i = endIndex - 1; i >= 0; i--)
    //         {
    //             if (_recipeData[i] is EndForLoop)
    //                 tabulateLevel++;
    //             else if (_recipeData[i] is ForLoop)
    //                 tabulateLevel--;
    //
    //             if (tabulateLevel == 0)
    //                 return i;
    //         }
    //
    //         return -1;
    //     }
    // }
}