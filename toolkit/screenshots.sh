#!/usr/bin/env bash
# Gera PNGs das Views do App sem abrir janela (Light/Dark × pt-BR/en) para revisão visual.
# uso: toolkit/screenshots.sh [filtro-de-view] [largura altura]   → .screenshots/<View>/<tema>-<cultura>.png
source "$(dirname "$0")/_lib.sh"
dotnet build toolkit/Screenshots -c Release -nologo -v q >/dev/null || die "falha ao compilar Screenshots (rode toolkit/check.sh)"
rm -rf .screenshots/${1:-}*
exec dotnet toolkit/Screenshots/bin/Release/net8.0/Screenshots.dll .screenshots "${1:-}" "${2:-1280}" "${3:-800}"
