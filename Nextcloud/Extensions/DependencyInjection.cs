using ElvantoSync.Infrastructure.Nextcloud;
using Microsoft.Extensions.DependencyInjection;
using Nextcloud.Clients;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Nextcloud.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddNextcloud(this IServiceCollection services, string nextcloudUrl, string username, string password, string applicationName)
    {
        byte[] authToken = Encoding.UTF8.GetBytes($"{username}:{password}");
        var auth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

        services.AddHttpClient<NextcloudCircleClient>(i =>
        {
            i.BaseAddress = new Uri(nextcloudUrl);
            i.DefaultRequestHeaders.Authorization = auth;
            i.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(applicationName));
            i.DefaultRequestHeaders.Add("OCS-ApiRequest", "true");
            i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        CookieContainer circleCookieContainer = new CookieContainer();
        services.AddHttpClient<NextcloudCollectivesClient>(i =>
        {
            i.BaseAddress = new Uri(nextcloudUrl);
            i.DefaultRequestHeaders.Authorization = auth;
            i.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(applicationName));
            i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
        {
            CookieContainer = circleCookieContainer,
            UseCookies = true,
        });

        services.AddHttpClient<NextcloudDeckClient>(i =>
        {
            i.BaseAddress = new Uri(nextcloudUrl);
            i.DefaultRequestHeaders.Authorization = auth;
            i.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(applicationName));
            i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        return services;
    }
}
