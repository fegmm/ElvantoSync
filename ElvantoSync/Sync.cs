using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ElvantoSync
{
    public interface ISync{
        public Task ApplyAsync();
        public bool IsActive();
    }

    public abstract class Sync<TKey, TFrom, TTo>(Settings settings) : ISync
    {
        public Settings Settings => settings;

        public abstract bool IsActive();
        public abstract Task<Dictionary<TKey, TFrom>> GetFromAsync();
        public abstract Task<Dictionary<TKey, TTo>> GetToAsync();
        public virtual Task AddMissingAsync(Dictionary<TKey, TFrom> missing) => Task.CompletedTask;
        public virtual Task RemoveAdditionalAsync(Dictionary<TKey, TTo> additionals) => Task.CompletedTask;
        public virtual Task ApplyUpdate(IEnumerable<(TFrom, TTo)> matches) => Task.CompletedTask;
       

        public async Task ApplyAsync()
        {
            var from = await GetFromAsync();
            var to = await GetToAsync();
            var missing = from.Where(i => !to.ContainsKey(i.Key)).ToDictionary(i => i.Key, i => i.Value);
            var additional = to.Where(i => !from.ContainsKey(i.Key)).ToDictionary(i => i.Key, i => i.Value);
            var matches = from.Where(i => to.ContainsKey(i.Key)).Select(i => (i.Value, to[i.Key]));

            System.IO.Directory.CreateDirectory(settings.OutputFolder);
            await System.IO.File.WriteAllLinesAsync(System.IO.Path.Combine(settings.OutputFolder, this.GetType().Name + "-missings.txt"), missing.Keys.Select(i => i.ToString()));
            await System.IO.File.WriteAllLinesAsync(System.IO.Path.Combine(settings.OutputFolder, this.GetType().Name + "-additionals.txt"), additional.Keys.Select(i => i.ToString())); 
            
            if (!settings.LogOnly)
            {
                await AddMissingAsync(missing);
                await RemoveAdditionalAsync(additional);
                await ApplyUpdate(matches);
            }
        }
    }
}