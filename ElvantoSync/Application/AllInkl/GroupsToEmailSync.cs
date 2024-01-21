using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.Extensions;
using ElvantoSync.Settings.AllInkl;
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

internal class GroupsToEmailSync(ElvantoApi.Client elvanto, NextcloudApi.Api nextcloud, KasApi.Client kas, GroupsToEmailSyncSettings settings) : Sync<Group, MailForward>(settings)
{
    public override string FromKeySelector(Group i) => SanitizeName(i.Name);
    public override string ToKeySelector(MailForward i) => SanitizeName(i.MailForwardAdress.Split("@")[0]);


    public override async Task<IEnumerable<Group>> GetFromAsync()
    {
        var from = (await elvanto.GroupsGetAllAsync(new GetAllRequest() { Fields = ["people"] })).Groups.Group
            .Where(i => i.People?.Person != null && i.People.Person.Any());

        await CreateAndUploadPdf(nextcloud, from);

        return from;
    }

    public async override Task ApplyUpdate(IEnumerable<(Group, MailForward)> matches)
    {
        var requests = matches.Select(async match =>
        {
            (Group group, MailForward mail) = match;
            var compare = group.People.Person.CompareTo(mail.MailForwardTargets, i => i.Email, j => j);
            var mails = group.People.Person.Select(i => i.Email).Distinct().Where(i => string.IsNullOrEmpty(i)).ToArray();
            if (compare.additional.Any() || compare.missing.Any())
            {
                await kas.ExecuteRequestWithParams(new UpdateMailForward() { MailForward = mail.MailForwardAdress, Targets = mails });
            }
        });
        await Task.WhenAll(requests);
    }

    private async Task CreateAndUploadPdf(NextcloudApi.Api nextcloud, IEnumerable<Group> from)
    {
        if (settings.UploadGroupMailAddressesToNextcloudPath != null)
        {
            var path = settings.UploadGroupMailAddressesToNextcloudPath;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Document document = new Document();
            Section section = document.AddSection();
            var table = section.AddTable();
            table.AddColumn("8cm");
            table.AddColumn("10cm");
            foreach (var item in from.OrderBy(i => i.Name))
            {
                string sanatizedName = SanitizeName(item.Name);
                var row = table.AddRow();
                row.Cells[0].AddParagraph(item.Name);
                var mail_link = row.Cells[1].AddParagraph().AddHyperlink($"mailto:{sanatizedName}@{settings.KASDomain}", HyperlinkType.Url);
                mail_link.AddFormattedText($"{sanatizedName}@{settings.KASDomain}");
            }

            PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(false);
            pdfRenderer.Document = document;
            pdfRenderer.RenderDocument();

            using var stream = new MemoryStream();
            pdfRenderer.Save(stream, false);

            await NextcloudApi.CloudFile.Upload(nextcloud, $"{nextcloud.Settings.Username}/{path}", stream);
        }
    }

    public override async Task<IEnumerable<MailForward>> GetToAsync()
        => (await kas.GetMailforwardsAsync()).Where(i => i.MailForwardAdress.Split("@")[1] == settings.KASDomain);

    public override async Task AddMissingAsync(IEnumerable<Group> missing)
    {
        var tasks = missing
            .Select(i => kas.ExecuteRequestWithParams(new AddMailForward()
            {
                LocalPart = SanitizeName(i.Name),
                DomainPart = settings.KASDomain,
                Targets = i.People.Person
                    .Select(i => i.Email)
                    .Distinct()
                    .Where(i => !string.IsNullOrEmpty(i))
                    .ToArray()
            }));
        await Task.WhenAll(tasks);
    }

    public override async Task RemoveAdditionalAsync(IEnumerable<MailForward> additionals)
    {
        var tasks = additionals
            .Select(i => kas.ExecuteRequestWithParams(new DeleteMailForward() { MailForward = i.MailForwardAdress }));
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
