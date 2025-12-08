using System;
using System.ComponentModel;

using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;

namespace NtoLib.Recipes.MbeTable;

public partial class MbeTableFB
{
	private uint _controllerIp1 = 192;
	private uint _controllerIp2 = 168;
	private uint _controllerIp3 = 0;
	private uint _controllerIp4 = 141;
	private uint _controllerTcpPort = 502;
	private uint _unitId = 69;
	private uint _timeoutMs = 1000;
	private uint _maxRetries = 3;
	private uint _backoffDelayMs = 200;
	private uint _magicNumber = 69;
	private WordOrder _wordOrder = WordOrder.LowHigh;
	private uint _controlBaseAddr = 8000;
	private uint _floatBaseAddr = 8100;
	private uint _floatAreaSize = 19600;
	private uint _intBaseAddr = 27700;
	private uint _intAreaSize = 1400;
	private uint _boolBaseAddr = 29100;
	private uint _boolAreaSize = 50;
	private float _epsilon = 1e-4f;
	private bool _logToFile = false;
	private string _logDirPath = "C:\\DISTR\\Logs";
	private string _configDirPath = "C:\\DISTR\\Config\\NtoLibTableConfig";

	[DisplayName("01. IP адрес контроллера байт 1")]
	public uint UControllerIp1
	{
		get => _controllerIp1;
		set => _controllerIp1 = value;
	}

	[DisplayName("02. IP адрес контроллера байт 2")]
	public uint UControllerIp2
	{
		get => _controllerIp2;
		set => _controllerIp2 = value;
	}

	[DisplayName("03. IP адрес контроллера байт 3")]
	public uint UControllerIp3
	{
		get => _controllerIp3;
		set => _controllerIp3 = value;
	}

	[DisplayName("04. IP адрес контроллера байт 4")]
	public uint UControllerIp4
	{
		get => _controllerIp4;
		set => _controllerIp4 = value;
	}

	[DisplayName("05. TCP порт")]
	public uint ControllerTcpPort
	{
		get => _controllerTcpPort;
		set => _controllerTcpPort = value;
	}

	[DisplayName("06. Unit ID")]
	[Description("Идентификатор Modbus устройства")]
	public uint UnitId
	{
		get => _unitId;
		set => _unitId = value;
	}

	[DisplayName("07. Задержка")]
	[Description("Задержка при ошибке связи, мс")]
	public uint TimeoutMs
	{
		get => _timeoutMs;
		set => _timeoutMs = value;
	}

	[DisplayName("08. Количество повторов")]
	[Description("Количество повторов при ошибке связи")]
	public uint MaxRetries
	{
		get => _maxRetries;
		set => _maxRetries = value;
	}

	[DisplayName("09. Задержка между повторами")]
	[Description("Задержка между попытками повтора при ошибке связи, мс")]
	public uint BackoffDelayMs
	{
		get => _backoffDelayMs;
		set => _backoffDelayMs = value;
	}

	[DisplayName("10. Magic Number")]
	[Description("Магическое число для проверки связи. Должно совпадать с настройкой контроллера.")]
	public uint MagicNumber
	{
		get => _magicNumber;
		set => _magicNumber = value;
	}

	[DisplayName("11. Порядок слов")]
	[Description("Порядок байт при передаче 32-битных значений (Float, DWord)")]
	[TypeConverter(typeof(WordOrderConverter))]
	public WordOrder WordOrder
	{
		get => _wordOrder;
		set => _wordOrder = value;
	}

	[DisplayName("12. Адрес системной области данных")]
	[Description("Определяет начальный адрес, где располагается зона системных данных (10 регистров)")]
	public uint UControlBaseAddr
	{
		get => _controlBaseAddr;
		set => _controlBaseAddr = value;
	}

	[DisplayName("13. Базовый адрес хранения данных типа Real (Float)")]
	[Description("Определяет начальный адрес, куда помещаются данные типа 'вещественный'")]
	public uint UFloatBaseAddr
	{
		get => _floatBaseAddr;
		set => _floatBaseAddr = value;
	}

	[DisplayName("14. Размер области хранения данных типа Real (Float)")]
	[Description("Определяет размер области для данных типа 'вещественный'.")]
	public uint UFloatAreaSize
	{
		get => _floatAreaSize;
		set => _floatAreaSize = value;
	}

	[DisplayName("15. Базовый адрес хранения данных типа Int")]
	[Description("Определяет начальный адрес, куда помещаются данные типа 'целый 16 бит'")]
	public uint UIntBaseAddr
	{
		get => _intBaseAddr;
		set => _intBaseAddr = value;
	}

	[DisplayName("16. Размер области хранения данных типа Int")]
	[Description("Определяет размер области для данных типа 'целый 16 бит'")]
	public uint UIntAreaSize
	{
		get => _intAreaSize;
		set => _intAreaSize = value;
	}

	[DisplayName("17. Эпсилон")]
	[Description("Допуск сравнения чисел с плавающей точкой")]
	public float Epsilon
	{
		get => _epsilon;
		set => _epsilon = value;
	}

	[DisplayName("18. Записывать лог в файл")]
	[Description("Если включено, все операции чтения/записи будут логироваться в файл.")]
	public bool LogToFile
	{
		get => _logToFile;
		set => _logToFile = value;
	}

	[DisplayName("19. Путь к каталогу логов")]
	[Description("Каталог, куда будут записываться логи. Можно использовать переменные окружения.")]
	public string LogDirPath
	{
		get => _logDirPath;
		set => _logDirPath = value;
	}

	[DisplayName("20. Путь к конфигурационному каталогу")]
	[Description("Каталог, где хранятся файлы конфигурации таблицы рецептов. Изменения вступят в силу при следующей загрузке блока.")]
	public string ConfigDirPath
	{
		get => _configDirPath;
		set => _configDirPath = value;
	}
}
