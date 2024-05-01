using System;
using System.Collections.Generic;
using System.Linq;

namespace ElvantoSync.Extensions;

internal static class IEnumerableExtensions
{
    public static CompareResult<TFrom, TTo> CompareTo<TFrom, TTo, TKey>(this IEnumerable<TFrom> from, IEnumerable<TTo> to, Func<TFrom, TKey> fromKeySelector, Func<TTo, TKey> toKeySelector)
    {
        var fromDictionary = from.ToDictionary(fromKeySelector);
        var toDictionary = to.ToDictionary(toKeySelector);

        var additional = fromDictionary.Where(i => !toDictionary.ContainsKey(i.Key)).Select(i => i.Value);
        var missing = toDictionary.Where(i => !fromDictionary.ContainsKey(i.Key)).Select(i => i.Value);
        var matches = fromDictionary.Where(i => toDictionary.ContainsKey(i.Key)).Select(i => (i.Value, toDictionary[i.Key]));

        return new CompareResult<TFrom, TTo>(additional, matches, missing);
    }

    public static (List<T>, HashSet<string>) SplitByUniquenessBy<T>(this IEnumerable<T> items, Func<T, string> keySelector)
    {
        var duplicates = items
            .GroupBy(keySelector)
            .Where(i => i.Count() > 1)
            .Select(i => i.Key)
            .ToHashSet();

        var unique = items
            .Where(i => !duplicates.Contains(keySelector(i)))
            .ToList();
        return (unique, duplicates);
    }
}

record CompareResult<TFrom, TTo>(IEnumerable<TFrom> additional, IEnumerable<(TFrom, TTo)> matches, IEnumerable<TTo> missing);