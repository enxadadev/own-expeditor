#!/usr/bin/env bash
# Regras do CLAUDE.md por grep: (a) texto fixo na UI, (b) cor fixa, (c) listas fixas de cidades/cargas, (d) escrita de save sem backup.
# uso: ia/toolkit/check/check-rules.sh [caminhos...]   (padrão: src/; exceções em ia/toolkit/check/check-rules.allow, linhas "regra:caminho")
# agentes: code-reviewer, ui-dev, core-dev, ux-reviewer, test-runner
# saída: "OK ..." ou até 30 linhas "arquivo:linha: [regra] problema"; exit 0 ok, 1 achados, 2 uso errado
[[ ${1:-} == --help || ${1:-} == -h ]] && { sed -n '2,5p' "$0" | sed 's/^# \?//'; exit 0; }
source "$(dirname "$0")/../_lib.sh"
for p in "$@"; do [[ -e $p ]] || { echo "caminho inexistente: $p" >&2; exit 2; }; done
exec python3 -I - "$@" <<'PY'
import re, sys, fnmatch, pathlib
roots = [pathlib.Path(a) for a in sys.argv[1:]] or [pathlib.Path("src")]
MAX = 30
allow = []
af = pathlib.Path("ia/toolkit/check/check-rules.allow")
if af.exists():
    for l in af.read_text(encoding="utf8").splitlines():
        l = l.strip()
        if l and not l.startswith("#") and ":" in l:
            r, p = l.split(":", 1); allow.append((r.strip(), p.strip()))
def allowed(rule, path):
    return any(r == rule and (path == p or path.startswith(p.rstrip("/") + "/") or fnmatch.fnmatch(path, p)) for r, p in allow)

files = []
for r in roots:
    it = [r] if r.is_file() else r.rglob("*")
    files += [p for p in it if p.is_file() and p.suffix in (".cs", ".axaml", ".xaml") and not {"bin", "obj"} & set(p.parts)]
files = sorted(set(files))
found = []
def add(rule, f, n, msg):
    if not allowed(rule, str(f)): found.append(f"{f}:{n}: [{rule}] {msg}")

XATTR = re.compile(r'\b(Text|Content|Header|Watermark|ToolTip\.Tip|AutomationProperties\.Name|Title|Caption)="([^"{][^"]*)"')
KEYLIKE = re.compile(r"^(?:(?:Ctrl|Alt|Shift|Cmd|Win|Meta)\+)*(?:[A-Z]|F\d{1,2}|Enter|Esc|Tab|Space|Del|Up|Down|Left|Right)$")
VMSTR = re.compile(r'\b(Title|Text|Message|Label|Caption|Header|Description|ToolTip|Tooltip|Watermark|Status\w*|Error\w*|Hint)\s*(?:=>|=)\s*(\$?)"([^"]*)"')
COLOR = re.compile(r'#[0-9A-Fa-f]{8}\b|#[0-9A-Fa-f]{6}\b|#[0-9A-Fa-f]{3,4}\b(?=["\s<,)])|\bBrushes\.\w+|\bColor\.Parse\b|\bColor\.From(?:Rgb|Argb)\b|\bColors\.[A-Z]\w+')
CITIES = {c.lower() for c in """Berlin Paris Calais Duisburg Hamburg Amsterdam Rotterdam Prague Praha Warszawa Warsaw Madrid Lisboa Lisbon Roma Rome Milano Milan Bruxelles Brussels Wien Vienna Luxembourg Dijon Lyon London Dover Cambridge Stockholm Oslo Helsinki Kaunas Vilnius Szczecin Katowice Bratislava Budapest Zurich Bern Munchen Munich Frankfurt Hannover Bremen Lille Marseille Barcelona Valencia Bordeaux Tallinn Riga Gdansk Poznan Wroclaw Krakow Kassel Stuttgart Nurnberg Leipzig Dresden Antwerp Liege Genova Torino Napoli""".split()}
CARGOS = {c.lower() for c in """cement fuel timber scrap flour beer lumber steel coal grain milk wheat sand gravel bricks chemicals paper furniture clothes electronics sugar cheese concrete asphalt diesel gasoline petrol tractors cars machinery""".split()}
QSTR = re.compile(r'"([A-Za-z][A-Za-z .\-]*)"')
SAVEFILE = re.compile(r'"[^"]*\.sii[^"]*"|\bI?SaveFileStore\b')
WRITE = re.compile(r'\bFile\.(WriteAll(?:Text|Bytes|Lines)(?:Async)?|AppendAll(?:Text|Lines|Bytes)(?:Async)?|Create|Copy|Open|Move|Replace)\s*\(|\bnew\s+FileStream\s*\(')
# fallback "Brush("Token") ?? Brushes.X": lookup de recurso na mesma linha antes do ??
FALLBACK = re.compile(r'(?:Brush|Resource|Color|Find\w*|TryGet\w*)\s*\(\s*"[^"]+"[^;]*\?\?\s*(?:new\s+SolidColorBrush\([^;]*)?$')
BACKUP = re.compile(r'Backup|\.bak-|bak-')

for f in files:
    txt = f.read_text(encoding="utf-8-sig", errors="replace")
    lines = txt.splitlines()
    parts = set(f.parts)
    is_tokens = f.name in ("Colors.axaml", "Tokens.axaml") or "Themes" in parts
    in_app = "OwnExpeditor.App" in parts
    is_gallery = "Gallery" in parts or "Design" in parts   # só Debug / dados de desenho
    is_xaml = f.suffix in (".axaml", ".xaml")
    cmt = False
    for n, l in enumerate(lines, 1):
        s = l.strip()
        if is_xaml:
            if "<!--" in s and "-->" not in s: cmt = True
            if cmt:
                if "-->" in s: cmt = False
                continue
            if s.startswith("<!--"): continue
        elif s.startswith(("//", "*", "/*")): continue
        # (a)
        if is_xaml and in_app and not is_gallery:
            for m in XATTR.finditer(l):
                v = m.group(2).strip()
                if not re.search(r"[A-Za-z]{2}", v) or KEYLIKE.match(v): continue
                if m.group(1) == "Title" and "Window" not in l and "Title=" in l and "Binding" in l: continue
                add("a", f, n, f'texto fixo {m.group(1)}="{v}" (use {{loc:Tr Chave}})')
        if f.suffix == ".cs" and in_app and not is_gallery and "ViewModel" in f.name:
            for m in VMSTR.finditer(l):
                v = m.group(3)
                if re.fullmatch(r"[A-Za-z0-9]+(?:_[A-Za-z0-9]+)+", v): continue  # chave de recurso
                if re.match(r"[A-Z][a-z]+.*[a-z]", v) and (" " in v or v.endswith((".", "!", "?", ":"))):
                    add("a", f, n, f'string de UI fixa "{v}" (use ILocalizer)')
        # (b)
        if not is_tokens and not is_gallery:
            for m in COLOR.finditer(l):
                if FALLBACK.search(l[:m.start()]) and m.group(0).startswith(("Brushes.", "Color")): continue
                if f.suffix == ".cs" and re.match(r"#[0-9A-Fa-f]{3,4}$", m.group(0)): continue
                add("b", f, n, f"cor fixa {m.group(0)} (use token/DynamicResource)")
    # (c) janelas de 5 linhas
    if "Design" not in parts and "Gallery" not in parts:
        for kind, names in (("cidades", CITIES), ("cargas", CARGOS)):
            for n in range(len(lines)):
                win = " ".join(lines[n:n + 5])
                got = {m.group(1).lower() for m in QSTR.finditer(win)} & names
                if len(got) >= 3 and (n == 0 or len({m.group(1).lower() for m in QSTR.finditer(" ".join(lines[n - 1:n + 4]))} & names) < len(got)):
                    add("c", f, n + 1, f"lista fixa de {kind}: {', '.join(sorted(got))}")
                    break
    # (d) escrita em arquivo de save; granularidade: arquivo inteiro
    if f.suffix == ".cs" and SAVEFILE.search(txt) and not BACKUP.search(txt):
        for n, l in enumerate(lines, 1):
            if WRITE.search(l) and not l.strip().startswith("//"):
                add("d", f, n, "escrita de save sem backup no arquivo (save-safety)")

# dedupe + (c) uma por arquivo/tipo
seen, res = set(), []
for x in found:
    if x not in seen: seen.add(x); res.append(x)
if not res:
    print(f"OK regras: {len(files)} arquivos verificados"); sys.exit(0)
for x in res[:MAX]: print(x)
if len(res) > MAX: print(f"... +{len(res) - MAX}")
sys.exit(1)
PY
