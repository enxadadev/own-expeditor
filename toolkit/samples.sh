#!/usr/bin/env bash
# Lista game.sii de exemplo com o formato interno (Text = utilizável hoje; Binary = BSII, ainda não suportado).
# uso: toolkit/samples.sh [--text]   (--text: só os utilizáveis)
source "$(dirname "$0")/_lib.sh"
for d in samples "$(git rev-parse --path-format=absolute --git-common-dir)/../samples"; do
  [[ -d $d ]] || continue
  mapfile -t files < <(find "$(realpath "$d")" -name game.sii | sort)
  toolkit/sii.sh formats "${files[@]}" | { [[ ${1:-} == --text ]] && grep '^Text' || cat; }
  exit 0
done
die "nenhuma pasta samples/ encontrada"
