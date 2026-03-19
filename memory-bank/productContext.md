# Product Context — Savio Mock Server

## Por que Existe

Desenvolvedores frequentemente precisam mockar APIs externas durante o desenvolvimento e testes. As alternativas existentes têm limitações:

| Ferramenta | Limitação |
|------------|-----------|
| Mockoon | Desktop only, não acessível remotamente |
| WireMock | Configuração via arquivo/código, sem UI web |
| Postman Mock | Pago para uso em equipe, dependência de nuvem externa |
| json-server | Apenas JSON, sem autenticação, sem histórico |

**Savio Mock Server** resolve isso sendo self-hosted, com interface web completa e multi-usuário.

## Usuários-Alvo

- **Desenvolvedor solo** — testa integrações sem depender de APIs reais
- **Equipe pequena** — cada membro tem seu próprio alias e espaço isolado
- **QA/Tester** — cria cenários de erro/sucesso sem alterar o backend real
- **Demonstrações** — apresenta fluxos de UI sem backend funcional

## Fluxo Principal do Usuário

1. Cria conta com alias único (ex: `joao`)
2. Cria mock: `GET /api/users` → 200 + JSON body
3. Configura seu sistema para apontar para `https://servidor/joao/api/users`
4. Faz requisições e vê o histórico em tempo real no dashboard
5. Ativa/desativa mocks ou grupos conforme o cenário de teste

## Casos de Uso por Funcionalidade

### Grupos de Mocks
**Cenário:** QA precisa alternar entre "tudo funcionando" e "servidor em erro"
- Grupo "Cenário OK" → todos os endpoints retornam 200
- Grupo "Cenário Falha" → endpoints retornam 500/503
- Alterna entre cenários com um clique

### Unmocked Requests
**Cenário:** Desenvolvedor não sabe exatamente quais endpoints o frontend chama
- Aponta o frontend para o mock server
- Navega pela aplicação normalmente
- Unmocked Requests captura todas as chamadas sem mock
- Cria os mocks necessários a partir das requisições capturadas

### Delay Configurável
**Cenário:** Testar comportamento da UI com latência alta
- Configura delay de 3000ms no endpoint
- Verifica se loading states aparecem corretamente

### Upload Binário
**Cenário:** API retorna uma imagem ou PDF
- Faz upload do arquivo binário como resposta do mock
- Frontend recebe o arquivo corretamente

## Decisões de Design

- **Alias na URL** (não subdomínio) → funciona em qualquer ambiente sem configuração DNS
- **Blazor Server** (não WASM) → sem necessidade de API separada, estado centralizado
- **SQLite por padrão** → zero configuração para começar a usar
- **Code-behind obrigatório** → separação clara de responsabilidades, testabilidade
- **Timestamps em UTC no banco** → portabilidade; conversão para local no frontend via JS
