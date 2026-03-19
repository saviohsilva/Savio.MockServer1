# Progress — Savio Mock Server

## Versão 1.0.0 — Status: Pronto para Release

### Funcionalidades Completas ✅

#### Core
- [x] Middleware de mock (`MockEndpointMiddleware`)
- [x] Middleware de histórico (`RequestHistoryMiddleware`)
- [x] Roteamento por alias: `/{alias}/{rota}`
- [x] Suporte a GET, POST, PUT, DELETE, PATCH
- [x] Respostas com body texto (JSON, XML, HTML)
- [x] Respostas com arquivos binários (upload)
- [x] Respostas multipart/form-data
- [x] Delay configurável por endpoint
- [x] Ativação/desativação individual de mocks

#### Grupos de Mocks
- [x] CRUD de grupos
- [x] Ativação/desativação em lote
- [x] Duplicação de grupos

#### Histórico e Monitoramento
- [x] Registro de todas as requisições (headers, body, status, IP, delay)
- [x] Timestamps em UTC no banco, convertidos para fuso local no frontend
- [x] Captura de requisições não mockadas (Unmocked Requests)
- [x] Criação de mock a partir de unmocked request
- [x] Dashboard com total de chamadas e último acesso
- [x] Health check endpoint `/_health`

#### Autenticação
- [x] Registro com e-mail + alias + senha
- [x] Confirmação de e-mail (SMTP ou automática)
- [x] Login com lockout (5 tentativas / 5 min)
- [x] MFA TOTP (Google/Microsoft Authenticator, Authy)
- [x] MFA por e-mail (OTP a cada login)
- [x] Códigos de recuperação MFA (10 códigos)
- [x] Alteração de alias com aviso de impacto

#### UI / UX
- [x] Dashboard com visão geral
- [x] Editor de mocks
- [x] Histórico de requisições com detalhes
- [x] Página de configurações de conta
- [x] 3 temas visuais (Laranja, Preto/Verde, Azul Corporativo)
- [x] URL do servidor exibida no header após login
- [x] Logo com outline no sidebar

#### Infraestrutura
- [x] SQLite (padrão), MySQL, SQL Server
- [x] Migrations automáticas na inicialização
- [x] HTTPS com Kestrel (redirect HTTP → HTTPS)
- [x] Code-behind em todos os arquivos Blazor (20 `.razor.cs`)
- [x] `BrowserTimezoneService` para conversão UTC → local

#### Publicação
- [x] `.gitignore` com SQLite e configs sensíveis
- [x] Banco de dados removido do git cache
- [x] README.md atualizado e coerente
- [x] About.razor com detalhes completos
- [x] LICENSE trilíngue (PT + EN + ES)
- [x] CHANGELOG.md
- [x] Versão no `.csproj`
- [x] Memory bank criado

---

## Funcionalidades Conhecidas como Pendentes / Backlog

- [ ] Exportar/Importar mocks (JSON)
- [ ] Paginação no histórico (performance com muitos registros)
- [ ] Rate limiting por usuário
- [ ] Webhook/callback ao receber requisição
- [ ] Docker compose para deploy simplificado
- [ ] Testes automatizados (unitários e integração)
- [ ] Dark mode persistido por conta (não só por dispositivo)

---

## Histórico de Versões

| Versão | Data | Descrição |
|--------|------|-----------|
| 1.0.0 | 2026-03-19 | Lançamento inicial |
