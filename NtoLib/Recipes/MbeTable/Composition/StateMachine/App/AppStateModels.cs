#nullable enable
using System;
using System.Collections.Immutable;

namespace NtoLib.Recipes.MbeTable.Composition.StateMachine.App
{
    public record UiPermissions(
        bool CanWriteRecipe,
        bool CanOpenFile,
        bool CanAddStep,
        bool CanDeleteStep,
        bool CanSaveFile
    )
    {
        public static UiPermissions None => new(false, false, false, false, false);
    }
    
    public enum AppMode
    {
        Editor,
        Runtime
    }

    public enum BusyKind
    {
        Idle,
        Loading,
        Saving,
        Transferring,
        Executing
    }

    public enum StatusKind
    {
        None,
        Info,
        Error,
        Warning
    }

    public enum MessageTag
    {
        None,
        VmInvalid,
        LoadSuccess,
        LoadError,
        SaveSuccess,
        SaveError,
        TransferSuccess,
        TransferError
    }

    public record UiMessage(
        MessageTag Tag,
        string Text,
        StatusKind Kind,
        bool Sticky,
        bool AckRequired
    );

    public static class ErrorLogLimits
    {
        public const int MaxErrors = 100;
    }

    public record AppState(
        AppMode Mode,
        BusyKind Busy,
        bool VmOk,
        bool EnaSendOk,
        bool RecipeActive,
        UiPermissions Permissions,
        UiMessage? Message,
        Guid? ActiveOperationId,
        string? ActiveFilePath,
        IImmutableList<string> ErrorLog
    )
    {
        public static AppState Initial(AppMode mode) =>
            new(mode,
                BusyKind.Idle,
                VmOk: false,
                EnaSendOk: false,
                RecipeActive: false,
                Permissions: UiPermissions.None,
                Message: null,
                ActiveOperationId: null,
                ActiveFilePath: null,
                ErrorLog: ImmutableList<string>.Empty);
    }
}