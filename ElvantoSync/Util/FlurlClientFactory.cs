using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ElvantoSync.ElvantoApi;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;

namespace ElvantoSync.Util
{
    public class FlurlClientFactory
    {
        private readonly ILogger _logger;
        private readonly FlurlClient _client;

        public FlurlClientFactory(ILogger logger)
        {
            _logger = logger;
            _client = new FlurlClient(HttpClientWithCookiePersistence());
            ConfigureClient(_client);
        }

        public FlurlClient GetClient()
        {
            return _client;
        }

        private void ConfigureClient(FlurlClient client)
        {
            ConfigureFlurlLog(client);

        }

        private static HttpClient HttpClientWithCookiePersistence()
        {
            CookieContainer cookies = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                UseCookies = true
            };
            return new HttpClient(handler);

        }

        private void ConfigureFlurlLog(FlurlClient client)
        {

            client.BeforeCall(async call =>
            {
                _logger.LogInformation($"Request: {call.Request.Verb} {call.Request.Url}");
                _logger.LogInformation($"Headers: {String.Join("; ", call.Request.Headers)}");
                if (call.HttpRequestMessage.Content != null)
                {
                    var requestBody = await call.HttpRequestMessage.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Request Body: {requestBody}");
                }
            });

            client.AfterCall(async call =>
            {
                _logger.LogInformation($"Response received from {call.Request.Url} - Status: {call.Response.StatusCode}");
                _logger.LogInformation($"Response Headers: {String.Join("; ", call.Response.Headers)}");

                if (call.Response.ResponseMessage.Content != null)
                {
                    var responseBody = await call.Response.ResponseMessage.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Response Body: {responseBody}");
                }

            });

            client.OnError(async call =>
            {
                _logger.LogError($"Error occurred during request to {call.Request.Url}: {call.Exception}");
                if (call.Response != null)
                {
                    _logger.LogError($"Response Status: {call.Response.StatusCode}");
                    try
                    {
                        var errorBody = await call.Response.ResponseMessage.Content.ReadAsStringAsync();
                        _logger.LogError($"Error Body: {errorBody}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error reading response body: {ex}", ex);
                    }
                }
            });
        }
    }

}