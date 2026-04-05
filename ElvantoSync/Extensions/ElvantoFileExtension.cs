using System;
using Fegmm.Elvanto.Models;

public static class ElvantoFileExtension
{
    extension(Fegmm.Elvanto.Models.FileObject file)
    {
        public DateTimeOffset? LastModified => file switch
        {
            _ when file.Content.Contains(".cloudfront.net") && int.TryParse(file.Content.Split("_")[^1], out var timestamp) => DateTimeOffset.FromUnixTimeSeconds(timestamp),
            _ => null
        };
    }
}