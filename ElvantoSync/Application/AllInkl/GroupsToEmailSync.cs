using ElvantoSync.ElvantoService;
using ElvantoSync.Extensions;
using ElvantoSync.Persistence;
using ElvantoSync.Settings.AllInkl;
using Fegmm.Elvanto.Groups.GetAllJson;
using Fegmm.Elvanto.Models;
using KasApi.Requests;
using KasApi.Response;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElvantoSync.AllInkl;

internal class GroupsToEmailSync(
    IElvantoClient elvanto,
    NextcloudApi.Api nextcloud,
    KasApi.IKasClient kas,
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
        (await elvanto.GroupsGetAllAsync(new() { Fields = [GroupAdditionalFields.People] }))
            .Where(i => i.People?.Person.Any() ?? false);

    public override async Task<IEnumerable<MailForward>> GetToAsync()
        => (await kas.GetMailforwardsAsync()).Where(i => i.MailForwardAdress.Split("@")[1] == settings.Value.KASDomain);

    protected override async Task UpdateMatch(Group group, MailForward mail)
    {
        var compare = group.People.Person
            .DistinctBy(i => i.Email)
            .CompareTo(mail.MailForwardTargets, i => i.Email, j => j);

        if (compare.additional.Any() || compare.missing.Any())
        {
            var mails = group.People.Person.Select(i => i.Email)
                .Distinct()
                .Where(i => !string.IsNullOrEmpty(i))
                .ToArray();
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
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Content().Element(container =>
                {
                    container.Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(8, Unit.Centimetre);
                            columns.ConstantColumn(10, Unit.Centimetre);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Gruppe").Bold().Underline().AlignCenter();
                            header.Cell().Text("Mail").Bold().Underline().AlignCenter();
                        });

                        foreach (var item in matches.OrderBy(i => i.Group.Name))
                        {
                            table.Cell().Text(item.Group.Name).AlignLeft();
                            table.Cell().Hyperlink($"mailto:{item.MailForward.MailForwardAdress}").Text(item.MailForward.MailForwardAdress).Italic().AlignRight();
                        }
                    });
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
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
