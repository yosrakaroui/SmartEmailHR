namespace SmartEmailHR.API.Configuration;

public sealed class N8nOptions
{
    public const string SectionName = "N8n";

    public string SharedSecret { get; set; } = string.Empty;
    public string AcceptationWebhookUrl { get; set; } = string.Empty;
    public string RefusWebhookUrl { get; set; } = string.Empty;
    public string ErrorWebhookUrl { get; set; } = string.Empty;
}

