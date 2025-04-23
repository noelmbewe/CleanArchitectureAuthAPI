using Application.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using System.Text.Json;

namespace Application.Features.Auth.Commands;

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, string>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOtpService _otpService;
    private readonly IDistributedCache _cache;

    public VerifyOtpCommandHandler(IUnitOfWork unitOfWork, IOtpService otpService, IDistributedCache cache)
    {
        _unitOfWork = unitOfWork;
        _otpService = otpService;
        _cache = cache;
    }

    public async Task<string> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var storedOtp = await _otpService.GetOtpAsync(request.Email);
        if (storedOtp != request.Otp)
            throw new InvalidOperationException("Invalid OTP.");

        var userDataJson = await _cache.GetStringAsync($"pending_user:{request.Email}");
        if (string.IsNullOrEmpty(userDataJson))
            throw new InvalidOperationException("Registration session expired.");

        var userData = JsonSerializer.Deserialize<AnonymousUserData>(userDataJson);
        if (userData == null)
            throw new InvalidOperationException("Failed to retrieve user data.");

        var user = new User
        {
            Username = userData.Username,
            Email = userData.Email,
            PasswordHash = userData.PasswordHash
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
        await _otpService.RemoveOtpAsync(request.Email);
        await _cache.RemoveAsync($"pending_user:{request.Email}");

        Log.Information("User {Email} registered successfully", request.Email);
        return "User registered successfully.";
    }

    private class AnonymousUserData
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }
}