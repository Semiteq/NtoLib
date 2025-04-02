using Moq;
using NtoLib.Recipes.MbeTable;
using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Test.MbeTable
{
    [TestClass]
    public class RecipeFileReaderTests
    {
        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            // Initialize dictionaries to ensure non-zero counts
            ActionTarget.SetNames(ActionType.Shutter, new Dictionary<int, string>
            {
                { 1, "Shutter1" },
                { 2, "Shutter2" }
            });
            ActionTarget.SetNames(ActionType.Heater, new Dictionary<int, string>
            {
                { 1, "Heater1" },
                { 2, "Heater2" }
            });
            ActionTarget.SetNames(ActionType.NitrogenSource, new Dictionary<int, string>
            {
                { 1, "Nitrogen1" },
                { 2, "Nitrogen2" }
            });
        }

        [TestMethod]
        public void TryLoadValidCsvRecipeFilesInDir()
        {
            var baseDirectory = Path.GetFullPath(@"..\..\..\MbeTable\Recipes\valid");
            Assert.IsTrue(Directory.Exists(baseDirectory), $"Directory not found: {baseDirectory}");

            // Retrieve all CSV files in the directory and its subdirectories
            var csvFiles = Directory.EnumerateFiles(baseDirectory, "*.csv", SearchOption.AllDirectories)
                                    .OrderBy(path => path)
                                    .ToList();

            TestContext.WriteLine($"Total files found: {csvFiles.Count}");

            var passedFiles = new List<string>();
            var failedFiles = new List<string>();

            var statusManagerMock = new Mock<IStatusManager>();
            statusManagerMock.Setup(sm => sm.WriteStatusMessage(It.IsAny<string>(), It.IsAny<bool>()));

            foreach (var filePath in csvFiles)
            {
                // Assigning the file path to the OpenFileDialog.FileName property
                var fileDialogMock = new Mock<IFileDialog>();
                fileDialogMock.Setup(fd => fd.FileName).Returns(filePath);
                var reader = new RecipeFileReader();

                try
                {
                    var result = reader.Read(filePath);
                    Assert.IsNotNull(result, $"Result should not be null for file {filePath}");

                    passedFiles.Add(filePath);
                    TestContext.WriteLine($"Passed: {filePath}");
                }
                catch (Exception ex)
                {
                    failedFiles.Add(filePath);
                    TestContext.WriteLine($"Failed: {filePath} - {ex.Message}");
                    Assert.Fail($"Exception for file '{filePath}': {ex.Message}");
                }
            }

            TestContext.WriteLine($"Files processed: {csvFiles.Count}");
            TestContext.WriteLine($"Passed files: {passedFiles.Count}");
            TestContext.WriteLine($"Failed files: {failedFiles.Count}");
        }

        [TestMethod]
        public void TryLoadInvalidCsvRecipesInDir()
        {
            var baseDirectory = Path.GetFullPath(@"..\..\..\MbeTable\Recipes\invalid");
            Assert.IsTrue(Directory.Exists(baseDirectory), $"Directory not found: {baseDirectory}");

            // Retrieve all CSV files in the directory and its subdirectories
            var csvFiles = Directory.EnumerateFiles(baseDirectory, "*.csv", SearchOption.AllDirectories)
                                    .OrderBy(path => path)
                                    .ToList();

            TestContext.WriteLine($"Total files found: {csvFiles.Count}");

            var passedFiles = new List<string>();
            var failedFiles = new List<string>();

            var statusManagerMock = new Mock<IStatusManager>();
            statusManagerMock.Setup(sm => sm.WriteStatusMessage(It.IsAny<string>(), It.IsAny<bool>()));

            foreach (var filePath in csvFiles)
            {
                var fileDialogMock = new Mock<IFileDialog>();
                fileDialogMock.Setup(fd => fd.FileName).Returns(filePath);
                var reader = new RecipeFileReader();

                try
                {
                    var result = reader.Read(filePath);
                    // If no exception is thrown, the test should fail
                    Assert.Fail($"Invalid test failed for file {filePath}");
                    passedFiles.Add(filePath);
                    TestContext.WriteLine($"Passed: {filePath}");
                }
                catch (Exception ex)
                {
                    failedFiles.Add(filePath);
                    TestContext.WriteLine($"Failed: {filePath} - {ex.Message}");
                }
            }

            TestContext.WriteLine($"Files processed: {csvFiles.Count}");
            TestContext.WriteLine($"Passed files: {passedFiles.Count}");
            TestContext.WriteLine($"Failed files: {failedFiles.Count}");
        }
    }
}
