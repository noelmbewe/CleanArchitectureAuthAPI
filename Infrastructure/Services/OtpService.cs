using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Threading.Tasks;
using Application.Interfaces;

namespace Infrastructure.Services;

public class OtpService : IOtpService
{
    private readonly IDistributedCache _cache;

    public OtpService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public string GenerateOtp()
    {
        return new Random().Next(100000, 999999).ToString();
    }

    public async Task StoreOtpAsync(string email, string otp)
    {
        await _cache.SetStringAsync($"otp:{email}", otp, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });
    }

    public async Task<string?> GetOtpAsync(string email)
    {
        return await _cache.GetStringAsync($"otp:{email}");
    }

    public async Task RemoveOtpAsync(string email)
    {
        await _cache.RemoveAsync($"otp:{email}");
    }
}