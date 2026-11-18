#!/usr/bin/env bash
# Lista os scripts do toolkit (e subpastas) com a descrição de cada um.
# uso: toolkit/help.sh [--agents]   (--agents agrupa por quem usa; lê as linhas "# uso:" e "# agentes:")
# agentes: chief-architect, core-dev, sii-engine, ui-dev, test-engineer, test-runner, code-reviewer, git-ops, docs-keeper
# saída: 1 linha "pasta/nome — descrição" por script; --agents: 1 linha por agente (scripts sem descrição)
[[ ${1:-} == --help || ${1:-} == -h ]] && { sed -n '2,5p' "$0" | sed 's/^# \?//'; exit 0; }
case "${1:-}" in ""|--agents) ;; *) echo "uso: toolkit/help.sh [--agents]" >&2; exit 2;; esac
source "$(dirname "$0")/_lib.sh"
rows=""
while IFS= read -r f; do
  base=$(basename "$f"); [[ $base == _* ]] && continue
  hdr=$(sed -n '2,12p' "$f" | grep -E '^#' || true)
  desc=$(sed -n '1,12p' "$f" | grep -E '^# ' | grep -vE '^# (uso|agentes|saída|saida):' | head -1 | sed 's/^# //' || true)
  [[ -z $desc ]] && desc=$(sed -n '1,12p' "$f" | grep -m1 -E '^[A-Za-zÀ-ú].*' | grep -v '^#!' | sed 's/^"""//' || true)
  ag=$(sed -n 's/^# agentes: *//p' <<<"$hdr" | head -1)
  rows+="${f#toolkit/}"$'\t'"$desc"$'\t'"${ag:-?}"$'\n'
done < <(find toolkit \( -name bin -o -name obj \) -prune -o -type f \( -name '*.sh' -o -name '*.py' \) -print | sort)
if [[ ${1:-} == --agents ]]; then
  # uma linha por agente: nomes dos scripts (sem repetir a descrição)
  agents=$(awk -F'\t' 'NF{n=split($3,a,/, */); for(i=1;i<=n;i++) print a[i]}' <<<"$rows" | grep -vxE 'todos|os|devs' | sort -u)
  for a in $agents; do
    list=$(awk -F'\t' -v a="$a" 'NF{n=split($3,x,/, */); for(i=1;i<=n;i++) if(x[i]==a){print $1; break}}' <<<"$rows" | sed 's#.*/##; s#\.\(sh\|py\)$##' | paste -sd' ')
    echo "$a: $list"
  done
else
  awk -F'\t' 'NF{print $1 " — " $2}' <<<"$rows"
fi
