using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.Features.Auth.Commands;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, string>
{
    private readonly IDistributedCache _cache;
    private readonly IRefreshTokenService _refreshTokenService;

    public LogoutCommandHandler(IDistributedCache cache, IRefreshTokenService refreshTokenService)
    {
        _cache = cache;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<string> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Remove the cached JWT token
        await _cache.RemoveAsync($"token_{request.Id}", cancellationToken);

        // Verify and remove the refresh token
        var storedRefreshToken = await _refreshTokenService.GetRefreshTokenAsync(request.Id);
        if (storedRefreshToken == request.RefreshToken)
        {
            // Since IRefreshTokenService doesn't have a remove method, remove directly from cache
            await _cache.RemoveAsync($"refreshToken_{request.Id}", cancellationToken);
        }

        return "Logout successful.";
    }
}