#!/usr/bin/env bash
# Gera PNGs (build Debug, pois a Galeria é excluída do Release) das Views do App sem abrir janela (Light/Dark × pt-BR/en) para revisão visual.
# uso: ia/toolkit/release/screenshots.sh [filtro-de-view] [largura altura]   → .screenshots/<View>/<tema>-<cultura>.png
# agentes: ux-reviewer, ui-dev
# saída: lista de PNGs gerados
source "$(dirname "$0")/../_lib.sh"
dotnet build ia/toolkit/release/Screenshots -c Debug -nologo -v q >/dev/null || die "falha ao compilar Screenshots (rode ia/toolkit/check/check.sh)"
rm -rf .screenshots/${1:-}*
exec dotnet ia/toolkit/release/Screenshots/bin/Debug/net8.0/Screenshots.dll .screenshots "${1:-}" "${2:-1280}" "${3:-800}"
