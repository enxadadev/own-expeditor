#!/usr/bin/env bash
# Próximo número OWNE livre (maior do histórico de todas as branches + 1).
# uso: toolkit/git/next-owne.sh
# agentes: git-ops
# saída: 1 linha
source "$(dirname "$0")/../_lib.sh"
git fetch -q --all 2>/dev/null || true
n=$(git log --all --format=%s | grep -oE '^OWNE-[0-9]+' | cut -d- -f2 | sort -n | tail -1)
echo "OWNE-$(( ${n:-0} + 1 ))"
