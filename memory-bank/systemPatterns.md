# System Patterns — Savio Mock Server

## Arquitetura Geral

```
Request HTTP
    └── MockEndpointMiddleware
            └── IMockRepository → MockEndpoint
            └── IRequestHistoryRepository → salva histórico
            └── retorna response configurado

Blazor Server (SignalR)
    └── Pages/*.razor + Pages/*.razor.cs
    └── Shared/MainLayout.razor + .razor.cs
    └── Components/*.razor + *.razor.cs
```

## Padrão Repository

Todas as operações de banco passam por interfaces:

| Interface | Responsabilidade |
|-----------|-----------------|
| `IMockRepository` | CRUD de MockEndpoint, consulta por alias/rota |
| `IRequestHistoryRepository` | Salvar e consultar histórico de requisições |
| `IUnmockedRequestRepository` | Requisições sem mock registradas |
| `IMockGroupRepository` | CRUD de grupos de mocks |

Implementações concretas injetadas via DI em `ServiceExtensions.cs`.

## Padrão Code-Behind (Blazor)

```
Pages/
  MinhaPagina.razor       → só HTML/Razor markup
  MinhaPagina.razor.cs    → partial class MinhaPagina : ComponentBase
                             [Inject] IService Service { get; set; }
                             protected override async Task OnInitializedAsync() { }
```

**Regra:** nunca usar `@code { }` nos arquivos `.razor`.
**Regra:** nunca usar `@inject` nos arquivos `.razor`; usar `[Inject]` no `.razor.cs`.

## Isolamento por Usuário (Alias)

- `ApplicationUser.Alias` → string única por usuário (3-50 chars, lowercase, números, hífens, underscores)
- Todas as URLs de mock seguem: `/{alias}/{rota}`
- Ao alterar alias, todas as URLs mudam imediatamente (sem necessidade de migração de dados)

## Fluxo de Autenticação

```
Login → verifica email confirmado
      → verifica MFA habilitado
          → TOTP: app autenticador
          → Email OTP: código enviado por SMTP
      → gera claims + cookie de sessão
```

## Temas Visuais

Controlados por classe CSS no `<body>`:
- `theme-orange` → Laranja (padrão)
- `theme-black-green` → Preto e Verde
- `theme-blue` → Azul Corporativo

Persistência: `localStorage` via JS interop (`window.getTheme()` / `window.setTheme()`).
Inicialização: `MainLayout.OnAfterRenderAsync`.

## URL do Servidor no Header

`MainLayout.razor.cs` computa:
```csharp
serverUrl = Navigation.BaseUri.TrimEnd('/') + "/" + alias;
```
Exibido no sidebar para o usuário copiar a URL base dos seus mocks.

## Injeção de Dependência — Serviços Registrados

Em `Extensions/ServiceExtensions.cs`:
- `IMockRepository` → `MockRepository` (Scoped)
- `IRequestHistoryRepository` → `RequestHistoryRepository` (Scoped)
- `IUnmockedRequestRepository` → `UnmockedRequestRepository` (Scoped)
- `IMockGroupRepository` → `MockGroupRepository` (Scoped)
- `MockService` (Scoped)
- `BrowserTimezoneService` (Scoped)
- `AliasService` (Scoped)
- `IEmailSender` → `SmtpEmailSender` (Singleton ou Transient)

## Estrutura de Pastas

```
Data/Entities/         → EF Core entities (ApplicationUser, MockEndpoint, etc.)
Data/Repositories/     → Interfaces + Implementações
Models/                → DTOs e ViewModels
Services/              → Lógica de negócio
Middleware/            → HTTP pipeline (mock + history)
Endpoints/             → Minimal API (AuthEndpoints)
Extensions/            → IServiceCollection extensions
Helpers/               → UiHelpers (utilitários de UI)
Pages/                 → Blazor pages + code-behind
Pages/Account/         → Autenticação (Login, Register, MFA, Settings)
Shared/                → Layouts (MainLayout, AuthLayout, RedirectToLogin)
Components/            → Componentes reutilizáveis
wwwroot/css/           → Estilos (layout.css, temas)
wwwroot/js/            → app.js (theme, timezone, utilitários)
wwwroot/img/           → logo.svg e outros assets
```

## Convenções de Nomenclatura

| Tipo | Convenção | Exemplo |
|------|-----------|---------|
| Entidades EF | PascalCase | `MockEndpoint`, `RequestHistory` |
| Interfaces | prefixo `I` | `IMockRepository` |
| DTOs/Models | sufixo `Model` ou `Dto` | `MockEndpointModel` |
| Páginas Blazor | PascalCase | `Index.razor`, `Historico.razor` |
| Rotas Blazor | kebab-case | `@page "/mock-editor/{id:int}"` |
