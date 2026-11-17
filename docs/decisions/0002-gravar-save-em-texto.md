# 0002. Gravar o save em texto SiiNunit

- Status: aceita (a confirmar no jogo)
- Data: 2026-11-17

## Contexto

O `game.sii` que o jogo grava por padrão é `ScsC`: texto `SiiNunit` ou binário
`BSII`, cifrado com AES-256-CBC e comprimido com zlib. Para injetar um job, o
app precisa alterar o conteúdo. Regravar no formato criptografado exigiria
implementar a cifragem e garantir que o cabeçalho e o HMAC do jogo batam.

O `docs/sii-format.md` registra que o jogo aceita carregar o save em texto puro.

## Decisão

Ao gravar, o `SiiDocument.Save` escreve o arquivo como texto `SiiNunit`, em
UTF-8 sem BOM e com finais de linha CRLF. Não há recriptografia. O backup
do arquivo original é feito antes (ver [../architecture/data-flow.md](../architecture/data-flow.md)).

## Consequências

- Não é preciso implementar a cifragem ScsC. A leitura continua aceitando os
  dois formatos.
- Um save gravado pelo app deixa de ser ScsC. Se o jogo não carregar texto puro
  nessa versão, o usuário precisa restaurar o backup. Esse risco está em
  aberto no [sii-format.md](../sii-format.md), seção 5.
- O formato binário `BSII` continua sem suporte. O app pede `g_save_format "2"`
  ao usuário quando encontra um save binário. Ver [../usage.md](../usage.md).
- Para validar esta decisão é preciso testar um save gravado no jogo real.
  TODO: confirmar.
