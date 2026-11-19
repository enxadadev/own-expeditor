# toolkit

Scripts que automatizam trabalho repetitivo dos agentes. Para a lista atualizada
e a descrição de cada um, rode `ia/toolkit/help.sh` (ou `ia/toolkit/help.sh --agents`).

## Convenção de cabeçalho

Todo script começa com:

```
#!/usr/bin/env bash
# <descrição de uma linha>
# uso: ia/toolkit/<pasta>/<script> [args]
# agentes: <quem usa>
# saída: <o que imprime>
```

A saída deve ser curta: `OK` ou `arquivo:linha`.

## Pastas

- `git/` — commit, versão, hooks e diff para revisão.
- `check/` — validações (build, camadas, docs, regras, i18n, agentes).
- `inspect/` — inspeção de saves e código (inclui o projeto `SiiTool`).
- `gen/` — geradores (resx, ADR).
- `release/` — release, screenshots e revisão Codex (inclui o projeto `Screenshots`).

`_lib.sh` (comum, usado via `source "$(dirname "$0")/../_lib.sh"`) e `help.sh` ficam na raiz.
