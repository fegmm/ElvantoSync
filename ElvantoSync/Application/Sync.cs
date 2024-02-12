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
        if (settings.Value.AddMissing)
        {
            return;
        }

        var tasks = missings.ToDictionary(FromKeySelector, AddMissing);

        try
        {
            await Task.WhenAll(tasks.Values);
        }
        catch (Exception e)
        {
            var failedIds = tasks.Where(i => i.Value.IsFaulted).Select(i => i.Key);
            logger.LogError(e, "Adding missing items failed for ids:\n{ids}", failedIds);
        }
        finally
        {
            var newMappings = tasks.Where(i => i.Value.IsCompletedSuccessfully)
                .Select(i => new IndexMapping() { FromId = i.Key, ToId = i.Value.Result, Type = this.GetType().Name });

            await dbContext.IndexMappings.AddRangeAsync(newMappings);
            await dbContext.SaveChangesAsync();
        }
    }

    protected virtual Task RemoveAdditional(TTo additional) => Task.CompletedTask;
    public async Task RemoveAdditionals(IEnumerable<TTo> additionals)
    {
        if (settings.Value.DeleteAdditionals)
        {
            return;
        }

        var tasks = additionals.ToDictionary(ToKeySelector, RemoveAdditional);

        try
        {
            await Task.WhenAll(tasks.Values);
        }
        catch (Exception e)
        {
            var failedIds = tasks.Where(i => i.Value.IsFaulted).Select(i => i.Key);
            logger.LogError(e, "Removing additional items failed for ids:\n{ids}", failedIds);
        }
        finally
        {
            var mappingToDelete = tasks.Where(i => i.Value.IsCompletedSuccessfully).Select(i => i.Key);
            await dbContext.IndexMappings
                .Where(i => i.Type == this.GetType().Name && mappingToDelete.Contains(i.ToId))
                .ExecuteDeleteAsync();
        }
    }

    protected virtual Task UpdateMatch(TFrom from, TTo to) => Task.CompletedTask;
    public virtual async Task UpdateMatches(IEnumerable<(TFrom, TTo)> matches)
    {
        if (settings.Value.UpdateExisting)
        {
            var tasks = matches.ToDictionary(
                match => (FromKeySelector(match.Item1), ToKeySelector(match.Item2)),
                match => UpdateMatch(match.Item1, match.Item2)
            );

            try
            {
                await Task.WhenAll(tasks.Values);
            }
            catch (Exception e)
            {
                var failedIds = tasks.Where(i => i.Value.IsFaulted).Select(i => i.Key);
                logger.LogError(e, "Applying updates failed for ids:\n{ids}", failedIds);
            }
        }
    }

    public async Task Apply()
    {
        var from = await GetFromAsync();
        var to = await GetToAsync();

        var compare = await RunComparison(from.ToList(), to.ToList());

        Directory.CreateDirectory(settings.Value.OutputFolder);
        await File.WriteAllLinesAsync(Path.Combine(settings.Value.OutputFolder, this.GetType().Name + "-missings.txt"), compare.additional.Select(FromKeySelector));
        await File.WriteAllLinesAsync(Path.Combine(settings.Value.OutputFolder, this.GetType().Name + "-additionals.txt"), compare.missing.Select(ToKeySelector));

        if (!settings.Value.LogOnly)
        {
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
        var fromIds = from.ToDictionary(FromKeySelector, i => i);
        var mappedFroms = await dbContext.IndexMappings
            .Where(i => i.Type == this.GetType().Name)
            .Where(i => fromIds.Keys.Contains(i.FromId))
            .ToDictionaryAsync(i => i.ToId, i => fromIds[i.FromId]);
        var mappedCompare = mappedFroms.CompareTo(to, i => i.Key, ToKeySelector);
        var (additional, matches, missing) = (
            mappedCompare.additional.Select(i => i.Value),
            mappedCompare.matches.Select(i => (i.Item1.Value, i.Item2)),
            mappedCompare.missing
        );

        if (!settings.Value.EnableFallback)
        {
            return new CompareResult<TFrom, TTo>(additional, matches, missing);
        }

        var fallbackCompare = from.CompareTo(to, FallbackFromKeySelector, FallbackToKeySelector);

        var filteredAdditional = additional.Where(i => fallbackCompare.additional.Contains(i));
        var filteredMissing = missing.Where(i => fallbackCompare.missing.Contains(i));
        var foundAdditional = additional.Except(filteredAdditional);
        var foundMissing = missing.Except(filteredMissing);

        matches = matches
            .Concat(fallbackCompare.matches.Where(i => foundAdditional.Contains(i.Item1) || foundMissing.Contains(i.Item2)))
            .Distinct();

        return new CompareResult<TFrom, TTo>(filteredAdditional, matches, filteredMissing);
    }
}