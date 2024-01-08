using Newtonsoft.Json;

namespace KasApi.Requests;

public class BaseRequest : IBaseRequest
{
    public string? kas_action { get; set; }
    public string? kas_login { get; set; }
    public string? kas_auth_type { get; set; }
    public string? kas_auth_data { get; set; }

    [JsonProperty(PropertyName = nameof(KasRequestParams))]
    public Dictionary<string, string>? KasRequestParams { get; set; }

    public void UpdateAuthorizationData(AuthorizeHeader auth)
    {
        this.kas_auth_data = auth.kas_auth_data;
        this.kas_auth_type = auth.kas_auth_type;
        this.kas_login = auth.kas_login;
    }
}

public class DictBaseRequest : Dictionary<string, string>
{
    [JsonIgnore]
    public string kas_action { get => this["kas_action"]; set => this["kas_action"] = value; }
    
    public DictBaseRequest(string kas_action)
    {
        this.kas_action = kas_action;
    }
}

public interface IBaseRequest
{
    string kas_action { get; set; }
    string kas_login { get; set; }
    string kas_auth_type { get; set; }
    string kas_auth_data { get; set; }
    void UpdateAuthorizationData(AuthorizeHeader auth);
}