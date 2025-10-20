﻿using InSAT.OPC;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

/// <summary>
/// Abstraction over pin reading for testability.
/// </summary>
public interface IPinAccessor
{
    OpcQuality GetQuality(int pinId);
    T GetValue<T>(int pinId);
}