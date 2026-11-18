#!/usr/bin/env bash
# Valida mensagem de commit.
# uso: ia/toolkit/git/lint-commit-msg.sh <arquivo> [--allow-repeat]
# agentes: git-ops
# saída: 0 a poucas linhas de erro
# Também usado como hook commit-msg (ia/toolkit/git/install-hooks.sh).
source "$(dirname "$0")/../_lib.sh"
f=${1:?arquivo}; msg=$(grep -v '^#' "$f")
subj=$(head -1 <<<"$msg"); err=0
fail() { echo "commit-msg: $*" >&2; err=1; }
[[ $subj =~ ^OWNE-([0-9]+):\ [^[:space:]].{0,70}$ ]] || fail "título deve ser 'OWNE-N: Summary' (≤ 72 chars)"
[[ $subj == *. ]] && fail "título não termina com ponto"
[[ $(sed -n 2p <<<"$msg") == "" ]] || fail "linha 2 deve ficar em branco"
[[ -n $(sed -n '3,$p' <<<"$msg" | tr -d '[:space:]') ]] || fail "falta a Description"
grep -qiE 'claude|anthropic|co-authored|generated with' <<<"$msg" && fail "menção proibida (claude/anthropic/co-author)"
if [[ ${2:-} != --allow-repeat && -n ${BASH_REMATCH[1]:-} ]]; then
  n=${BASH_REMATCH[1]}
  git log --all --format=%s | grep -qE "^OWNE-$n:" && [[ ${GIT_REFLOG_ACTION:-} != *amend* ]] && fail "OWNE-$n já existe (use o próximo: $(ia/toolkit/git/next-owne.sh))"
fi
exit $err
