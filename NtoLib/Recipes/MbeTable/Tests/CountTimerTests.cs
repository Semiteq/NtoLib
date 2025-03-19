using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime;

namespace NtoLib.Recipes.MbeTable.Tests
{
    [TestClass]
    public class CountTimerTests
    {
        [TestMethod]
        public async Task TimerFiresAfterDuration()
        {
            // Arrange: Create a CountTimer with a short duration.
            var logger = NullLogger<CountTimer>.Instance;
            using var timer = new CountTimer(TimeSpan.FromMilliseconds(200), logger);
            var finished = false;
            timer.OnTimerFinished += () => finished = true;
            
            // Act: Start the timer and wait longer than the duration.
            timer.Start();
            await Task.Delay(300);
            
            // Assert: Timer should have finished.
            Assert.IsFalse(timer.IsRunning, "Timer should not be running after duration elapsed.");
            Assert.IsTrue(finished, "OnTimerFinished event should have been fired.");
        }

        [TestMethod]
        public async Task PauseAndResumeTest()
        {
            // Arrange: Create a CountTimer.
            var logger = NullLogger<CountTimer>.Instance;
            using var timer = new CountTimer(TimeSpan.FromMilliseconds(500), logger);
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
            var logger = NullLogger<CountTimer>.Instance;
            using var timer = new CountTimer(TimeSpan.FromSeconds(1), logger);
            timer.Start();
            
            // Act: Stop the timer.
            timer.Stop();
            
            // Assert: Timer should be stopped.
            Assert.IsTrue(timer.IsStopped, "Timer should be marked as stopped.");
            Assert.IsFalse(timer.IsRunning, "Timer should not be running after stop.");
        }
    }
}