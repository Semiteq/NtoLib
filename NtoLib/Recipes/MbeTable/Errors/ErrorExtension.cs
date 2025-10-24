using System;
using System.Globalization;

using FluentResults;

namespace NtoLib.Recipes.MbeTable.Errors;

public static class ErrorExtension
{
    private const string CodeKey = nameof(Codes);

    public static Error WithCode(this Error error, Codes code)
    {
        if (error == null) throw new ArgumentNullException(nameof(error));
        return error.WithMetadata(CodeKey, code);
    }

    public static bool TryGetCode(this IReason reason, out Codes code)
    {
        if (reason?.Metadata != null &&
            reason.Metadata.TryGetValue(CodeKey, out var val) &&
            TryConvertToCode(val, out code))
        {
            return true;
        }

        code = default;
        return false;
    }

    public static bool TryGetCode(this Result result, out Codes code)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));

        foreach (var reason in result.Reasons)
        {
            if (reason.TryGetCode(out code))
                return true;
        }

        code = default;
        return false;
    }

    public static bool TryGetCode<T>(this Result<T> result, out Codes code)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        return result.ToResult().TryGetCode(out code);
    }

    private static bool TryConvertToCode(object value, out Codes code)
    {
        switch (value)
        {
            case Codes c:
                code = c;
                return true;
            case int i:
                code = (Codes)i;
                return true;
            case string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n):
                code = (Codes)n;
                return true;
            default:
                code = default;
                return false;
        }
    }
}