using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application.Features.Auth.Queries;

public class LoginQueryHandler : IRequestHandler<LoginQuery, string>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedCache _cache;
    private readonly IRefreshTokenService _refreshTokenService;

    public LoginQueryHandler(IUnitOfWork unitOfWork, IDistributedCache cache, IRefreshTokenService refreshTokenService)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<string> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid credentials.");

        var cachedToken = await _cache.GetStringAsync($"token_{user.Id}");
        if (!string.IsNullOrEmpty(cachedToken))
            return cachedToken;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("your-very-secure-secret-key-here-32chars+" ?? throw new InvalidOperationException());
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("id", user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Id == 1 ? "Admin" : "User")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        await _cache.SetStringAsync($"token_{user.Id}", tokenString, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        });

        var refreshToken = _refreshTokenService.GenerateRefreshToken();
        await _refreshTokenService.StoreRefreshTokenAsync(user.Id.ToString(), refreshToken);

        return $"{tokenString}:{refreshToken}";
    }
}