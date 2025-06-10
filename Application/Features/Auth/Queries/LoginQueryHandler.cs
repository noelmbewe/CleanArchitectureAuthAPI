using Application.Interfaces;
using Application.Features.Auth.Queries;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Application.Features.Auth.Queries;

public class LoginQueryHandler : IRequestHandler<LoginQuery, string>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedCache _cache;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<LoginQueryHandler> _logger;

    public LoginQueryHandler(
        IUnitOfWork unitOfWork,
        IDistributedCache cache,
        IRefreshTokenService refreshTokenService,
        ILogger<LoginQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    public async Task<string> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to log in user with email: {Email}", request.Email);

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("User not found for email: {Email}", request.Email);
            throw new InvalidOperationException("Invalid credentials: User not found.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Password verification failed for email: {Email}", request.Email);
            throw new InvalidOperationException("Invalid credentials: Incorrect password.");
        }

        _logger.LogInformation("User authenticated successfully for email: {Email}", request.Email);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("your-very-secure-secret-key-here-32chars+");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("id", user.Id.ToString())
            }),
            Audience = "auth-api", // Add audience claim
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        await _cache.SetStringAsync($"token_{user.Id}", tokenString, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        }, cancellationToken);

        var refreshToken = _refreshTokenService.GenerateRefreshToken();
        await _refreshTokenService.StoreRefreshTokenAsync(user.Id.ToString(), refreshToken);

        _logger.LogInformation("Login successful for user ID: {UserId}", user.Id);
        return $"{tokenString}:{refreshToken}";
    }
}