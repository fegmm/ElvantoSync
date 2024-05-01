using ElvantoSync.Infrastructure.Nextcloud;
using Microsoft.Extensions.DependencyInjection;
using Nextcloud.Clients;
using Nextcloud.Interfaces;
using NextcloudApi;
using Polly;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.RateLimiting;
using WebDav;
namespace Nextcloud.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddNextcloudClients(this IServiceCollection services, string nextcloudUrl, string username, string password, string applicationName)
    {
        byte[] authToken = Encoding.UTF8.GetBytes($"{username}:{password}");
        var auth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

        var client = services.AddHttpClient<INextcloudCircleClient, NextcloudCircleClient>(i =>
        {
            i.BaseAddress = new Uri(nextcloudUrl);
            i.Timeout = TimeSpan.FromMinutes(5);
            i.DefaultRequestHeaders.Authorization = auth;
            i.DefaultRequestHeaders.UserAgent.ParseAdd(applicationName);
            i.DefaultRequestHeaders.Add("OCS-ApiRequest", "true");
            i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
        client.AddResilienceHandler("rate-limit", GetRateLimiter);

        client = services.AddHttpClient<INextcloudCollectivesClient, NextcloudCollectivesClient>(i =>
        {
            i.BaseAddress = new Uri(nextcloudUrl);
            i.Timeout = TimeSpan.FromMinutes(5);
            i.DefaultRequestHeaders.Authorization = auth;
            i.DefaultRequestHeaders.UserAgent.ParseAdd(applicationName);
            i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            i.DefaultRequestHeaders.Add("requesttoken", NextcloudCollectivesClient.GetCsrfToken(i).Result);
        });
        client.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
        {
            UseCookies = true,
        });
        client.AddResilienceHandler("rate-limit-1", i => i.AddConcurrencyLimiter(new ConcurrencyLimiterOptions()
        {
            PermitLimit = 1,
            QueueLimit = 10000,
        }));

        client = services.AddHttpClient<INextcloudDeckClient, NextcloudDeckClient>(i =>
        {
            i.BaseAddress = new Uri(nextcloudUrl);
            i.Timeout = TimeSpan.FromMinutes(5);
            i.DefaultRequestHeaders.Authorization = auth;
            i.DefaultRequestHeaders.UserAgent.ParseAdd(applicationName);
            i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
        client.AddResilienceHandler("rate-limit", GetRateLimiter);

        client = services.AddHttpClient<INextcloudTalkClient, NextcloudTalkClient>(i =>
        {
            i.BaseAddress = new Uri(nextcloudUrl);
            i.Timeout = TimeSpan.FromMinutes(5);
            i.DefaultRequestHeaders.Authorization = auth;
            i.DefaultRequestHeaders.Add("OCS-ApiRequest", "true");
            i.DefaultRequestHeaders.UserAgent.ParseAdd(applicationName);
            i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
        client.AddResilienceHandler("rate-limit", GetRateLimiter);

        client = services.AddHttpClient<INextcloudProvisioningClient, NextcloudProvisioningClient>(i =>
        {
            i.BaseAddress = new Uri(nextcloudUrl);
            i.Timeout = TimeSpan.FromMinutes(5);
            i.DefaultRequestHeaders.Authorization = auth;
            i.DefaultRequestHeaders.Add("OCS-ApiRequest", "true");
            i.DefaultRequestHeaders.UserAgent.ParseAdd(applicationName);
            i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
        client.AddResilienceHandler("rate-limit", GetRateLimiter);


        client = services.AddHttpClient<INextcloudGroupFolderClient, NextcloudGroupFolderClient>(i =>
        {
            i.BaseAddress = new Uri(nextcloudUrl);
            i.Timeout = TimeSpan.FromMinutes(5);
            i.DefaultRequestHeaders.Authorization = auth;
            i.DefaultRequestHeaders.Add("OCS-ApiRequest", "true");
            i.DefaultRequestHeaders.UserAgent.ParseAdd(applicationName);
            i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
        client.AddResilienceHandler("rate-limit", GetRateLimiter);

        client = services.AddHttpClient<WebDavClient, WebDavClient>(i =>
        {
            string encoded = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
            i.BaseAddress = new Uri(nextcloudUrl);
            i.DefaultRequestHeaders.Add("OCS-APIRequest", "true");
            i.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);
        });
        client.AddResilienceHandler("rate-limit", GetRateLimiter);

        //services.AddSingleton<WebDavClient>(GetWebDav(username, password, nextcloudUrl));
        services.AddSingleton<NextcloudApi.Api>(GetNextCloudApi(username, password, nextcloudUrl));
        return services;
    }

    private static void GetRateLimiter(ResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        builder.AddConcurrencyLimiter(new ConcurrencyLimiterOptions()
        {
            PermitLimit = 5,
            QueueLimit = 10000,
        });
    }

    private static NextcloudApi.Api GetNextCloudApi(string username, string password, string nextcloudUrl)
    {
        return new NextcloudApi.Api(new NextcloudApi.Settings()
        {
            ServerUri = new Uri(nextcloudUrl),
            Username = username,
            Password = password,
            ApplicationName = nameof(ElvantoSync),
            RedirectUri = new Uri(nextcloudUrl)
        });
    }
}
