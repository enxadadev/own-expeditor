#!/usr/bin/env python3
# Gera os .resx do App (Strings*/Gallery*) a partir das tabelas de design/*/strings.md.
# uso: ia/toolkit/gen/gen-resx.py [--check]   (--check não grava: exit 1 se estariam diferentes)
# agentes: ui-dev, ux-designer, test-runner
# saída: 1-5 linhas (conflitos de chave no stderr)
"""Gera os .resx do App a partir das tabelas `Chave | pt-BR | en` de design/*/strings.md.

uso: ia/toolkit/gen/gen-resx.py [--check]
  Strings.resx (en, neutro), Strings.pt-BR.resx e Gallery*.resx (chaves Gallery_*, só Debug)
  em src/OwnExpeditor.App/Resources/. --check não grava: sai com 1 se os arquivos estariam diferentes.

Regras: chave = 1a coluna sem crases, só [A-Za-z0-9_]; `Canvas_*` (mockups) é ignorada.
Seção "## Removidas" de um strings.md: as chaves entre crases (`Prefixo_*` vale como prefixo) somem das fontes de
menor prioridade, até a linha "**Continuam".
Chave repetida em arquivos diferentes: vale a de maior prioridade (ordem de SOURCES) e o
conflito é listado no stderr. Chave com valores diferentes dentro da mesma prioridade = erro.
"""
import re, sys, pathlib
from xml.sax.saxutils import escape

ROOT = pathlib.Path(__file__).resolve().parent.parent.parent.parent
# Maior prioridade primeiro. review-apply é a fonte dos 20 Error_<CoreError> (ADR 0006).
SOURCES = ["controles", "review-apply", "shell", "fretes-pais-cidade", "fretes"]
OUT = ROOT / "src/OwnExpeditor.App/Resources"
KEY = re.compile(r"[A-Za-z][A-Za-z0-9_]*")

HEADER = """<?xml version="1.0" encoding="utf-8"?>
<!-- GERADO por ia/toolkit/gen/gen-resx.py a partir de design/*/strings.md. Não edite à mão. -->
<root>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
"""


def parse():
    table = {}  # key -> (src, pt, en)
    for src in SOURCES:
        f = ROOT / "design" / src / "strings.md"
        for n, line in enumerate(f.read_text(encoding="utf8").splitlines(), 1):
            if not line.startswith("|"):
                continue
            cells = [c.strip() for c in re.split(r"(?<!\\)\|", line.strip())[1:-1]]
            if len(cells) < 3:
                continue
            key = cells[0].strip("`")
            if not KEY.fullmatch(key) or key.startswith("Canvas_") or key == "Chave":
                continue
            pt, en = (c.replace("\\|", "|").replace("\\n", "\n") for c in cells[1:3])
            if key in table:
                if table[key][1:] != (pt, en):
                    prev = table[key][0]
                    if prev == src:
                        sys.exit(f"ERRO: {key} repetida com valores diferentes em {src} ({f.name}:{n})")
                    print(f"conflito: {key}: vale {prev}, ignorado {src}", file=sys.stderr)
                continue
            table[key] = (src, pt, en)
    drop_removed(table)
    return table


def drop_removed(table):
    """Aplica a seção "## Removidas" (design/fretes-pais-cidade/strings.md) às chaves de fontes de menor prioridade."""
    f = ROOT / "design" / "fretes-pais-cidade" / "strings.md"
    patterns, on = [], False
    for line in f.read_text(encoding="utf8").splitlines():
        if line.startswith("## "):
            on = line.strip() == "## Removidas"
        elif line.startswith("**Continuam"):
            on = False
        elif on:
            patterns += re.findall(r"`([A-Za-z0-9_]+\*?)`", line)
    for key in list(table):
        if table[key][0] == "fretes-pais-cidade":
            continue
        if any(key.startswith(p[:-1]) if p.endswith("*") else key == p for p in patterns):
            del table[key]


def render(entries):
    out = [HEADER]
    for k in sorted(entries):
        out.append(f'  <data name="{k}" xml:space="preserve"><value>{escape(entries[k])}</value></data>\n')
    out.append("</root>\n")
    return "".join(out)


def main():
    table = parse()
    files = {}
    for suffix, idx in (("", 2), (".pt-BR", 1)):
        files[f"Strings{suffix}.resx"] = render({k: v[idx] for k, v in table.items() if not k.startswith("Gallery_")})
        files[f"Gallery{suffix}.resx"] = render({k: v[idx] for k, v in table.items() if k.startswith("Gallery_")})
    stale = False
    OUT.mkdir(parents=True, exist_ok=True)
    for name, text in files.items():
        p = OUT / name
        if "--check" in sys.argv:
            stale |= not p.exists() or p.read_text(encoding="utf8") != text
        else:
            p.write_text(text, encoding="utf8")
    print(f"RESX: {len(table)} chaves" + (" (desatualizado)" if stale else ""))
    sys.exit(1 if stale else 0)


main()
