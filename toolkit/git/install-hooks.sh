#!/usr/bin/env bash
# Instala o hook commit-msg que roda lint-commit-msg.sh (vale para todos os worktrees).
# uso: toolkit/git/install-hooks.sh
# agentes: git-ops
# saída: 1 linha
source "$(dirname "$0")/../_lib.sh"
hooks=$(git rev-parse --path-format=absolute --git-common-dir)/hooks
cat > "$hooks/commit-msg" <<'H'
#!/usr/bin/env bash
# Funciona em worktrees com layout novo (toolkit/git/) e antigo (toolkit/).
top=$(git rev-parse --show-toplevel)
for s in "$top/toolkit/git/lint-commit-msg.sh" "$top/toolkit/lint-commit-msg.sh"; do
  [[ -f $s ]] && exec "$s" "$1"
done
echo "aviso: lint-commit-msg.sh não encontrado em $top/toolkit; mensagem não validada" >&2
exit 0
H
chmod +x "$hooks/commit-msg"; echo "hook instalado: $hooks/commit-msg"
