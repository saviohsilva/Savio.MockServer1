using Microsoft.AspNetCore.Identity;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Services;

namespace Savio.MockServer.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/account/do-login", async (
            HttpContext context,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment env) =>
        {
            var form = await context.Request.ReadFormAsync();
            var email = form["email"].ToString();
            var password = form["password"].ToString();
            var rememberMe = form["rememberMe"] == "true";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return Results.Redirect("/account/login?error=invalid");

            var result = await signInManager.PasswordSignInAsync(
                email, password, rememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
                return Results.Redirect("/");

            if (result.RequiresTwoFactor)
            {
                if (env.IsDevelopment())
                {
                    var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
                    if (user != null)
                    {
                        await signInManager.SignInAsync(user, rememberMe);
                        return Results.Redirect("/");
                    }
                }
                return Results.Redirect($"/account/mfa-verify?rememberMe={rememberMe}");
            }

            if (result.IsLockedOut)
                return Results.Redirect("/account/login?error=locked");
            if (result.IsNotAllowed)
                return Results.Redirect("/account/login?error=notallowed");

            return Results.Redirect("/account/login?error=invalid");
        });

        app.MapGet("/account/do-logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Redirect("/account/login");
        });

        app.MapPost("/account/do-mfa-verify", async (
            HttpContext context,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager) =>
        {
            var form = await context.Request.ReadFormAsync();
            var code = form["code"].ToString().Replace(" ", "").Replace("-", "");
            var rememberMachine = form["rememberMachine"] == "true";
            var mfaMethod = form["mfaMethod"].ToString();

            if (string.IsNullOrWhiteSpace(code))
                return Results.Redirect("/account/mfa-verify?error=invalid");

            if (mfaMethod == "Email")
            {
                var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user == null)
                    return Results.Redirect("/account/login?error=invalid");

                var isValid = await userManager.VerifyTwoFactorTokenAsync(user, "Email", code);
                if (isValid)
                {
                    await signInManager.SignInAsync(user, isPersistent: false);
                    if (rememberMachine)
                        await signInManager.RememberTwoFactorClientAsync(user);
                    return Results.Redirect("/");
                }
                return Results.Redirect("/account/mfa-verify?error=invalid");
            }

            var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
                code, isPersistent: false, rememberClient: rememberMachine);

            if (result.Succeeded)
                return Results.Redirect("/");
            if (result.IsLockedOut)
                return Results.Redirect("/account/login?error=locked");

            return Results.Redirect("/account/mfa-verify?error=invalid");
        });
    }
}
