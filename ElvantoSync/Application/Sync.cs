using ElvantoSync.Extensions;
using ElvantoSync.Settings;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync;

interface ISync
{
    public bool IsActive { get; }
    public Task ApplyAsync();
}

abstract class MappedSync<TFrom, TTo>(Persistence.DbContext dbContext, MappedSyncSettings settings) : Sync<TFrom, TTo>(settings)
{
    public MappedSyncSettings Settings => settings;

    public override async Task<CompareResult<TFrom, TTo>> RunComparison(IEnumerable<TFrom> from, IEnumerable<TTo> to)
    {
        var fromIds = from.ToDictionary(FromKeySelector, i => i);
        var mappedFroms = await dbContext.IndexMappings
            .Where(i => i.Type == this.GetType().Name)
            .Where(i => fromIds.ContainsKey(i.FromId))
            .ToDictionaryAsync(i => i.ToId, i => fromIds[i.FromId]);
        var compare = mappedFroms.CompareTo(to, i => i.Key, ToKeySelector);
        return new (compare.additional.Select(i => i.Value), compare.matches.Select(i => (i.Item1.Value, i.Item2)), compare.missing);

        /*
         * ToIdStored(FromId((From)) <-> ToId
         *  - Additional: Create
         *  - Matches: Update
         *  - Missing: Drop ToId
         * 
         * ToIdStored <-> ToId
         *  - Additional: Remove Cache
         *  - Matches: NoOp
         *  - Missing: Create
         */
    }
}

abstract class Sync<TFrom, TTo>(SyncSettings settings) : ISync
{
    public bool IsActive => settings.IsEnabled;
    public abstract string FromKeySelector(TFrom i);
    public abstract string ToKeySelector(TTo i);
    public abstract Task<IEnumerable<TFrom>> GetFromAsync();
    public abstract Task<IEnumerable<TTo>> GetToAsync();
    public virtual Task AddMissingAsync(IEnumerable<TFrom> missing) => Task.CompletedTask;
    public virtual Task RemoveAdditionalAsync(IEnumerable<TTo> additionals) => Task.CompletedTask;
    public virtual Task ApplyUpdate(IEnumerable<(TFrom, TTo)> matches) => Task.CompletedTask;
    public virtual Task<CompareResult<TFrom, TTo>> RunComparison(IEnumerable<TFrom> from, IEnumerable<TTo> to) 
        => Task.FromResult(from.CompareTo(to, FromKeySelector, ToKeySelector));

    public async Task ApplyAsync()
    {
        var from = await GetFromAsync();
        var to = await GetToAsync();

        var compare = await RunComparison(from, to);

        Directory.CreateDirectory(settings.OutputFolder);
        await File.WriteAllLinesAsync(Path.Combine(settings.OutputFolder, this.GetType().Name + "-missings.txt"), compare.additional.Select(FromKeySelector));
        await File.WriteAllLinesAsync(Path.Combine(settings.OutputFolder, this.GetType().Name + "-additionals.txt"), compare.missing.Select(ToKeySelector));

        if (!settings.LogOnly)
        {
            await AddMissingAsync(compare.additional);
            await RemoveAdditionalAsync(compare.missing);
            await ApplyUpdate(compare.matches);
        }
    }
}