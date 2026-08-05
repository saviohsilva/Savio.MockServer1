# Roadmap

Este arquivo centraliza melhorias planejadas para o Savio Mock Server.

Status:
- Planned: item mapeado, ainda sem implementacao.
- In Progress: item em desenvolvimento.
- Done: item concluido e publicado.

## Backlog Prioritario

### 1) Suporte a importacao e exportacao via cURL

- Status: Done
- Objetivo:
  - Permitir criar/configurar um mock a partir de um comando cURL.
  - Permitir gerar o comando cURL de um mock ja criado.
- Valor:
  - Acelera onboarding e compartilhamento de mocks.
  - Facilita reproducao de cenarios em times e documentacoes tecnicas.
- Criterios de aceite (alto nivel):
  - Usuario cola um cURL valido e a tela preenche metodo, rota, headers e body.
  - Usuario pode clicar em "Gerar cURL" em um mock existente e copiar o comando pronto.
  - Quando houver campos nao mapeaveis automaticamente, a interface sinaliza ajuste manual.

### 2) Unificacao de ativar/desativar endpoints (individual e por grupo)

- Status: Done
- Objetivo:
  - Substituir os pares de botoes (ativar e desativar) por um unico botao de toggle, tanto por mock quanto por grupo.
- Valor:
  - Reduz complexidade visual e melhora usabilidade da tela de mocks e grupos.
  - Diminui chance de clique errado e simplifica manutencao da interface.
- Criterios de aceite (alto nivel):
  - Um unico controle alterna estado do mock/grupo (ativo/inativo).
  - Rotulo e estilo do botao refletem o estado atual.
  - Acao atualiza o(s) endpoint(s) de forma consistente.

### 3) Preview seguro de conteudo binario no historico

- Status: Done
- Objetivo:
  - Exibir preview inline de imagens/PDF armazenados como binario (request ou response) no historico de requisicoes.
  - Permitir inspecionar e baixar arquivos individuais de requests multipart/form-data.
- Valor:
  - Evita exposicao a arquivos maliciosos disfarcados de imagem/PDF (spoofing de Content-Type).
  - Melhora a inspecao de requests/responses binarios sem downloads desnecessarios.
- Criterios de aceite (alto nivel):
  - A assinatura real dos bytes do arquivo e validada antes de permitir preview inline.
  - Tipos nao reconhecidos ou incompatíveis com o Content-Type declarado exibem aviso e permitem apenas download.
  - Arquivos de partes multipart do request podem ser baixados individualmente ou em lote.

### 4) Response Form URL Encoded e aprimoramentos no Multipart

- Status: Done
- Objetivo:
  - Permitir configurar responses no formato application/x-www-form-urlencoded via editor de campos chave/valor.
  - Enriquecer o response Multipart com parts em JSON e arquivos embutidos em Base64, alem do upload via blob ja existente.
- Valor:
  - Cobre cenarios de APIs que retornam form-urlencoded sem exigir edicao de texto bruto.
  - Amplia a flexibilidade de composicao de respostas multipart complexas.
- Criterios de aceite (alto nivel):
  - Usuario monta o body form-urlencoded a partir de campos chave/valor.
  - Multipart permite adicionar parts JSON e parts com arquivo em Base64 embutido diretamente na tela.

### 5) Notificacoes via toast

- Status: Done
- Objetivo:
  - Substituir alertas fixos na pagina por notificacoes do tipo toast (canto superior direito).
- Valor:
  - Feedback de acoes nao interfere no layout da pagina nem exige rolagem.
- Criterios de aceite (alto nivel):
  - Mensagens de sucesso/aviso da importacao de cURL aparecem como toast temporario.

### 6) Cliente de requisicoes HTTP integrado (estilo Postman)

- Status: Planned
- Objetivo:
  - Permitir montar e enviar requisicoes HTTP reais (metodo, URL/rota, headers, query params, body) diretamente pela interface, de forma semelhante a um cliente Postman/Insomnia.
  - Exibir status code, tempo de resposta, headers e body da resposta recebida.
- Valor:
  - Permite testar endpoints mockados (e potencialmente APIs externas) sem sair da aplicacao.
  - Reduz a necessidade de ferramentas externas para validar o comportamento configurado.
- Criterios de aceite (alto nivel):
  - Usuario informa metodo, URL, headers, query params e body e dispara a requisicao.
  - Tela exibe status code, tempo de resposta, headers e body da resposta formatados (com preview seguro para binarios).
  - Reaproveita o parser/gerador de cURL existente (import/export) para ida e volta rapida com o cliente.
  - Historico de chamadas realizadas pelo cliente fica disponivel para reconsulta.

### 7) Visualizacao de JSON em arvore

- Status: Planned
- Objetivo:
  - Exibir bodies JSON (request/response no historico, e editores de body de mock) em uma visualizacao em arvore expansivel/colapsavel.
  - Mostrar a contagem de itens de cada no (ex.: `[2]` para arrays, `{2}` para objetos) e realce de sintaxe por tipo (chave, string, numero).
- Valor:
  - Facilita a leitura e navegacao de payloads JSON grandes ou profundamente aninhados.
  - Reduz a necessidade de copiar o JSON para uma ferramenta externa so para inspecionar a estrutura.
- Criterios de aceite (alto nivel):
  - Usuario pode alternar entre visualizacao em texto puro (atual) e visualizacao em arvore.
  - Nos de objeto/array podem ser expandidos e colapsados individualmente, exibindo a contagem de filhos.
  - JSON invalido ou nao-JSON mantem a exibicao em texto puro sem quebrar a tela.

## Como contribuir com o roadmap

- Abra uma issue descrevendo problema, impacto e sugestao.
- Referencie a issue no item correspondente deste arquivo.
- Quando a melhoria for entregue, mova para "Done" e cite versao/release.
