using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Endpoints;

public static class AuthEndpoints
{
    private const string LoginInvalidRedirect = "/account/login?error=invalid";
    private const string MfaTokenProvider = "Email";

    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/account/do-login", HandleLoginAsync);
        app.MapGet("/account/do-logout", HandleLogoutAsync);
        app.MapPost("/account/do-mfa-verify", HandleMfaVerifyAsync);
        app.MapPost("/account/do-mfa-resend-email", HandleMfaResendEmailAsync);
        app.MapPost("/account/do-resend-confirmation", HandleResendConfirmationAsync);
    }

    private static async Task<IResult> HandleLoginAsync(
        HttpContext context,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender)
    {
        var form = await context.Request.ReadFormAsync();
        var identifier = form["email"].ToString().Trim();
        var password = form["password"].ToString();
        var rememberMe = form["rememberMe"] == "true";

        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
            return Results.Redirect(LoginInvalidRedirect);

        var user = await userManager.FindByEmailAsync(identifier) ??
                    await userManager.FindByNameAsync(identifier);

        if (user == null || string.IsNullOrWhiteSpace(user.UserName))
            return Results.Redirect(LoginInvalidRedirect);

        // Always require MFA code when the user has MFA enabled, even on remembered browsers.
        if (await userManager.GetTwoFactorEnabledAsync(user))
            await signInManager.ForgetTwoFactorClientAsync();

        var result = await signInManager.PasswordSignInAsync(
            user.UserName, password, rememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
            return Results.Redirect("/");

        if (result.RequiresTwoFactor)
        {
            var mfaMethod = string.IsNullOrWhiteSpace(user?.MfaMethod) ? "Authenticator" : user.MfaMethod;

            if (mfaMethod == MfaTokenProvider && user?.Email is not null)
                await SendEmailMfaCodeAsync(userManager, emailSender, user);

            return Results.Redirect($"/account/mfa-verify?rememberMe={rememberMe}&mfaMethod={Uri.EscapeDataString(mfaMethod)}");
        }

        if (result.IsLockedOut)
            return Results.Redirect("/account/login?error=locked");
        if (result.IsNotAllowed)
            return Results.Redirect($"/account/login?error=notallowed&email={Uri.EscapeDataString(identifier)}");

        return Results.Redirect(LoginInvalidRedirect);
    }

    private static async Task SendEmailMfaCodeAsync(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        ApplicationUser user)
    {
        var code = await userManager.GenerateTwoFactorTokenAsync(user, MfaTokenProvider);
        await emailSender.SendEmailAsync(
            user.Email!,
            "Código de verificação — Savio Mock Server",
            $"<h3>Código de verificação</h3>" +
            $"<p>Seu código: <strong>{code}</strong></p>" +
            $"<p>Este código expira em 10 minutos.</p>");
    }

    private static async Task<IResult> HandleLogoutAsync(
        HttpContext context,
        SignInManager<ApplicationUser> signInManager)
    {
        var credentialUpdated = context.Request.Query["credentialUpdated"] == "true";

        await signInManager.SignOutAsync();

        var loginUrl = credentialUpdated
            ? "/account/login?credentialUpdated=true"
            : "/account/login";

        return Results.Redirect(loginUrl);
    }

    private static async Task<IResult> HandleMfaVerifyAsync(
        HttpContext context,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        var form = await context.Request.ReadFormAsync();
        var code = form["code"].ToString().Replace(" ", "").Replace("-", "");
        var rememberMachine = form["rememberMachine"] == "true";
        var mfaMethod = form["mfaMethod"].ToString();

        if (string.IsNullOrWhiteSpace(code))
            return Results.Redirect("/account/mfa-verify?error=invalid");

        if (mfaMethod == MfaTokenProvider)
            return await HandleEmailMfaAsync(signInManager, userManager, code, rememberMachine);

        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
            code, isPersistent: false, rememberClient: rememberMachine);

        if (result.Succeeded)
            return Results.Redirect("/");
        if (result.IsLockedOut)
            return Results.Redirect("/account/login?error=locked");

        return Results.Redirect("/account/mfa-verify?error=invalid");
    }

    private static async Task<IResult> HandleEmailMfaAsync(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        string code,
        bool rememberMachine)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            return Results.Redirect(LoginInvalidRedirect);

        var isValid = await userManager.VerifyTwoFactorTokenAsync(user, MfaTokenProvider, code);
        if (!isValid)
            return Results.Redirect("/account/mfa-verify?error=invalid");

        await signInManager.SignInAsync(user, isPersistent: false);
        if (rememberMachine)
            await signInManager.RememberTwoFactorClientAsync(user);

        return Results.Redirect("/");
    }

    private static async Task<IResult> HandleMfaResendEmailAsync(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            return Results.Redirect(LoginInvalidRedirect);

        try
        {
            await SendEmailMfaCodeAsync(userManager, emailSender, user);
            return Results.Redirect($"/account/mfa-verify?mfaMethod={MfaTokenProvider}&resent=true");
        }
        catch
        {
            return Results.Redirect($"/account/mfa-verify?mfaMethod={MfaTokenProvider}&error=email-send");
        }
    }

    private static async Task<IResult> HandleResendConfirmationAsync(
        HttpContext context,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender)
    {
        var form = await context.Request.ReadFormAsync();
        var identifier = form["email"].ToString().Trim();

        if (string.IsNullOrWhiteSpace(identifier))
            return Results.Redirect("/account/login?resend=missing");

        var user = await userManager.FindByEmailAsync(identifier) ??
                   await userManager.FindByNameAsync(identifier);

        // Resposta genérica para evitar enumeração de usuários.
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return Results.Redirect($"/account/login?resend=success&email={Uri.EscapeDataString(identifier)}");

        var isConfirmed = await userManager.IsEmailConfirmedAsync(user);
        if (isConfirmed)
            return Results.Redirect($"/account/login?resend=already-confirmed&email={Uri.EscapeDataString(user.Email)}");

        try
        {
            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = $"{context.Request.Scheme}://{context.Request.Host}/account/confirm-email?userId={user.Id}&code={Uri.EscapeDataString(code)}";

            await emailSender.SendEmailAsync(
                user.Email,
                "Confirme sua conta — Savio Mock Server",
                "<h3>Confirmação de conta</h3>" +
                "<p>Recebemos uma solicitação para reenviar a confirmação da sua conta.</p>" +
                $"<p><a href='{callbackUrl}'>Clique aqui para confirmar sua conta</a></p>" +
                "<p>Se você já confirmou sua conta, ignore este e-mail.</p>");

            return Results.Redirect($"/account/login?resend=success&email={Uri.EscapeDataString(user.Email)}");
        }
        catch
        {
            return Results.Redirect($"/account/login?resend=error&error=notallowed&email={Uri.EscapeDataString(identifier)}");
        }
    }
}
