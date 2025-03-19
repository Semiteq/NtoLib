using System;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime;

namespace NtoLib.Recipes.MbeTable.Tests
{
    [TestClass]
    public class RecipeTimeManagerTests
    {
        //[TestMethod]
        //public void RecalculateTest()
        //{
        //    // Arrange: Create a list of recipe lines with two TimeSetpoint actions.
        //    var recipeLines = new List<RecipeLine>
        //    {
        //        new TestRecipeLine("Line1") { Duration = 10 },
        //        new TestRecipeLine("Line2") { Duration = 20 }
        //    };
        //    var manager = new RecipeTimeManager();

        //    // Act: Set data and recalculate the recipe time.
        //    manager.SetData(recipeLines, null);
        //    manager.Recalculate();

        //    // Assert: Total time should equal the sum of durations.
        //    var expectedTotal = TimeSpan.FromSeconds(10 + 20);
        //    Assert.AreEqual(expectedTotal, manager.TotalTime, "Total recipe time should be the sum of durations.");

        //    // Also, check that the second recipe line's Time is updated.
        //    Assert.AreEqual(10, recipeLines[1].Time,
        //        "The second recipe line should have Time equal to the first line's duration.");
        //}

        //[TestMethod]
        //public void GetRowTimeTest()
        //{
        //    // Arrange: Create recipe lines using the concrete TestRecipeLine.
        //    var recipeLines = new List<RecipeLine>
        //    {
        //        new TestRecipeLine("Line1") { Duration = 10 },
        //        new TestRecipeLine("Line2") { Duration = 20 }
        //    };
        //    var manager = new RecipeTimeManager();
        //    manager.SetData(recipeLines, null);
        //    manager.Recalculate();

        //    // Act: Retrieve row time for the first row.
        //    var rowTime = manager.GetRowTime(1, 0, 0, 0);

        //    // Assert: The first flattened row should have execution time equal to the Duration (10 seconds).
        //    Assert.AreEqual(TimeSpan.FromSeconds(10), rowTime,
        //        "Execution time for the first row should equal its duration.");
        //}

        [TestMethod]
        public void ManageRecipeTimerTest_Inactive()
        {
            // Arrange: Create a RecipeTimeManager and a fake timer.
            var loggerFactory = NullLoggerFactory.Instance;
            var manager = new RecipeTimeManager();
            var fakeTimer = new FakeCountTimer { RemainingTime = TimeSpan.FromSeconds(30) };

            // Act: Manage timer when recipe is inactive.
            var result = manager.ManageRecipeTimer(false, fakeTimer, TimeSpan.FromSeconds(30), loggerFactory);

            // Assert: When inactive, timer should be stopped and null returned.
            Assert.IsNull(result, "Inactive recipe should result in null timer.");
        }

        [TestMethod]
        public void ManageRecipeTimerTest_Active_StartsTimer()
        {
            // Arrange: Create a RecipeTimeManager.
            var loggerFactory = NullLoggerFactory.Instance;
            var manager = new RecipeTimeManager();

            // Act: Manage timer when recipe is active and not running.
            var result = manager.ManageRecipeTimer(true, null, TimeSpan.FromSeconds(30), loggerFactory);

            // Assert: A new timer should be created and be running.
            Assert.IsNotNull(result, "Active recipe should result in a non-null timer.");
            Assert.IsTrue(result.IsRunning, "Newly created timer should be running.");
            result.Stop();
        }
    }
}