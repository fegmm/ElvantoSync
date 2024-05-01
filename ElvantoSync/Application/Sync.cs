using ElvantoSync.Extensions;
using ElvantoSync.Persistence.Entities;
using ElvantoSync.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync;

public interface ISync
{
    public bool IsActive { get; }
    public Task Apply();
}

public abstract class Sync<TFrom, TTo>(Persistence.DbContext dbContext, IOptions<SyncSettings> settings, ILogger logger) : ISync
{
    public bool IsActive => settings.Value.IsEnabled;

    public abstract string FromKeySelector(TFrom i);
    public abstract string ToKeySelector(TTo i);

    public virtual string FallbackFromKeySelector(TFrom i) => FromKeySelector(i);
    public virtual string FallbackToKeySelector(TTo i) => ToKeySelector(i);

    public abstract Task<IEnumerable<TFrom>> GetFromAsync();
    public abstract Task<IEnumerable<TTo>> GetToAsync();

    protected abstract Task<string> AddMissing(TFrom missing);
    public async Task AddMissings(IEnumerable<TFrom> missings)
    {
        if (!settings.Value.AddMissing)
        {
            return;
        }

        List<IndexMapping> created = [];
        var creationTaskBatched = missings
            .Chunk(1)
            .Select(batch => batch.ToDictionary(FromKeySelector, AddMissing));
        

        foreach (var tasks in creationTaskBatched)
        {
            try
            {
                await Task.WhenAll(tasks.Values);
            }
            catch
            {
                foreach (var faulted in tasks.Where(i => i.Value.IsFaulted))
                {
                    logger.LogError(faulted.Value.Exception, "Adding missing item failed for id: {id}", faulted.Key);
                }
            }
            finally
            {
                created.AddRange(tasks
                    .Where(i => i.Value.IsCompletedSuccessfully)
                    .Select(i => new IndexMapping()
                    {
                        FromId = i.Key,
                        ToId = i.Value.Result,
                        Type = this.GetType().Name
                    })
                );
            }
        }
        await dbContext.IndexMappings.AddRangeAsync(created);
        await dbContext.SaveChangesAsync();
    }

    protected virtual Task RemoveAdditional(TTo additional) => Task.CompletedTask;
    public async Task RemoveAdditionals(IEnumerable<TTo> additionals)
    {
        if (!settings.Value.DeleteAdditionals)
        {
            return;
        }

        List<string> removed = [];
        var deletionTaskBatched = additionals
            .Chunk(1)
            .Select(batch => batch.ToDictionary(ToKeySelector, RemoveAdditional));

        foreach (var tasks in deletionTaskBatched)
        {
            try
            {
                await Task.WhenAll(tasks.Values);
            }
            catch
            {
                foreach (var faulted in tasks.Where(i => i.Value.IsFaulted))
                {
                    logger.LogError(faulted.Value.Exception, "Removing additional item failed for id: {id}", faulted.Key);
                }
            }
            finally
            {
                removed.AddRange(tasks
                    .Where(i => i.Value.IsCompletedSuccessfully)
                    .Select(i => i.Key)
                );
            }
        }
        await dbContext.IndexMappings
            .Where(i => i.Type == this.GetType().Name && removed.Contains(i.ToId))
            .ExecuteDeleteAsync();
    }

    protected virtual Task UpdateMatch(TFrom from, TTo to) => Task.CompletedTask;
    public virtual async Task UpdateMatches(IEnumerable<(TFrom, TTo)> matches)
    {
        if (!settings.Value.UpdateExisting)
        {
            return;
        }

        var updateTaskBatched = matches
            .Chunk(1)
            .Select(batch => batch.ToDictionary(i => (FromKeySelector(i.Item1), ToKeySelector(i.Item2)), i => UpdateMatch(i.Item1, i.Item2)));

        foreach (var tasks in updateTaskBatched)
        {
            try
            {
                await Task.WhenAll(tasks.Values);
            }
            catch
            {
                foreach (var faulted in tasks.Where(i => i.Value.IsFaulted))
                {
                    logger.LogError(faulted.Value.Exception, "Updating match failed for ids:\n{fromId}\n{toId}", faulted.Key.Item1, faulted.Key.Item2);
                }
            }
        }
    }

    public async Task Apply()
    {
        var from = await GetFromAsync();
        var to = await GetToAsync();

        var compare = await RunComparison(from.ToList(), to.ToList());

        Directory.CreateDirectory(settings.Value.OutputFolder);
        await File.WriteAllLinesAsync(Path.Combine(settings.Value.OutputFolder, this.GetType().Name + "-missings.txt"), compare.additional.Select(i => $"{FallbackFromKeySelector(i)}:{FromKeySelector(i)}"));
        await File.WriteAllLinesAsync(Path.Combine(settings.Value.OutputFolder, this.GetType().Name + "-additionals.txt"), compare.missing.Select(i => $"{FallbackToKeySelector(i)}:{ToKeySelector(i)}"));


        if (!settings.Value.LogOnly)
        {
            logger.LogInformation("Will add {count} items, updated {count} and remove {count} items", compare.additional.Count(), compare.matches.Count(), compare.missing.Count());
            await AddMissings(compare.additional);
            await UpdateMatches(compare.matches);
            await RemoveAdditionals(compare.missing);
        }
        else
        {
            logger.LogInformation("Would have added {count} items, updated {count} items and removed {count} items", compare.additional.Count(), compare.matches.Count(), compare.missing.Count());
        }
    }

    private async Task<CompareResult<TFrom, TTo>> RunComparison(List<TFrom> from, List<TTo> to)
    {
        if (dbContext.IndexMappings.Where(i => i.Type == this.GetType().Name).Any())
        {
            return await RunComparisonWithMappings(from, to);
        }
        else
        {
            return await RunComparisonWithFallback(from, to);
        }
    }

    private async Task<CompareResult<TFrom, TTo>> RunComparisonWithFallback(List<TFrom> from, List<TTo> to)
    {
        // Handle duplicate fuzzy Ids by sorting them out and log warning
        (from, to) = FilterDuplicateFallbackIds(from, to);

        // Initial state (only items with unique keys)
        IEnumerable<TFrom> additionals = from;
        IEnumerable<TTo> missings = to;
        IEnumerable<(TFrom, TTo)> matches = [];

        // Run comparison
        CompareResult<TFrom, TTo> comparison = additionals.CompareTo(missings, FallbackFromKeySelector, FallbackToKeySelector);
        HashSet<TFrom> matchedAdditionals = comparison.matches.Select(i => i.Item1).ToHashSet();
        HashSet<TTo> matchedMissings = comparison.matches.Select(i => i.Item2).ToHashSet();

        // Filter out matched items
        additionals = additionals.Where(i => !matchedAdditionals.Contains(i)).ToList();
        missings = missings.Where(i => !matchedMissings.Contains(i)).ToList();
        matches = matches.Concat(comparison.matches);

        // Save found mappings
        IEnumerable<IndexMapping> newMappings = comparison.matches.Select(i => new IndexMapping()
        {
            FromId = FromKeySelector(i.Item1),
            ToId = ToKeySelector(i.Item2),
            Type = this.GetType().Name
        });
        await dbContext.IndexMappings.AddRangeAsync(newMappings);
        await dbContext.SaveChangesAsync();

        return new CompareResult<TFrom, TTo>(additionals, matches, missings);
    }

    private (List<TFrom>, List<TTo>) FilterDuplicateFallbackIds(List<TFrom> from, List<TTo> to)
    {
        var (fromUnique, fromDuplicates) = from.SplitByUniquenessBy(FallbackFromKeySelector);
        var (toUnique, toDuplicates) = to.SplitByUniquenessBy(FallbackToKeySelector);

        foreach (var duplicate in fromDuplicates)
        {
            logger.LogWarning("Ignoring entities with duplicate fallback key found in From: {key}", duplicate);
        }

        foreach (var duplicate in toDuplicates)
        {
            logger.LogWarning("Ignoring entities with duplicate fallback key found in To: {key}", duplicate);
        }

        return (fromUnique, toUnique);
    }

    private async Task<CompareResult<TFrom, TTo>> RunComparisonWithMappings(List<TFrom> from, List<TTo> to)
    {
        // Initial state
        IEnumerable<TFrom> additionals = from;
        IEnumerable<TTo> missings = to;
        IEnumerable<(TFrom, TTo)> matches = [];

        // Convert from to use stored ToIds
        Dictionary<string, TFrom> fromDict = from.ToDictionary(FromKeySelector);
        Dictionary<string, TFrom> mappedFrom = await dbContext.IndexMappings
            .Where(i => i.Type == this.GetType().Name && fromDict.Keys.Contains(i.FromId))
            .ToDictionaryAsync(i => i.ToId, i => fromDict[i.FromId]);

        // Run comparison
        var mappedComp = mappedFrom.CompareTo(to, i => i.Key, ToKeySelector);
        var matchedAdditionals = mappedComp.matches.Select(i => i.Item1.Value).ToHashSet();
        var matchedMissings = mappedComp.matches.Select(i => i.Item2).ToHashSet();

        additionals = additionals.Where(i => !matchedAdditionals.Contains(i)).ToList();
        missings = missings.Where(i => !matchedMissings.Contains(i)).ToList();
        matches = mappedComp.matches.Select(i => (i.Item1.Value, i.Item2));

        return new CompareResult<TFrom, TTo>(additionals, matches, missings);
    }
}