using MediatR;

namespace Application.Features.Auth.Commands;

public class RegisterCommand : IRequest<string>
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}