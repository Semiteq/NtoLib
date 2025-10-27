using System;

namespace NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

[Flags]
public enum BlockingScope
{
    None = 0,
    Save = 1 << 0,
    Send = 1 << 1,
    Edit = 1 << 2,
    Load = 1 << 3,
    
    SaveAndSend = Save | Send,
    AllOperations = Save | Send | Edit | Load
}