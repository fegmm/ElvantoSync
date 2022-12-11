using KasApi.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KasApi.Requests
{
    public class DeleteMailForward : DictBaseRequest
    {
        [JsonIgnore]
        public string MailForward { get => this["mail_forward"]; set => this["mail_forward"] = value; }

        public DeleteMailForward() : base("delete_mailforward") { }
    }
}
