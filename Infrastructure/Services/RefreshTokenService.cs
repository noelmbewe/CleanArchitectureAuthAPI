using Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;

namespace Infrastructure.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IDistributedCache _cache;

    public RefreshTokenService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task StoreRefreshTokenAsync(string userId, string refreshToken)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
        };
        await _cache.SetStringAsync($"refresh_token:{userId}", refreshToken, options);
    }

    public async Task<string?> GetRefreshTokenAsync(string userId)
    {
        return await _cache.GetStringAsync($"refresh_token:{userId}");
    }
}