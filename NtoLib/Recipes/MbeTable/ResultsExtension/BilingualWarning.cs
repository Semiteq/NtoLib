using FluentResults;

namespace NtoLib.Recipes.MbeTable.ResultsExtension;

public class BilingualWarning : Success
{
	public BilingualWarning(string messageEn, string messageRu) : base(messageEn)
	{
		MessageEn = messageEn;
		MessageRu = messageRu;
	}

	public string MessageEn { get; }
	public string MessageRu { get; }
}
