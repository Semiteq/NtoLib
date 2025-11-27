using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using FB;

using InSAT.Library.Interop;

using NtoLib.ConfigLoader.Facade;
using NtoLib.ConfigLoader.Pins;

namespace NtoLib.ConfigLoader;

[Serializable]
[ComVisible(true)]
[Guid("01187EA6-1B07-431B-9F4F-B7205C8246C9")]
[CatID(CatIDs.CATID_OTHER)]
[DisplayName("Загрузчик конфигурации")]
public class ConfigLoaderFB : StaticFBBase
{
	private const int SavePinId = 1;
	private const int LoadedPinId = 2;
	private const int SavedPinId = 3;
	private const int ErrorPinId = 4;

	private const uint ShutterQuantity = 16;
	private const uint SourcesQuantity = 32;
	private const uint ChamberHeaterQuantity = 16;
	private const uint WaterQuantity = 16;

	private static readonly TimeSpan _savedFlagDuration = TimeSpan.FromSeconds(1);

	[DisplayName("1. Путь к файлу конфигурации")]
	public string FilePath { get; set; } = @"C:\DISTR\Config\NamesConfig.yaml";

	private bool _previousSaveCommand;
	private DateTime _savedFlagResetTimeUtc;
	private bool _isRuntimeInitialized;

	[NonSerialized] private PinGroupManager _pinGroupManager;

	[NonSerialized] private IConfigLoaderService _service;

	protected override void ToRuntime()
	{
		base.ToRuntime();
		InitializeRuntime();
	}

	protected override void ToDesign()
	{
		base.ToDesign();
		CleanupRuntime();
	}

	protected override void CreatePinMap(bool newObject)
	{
		base.CreatePinMap(newObject);

		_pinGroupManager = new PinGroupManager(this);
		_pinGroupManager.CreateAllGroups(
			ShutterQuantity,
			SourcesQuantity,
			ChamberHeaterQuantity,
			WaterQuantity);

		FirePinSpaceChanged();
	}

	protected override void UpdateData()
	{
		base.UpdateData();

		if (!_isRuntimeInitialized)
		{
			return;
		}

		UpdateSavedFlagTimeout();
		ProcessSaveCommand();
	}

	private void InitializeRuntime()
	{
		if (_isRuntimeInitialized)
		{
			return;
		}

		try
		{
			_service = new ConfigLoaderService(
				ShutterQuantity,
				SourcesQuantity,
				ChamberHeaterQuantity,
				WaterQuantity);

			_previousSaveCommand = false;
			_isRuntimeInitialized = true;

			LoadConfiguration();
		}
		catch (Exception ex)
		{
			SetPinValue(LoadedPinId, false);
			SetPinValue(ErrorPinId, $"Initialization failed: {ex.Message}");
		}
	}

	private void CleanupRuntime()
	{
		_isRuntimeInitialized = false;
		_service = null;
	}

	private void LoadConfiguration()
	{
		if (_service == null)
		{
			SetPinValue(LoadedPinId, false);
			SetPinValue(ErrorPinId, "Service is not initialized.");
			return;
		}

		var result = _service.Load(FilePath);

		if (result.IsFailed)
		{
			SetPinValue(LoadedPinId, false);
			SetPinValue(ErrorPinId, _service.LastError);
			return;
		}

		WriteOutputPins();
		SetPinValue(LoadedPinId, true);
		SetPinValue(ErrorPinId, string.Empty);
	}

	private void WriteOutputPins()
	{
		if (_pinGroupManager == null || _service == null)
		{
			return;
		}

		_pinGroupManager.WriteOutputPins(_service.CurrentConfiguration);
	}

	private void ProcessSaveCommand()
	{
		var currentSaveCommand = GetPinValue<bool>(SavePinId);
		var isRisingEdge = currentSaveCommand && !_previousSaveCommand;
		_previousSaveCommand = currentSaveCommand;

		if (!isRisingEdge)
		{
			return;
		}

		ExecuteSave();
	}

	private void ExecuteSave()
	{
		if (_pinGroupManager == null || _service == null)
		{
			SetPinValue(SavedPinId, false);
			SetPinValue(ErrorPinId, "Service or pin manager is not initialized.");
			return;
		}

		var inputDto = _pinGroupManager.ReadInputPins(
			ShutterQuantity,
			SourcesQuantity,
			ChamberHeaterQuantity,
			WaterQuantity);

		var result = _service.SaveAndReload(FilePath, inputDto);

		if (result.IsFailed)
		{
			SetPinValue(SavedPinId, false);
			SetPinValue(ErrorPinId, _service.LastError);
			return;
		}

		WriteOutputPins();
		SetPinValue(SavedPinId, true);
		_savedFlagResetTimeUtc = DateTime.UtcNow.Add(_savedFlagDuration);
		SetPinValue(ErrorPinId, string.Empty);
	}

	private void UpdateSavedFlagTimeout()
	{
		var isSaved = GetPinValue<bool>(SavedPinId);

		if (!isSaved)
		{
			return;
		}

		if (DateTime.UtcNow >= _savedFlagResetTimeUtc)
		{
			SetPinValue(SavedPinId, false);
		}
	}
}
