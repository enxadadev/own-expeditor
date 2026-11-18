#!/usr/bin/env bash
# Sobe a versão M.X.Y em Directory.Build.props.
# uso: toolkit/git/bump-version.sh patch|minor|major [--dry-run]
# agentes: git-ops
# saída: 1-3 linhas
source "$(dirname "$0")/../_lib.sh"
old=$(props_version); [[ $old =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)$ ]] || die "versão inválida: $old"
M=${BASH_REMATCH[1]} X=${BASH_REMATCH[2]} Y=${BASH_REMATCH[3]}
case ${1:-} in
  patch) Y=$((Y+1)) ;; minor) X=$((X+1)); Y=0 ;; major) M=$((M+1)); X=0; Y=0 ;;
  *) die "uso: bump-version.sh patch|minor|major [--dry-run]" ;;
esac
new="$M.$X.$Y"
[[ ${2:-} == --dry-run ]] || sed -i "s:<Version>$old</Version>:<Version>$new</Version>:" Directory.Build.props
echo "$old -> $new"
