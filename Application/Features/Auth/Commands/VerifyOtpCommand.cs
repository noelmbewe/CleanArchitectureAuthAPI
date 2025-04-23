using MediatR;

namespace Application.Features.Auth.Commands;

public class VerifyOtpCommand : IRequest<string>
{
    public string Email { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}