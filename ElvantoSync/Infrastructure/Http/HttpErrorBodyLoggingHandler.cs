using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace ElvantoSync.Infrastructure.Http;

public sealed class HttpErrorBodyLoggingHandler(
    ILogger<HttpErrorBodyLoggingHandler> logger
) : DelegatingHandler
{
    private const int MaxLoggedBodyLength = 16 * 1024;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var response = await base.SendAsync(request, cancellationToken);

        if ((int)response.StatusCode is >= 400 and < 500)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (body.Length > MaxLoggedBodyLength)
            {
                body = body[..MaxLoggedBodyLength] + "… [truncated]";
            }

            logger.LogError(
                "HTTP {StatusCode} response for {Method} {Uri}. Content-Type: {ContentType}. Body: {Body}",
                (int)response.StatusCode,
                request.Method,
                request.RequestUri,
                response.Content.Headers.ContentType?.ToString() ?? "<none>",
                string.IsNullOrEmpty(body) ? "<empty>" : body);
        }

        return response;
    }
}

public static class HttpErrorBodyLoggingExtensions
{
    public static IServiceCollection AddHttpErrorBodyLogging(this IServiceCollection services)
    {
        services.AddTransient<HttpErrorBodyLoggingHandler>();
        services.AddTransient<IHttpMessageHandlerBuilderFilter, HttpErrorBodyLoggingHandlerBuilderFilter>();
        return services;
    }
}

internal sealed class HttpErrorBodyLoggingHandlerBuilderFilter(
    IServiceProvider services
) : IHttpMessageHandlerBuilderFilter
{
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        => builder =>
        {
            next(builder);
            // Keep this handler outermost so retry handlers finish first. This logs
            // one final 4xx response instead of one body per retry attempt.
            builder.AdditionalHandlers.Insert(0, services.GetRequiredService<HttpErrorBodyLoggingHandler>());
        };
}
