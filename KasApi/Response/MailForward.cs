namespace KasApi.Response;

public class MailForward
{
    public string MailForwardAdress { get; set; }
    public string MailForwardComment { get; set; }
    public string[] MailForwardTargets { get; set; }
    public string[] MailForwardSpamfilter { get; set; }
    public bool InProgress { get; set; }

    public MailForward(Dictionary<string, object> xml_item)
    {
        MailForwardAdress = (string)xml_item["mail_forward_adress"];
        MailForwardComment = (string)xml_item["mail_forward_comment"];
        MailForwardTargets = ((string)xml_item["mail_forward_targets"]).Split(',');
        MailForwardSpamfilter = ((string)xml_item["mail_forward_spamfilter"]).Split(',');
        InProgress = bool.Parse((string)xml_item["in_progress"]);
    }
}
