using FluentResults;

namespace NtoLib.Recipes.MbeTable.ResultsExtension;

public sealed class BilingualError : Error
{
    public string MessageEn { get; }
    public string MessageRu { get; }
    
    public BilingualError(string messageEn, string messageRu) : base(messageEn)
    {
        MessageEn = messageEn;
        MessageRu = messageRu;
    }
}