using System;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Behavior;

internal interface ITableGridBehavior : IDisposable
{
	void Attach();
	void Detach();
}
