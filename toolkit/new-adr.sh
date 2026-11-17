#!/usr/bin/env bash
# Cria o próximo ADR numerado a partir do texto em stdin. uso: toolkit/new-adr.sh "titulo-curto" < adr.md
source "$(dirname "$0")/_lib.sh"
slug=$(tr '[:upper:] ' '[:lower:]-' <<<"${1:?titulo}" | tr -cd 'a-z0-9-')
mkdir -p docs/decisions
n=$(ls docs/decisions | grep -oE '^[0-9]{4}' | sort -n | tail -1)
f=docs/decisions/$(printf %04d $((10#${n:-0} + 1)))-$slug.md
cat > "$f"; [[ -s $f ]] || { rm "$f"; die "ADR vazio (stdin)"; }
echo "$f"
