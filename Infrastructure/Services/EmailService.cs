using Infrastructure.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Serilog;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Infrastructure.Services;

public class EmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { TextBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            // Bypass certificate revocation check for testing
            client.ServerCertificateValidationCallback = (s, c, h, e) => true; // WARNING: Use only for testing
            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            Log.Information("Email sent successfully to {ToEmail} with subject {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send email to {ToEmail} with subject {Subject}", toEmail, subject);
            throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
        }
    }

    public async Task TestSmtpConnectionAsync()
    {
        try
        {
            using var client = new SmtpClient();
            client.ServerCertificateValidationCallback = (s, c, h, e) => true; // WARNING: Use only for testing
            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
            await client.DisconnectAsync(true);
            Log.Information("SMTP connection test successful for {SmtpUsername}", _emailSettings.SmtpUsername);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SMTP connection test failed for {SmtpUsername}", _emailSettings.SmtpUsername);
            throw new InvalidOperationException($"SMTP connection test failed: {ex.Message}", ex);
        }
    }
}