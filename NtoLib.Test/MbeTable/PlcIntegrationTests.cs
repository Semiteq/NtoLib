using NtoLib.Recipes.MbeTable;
using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.PLC;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Test.MbeTable
{
    public class FileStatusManager : IStatusManager, IDisposable
    {
        private readonly StreamWriter _writer;

        public FileStatusManager(string logPath)
        {
            _writer = new StreamWriter(logPath, true);
            _writer.WriteLine($"\nTest started at {DateTime.Now}");
        }

        public void WriteStatusMessage(string message, bool isError)
        {
            var formattedMessage = $"[{DateTime.Now:HH:mm:ss.fff}] [{(isError ? "ERROR" : "INFO")}] {message}";
            _writer.WriteLine(formattedMessage);
            _writer.Flush();
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }

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
                BoolAreaSize = 50,
                BoolBaseAddr = 29100,

                ControlBaseAddr = 8000,

                FloatAreaSize = 19600,
                FloatBaseAddr = 8100,

                IntAreaSize = 1400,
                IntBaseAddr = 27700,

                Ip1 = 192,
                Ip2 = 168,
                Ip3 = 0,
                Ip4 = 141,
                Port = 502,

                ModbusTransactionId = 0,

                SlmpArea = MbeTableFB.SlmpArea.D,
                Timeout = 1000
            };
        }

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
        public void WriteAndLoadAllRecipes_ShouldMatchOriginal()
        {
            const int timeOutWriteRead = 200;

            var logPath = Path.Combine(Path.GetTempPath(), $"plc_test_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            using var statusManager = new FileStatusManager(logPath);

            // Arrange
            var settings = CreateTestSettings();
            var plcCommunication = new PlcCommunication();
            var comparator = new RecipeComparator();

            var baseDirectory = Path.GetFullPath(@"..\..\..\MbeTable\Recipes\valid");
            Assert.IsTrue(Directory.Exists(baseDirectory), $"Directory not found: {baseDirectory}");

            var csvFiles = Directory.EnumerateFiles(baseDirectory, "*.csv", SearchOption.AllDirectories)
                .OrderBy(path => path)
                .ToList();

            statusManager.WriteStatusMessage($"Total CSV files found: {csvFiles.Count}", false);

            var passedFiles = new List<string>();
            var failedFiles = new Dictionary<string, string>();

            foreach (var filePath in csvFiles)
            {
                try
                {
                    Thread.Sleep(timeOutWriteRead);

                    statusManager.WriteStatusMessage($"Processing file: {filePath}", false);

                    var reader = new RecipeFileReader();

                    statusManager.WriteStatusMessage($"Reading recipe from file: {filePath}", false);
                    var originalRecipe = reader.Read(filePath);

                    statusManager.WriteStatusMessage($"Writing recipe to PLC: {filePath}", false);
                    var writeResult = plcCommunication.WriteRecipeToPlc(originalRecipe, settings);
                    Assert.IsTrue(writeResult, $"Writing to PLC failed for file: {filePath}");

                    Thread.Sleep(timeOutWriteRead);

                    statusManager.WriteStatusMessage($"Reading recipe from PLC for file: {filePath}", false);
                    var loadedRecipe = plcCommunication.LoadRecipeFromPlc(settings);
                    Assert.IsNotNull(loadedRecipe, $"Loaded recipe is null for file: {filePath}");

                    statusManager.WriteStatusMessage($"Comparing original and loaded recipes: {filePath}", false);
                    Assert.IsTrue(comparator.Compare(originalRecipe, loadedRecipe),
                        $"Loaded recipe does not match original for file: {filePath}");

                    passedFiles.Add(filePath);
                    statusManager.WriteStatusMessage($"File processed successfully: {filePath}", false);
                }
                catch (Exception ex)
                {
                    failedFiles[filePath] = ex.Message;
                    statusManager.WriteStatusMessage($"Exception for file: {filePath}", true);
                    statusManager.WriteStatusMessage($"Details: {ex}", true);
                }
            }

            // Итоговая статистика
            statusManager.WriteStatusMessage("=====================================", false);
            statusManager.WriteStatusMessage("TEST SUMMARY:", false);
            statusManager.WriteStatusMessage($"Total processed files: {csvFiles.Count}", false);
            statusManager.WriteStatusMessage($"Passed: {passedFiles.Count}", false);
            statusManager.WriteStatusMessage($"Failed: {failedFiles.Count}", false);

            if (failedFiles.Count > 0)
            {
                statusManager.WriteStatusMessage("Failed files:", false);
                foreach (var kvp in failedFiles)
                {
                    statusManager.WriteStatusMessage($" - {kvp.Key}: {kvp.Value}", false);
                }

                Assert.Fail($"Some files failed. See log file: {logPath}");
            }
        }
    }
}