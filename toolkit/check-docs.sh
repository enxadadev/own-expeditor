#!/usr/bin/env bash
# Valida docs/: links relativos resolvem e docs/README.md cita todos os documentos.
source "$(dirname "$0")/_lib.sh"
err=0
for md in $(find docs -name '*.md'); do
  for l in $(grep -oE '\]\([^)#]+' "$md" | sed 's/^](//' | grep -vE '^(https?:|mailto:)' || true); do
    [[ -e $(dirname "$md")/$l ]] || { echo "LINK QUEBRADO: $md -> $l"; err=1; }
  done
done
for md in $(find docs -name '*.md' ! -path docs/README.md | sed 's#^docs/##'); do
  grep -q "$md" docs/README.md || { echo "FORA DO INDICE: $md"; err=1; }
done
echo "TODOS: $(grep -rn 'TODO: confirmar' docs | wc -l)"
(( err )) || echo "DOCS: ok"; exit $err
