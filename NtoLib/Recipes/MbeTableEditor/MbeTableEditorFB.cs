using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using FB;
using FB.VisualFB;

using InSAT.Library.Interop;

using NtoLib.Recipes.MbeTable;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;
using NtoLib.Recipes.MbeTable.ModuleCore.Formulas;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure;

namespace NtoLib.Recipes.MbeTableEditor;

[CatID(CatIDs.CATID_OTHER)]
[Guid("B9FC0D44-3827-4D86-9CD8-0F0965DCED47")]
[FBOptions(FBOptions.EnableChangeConfigInRT)]
[VisualControls(typeof(MbeTableEditorControl))]
[DisplayName("Редактор рецептов MBE")]
[ComVisible(true)]
[Serializable]
public partial class MbeTableEditorFB : VisualFBBase, IPinGroupReader
{
	private const string DefaultPropertyDefsFileName = "PropertyDefs.yaml";
	private const string DefaultColumnDefsFileName = "ColumnDefs.yaml";
	private const string DefaultPinGroupDefsFileName = "PinGroupDefs.yaml";
	private const string DefaultActionsDefsFileName = "ActionsDefs.yaml";

	[NonSerialized] private Lazy<AppConfiguration>? _appConfigurationLazy;
	[NonSerialized] private IReadOnlyDictionary<short, CompiledFormula>? _compiledFormulas;
	[NonSerialized] private IServiceProvider? _serviceProvider;

	[Browsable(false)] public IServiceProvider? ServiceProvider => _serviceProvider;

	protected override void ToDesign()
	{
		base.ToDesign();
		CleanupRuntime();
	}

	protected override void ToRuntime()
	{
		base.ToRuntime();
		InitializeRuntime();
	}

	public override void Dispose()
	{
		CleanupRuntime();
		base.Dispose();
	}

	private void InitializeRuntime()
	{
		if (_serviceProvider != null)
		{
			return;
		}

		try
		{
			var state = EnsureConfigurationLoaded();

			if (_compiledFormulas == null)
			{
				throw new InvalidOperationException("Compiled formulas cache was not initialized.");
			}

			_serviceProvider = MbeTableServiceConfigurator.ConfigureEditorServices(this, state, _compiledFormulas);
		}
		catch (Exception ex)
		{
			var fullMessage =
				$"Service initialization failed:\n\n{ex.GetType().Name}: {ex.Message}\n\nStackTrace:\n{ex.StackTrace}";

			if (ex.InnerException != null)
			{
				fullMessage +=
					$"\n\nInner Exception:\n{ex.InnerException.GetType().Name}: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
			}

			MessageBox.Show(fullMessage, @"Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

			throw;
		}
	}

	private void CleanupRuntime()
	{
		if (_serviceProvider == null)
		{
			return;
		}

		if (_serviceProvider is IDisposable disposableProvider)
		{
			disposableProvider.Dispose();
		}

		_serviceProvider = null;
	}
}
