using System.Collections.Generic;

namespace ElvantoSync.Settings.ChurchTools;

internal record PeopleToChurchToolsSyncSettings : SyncSettings
{
    internal const string ConfigSection = "Sync:ChurchTools:PeopleToChurchToolsSync";

    public string CategoryToSync { get; set; }
    public HashSet<string> ExceptFromSync { get; set; } = [];

    public ChurchToolsCustomFields ChurchToolsCustomFields { get; set; } = new();
    public ElvantoCustomFields ElvantoCustomFields { get; set; } = new();

    public string HasCodeOfConductId { get; set; } = "949cca22-248b-4950-ad4c-181a3aa10477";
    public string HasMetroCardId { get; set; } = "edb08d0c-ac17-450e-9ed1-dcc95e0745bd";
    public string HasSelfCommitmentId { get; set; } = "595291aa-14aa-4da3-affe-a349d360ba4b";

    public List<string> PrivacyApprovals { get; set; } = [
        "8542a4b1-9cce-46d8-9793-f97caf3908a1",
        "2525485c-51c8-40b8-9ad3-04f488e1549d",
        "3c2c476d-ce16-4982-a7b5-a64347251580",
        "ad896039-db26-4e96-ba24-1bc87d4050de",
        "15d06a1c-669c-463d-b08d-3aa01de1fd65",
        "3ec35044-7109-4399-89dd-ff7a3cb91907",
    ];

    public List<string> InterestToVolunteerInOptions { get; set; } = [
        "e11b6a9a-26af-48ed-9c41-318429159dd4",
        "a1223c34-6d77-4cb7-bcfc-080bdc64037e",
        "d112e4a4-478f-4836-8997-4e651a089914",
        "67ceda3c-6f14-45c4-a823-eb15f6dcc122",
        "a17fa8db-aa41-4c84-9c84-6353fa1921e3",
        "3ecf5e48-56eb-48b7-bc71-45d63a43996d",
        "c4abffae-793c-4ae0-9ca8-24844ca67370",
        "11bcb1be-7b18-4308-b749-04ead7fefd5d",
        "73190372-1530-477b-9b7a-8b3d981c5b31",
        "3fd092be-111a-41ce-8fd0-9d59a3c66c02",
        "d8c1a8d1-1e39-4228-a555-31dadb0e560d",
        "afe95c8a-5d54-4e5a-9ef2-0a3c2dfd389c",
        "24e797b0-cefb-4972-9cab-37cc4dd0f53e",
        "17121b42-aceb-4204-a3ec-be19131c42c6",
        "a8241174-28a1-4e9e-8909-e7b40fdf544c",
        "85ec8b61-b343-4a50-9d31-a0dbe6c07548",
        "8cf89284-f674-40fb-b73b-8cc9ce07b678",
        "55517993-d550-4a13-8bc9-1e27fa7446d9",
        "831c50a0-3eda-4947-ad75-61973a53577e",
        "53805041-54e2-47b0-a0c5-b4265cd8f995",
        "579d1ec1-5317-4438-bb6e-4b7e20c7e840",
        "1dc0294c-41a5-4802-9596-91ded041624e",
        "1cd68143-b37f-4ace-9ec5-f58ec05c6dcc",
        "475cabb1-a487-477e-991e-6086ed0626a7",
        "a9720b1b-dcaa-4962-82c0-31698b3facb5",
        "dbeeb5ba-4b1f-43db-ad0f-bcb20f799557",
        "fbbed1e9-5ca4-48a8-82e3-349aaf7374fd"
    ];
}

public record ElvantoCustomFields
{
    public string AdditionalPhoneNumbers { get; set; } = "64bf7c18-17d9-40ed-930d-b0aacb8a02fb";
    public string ApprovalOfPrivacyPolicy { get; set; } = "28f6f104-694e-4cd0-9601-1e6944ccf69e";
    public string CertificateOfConduct { get; set; } = "a21333b7-6aff-43c3-8787-3c7d3855af84";
    public string CodeOfConduct { get; set; } = "afe71696-4032-4784-a2aa-60c61a3873ca";
    public string DateOfApprovalOfPrivacyPolicy { get; set; } = "6f98fb6e-2b2a-4c20-a3fc-80b4ad396fac";
    public string DateOfArchiving { get; set; } = "c24db325-0280-406e-8e2c-1715b1965b67";
    public string DateOfBaptism { get; set; } = "d813c220-8091-4a95-9a72-5a6390805cfa";
    public string DateOfDeath { get; set; } = "0197e21d-3455-4809-bb8a-7b9dea71ef12";
    public string DateOfEntry { get; set; } = "c4e3eb7d-8f62-48d0-9cc5-fe0f09f83610";
    public string DateOfNonDisclosureAgreement { get; set; } = "e87c6d93-5ecb-4426-974f-2e0e2745c983";
    public string InterestToVolunteerIn { get; set; } = "1bf2def8-4cef-4ab2-9e2a-ffee42e3c05d";
    public string Job { get; set; } = "2a8b0899-47be-4fca-b6ed-ae7c751a29ac";
    public string Keyholder { get; set; } = "25f2c440-8c6b-4703-8061-4837b437d09e";
    public string MetroCard { get; set; } = "0dcd93b9-0c9f-4572-8b02-0f4eb3409809";
    public string NoteOnVolunteering { get; set; } = "445b1f1c-7347-4b23-a332-1067ae5e68df";
    public string SelfCommitment { get; set; } = "f039d5b9-4dc1-48d5-930f-16cb6bbd6dfd";
    public string Title { get; set; } = "5411248a-ad21-4d8b-bcdf-a657973958ff";
}

public record ChurchToolsCustomFields
{
    public string AdditionalPhoneNumbers { get; set; } = "sonstige_telefonnummer";
    public string ApprovalOfPrivacyPolicy { get; set; } = "datenschutzverordnung_genehmig";
    public string CertificateOfConduct { get; set; } = "f_hrungszeugnis";
    public string CodeOfConduct { get; set; } = "verhaltenskodex_unterschrieben";
    public string DateOfArchiving { get; set; } = "datum_der_archivierung";
    public string DateOfNonDisclosureAgreement { get; set; } = "vertraulichkeitsverpflichtung_";
    public string InterestToVolunteerIn { get; set; } = "in_diesem_bereich_k_nnte_ich_m";
    public string Keyholder { get; set; } = "schl_sselbesitz";
    public string MetroCard { get; set; } = "metro_karte";
    public string NoteOnVolunteering { get; set; } = "bemerkung_zur_mitarbeit";
    public string SelfCommitment { get; set; } = "selbstverpflichtung_unterschri";
}