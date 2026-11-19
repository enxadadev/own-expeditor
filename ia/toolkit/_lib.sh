# Funções comuns do toolkit. Uso: source "$(dirname "$0")/_lib.sh"
set -euo pipefail
ROOT="$(git -C "$(dirname "${BASH_SOURCE[0]}")" rev-parse --show-toplevel)"
cd "$ROOT"
die() { echo "ERRO: $*" >&2; exit 1; }
props_version() { sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props; }
# Branch principal (master/main, local ou remota).
main_branch() { for b in main master origin/main origin/master; do git rev-parse -q --verify "$b" >/dev/null && { echo "$b"; return; }; done; die "branch principal não encontrada"; }
