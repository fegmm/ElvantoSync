using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KasApi;
using KasApi.Requests;
using ElvantoSync.ElvantoApi.Models;
using KasApi.Response;
using System.IO;
using System.Text;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

namespace ElvantoSync.AllInkl
{
    internal class GroupsToEmailSync : Sync<string, Group, MailForward>
    {
        private readonly ElvantoApi.Client elvanto;
        private readonly Client kas;
        private readonly string domain;
        private readonly NextcloudApi.Api nextcloud;

        public GroupsToEmailSync(ElvantoApi.Client elvanto, KasApi.Client kas, string domain, NextcloudApi.Api nextcloud)
        {
            this.elvanto = elvanto;
            this.kas = kas;
            this.domain = domain;
            this.nextcloud = nextcloud;
        }

        public override async Task<Dictionary<string, Group>> GetFromAsync()
        {
            var from =  (await elvanto.GroupsGetAllAsync(new GetAllRequest() { Fields = new[] { "people" } })).Groups.Group
                .Where(i => i.People?.Person != null && i.People.Person.Any())
                .ToDictionary(i => SanitizeName(i.Name), i => i);

            if (Program.settings.UploadGroupMailAddressesToNextcloudPath != null)
            {
                var path = Program.settings.UploadGroupMailAddressesToNextcloudPath;
                var file_content = String.Join('\n', from.OrderBy(i => i.Value.Name).Select((item) => $"{item.Value.Name} => {item.Key}@{this.domain}"));

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Document document = new Document();
                Section section = document.AddSection();
                var table = section.AddTable();
                table.AddColumn("8cm");
                table.AddColumn("10cm");
                foreach (var item in from.OrderBy(i => i.Value.Name))
                {
                    var row = table.AddRow();
                    row.Cells[0].AddParagraph(item.Value.Name);
                    row.Cells[1].AddParagraph($"{item.Key}@{this.domain}");
                }

                PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(false);
                pdfRenderer.Document = document;
                pdfRenderer.RenderDocument();

                using var stream = new MemoryStream();
                pdfRenderer.Save(stream, false);

                await NextcloudApi.CloudFile.Upload(this.nextcloud, $"{this.nextcloud.Settings.Username}/{path}", stream);
            }

            return from;
        }

        public override async Task<Dictionary<string, MailForward>> GetToAsync()
        {
            return (await kas.GetMailforwardsAsync())
                .Where(i => i.MailForwardAdress.Split("@")[1] == this.domain)
                .ToDictionary(i => SanitizeName(i.MailForwardAdress.Split("@")[0]), i => i);
        }

        public override async Task AddMissingAsync(Dictionary<string, Group> missing)
        {
            var tasks = missing
                .Select(i => kas.ExecuteRequestWithParams(new AddMailForward() { LocalPart = i.Key, DomainPart = domain, Targets = new[] { "technik@fegmm.de" } }));
            await Task.WhenAll(tasks);
        }

        public override async Task RemoveAdditionalAsync(Dictionary<string, MailForward> additionals)
        {
            var tasks = additionals
                .Select(i => kas.ExecuteRequestWithParams(new DeleteMailForward() { MailForward = $"{i.Key}@{domain}" }));
            await Task.WhenAll(tasks);
        }

        internal static string SanitizeName(string name)
        {
            name = ReplaceSpecialCharacters(name);
            name = name.Replace(" ", "-").Trim('-', '.');
            name = ReplaceDoubleMinusRecursivly(name);
            name = name.ToLower();

            return name;

            static string ReplaceSpecialCharacters(string name)
            {
                var replace = new Dictionary<string, string>()
                {
                    {"ä", "ae"},
                    {"ö", "oe"},
                    {"ü", "ue"},
                    {"Ä", "Ae"},
                    {"Ö", "Oe"},
                    {"Ü", "Ue"},
                    {"ß", "ss" },
                    {"/", "-und-" },
                    {"&", "-und-" },
                    {"+", "-und-" },
                    {",", "-" },
                    {"(", "-" },
                    {")", "-" },
                    {"\"", "" },
                };

                foreach (var item in replace)
                    name = name.Replace(item.Key, item.Value);
                return name;
            }

            static string ReplaceDoubleMinusRecursivly(string name)
            {
                var old_length = 0;
                while (name.Length != old_length)
                {
                    old_length = name.Length;
                    name = name.Replace("--", "-");
                }
                return name;
            }
        }
    }
}
