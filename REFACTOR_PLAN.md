# 🏗️ PLANO DE REFATORAÇÃO — Savio.MockServer

## DIAGNÓSTICO COMPLETO

### Problemas Identificados

| # | Problema | Gravidade | Onde |
|---|---------|-----------|------|
| 1 | `GetMethodColor()` duplicado em **6 arquivos** | Alta | Index, Mocks, MockGroupEditor, Historico, HistoricoDetalhes, UnmockedRequests |
| 2 | `GetStatusColor()` duplicado em **5 arquivos** | Alta | Index, Mocks, MockGroupEditor, Historico, HistoricoDetalhes |
| 3 | Lógica de autenticação (pegar userId do AuthState) repetida em **6 arquivos** | Alta | Index, Mocks, MockEditor, MockGroupEditor, MfaSetup, Settings |
| 4 | `Mocks.razor` tem **785 linhas** — HTML + lógica de ordenação + filtro + batch + alertas tudo junto | Crítica | Pages/Mocks.razor |
| 5 | `HistoricoDetalhes.razor` tem **484 linhas** — HTML + FormatJson + download blob + helpers tudo junto | Alta | Pages/HistoricoDetalhes.razor |
| 6 | `MockEditor.razor` — lógica de alias stripping, upload binário, multipart junto do HTML | Alta | Pages/MockEditor.razor |
| 7 | `Program.cs` tem **288 linhas** — DB, Identity, CORS, middleware, endpoints auth, health, logging tudo junto | Alta | Program.cs |
| 8 | `MockService` usa `.GetAwaiter().GetResult()` em **todos** os métodos — anti-pattern em Blazor Server | Alta | Services/MockService.cs |
| 9 | `Historico.razor` injeta `MockDbContext` diretamente (linha 8), violando a camada de repositório | Média | Pages/Historico.razor |
| 10 | `Register.razor` injeta `MockDbContext` diretamente (linha 14), violando a camada de repositório | Média | Pages/Account/Register.razor |
| 11 | `_Host.cshtml` tem script inline (tema) | Baixa | Pages/_Host.cshtml |
| 12 | `AuthLayout.razor` tem style inline | Baixa | Shared/AuthLayout.razor |
| 13 | Vários Razor pages com atributos `style` inline (Login, MfaSetup, Register, MockEditor, HistoricoDetalhes) | Baixa | Vários |
| 14 | `Pages\_Imports.razor` com `@namespace Savio.MockServer.Shared` — namespace errado para Pages | Média | Pages/_Imports.razor |
| 15 | Modelo `MockEndpoint` tem propriedade `FileName` aparentemente sem uso | Baixa | Models/MockEndpoint.cs |
| 16 | `FormatJson()` duplicado em `HistoricoDetalhes` — poderia ser helper centralizado | Média | Pages/HistoricoDetalhes.razor |

---

## ESTRUTURA ATUAL

```
Savio.MockServer/
├── Data/
│   ├── Entities/              ← OK (5 entidades bem separadas)
│   │   ├── ApplicationUser.cs
│   │   ├── MockEndpointEntity.cs
│   │   ├── RequestHistoryEntity.cs
│   │   ├── UnmockedRequestEntity.cs
│   │   ├── MockGroupEntity.cs
│   │   └── MockBinaryBlobEntity.cs
│   ├── Repositories/          ← OK (4 interfaces + 4 implementações)
│   └── MockDbContext.cs       ← OK
├── Models/                    ← OK (7 classes)
├── Services/                  ← PROBLEMA: MockService é síncrono
│   ├── MockService.cs
│   ├── HttpRequestCaptureService.cs
│   ├── IMockBinaryStorage.cs
│   ├── MockBinaryStorage.cs
│   ├── MultipartResponseWriter.cs
│   └── SmtpEmailSender.cs
├── Middleware/                 ← OK
│   ├── MockEndpointMiddleware.cs
│   └── RequestHistoryMiddleware.cs
├── Pages/                     ← PROBLEMA PRINCIPAL: lógica misturada com HTML
│   ├── Account/               (7 páginas com lógica no @code)
│   ├── Index.razor            (244 linhas — código duplicado)
│   ├── Mocks.razor            (785 linhas! — monolítico)
│   ├── MockEditor.razor       (~500 linhas — lógica complexa)
│   ├── MockGroupEditor.razor  (311 linhas)
│   ├── Historico.razor        (347 linhas — injeta DbContext)
│   ├── HistoricoDetalhes.razor(484 linhas — helpers duplicados)
│   ├── UnmockedRequests.razor (151 linhas)
│   ├── About.razor            (74 linhas — OK)
│   ├── _Host.cshtml           (script inline)
│   └── _Imports.razor         (namespace errado)
├── Shared/                    ← PROBLEMA: style inline
├── Components/                ← OK (3 componentes pequenos)
├── wwwroot/
│   ├── css/                   ← OK (5 arquivos bem separados)
│   ├── js/                    ← OK (1 arquivo)
│   └── lib/                   ← OK
├── Program.cs                 ← PROBLEMA: 288 linhas monolíticas
└── Savio.MockServer.csproj    ← OK
```

## ESTRUTURA ALVO

```
Savio.MockServer/
├── Data/                       ← SEM MUDANÇA
├── Models/                     ← SEM MUDANÇA
├── Services/
│   ├── MockService.cs          ← REFATORADO: métodos async
│   └── ... (demais inalterados)
├── Middleware/                  ← SEM MUDANÇA
├── Helpers/
│   └── UiHelpers.cs            ← NOVO: GetMethodColor, GetStatusColor, FormatJson
├── Extensions/
│   ├── DatabaseExtensions.cs   ← NOVO: configuração de banco extraída do Program.cs
│   ├── IdentityExtensions.cs   ← NOVO: configuração de Identity extraída do Program.cs
│   └── ServiceExtensions.cs    ← NOVO: registro de serviços extraído do Program.cs
├── Endpoints/
│   └── AuthEndpoints.cs        ← NOVO: endpoints /account/do-login etc.
├── Pages/
│   ├── Account/
│   │   ├── Login.razor         ← REFATORADO: sem style inline
│   │   ├── Register.razor      ← REFATORADO: code-behind
│   │   ├── Register.razor.cs   ← NOVO: lógica extraída
│   │   └── ... (demais com code-behind)
│   ├── Index.razor             ← REFATORADO: só HTML
│   ├── Index.razor.cs          ← NOVO: lógica extraída
│   ├── Mocks.razor             ← REFATORADO: só HTML
│   ├── Mocks.razor.cs          ← NOVO: lógica extraída
│   ├── ... (padrão code-behind para todas)
│   ├── _Host.cshtml            ← REFATORADO: sem script inline
│   └── _Imports.razor          ← CORRIGIDO: namespace correto
├── Shared/
│   ├── MainLayout.razor        ← SEM MUDANÇA
│   ├── AuthLayout.razor        ← REFATORADO: sem style inline
│   └── RedirectToLogin.razor   ← SEM MUDANÇA
├── Components/                  ← SEM MUDANÇA
├── wwwroot/
│   ├── css/
│   │   ├── theme.css           ← SEM MUDANÇA
│   │   ├── layout.css          ← EDITADO: +classes para AuthLayout
│   │   ├── components.css      ← EDITADO: +classes para inline styles removidos
│   │   ├── utilities.css       ← SEM MUDANÇA
│   │   └── site.css            ← SEM MUDANÇA
│   ├── js/app.js               ← SEM MUDANÇA (script inline já está aqui)
│   └── lib/                    ← SEM MUDANÇA
├── Program.cs                   ← REFATORADO: ~50 linhas
└── Savio.MockServer.csproj     ← SEM MUDANÇA
```

---

# FASES DE EXECUÇÃO

---

## FASE 1 — Criar `Helpers/UiHelpers.cs` (eliminar duplicação)

### Objetivo
Eliminar as 6 cópias de `GetMethodColor()` e 5 cópias de `GetStatusColor()`, mais `FormatJson()`.

### Passo 1.1 — Criar o arquivo

Criar o arquivo `Helpers/UiHelpers.cs` com o conteúdo:

```csharp
namespace Savio.MockServer.Helpers;

public static class UiHelpers
{
    public static string GetMethodColor(string method) => method.ToUpper() switch
    {
        "GET" => "primary",
        "POST" => "success",
        "PUT" => "warning",
        "DELETE" => "danger",
        "PATCH" => "info",
        _ => "secondary"
    };

    public static string GetStatusColor(int status) => status switch
    {
        >= 200 and < 300 => "success",
        >= 300 and < 400 => "info",
        >= 400 and < 500 => "warning",
        >= 500 => "danger",
        _ => "secondary"
    };

    public static string FormatJson(string json)
    {
        try
        {
            var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
            return System.Text.Json.JsonSerializer.Serialize(jsonDoc, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }
}
```

### Passo 1.2 — Adicionar using global

No arquivo `_Imports.razor` (raiz), adicionar a linha:
```razor
@using Savio.MockServer.Helpers
```

O arquivo `_Imports.razor` final fica:
```razor
@using System.Net.Http
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using Savio.MockServer
@using Savio.MockServer.Shared
@using Savio.MockServer.Components
@using Savio.MockServer.Helpers
@using Blazored.Modal
@using Blazored.Modal.Services
@attribute [Authorize]
```

### Passo 1.3 — Substituir em cada arquivo

Em **CADA** arquivo abaixo, apagar o método local `GetMethodColor` (e `GetStatusColor` se existir) do bloco `@code`, e no HTML trocar as chamadas:

**ATENÇÃO**: Trocar `GetMethodColor(...)` por `UiHelpers.GetMethodColor(...)` e `GetStatusColor(...)` por `UiHelpers.GetStatusColor(...)`.

Arquivos a alterar:
1. `Pages/Index.razor` — Apagar linhas 226-243 (ambos métodos). No HTML trocar chamadas.
2. `Pages/Mocks.razor` — Apagar linhas 767-784 (ambos métodos). No HTML trocar chamadas.
3. `Pages/MockGroupEditor.razor` — Apagar linhas 293-310 (ambos métodos). No HTML trocar chamadas.
4. `Pages/Historico.razor` — Apagar linhas 329-346 (ambos métodos). No HTML trocar chamadas.
5. `Pages/HistoricoDetalhes.razor` — Apagar linhas 352-382 (ambos métodos + FormatJson). No HTML trocar `FormatJson(...)` por `UiHelpers.FormatJson(...)`.
6. `Pages/UnmockedRequests.razor` — Apagar linhas 142-150 (GetMethodColor). No HTML trocar chamadas.

**Verificação**: Build deve compilar sem erros. Nenhum arquivo deve mais conter método privado chamado `GetMethodColor`, `GetStatusColor` ou `FormatJson`.

---

## FASE 2 — Extrair code-behind das Pages principais

### Objetivo
Separar o HTML (template) da lógica C# usando o padrão code-behind. O `.razor` fica com só o HTML/Blazor markup, o `.razor.cs` fica com toda a lógica.

### Regra geral do code-behind

Para cada `NomePagina.razor`:
- Criar `NomePagina.razor.cs` como `partial class`
- A classe deve herdar de `ComponentBase` (ou `LayoutComponentBase` se for layout)
- Mover **todo** o bloco `@code { ... }` para o `.razor.cs`
- No `.razor`, remover o bloco `@code` inteiro
- Trocar `@inject` no `.razor` por `[Inject]` no `.razor.cs`
- Trocar `[CascadingParameter]` — já são atributos, ficam no `.razor.cs`
- Trocar `@using` por `using` no `.razor.cs`
- Os campos/propriedades que eram `private` no `@code` devem virar `protected` (para o .razor acessar)

### Passo 2.1 — Index.razor

**Criar** `Pages/Index.razor.cs`:
```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using Blazored.Modal;
using Blazored.Modal.Services;
using Savio.MockServer.Components;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Models;
using Savio.MockServer.Services;

namespace Savio.MockServer.Pages;

public partial class Index : ComponentBase
{
    [Inject] protected MockService MockService { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;
    [Inject] protected UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [CascadingParameter]
    protected IModalService Modal { get; set; } = default!;

    [CascadingParameter]
    protected Task<AuthenticationState>? AuthState { get; set; }

    protected List<MockEndpoint> mocks = new();
    protected string? currentUserId;
    protected string? currentAlias;

    protected override async Task OnInitializedAsync()
    {
        if (AuthState != null)
        {
            var authState = await AuthState;
            var user = await UserManager.GetUserAsync(authState.User);
            currentUserId = user?.Id;
            currentAlias = user?.Alias;
        }
        LoadMocks();
    }

    protected void LoadMocks()
    {
        mocks = MockService.GetAllMocks(currentUserId);
    }

    protected void NavigateToCreate()
    {
        Navigation.NavigateTo("/mock/create");
    }

    protected void NavigateToEdit(string id)
    {
        Navigation.NavigateTo($"/mock/edit/{id}");
    }

    protected async Task DeleteMock(string id)
    {
        var parameters = new ModalParameters
        {
            { nameof(ConfirmDialog.Message), "Tem certeza que deseja excluir este mock?" },
            { nameof(ConfirmDialog.Icon), "bi-trash" },
            { nameof(ConfirmDialog.IconColor), "danger" }
        };
        var options = new ModalOptions { Size = ModalSize.Small };
        var modal = Modal.Show<ConfirmDialog>("Confirmar Exclusão", parameters, options);
        var result = await modal.Result;

        if (!result.Cancelled)
        {
            MockService.DeleteMock(id);
            LoadMocks();
            StateHasChanged();
        }
    }

    protected void DuplicateMock(string id)
    {
        MockService.DuplicateMock(id);
        LoadMocks();
        StateHasChanged();
    }

    protected void ViewHistory(string mockId)
    {
        if (int.TryParse(mockId, out int numericId))
        {
            Navigation.NavigateTo($"/historico?mockId={numericId}");
        }
    }

    protected async Task TestMock(MockEndpoint mock)
    {
        var aliasPrefix = !string.IsNullOrEmpty(currentAlias) ? $"/{currentAlias}" : string.Empty;
        var url = $"http://localhost:5100{aliasPrefix}{mock.Route}";
        await JS.InvokeVoidAsync("window.open", url, "_blank");
    }
}
```

**Editar** `Pages/Index.razor` — remover TODO o bloco `@code { ... }` (linhas 146 até 244). Remover todas as linhas `@using` e `@inject` do topo (já estão no .cs). Manter apenas `@page "/"` e o HTML.

O `Pages/Index.razor` deve ficar assim (apenas markup):
```razor
@page "/"

<PageTitle>Mock Server - Dashboard</PageTitle>
<div class="container-fluid py-4">
    @* ... todo o HTML inalterado, mas usando UiHelpers.GetMethodColor/GetStatusColor ...  *@
</div>
```

**IMPORTANTE**: As chamadas no HTML que antes eram `GetMethodColor(...)` agora devem ser `UiHelpers.GetMethodColor(...)` (se não trocou na Fase 1, trocar agora).

### Passo 2.2 — Mocks.razor

Mesmo padrão. Criar `Pages/Mocks.razor.cs` com **toda** a lógica do `@code` (linhas 361-785).
Remover `@code` do `.razor`. Remover `@using` e `@inject` do `.razor` (movê-los como `using` e `[Inject]` no `.cs`).

A classe deve ser:
```csharp
namespace Savio.MockServer.Pages;

public partial class Mocks : ComponentBase
{
    // ... [Inject], [CascadingParameter], campos, métodos
    // Todos os campos que eram private devem virar protected
}
```

### Passo 2.3 — Historico.razor

Mesmo padrão. Criar `Pages/Historico.razor.cs`.

**IMPORTANTE**: Este arquivo injeta `MockDbContext _context` diretamente (linha 8). Na refatoração:
- **Remover** a injeção de `MockDbContext`
- Usar `IMockRepository` para buscar o mock por ID (linha 222: `var mock = await _context.MockEndpoints.FindAsync(MockId.Value);` → `var mock = await MockRepo.GetByIdAsync(MockId.Value);`)
- Adicionar `[Inject] protected IMockRepository MockRepo { get; set; } = default!;`

### Passo 2.4 — HistoricoDetalhes.razor

Mesmo padrão. Criar `Pages/HistoricoDetalhes.razor.cs`.

**LEMBRETE**: `FormatJson()` já foi extraído para `UiHelpers` na Fase 1. O `CopyToClipboard`, `IsTextContent`, `GetTextPreview`, `FormatFileSize`, `LoadResponseBlobContent`, `DownloadResponseBlob`, etc. vão para o code-behind.

### Passo 2.5 — MockEditor.razor

Mesmo padrão. Criar `Pages/MockEditor.razor.cs`.

### Passo 2.6 — MockGroupEditor.razor

Mesmo padrão. Criar `Pages/MockGroupEditor.razor.cs`.

### Passo 2.7 — UnmockedRequests.razor

Mesmo padrão. Criar `Pages/UnmockedRequests.razor.cs`.

### Passo 2.8 — Account pages (Register, Settings, MfaSetup)

Mesmo padrão para as 3 páginas com lógica relevante:
- `Pages/Account/Register.razor` + `Pages/Account/Register.razor.cs`
- `Pages/Account/Settings.razor` + `Pages/Account/Settings.razor.cs`
- `Pages/Account/MfaSetup.razor` + `Pages/Account/MfaSetup.razor.cs`

**IMPORTANTE para Register.razor**: Injeta `MockDbContext DbContext` diretamente (linha 14) para verificar alias duplicado. Na refatoração, usar o `UserManager` que já tem acesso a usuários, ou criar método no repositório. Para simplificar:
- Manter temporariamente a injeção de `MockDbContext` no code-behind. A correção ideal seria criar `IUserRepository` mas foge do escopo.

Páginas que NÃO precisam de code-behind (são muito pequenas):
- `Login.razor` — é basicamente um form HTML estático
- `Logout.razor` — 11 linhas total
- `MfaVerify.razor` — é basicamente um form HTML estático
- `RegisterConfirmation.razor` — 43 linhas, trivial
- `ConfirmEmail.razor` — 69 linhas, leve

**Verificação**: Build deve compilar. Cada `.razor` deve ter ZERO linhas de `@code`. Toda lógica C# está nos `.razor.cs`.

---

## FASE 3 — Limpar `Program.cs` (extrair extensões e endpoints)

### Objetivo
Reduzir `Program.cs` de ~288 linhas para ~50 linhas extraindo configurações para extension methods.

### Passo 3.1 — Criar `Extensions/DatabaseExtensions.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Savio.MockServer.Data;

namespace Savio.MockServer.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var dbProvider = configuration["Database:Provider"] ?? "SQLite";
        var connectionString = configuration[$"Database:ConnectionStrings:{dbProvider}"];

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"Connection string para o provider '{dbProvider}' não encontrada.");
        }

        services.AddDbContext<MockDbContext>(options =>
        {
            switch (dbProvider.ToUpper())
            {
                case "SQLITE":
                    options.UseSqlite(connectionString);
                    break;
                case "MYSQL":
                    var serverVersion = ServerVersion.AutoDetect(connectionString);
                    options.UseMySql(connectionString, serverVersion);
                    break;
                case "SQLSERVER":
                    options.UseSqlServer(connectionString);
                    break;
                default:
                    throw new InvalidOperationException($"Provider de banco de dados '{dbProvider}' não suportado. Use: SQLite, MySQL ou SQLServer");
            }
        });

        return services;
    }

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        var dbProvider = configuration["Database:Provider"] ?? "SQLite";
        var connectionString = configuration[$"Database:ConnectionStrings:{dbProvider}"];

        app.Logger.LogInformation("Configurando banco de dados: {Provider}", dbProvider);
        app.Logger.LogInformation("Connection String: {ConnectionString}",
            connectionString!.Length > 50 ? connectionString[..50] + "..." : connectionString);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MockDbContext>();

        if (string.Equals(dbProvider, "SQLite", StringComparison.OrdinalIgnoreCase))
        {
            app.Logger.LogInformation("Aplicando migrations (SQLite)...");
            await db.Database.MigrateAsync();

            try
            {
                await db.Database.ExecuteSqlRawAsync("SELECT ResponseBinaryBlobId FROM RequestHistory LIMIT 0");
                await db.Database.ExecuteSqlRawAsync("SELECT Id FROM AspNetUsers LIMIT 0");
            }
            catch
            {
                app.Logger.LogWarning("Schema corrompido detectado. Recriando banco de dados...");
                await db.Database.EnsureDeletedAsync();
                await db.Database.MigrateAsync();
                app.Logger.LogInformation("Banco de dados recriado com sucesso.");
            }
        }
        else
        {
            app.Logger.LogInformation("Criando schema via EnsureCreated ({Provider})...", dbProvider);
            await db.Database.EnsureCreatedAsync();
        }

        app.Logger.LogInformation("Banco de dados configurado com sucesso!");
    }
}
```

### Passo 3.2 — Criar `Extensions/IdentityExtensions.cs`

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Savio.MockServer.Data;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Services;

namespace Savio.MockServer.Extensions;

public static class IdentityExtensions
{
    public static IServiceCollection AddAppIdentity(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.SignIn.RequireConfirmedEmail = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireDigit = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<MockDbContext>()
        .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/account/login";
            options.LogoutPath = "/account/logout";
            options.AccessDeniedPath = "/account/login";
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
        });

        services.AddTransient<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
```

### Passo 3.3 — Criar `Extensions/ServiceExtensions.cs`

```csharp
using Blazored.Modal;
using Blazored.Toast;
using Savio.MockServer.Data.Repositories;
using Savio.MockServer.Services;

namespace Savio.MockServer.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // Repositórios
        services.AddScoped<IMockRepository, MockRepository>();
        services.AddScoped<IRequestHistoryRepository, RequestHistoryRepository>();
        services.AddScoped<IUnmockedRequestRepository, UnmockedRequestRepository>();
        services.AddScoped<IMockGroupRepository, MockGroupRepository>();

        // Serviços
        services.AddScoped<MockService>();
        services.AddScoped<IMockBinaryStorage, MockBinaryStorage>();

        // Bibliotecas de terceiros
        services.AddBlazoredModal();
        services.AddBlazoredToast();

        return services;
    }
}
```

### Passo 3.4 — Criar `Endpoints/AuthEndpoints.cs`

```csharp
using Microsoft.AspNetCore.Identity;
using Savio.MockServer.Data.Entities;

namespace Savio.MockServer.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/account/do-login", async (HttpContext context,
            SignInManager<ApplicationUser> signInManager) =>
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
                return Results.Redirect($"/account/mfa-verify?rememberMe={rememberMe}");
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
            SignInManager<ApplicationUser> signInManager) =>
        {
            var form = await context.Request.ReadFormAsync();
            var code = form["code"].ToString().Replace(" ", "").Replace("-", "");
            var rememberMachine = form["rememberMachine"] == "true";

            if (string.IsNullOrWhiteSpace(code))
                return Results.Redirect("/account/mfa-verify?error=invalid");

            var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
                code, isPersistent: false, rememberClient: rememberMachine);

            if (result.Succeeded)
                return Results.Redirect("/");
            if (result.IsLockedOut)
                return Results.Redirect("/account/login?error=locked");

            return Results.Redirect("/account/mfa-verify?error=invalid");
        });

        return app;
    }
}
```

### Passo 3.5 — Criar `Endpoints/HealthEndpoints.cs`

```csharp
using Savio.MockServer.Services;

namespace Savio.MockServer.Endpoints;

public static class HealthEndpoints
{
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/_health", async (HttpContext context) =>
        {
            using var scope = context.RequestServices.CreateScope();
            var mockService = scope.ServiceProvider.GetRequiredService<MockService>();
            var mocks = mockService.GetAllMocks();

            return Results.Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                mocksCount = mocks.Count
            });
        });

        return app;
    }

    public static void LogStartupInfo(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var mockService = scope.ServiceProvider.GetRequiredService<MockService>();
        var mocks = mockService.GetAllMocks();

        app.Logger.LogInformation("=".PadRight(60, '='));
        app.Logger.LogInformation("🚀 Savio Mock Server iniciado!");
        app.Logger.LogInformation("📊 Total de mocks carregados: {Count}", mocks.Count);

        foreach (var mock in mocks.Where(m => m.IsActive))
        {
            app.Logger.LogInformation("  ✅ {Method} {Route} -> {StatusCode}",
                mock.Method, mock.Route, mock.StatusCode);
        }

        app.Logger.LogInformation("=".PadRight(60, '='));
    }
}
```

### Passo 3.6 — Reescrever `Program.cs`

O `Program.cs` final deve ficar assim:

```csharp
using System.Text;
using Savio.MockServer.Extensions;
using Savio.MockServer.Endpoints;
using Savio.MockServer.Middleware;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
});

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddAppIdentity();
builder.Services.AddAppServices();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    options.MaxBufferedUnacknowledgedRenderBatches = 10;
    options.DetailedErrors = builder.Environment.IsDevelopment();
})
.AddHubOptions(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
    options.EnableDetailedErrors = true;
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

await app.MigrateDatabaseAsync();

app.UseCors();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<MockEndpointMiddleware>();
app.UseMiddleware<RequestHistoryMiddleware>();

app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.LogStartupInfo();

await app.RunAsync();
```

**Verificação**: Build deve compilar. A aplicação deve funcionar exatamente como antes.

---

## FASE 4 — Eliminar styles e scripts inline

### Objetivo
Remover todo CSS inline e JavaScript inline dos arquivos `.razor` e `.cshtml`.

### Passo 4.1 — Mover script inline de `_Host.cshtml`

No `_Host.cshtml`, **remover** o bloco de script inline (linhas 35-38):
```html
<script>
    (function(){var t=localStorage.getItem('savio-theme');if(t)document.documentElement.setAttribute('data-theme',t);})();
</script>
```

Este código já está duplicado no `wwwroot/js/app.js` (linhas 151-156, função `applyStoredTheme`). Ele roda automaticamente. Porém, o `app.js` é carregado no final do body. Para evitar o flash de tema, mover a chamada para o `<head>` criando um script mínimo separado.

Criar `wwwroot/js/theme-init.js`:
```javascript
(function(){var t=localStorage.getItem('savio-theme');if(t)document.documentElement.setAttribute('data-theme',t);})();
```

No `_Host.cshtml`, substituir o bloco `<script>` inline por:
```html
<script src="js/theme-init.js"></script>
```

### Passo 4.2 — Remover style inline de `AuthLayout.razor`

O `AuthLayout.razor` atual tem:
```razor
<div class="d-flex justify-content-center align-items-center" style="min-height: 100vh; background: linear-gradient(135deg, var(--bg-dark) 0%, var(--bg-dark-secondary) 100%);">
```

Adicionar ao **final** de `wwwroot/css/layout.css`:
```css
/* ========================================
   AUTH LAYOUT
   ======================================== */

.auth-page {
    min-height: 100vh;
    background: linear-gradient(135deg, var(--bg-dark) 0%, var(--bg-dark-secondary) 100%);
}
```

Editar `AuthLayout.razor`:
```razor
@inherits LayoutComponentBase

<div class="d-flex justify-content-center align-items-center auth-page">
    @Body
</div>
```

### Passo 4.3 — Remover styles inline das Account pages

Nos arquivos `Login.razor`, `Register.razor`, `MfaVerify.razor`, `RegisterConfirmation.razor`, `ConfirmEmail.razor`, existe este style inline:
```html
style="width: 100%; max-width: 420px;"
```
ou
```html
style="width: 100%; max-width: 480px;"
```

Adicionar ao **final** de `wwwroot/css/components.css`:
```css
/* ========================================
   AUTH CARDS
   ======================================== */

.auth-card {
    width: 100%;
    max-width: 420px;
}

.auth-card-wide {
    width: 100%;
    max-width: 480px;
}
```

Nos `.razor`, trocar:
- `<div class="card shadow" style="width: 100%; max-width: 420px;">` → `<div class="card shadow auth-card">`
- `<div class="card shadow" style="width: 100%; max-width: 480px;">` → `<div class="card shadow auth-card-wide">`

Arquivos afetados:
- `Login.razor` (420px → `auth-card`)
- `Register.razor` (480px → `auth-card-wide`)
- `MfaVerify.razor` (420px → `auth-card`)
- `RegisterConfirmation.razor` (420px → `auth-card`)
- `ConfirmEmail.razor` (420px → `auth-card`)

### Passo 4.4 — Remover styles inline restantes em outras pages

Localizar qualquer `style="..."` dentro dos `.razor` e `.cshtml` (exceto atributos em `<input>` de tipo como `maxlength`). Se houver, criar classe CSS em `components.css` e substituir.

Exemplos que existem no código:
- `MfaSetup.razor` linha 66-67: `style="max-width:200px;"` no input de código → Criar `.mfa-code-input { max-width: 200px; }`
- `Settings.razor` linha 30: `style="width:200px;"` → Criar `.settings-label { width: 200px; }`
- `Historico.razor` inline width no select → Criar `.page-size-select { width: auto; display: inline-block; }`
- `HistoricoDetalhes.razor` linhas com `style="max-height: 400px; overflow: auto;"` → Criar `.code-preview { max-height: 400px; overflow: auto; }` e `.code-preview-lg { max-height: 500px; overflow: auto; }`
- `HistoricoDetalhes.razor` linha 64: `style="width: 180px;"` → Criar `.detail-label { width: 180px; }`

Adicionar ao `components.css`:
```css
/* ========================================
   FORMULÁRIOS E DETALHES
   ======================================== */

.mfa-code-input {
    max-width: 200px;
}

.settings-label {
    width: 200px;
}

.page-size-select {
    width: auto;
    display: inline-block;
}

.code-preview {
    max-height: 400px;
    overflow: auto;
}

.code-preview-lg {
    max-height: 500px;
    overflow: auto;
}

.code-preview-sm {
    max-height: 200px;
    overflow: auto;
}

.detail-label {
    width: 180px;
}

.mock-actions-col {
    width: 200px;
}

.group-actions-col {
    width: 120px;
}

.available-mock-actions-col {
    width: 80px;
}

.history-actions-col {
    width: 140px;
}
```

**Verificação**: Buscar por `style="` em TODOS os `.razor` e `.cshtml`. Não deve haver nenhum style inline restante (exceto dentro de tags `<environment>` ou `<component>`).

---

## FASE 5 — Corrigir `Pages/_Imports.razor`

### Passo 5.1

O arquivo `Pages/_Imports.razor` atual tem:
```razor
@using Microsoft.AspNetCore.Components.Web
@namespace Savio.MockServer.Shared
```

O namespace `Savio.MockServer.Shared` está **errado** para a pasta `Pages`. Deve ser:
```razor
@using Microsoft.AspNetCore.Components.Web
@namespace Savio.MockServer.Pages
```

**ATENÇÃO**: Após essa mudança, é preciso verificar se algum `@page` directive ou referência por namespace é afetado. Os arquivos `.razor.cs` criados na Fase 2 já usam `namespace Savio.MockServer.Pages;`, então eles devem casar corretamente.

---

## FASE 6 — Tornar `MockService` assíncrono

### Objetivo
Eliminar todos os `.GetAwaiter().GetResult()` do `MockService`, que é anti-pattern em Blazor Server e pode causar deadlocks.

### Passo 6.1 — Converter métodos de `MockService`

Cada método síncrono que usa `.GetAwaiter().GetResult()` deve virar `async Task<>` ou `async Task`. São **19 métodos** para converter.

Exemplo de conversão:

**ANTES:**
```csharp
public List<MockEndpoint> GetAllMocks(string? userId = null)
{
    var entities = _repository.GetAllAsync(userId).GetAwaiter().GetResult();
    return entities.Select(EntityToModel).ToList();
}
```

**DEPOIS:**
```csharp
public async Task<List<MockEndpoint>> GetAllMocksAsync(string? userId = null)
{
    var entities = await _repository.GetAllAsync(userId);
    return entities.Select(EntityToModel).ToList();
}
```

Lista completa de métodos para converter (manter os mesmos nomes + sufixo `Async`):

| Método atual | Assinatura nova |
|---|---|
| `GetAllMocks` | `async Task<List<MockEndpoint>> GetAllMocksAsync(string? userId = null)` |
| `GetFilteredMocks` | `async Task<List<MockEndpoint>> GetFilteredMocksAsync(MockFilter filter)` |
| `GetStandaloneMocks` | `async Task<List<MockEndpoint>> GetStandaloneMocksAsync(MockFilter? filter = null)` |
| `GetMocksByGroupId` | `async Task<List<MockEndpoint>> GetMocksByGroupIdAsync(int groupId)` |
| `GetMockById` | `async Task<MockEndpoint?> GetMockByIdAsync(string id)` |
| `AddMock` | `async Task<(bool success, string? error)> AddMockAsync(MockEndpoint mock, string? userId = null)` |
| `UpdateMock` | `async Task<(bool success, string? error)> UpdateMockAsync(MockEndpoint mock, string? userId = null)` |
| `DeleteMock` | `async Task DeleteMockAsync(string id)` |
| `DuplicateMock` | `async Task<(bool success, string? error)> DuplicateMockAsync(string id)` |
| `DuplicateGroup` | `async Task<(bool success, string? error)> DuplicateGroupAsync(int groupId)` |
| `RecordCall` | `async Task RecordCallAsync(string route, string method, string? userId = null)` |
| `SetMockActive` | `async Task<(bool success, string? error)> SetMockActiveAsync(string id, bool isActive)` |
| `CheckActiveConflict` | `async Task<string?> CheckActiveConflictAsync(string route, string method, bool isActive, int? excludeId, string? userId = null)` |
| `GetAllGroups` | `async Task<List<MockGroup>> GetAllGroupsAsync(string? userId = null)` |
| `GetGroupById` | `async Task<MockGroup?> GetGroupByIdAsync(int id)` |
| `AddGroup` | `async Task<(bool success, string? error)> AddGroupAsync(string name, string? description, string? userId = null)` |
| `UpdateGroup` | `async Task<(bool success, string? error)> UpdateGroupAsync(int id, string name, string? description, string? userId = null)` |
| `DeleteGroup` | `async Task DeleteGroupAsync(int id)` |
| `AddMockToGroup` | `async Task AddMockToGroupAsync(string mockId, int groupId)` |
| `RemoveMockFromGroup` | `async Task RemoveMockFromGroupAsync(string mockId)` |
| `ActivateGroupMocks` | `async Task<(bool success, string? error, List<string> conflicts)> ActivateGroupMocksAsync(int groupId)` |
| `DeactivateGroupMocks` | `async Task DeactivateGroupMocksAsync(int groupId)` |

### Passo 6.2 — Atualizar chamadores

**Cada** `.razor.cs` (code-behind criado na Fase 2) que chama `MockService` deve ser atualizado:

**ANTES:**
```csharp
mocks = MockService.GetAllMocks(currentUserId);
```

**DEPOIS:**
```csharp
mocks = await MockService.GetAllMocksAsync(currentUserId);
```

Os métodos que chamam MockService e NÃO eram async devem virar async. Exemplo:

**ANTES:**
```csharp
protected void LoadMocks()
{
    mocks = MockService.GetAllMocks(currentUserId);
}
```

**DEPOIS:**
```csharp
protected async Task LoadMocksAsync()
{
    mocks = await MockService.GetAllMocksAsync(currentUserId);
}
```

Atualizar os nomes nas chamadas correspondentes no HTML (.razor):
- `@onclick="LoadMocks"` → `@onclick="LoadMocksAsync"`

### Passo 6.3 — Atualizar `MockEndpointMiddleware`

O `MockEndpointMiddleware` chama `mockService.GetAllMocks()` e `mockService.RecordCall()`. Trocar por:
- `await mockService.GetAllMocksAsync(...)`
- `await mockService.RecordCallAsync(...)`

### Passo 6.4 — Atualizar `HealthEndpoints.cs`

Trocar `mockService.GetAllMocks()` por `await mockService.GetAllMocksAsync()`.

### Passo 6.5 — Atualizar startup log em `HealthEndpoints.cs`

O `LogStartupInfo` precisa virar `async Task`:
```csharp
public static async Task LogStartupInfoAsync(this WebApplication app)
```

E no `Program.cs`:
```csharp
await app.LogStartupInfoAsync();
```

**Verificação**: Buscar por `.GetAwaiter().GetResult()` em todo o projeto. Não deve haver nenhuma ocorrência. Build deve compilar sem erros.

---

## FASE 7 — Verificação Final

### Passo 7.1 — Build completo
Executar build. Não pode ter erros.

### Passo 7.2 — Checklist de style inline
Buscar `style="` em todos os `.razor` e `.cshtml`. Não deve haver nenhum.

### Passo 7.3 — Checklist de script inline
Buscar `<script>` que não seja `src=` em todos os `.cshtml`. Não deve haver nenhum (exceto referências a `_framework/blazor.server.js` e `js/app.js` e `js/theme-init.js`).

### Passo 7.4 — Checklist de código duplicado
Buscar `GetMethodColor` em todo o projeto. Deve existir APENAS em `Helpers/UiHelpers.cs`.
Buscar `GetStatusColor` em todo o projeto. Deve existir APENAS em `Helpers/UiHelpers.cs`.
Buscar `FormatJson` em todo o projeto. Deve existir APENAS em `Helpers/UiHelpers.cs`.

### Passo 7.5 — Checklist de `@code`
Buscar `@code` em todos os `.razor` nas pastas `Pages/` e `Pages/Account/`. Os que têm code-behind (`.razor.cs`) NÃO devem ter `@code`. Os que não têm code-behind (Login, Logout, MfaVerify, RegisterConfirmation, ConfirmEmail) podem manter `@code` pois são triviais.

### Passo 7.6 — Checklist de GetAwaiter
Buscar `.GetAwaiter().GetResult()` em todo o projeto. Não deve haver nenhum.

### Passo 7.7 — Teste funcional
1. Rodar a aplicação
2. Testar: login, registro, criar mock, listar mocks, testar mock via HTTP, ver histórico, ver detalhes do histórico, criar mock de unmocked request, trocar tema, MFA setup
3. Verificar que a funcionalidade está idêntica

---

## RESUMO DAS FASES

| Fase | Objetivo | Arquivos novos | Arquivos editados |
|------|---------|---------------|-------------------|
| 1 | Eliminar código duplicado (helpers) | 1 | 7 |
| 2 | Code-behind em todas as pages | 10 | 10 |
| 3 | Limpar Program.cs | 5 | 1 |
| 4 | Eliminar CSS/JS inline | 1 | ~12 |
| 5 | Corrigir namespace | 0 | 1 |
| 6 | MockService async | 0 | ~12 |
| 7 | Verificação | 0 | 0 |

**Total**: ~17 arquivos novos, ~30 arquivos editados, 0 arquivos deletados.

---

## ORDEM DE EXECUÇÃO RECOMENDADA

Executar exatamente na ordem das fases (1→2→3→4→5→6→7). Cada fase é independente e o build deve passar ao final de cada uma.

**IMPORTANTE para a IA executora**: Faça build ao final de CADA fase. Se falhar, corrija antes de prosseguir para a próxima. Não pule fases.
