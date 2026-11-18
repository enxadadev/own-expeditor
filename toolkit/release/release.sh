#!/usr/bin/env bash
# Gera o exe self-contained win-x64 + notas.
# uso: toolkit/release/release.sh
# agentes: release-builder
# saída: caminho de publish/<versão>
source "$(dirname "$0")/../_lib.sh"
v=$(props_version); out=publish/$v
toolkit/check/check.sh >/dev/null || die "testes falhando — rode toolkit/check/check.sh"
dotnet publish src/OwnExpeditor.App -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$out" -nologo -v q >/dev/null || die "publish falhou"
exe=$out/OwnExpeditor.App.exe; [[ -f $exe ]] || die "exe não gerado"
awk -v v="## $v" 'index($0,v)==1{p=1;print;next} p&&/^## /{exit} p' docs/changelog.md > "$out/RELEASE-NOTES.md"
[[ -s $out/RELEASE-NOTES.md ]] || echo "ALERTA: docs/changelog.md sem seção '## $v'"
echo "RELEASE: $v  EXE: $exe  TAMANHO: $(du -h "$exe" | cut -f1)"
