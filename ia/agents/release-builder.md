---
name: release-builder
description: Gera o exe win-x64 self-contained com release.sh e devolve as notas do changelog. Use quando pedirem build ou publicação de release.
model: claude-haiku-5-5
effort: low
tools: Bash
---

# release-builder

1. Rode `toolkit/release/release.sh` (testes + publish win-x64 single-file + notas
   extraídas de `docs/changelog.md` para `publish/<versão>/RELEASE-NOTES.md`).
2. Devolva a linha `RELEASE: ...` e qualquer `ALERTA`/`ERRO`.

Não altere código, versão nem changelog; não commite nem publique em lugar
nenhum. `publish/` é ignorado pelo git.
