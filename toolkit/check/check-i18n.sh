#!/usr/bin/env bash
# Confere os .resx do App: chaves faltando entre pt-BR e en, chaves sem uso e chaves usadas sem definição.
# uso: toolkit/check/check-i18n.sh   (Strings*.resx e Gallery*.resx em src/OwnExpeditor.App/Resources; fonte: design/*/strings.md via gen-resx.py)
# agentes: ui-dev, ux-reviewer, code-reviewer, test-runner
# saída: erros (até 30 linhas "arquivo:linha: problema") e exit 1 se chave USADA sem definição ou pt-BR x en divergente/vazio;
#        chave definida sem uso é só "AVISO n chaves sem uso: ..." (até 30) com exit 0; "OK ..." se nada
[[ ${1:-} == --help || ${1:-} == -h ]] && { sed -n '2,7p' "$0" | sed 's/^# \?//'; exit 0; }
[[ $# -eq 0 ]] || { echo "uso: toolkit/check/check-i18n.sh" >&2; exit 2; }
source "$(dirname "$0")/../_lib.sh"
exec python3 -I - <<'PY'
import re, sys, pathlib
MAX = 30
RES = pathlib.Path("src/OwnExpeditor.App/Resources")
if not RES.is_dir(): print("sem Resources do App", file=sys.stderr); sys.exit(2)
DATA = re.compile(r'<data name="([^"]+)"')
def keys(p):
    return {m.group(1): n for n, l in enumerate(p.read_text(encoding="utf8").splitlines(), 1) for m in [DATA.search(l)] if m}
out = []
defined = {}
for base in sorted({p.name.split(".")[0] for p in RES.glob("*.resx")}):
    en, pt = RES / f"{base}.resx", RES / f"{base}.pt-BR.resx"
    for p in (en, pt):
        if not p.exists(): out.append(f"{p}:1: arquivo ausente")
    if not (en.exists() and pt.exists()): continue
    ke, kp = keys(en), keys(pt)
    for k in sorted(ke.keys() - kp.keys()): out.append(f"{en}:{ke[k]}: {k} falta em pt-BR")
    for k in sorted(kp.keys() - ke.keys()): out.append(f"{pt}:{kp[k]}: {k} falta em en")
    for k in ke: defined.setdefault(k, en)
    # valores vazios
    for p, ks in ((en, ke), (pt, kp)):
        for m in re.finditer(r'<data name="([^"]+)"[^>]*>\s*<value>\s*</value>', p.read_text(encoding="utf8")):
            out.append(f"{p}:{ks.get(m.group(1), 1)}: {m.group(1)} com valor vazio")
# fontes
srcs = [p for p in pathlib.Path("src").rglob("*") if p.suffix in (".cs", ".axaml", ".xaml") and not {"bin", "obj"} & set(p.parts)]
used = {}   # key -> (file, line) primeira referência
plural = set(); prefixes = set(); suffixes = set(); text_all = []
def use(k, f, n, pl=False):
    used.setdefault(k, (f, n))
    if pl: plural.add(k)
for f in srcs:
    txt = f.read_text(encoding="utf-8-sig", errors="replace"); text_all.append(txt)
    for n, l in enumerate(txt.splitlines(), 1):
        if l.lstrip().startswith(("//", "<!--", "*", "/*")): continue
        for m in re.finditer(r"\{(?:\w+:)?Tr\s+([A-Za-z][A-Za-z0-9_]*)", l): use(m.group(1), f, n)
        for m in re.finditer(r'\bGet\(\s*"([A-Z][A-Za-z0-9]*(?:_[A-Za-z0-9]+)+)"', l): use(m.group(1), f, n)
        for m in re.finditer(r'\bGetPlural\(\s*"([A-Za-z][A-Za-z0-9_]*)"', l): use(m.group(1), f, n, True)
        for m in re.finditer(r'\b(?:[Ll]oc(?:alizer)?(?:\.Instance)?|_loc|_localizer)\[\s*"([A-Z][A-Za-z0-9_]*[A-Za-z0-9])"', l): use(m.group(1), f, n)
        for m in re.finditer(r'\$"([A-Za-z][A-Za-z0-9_]*_)\{', l): prefixes.add(m.group(1))
        for m in re.finditer(r'"([A-Za-z][A-Za-z0-9_]*_)"\s*\+', l): prefixes.add(m.group(1))
        for m in re.finditer(r'\+\s*"(_[A-Za-z0-9_]+)"', l): suffixes.add(m.group(1))     # Prefix + "_Title"
        for m in re.finditer(r'\$"\{[^}"]+\}(_[A-Za-z0-9_]+)"', l): suffixes.add(m.group(1))  # $"{prefix}_X"
        if "Keys" in f.name or f.name in ("LoadProgress.cs",):
            for m in re.finditer(r'"([A-Z][A-Za-z0-9]*(?:_[A-Za-z0-9]+)+)"', l): use(m.group(1), f, n)
# chaves montadas: prefixo literal (ex. "OpenError_Parse") + sufixo ("_Title") visto no código
lits = {m for t_ in text_all for m in re.findall(r'"([A-Za-z][A-Za-z0-9_]*)"', t_)}
unused = []
alltext = "\n".join(text_all)
def defd(k): return k in defined
for k, (f, n) in sorted(used.items()):
    if k in plural:
        if not any(defd(k + s) for s in ("_Other", "_One", "_Zero")): out.append(f"{f}:{n}: {k} usada (plural) sem definição")
    elif not defd(k):
        out.append(f"{f}:{n}: {k} usada sem definição")
for k, res in sorted(defined.items()):
    ks = {k}
    for s in ("_One", "_Other", "_Zero"):
        if k.endswith(s): ks.add(k[: -len(s)])
    if ks & set(used): continue
    if any(k.startswith(p) for p in prefixes): continue
    if any(re.search(r"(?<![A-Za-z0-9_])" + re.escape(x) + r"(?![A-Za-z0-9_])", alltext) for x in ks): continue
    if any(k.endswith(s) and k != s for s in suffixes) and any(k.startswith(w) for w in lits): continue
    unused.append(f"{res}:{keys(res)[k]}: {k}")
if out:
    for x in out[:MAX]: print(x)
    if len(out) > MAX: print(f"... +{len(out) - MAX}")
if unused:
    print(f"AVISO {len(unused)} chaves sem uso: " + "; ".join(u.split(": ", 1)[1] for u in unused[:MAX]) + (f"; ... +{len(unused) - MAX}" if len(unused) > MAX else ""))
if out: sys.exit(1)
if not unused: print(f"OK i18n: {len(defined)} chaves, pt-BR = en, todas usadas e definidas")
sys.exit(0)
PY
