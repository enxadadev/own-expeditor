#!/usr/bin/env bash
# Instala o hook commit-msg que roda lint-commit-msg.sh (vale para todos os worktrees).
source "$(dirname "$0")/_lib.sh"
hooks=$(git rev-parse --path-format=absolute --git-common-dir)/hooks
cat > "$hooks/commit-msg" <<'H'
#!/usr/bin/env bash
exec "$(git rev-parse --show-toplevel)/toolkit/lint-commit-msg.sh" "$1"
H
chmod +x "$hooks/commit-msg"; echo "hook instalado: $hooks/commit-msg"
