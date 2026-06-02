using System.ComponentModel;

namespace NtoLib.Recipes.MbeTableEditor;

public partial class MbeTableEditorFB
{
	private float _epsilon = 1e-4f;

	private string? _configDirPath = "C:\\DISTR\\Config\\MBE";

	private bool _logToFile = false;
	private string _logDirPath = "C:\\DISTR\\Logs\\MbeTableEditor";

	[Category("Вычисления")]
	[DisplayName("Эпсилон")]
	[Description("Допуск сравнения чисел с плавающей точкой")]
	public float Epsilon
	{
		get => _epsilon;
		set => _epsilon = value;
	}

	[Category("Конфигурация")]
	[DisplayName("Путь к конфигурационному каталогу")]
	[Description(
		"Каталог, где хранятся файлы конфигурации таблицы рецептов. Изменения вступят в силу при следующей загрузке блока.")]
	public string? ConfigDirPath
	{
		get => _configDirPath;
		set => _configDirPath = value;
	}

	[Category("Логирование")]
	[DisplayName("Записывать лог в файл")]
	[Description("Если включено, все операции чтения/записи будут логироваться в файл.")]
	public bool LogToFile
	{
		get => _logToFile;
		set => _logToFile = value;
	}

	[Category("Логирование")]
	[DisplayName("Путь к каталогу логов")]
	[Description("Каталог, куда будут записываться логи. Можно использовать переменные окружения.")]
	public string LogDirPath
	{
		get => _logDirPath;
		set => _logDirPath = value;
	}
}
