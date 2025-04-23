using MediatR;

namespace Application.Features.Auth.Commands;

public class ResendOtpCommand : IRequest<string>
{
    public required string Email { get; set; }
}