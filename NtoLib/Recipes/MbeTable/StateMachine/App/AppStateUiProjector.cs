#nullable enable
using NtoLib.Recipes.MbeTable.Presentation.Status;

namespace NtoLib.Recipes.MbeTable.StateMachine.App
{
    public record UiProjection(
        bool EnableWrite,
        bool EnableOpen,
        bool EnableAddBefore,
        bool EnableAddAfter,
        bool EnableDelete,
        bool EnableSave,
        string? StatusText,
        StatusMessage StatusKind
    );
    
    public sealed class AppStateUiProjector
    {
        public UiProjection Project(AppState s)
        {
            var kind = s.Message?.Kind switch
            {
                StatusKind.Error => StatusMessage.Error,
                StatusKind.Warning => StatusMessage.Warning,
                _ => StatusMessage.Info
            };

            return new UiProjection(
                EnableWrite: s.Permissions.CanWriteRecipe,
                EnableOpen: s.Permissions.CanOpenFile,
                EnableAddBefore: s.Permissions.CanAddStep,
                EnableAddAfter: s.Permissions.CanAddStep,
                EnableDelete: s.Permissions.CanDeleteStep,
                EnableSave: s.Permissions.CanSaveFile,
                StatusText: s.Message?.Text,
                StatusKind: kind
            );
        }
    }
}