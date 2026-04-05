using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElvantoSync.Extensions;

public static class TaskExtensions
{
    extension<T>(IEnumerable<Task<T>> tasks)
    {
        public async Task<IEnumerable<T>> WhenAll() => await Task.WhenAll(tasks);
        public async IAsyncEnumerable<T> ToAsyncEnumerable()
        {
            foreach (var task in tasks)
            {
                yield return await task;
            }
        }
    }
}