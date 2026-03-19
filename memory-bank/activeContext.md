# Active Context — Savio Mock Server

## Foco Atual

Preparação para publicação pública no GitHub (v1.0.0).

## Últimas Alterações Realizadas

### Encoding e HTTPS
- Corrigido encoding UTF-8 duplo em todos os arquivos `.razor` (17 arquivos)
- Adicionado perfil HTTPS em `launchSettings.json` (https://localhost:5101)
- `Program.cs`: adicionado `app.UseHttpsRedirection()`

### Arquitetura
- Migração de todos os `@code` blocks para arquivos `.razor.cs` (20 arquivos code-behind)
- `BrowserTimezoneService` criado para converter UTC → fuso horário do navegador
- URL dinâmica do servidor com alias exibida no header (`MainLayout.razor.cs`)
- Logo com outline via CSS `filter: drop-shadow` em `.sidebar-logo`

### Publicação
- `.gitignore` atualizado: `*.db`, `*.db-shm`, `*.db-wal`, `appsettings.Development.json`
- `git rm --cached` nos arquivos SQLite que estavam trackeados
- `Pages/About.razor` reescrito com detalhes completos
- `README.md` reescrito e atualizado (UTF-8 via `create_file`)
- `LICENSE` trilíngue criado (PT + EN + ES)
- `CHANGELOG.md` criado com histórico da v1.0.0
- Versão `1.0.0` adicionada ao `.csproj`
- `memory-bank/` criado com documentação de contexto

## Estado do Build

Último build: **sucesso** ✅

## Arquivos Críticos para Contexto

| Arquivo | Por quê é importante |
|---------|---------------------|
| `Program.cs` | Toda a configuração DI, middleware pipeline, HTTPS |
| `Shared/MainLayout.razor.cs` | Inicializa timezone service e serverUrl |
| `Services/BrowserTimezoneService.cs` | Conversão UTC → local em 6 páginas |
| `Middleware/MockEndpointMiddleware.cs` | Core do produto: serve os mocks |
| `Data/MockDbContext.cs` | Schema do banco + configurações EF |
| `Extensions/ServiceExtensions.cs` | Registro de todos os serviços |
| `wwwroot/js/app.js` | Theme management + timezone JS interop |

## Próximos Passos Sugeridos

- [ ] Fazer o primeiro commit e push para `origin/master`
- [ ] Criar release `v1.0.0` no GitHub
- [ ] Avaliar funcionalidades para a v1.1.0
