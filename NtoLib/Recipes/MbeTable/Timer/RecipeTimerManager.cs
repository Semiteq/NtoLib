using System;
using NtoLib.Recipes.MbeTable.Table;

namespace NtoLib.Recipes.MbeTable.Managers.Contracts
{
    public class RecipeTimerManager
    { 
        private readonly TableTimeManager _tableTimeManager;
        private RecipeTimer _recipeTimer;
        
        public RecipeTimerManager(TableTimeManager tableTimeManager)
        {
            _tableTimeManager = tableTimeManager ?? throw new ArgumentNullException(nameof(tableTimeManager));
        }
        
        public void HandleLineChange(int actualLineNumber)
        {
            if (_tableTimeManager == null)
                throw new InvalidOperationException("TableTimeManager is not initialized.");
            
            var actualLineTimeDuration = _tableTimeManager.GetLineTotalTime(actualLineNumber);
            _recipeTimer = new RecipeTimer();
            _recipeTimer.Start(actualLineTimeDuration);
        }
        
        public (int leftStepTime, int leftTotalTime) GetLeftTimes(int actualLineNumber, float stepCurrentTime)
        {
            if (_tableTimeManager == null)
                throw new InvalidOperationException("TableTimeManager is not initialized.");

            var actualLineTimeDuration = _tableTimeManager.GetLineTotalTime(actualLineNumber).Seconds;
            var leftStepTime = actualLineTimeDuration - _recipeTimer.Remaining.Seconds;

            var leftTotalTime = _tableTimeManager.GetLineStartTime(actualLineNumber).Seconds - _recipeTimer.Remaining.Seconds;
            
            return (leftStepTime, leftTotalTime);
        }
    }
}
