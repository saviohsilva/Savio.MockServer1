# Savio Mock Server

<p align="center">
  <img src="wwwroot/img/logo.svg" alt="Savio Mock Server" width="80" />
</p>

<p align="center">
  <strong>Um servidor de mock de APIs HTTP com interface visual, autenticação e isolamento por usuário.</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor" alt="Blazor Server" />
  <img src="https://img.shields.io/badge/EF_Core-8.0-512BD4" alt="EF Core 8" />
  <img src="https://img.shields.io/badge/License-MIT-green" alt="MIT License" />
</p>

---

## 📋 Sobre

O **Savio Mock Server** é uma aplicação web que permite criar, gerenciar e servir mocks de APIs HTTP em tempo real. Cada usuário cria uma conta com um **alias** único e suas rotas de mock ficam acessíveis através desse alias, garantindo isolamento completo entre usuários.

### Principais Funcionalidades

- 🔧 **Criação visual de mocks** — Interface intuitiva para criar endpoints HTTP com método, rota, status code, headers e body customizados
- 👥 **Multi-usuário** — Cada usuário tem seu próprio espaço isolado com alias único
- 📊 **Dashboard** — Visão geral dos mocks ativos com métricas de chamadas
- 📝 **Histórico de requisições** — Registro detalhado de todas as requisições recebidas (headers, body, response)
- 🗂️ **Grupos de mocks** — Organize seus mocks em grupos para ativação/desativação em lote
- 📦 **Upload binário** — Suporte a respostas com arquivos binários (imagens, PDFs, etc.)
- 🔄 **Multipart responses** — Suporte a respostas multipart/form-data
- 🎯 **Detecção de unmocked requests** — Captura requisições não mockadas e permite criar mocks a partir delas
- ⏱️ **Delay configurável** — Simule latência nas respostas
- 🌐 **Fuso horário do navegador** — Timestamps exibidos no horário local do cliente (UTC convertido via JavaScript)
- 🎨 **Temas** — 3 temas visuais (Laranja, Preto/Verde, Azul Corporativo)
- 🔒 **Autenticação** — Login, registro, confirmação de e-mail, MFA (TOTP e E-mail)
- 🛡️ **MFA** — Autenticação multifator via aplicativo autenticador (TOTP) ou e-mail
- 🔐 **HTTPS** — Suporte nativo a HTTP e HTTPS via Kestrel

---

## 🏗️ Arquitetura

```
Savio.MockServer/
├── Data/
│   ├── Entities/           # Entidades do EF Core (ApplicationUser, MockEndpoint, RequestHistory, etc.)
│   ├── Repositories/       # Padrão Repository (IMockRepository, IRequestHistoryRepository, etc.)
│   └── MockDbContext.cs    # IdentityDbContext com configurações do modelo
├── Models/                 # DTOs e modelos de domínio
├── Services/               # Lógica de negócio (MockService, BrowserTimezoneService, SmtpEmailSender, etc.)
├── Middleware/             # Middlewares HTTP (MockEndpointMiddleware, RequestHistoryMiddleware)
├── Endpoints/              # Minimal API endpoints (AuthEndpoints)
├── Extensions/             # Extensões de IServiceCollection e IApplicationBuilder
├── Helpers/                # Utilitários (UiHelpers)
├── Pages/                  # Páginas Blazor com code-behind (.razor + .razor.cs)
│   └── Account/            # Páginas de autenticação (Login, Register, MFA, Settings)
├── Shared/                 # Layouts (MainLayout, AuthLayout) com code-behind
├── Components/             # Componentes reutilizáveis (ConfirmDialog, ErrorDialog, DateTimeLocalFilter)
├── wwwroot/                # Assets estáticos (CSS, JS, imagens)
└── Program.cs              # Entry point com toda a configuração
```

### Stack Técnica

| Camada | Tecnologia |
|--------|-----------|
| **Runtime** | .NET 8, C# 12 |
| **UI** | Blazor Server |
| **Padrão de código** | Code-behind (`.razor.cs`) — sem `@code` nos arquivos `.razor` |
| **Autenticação** | ASP.NET Identity |
| **ORM** | Entity Framework Core 8.0 |
| **Banco de Dados** | SQLite (padrão), MySQL, SQL Server |
| **UI Components** | Blazored.Modal, Blazored.Toast, Bootstrap 5, Bootstrap Icons |
| **Servidor** | Kestrel (HTTP + HTTPS) |

---

## ⚡ Início Rápido

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) ou superior

### Instalação

```bash
# Clone o repositório
git clone https://github.com/saviohsilva/Savio.MockServer1.git
cd Savio.MockServer

# Restaurar dependências
dotnet restore

# Executar a aplicação (HTTPS)
dotnet run --launch-profile https
```

A aplicação estará disponível em:
- **HTTPS:** `https://localhost:5101`
- **HTTP:** `http://localhost:5100` (redireciona para HTTPS)

> **Certificado de desenvolvimento:** se for a primeira vez usando HTTPS, execute `dotnet dev-certs https --trust`.

### Primeiro Acesso

1. Acesse `https://localhost:5101`
2. Clique em **Criar Conta**
3. Preencha e-mail, alias (ex: `meuhost`) e senha
4. Se SMTP não estiver configurado, a conta será confirmada automaticamente
5. Faça login e comece a criar mocks!

---

## ⚙️ Configuração

### Banco de Dados

O projeto suporta 3 provedores de banco de dados. Configure no `appsettings.json`:

```json
{
  "Database": {
    "Provider": "SQLite",
    "ConnectionStrings": {
      "SQLite": "Data Source=Savio_mocks.db",
      "MySQL": "Server=localhost;Port=3306;Database=Savio_mocks;User=root;Password=<senha>;",
      "SQLServer": "Server=localhost;Database=SavioMocks;User Id=sa;Password=<senha>;TrustServerCertificate=True;"
    }
  }
}
```

Altere `Provider` para `SQLite`, `MySQL` ou `SQLServer`. As migrations são aplicadas automaticamente na inicialização.

> ⚠️ **Segurança:** os arquivos `*.db` (SQLite) estão no `.gitignore` e nunca serão commitados. Para MySQL/SQL Server, use variáveis de ambiente ou User Secrets para as credenciais.

### E-mail (SMTP)

Para habilitar envio de e-mails (confirmação de conta, MFA por e-mail), configure a seção `Email`:

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "seu-email@gmail.com",
    "SmtpPass": "<senha-de-app>",
    "FromEmail": "seu-email@gmail.com",
    "FromName": "Savio Mock Server"
  }
}
```

> **Gmail**: Gere uma [Senha de App](https://myaccount.google.com/apppasswords) (requer verificação em duas etapas na conta Google).

> **Sem SMTP configurado**: Os e-mails são logados no console e contas são confirmadas automaticamente.

#### Usando User Secrets (recomendado para desenvolvimento)

```bash
dotnet user-secrets init
dotnet user-secrets set "Email:SmtpHost" "smtp.gmail.com"
dotnet user-secrets set "Email:SmtpPort" "587"
dotnet user-secrets set "Email:SmtpUser" "seu-email@gmail.com"
dotnet user-secrets set "Email:SmtpPass" "sua-senha-de-app"
dotnet user-secrets set "Email:FromEmail" "seu-email@gmail.com"
```

### Portas e HTTPS

O perfil `https` (padrão) está configurado em `Properties/launchSettings.json`:

| Perfil | URL | Comportamento |
|--------|-----|---------------|
| `https` | `https://localhost:5101` + `http://localhost:5100` | HTTP redireciona para HTTPS |
| `http` | `http://localhost:5100` | Apenas HTTP |

---

## 🔧 Como Usar

### Criando um Mock

1. No **Dashboard**, clique em **Novo Mock**
2. Configure:
   - **Método HTTP**: GET, POST, PUT, DELETE, PATCH
   - **Rota**: Ex: `/api/users` (sem o alias)
   - **Status Code**: Ex: 200, 201, 404
   - **Response Headers**: Content-Type, etc.
   - **Response Body**: JSON, XML, HTML, binário ou multipart
   - **Delay** (opcional): Latência em milissegundos
3. Salve o mock

### Acessando o Mock

Seus mocks ficam acessíveis em:

```
https://localhost:5101/{seu-alias}/{rota}
```

Exemplo:

```bash
# Se seu alias é "meuhost" e você criou um mock GET /api/users
curl https://localhost:5101/meuhost/api/users
```

O endereço completo com o alias é exibido no header da aplicação após o login.

### Grupos de Mocks

Organize mocks em grupos para:
- Ativar/desativar um conjunto de mocks de uma vez
- Simular diferentes cenários (ex: "Cenário Sucesso" vs "Cenário Erro")
- Duplicar grupos inteiros

### Unmocked Requests

Quando uma requisição chega em uma rota que não tem mock, ela é registrada na seção **Não Mockadas**. A partir daí, você pode criar um mock com um clique.

### Histórico de Requisições

Cada requisição recebida é registrada com:
- Headers de request e response
- Body de request e response
- Status code retornado
- IP do cliente
- Timestamp (exibido no **fuso horário do navegador** do usuário)
- Delay aplicado

---

## 🔒 Segurança

### Autenticação

- **Registro**: E-mail + alias + senha (mínimo 6 caracteres)
- **Confirmação de e-mail**: Obrigatória quando SMTP está configurado
- **Lockout**: Conta bloqueada após 5 tentativas falhas (5 minutos)

### Autenticação Multifator (MFA)

Dois métodos disponíveis:
- **TOTP** (Aplicativo Autenticador): Google Authenticator, Microsoft Authenticator, Authy
- **E-mail**: Código de verificação enviado por e-mail a cada login

> **Modo de desenvolvimento**: MFA pode ser automaticamente ignorado quando `ASPNETCORE_ENVIRONMENT=Development`.

### Alias

- Cada usuário tem um alias único (3-50 caracteres, letras minúsculas, números, hífens e underscores)
- O alias pode ser alterado nas Configurações da Conta
- Ao alterar o alias, todas as URLs de mock mudam imediatamente

### Boas práticas de segurança

- Banco de dados SQLite (`.db`) está no `.gitignore` — nunca commitado
- `appsettings.Development.json` está no `.gitignore`
- Credenciais SMTP devem ser configuradas via **User Secrets** ou variáveis de ambiente
- Strings de conexão com senhas reais nunca devem ser commitadas

---

## 🎨 Temas

A aplicação oferece 3 temas visuais acessíveis pelo seletor no menu lateral:
- 🟠 **Laranja** (padrão)
- 🖤 **Preto e Verde**
- 🔵 **Azul Corporativo**

---

## 📡 API de Health Check

```bash
GET https://localhost:5101/_health
```

Resposta:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-01T00:00:00Z",
  "mocksCount": 42
}
```

---

## 🛠️ Desenvolvimento

### Estrutura do Projeto

| Pasta | Conteúdo |
|-------|---------|
| `Data/Entities/` | 6 entidades EF Core |
| `Data/Repositories/` | 4 interfaces + 4 implementações |
| `Models/` | DTOs e modelos de domínio |
| `Services/` | MockService, BrowserTimezoneService, AliasService, SmtpEmailSender, etc. |
| `Middleware/` | MockEndpointMiddleware, RequestHistoryMiddleware |
| `Endpoints/` | AuthEndpoints (Minimal API) |
| `Pages/` | 14 páginas Blazor + code-behind `.razor.cs` |
| `Pages/Account/` | 8 páginas de autenticação + code-behind |
| `Components/` | ConfirmDialog, ErrorDialog, DateTimeLocalFilter + code-behind |
| `Shared/` | MainLayout, AuthLayout, RedirectToLogin + code-behind |

### Executando em Desenvolvimento

```bash
# Com hot reload (perfil HTTPS)
dotnet watch run --launch-profile https

# Apenas HTTP
dotnet watch run --launch-profile http
```

### Padrão de Código

Todos os arquivos Blazor seguem o padrão **code-behind**:
- `.razor` — apenas markup HTML/Razor (sem `@code` blocks)
- `.razor.cs` — lógica C# em `partial class` com `[Inject]` em vez de `@inject`

### Pacotes NuGet

| Pacote | Versão | Uso |
|--------|--------|-----|
| Blazored.Modal | 7.3.x | Diálogos modais |
| Blazored.Toast | 4.2.x | Notificações toast |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.x | Autenticação e autorização |
| Microsoft.EntityFrameworkCore.Sqlite | 8.0.x | Provider SQLite |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.x | Provider SQL Server |
| Pomelo.EntityFrameworkCore.MySql | 8.0.x | Provider MySQL |

---

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

---

## 👤 Autor

**Sávio Henrique da Silva**

- GitHub: [@saviohsilva](https://github.com/saviohsilva)
- LinkedIn: [saviohenriquesilva](https://www.linkedin.com/in/saviohenriquesilva)
- E-mail: [savioh.silva@outlook.com.br](mailto:savioh.silva@outlook.com.br)

---

<p align="center">
  Feito com ❤️ e .NET 8
</p>
