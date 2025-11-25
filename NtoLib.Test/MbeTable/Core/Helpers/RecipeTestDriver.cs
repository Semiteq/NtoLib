using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;

namespace NtoLib.Test.MbeTable.Core.Helpers;

public sealed class RecipeTestDriver
{
	private readonly IRecipeFacade _facade;

	public RecipeTestDriver(IRecipeFacade facade)
	{
		_facade = facade;
	}

	public RecipeTestDriver AddDefaultStep(int index)
	{
		_facade.AddStep(index);
		return this;
	}

	public RecipeTestDriver AddWait(int index)
	{
		_facade.AddStep(index);
		_facade.ReplaceAction(index, (short)ServiceActions.Wait);
		return this;
	}

	public RecipeTestDriver AddFor(int index, int iterations)
	{
		_facade.AddStep(index);
		_facade.ReplaceAction(index, (short)ServiceActions.ForLoop);
		_facade.UpdateProperty(index, MandatoryColumns.Task, (float)iterations);
		return this;
	}

	public RecipeTestDriver AddEndFor(int index)
	{
		_facade.AddStep(index);
		_facade.ReplaceAction(index, (short)ServiceActions.EndForLoop);
		return this;
	}

	public RecipeTestDriver SetDuration(int index, float seconds)
	{
		_facade.UpdateProperty(index, MandatoryColumns.StepDuration, seconds);
		return this;
	}

	public RecipeTestDriver SetTask(int index, float value)
	{
		_facade.UpdateProperty(index, MandatoryColumns.Task, value);
		return this;
	}

	public RecipeTestDriver ReplaceAction(int index, short actionId)
	{
		_facade.ReplaceAction(index, actionId);
		return this;
	}

	public RecipeTestDriver SetSpeed(int index, float value)
	{
		var col = new ColumnIdentifier("speed");
		_facade.UpdateProperty(index, col, value);
		return this;
	}
}
