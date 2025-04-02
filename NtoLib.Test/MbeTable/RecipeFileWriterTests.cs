using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Test.MbeTable
{
    [TestClass]
    public class RecipeFileWriterTests
    {
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
        public void CheckRecipeWritten()
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"test_recipe_{Guid.NewGuid()}.csv");

            var writer = new RecipeFileWriter();
            var tableData = new List<Recipes.MbeTable.RecipeLines.RecipeLine>
            {
                Recipes.MbeTable.RecipeLines.RecipeLineFactory.NewLine("CLOSE", 1, 0, 0, 0, 0, "")
            };

            try
            {
                writer.Write(tableData, tempFilePath);

                Assert.IsTrue(File.Exists(tempFilePath));
                var fileContent = File.ReadAllLines(tempFilePath);
                Assert.AreEqual(2, fileContent.Length);
                Assert.IsTrue(fileContent[1].Contains("1"));
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        [TestMethod]
        public void WriteEmptyRecipeList_ShouldOnlyWriteHeader()
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"test_recipe_{Guid.NewGuid()}.csv");
            var writer = new RecipeFileWriter();
            var emptyList = new List<Recipes.MbeTable.RecipeLines.RecipeLine>();

            try
            {
                writer.Write(emptyList, tempFilePath);
                var fileContent = File.ReadAllLines(tempFilePath);
                Assert.AreEqual(1, fileContent.Length, "Файл должен содержать только заголовок");
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }

        [TestMethod]
        public void WriteMultipleLines_ChecksCorrectFormat()
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"test_recipe_{Guid.NewGuid()}.csv");
            var writer = new RecipeFileWriter();
            var recipe = new List<Recipes.MbeTable.RecipeLines.RecipeLine>
            {
                Recipes.MbeTable.RecipeLines.RecipeLineFactory.NewLine("OPEN", 1, 0, 0, 0, 0, "!@#$%^&*()_+-=[]{}|;:'\\\"<>,.?/\\\\"),
                Recipes.MbeTable.RecipeLines.RecipeLineFactory.NewLine("CLOSE", 2, 0, 0, 0, 0, "comment"),
                Recipes.MbeTable.RecipeLines.RecipeLineFactory.NewLine("OPEN", 1, 0, 0, 0, 0, "test!!&&//")
            };

            try
            {
                writer.Write(recipe, tempFilePath);
                var fileContent = File.ReadAllLines(tempFilePath);

                Assert.AreEqual(4, fileContent.Length, "Должно быть 4 строки (заголовок + 3 записи)");
        
                var headerColumns = ParseCsvLine(fileContent[0]).Length;
                var dataColumns = ParseCsvLine(fileContent[1]).Length;
        
                Assert.AreEqual(headerColumns, dataColumns, 
                    "Количество столбцов в данных должно совпадать с заголовком");
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }

        private static string[] ParseCsvLine(string line)
        {
            var inQuotes = false;
            var cells = new List<string>();
            var currentCell = new System.Text.StringBuilder();

            foreach (var c in line)
            {
                if (c == '"')
                    inQuotes = !inQuotes;
                else if (c == ';' && !inQuotes)
                {
                    cells.Add(currentCell.ToString());
                    currentCell.Clear();
                }
                else
                    currentCell.Append(c);
            }
    
            cells.Add(currentCell.ToString());
            return cells.ToArray();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WriteNullRecipeList_ShouldThrowArgumentNullException()
        {
            var writer = new RecipeFileWriter();
            writer.Write(null, "test.csv");
        }

        [TestMethod]
        public void WriteWithComments_ChecksCommentsSaved()
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"test_recipe_{Guid.NewGuid()}.csv");
            var writer = new RecipeFileWriter();
            var comment = "Test Comment With;Semicolon";
            var recipe = new List<Recipes.MbeTable.RecipeLines.RecipeLine>
            {
                Recipes.MbeTable.RecipeLines.RecipeLineFactory.NewLine("CLOSE", 1, 0, 0, 0, 0, comment)
            };

            try
            {
                writer.Write(recipe, tempFilePath);
                var fileContent = File.ReadAllLines(tempFilePath);
                Assert.IsTrue(fileContent[1].EndsWith(comment),
                    "Комментарий должен быть сохранен в последнем столбце");
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }
    }
}