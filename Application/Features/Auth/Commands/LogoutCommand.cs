using MediatR;

namespace Application.Features.Auth.Commands;

public class LogoutCommand : IRequest<string>
{
    public string Id { get; set; }
    public string RefreshToken { get; set; }
}