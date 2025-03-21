using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime;

namespace NtoLib.Test.MbeTable
{
    [TestClass]
    public class CountTimerTests
    {
        [TestMethod]
        public async Task TimerFiresAfterDuration()
        {
            // Arrange: Create a CountTimer with a short duration.
            using var timer = new CountTimer(TimeSpan.FromMilliseconds(2000));
            var finished = false;
            timer.OnTimerFinished += () => finished = true;

            // Act: Start the timer and wait longer than the duration.
            timer.Start();
            await Task.Delay(3000);

            // Assert: Timer should have finished.
            Assert.IsFalse(timer.IsRunning, "Timer should not be running after duration elapsed.");
            Assert.IsTrue(finished, "OnTimerFinished event should have been fired.");
        }

        [TestMethod]
        public async Task PauseAndResumeTest()
        {
            // Arrange: Create a CountTimer.
            using var timer = new CountTimer(TimeSpan.FromMilliseconds(500));
            timer.Start();
            await Task.Delay(150);

            // Act: Pause the timer.
            timer.Pause();
            var elapsedAfterPause = timer.GetElapsedTime();
            await Task.Delay(200);
            var elapsedAfterDelay = timer.GetElapsedTime();

            // Assert: Elapsed time should not increase when paused.
            Assert.AreEqual(elapsedAfterPause, elapsedAfterDelay, "Elapsed time should not change while timer is paused.");

            // Act: Resume the timer and wait.
            timer.Resume();
            await Task.Delay(400);

            // Assert: Timer should finish after resuming.
            Assert.IsFalse(timer.IsRunning, "Timer should have finished after resume.");
        }

        [TestMethod]
        public void StopTest()
        {
            // Arrange: Create a CountTimer.
            using var timer = new CountTimer(TimeSpan.FromSeconds(1));
            timer.Start();

            // Act: Stop the timer.
            timer.Stop();

            // Assert: Timer should be stopped.
            Assert.IsTrue(timer.IsStopped, "Timer should be marked as stopped.");
            Assert.IsFalse(timer.IsRunning, "Timer should not be running after stop.");
        }
    }
}