using System.Text.Json.Serialization;

namespace KasApi.Requests;
public class UpdateMailForward : DictBaseRequest
{
    [JsonIgnore]
    public string MailForward { get => this["mail_forward"]; set => this["mail_forward"] = value; }

    [JsonIgnore]
    public string[] Targets
    {
        get => this.Where(i => i.Key.StartsWith("target_")).Select(i => i.Value).ToArray();
        set
        {
            foreach (var item in this.Where(i => i.Key.StartsWith("target_")).Select(i => i.Key))
                Remove(item);
            var unique = value.Distinct().ToArray();
            for (int i = 0; i < unique.Length; i++)
                this["target_" + i] = unique[i];
        }
    }

    public UpdateMailForward() : base("update_mailforward") { }
}
