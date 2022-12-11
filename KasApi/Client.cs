using KasApi.Requests;
using KasApi.Response;
using ServiceReference1;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace KasApi
{
    public class Client
    {
        private readonly KasApiPortTypeClient client;
        private readonly AuthorizeHeader authorization;

        private Dictionary<string, DateTime> next_call_possible;
        private ConcurrentDictionary<string,  SemaphoreSlim> semaphores;

        public Client(AuthorizeHeader authorization)
        {
            this.client = new ServiceReference1.KasApiPortTypeClient(KasApiPortTypeClient.EndpointConfiguration.KasApiPort);
            this.authorization = authorization;
            this.next_call_possible = new Dictionary<string, DateTime>();
            this.semaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
        }

        private async Task<object?> ExecuteRequestAsync(IBaseRequest request)
        {
            request.UpdateAuthorizationData(this.authorization);

            await this.semaphores.GetOrAdd(request.kas_action, new SemaphoreSlim(1,1)).WaitAsync();

            try
            {
                var wait_time = this.next_call_possible.GetValueOrDefault(request.kas_action, DateTime.Now) - DateTime.Now;
                if (wait_time.TotalMilliseconds > 0)
                    await Task.Delay(wait_time);

                object? parse_xml_to_objects(XElement i, bool sub_value = true)
                {
                    var reference = (sub_value ? i.Element("value") : i);
                    var type = reference?.Attributes().First(i => i.Name.LocalName == "type").Value;

                    if (type == null || reference == null) return null;

                    return type switch
                    {
                        "ns2:Map" => reference.Elements("item").ToDictionary(i => i.Element("key").Value, i => parse_xml_to_objects(i)),
                        "xsd:string" => reference.Value,
                        "xsd:int" => reference.Value,
                        "SOAP-ENC:Array" => reference.Elements("item").Select(i => parse_xml_to_objects(i, sub_value = false)).ToArray(),
                        _ => throw new NotImplementedException()
                    };
                }

                var request_json = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                var response = await this.client.KasApiAsync(request_json);
                var response_xml = response.Return as XmlNode[];
                if (response_xml == null) return null;

                var response_doc = XDocument.Load(new XmlNodeReader(response_xml[response_xml.Length - 1]));

                XElement? item = response_doc.Element("item");
                if (item == null) return null;
                var result = parse_xml_to_objects(item) as Dictionary<string, object>;
                if (result == null) return null;
                this.next_call_possible[request.kas_action] = DateTime.Now.AddSeconds(int.Parse((string)result.GetValueOrDefault("KasFloodDelay", "0")));
                return result["ReturnInfo"];
            }
            catch (Exception)
            {
                throw;
            }
            finally {
                this.semaphores[request.kas_action].Release();
            }
        }

        public async Task<MailForward[]> GetMailforwardsAsync()
        {
            var request = new BaseRequest() { kas_action = "get_mailforwards"};
            var result =  await this.ExecuteRequestAsync(request);
            var result_array = result as object[];
            var result_dicts = result_array?.Cast<Dictionary<string, object>>();
            if (result_dicts == null)
                return Array.Empty<MailForward>();
            return result_dicts.Select(i => new MailForward(i)).ToArray();
        }

        public async Task ExecuteRequestWithParams(Dictionary<string, string> parameters)
        {
            parameters.Remove("kas_action", out string? kas_action);
            var request = new BaseRequest() { kas_action = kas_action, KasRequestParams = parameters};
            var result = await this.ExecuteRequestAsync(request);
        }
    }
}
