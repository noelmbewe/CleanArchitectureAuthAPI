using Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Auth.Commands;

public class ResendOtpCommandHandler : IRequestHandler<ResendOtpCommand, string>
{
    private readonly IOtpService _otpService;
    private readonly IMessageBroker _messageBroker;

    public ResendOtpCommandHandler(IOtpService otpService, IMessageBroker messageBroker)
    {
        _otpService = otpService;
        _messageBroker = messageBroker;
    }

    public async Task<string> Handle(ResendOtpCommand request, CancellationToken cancellationToken)
    {
        var cachedOtp = await _otpService.GetOtpAsync(request.Email);
        if (!string.IsNullOrEmpty(cachedOtp))
        {
            throw new Exception("An active OTP already exists.");
        }

        var otp = _otpService.GenerateOtp();
        await _otpService.StoreOtpAsync(request.Email, otp);
        await _messageBroker.PublishAsync("email_queue", $"{request.Email}:{otp}");
        return "New OTP sent to your email.";
    }
}