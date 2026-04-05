using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions.Serialization;

namespace ElvantoSync.Extensions;

internal static class IParsableExtensions
{
    public static async Task<T> ConvertTo<T>(this IParsable source) where T : IParsable
    {
        await using var stream = KiotaJsonSerializer.SerializeAsStream(source);
        return await KiotaJsonSerializer.DeserializeAsync<T>(stream);
    }

    public static async Task<IEnumerable<T>> ConvertTo<T>(this UntypedNode source) where T : IParsable
    {
        await using var stream = KiotaJsonSerializer.SerializeAsStream(source);
        return await KiotaJsonSerializer.DeserializeCollectionAsync<T>(stream);
    }
}