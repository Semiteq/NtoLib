using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NtoLib.Recipes.MbeTable.RecipeLines;
using NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime;

namespace NtoLib.Recipes.MbeTable.Tests.Recipes
{
    [TestClass]
    public class LineChangeProcessorTests
    {
        // Helper method to set private fields via reflection.
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(obj, value);
        }

        // Helper method to get private field value.
        private T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)field.GetValue(obj);
        }

        [TestMethod]
        public void Process_InactiveRecipe_StopsLineTimer()
        {
            // Arrange: Create a LineChangeProcessor with null logger.
            var loggerFactory = NullLoggerFactory.Instance;
            var logger = NullLogger<LineChangeProcessor>.Instance;
            var processor = new LineChangeProcessor(logger, loggerFactory);

            // Set _lineTimer to a fake timer using reflection.
            var fakeTimer = new FakeCountTimer();
            SetPrivateField(processor, "_lineTimer", fakeTimer);
            // Set _lastIsRecipeActive to true.
            SetPrivateField(processor, "_lastIsRecipeActive", true);

            // Act: Process with inactive recipe.
            processor.Process(false, 1, 5f, null);

            // Assert: _lineTimer should be null (i.e. stopped).
            var timerField = GetPrivateField<ICountTimer?>(processor, "_lineTimer");
            Assert.IsNull(timerField, "Line timer should be stopped when recipe is inactive.");
        }

        [TestMethod]
        public void Process_ActiveRecipe_AppliesTimerCorrection()
        {
            // Arrange: Create a LineChangeProcessor.
            var loggerFactory = NullLoggerFactory.Instance;
            var logger = NullLogger<LineChangeProcessor>.Instance;
            var processor = new LineChangeProcessor(logger, loggerFactory);

            // Create a fake internal timer with elapsed time of 6 seconds.
            var fakeInternalTimer = new FakeCountTimer { ElapsedTime = TimeSpan.FromSeconds(6) };
            // Set previous expected step time to 5 seconds.
            SetPrivateField(processor, "_previousExpectedStepTime", 5f);
            // Set _lineTimer to fakeInternalTimer.
            SetPrivateField(processor, "_lineTimer", fakeInternalTimer);
            // Set _lastIsRecipeActive to true.
            SetPrivateField(processor, "_lastIsRecipeActive", true);

            // Create a fake countdown timer with an initial remaining time.
            var fakeCountdownTimer = new FakeCountTimer { RemainingTime = TimeSpan.FromSeconds(30) };

            // Act: Process active recipe with a current line change.
            // The expected diff is (6 - 5) = 1 second, which exceeds the CorrectionThreshold (0.1).
            processor.Process(true, 2, 7f, fakeCountdownTimer);

            // Assert: The fake countdown timer's remaining time should be increased by 1 second.
            var expectedRemaining = TimeSpan.FromSeconds(31);
            Assert.AreEqual(expectedRemaining, fakeCountdownTimer.RemainingTime, "Countdown timer remaining time should be corrected by the time difference.");
        }
    }
}