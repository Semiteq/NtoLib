using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.State;

public static class ReasonSequenceComparer
{
	public static bool SequenceEqual(IEnumerable<IReason> a, IEnumerable<IReason> b)
	{
		if (ReferenceEquals(a, b))
		{
			return true;
		}

		if (a is null || b is null)
		{
			return false;
		}

		static string Key(IReason r)
		{
			var type = r.GetType().FullName ?? r.GetType().Name;
			var message = r.Message ?? string.Empty;

			return $"{type}|{message}";
		}

		var dictA = a.GroupBy(Key).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
		var dictB = b.GroupBy(Key).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

		if (dictA.Count != dictB.Count)
		{
			return false;
		}

		foreach (var kv in dictA)
		{
			if (!dictB.TryGetValue(kv.Key, out var count))
			{
				return false;
			}

			if (count != kv.Value)
			{
				return false;
			}
		}

		return true;
	}
}
