#!/usr/bin/env bash
# Inspeciona saves com a ferramenta SiiTool (compila na primeira vez).
# uso: ia/toolkit/inspect/sii.sh <comando> <save> [args]   — rode sem args para ajuda
# agentes: save-inspector, sii-engine
# saída: depende do comando
source "$(dirname "$0")/../_lib.sh"
BIN=ia/toolkit/inspect/SiiTool/bin/Release/net8.0/SiiTool.dll
if [[ ! -f $BIN || -n $(find src/OwnExpeditor.Sii ia/toolkit/inspect/SiiTool -name '*.cs' -newer "$BIN" 2>/dev/null) ]]; then
  dotnet build ia/toolkit/inspect/SiiTool -c Release -nologo -v q >/dev/null || die "falha ao compilar SiiTool"
fi
exec dotnet "$BIN" "$@"
