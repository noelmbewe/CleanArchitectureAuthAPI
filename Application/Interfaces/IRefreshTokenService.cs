namespace Application.Interfaces;


public interface IRefreshTokenService
{
    string GenerateRefreshToken();
    Task StoreRefreshTokenAsync(string userId, string refreshToken);
    Task<string?> GetRefreshTokenAsync(string userId);
}