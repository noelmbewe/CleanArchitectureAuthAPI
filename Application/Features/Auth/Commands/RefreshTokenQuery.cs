using MediatR;

namespace Application.Features.Auth.Queries;

public class RefreshTokenQuery : IRequest<string>
{
    public string RefreshToken { get; set; }

    public RefreshTokenQuery(string refreshToken)
    {
        RefreshToken = refreshToken;
    }
}