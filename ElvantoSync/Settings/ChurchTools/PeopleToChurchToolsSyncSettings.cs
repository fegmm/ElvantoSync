using System.Collections.Generic;

namespace ElvantoSync.Settings.ChurchTools;

internal record PeopleToChurchToolsSyncSettings : SyncSettings
{
    internal const string ConfigSection = "Sync:ChurchTools:PeopleToChurchToolsSync";

    public int DefaultDepartment { get; set; } = 4;
    public Dictionary<string, int> Departments { get; set; } = new()
    {
        ["d5f23ea9-dae5-4720-ae10-7cfa8e413ed9"] = 1, // Gemeindemitglied
        ["0fed29fd-093c-49ee-8f48-e1a91c77fe1f"] = 5, // Im Aufnahmeprozess
        ["02ffab28-6ecc-4131-a72b-6bca7b10f65c"] = 1, // Außendienstler
        ["d9d5f5ce-439d-4141-bc22-ab3869da6842"] = 5, // Pausierte Mitgliedschaft
        ["00b91199-bbf7-4b14-a5bf-0ee407a90691"] = 5, // Nichtmitglied (Mitarbeiter oder Hauskreisbesucher)
        ["7e8d2b76-3883-418a-aa8a-06e4f9e50bda"] = 7, // Kinder
        ["0a6f2a16-0d7d-4549-a4d9-6bb954932da9"] = 6, // Externe Raumnutzer
    };

    public int DefaultStatusId { get; set; } = 0;
    public Dictionary<string, int> Status { get; set; } = new()
    {
        ["d5f23ea9-dae5-4720-ae10-7cfa8e413ed9"] = 3, // Gemeindemitglied
        ["0fed29fd-093c-49ee-8f48-e1a91c77fe1f"] = 5, // Im Aufnahmeprozess
        ["02ffab28-6ecc-4131-a72b-6bca7b10f65c"] = 6, // Außendienstler
        ["d9d5f5ce-439d-4141-bc22-ab3869da6842"] = 7, // Pausierte Mitgliedschaft
        ["00b91199-bbf7-4b14-a5bf-0ee407a90691"] = 1, // Nichtmitglied (Mitarbeiter oder Hauskreisbesucher)
        ["7e8d2b76-3883-418a-aa8a-06e4f9e50bda"] = 8, // Kinder
        ["0a6f2a16-0d7d-4549-a4d9-6bb954932da9"] = 2, // Externe Raumnutzer
    };

    public HashSet<string> ExceptFromSync { get; set; } = [];

    public ChurchToolsCustomFields ChurchToolsCustomFields { get; set; } = new();
    public ElvantoCustomFields ElvantoCustomFields { get; set; } = new();

    public string HasCodeOfConductId { get; set; } = "949cca22-248b-4950-ad4c-181a3aa10477";
    public string HasMetroCardId { get; set; } = "edb08d0c-ac17-450e-9ed1-dcc95e0745bd";
    public string HasSelfCommitmentId { get; set; } = "595291aa-14aa-4da3-affe-a349d360ba4b";

    public Dictionary<string, int> PrivacyApprovals { get; set; } = new() {
        ["8542a4b1-9cce-46d8-9793-f97caf3908a1"] = 1,
        ["2525485c-51c8-40b8-9ad3-04f488e1549d"] = 2,
        ["3c2c476d-ce16-4982-a7b5-a64347251580"] = 3,
        ["ad896039-db26-4e96-ba24-1bc87d4050de"] = 4,
        ["15d06a1c-669c-463d-b08d-3aa01de1fd65"] = 5,
        ["3ec35044-7109-4399-89dd-ff7a3cb91907"] = 6,
    };

    public Dictionary<string, int> InterestToVolunteerInOptions { get; set; } = new()
    {
        ["e11b6a9a-26af-48ed-9c41-318429159dd4"] = 1,
        ["a1223c34-6d77-4cb7-bcfc-080bdc64037e"] = 2,
        ["d112e4a4-478f-4836-8997-4e651a089914"] = 3,
        ["67ceda3c-6f14-45c4-a823-eb15f6dcc122"] = 4,
        ["a17fa8db-aa41-4c84-9c84-6353fa1921e3"] = 5,
        ["3ecf5e48-56eb-48b7-bc71-45d63a43996d"] = 6,
        ["c4abffae-793c-4ae0-9ca8-24844ca67370"] = 7,
        ["11bcb1be-7b18-4308-b749-04ead7fefd5d"] = 8,
        ["73190372-1530-477b-9b7a-8b3d981c5b31"] = 9,
        ["3fd092be-111a-41ce-8fd0-9d59a3c66c02"] = 10,
        ["d8c1a8d1-1e39-4228-a555-31dadb0e560d"] = 11,
        ["afe95c8a-5d54-4e5a-9ef2-0a3c2dfd389c"] = 12,
        ["24e797b0-cefb-4972-9cab-37cc4dd0f53e"] = 13,
        ["17121b42-aceb-4204-a3ec-be19131c42c6"] = 14,
        ["a8241174-28a1-4e9e-8909-e7b40fdf544c"] = 15,
        ["85ec8b61-b343-4a50-9d31-a0dbe6c07548"] = 16,
        ["8cf89284-f674-40fb-b73b-8cc9ce07b678"] = 17,
        ["55517993-d550-4a13-8bc9-1e27fa7446d9"] = 18,
        ["831c50a0-3eda-4947-ad75-61973a53577e"] = 19,
        ["53805041-54e2-47b0-a0c5-b4265cd8f995"] = 20,
        ["579d1ec1-5317-4438-bb6e-4b7e20c7e840"] = 21,
        ["1dc0294c-41a5-4802-9596-91ded041624e"] = 22,
        ["1cd68143-b37f-4ace-9ec5-f58ec05c6dcc"] = 23,
        ["475cabb1-a487-477e-991e-6086ed0626a7"] = 24,
        ["a9720b1b-dcaa-4962-82c0-31698b3facb5"] = 25,
        ["dbeeb5ba-4b1f-43db-ad0f-bcb20f799557"] = 26,
        ["fbbed1e9-5ca4-48a8-82e3-349aaf7374fd"] = 27
    };
}

public record ElvantoCustomFields
{
    public string AdditionalPhoneNumbers { get; set; } = "custom_64bf7c18-17d9-40ed-930d-b0aacb8a02fb";
    public string ApprovalOfPrivacyPolicy { get; set; } = "custom_28f6f104-694e-4cd0-9601-1e6944ccf69e";
    public string CertificateOfConduct { get; set; } = "custom_a21333b7-6aff-43c3-8787-3c7d3855af84";
    public string CodeOfConduct { get; set; } = "custom_afe71696-4032-4784-a2aa-60c61a3873ca";
    public string DateOfApprovalOfPrivacyPolicy { get; set; } = "custom_6f98fb6e-2b2a-4c20-a3fc-80b4ad396fac";
    public string DateOfArchiving { get; set; } = "custom_c24db325-0280-406e-8e2c-1715b1965b67";
    public string DateOfBaptism { get; set; } = "custom_d813c220-8091-4a95-9a72-5a6390805cfa";
    public string DateOfDeath { get; set; } = "custom_0197e21d-3455-4809-bb8a-7b9dea71ef12";
    public string DateOfEntry { get; set; } = "custom_c4e3eb7d-8f62-48d0-9cc5-fe0f09f83610";
    public string DateOfNonDisclosureAgreement { get; set; } = "custom_e87c6d93-5ecb-4426-974f-2e0e2745c983";
    public string InterestToVolunteerIn { get; set; } = "custom_1bf2def8-4cef-4ab2-9e2a-ffee42e3c05d";
    public string Job { get; set; } = "custom_2a8b0899-47be-4fca-b6ed-ae7c751a29ac";
    public string Keyholder { get; set; } = "custom_25f2c440-8c6b-4703-8061-4837b437d09e";
    public string MetroCard { get; set; } = "custom_0dcd93b9-0c9f-4572-8b02-0f4eb3409809";
    public string NoteOnVolunteering { get; set; } = "custom_445b1f1c-7347-4b23-a332-1067ae5e68df";
    public string SelfCommitment { get; set; } = "custom_f039d5b9-4dc1-48d5-930f-16cb6bbd6dfd";
    public string Title { get; set; } = "custom_5411248a-ad21-4d8b-bcdf-a657973958ff";
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