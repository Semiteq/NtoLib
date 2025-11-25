using System;

namespace NtoLib.Recipes.MbeTable;

public partial class MbeTableFB
{
	internal const int IdRecipeActive = 1;
	internal const int IdCurrentLine = 3;
	internal const int IdStepCurrentTime = 4;
	internal const int IdForLoopCount1 = 5;
	internal const int IdForLoopCount2 = 6;
	internal const int IdForLoopCount3 = 7;
	internal const int IdEnaSend = 8;
	internal const int IdTotalTimeLeft = 101;
	internal const int IdLineTimeLeft = 102;
	internal const int IdIsRecipeConsistent = 103;

	private bool AreFloatsEqual(float a, float b) => Math.Abs(a - b) <= _epsilon;
}
