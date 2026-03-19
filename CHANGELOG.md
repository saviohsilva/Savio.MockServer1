# Changelog

Todas as alterações notáveis neste projeto serão documentadas neste arquivo.

O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/)
e este projeto adota [Versionamento Semântico](https://semver.org/lang/pt-BR/).

---

## [1.0.0] — 2026-03-19

### Adicionado

#### Autenticação e Usuários
- Registro de conta com e-mail, alias único e senha
- Login com confirmação de e-mail (automática quando SMTP não configurado)
- Lockout automático após 5 tentativas falhas (5 minutos)
- Autenticação Multifator (MFA) via TOTP (Google/Microsoft Authenticator, Authy)
- Autenticação Multifator (MFA) via E-mail
- Códigos de recuperação MFA (10 códigos de uso único)
- Gerenciamento de alias nas configurações da conta (com aviso de impacto nas URLs)
- Suporte a MFA ignorado automaticamente em ambiente de desenvolvimento

#### Mocks
- Criação visual de endpoints mock (método HTTP, rota, status code, headers, body)
- Suporte a métodos: GET, POST, PUT, DELETE, PATCH
- Respostas com body em JSON, XML, HTML ou texto livre
- Upload de arquivos binários como resposta (imagens, PDFs, etc.)
- Suporte a respostas multipart/form-data
- Delay configurável por endpoint (simulação de latência)
- Ativação/desativação individual de mocks

#### Grupos de Mocks
- Criação e gerenciamento de grupos
- Ativação/desativação em lote de todos os mocks do grupo
- Duplicação de grupos inteiros

#### Histórico e Monitoramento
- Registro detalhado de todas as requisições recebidas (headers, body, status, IP, timestamp)
- Timestamps convertidos para o fuso horário do navegador do usuário (UTC via JS interop)
- Captura automática de requisições não mockadas (Unmocked Requests)
- Criação de mock a partir de requisição não mockada com um clique
- Dashboard com métricas: total de chamadas e último acesso por endpoint

#### Infraestrutura
- Suporte a múltiplos bancos de dados: SQLite (padrão), MySQL, SQL Server
- Migrations aplicadas automaticamente na inicialização
- Suporte nativo a HTTPS via Kestrel com redirecionamento HTTP → HTTPS
- Health Check endpoint em `/_health`
- Padrão de código code-behind: `.razor` + `.razor.cs` (sem `@code` blocks)
- Isolamento completo por usuário via alias único na URL
- 3 temas visuais: Laranja (padrão), Preto e Verde, Azul Corporativo

---

## [Não lançado]

> Funcionalidades planejadas ou em desenvolvimento serão listadas aqui.

---

[1.0.0]: https://github.com/saviohsilva/Savio.MockServer/releases/tag/v1.0.0
[Não lançado]: https://github.com/saviohsilva/Savio.MockServer/compare/v1.0.0...HEAD
