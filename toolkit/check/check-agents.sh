#!/usr/bin/env bash
# Valida ia/agents/*.md e ia/skills/*/SKILL.md: frontmatter, nomes, links em .claude/, tabelas do ia/README.md e scripts citados.
# uso: toolkit/check/check-agents.sh   (description > 200 caracteres só avisa)
# agentes: docs-keeper, code-reviewer, chief-architect, test-runner
# saída: "OK ..." ou até 30 linhas "arquivo:linha: problema" (avisos com "AVISO"); exit 0 ok, 1 problemas
[[ ${1:-} == --help || ${1:-} == -h ]] && { sed -n '2,5p' "$0" | sed 's/^# \?//'; exit 0; }
[[ $# -eq 0 ]] || { echo "uso: toolkit/check/check-agents.sh" >&2; exit 2; }
source "$(dirname "$0")/../_lib.sh"
exec python3 -I - <<'PY'
import re, sys, pathlib, os
MAX = 30
IA = pathlib.Path("ia")
errs, warns = [], []
def front(p):
    lines = p.read_text(encoding="utf8").splitlines()
    if not lines or lines[0].strip() != "---":
        errs.append(f"{p}:1: sem frontmatter"); return {}, lines
    d, i, cur = {}, 1, None
    while i < len(lines) and lines[i].strip() != "---":
        m = re.match(r"^([A-Za-z_-]+):\s*(.*)$", lines[i])
        if m:
            cur = m.group(1); v = m.group(2).strip()
            d[cur] = ("" if v in (">", "|", ">-", "|-") else v.strip("\"'"))
            d.setdefault("_line_" + cur, i + 1)
        elif cur and lines[i].strip():
            d[cur] = (d[cur] + " " + lines[i].strip()).strip()
        i += 1
    if i >= len(lines): errs.append(f"{p}:1: frontmatter sem fechamento")
    return d, lines
items = []  # (kind, name, path)
for p in sorted((IA / "agents").glob("*.md")): items.append(("agent", p.stem, p))
for p in sorted((IA / "skills").glob("*/SKILL.md")): items.append(("skill", p.parent.name, p))
for kind, name, p in items:
    d, lines = front(p)
    if not d: continue
    for k in ("name", "description"):
        if not d.get(k): errs.append(f"{p}:1: frontmatter sem {k}")
    if d.get("name") and d["name"] != name: errs.append(f"{p}:{d['_line_name']}: name '{d['name']}' != '{name}'")
    ds = d.get("description", "")
    if len(ds) > 200: warns.append(f"{p}:{d.get('_line_description', 1)}: AVISO description com {len(ds)} caracteres (>200)")
    # link em .claude/
    link = pathlib.Path(".claude") / ("agents" if kind == "agent" else "skills") / (p.name if kind == "agent" else name)
    if not link.is_symlink() and not link.exists(): errs.append(f"{link}:1: link ausente para {p}")
    elif link.is_symlink() and not link.resolve().exists(): errs.append(f"{link}:1: link quebrado")
    elif link.is_symlink() and link.resolve() != (p.resolve() if kind == "agent" else p.parent.resolve()):
        errs.append(f"{link}:1: link aponta para {os.readlink(link)}, esperado {p}")
# itens em .claude sem origem
for sub, pat in (("agents", "*.md"), ("skills", "*")):
    base = pathlib.Path(".claude") / sub
    if base.is_dir():
        have = {n for k, n, _ in items if k == ("agent" if sub == "agents" else "skill")}
        for l in sorted(base.glob(pat)):
            n = l.stem if sub == "agents" else l.name
            if n not in have: errs.append(f"{l}:1: sem origem em ia/")
# README
rd = IA / "README.md"
rl = rd.read_text(encoding="utf8").splitlines()
def section(title_re):
    out, on = [], False
    for n, l in enumerate(rl, 1):
        if re.match(r"^#{1,3} ", l): on = bool(re.search(title_re, l))
        elif on: out.append((n, l))
    return out
def first_cells(sec):
    r = set()
    for n, l in sec:
        if l.startswith("|"):
            c = [x.strip().strip("`") for x in l.strip().strip("|").split("|")]
            if c and re.fullmatch(r"[a-z][a-z0-9-*]*", c[0]): r.add(c[0])
    return r
ag_tab = first_cells(section(r"Quem faz o quê"))
sk_tab = first_cells(section(r"Skills$")) | first_cells(section(r"Workflows"))
for kind, name, p in items:
    if kind == "agent" and name not in ag_tab: errs.append(f"{rd}:1: agente {name} fora da tabela de agentes")
    if kind == "skill":
        ok = name in sk_tab or (name.startswith("wf-") and ("wf-*" in sk_tab and name in "\n".join(rl)))
        if not ok: errs.append(f"{rd}:1: skill {name} fora da tabela de skills/workflows")
for a in sorted(ag_tab - {n for k, n, _ in items if k == "agent"}): errs.append(f"{rd}:1: tabela cita agente inexistente {a}")
for a in sorted(x for x in first_cells(section(r"Skills$")) if x != "wf-*" and x not in {n for k, n, _ in items if k == "skill"}):
    errs.append(f"{rd}:1: tabela cita skill inexistente {a}")
# scripts citados
SC = re.compile(r"toolkit/([A-Za-z0-9_.\-/]+\.(?:sh|py))")
files = [p for _, _, p in items] + [rd] + sorted((IA / "skills").glob("*/*.md"))
for p in dict.fromkeys(files):
    for n, l in enumerate(p.read_text(encoding="utf8").splitlines(), 1):
        for m in SC.finditer(l):
            if not pathlib.Path("toolkit", m.group(1)).exists() and "<" not in m.group(0):
                errs.append(f"{p}:{n}: script citado não existe: toolkit/{m.group(1)}")
res = errs + warns
if not res:
    print(f"OK agentes: {sum(1 for k,_,_ in items if k=='agent')} agentes, {sum(1 for k,_,_ in items if k=='skill')} skills"); sys.exit(0)
for x in res[:MAX]: print(x)
if len(res) > MAX: print(f"... +{len(res) - MAX}")
sys.exit(1 if errs else 0)
PY
