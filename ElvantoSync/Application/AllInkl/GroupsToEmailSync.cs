using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.Extensions;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.AllInkl;
using KasApi.Requests;
using KasApi.Response;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElvantoSync.AllInkl;

internal class GroupsToEmailSync(
    IElvantoClient elvanto,
    NextcloudApi.Api nextcloud,
    KasApi.Client kas,
    DbContext dbContext,
    IOptions<GroupsToEmailSyncSettings> settings,
    ILogger<GroupsToEmailSync> logger
) : Sync<Group, MailForward>(dbContext, settings, logger)
{
    public override string FromKeySelector(Group i) => i.Name;
    public override string ToKeySelector(MailForward i) => i.MailForwardAdress;
    public override string FallbackFromKeySelector(Group i) => SanitizeName(i.Name);
    public override string FallbackToKeySelector(MailForward i) => i.MailForwardAdress.Split('@')[0];

    public override async Task<IEnumerable<Group>> GetFromAsync() =>
        (await elvanto.GroupsGetAllAsync(new GetAllRequest() { Fields = ["people"] })).Groups.Group
            .Where(i => i.People?.Person.Any() ?? false);

    public override async Task<IEnumerable<MailForward>> GetToAsync()
        => (await kas.GetMailforwardsAsync()).Where(i => i.MailForwardAdress.Split("@")[1] == settings.Value.KASDomain);

    protected override async Task UpdateMatch(Group group, MailForward mail)
    {
        var compare = group.People.Person.CompareTo(mail.MailForwardTargets, i => i.Email, j => j);
        var mails = group.People.Person.Select(i => i.Email).Distinct().Where(i => string.IsNullOrEmpty(i)).ToArray();
        if (compare.additional.Any() || compare.missing.Any())
        {
            await kas.ExecuteRequestWithParams(new UpdateMailForward() { MailForward = mail.MailForwardAdress, Targets = mails });
        }
    }

    public override async Task UpdateMatches(IEnumerable<(Group, MailForward)> matches)
    {
        await base.UpdateMatches(matches);
        await CreateAndUploadPdf(matches);
    }

    private async Task CreateAndUploadPdf(IEnumerable<(Group Group, MailForward MailForward)> matches)
    {
        var path = settings.Value.UploadGroupMailAddressesToNextcloudPath;
        if (string.IsNullOrEmpty(path))
        {
            return;
        }


        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Document document = new Document();
        Section section = document.AddSection();
        var table = section.AddTable();
        table.AddColumn("8cm");
        table.AddColumn("10cm");
        foreach (var item in matches.OrderBy(i => i.Group.Name))
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(item.Group.Name);
            var mail_link = row.Cells[1].AddParagraph().AddHyperlink($"mailto:{item.MailForward.MailForwardAdress}", HyperlinkType.Url);
            mail_link.AddFormattedText(item.MailForward.MailForwardAdress);
        }

        PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(false);
        pdfRenderer.Document = document;
        pdfRenderer.RenderDocument();

        using var stream = new MemoryStream();
        pdfRenderer.Save(stream, false);

        await NextcloudApi.CloudFile.Upload(nextcloud, $"{nextcloud.Settings.Username}/{path}", stream);
    }

    protected async override Task<string> AddMissing(Group group)
    {
        string sanitizedGroupName = SanitizeName(group.Name);
        string[] targets = group.People.Person
                            .Select(i => i.Email)
                            .Distinct()
                            .Where(i => !string.IsNullOrEmpty(i))
                            .ToArray();

        await kas.ExecuteRequestWithParams(new AddMailForward()
        {
            LocalPart = sanitizedGroupName,
            DomainPart = settings.Value.KASDomain,
            Targets = targets
        });

        return $"{sanitizedGroupName}@{settings.Value.KASDomain}";
    }

    protected override async Task RemoveAdditional(MailForward forward)
        => await kas.ExecuteRequestWithParams(new DeleteMailForward() { MailForward = forward.MailForwardAdress });

    private static string SanitizeName(string name)
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
