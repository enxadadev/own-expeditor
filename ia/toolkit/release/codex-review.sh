#!/usr/bin/env bash
# Revisão independente pelo Codex CLI (OpenAI) do diff atual contra uma base.
# uso: ia/toolkit/release/codex-review.sh [base]   → grava review em .reviews/codex-<data>.md
# agentes: code-reviewer
# saída: caminho do arquivo gerado
source "$(dirname "$0")/../_lib.sh"
command -v codex >/dev/null || die "Codex CLI não instalado (npm i -g @openai/codex && codex login)"
base=${1:-$(main_branch)}; out=.reviews/codex-$(date +%Y%m%d-%H%M%S).md; mkdir -p .reviews
prompt="Revise o diff abaixo do projeto Own Expeditor (C#/.NET 8, Avalonia). Regras do projeto em CLAUDE.md.
Liste só problemas reais, por severidade BLOQUEANTE/IMPORTANTE/SUGESTAO, cada um com arquivo:linha,
problema e correção. Foque: corrupção de save, backup antes de gravar, round-trip SII, camadas,
listas fixas, MVVM, desempenho em saves grandes. Termine com veredito."
{ echo "$prompt"; echo; cat CLAUDE.md; echo; ia/toolkit/git/diff-summary.sh "$base"; } \
  | codex exec --sandbox read-only - > "$out" 2>/dev/null || die "codex falhou"
echo "REVIEW: $out ($(grep -cE 'BLOQUEANTE' "$out" || true) bloqueantes, $(grep -cE 'IMPORTANTE' "$out" || true) importantes)"
