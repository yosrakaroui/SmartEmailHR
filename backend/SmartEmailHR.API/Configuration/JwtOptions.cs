namespace SmartEmailHR.API.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "SmartEmailHR.API";
    public string Audience { get; set; } = "SmartEmailHR.Frontend";
    public int ExpirationHours { get; set; } = 8;
}

