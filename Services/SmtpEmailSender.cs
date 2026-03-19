using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace Savio.MockServer.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var smtpHost = _configuration["Email:SmtpHost"];
        var smtpPort = int.TryParse(_configuration["Email:SmtpPort"], out var port) ? port : 587;
        var smtpUser = _configuration["Email:SmtpUser"];
        var smtpPass = _configuration["Email:SmtpPass"];
        var fromEmail = _configuration["Email:FromEmail"] ?? smtpUser;
        var fromName = _configuration["Email:FromName"] ?? "Savio Mock Server";

        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            _logger.LogWarning(
                "⚠️ SMTP não configurado (Email:SmtpHost vazio). " +
                "E-mail para {Email} não será enviado. " +
                "Configure a seção 'Email' no appsettings.json para habilitar envio real. " +
                "Assunto: {Subject}", email, subject);
            _logger.LogInformation("📧 Conteúdo do e-mail (para depuração):\n{Message}", htmlMessage);
            return;
        }

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };

        var message = new MailMessage
        {
            From = new MailAddress(fromEmail!, fromName),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };
        message.To.Add(email);

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("✅ E-mail enviado com sucesso para {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao enviar e-mail para {Email}", email);
            throw;
        }
    }
}
