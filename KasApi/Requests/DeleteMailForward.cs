using Newtonsoft.Json;

namespace KasApi.Requests;

public class DeleteMailForward : DictBaseRequest
{
    [JsonIgnore]
    public string MailForward { get => this["mail_forward"]; set => this["mail_forward"] = value; }

    public DeleteMailForward() : base("delete_mailforward") { }
}
