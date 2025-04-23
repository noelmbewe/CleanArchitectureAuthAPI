namespace Application.Interfaces;

public interface IOtpService
{
    string GenerateOtp();
    Task StoreOtpAsync(string email, string otp);
    Task<string?> GetOtpAsync(string email);
    Task RemoveOtpAsync(string email);
    
}
