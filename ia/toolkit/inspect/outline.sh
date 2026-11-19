#!/usr/bin/env bash
# Esqueleto da API pública (namespace, tipos, membros public/internal) de .cs sem corpo.
# uso: ia/toolkit/inspect/outline.sh <arquivo.cs|pasta>   (pasta = recursivo, agrupado por arquivo)
# agentes: core-dev, sii-engine, ui-dev, test-engineer, code-reviewer, chief-architect
# saída: máx. 200 linhas (resto vira "... +N"); formato "  <linha> <assinatura>"
[[ ${1:-} == --help || ${1:-} == -h ]] && { sed -n '2,5p' "$0" | sed 's/^# \?//'; exit 0; }
[[ $# -eq 1 ]] || { echo "uso: ia/toolkit/inspect/outline.sh <arquivo.cs|pasta>" >&2; exit 2; }
source "$(dirname "$0")/../_lib.sh"
exec python3 -I - "$1" <<'PY'
import re, sys, pathlib
MAX = 200
arg = pathlib.Path(sys.argv[1])
if not arg.exists():
    print(f"{arg}: nao existe", file=sys.stderr); sys.exit(2)
files = [arg] if arg.is_file() else sorted(p for p in arg.rglob("*.cs")
        if not {"bin", "obj"} & set(p.parts) and not p.name.endswith((".g.cs", ".Designer.cs")))
MODS = r"(?:(?:public|internal|private|protected|static|sealed|abstract|partial|readonly|unsafe|file|new|ref|required|virtual|override|extern|async|const|volatile)\s+)*"
TYPE = re.compile(r"^\s*(?:\[[^\]]*\]\s*)*(" + MODS + r")(class|record\s+struct|record\s+class|record|interface|enum|struct)\s+([A-Za-z_]\w*)")
NS = re.compile(r"^\s*namespace\s+([\w.]+)")

def clean(line):
    line = re.sub(r'@?"(?:[^"\\]|\\.)*"', '""', line)
    line = re.sub(r"'(?:[^'\\]|\\.)'", "''", line)
    return re.sub(r"//.*$", "", line)

def sig_of(lines, i):
    """Junta linhas a partir de i ate '{', '=>' ou ';' fora de parenteses."""
    buf, depth = "", 0
    for j in range(i, min(i + 12, len(lines))):
        t = clean(lines[j]).strip()
        for k, ch in enumerate(t):
            if ch == "(": depth += 1
            elif ch == ")": depth -= 1
            elif depth <= 0 and (ch in "{;" or t[k:k+2] == "=>" or (ch == "=" and t[k:k+2] != "==")):
                rest = t[k:]
                acc = ""
                if ch == "{":
                    m = re.match(r"\{\s*((?:(?:get|set|init)\s*;\s*)+)\}", rest)
                    if m: acc = " { " + re.sub(r"\s+", " ", m.group(1)).strip() + " }"
                    elif re.match(r"\{\s*get\b", rest) and "(" not in buf + t[:k]: acc = " { get; }"
                elif t[k:k+2] == "=>" and "(" not in buf + t[:k] and not re.search(r"\b(operator|event)\b", buf + t[:k]): acc = " { get; }"
                return re.sub(r"\s+", " ", (buf + " " + t[:k]).strip()) + acc
        buf += " " + t
    return re.sub(r"\s+", " ", buf.strip())

out = []
for f in files:
    lines = f.read_text(encoding="utf-8-sig", errors="replace").splitlines()
    block = []
    depth = 0
    stack = []  # (open_depth, kind, visible)
    in_comment = False
    ns_file = None
    for i, raw in enumerate(lines):
        if in_comment:
            if "*/" in raw: in_comment = False; raw = raw.split("*/", 1)[1]
            else: continue
        raw = re.sub(r"/\*.*?\*/", "", raw)
        if "/*" in raw: in_comment = True; raw = raw.split("/*", 1)[0]
        c = clean(raw)
        if not c.strip() or c.strip().startswith("#"):
            depth += c.count("{") - c.count("}"); continue
        top = stack[-1] if stack else None
        in_type_body = top is not None and depth == top[0] + 1
        entry = None
        m = NS.match(c)
        if m and (top is None):
            tag = f"namespace {m.group(1)}"
            entry = (i + 1, 0, tag)
        elif (top is None and depth <= (1 if any("namespace" in b for b in [x[2] for x in block]) else 0)) or in_type_body or (top is None):
            t = TYPE.match(c)
            if t:
                mods = t.group(1).split()
                kind_vis = "private" not in mods and not (in_type_body and not ({"public","internal","protected"} & set(mods)))
                if top is not None and not top[2]: kind_vis = False
                if kind_vis:
                    sg = sig_of(lines, i)
                    if t.group(2) == "enum":
                        vals = []
                        mm = re.search(r"\{(.*)\}", clean(lines[i]))
                        if mm: vals = [v.strip().split("=")[0].strip() for v in mm.group(1).split(",") if v.strip()]
                        for l2 in ([] if "}" in clean(lines[i]) else lines[i + 1:i + 60]):
                            l2 = clean(l2).strip()
                            if l2 in ("{", ""): continue
                            if l2.startswith("}"): break
                            vals += [v.strip().split("=")[0].strip() for v in l2.split(",") if v.strip() and not v.strip().startswith("[")]
                        sg += " { " + ", ".join(vals) + " }"
                    entry = (i + 1, len(stack) + (1 if block else 0), sg)
                    stack_vis = True
                else:
                    stack_vis = False
                # registra tipo (aberto quando aparecer '{' ou ja nesta linha)
                stack.append([depth, t.group(2), stack_vis, False])
            elif in_type_body and top[2]:
                mods = re.match(r"^\s*(?:\[[^\]]*\]\s*)*((?:\w+\s+)*)", c).group(1).split()
                is_iface = top[1] == "interface"
                if top[1] == "enum":
                    pass
                elif ({"public", "internal"} & set(mods) or (is_iface and not {"private"} & set(mods))) \
                        and re.search(r"[\w>\])]\s*(?:\(|\{|;|=)", c + (clean(lines[i+1]) if i + 1 < len(lines) else "")):
                    s = sig_of(lines, i)
                    if s and not s.startswith(("return ", "using ", "var ")):
                        entry = (i + 1, len(stack) + (1 if block else 0), s)
        if entry:
            if entry[2].startswith("namespace "): block.append(entry)
            out.append((f, entry))
        # enum: lista valores
        opens = c.count("{"); closes = c.count("}")
        depth += opens - closes
        while stack and depth <= stack[-1][0] and (closes or stack[-1][0] < depth + 1) and (opens or closes):
            if depth <= stack[-1][0] and (stack[-1][3] or closes): stack.pop()
            else: break
        for s in stack:
            if depth > s[0]: s[3] = True
    # arquivos sem entradas sao omitidos
last = None; res = []
for f, (ln, lvl, text) in out:
    if f != last:
        res.append(str(f)); last = f
    res.append(f"  {'  ' * lvl}{ln} {text}")
for l in res[:MAX]: print(l)
if len(res) > MAX: print(f"... +{len(res) - MAX}")
PY
