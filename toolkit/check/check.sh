#!/usr/bin/env bash
# Build + testes com resumo curto.
# uso: toolkit/check/check.sh [--filter <expr>]
# agentes: test-runner, build-fixer, core-dev, sii-engine, ui-dev, test-engineer, git-ops
# saída: <= 20 linhas
source "$(dirname "$0")/../_lib.sh"
LOG=$(mktemp -d); trap 'rm -rf "$LOG"' EXIT
if dotnet build -nologo -v q -clp:NoSummary >"$LOG/b" 2>&1; then
  w=$(grep -c ' warning ' "$LOG/b" || true); echo "BUILD: ok ($w avisos)"
else
  e=$(grep -E ' error ' "$LOG/b" | sort -u)
  echo "BUILD: falhou ($(grep -c . <<<"$e") erros)"
  sed -E "s#$ROOT/##; s# \[.*\]##" <<<"$e" | head -15; exit 1
fi
dotnet test --no-build -nologo -v q --logger "trx;LogFilePrefix=r" --results-directory "$LOG/t" "$@" >"$LOG/o" 2>&1 && rc=0 || rc=$?
c=$(cat "$LOG"/t/*.trx 2>/dev/null | grep -o '<Counters [^>]*' || true)
sum() { grep -oE " $1=\"[0-9]+\"" <<<"$c" | grep -oE '[0-9]+' | paste -sd+ | bc 2>/dev/null || echo 0; }
p=$(sum passed); f=$(sum failed); sk=$(sum notExecuted)
echo "TESTES: $p passaram, $f falharam, $sk pulados"
if (( rc != 0 )); then
  echo "FALHAS:"
  grep -E '^\s+Failed |Assert|Exception' "$LOG/o" | sed -E 's/^\s+//' | head -15
  exit 1
fi
