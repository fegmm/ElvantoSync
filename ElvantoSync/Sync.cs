using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElvantoSync
{
    abstract class Sync<TKey, TFrom, TTo>(Settings settings)
    {
        public Settings Settings => settings;
        public abstract Task<Dictionary<TKey, TFrom>> GetFromAsync();
        public abstract Task<Dictionary<TKey, TTo>> GetToAsync();
        public virtual Task AddMissingAsync(Dictionary<TKey, TFrom> missing) => Task.CompletedTask;
        public virtual Task RemoveAdditionalAsync(Dictionary<TKey, TTo> additionals) => Task.CompletedTask;

        public async Task ApplyAsync()
        {
            var from = await GetFromAsync();
            var to = await GetToAsync();
            var missing = from.Where(i => !to.ContainsKey(i.Key)).ToDictionary(i => i.Key, i => i.Value);
            var additional = to.Where(i => !from.ContainsKey(i.Key)).ToDictionary(i => i.Key, i => i.Value);

            System.IO.Directory.CreateDirectory(settings.OutputFolder);
            await System.IO.File.WriteAllLinesAsync(System.IO.Path.Combine(settings.OutputFolder, this.GetType().Name + "-missings.txt"), missing.Keys.Select(i => i.ToString()));
            await System.IO.File.WriteAllLinesAsync(System.IO.Path.Combine(settings.OutputFolder, this.GetType().Name + "-additionals.txt"), additional.Keys.Select(i => i.ToString())); 
            
            if (!settings.LogOnly)
            {
                await AddMissingAsync(missing);
                await RemoveAdditionalAsync(additional);
            }
        }
    }
}
