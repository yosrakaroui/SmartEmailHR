namespace SmartEmailHR.API.Services;

public interface IN8nSecretValidator
{
    bool IsValid(string? providedSecret);
}

