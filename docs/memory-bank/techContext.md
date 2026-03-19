# Tech Context — Savio Mock Server

## Stack

| Camada | Tecnologia | Versão |
|--------|-----------|--------|
| Runtime | .NET | 8.0 |
| Linguagem | C# | 12 |
| UI Framework | Blazor Server | .NET 8 |
| Auth | ASP.NET Core Identity | 8.0.11 |
| ORM | Entity Framework Core | 8.0.11 |
| DB padrão | SQLite | via EF Core |
| DB opcional | MySQL | Pomelo 8.0.2 |
| DB opcional | SQL Server | EF Core 8.0.11 |
| Modais | Blazored.Modal | 7.3.1 |
| Toasts | Blazored.Toast | 4.2.1 |
| CSS | Bootstrap | 5 |
| Ícones | Bootstrap Icons | latest |
| Servidor | Kestrel | .NET 8 |

## Padrão de Código Blazor

**Todos** os arquivos Blazor usam o padrão code-behind:
- `.razor` → apenas markup HTML/Razor, **sem `@code` blocks**
- `.razor.cs` → `partial class` com toda a lógica C#
- Injeção de dependência via `[Inject]` (não `@inject`)

## Banco de Dados

Configurado em `appsettings.json` na seção `Database.Provider`:
- `"SQLite"` → `Data Source=Savio_mocks.db`
- `"MySQL"` → Pomelo provider
- `"SQLServer"` → Microsoft provider

Migrations aplicadas automaticamente em `Program.cs` via `db.Database.Migrate()`.

## Autenticação

- `ApplicationUser` estende `IdentityUser` com propriedade `Alias` (string única)
- Email confirmation obrigatória quando SMTP configurado
- MFA: TOTP (via authenticator app) ou Email OTP
- Lockout: 5 falhas → 5 minutos bloqueado
- Ambiente Development: MFA pode ser ignorado automaticamente

## Portas e HTTPS

| Perfil | URLs |
|--------|------|
| `https` (padrão) | https://localhost:5101 + http://localhost:5100 |
| `http` | http://localhost:5100 |

`Program.cs` tem `app.UseHttpsRedirection()`.
Certificado dev: `dotnet dev-certs https --trust`.

## Fuso Horário

`BrowserTimezoneService` (scoped):
- JS interop: `window.getBrowserTimezoneOffsetMinutes()` → `new Date().getTimezoneOffset()`
- Inicializado em `MainLayout.OnAfterRenderAsync(firstRender: true)`
- Método: `FormatLocalTime(DateTime? utc, string format = "dd/MM HH:mm:ss")`

## Roteamento de Mocks

`MockEndpointMiddleware` intercepta requisições no formato `/{alias}/{rota}`:
- Consulta `IMockRepository` pelo alias + rota + método
- Aplica delay se configurado
- Retorna status code + headers + body configurados
- Registra em `RequestHistory`

## Variáveis de Ambiente / Configuração Sensível

Nunca commitar:
- `*.db`, `*.db-shm`, `*.db-wal` → no `.gitignore`
- `appsettings.Development.json` → no `.gitignore`
- Usar User Secrets para credenciais SMTP em dev

## Comandos Úteis

```bash
# Rodar em desenvolvimento (HTTPS)
dotnet watch run --launch-profile https

# Adicionar migration
dotnet ef migrations add NomeDaMigration

# Aplicar migrations manualmente
dotnet ef database update
```
