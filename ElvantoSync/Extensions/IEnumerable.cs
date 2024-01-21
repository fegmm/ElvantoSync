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

        var additional = toDictionary.Where(i => !fromDictionary.ContainsKey(i.Key)).Select(i => fromDictionary[i.Key]);
        var missing = fromDictionary.Where(i => !toDictionary.ContainsKey(i.Key)).Select(i => toDictionary[i.Key]);
        var matches = fromDictionary.Where(i => toDictionary.ContainsKey(i.Key)).Select(i => (i.Value, toDictionary[i.Key]));

        return new CompareResult<TFrom, TTo>(additional, matches, missing);
    }
}

record CompareResult<TFrom, TTo>(IEnumerable<TFrom> additional, IEnumerable<(TFrom, TTo)> matches, IEnumerable<TTo> missing);