using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;

namespace Application.Features.Auth.Commands;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, string>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageBroker _messageBroker;
    private readonly IOtpService _otpService;
    private readonly IDistributedCache _cache;

    public RegisterCommandHandler(IUnitOfWork unitOfWork, IMessageBroker messageBroker, IOtpService otpService, IDistributedCache cache)
    {
        _unitOfWork = unitOfWork;
        _messageBroker = messageBroker;
        _otpService = otpService;
        _cache = cache;
    }

    public async Task<string> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (existingUser != null)
            throw new InvalidOperationException("Email already exists.");

        var userData = System.Text.Json.JsonSerializer.Serialize(new
        {
            request.Username,
            request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        });
        await _cache.SetStringAsync($"pending_user:{request.Email}", userData, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        var otp = _otpService.GenerateOtp();
        await _otpService.StoreOtpAsync(request.Email, otp);
        await _messageBroker.PublishAsync("email_queue", $"{request.Email}:{otp}");


        Log.Information("OTP sent for user {Email}", request.Email);
        return "OTP sent to your email.";
    }
}