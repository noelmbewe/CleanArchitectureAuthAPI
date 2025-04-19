using MediatR;

namespace Application.Features.Auth.Queries;

public class LoginQuery : IRequest<string>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}