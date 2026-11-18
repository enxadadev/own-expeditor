---
name: wf-release
description: Workflow de release do exe Windows self-contained — checagens, changelog, publish e notas. Use quando pedirem build de release.
---

# Workflow: release

| # | Quem | Faz | Saída / gate |
|---|------|-----|--------------|
| 1 | sessão | Árvore limpa (`git status`) e na branch certa. | ⛔ |
| 2 | `docs-keeper` | `docs/changelog.md` tem `## <versão>` com o que mudou para o usuário. | ⛔ |
| 3 | `release-builder` | `toolkit/release/release.sh` (testes + publish win-x64 + notas). | exe + `RELEASE-NOTES.md` em `publish/<versão>/` |
| 4 | usuário | Testa o exe no Windows (abrir save, injetar job, conferir backup). | ⛔ ok manual |
| 5 | `git-ops` | Tag `v<versão>` só com aprovação; push só se pedido. | ⛔ |
