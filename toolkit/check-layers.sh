#!/usr/bin/env bash
# Verifica a regra de camadas pelos ProjectReference (App → Core → Data/Sii → Domain).
source "$(dirname "$0")/_lib.sh"
declare -A ALLOW=([App]="Core Domain" [Core]="Data Sii Domain" [Data]="Sii Domain" [Sii]="Domain" [Domain]="")
err=0
for p in "${!ALLOW[@]}"; do
  csproj=src/OwnExpeditor.$p/OwnExpeditor.$p.csproj; [[ -f $csproj ]] || continue
  for ref in $(grep -oE 'OwnExpeditor\.[A-Za-z]+\.csproj' "$csproj" | sed 's/OwnExpeditor\.\(.*\)\.csproj/\1/'); do
    [[ " ${ALLOW[$p]} " == *" $ref "* ]] || { echo "VIOLACAO: $p -> $ref"; err=1; }
  done
done
# usings que atravessam camadas sem ProjectReference também contam
for p in Domain Sii Data; do
  grep -rlE 'using OwnExpeditor\.(App|Core)\b' src/OwnExpeditor.$p --include=*.cs 2>/dev/null | sed "s/^/VIOLACAO (using): /" && err=1
done
(( err )) || echo "CAMADAS: ok"; exit $err
