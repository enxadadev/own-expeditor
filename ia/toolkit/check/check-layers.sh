#!/usr/bin/env bash
# Verifica a regra de camadas (App → Core → Data/Sii → Domain).
# uso: ia/toolkit/check/check-layers.sh
# agentes: code-reviewer, core-dev, ui-dev, test-runner
# saída: 'CAMADAS: ok' ou 1 linha por violação
# Níveis verificados:
#   1. ProjectReference nos .csproj de src/;
#   2. `using`/`global using` de namespaces de camada proibida;
#   3. uso qualificado (OwnExpeditor.Sii.Foo) em código e .axaml.
# tools/, ia/toolkit/ e tests/ ficam fora da regra (só src/ é verificado).
source "$(dirname "$0")/../_lib.sh"
declare -A ALLOW=([App]="Core Domain" [Core]="Data Sii Domain" [Data]="Sii Domain" [Sii]="Domain" [Domain]="")
LAYERS="App Core Data Sii Domain"
err=0
for p in $LAYERS; do
  dir=src/OwnExpeditor.$p; csproj=$dir/OwnExpeditor.$p.csproj; [[ -f $csproj ]] || continue
  for ref in $(grep -oE 'OwnExpeditor\.[A-Za-z]+\.csproj' "$csproj" | sed 's/OwnExpeditor\.\(.*\)\.csproj/\1/'); do
    [[ " ${ALLOW[$p]} " == *" $ref "* ]] || { echo "VIOLACAO (referencia): $p -> $ref"; err=1; }
  done
  for other in $LAYERS; do
    [[ $other == "$p" || " ${ALLOW[$p]} " == *" $other "* ]] && continue
    # pega `using`, `global using` e uso qualificado; ignora linhas de comentário
    hits=$(grep -rnE "OwnExpeditor\.$other\b" "$dir" --include=*.cs --include=*.axaml --include=*.xaml \
           --exclude-dir=bin --exclude-dir=obj 2>/dev/null | grep -vE '^[^:]+:[0-9]+:\s*(//|\*|/\*|<!--)' || true)
    [[ -n $hits ]] && { echo "$hits" | sed "s/^/VIOLACAO ($p usa $other): /"; err=1; }
  done
done
(( err )) || echo "CAMADAS: ok"; exit $err
