# Visão geral da arquitetura

O Own Expeditor é um editor de jobs (fretes) para Euro Truck Simulator 2 e
American Truck Simulator. Em vez de criar ofertas do zero, ele **substitui
ofertas que já existem** no save (`game.sii`) pela origem, destino, carga e
reboque escolhidos pelo usuário. A ideia vem do Virtual Speditor.

## Camadas

```mermaid
flowchart TD
    App["OwnExpeditor.App<br/>UI Avalonia (MVVM)"]
    Core["OwnExpeditor.Core<br/>casos de uso"]
    Data["OwnExpeditor.Data<br/>perfis, saves, cache de distâncias"]
    Sii["OwnExpeditor.Sii<br/>decrypt ScsC, parser/writer SiiNunit"]
    Domain["OwnExpeditor.Domain<br/>entidades puras"]

    App --> Core
    App -.->|records puros, ADR 0005| Domain
    Core --> Data
    Core --> Sii
    Data --> Sii
    Data --> Domain
    Sii --> Domain
    Core --> Domain
```

As setas apontam para a dependência. Detalhes de cada projeto em
[layers.md](layers.md).

## Fluxo principal

O fluxo que o app precisa cumprir, na ordem:

1. **Ler save**: localizar `game.sii` do perfil e decodificá-lo até texto
   `SiiNunit`.
2. **Listar opções**: montar origens, destinos, cargas e reboques a partir do
   próprio save.
3. **Injetar job**: trocar uma oferta existente pela combinação escolhida.
4. **Gravar**: criar backup e gravar o save novo.

Estado atual de cada etapa:

| Etapa | Situação |
|-------|----------|
| Decodificar ScsC até texto | Implementada em `SiiDecoder` (`Sii`) |
| Parsear e gravar texto | Implementada em `SiiDocument` (`Sii`), com round-trip |
| Backup antes de gravar | Implementado em `SiiDocument.Save` |
| Localizar perfil e save | TODO: confirmar. Não há código em `Data` |
| Listar opções | TODO: confirmar. Não há código em `Core` |
| Injetar job pela UI | TODO: confirmar. A UI é só o template |

## Shell modular (planejado)

O app deve virar um shell com módulos (Fretes primeiro, depois Caminhões,
Perfil, Empresa e Mapa). Todos seguem o mesmo ciclo:

```mermaid
flowchart LR
    X[explorar<br/>ver o save] --> E[editar<br/>enfileirar mudanças]
    E --> R[revisar<br/>antes → depois]
    R --> P[aplicar<br/>backup + escrita atômica]
    P --> X
```

- **Explorar e editar** ficam no módulo. Ele não grava nada: monta
  `ChangeSet`s numa sessão de edição do `Core`.
- **Revisar, aplicar e ver o resultado** ficam no shell e valem para todos os
  módulos. Uma gravação cobre todas as mudanças pendentes, com um único backup.
- **Aplicar** é atômico: confere se o jogo está fechado e se o arquivo não
  mudou, grava `game.sii.tmp`, verifica e só então substitui o original.

Decisão: [ADR 0005](../decisions/0005-shell-modular-e-sessao-de-edicao.md).
Plano de implementação e tarefas: [plan-0005-shell.md](plan-0005-shell.md).

Estado: tudo isso está planejado. Nada foi implementado ainda.
> TODO: confirmar a implementação de cada item do plano antes de descrever o
> comportamento como fato.

## Requisitos transversais: tema e idioma

A UI precisa oferecer tema Light, Dark ou Sistema, e os idiomas pt-BR e en. A
regra está no `CLAUDE.md` e na decisão
[ADR 0004](../decisions/0004-suporte-a-tema-e-idioma.md). Em cada camada:
textos ficam em `.resx` na App, o Core devolve códigos de erro e a Data guarda
as preferências. Detalhes por camada em [layers.md](layers.md).

| Item | Situação |
|------|----------|
| Tema segue o sistema | Implementado: `App.axaml` usa `RequestedThemeVariant="Default"` |
| Escolha de tema Light/Dark pelo usuário | TODO: confirmar. Não há tela nem preferência |
| Textos em `.resx` (pt-BR e en) | TODO: confirmar. Não há arquivos `.resx` |
| Preferências em JSON, na Data | TODO: confirmar. Não há código em `Data` |
| Troca de tema e idioma em runtime | TODO: confirmar. Depende dos itens acima |

Sequência detalhada de leitura e escrita: [data-flow.md](data-flow.md).
Formato do arquivo: [../sii-format.md](../sii-format.md).
