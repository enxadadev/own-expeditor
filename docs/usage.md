# Guia do usuário

> TODO: confirmar. O app ainda não tem a função principal. Este guia cobre o
> estado atual e o que já existe na biblioteca de leitura do save. O fluxo de
> escolha e injeção de jobs está descrito como planejado e precisa ser
> confirmado quando for implementado.

## Estado atual

Hoje o `OwnExpeditor.App` abre uma janela de template do Avalonia, com um campo
de texto de exemplo. Ainda não há tela para escolher origem, destino, carga ou
reboque, e não há botão para injetar job no save.

Para abrir:

```bash
dotnet run --project src/OwnExpeditor.App
```

Pré-requisitos e build: ver [development.md](development.md).

## Fluxo planejado

Quando a função principal existir, o uso esperado é:

1. Abrir o perfil de ETS2 ou ATS e escolher o save.
2. Escolher a empresa de origem, o destino, a carga e o reboque. As opções vêm
   do próprio save, sem lista fixa.
3. Escolher uma oferta a substituir (um slot vazio ou uma oferta existente).
4. Confirmar. O app faz backup e grava o save novo.

TODO: confirmar cada passo acima com a UI real.

## Backups

Ao gravar um save, a biblioteca `Sii` cria um backup ao lado do arquivo antes
de sobrescrevê-lo. A UI ainda não grava saves, então isso vale para o código
de `SiiDocument.Save`, não para um botão do app:

```
game.sii.bak-20261117-153012
```

O nome é `game.sii.bak-` seguido da data e hora locais, no formato
`yyyyMMdd-HHmmss`. O backup é uma cópia exata do arquivo que existia antes da
gravação.

### Como restaurar

O app não tem comando de restauração. Para desfazer uma alteração:

1. Feche o jogo.
2. Copie o arquivo `game.sii.bak-<data-hora>` por cima de `game.sii`, no mesmo
   diretório do perfil.
3. Abra o jogo de novo.

TODO: confirmar que o jogo carrega o backup sem nenhum passo extra.

Os backups não são apagados pelo app. Remova-os manualmente quando não
precisar mais deles.

## Onde ficam os saves

TODO: confirmar. O código ainda não localiza perfis nem saves (o projeto
`Data` está vazio). O caminho típico no Windows é a pasta `Documents` do
usuário, em `Euro Truck Simulator 2\profiles\<id>\` (ou
`American Truck Simulator\profiles\<id>\`). Confirme no seu computador antes
de copiar ou apagar arquivos.

## Problemas comuns

As mensagens abaixo vêm do código atual (`SiiDecoder` e `SiiDocument`):

| Mensagem | Causa | O que fazer |
|----------|-------|-------------|
| `Save em formato binário (BSII) ainda não é suportado. Use g_save_format "2" e salve de novo.` | O save está no formato binário do jogo. | Defina `g_save_format "2"` (a mensagem do app indica essa opção) e salve um jogo novo. TODO: confirmar onde essa opção é definida no jogo. |
| `Formato 3nK ainda não é suportado.` | O arquivo usa o formato ofuscado `3nK`. | Não há solução no app hoje. |
| `Arquivo não reconhecido como .sii.` | O arquivo não começa com `ScsC`, `BSII`, `3nK` nem `SiiNunit`. | Confira se o arquivo é o `game.sii` do perfil. |
| `Cabeçalho ScsC inválido.` | O arquivo é menor que o cabeçalho ou está corrompido. | Use um backup íntegro. |
| `Tamanho decodificado (...) difere do cabeçalho (...)` | A descompressão não bateu com o tamanho gravado. | O arquivo pode estar corrompido. Use um backup. |
| `Linha N: ...` | O texto `SiiNunit` tem uma linha fora do formato esperado. | O arquivo pode ter sido editado à mão. Restaure o backup. |
| `IOException` ao gravar, com o backup já existente | Duas gravações no mesmo segundo geraram o mesmo nome de backup. | Espere um segundo e tente de novo. Nada foi gravado. |

Antes de gravar qualquer save que você não tenha copiado manualmente, faça uma
cópia da pasta do perfil por fora do app.
