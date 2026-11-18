#!/usr/bin/env bash
# Commita os arquivos listados com a mensagem do arquivo, após validá-la.
# uso: toolkit/git/commit.sh <msg.txt> <arquivo>... [--allow-repeat]
# agentes: git-ops
# saída: hash e resumo do commit
# SÓ rodar depois da aprovação explícita do usuário.
source "$(dirname "$0")/../_lib.sh"
msg=${1:?mensagem}; shift; extra=()
files=(); for a in "$@"; do [[ $a == --allow-repeat ]] && extra+=(--allow-repeat) || files+=("$a"); done
(( ${#files[@]} )) || die "liste os arquivos (nada de add -A)"
toolkit/git/lint-commit-msg.sh "$msg" "${extra[@]}"
git add -- "${files[@]}"
git commit -q -F "$msg" --no-verify   # já validado acima
git log -1 --format='COMMIT: %h %s'; echo "BRANCH: $(git branch --show-current)"; echo "VERSAO: $(props_version)"
