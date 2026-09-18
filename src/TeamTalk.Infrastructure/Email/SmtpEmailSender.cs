using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamTalk.Application.Common.Interfaces;

namespace TeamTalk.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var host = _configuration["Email:Smtp:Host"];
        var fromAddress = _configuration["Email:FromAddress"] ?? "no-reply@teamtalk.local";
        var fromName = _configuration["Email:FromName"] ?? "TeamTalk";

        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation(
                "Email:Smtp:Host is not configured; logging email instead of sending. To: {ToEmail}, Subject: {Subject}, Body: {Body}",
                toEmail,
                subject,
                htmlBody);
            return;
        }

        var port = int.TryParse(_configuration["Email:Smtp:Port"], out var configuredPort) ? configuredPort : 25;
        var enableSsl = bool.TryParse(_configuration["Email:Smtp:EnableSsl"], out var configuredSsl) && configuredSsl;
        var user = _configuration["Email:Smtp:User"];
        var password = _configuration["Email:Smtp:Password"];

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
        };

        if (!string.IsNullOrWhiteSpace(user))
        {
            client.Credentials = new NetworkCredential(user, password);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message, cancellationToken);
    }
}
