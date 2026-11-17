#!/usr/bin/env bash
# Diff compacto para revisores. uso: toolkit/diff-summary.sh [base] [-- caminhos]
# base padrão: main (merge-base). Mostra stat + diff sem arquivos gerados.
source "$(dirname "$0")/_lib.sh"
if [[ ${1:-} && $1 != -- ]]; then base=$1; shift; else base=$(main_branch); fi; [[ ${1:-} == -- ]] && shift || true
mb=$(git merge-base "$base" HEAD)
echo "BASE: $base ($(git rev-parse --short "$mb"))  COMMITS: $(git rev-list --count "$mb"..HEAD)"
git diff --stat "$mb" -- "$@" ':!*.Designer.cs' ':!**/obj/**' | tail -25
echo; git diff -U2 "$mb" -- "$@" ':!*.Designer.cs' ':!**/obj/**' ':!*.sii'
