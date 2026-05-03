using SmartEmailHR.API.Models;

namespace SmartEmailHR.API.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}

