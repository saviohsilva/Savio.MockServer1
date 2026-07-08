using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace Savio.MockServer.Services;

public class SmtpEmailSender(EmailSettingService emailSettingService, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var settings = await emailSettingService.GetEffectiveSettingsAsync();

        if (settings == null)
        {
            logger.LogWarning(
                "⚠️ SMTP não configurado. " +
                "E-mail para {Email} não será enviado. " +
                "Configure a seção 'Email' no appsettings.json ou na tela de Configurações para habilitar envio real. " +
                "Assunto: {Subject}", email, subject);
            logger.LogInformation("📧 Conteúdo do e-mail (para depuração):\n{Message}", htmlMessage);
            return;
        }

        var fromEmail = settings.FromEmail ?? settings.SmtpUser;
        var fromName = settings.FromName ?? "Savio Mock Server";

        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            Credentials = new NetworkCredential(settings.SmtpUser, settings.SmtpPass),
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
            logger.LogInformation("✅ E-mail enviado com sucesso para {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Erro ao enviar e-mail para {Email}", email);
            throw new InvalidOperationException($"Falha ao enviar e-mail para '{email}'.", ex);
        }
    }
}
