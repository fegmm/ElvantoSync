using ElvantoSync.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync;

interface ISync{
    public Task ApplyAsync();
    public bool IsActive();
}

abstract class Sync<TFrom, TTo>(Settings settings) : ISync
{
    public Settings Settings => settings;

    public abstract bool IsActive();
    public abstract string FromKeySelector(TFrom i);
    public abstract string ToKeySelector(TTo i);
    public abstract Task<IEnumerable<TFrom>> GetFromAsync();
    public abstract Task<IEnumerable<TTo>> GetToAsync();
    public virtual Task AddMissingAsync(IEnumerable<TFrom> missing) => Task.CompletedTask;
    public virtual Task RemoveAdditionalAsync(IEnumerable<TTo> additionals) => Task.CompletedTask;
    public virtual Task ApplyUpdate(IEnumerable<(TFrom, TTo)> matches) => Task.CompletedTask;

   

    public async Task ApplyAsync()
    {
        var from = await GetFromAsync();
        var to = await GetToAsync();
        
        var compare = from.CompareTo(to, FromKeySelector, ToKeySelector);

        System.IO.Directory.CreateDirectory(settings.OutputFolder);
        await System.IO.File.WriteAllLinesAsync(System.IO.Path.Combine(settings.OutputFolder, this.GetType().Name + "-missings.txt"), compare.additional.Select(FromKeySelector));
        await System.IO.File.WriteAllLinesAsync(System.IO.Path.Combine(settings.OutputFolder, this.GetType().Name + "-additionals.txt"), compare.missing.Select(ToKeySelector)); 
        
        if (!settings.LogOnly)
        {
            await AddMissingAsync(compare.additional);
            await RemoveAdditionalAsync(compare.missing);
            await ApplyUpdate(compare.matches);
        }
    }
}