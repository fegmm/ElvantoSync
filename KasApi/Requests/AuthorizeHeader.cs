using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KasApi.Requests
{
    public record AuthorizeHeader
    {
        public string? kas_login {get;set;}
        public string? kas_auth_type {get;set;}
        public string? kas_auth_data { get; set; }
    };
}
