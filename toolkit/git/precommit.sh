#!/usr/bin/env bash
# Prepara a proposta de commit para o git-ops em uma chamada.
# uso: toolkit/git/precommit.sh   → branch, próximo OWNE, versão, arquivos, alertas
# agentes: git-ops
# saída: ~15 linhas
source "$(dirname "$0")/../_lib.sh"
br=$(git branch --show-current)
echo "BRANCH: $br$([[ $br == "$(main_branch)" ]] && echo '  (ALERTA: branch principal — criar branch)')"
echo "PROXIMO: $(toolkit/git/next-owne.sh)"
echo "VERSAO: $(props_version)"
echo "ARQUIVOS:"; git status --porcelain | sed 's/^/  /'
bad=$(git status --porcelain | awk '{print $NF}' | grep -E '\.sii$|\.sii\.bak-|(^|/)(bin|obj|samples|publish)/' || true)
[[ -n $bad ]] && { echo "ALERTA: arquivos proibidos:"; sed 's/^/  /' <<<"$bad"; }
if ! git diff HEAD --quiet -- Directory.Build.props 2>/dev/null; then
  grep -q "## $(props_version)" docs/changelog.md 2>/dev/null || echo "ALERTA: versão mudou e docs/changelog.md não tem '## $(props_version)'"
fi
echo "AREAS:"; git status --porcelain | awk '{print $NF}' | cut -d/ -f1-2 | sort -u | sed 's/^/  /'
