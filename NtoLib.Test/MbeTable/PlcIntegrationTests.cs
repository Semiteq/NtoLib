using System.Windows.Forms;
using NtoLib.Recipes.MbeTable;
using NtoLib.Recipes.MbeTable.PLC;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace PlcIntegrationTests
{
    // Minimal implementation of IStatusManager that outputs messages to the console.
    public class ConsoleStatusManager : IStatusManager
    {
        public void WriteStatusMessage(string message, bool isError)
        {
            Console.WriteLine($"{(isError ? "ERROR" : "INFO")}: {message}");
        }
    }

    [TestClass]
    [TestCategory("PLC")]
    public class PlcIntegrationTests
    {
        private CommunicationSettings CreateTestSettings()
        {
            return new CommunicationSettings
            {
                Ip1 = 192,
                Ip2 = 168,
                Ip3 = 0,
                Ip4 = 141,
                Port = 502,
                ControlBaseAddr = 0,
                FloatBaseAddr = 100,
                IntBaseAddr = 200,
                BoolBaseAddr = 300,
                FloatAreaSize = 100,
                IntAreaSize = 50,
                BoolAreaSize = 10
            };
        }

        [TestMethod]
        public void WriteAndLoadAllRecipes_ShouldMatchOriginal()
        {
            // Arrange
            var settings = CreateTestSettings();
            var statusManager = new ConsoleStatusManager();
            var plcCommunication = new PlcCommunication(statusManager);
            var comparator = new RecipeComparator();

            // Base folder containing the recipe files.
            // Expected folder structure: comment, custom, default, for, sequence, zero.
            // Adjust the baseFolder path if necessary.
            var baseFolder = "Recipes";
            if (!Directory.Exists(baseFolder))
            {
                Assert.Inconclusive($"Base folder not found: {baseFolder}");
            }

            // Get all .csv files recursively.
            var recipeFiles = Directory.GetFiles(baseFolder, "*.csv", SearchOption.AllDirectories);
            var errorMessages = new List<string>();

            foreach (var filePath in recipeFiles)
            {
                try
                {
                    // Create OpenFileDialog with preset file path.
                    var openFileDialog = new OpenFileDialog { FileName = filePath };

                    // Read recipe from file.
                    var fileReader = new RecipeFileReader(openFileDialog, statusManager);
                    var originalRecipe = fileReader.Read();

                    if (originalRecipe.Count == 0)
                    {
                        errorMessages.Add($"File '{filePath}' returned an empty recipe.");
                        continue;
                    }

                    // Write recipe to PLC.
                    var writeResult = plcCommunication.WriteRecipeToPlc(originalRecipe, settings);
                    if (!writeResult)
                    {
                        errorMessages.Add($"Writing recipe to PLC failed for file '{filePath}'.");
                        continue;
                    }

                    // Wait for PLC to process data.
                    Thread.Sleep(500);

                    // Read recipe from PLC.
                    var loadedRecipe = plcCommunication.LoadRecipeFromPlc(settings);

                    // Compare the original and loaded recipes.
                    if (!comparator.Compare(originalRecipe, loadedRecipe))
                    {
                        errorMessages.Add($"The loaded recipe does not match the original for file '{filePath}'.");
                    }
                }
                catch (Exception ex)
                {
                    errorMessages.Add($"Exception for file '{filePath}': {ex.Message}");
                }
            }

            // Assert: Fail if any errors were collected.
            if (errorMessages.Count > 0)
            {
                Assert.Fail("Some recipe files failed:\n" + string.Join("\n", errorMessages));
            }
        }
    }
}
