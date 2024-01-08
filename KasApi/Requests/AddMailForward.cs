using System.Text.Json.Serialization;

namespace KasApi.Requests;

public class AddMailForward : DictBaseRequest
{
    [JsonIgnore]
    public string LocalPart { get => this["local_part"]; set => this["local_part"] = value; }

    [JsonIgnore]
    public string DomainPart { get => this["domain_part"]; set => this["domain_part"] = value; }

    [JsonIgnore]
    public string[] Targets
    {
        get => this.Where(i => i.Key.StartsWith("target_")).Select(i => i.Value).ToArray();
        set
        {
            foreach (var item in this.Where(i => i.Key.StartsWith("target_")).Select(i => i.Key))
                Remove(item);

            var unique = value.Distinct().ToArray();
            for (var i = 0; i < unique.Length; i++)
                this["target_" + i] = unique[i];
        }
    }

    public AddMailForward() : base("add_mailforward") { }
}
