﻿using System;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Rendering;

/// <summary>
/// Coordinates table rendering by subscribing to state changes and triggering
/// selective invalidations of the DataGridView.
/// </summary>
public interface ITableRenderCoordinator : IDisposable
{
    /// <summary>
    /// Initializes event subscriptions and prepares for rendering coordination.
    /// </summary>
    void Initialize();
}