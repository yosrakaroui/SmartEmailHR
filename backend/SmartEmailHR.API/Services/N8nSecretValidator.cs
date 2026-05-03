using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SmartEmailHR.API.Configuration;

namespace SmartEmailHR.API.Services;

public sealed class N8nSecretValidator : IN8nSecretValidator
{
    private readonly N8nOptions _options;

    public N8nSecretValidator(IOptions<N8nOptions> options)
    {
        _options = options.Value;
    }

    public bool IsValid(string? providedSecret)
    {
        if (string.IsNullOrWhiteSpace(_options.SharedSecret))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(providedSecret))
        {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(_options.SharedSecret.Trim());
        var actualBytes = Encoding.UTF8.GetBytes(providedSecret.Trim());
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}

