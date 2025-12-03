using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using FB;

using InSAT.Library.Interop;

using MasterSCADA.Hlp;

using NtoLib.PinConnector.Facade;

namespace NtoLib.PinConnector;

[Serializable]
[ComVisible(true)]
[Guid("B8C7E2E4-7E9D-4F5F-9F8B-8B4C4B1E1234")]
[CatID(CatIDs.CATID_OTHER)]
[DisplayName("Менеджер связи пинов")]
public class PinConnectorFB : StaticFBBase
{
	private const int ConnectTriggerId = 1;
	private const int SourcePathId = 2;
	private const int TargetPathId = 3;
	private const int ErrorPinId = 4;
	private const int QueuedCountPinId = 5;

	private bool _isRuntimeInitialized;
	private bool _previousTrigger;

	[NonSerialized] private IPinConnectorService? _service;
	[NonSerialized] private int _queuedCount;

	protected override void ToRuntime()
	{
		base.ToRuntime();
		InitializeRuntime();
	}

	protected override void ToDesign()
	{
		// Flush pending connections when returning to design mode
		if (_service != null)
		{
			var result = _service.FlushPending();
			if (result.IsFailed)
			{
				// Log or handle flush errors if needed
				System.Diagnostics.Debug.WriteLine($"FlushPending errors: {string.Join("|", result.Errors)}");
			}
		}

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

		ProcessConnectCommand();
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

			_service = new PinConnectorService(TreeItemHlp.Project);
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
		_service = null;
		_queuedCount = 0;
	}

	private void ProcessConnectCommand()
	{
		var currentTrigger = GetPinValue<bool>(ConnectTriggerId);
		var isRisingEdge = currentTrigger && !_previousTrigger;
		_previousTrigger = currentTrigger;

		if (!isRisingEdge || _service == null)
		{
			return;
		}

		var sourcePath = GetPinValue<string>(SourcePathId);
		var targetPath = GetPinValue<string>(TargetPathId);

		var result = _service.Enqueue(sourcePath, targetPath);
		if (result.IsFailed)
		{
			SetPinValue(ErrorPinId, string.Join("|", result.Errors));
		}
		else
		{
			_queuedCount++;
			SetPinValue(QueuedCountPinId, _queuedCount);
			SetPinValue(ErrorPinId, string.Empty);
		}
	}
}
