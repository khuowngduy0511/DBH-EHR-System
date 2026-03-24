using System.Security.Claims;

namespace DBH.Auth.Service.Services;

public interface ITokenService
{
    string GenerateToken(Guid userId, string email, string fullName, string organizationId, IEnumerable<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
