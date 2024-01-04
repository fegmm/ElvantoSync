using ElvantoSync.ElvantoApi.Models;
using KasApi.Requests;
using KasApi.Response;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElvantoSync.AllInkl;

internal class GroupsToEmailSync(ElvantoApi.Client elvanto, NextcloudApi.Api nextcloud, KasApi.Client kas, Settings settings) : Sync<string, Group, MailForward>(settings)
{
    public override async Task<Dictionary<string, Group>> GetFromAsync()
    {
        var from = (await elvanto.GroupsGetAllAsync(new GetAllRequest() { Fields = new[] { "people" } })).Groups.Group
            .Where(i => i.People?.Person != null && i.People.Person.Any())
            .ToDictionary(i => SanitizeName(i.Name), i => i);

        if (Settings.UploadGroupMailAddressesToNextcloudPath != null)
        {
            var path = Settings.UploadGroupMailAddressesToNextcloudPath;

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
                var mail_link = row.Cells[1].AddParagraph().AddHyperlink($"mailto:{item.Key}@{Settings.KASDomain}", HyperlinkType.Url);
                mail_link.AddFormattedText($"{item.Key}@{Settings.KASDomain}");
            }

            PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(false);
            pdfRenderer.Document = document;
            pdfRenderer.RenderDocument();

            using var stream = new MemoryStream();
            pdfRenderer.Save(stream, false);

            await NextcloudApi.CloudFile.Upload(nextcloud, $"{nextcloud.Settings.Username}/{path}", stream);
        }

        return from;
    }

    public override async Task<Dictionary<string, MailForward>> GetToAsync()
    {
        return (await kas.GetMailforwardsAsync())
            .Where(i => i.MailForwardAdress.Split("@")[1] == Settings.KASDomain)
            .ToDictionary(i => SanitizeName(i.MailForwardAdress.Split("@")[0]), i => i);
    }

    public override async Task AddMissingAsync(Dictionary<string, Group> missing)
    {
        var tasks = missing
            .Select(i => kas.ExecuteRequestWithParams(new AddMailForward() { LocalPart = i.Key, DomainPart = Settings.KASDomain, Targets = new[] { "technik@fegmm.de" } }));
        await Task.WhenAll(tasks);
    }

    public override async Task RemoveAdditionalAsync(Dictionary<string, MailForward> additionals)
    {
        var tasks = additionals
            .Select(i => kas.ExecuteRequestWithParams(new DeleteMailForward() { MailForward = $"{i.Key}@{Settings.KASDomain}" }));
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
