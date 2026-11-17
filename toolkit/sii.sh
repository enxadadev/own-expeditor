#!/usr/bin/env bash
# Inspeciona saves com a ferramenta SiiTool (compila na primeira vez).
# uso: toolkit/sii.sh <comando> <save> [args]   — rode sem args para ajuda.
source "$(dirname "$0")/_lib.sh"
BIN=toolkit/SiiTool/bin/Release/net8.0/SiiTool.dll
if [[ ! -f $BIN || -n $(find src/OwnExpeditor.Sii toolkit/SiiTool -name '*.cs' -newer "$BIN" 2>/dev/null) ]]; then
  dotnet build toolkit/SiiTool -c Release -nologo -v q >/dev/null || die "falha ao compilar SiiTool"
fi
exec dotnet "$BIN" "$@"
