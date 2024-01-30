namespace ElvantoSync.Settings;

internal record ApplicationSettings
{
    internal const string ConfigSection = "Application";

    public string ConnectionString { get; set; } = "Data Source=ElvantoSync.db";
    public string ElvantoKey { get; init; }
    public string KASLogin { get; init; }
    public string KASAuthData { get; init; }
    public string NextcloudServer { get; init; }
    public string NextcloudUser { get; init; }
    public string NextcloudPassword { get; init; }
}