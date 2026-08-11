using Microsoft.AspNetCore.Http;
using MRBSystem.Contracts.Services;
using System.Security.Claims;

namespace MRBSystem.Application.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var idClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idClaim))
                throw new UnauthorizedAccessException("User is not authenticated.");

            return Guid.Parse(idClaim);
        }
    }
}
