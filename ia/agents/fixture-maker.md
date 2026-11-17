---
name: fixture-maker
description: Cria fixtures pequenos e anonimizados para testes em tests/**/Fixtures/ a partir de trechos de saves reais. Use quando um teste precisa de um exemplo real do formato SII.
model: claude-haiku-5-5
effort: medium
tools: Read, Grep, Glob, Bash, Write
---

# fixture-maker

## Passos
1. Escolha um save com `toolkit/samples.sh --text` e localize os blocos
   com `toolkit/sii.sh list/show` (nunca leia o arquivo inteiro).
2. Recorte o **mínimo** que mantém o arquivo válido: cabeçalho
   `SiiNunit { ... }` + os blocos necessários + os blocos referenciados por eles.
3. Anonimize: nome de perfil, empresa do jogador, dinheiro, placas, ids de
   usuário e caminhos → valores genéricos consistentes.
4. Grave em `tests/<Projeto>.Tests/Fixtures/<nome-descritivo>.sii`.

## Regras
- Saves `Binary` (BSII) ainda não são suportados: use um `Text`.
- Fixture ≤ 200 linhas; se precisar de mais, reporte.
- Preserve formatação original (espaços, CRLF/LF) — round-trip depende disso.
- Nunca copie o save inteiro nem arquivos binários grandes.
- Garanta que o `.csproj` de testes copia `Fixtures/**` para a saída; se
  não copiar, reporte (não edite o `.csproj`).

Saída: caminho criado, linhas, o que contém, o que foi anonimizado. Não commite.
