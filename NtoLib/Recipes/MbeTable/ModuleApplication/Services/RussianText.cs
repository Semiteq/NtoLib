using System;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Services;

internal static class RussianText
{
    public static string Pluralize(int n, string one, string few, string many)
    {
        n = Math.Abs(n) % 100;
        var n1 = n % 10;
        if (n > 10 && n < 20) return many;
        if (n1 > 1 && n1 < 5) return few;
        if (n1 == 1) return one;
        return many;
    }

    public static string FormatCountNoun(int n, string one, string few, string many)
    {
        return $"{n} {Pluralize(n, one, few, many)}";
    }

    public static string InstrumentalWithCount(int n, string oneSingularInstrumental, string pluralInstrumental)
    {
        return n == 1 ? $"с {oneSingularInstrumental}" : $"с {pluralInstrumental}";
    }
}