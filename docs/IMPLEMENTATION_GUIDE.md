# 📋 GUIA DE IMPLEMENTAÇÃO — Savio.MockServer

> **OBJETIVO**: Este documento é um prompt de instrução completo para que uma IA (mesmo sem conhecimento prévio do projeto) consiga implementar as features e melhorias listadas abaixo. Siga cada seção na ordem, faça build ao final de cada feature, e só prossiga para a próxima se o build passar.

---

## 📌 CONTEXTO DO PROJETO

**Savio.MockServer** é uma aplicação **Blazor Server (.NET 8)** que funciona como um servidor de mock de APIs HTTP. Cada usuário cria uma conta com um **alias** único (ex: `meuhost`), e suas rotas de mock ficam acessíveis em `http://localhost:5100/{alias}/{rota}`.

### Stack Técnica
| Tecnologia | Detalhes |
|---|---|
| Runtime | .NET 8, C# 12 |
| UI Framework | Blazor Server (render-mode `Server`) |
| Identity | ASP.NET Identity com `IdentityDbContext<ApplicationUser>` |
| Banco de Dados | EF Core 8.0 — SQLite (padrão), MySQL, SQL Server |
| Pacotes UI | Blazored.Modal 7.3.1, Blazored.Toast 4.2.1, Bootstrap 5 |
| E-mail | `SmtpEmailSender` (implementa `IEmailSender`) |
| Servidor | Kestrel em localhost:5100 |

### Arquivos-Chave para as Features
| Arquivo | Função |
|---|---|
| `Program.cs` | Pipeline completo (288 linhas): DB, Identity, CORS, middleware, endpoints de auth |
| `Services/SmtpEmailSender.cs` | Implementação de envio de e-mail via SMTP |
| `Pages/Account/Register.razor` | Registro com validação de alias + confirmação de e-mail |
| `Pages/Account/Settings.razor` | Configurações da conta (alias somente leitura atualmente) |
| `Pages/Account/MfaSetup.razor` | Setup de MFA (apenas TOTP atualmente) |
| `Pages/Account/MfaVerify.razor` | Verificação de MFA no login (apenas TOTP atualmente) |
| `Pages/Account/Login.razor` | Formulário de login |
| `Data/Entities/ApplicationUser.cs` | Entidade do usuário com `Alias` e `CreatedAt` |
| `Data/MockDbContext.cs` | DbContext com índice único em `Alias` |
| `appsettings.json` | Configuração com seção `Email` (SMTP vazio por padrão) |
| `appsettings.Development.json` | Config de desenvolvimento (sem seção Email) |
| `Middleware/MockEndpointMiddleware.cs` | Resolução de alias e execução de mocks |
| `REFACTOR_PLAN.md` | Plano de refatoração arquitetural com 7 fases (já criado) |

### Comportamento Atual
- **SMTP não configurado**: Quando `Email:SmtpHost` está vazio, o `SmtpEmailSender` apenas loga no console e **não envia** o e-mail. O `Register.razor` detecta isso e **auto-confirma** a conta.
- **Alias**: É definido na criação da conta e exibido como **somente leitura** em `Settings.razor`. Não é possível alterá-lo.
- **MFA**: Apenas TOTP (Google Authenticator/Microsoft Authenticator). Não há opção de receber código por e-mail.
- **MFA em debug**: Sempre obrigatório quando habilitado, mesmo em modo de desenvolvimento.
- **Identity**: `RequireConfirmedEmail = true`, senhas mínimo 6 chars, lockout após 5 tentativas.

---

## 🔧 FEATURE 1 — Configuração de SMTP para Confirmação de E-mail

### Objetivo
Garantir que a aplicação funcione com SMTP configurado para envio de e-mail de confirmação. A conta **só pode ser acessada** após a confirmação via e-mail.

### O que já existe
- `Services/SmtpEmailSender.cs` — Implementação completa de `IEmailSender`. Quando `SmtpHost` está vazio, loga warning e não envia.
- `Pages/Account/Register.razor` — Já verifica se SMTP está configurado. Se sim, envia e-mail. Se não, auto-confirma.
- `Pages/Account/ConfirmEmail.razor` — Já existe a página que recebe `userId` + `code` e confirma o e-mail.
- `Program.cs` linha 93 — `options.SignIn.RequireConfirmedEmail = true` já está habilitado.
- `appsettings.json` — Seção `Email` já existe com campos vazios.

### O que precisa ser feito

#### 1.1 — Preencher as configurações de SMTP no `appsettings.json`

Editar `appsettings.json` e preencher a seção `Email` com valores reais do servidor SMTP desejado. Exemplo com Gmail:

```json
"Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "seu-email@gmail.com",
    "SmtpPass": "sua-senha-de-app",
    "FromEmail": "seu-email@gmail.com",
    "FromName": "Savio Mock Server"
}
```

> **IMPORTANTE**: Para Gmail, é necessário gerar uma **Senha de App** em https://myaccount.google.com/apppasswords (a conta precisa ter verificação em duas etapas habilitada). Não use a senha da conta diretamente.

> **Alternativas de SMTP gratuito**: Outlook (smtp.office365.com:587), Mailtrap (sandbox para testes), SendGrid (100 e-mails/dia grátis).

#### 1.2 — Configurar appsettings para desenvolvimento

Editar `appsettings.Development.json` e adicionar uma seção `Email` para ambiente de desenvolvimento. Há duas opções:

**Opção A — Usar SMTP real em dev** (recomendado para testar fluxo completo):
```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "seu-email@gmail.com",
    "SmtpPass": "sua-senha-de-app",
    "FromEmail": "seu-email@gmail.com",
    "FromName": "Savio Mock Server (Dev)"
  }
}
```

**Opção B — Manter SMTP desabilitado em dev** (auto-confirma contas):
Não adicionar seção `Email` no `appsettings.Development.json`. O `SmtpHost` ficará vazio e o comportamento de auto-confirmação será mantido.

#### 1.3 — Melhorar o `SmtpEmailSender` para melhor resiliência

Editar `Services/SmtpEmailSender.cs`:

1. **Adicionar retry simples** em caso de falha transitória de rede.
2. **Melhorar o log** quando SMTP não está configurado para indicar claramente que a conta será auto-confirmada.

Alterar o método `SendEmailAsync`:

```csharp
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
```

#### 1.4 — Garantir que a Register page redireciona corretamente

O `Register.razor` já trata os dois cenários (SMTP configurado vs não configurado). **Verificar** que:
- Com SMTP configurado: o usuário é redirecionado para `/account/register-confirmation` com uma mensagem pedindo que verifique o e-mail.
- Sem SMTP: a conta é auto-confirmada e o usuário é redirecionado com `autoConfirmed=true`.

**Nenhuma alteração necessária no Register.razor** se o comportamento já está correto.

#### 1.5 — Verificação

1. Compilar a aplicação (`dotnet build`)
2. Com SMTP vazio: registrar uma conta → deve auto-confirmar e permitir login
3. Com SMTP preenchido: registrar uma conta → deve enviar e-mail → conta só acessa após clicar no link
4. Tentar fazer login com conta não confirmada → deve mostrar erro "notallowed"

---

## 🔧 FEATURE 2 — Edição de Alias com Validação de Unicidade

### Objetivo
Permitir que o usuário altere seu alias na página de Configurações (`Settings.razor`), com validação de unicidade (não pode existir outro igual).

### O que já existe
- `Settings.razor` — Exibe o alias como **somente leitura** (texto estático)
- `Register.razor` — Já tem lógica de validação de alias (regex + unicidade via `DbContext.Users.AnyAsync`)
- `ApplicationUser.cs` — `Alias` é `[Required]` e `[MaxLength(100)]`
- `MockDbContext.cs` — Índice único em `Alias`

### O que precisa ser feito

#### 2.1 — Editar `Pages/Account/Settings.razor`

Adicionar um formulário de edição de alias na seção de informações. O fluxo deve ser:
1. O alias atual é exibido em um campo de texto editável
2. O usuário digita o novo alias e clica em "Salvar"
3. A aplicação valida:
   - Formato (mesma regex de Register: `^[a-z0-9][a-z0-9_-]{1,48}[a-z0-9]$`)
   - Unicidade (consulta `DbContext.Users.AnyAsync(u => u.Alias == novoAlias && u.Id != user.Id)`)
4. Se válido, atualiza via `UserManager.UpdateAsync(user)`
5. Exibe mensagem de sucesso ou erro

**Código a adicionar/modificar no `Settings.razor`:**

Adicionar no bloco `@code` as seguintes injeções (no topo do `.razor`):
```razor
@inject Savio.MockServer.Data.MockDbContext DbContext
@using Microsoft.EntityFrameworkCore
@using System.Text.RegularExpressions
```

Substituir a linha que mostra o alias estático:
```razor
<tr>
    <td class="fw-bold">Alias</td>
    <td><code>@user.Alias</code></td>
</tr>
```

Por um formulário inline:
```razor
<tr>
    <td class="fw-bold">Alias</td>
    <td>
        <div class="d-flex align-items-center gap-2">
            @if (isEditingAlias)
            {
                <div class="input-group" style="max-width: 350px;">
                    <span class="input-group-text">/</span>
                    <input type="text" class="form-control form-control-sm"
                           @bind="newAlias" @bind:event="oninput"
                           placeholder="meu-alias" maxlength="50" />
                </div>
                <button class="btn btn-sm btn-success" @onclick="SaveAlias" disabled="@isSavingAlias">
                    @if (isSavingAlias)
                    {
                        <span class="spinner-border spinner-border-sm"></span>
                    }
                    else
                    {
                        <i class="bi bi-check-lg"></i>
                    }
                </button>
                <button class="btn btn-sm btn-outline-secondary" @onclick="CancelEditAlias">
                    <i class="bi bi-x-lg"></i>
                </button>
            }
            else
            {
                <code>@user.Alias</code>
                <button class="btn btn-sm btn-outline-primary" @onclick="StartEditAlias">
                    <i class="bi bi-pencil"></i> Alterar
                </button>
            }
        </div>
        @if (!string.IsNullOrEmpty(aliasMessage))
        {
            <small class="@aliasMessageClass mt-1 d-block">@aliasMessage</small>
        }
    </td>
</tr>
```

Adicionar no bloco `@code`:
```csharp
private bool isEditingAlias;
private bool isSavingAlias;
private string newAlias = string.Empty;
private string? aliasMessage;
private string aliasMessageClass = "text-danger";

private void StartEditAlias()
{
    isEditingAlias = true;
    newAlias = user!.Alias;
    aliasMessage = null;
}

private void CancelEditAlias()
{
    isEditingAlias = false;
    aliasMessage = null;
}

private async Task SaveAlias()
{
    isSavingAlias = true;
    aliasMessage = null;
    StateHasChanged();

    try
    {
        var alias = newAlias.Trim().ToLowerInvariant();

        // Validar formato
        if (!Regex.IsMatch(alias, @"^[a-z0-9][a-z0-9_-]{1,48}[a-z0-9]$"))
        {
            aliasMessage = "O alias deve conter entre 3 e 50 caracteres, apenas letras minúsculas, números, hífens e underscores. Deve começar e terminar com letra ou número.";
            aliasMessageClass = "text-danger";
            return;
        }

        // Se não mudou, não faz nada
        if (alias == user!.Alias)
        {
            isEditingAlias = false;
            return;
        }

        // Validar unicidade (excluir o próprio usuário)
        var aliasExists = await DbContext.Users
            .AnyAsync(u => u.Alias == alias && u.Id != user.Id);

        if (aliasExists)
        {
            aliasMessage = $"O alias '{alias}' já está em uso por outro usuário. Escolha outro.";
            aliasMessageClass = "text-danger";
            return;
        }

        // Atualizar
        user.Alias = alias;
        var result = await UserManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            isEditingAlias = false;
            aliasMessage = "Alias atualizado com sucesso!";
            aliasMessageClass = "text-success";
        }
        else
        {
            aliasMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            aliasMessageClass = "text-danger";
        }
    }
    catch (Exception ex)
    {
        aliasMessage = $"Erro ao atualizar alias: {ex.Message}";
        aliasMessageClass = "text-danger";
    }
    finally
    {
        isSavingAlias = false;
    }
}
```

#### 2.2 — Verificar impacto nas rotas de mock

Quando o alias muda, as rotas de mock do usuário passam a responder no novo alias automaticamente, pois o `MockEndpointMiddleware` resolve o alias em tempo real consultando o banco. **Nenhuma alteração adicional necessária** nos middlewares.

#### 2.3 — Adicionar aviso sobre impacto da mudança

No formulário de edição de alias, adicionar um aviso visual:
```razor
@if (isEditingAlias)
{
    <small class="text-warning d-block mt-1">
        <i class="bi bi-exclamation-triangle"></i> Ao alterar o alias, todas as suas URLs de mock mudarão. Certifique-se de atualizar os clientes que consomem suas rotas.
    </small>
}
```

#### 2.4 — Verificação
1. Compilar a aplicação
2. Ir em Settings → clicar "Alterar" no alias
3. Digitar alias inválido → deve mostrar erro de formato
4. Digitar alias que já existe de outro usuário → deve mostrar erro de unicidade
5. Digitar alias válido e único → deve salvar e atualizar a URL base exibida
6. Verificar que as rotas de mock agora respondem no novo alias

---

## 🔧 FEATURE 3 — MFA por E-mail

### Objetivo
Adicionar a opção de receber o código de verificação de MFA por e-mail, além do TOTP (aplicativo autenticador) que já existe.

### O que já existe
- `MfaSetup.razor` — Setup de TOTP com QR code e verificação de código
- `MfaVerify.razor` — Formulário simples de código TOTP
- `Program.cs` endpoint `/account/do-mfa-verify` — Usa `TwoFactorAuthenticatorSignInAsync` (apenas TOTP)
- Identity já tem `AddDefaultTokenProviders()` que inclui provider de e-mail

### O que precisa ser feito

#### 3.1 — Adicionar propriedade `MfaMethod` ao `ApplicationUser`

Editar `Data/Entities/ApplicationUser.cs`:
```csharp
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Savio.MockServer.Data.Entities;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string Alias { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Método preferido de MFA: "Authenticator" (TOTP) ou "Email"
    /// </summary>
    [MaxLength(20)]
    public string MfaMethod { get; set; } = "Authenticator";
}
```

#### 3.2 — Criar migration para a nova coluna

Executar no terminal:
```bash
dotnet ef migrations add AddMfaMethodToApplicationUser
```

> **Nota**: Se usando SQLite, a migration será aplicada automaticamente na inicialização. Se o schema corrompido for detectado, o banco será recriado.

#### 3.3 — Atualizar `MfaSetup.razor`

Modificar a página para permitir escolher entre TOTP e E-mail:

**Adicionar antes do card de habilitação de MFA** (quando `!mfaEnabled`):
```razor
<div class="card mb-4">
    <div class="card-header">
        <h5 class="mb-0">Escolha o método de verificação</h5>
    </div>
    <div class="card-body">
        <div class="form-check mb-2">
            <input class="form-check-input" type="radio" name="mfaMethod" id="methodAuthenticator"
                   value="Authenticator" checked="@(selectedMethod == "Authenticator")"
                   @onchange='() => selectedMethod = "Authenticator"' />
            <label class="form-check-label" for="methodAuthenticator">
                <i class="bi bi-phone"></i> Aplicativo Autenticador (Google/Microsoft Authenticator)
                <small class="d-block text-muted">Recomendado — mais seguro e funciona offline</small>
            </label>
        </div>
        <div class="form-check">
            <input class="form-check-input" type="radio" name="mfaMethod" id="methodEmail"
                   value="Email" checked="@(selectedMethod == "Email")"
                   @onchange='() => selectedMethod = "Email"' />
            <label class="form-check-label" for="methodEmail">
                <i class="bi bi-envelope"></i> E-mail (@user?.Email)
                <small class="d-block text-muted">Um código será enviado para seu e-mail a cada login</small>
            </label>
        </div>
    </div>
</div>
```

**Modificar o card de setup existente**:
- Se `selectedMethod == "Authenticator"`: mostrar o fluxo atual (QR code + código)
- Se `selectedMethod == "Email"`: mostrar um botão "Enviar código de teste" → envia código para o e-mail → pede para digitar o código recebido

**No bloco `@code`**, adicionar:
```csharp
@inject IEmailSender EmailSender

private string selectedMethod = "Authenticator";
private string? emailTestCode;
private bool emailCodeSent;

private async Task SendEmailTestCode()
{
    var authState = await AuthState!;
    var user = await UserManager.GetUserAsync(authState.User);
    if (user == null) return;

    // Gerar token de e-mail MFA
    var code = await UserManager.GenerateTwoFactorTokenAsync(user, "Email");
    emailTestCode = code; // Guardar para validação

    await EmailSender.SendEmailAsync(
        user.Email!,
        "Código de verificação MFA — Savio Mock Server",
        $"<h3>Código de verificação</h3>" +
        $"<p>Seu código de verificação é: <strong>{code}</strong></p>" +
        $"<p>Este código expira em 10 minutos.</p>");

    emailCodeSent = true;
    statusMessage = "Código enviado para seu e-mail! Verifique sua caixa de entrada.";
    statusClass = "alert-info";
}

private async Task EnableMfaEmail()
{
    isLoading = true;
    statusMessage = null;
    StateHasChanged();

    try
    {
        var authState = await AuthState!;
        var user = await UserManager.GetUserAsync(authState.User);
        if (user == null) return;

        var code = verificationCode.Replace(" ", "").Replace("-", "");
        var isValid = await UserManager.VerifyTwoFactorTokenAsync(user, "Email", code);

        if (isValid)
        {
            await UserManager.SetTwoFactorEnabledAsync(user, true);
            user.MfaMethod = "Email";
            await UserManager.UpdateAsync(user);
            recoveryCodes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            mfaEnabled = true;
            statusMessage = "MFA por e-mail habilitado com sucesso! Guarde os códigos de recuperação.";
            statusClass = "alert-success";
        }
        else
        {
            statusMessage = "Código inválido. Verifique e tente novamente.";
            statusClass = "alert-danger";
        }
    }
    finally
    {
        isLoading = false;
    }
}
```

**Atualizar o método `EnableMfa` existente** para salvar `MfaMethod = "Authenticator"`:
```csharp
if (isValid)
{
    await UserManager.SetTwoFactorEnabledAsync(user, true);
    user.MfaMethod = "Authenticator";
    await UserManager.UpdateAsync(user);
    // ... resto igual
}
```

#### 3.4 — Atualizar `MfaVerify.razor`

Modificar para exibir o formulário adequado baseado no método de MFA do usuário:

```razor
@page "/account/mfa-verify"
@layout Savio.MockServer.Shared.AuthLayout
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Identity
@using Microsoft.AspNetCore.Identity.UI.Services
@using Savio.MockServer.Data.Entities
@attribute [AllowAnonymous]
@inject SignInManager<ApplicationUser> SignInManager
@inject UserManager<ApplicationUser> UserManager
@inject IEmailSender EmailSender

<PageTitle>Verificação em Duas Etapas - Savio Mock Server</PageTitle>

<div class="card shadow" style="width: 100%; max-width: 420px;">
    <div class="card-body p-4">
        <div class="text-center mb-4">
            <i class="bi bi-shield-lock fs-1 text-primary d-block mb-2"></i>
            <h4>Verificação em Duas Etapas</h4>
            @if (mfaMethod == "Email")
            {
                <p class="text-muted small">Um código foi enviado para seu e-mail</p>
            }
            else
            {
                <p class="text-muted small">Digite o código do seu aplicativo autenticador</p>
            }
        </div>

        @if (!string.IsNullOrEmpty(errorMessage))
        {
            <div class="alert alert-danger">
                <i class="bi bi-exclamation-triangle"></i> @errorMessage
            </div>
        }

        <form method="post" action="/account/do-mfa-verify">
            <input type="hidden" name="mfaMethod" value="@mfaMethod" />

            <div class="mb-3">
                <label class="form-label">Código de Verificação</label>
                <input type="text" class="form-control text-center fs-4" name="code"
                       placeholder="000000" maxlength="7" required />
            </div>

            @if (mfaMethod == "Email")
            {
                <div class="mb-3 text-center">
                    <button type="button" class="btn btn-link btn-sm" @onclick="ResendEmailCode">
                        <i class="bi bi-arrow-repeat"></i> Reenviar código por e-mail
                    </button>
                </div>
            }

            <div class="mb-3 form-check">
                <input type="checkbox" class="form-check-input" name="rememberMachine" value="true" id="rememberMachine" />
                <label class="form-check-label small" for="rememberMachine">Lembrar este dispositivo</label>
            </div>

            <button type="submit" class="btn btn-primary w-100">
                <i class="bi bi-shield-check"></i> Verificar
            </button>
        </form>

        <hr />
        <div class="text-center">
            <a href="/account/login" class="text-decoration-none small">
                <i class="bi bi-arrow-left"></i> Voltar ao login
            </a>
        </div>
    </div>
</div>

@code {
    [SupplyParameterFromQuery]
    public string? Error { get; set; }

    private string? errorMessage;
    private string mfaMethod = "Authenticator";

    protected override async Task OnInitializedAsync()
    {
        errorMessage = Error switch
        {
            "invalid" => "Código inválido. Tente novamente.",
            _ => null
        };

        // Detectar método de MFA do usuário
        var twoFactorUser = await SignInManager.GetTwoFactorAuthenticationUserAsync();
        if (twoFactorUser != null)
        {
            mfaMethod = twoFactorUser.MfaMethod ?? "Authenticator";

            // Se método é e-mail, enviar código automaticamente
            if (mfaMethod == "Email" && string.IsNullOrEmpty(Error))
            {
                await SendEmailCodeAsync(twoFactorUser);
            }
        }
    }

    private async Task SendEmailCodeAsync(ApplicationUser user)
    {
        try
        {
            var code = await UserManager.GenerateTwoFactorTokenAsync(user, "Email");
            await EmailSender.SendEmailAsync(
                user.Email!,
                "Código de verificação — Savio Mock Server",
                $"<h3>Código de verificação</h3>" +
                $"<p>Seu código: <strong>{code}</strong></p>" +
                $"<p>Este código expira em 10 minutos.</p>");
        }
        catch (Exception)
        {
            errorMessage = "Erro ao enviar código por e-mail. Tente novamente.";
        }
    }

    private async Task ResendEmailCode()
    {
        var twoFactorUser = await SignInManager.GetTwoFactorAuthenticationUserAsync();
        if (twoFactorUser != null)
        {
            await SendEmailCodeAsync(twoFactorUser);
            errorMessage = null;
            StateHasChanged();
        }
    }
}
```

#### 3.5 — Atualizar endpoint `/account/do-mfa-verify` no `Program.cs`

O endpoint precisa tratar ambos os métodos (TOTP e E-mail):

```csharp
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

    Microsoft.AspNetCore.Identity.SignInResult result;

    if (mfaMethod == "Email")
    {
        // Verificar código de e-mail
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            return Results.Redirect("/account/login?error=invalid");

        var isValid = await userManager.VerifyTwoFactorTokenAsync(user, "Email", code);
        if (isValid)
        {
            // Sign in manualmente
            await signInManager.SignInAsync(user, isPersistent: false);
            if (rememberMachine)
            {
                await signInManager.RememberTwoFactorClientAsync();
            }
            return Results.Redirect("/");
        }
        return Results.Redirect("/account/mfa-verify?error=invalid");
    }
    else
    {
        // TOTP (comportamento original)
        result = await signInManager.TwoFactorAuthenticatorSignInAsync(
            code, isPersistent: false, rememberClient: rememberMachine);
    }

    if (result.Succeeded)
        return Results.Redirect("/");
    if (result.IsLockedOut)
        return Results.Redirect("/account/login?error=locked");

    return Results.Redirect("/account/mfa-verify?error=invalid");
});
```

#### 3.6 — Verificação
1. Compilar a aplicação
2. Habilitar MFA por TOTP → deve funcionar como antes
3. Desabilitar MFA → habilitar por E-mail → deve enviar código para o e-mail
4. Fazer login com MFA por E-mail → deve receber código no e-mail → digitar → entrar
5. Botão "Reenviar" deve enviar novo código

---

## 🔧 FEATURE 4 — Ignorar MFA em Modo Debug

### Objetivo
Quando a aplicação estiver rodando em ambiente de **Development**, o MFA pode ser ignorado (skip). Isso facilita o desenvolvimento sem precisar digitar códigos constantemente.

### O que precisa ser feito

#### 4.1 — Modificar o endpoint `/account/do-login` no `Program.cs`

Quando `result.RequiresTwoFactor` for `true` **e** o ambiente for `Development`, fazer login direto sem pedir MFA:

```csharp
app.MapPost("/account/do-login", async (HttpContext context,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
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
        // Em modo debug, pular MFA
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
```

> **IMPORTANTE**: `IWebHostEnvironment` já está disponível no container de DI e pode ser injetado diretamente no endpoint.

#### 4.2 — Adicionar aviso visual (opcional)

Na `MfaSetup.razor`, quando em modo Development, exibir um aviso:

```razor
@inject IWebHostEnvironment Env

@if (Env.IsDevelopment())
{
    <div class="alert alert-info mb-4">
        <i class="bi bi-info-circle"></i> <strong>Modo de desenvolvimento:</strong>
        O MFA será automaticamente ignorado durante o login neste ambiente.
    </div>
}
```

#### 4.3 — Verificação
1. Compilar
2. Em modo Development: habilitar MFA → fazer login → deve pular MFA e ir direto para /
3. Em modo Production: habilitar MFA → fazer login → deve pedir código normalmente

---

## 🔧 FEATURE 5 — Revisão de Arquitetura e Melhorias

### Objetivo
Revisar a arquitetura do projeto e identificar pontos de melhoria. Um plano detalhado já foi criado no arquivo `REFACTOR_PLAN.md`.

### Resumo do Plano (já existente no `REFACTOR_PLAN.md`)

O plano completo já foi criado e está no arquivo `REFACTOR_PLAN.md` na raiz do projeto. Ele contém **7 fases** detalhadas:

| Fase | Objetivo | Complexidade |
|------|---------|-------------|
| 1 | Criar `Helpers/UiHelpers.cs` — eliminar duplicação de `GetMethodColor`, `GetStatusColor`, `FormatJson` em 6+ arquivos | Média |
| 2 | Extrair code-behind (`.razor.cs`) para 10+ páginas — separar HTML de lógica C# | Alta |
| 3 | Limpar `Program.cs` (288→~50 linhas) — extrair para `Extensions/` e `Endpoints/` | Média |
| 4 | Eliminar CSS/JS inline de `.razor` e `.cshtml` — usar classes CSS em arquivos separados | Baixa |
| 5 | Corrigir namespace de `Pages/_Imports.razor` (`Shared` → `Pages`) | Baixa |
| 6 | Tornar `MockService` totalmente assíncrono — eliminar `.GetAwaiter().GetResult()` | Alta |
| 7 | Verificação final — build, checklist de inline, duplicação, testes | — |

### Pontos Adicionais de Melhoria (novos, além do REFACTOR_PLAN.md)

#### 5.1 — Separação de concerns no `Register.razor`
O `Register.razor` injeta `MockDbContext` diretamente para verificar alias. O ideal seria criar um método no `UserManager` ou um `IUserAliasService` para encapsular essa lógica. Isso vale também para o `Settings.razor` que agora edita alias.

**Sugestão**: Criar `Services/AliasService.cs`:
```csharp
namespace Savio.MockServer.Services;

public class AliasService
{
    private readonly MockDbContext _context;

    public AliasService(MockDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsAliasAvailableAsync(string alias, string? excludeUserId = null)
    {
        return !await _context.Users
            .AnyAsync(u => u.Alias == alias && (excludeUserId == null || u.Id != excludeUserId));
    }

    public bool IsValidAliasFormat(string alias)
    {
        return Regex.IsMatch(alias, @"^[a-z0-9][a-z0-9_-]{1,48}[a-z0-9]$");
    }
}
```

Registrar em `Program.cs`: `builder.Services.AddScoped<AliasService>();`

#### 5.2 — Usar `User Secrets` para SMTP em desenvolvimento

Em vez de colocar senhas de SMTP no `appsettings.json` (que pode ser commitado), usar `dotnet user-secrets`:
```bash
dotnet user-secrets init
dotnet user-secrets set "Email:SmtpHost" "smtp.gmail.com"
dotnet user-secrets set "Email:SmtpPort" "587"
dotnet user-secrets set "Email:SmtpUser" "seu-email@gmail.com"
dotnet user-secrets set "Email:SmtpPass" "sua-senha-de-app"
dotnet user-secrets set "Email:FromEmail" "seu-email@gmail.com"
```

#### 5.3 — Validação SMTP na inicialização

Adicionar log de aviso na inicialização quando SMTP não está configurado:
```csharp
// Após o build do app, verificar SMTP
var smtpHost = app.Configuration["Email:SmtpHost"];
if (string.IsNullOrWhiteSpace(smtpHost))
{
    app.Logger.LogWarning("⚠️ SMTP não configurado. E-mails de confirmação serão logados no console e contas serão auto-confirmadas.");
}
```

#### 5.4 — Executar o REFACTOR_PLAN.md

Para executar o plano de refatoração completo, siga as instruções detalhadas no arquivo `REFACTOR_PLAN.md`. Cada fase é independente e o build deve passar ao final de cada uma.

**ORDEM**: Execute as Features 1-4 primeiro (são funcionais), depois o REFACTOR_PLAN.md (é estrutural). Assim o refactoring já incluirá o código novo das features.

---

## 🔧 FEATURE 6 — Arquivo `README.md` para o Repositório Git

### Objetivo
Criar um `README.md` completo e profissional para o repositório.

### O que precisa ser feito

Criar o arquivo `README.md` na raiz do projeto com o conteúdo definido abaixo. O README será criado como um arquivo separado junto com este documento.

**Veja o arquivo `README.md` que foi criado junto com este guia.**

---

## ✅ CHECKLIST FINAL

Após implementar todas as features, verificar:

- [ ] **SMTP**: E-mail de confirmação é enviado quando SMTP está configurado
- [ ] **SMTP**: Conta é auto-confirmada quando SMTP não está configurado
- [ ] **SMTP**: Login é bloqueado para contas não confirmadas
- [ ] **Alias**: Pode ser editado em Settings com validação de formato
- [ ] **Alias**: Não permite aliases duplicados (unicidade)
- [ ] **Alias**: As rotas de mock respondem no novo alias após alteração
- [ ] **Alias**: Aviso visual sobre impacto da mudança de alias
- [ ] **MFA E-mail**: É possível habilitar MFA por e-mail
- [ ] **MFA E-mail**: Código é enviado por e-mail durante login
- [ ] **MFA E-mail**: Botão de reenvio funciona
- [ ] **MFA E-mail**: TOTP continua funcionando como antes
- [ ] **MFA Debug**: Em Development, MFA é automaticamente pulado no login
- [ ] **MFA Debug**: Em Production, MFA funciona normalmente
- [ ] **README**: Arquivo README.md existe e está completo
- [ ] **Build**: Compilação sem erros
- [ ] **Arquitetura**: REFACTOR_PLAN.md revisado e pronto para execução

---

## 📝 ORDEM DE EXECUÇÃO RECOMENDADA

1. **Feature 1** — SMTP (configuração + ajustes no SmtpEmailSender)
2. **Feature 2** — Alias editável (Settings.razor)
3. **Feature 3** — MFA por E-mail (ApplicationUser + MfaSetup + MfaVerify + Program.cs)
4. **Feature 4** — MFA skip em debug (Program.cs endpoint)
5. **Feature 6** — README.md
6. **Feature 5** — Refatoração arquitetural (seguir REFACTOR_PLAN.md)

> **REGRA DE OURO**: Faça build ao final de cada feature. Só prossiga se compilar sem erros.
