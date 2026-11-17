#!/usr/bin/env bash
# Commita os arquivos listados com a mensagem do arquivo, após validá-la.
# SÓ rodar depois da aprovação explícita do usuário.
# uso: toolkit/commit.sh <msg.txt> <arquivo>... [--allow-repeat]
source "$(dirname "$0")/_lib.sh"
msg=${1:?mensagem}; shift; extra=()
files=(); for a in "$@"; do [[ $a == --allow-repeat ]] && extra+=(--allow-repeat) || files+=("$a"); done
(( ${#files[@]} )) || die "liste os arquivos (nada de add -A)"
toolkit/lint-commit-msg.sh "$msg" "${extra[@]}"
git add -- "${files[@]}"
git commit -q -F "$msg" --no-verify   # já validado acima
git log -1 --format='COMMIT: %h %s'; echo "BRANCH: $(git branch --show-current)"; echo "VERSAO: $(props_version)"
