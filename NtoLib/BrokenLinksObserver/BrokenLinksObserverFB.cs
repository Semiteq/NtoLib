using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using FB;

using FluentResults;

using InSAT.Library.Interop;

using NtoLib.BrokenLinksObserver.Facade;

namespace NtoLib.BrokenLinksObserver;

[Serializable]
[ComVisible(true)]
[Guid("2915271A-7CFB-4AFC-9D42-2423469221A4")]
[CatID(CatIDs.CATID_OTHER)]
[DisplayName("Менеджер битых ссылок")]
public class BrokenLinksObserverFB : StaticFBBase
{
	private const int SearchPinId = 1;
	private const int ProcessingPinId = 2;
	private const int ErrorPinId = 3;

	private static readonly TimeSpan _savedFlagDuration = TimeSpan.FromSeconds(1);

	[DisplayName("1. Путь к файлу отчета")]
	public string FilePath { get; set; } = @"C:\DISTR\Reports\Report.yaml";

	private bool _previousSearchCommand;
	private DateTime _savedFlagResetTimeUtc;
	private bool _isRuntimeInitialized;

	[NonSerialized] private BrokenLinksService? _brokenLinksService;

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

	protected override void UpdateData()
	{
		base.UpdateData();

		if (!_isRuntimeInitialized)
		{
			return;
		}

		UpdateSearchFlagTimeout();
		ProcessSearchCommand();
	}

	private void InitializeRuntime()
	{
		if (_isRuntimeInitialized)
		{
			return;
		}

		try
		{
			_brokenLinksService = new BrokenLinksService();
			_isRuntimeInitialized = true;
		}
		catch (Exception ex)
		{
			SetPinValue(ErrorPinId, $"Initialization failed: {ex.Message}");
		}
	}

	private void CleanupRuntime()
	{
		_isRuntimeInitialized = false;
		_brokenLinksService = null;
	}

	private void UpdateSearchFlagTimeout()
	{
		var isSearch = GetPinValue<bool>(SearchPinId);

		if (!isSearch)
		{
			return;
		}

		if (DateTime.UtcNow >= _savedFlagResetTimeUtc)
		{
			SetPinValue(ProcessingPinId, false);
		}
	}

	private void ProcessSearchCommand()
	{
		_brokenLinksService?.Refresh();
	}
}
