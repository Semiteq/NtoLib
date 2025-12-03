using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using FB;

using InSAT.Library.Interop;

using NtoLib.TrendPensManager.Facade;

namespace NtoLib.TrendPensManager;

[Serializable]
[ComVisible(true)]
[Guid("C92B2C17-3FCD-4D1A-950D-AC25DE65D8D4")]
[CatID(CatIDs.CATID_OTHER)]
[DisplayName("Менеджер перьев окна трендов")]
public class TrendPensManagerFB : StaticFBBase
{
	private const int AddPinId = 1;
	private const int PathToTrendId = 2;
	private const int PathToPinId = 3;
	private const int ErrorPinId = 4;

	private bool _previousAddCommand;

	private static readonly TimeSpan _savedFlagDuration = TimeSpan.FromSeconds(1);

	private DateTime _savedFlagResetTimeUtc;
	private bool _isRuntimeInitialized;

	[NonSerialized] private ITrendPensService? _trendPensService;

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

		ProcessAddCommand();
	}

	private void InitializeRuntime()
	{
		if (_isRuntimeInitialized)
		{
			return;
		}

		try
		{
			if (TreeItemHlp?.Project == null)
			{
				throw new InvalidOperationException("TreeItemHlp.Project is null.");
			}

			_trendPensService = new TrendPensService(TreeItemHlp.Project);
			_isRuntimeInitialized = true;
		}
		catch (Exception ex)
		{
			_isRuntimeInitialized = false;
			SetPinValue(ErrorPinId, $"Initialization failed: {ex.Message}");
		}
	}

	private void CleanupRuntime()
	{
		_isRuntimeInitialized = false;
		_trendPensService = null;
	}

	private void ProcessAddCommand()
	{
		if (_trendPensService == null)
		{
			return;
		}

		var currentSaveCommand = GetPinValue<bool>(AddPinId);
		var isRisingEdge = currentSaveCommand && !_previousAddCommand;
		_previousAddCommand = currentSaveCommand;

		if (!isRisingEdge)
		{
			return;
		}

		var trendPath = GetPinValue<string>(PathToTrendId);
		var pinPath = GetPinValue<string>(PathToPinId);
		var result = _trendPensService.Refresh(trendPath, pinPath);
		if (result.IsFailed)
		{
			SetPinValue(ErrorPinId, string.Join("|", result.Errors));
		}
	}
}
