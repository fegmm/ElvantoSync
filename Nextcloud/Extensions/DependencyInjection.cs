using ElvantoSync.Infrastructure.Nextcloud;
using Microsoft.Extensions.DependencyInjection;
using Nextcloud.Clients;
using Nextcloud.Interfaces;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using WebDav;
using NextcloudApi;
namespace Nextcloud.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddNextcloud(this IServiceCollection services, string nextcloudUrl, string username, string password, string applicationName)
    {
        byte[] authToken = Encoding.UTF8.GetBytes($"{username}:{password}");
        var auth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

        services.AddHttpClient<INextcloudCircleClient, NextcloudCircleClient>(i =>
        {
            i.BaseAddress = new Uri(nextcloudUrl);
            i.DefaultRequestHeaders.Authorization = auth;
            i.DefaultRequestHeaders.UserAgent.ParseAdd(applicationName);
            i.DefaultRequestHeaders.Add("OCS-ApiRequest", "true");
            i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        CookieContainer circleCookieContainer = new CookieContainer();
        services.AddHttpClient<INextcloudCollectivesClient, NextcloudCollectivesClient>(i =>
        {
            i.BaseAddress = new Uri(nextcloudUrl);
            i.DefaultRequestHeaders.Authorization = auth;
            i.DefaultRequestHeaders.UserAgent.ParseAdd(applicationName);
            i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
        {
            CookieContainer = circleCookieContainer,
            UseCookies = true,
        });

        services.AddHttpClient<INextcloudDeckClient, NextcloudDeckClient>(i =>
        {
            i.BaseAddress = new Uri(nextcloudUrl);
            i.DefaultRequestHeaders.Authorization = auth;
            i.DefaultRequestHeaders.UserAgent.ParseAdd(applicationName);
            i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
        services.AddHttpClient<INextcloudTalkClient, NextcloudTalkClient>(i =>
        {
            i.BaseAddress = new Uri(nextcloudUrl);
            i.DefaultRequestHeaders.Authorization = auth;
            i.DefaultRequestHeaders.Add("OCS-ApiRequest", "true");
            i.DefaultRequestHeaders.UserAgent.ParseAdd(applicationName);
            i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
        services.AddHttpClient<INextcloudProvisioningClient, NextcloudProvisioningClient>(i =>
        {
            i.BaseAddress = new Uri(nextcloudUrl);
            i.DefaultRequestHeaders.Authorization = auth;
            i.DefaultRequestHeaders.Add("OCS-ApiRequest", "true");
            i.DefaultRequestHeaders.UserAgent.ParseAdd(applicationName);
            i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
        services.AddHttpClient<INextcloudGroupFolderClient, NextcloudGroupFolderClient>(i =>
       {
           i.BaseAddress = new Uri(nextcloudUrl);
           i.DefaultRequestHeaders.Authorization = auth;
           i.DefaultRequestHeaders.Add("OCS-ApiRequest", "true");
           i.DefaultRequestHeaders.UserAgent.ParseAdd(applicationName);
           i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
       });
        services.AddSingleton<WebDavClient>(getWebDav(username, password, nextcloudUrl));
        services.AddSingleton<NextcloudApi.Api>(getNextCloudApi(username, password, nextcloudUrl));
        return services;
    }

    private static WebDavClient getWebDav(string username, string password, string nextcloudUrl)
    {

        string encoded = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));

        HttpClient client = new HttpClient();
        client.BaseAddress = new Uri(nextcloudUrl);
        client.DefaultRequestHeaders.Add("OCS-APIRequest", "true");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);
        return new WebDav.WebDavClient(client);
    }

    private static NextcloudApi.Api getNextCloudApi(string username, string password, string nextcloudUrl)
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
