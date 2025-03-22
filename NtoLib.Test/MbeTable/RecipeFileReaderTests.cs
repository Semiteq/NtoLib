using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Test.MbeTable
{
    // Dummy IStatusManager implementation for testing
    public class DummyStatusManager : NtoLib.Recipes.MbeTable.IStatusManager
    {
        public List<string> Messages { get; } = new();
        public void WriteStatusMessage(string message, bool isError)
        {
            Messages.Add(message);
        }
    }

    [TestClass]
    public class RecipeFileReaderTests
    {
        // Test for valid file reading.
        [TestMethod]
        public void Read_ValidFile_ReturnsRecipeLines()
        {
            // Create temporary file with header and one valid data line.
            var tempFile = Path.GetTempFileName();
            try
            {
                // For simplicity assume the valid data line contains 7 semicolon-separated columns.
                // Первый столбец (command) – число, позволяющее успешно пройти ParseCommand.
                var lines = new[]
                {
                    "Header",
                    "1;123;0;0;0;0;Comment"
                };
                File.WriteAllLines(tempFile, lines);

                // Set file name in OpenFileDialog.
                var openFileDialog = new OpenFileDialog { FileName = tempFile };
                var statusManager = new DummyStatusManager();
                var reader = new RecipeFileReader(openFileDialog, statusManager);
                var recipeLines = reader.Read();
                Assert.IsTrue(recipeLines.Count > 0, "Recipe lines should be returned for valid file.");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        // Test for empty file.
        [TestMethod]
        public void Read_EmptyFile_ReturnsEmptyListAndLogsError()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                // Write empty content.
                File.WriteAllText(tempFile, string.Empty);

                var openFileDialog = new OpenFileDialog { FileName = tempFile };
                var statusManager = new DummyStatusManager();
                var reader = new RecipeFileReader(openFileDialog, statusManager);
                var recipeLines = reader.Read();
                Assert.AreEqual(0, recipeLines.Count, "Empty file should return empty recipe list.");
                Assert.IsTrue(statusManager.Messages.Any(m => m.Contains("Файл пуст.")), "Status message should indicate empty file.");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}