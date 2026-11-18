"""Traça mascot-head.png em SVG colorido (vtracer) com sombras e contorno tratados.
uso (na pasta design/brand, com um Python que tenha vtracer e Pillow): python trace-mascot.py [saida.svg]

1. Pré-processo: alfa binário e faixa da borda pintada com o contorno escuro (sem halo claro do JPG).
2. Cores: vtracer em camadas empilhadas.
3. Sombras adaptativas: uma camada de tom médio que se sobrepõe a outra de cor muito parecida é um degrau de
   degradê → desfoque forte (some a faixa). Se a cor vizinha mais próxima é bem diferente, é borda de sombra
   de verdade → desfoque leve (fica marcada). Brancos ficam nítidos.
4. Viseira: região fechada pelo contorno preto, traçada à parte em 2x com degraus de cor mais finos
   (degradê do escuro ao verde-amarelo) e o risco branco do reflexo como camada própria, nítida.
5. Traço: os pixels bem escuros (contorno, olhos do pato, letras) são traçados de novo em 2x com mais
   suavização e desenhados por cima, com o contorno liso.
6. Tudo recortado pela silhueta."""
import pathlib, re, sys, vtracer
from collections import deque
from PIL import Image, ImageChops, ImageFilter
here = pathlib.Path(__file__).parent
out = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else here / "mascot-head-traced.svg"
BAND_BLUR, EDGE_BLUR, LINE_BLUR = 4.5, 0.9, 0.35
HIGHLIGHT = 150  # tons claros (reflexos da viseira, brilhos) só recebem o desfoque leve
BAND_DIST = 20          # distância de cor (RGB) até a vizinha mais próxima que conta como degrau de degradê
DARK, LIGHT, INK = 50, 215, 40  # luminância: contorno / branco / pixels de tinta do traço
NEUTRAL, STROKE, SOLID = 22, 13, 24  # tinta: dispersão RGB máx. (sem cor), largura (px em 2x) acima da qual é mancha, preto puro

im = Image.open(here / "mascot-head.png").convert("RGBA")
w, h = im.size
a = im.getchannel("A").point(lambda v: 255 if v >= 128 else 0)
inner = a.filter(ImageFilter.MinFilter(5))
rgb = Image.composite(im.convert("RGB"), Image.new("RGB", im.size, (22, 22, 24)), inner)
def visor_mask(rgb, seed=(300, 300), line_lum=70, line_spread=30):
    """Região fechada pelo contorno preto da viseira: preenchimento a partir de `seed` por tudo que não é traço."""
    w, h = rgb.size
    sm = rgb.filter(ImageFilter.GaussianBlur(0.8)); px = sm.load()
    wall = bytearray(w * h)
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            if 0.3*r + 0.59*g + 0.11*b < line_lum and max(r, g, b) - min(r, g, b) < line_spread: wall[y*w+x] = 1
    seen = bytearray(w * h); q = deque([seed]); out = Image.new("L", (w, h), 0); op = out.load()
    while q:
        x, y = q.popleft()
        if not (0 <= x < w and 0 <= y < h) or seen[y*w+x] or wall[y*w+x]: continue
        seen[y*w+x] = 1; op[x, y] = 255
        q.extend(((x+1,y),(x-1,y),(x,y+1),(x,y-1)))
    out = out.filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.MinFilter(5)); op = out.load()
    # buracos fechados (canto mais escuro da viseira, que parece traço) entram na máscara
    bg = bytearray(w * h); q = deque([(x, 0) for x in range(w)] + [(x, h - 1) for x in range(w)]
                                     + [(0, y) for y in range(h)] + [(w - 1, y) for y in range(h)])
    while q:
        x, y = q.popleft()
        if not (0 <= x < w and 0 <= y < h) or bg[y*w+x] or op[x, y] == 255: continue
        bg[y*w+x] = 1; q.extend(((x+1,y),(x-1,y),(x,y+1),(x,y-1)))
    for y in range(h):
        for x in range(w):
            if not bg[y*w+x]: op[x, y] = 255
    return out
def blur(fid, sd): return (f'<filter id="{fid}" x="-5%" y="-5%" width="110%" height="110%">'
                           f'<feGaussianBlur stdDeviation="{sd}"/></filter>')
def lum_hex(p):
    c = re.search(r'fill="#([0-9A-Fa-f]{6})"', p).group(1)
    r, g, b = (int(c[i:i + 2], 16) for i in (0, 2, 4)); return 0.3 * r + 0.59 * g + 0.11 * b
tmp = here / "_t.png"
def trace(img, dst, **kw):
    img.save(tmp); vtracer.convert_image_to_svg_py(str(tmp), str(dst), **kw); t = dst.read_text(); dst.unlink(); return t

src = rgb.copy(); src.putalpha(a)
raw = trace(src, here / "_c.svg", colormode="color", hierarchical="stacked", mode="spline", filter_speckle=2,
            color_precision=8, layer_difference=6, corner_threshold=60, length_threshold=3.5, max_iterations=10,
            splice_threshold=45, path_precision=2)
sil = Image.new("RGB", im.size, "white"); sil.paste((0, 0, 0), mask=a)
clip = "".join(re.findall(r"<path [^>]*/>", trace(sil, here / "_s.svg", colormode="binary", mode="spline", filter_speckle=8)))
# traço em 2x: tinta = pixels muito escuros (suavizados antes de binarizar para a borda sair lisa)
# tinta = escuro E neutro (a viseira escura é verde, não entra) E traço fino (manchas grandes = sombra, não entram)
big = rgb.resize((w * 2, h * 2), Image.LANCZOS).filter(ImageFilter.GaussianBlur(1.2))
r_, g_, b_ = big.split()
spread = ImageChops.subtract(ImageChops.lighter(ImageChops.lighter(r_, g_), b_), ImageChops.darker(ImageChops.darker(r_, g_), b_))
dark = big.convert("L").point(lambda v: 255 if v < INK else 0)
neutral = spread.point(lambda v: 255 if v < NEUTRAL else 0)
mask = ImageChops.multiply(dark, neutral)
blobs = mask.filter(ImageFilter.MinFilter(STROKE)).filter(ImageFilter.MaxFilter(STROKE))  # abertura: só o que é largo
mask = ImageChops.subtract(mask, blobs)
solid = ImageChops.multiply(big.convert("L").point(lambda v: 255 if v < SOLID else 0), neutral)  # preto puro (olhos do pato): entra em qualquer largura
mask = ImageChops.lighter(mask, solid)
mask = ImageChops.subtract(mask, visor_mask(rgb, (300, 300)).filter(ImageFilter.MinFilter(11)).resize((w * 2, h * 2)))
ink = mask.point(lambda v: 0 if v > 127 else 255).convert("RGB")
ink.paste((255, 255, 255), mask=a.resize((w * 2, h * 2)).point(lambda v: 255 - v))
inksvg = trace(ink, here / "_i.svg", colormode="binary", mode="spline", filter_speckle=6, corner_threshold=90,
               length_threshold=6.0, splice_threshold=60, path_precision=2)
ink_paths = "".join(p.replace("<path ", '<path fill="#141414" ', 1) if 'fill=' not in p else p
                    for p in re.findall(r"<path [^>]*/>", inksvg))

# viseira à parte: faixas de contorno empilhadas por família de cor.
# A viseira tem três famílias: o verde (degradê do quase preto ao verde vivo), o reflexo amarelo saturado e o
# risco claro/branco. Em cada família, os pixels viram níveis de luminância; cada nível é uma forma traçada com
# todos os pixels pelo menos tão claros (da família), pintada com a cor média da faixa e com borda suave.
# Empilhando verde → amarelo → claro, o degradê e o reflexo se refazem com as cores do original.
VISOR_SEED = (300, 300)
REFLEX_SMOOTH, REFLEX_STOPS = 5, 8   # suavização das bordas (± linhas) e paradas do degradê de cada faixa
widest = lambda rs: max(rs, key=lambda r: r[1] - r[0])
REFLEX = [  # (faixa, classes da forma, classes da cor, qual trecho da linha, desfoque da borda, trecho mínimo em px, trechos)
    ("yellow", "ypW", "y", lambda rs: (rs[0][0], rs[-1][1]), 1.4, 3, "main"),  # forma: região clara toda; cor: só o amarelo
    ("pale", "p", "p", widest, 0.6, 8, "main"),                                # a faixa clara vertical (só o trecho principal)
    ("white", "W", "W", widest, 0.5, 3, "all"),                                # risco branco de cima e filete de baixo
]
FAMILIES = [  # (nome, pertence?, níveis, desfoque do mais escuro → do mais claro)
    ("green", lambda c: True, 20, (4.0, 2.0)),
]
def lumc(c): return 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]
def hexc(c): return "#%02x%02x%02x" % tuple(int(round(v)) for v in c)
def mean(cs): return tuple(sum(c[k] for c in cs) / len(cs) for k in range(3))
vmask = visor_mask(rgb, VISOR_SEED)
vm2 = vmask.resize((w * 2, h * 2), Image.LANCZOS).point(lambda v: 255 if v >= 128 else 0)
vsmooth = rgb.filter(ImageFilter.GaussianBlur(0.8))
vpx, mpx = vsmooth.load(), vmask.load()
allpts = [(x, y, vpx[x, y]) for y in range(h) for x in range(w) if mpx[x, y] == 255]
vsil = Image.new("RGB", vm2.size, "white"); vsil.paste((0, 0, 0), mask=vm2)
vclip = "".join(re.findall(r"<path [^>]*/>", trace(vsil, here / "_vs.svg", colormode="binary", mode="spline", filter_speckle=8)))
fam_of = {}
for x, y, c in allpts:
    fam_of[(x, y)] = next((n for n, f, _, _ in reversed(FAMILIES) if f(c)), "green")
layers, vdefs = [], []
def shape(sel_xy, col, fid, close=0):
    lm = Image.new("L", (w, h), 0); lp = lm.load()
    for x, y in sel_xy: lp[x, y] = 255
    if close: lm = lm.filter(ImageFilter.MaxFilter(close)).filter(ImageFilter.MinFilter(close))  # fecha falhas da família
    lm = lm.resize((w * 2, h * 2), Image.LANCZOS).filter(ImageFilter.GaussianBlur(1.4)).point(lambda v: 255 if v >= 128 else 0)
    if not lm.getbbox(): return ""
    bi = Image.new("RGB", lm.size, "white"); bi.paste((0, 0, 0), mask=lm)
    lps = [re.sub(r'fill="#[0-9A-Fa-f]{6}"', f'fill="{col}"', q) for q in
           re.findall(r"<path [^>]*/>", trace(bi, here / "_lv.svg", colormode="binary", mode="spline",
                                              filter_speckle=6, corner_threshold=100, length_threshold=5.0))]
    return f'<g filter="url(#{fid})">{"".join(lps)}</g>'
for name, _, levels, (sd0, sd1) in FAMILIES:
    pts = [(x, y, c) for x, y, c in allpts if fam_of[(x, y)] == name] if name != "green" else allpts
    own = [(x, y, c) for x, y, c in allpts if fam_of[(x, y)] == name]
    if len(own) < 30: continue
    ls = sorted(lumc(c) for _, _, c in own)
    cuts = sorted({round(ls[int(len(ls) * i / levels)], 1) for i in range(levels)}); cuts.append(ls[-1] + 1)
    for i in range(len(cuts) - 1):
        band = [c for _, _, c in own if cuts[i] <= lumc(c) < cuts[i + 1]]
        if not band: continue
        col = hexc(mean(band)); fid = f"v{name}{i}"
        sd = sd0 + (sd1 - sd0) * i / max(1, len(cuts) - 2)
        if i == 0:
            if name == "green":
                layers.append(f'<rect x="0" y="0" width="{w * 2}" height="{h * 2}" fill="{col}"/>')
            else:  # base sólida da família: a área toda (sem falhas) com a cor da faixa mais escura dela
                vdefs.append(blur(fid, round(sd * 2, 2)))
                layers.append(shape([(x, y) for x, y, _ in own], col, fid, close=11))
            continue
        # verde: o nível cobre todo pixel com luminância >= corte (inclui os de outras famílias, que ficam por baixo)
        sel = [(x, y) for x, y, c in pts if lumc(c) >= cuts[i]]
        vdefs.append(blur(fid, round(sd * 2, 2)))
        layers.append(shape(sel, col, fid, close=0 if name == "green" else 9))
# reflexo da viseira desenhado como formas geométricas: as bordas de cada faixa (amarelo, risco claro, risco
# branco) são medidas linha a linha no original, suavizadas e viram curvas; a cor vem de um degradê vertical
# com a cor média da faixa em cada altura.
def kind(c):
    if min(c) > 200: return "W"
    if min(c) > 110: return "p"
    if c[0] > c[1] * 0.72 and c[2] < c[1] * 0.55 and c[1] > 70: return "y"
    return "."
opx = rgb.load()
def segments(rows, gap=6):
    """Separa as linhas medidas em trechos contínuos (um buraco de mais de `gap` linhas abre outro trecho)."""
    out, cur, last = [], {}, None
    for y in sorted(rows):
        if last is not None and y - last > gap: out.append(cur); cur = {}
        cur[y] = rows[y]; last = y
    if cur: out.append(cur)
    return out
def runs(y, keep, minrun=3):
    out, x0 = [], None
    for x in range(w + 1):
        on = x < w and mpx[x, y] == 255 and kind(opx[x, y]) in keep
        if on and x0 is None: x0 = x
        if not on and x0 is not None:
            if x - x0 >= minrun: out.append((x0, x))
            x0 = None
    return out
right_half = w * 0.68
def edges(keep, pick, minrun):
    rows = {}
    for y in range(h):
        rs = [r for r in runs(y, keep, minrun) if r[1] > right_half]
        if rs: rows[y] = pick(rs)
    return rows
def smooth(rows, k=REFLEX_SMOOTH):
    ys = sorted(rows); out = {}
    for y in ys:
        win = [rows[v] for v in ys if abs(v - y) <= k]
        out[y] = tuple(sum(c[i] for c in win) / len(win) for i in range(2))
    return out
def catmull(pts):
    d = f"M{pts[0][0]:.1f} {pts[0][1]:.1f}"
    for i in range(len(pts) - 1):
        p0, p1, p2, p3 = pts[max(i - 1, 0)], pts[i], pts[i + 1], pts[min(i + 2, len(pts) - 1)]
        c1 = (p1[0] + (p2[0] - p0[0]) / 6, p1[1] + (p2[1] - p0[1]) / 6)
        c2 = (p2[0] - (p3[0] - p1[0]) / 6, p2[1] - (p3[1] - p1[1]) / 6)
        d += f" C{c1[0]:.1f} {c1[1]:.1f} {c2[0]:.1f} {c2[1]:.1f} {p2[0]:.1f} {p2[1]:.1f}"
    return d
def band_path(rows, step=4):
    ys = sorted(rows)[::step] + [sorted(rows)[-1]]
    left = [(rows[y][0], y) for y in ys]; right = [(rows[y][1], y) for y in reversed(ys)]
    return catmull(left) + " L" + catmull(right)[1:] + " Z"
def vgrad(gid, rows, keep):
    ys = sorted(rows); y0, y1 = ys[0], ys[-1]; stops = []
    for t in range(REFLEX_STOPS):
        a = y0 + (y1 - y0) * t / REFLEX_STOPS; b = y0 + (y1 - y0) * (t + 1) / REFLEX_STOPS
        cs = [opx[x, y] for y in ys if a <= y < b or (t == REFLEX_STOPS - 1 and y == y1)
              for x in range(int(rows[y][0]), min(w, int(rows[y][1]))) if mpx[x, y] == 255 and kind(opx[x, y]) in keep]
        if cs: stops.append(((t + 0.5) / REFLEX_STOPS, mean(cs)))
    return (f'<linearGradient id="{gid}" gradientUnits="userSpaceOnUse" x1="0" y1="{y0}" x2="0" y2="{y1}">'
            + "".join(f'<stop offset="{o:.3f}" stop-color="{hexc(c)}"/>' for o, c in stops) + "</linearGradient>")
reflex, rdefs = [], []
far = w + 8  # a faixa amarela vai até além da borda: o recorte da viseira fecha
for name, keep, colkeep, pick, sd, minrun, which in REFLEX:
    segs = [g for g in segments(edges(keep, pick, minrun)) if len(g) >= 6]
    if which == "main": segs = sorted(segs, key=len)[-1:]
    rdefs.append(blur(f"rb{name}", sd))
    for k, rows in enumerate(segs):
        if name == "yellow": rows = {y: (r[0], far) for y, r in rows.items()}
        rows = smooth(rows)
        rdefs.append(vgrad(f"rg{name}{k}", rows, colkeep))
        reflex.append(f'<path d="{band_path(rows)}" fill="url(#rg{name}{k})" filter="url(#rb{name})"/>')
vdefs += rdefs
visor_group = (f'<g clip-path="url(#visor)"><g transform="scale(0.5)">{"".join(layers)}</g>'
               f'{"".join(reflex)}</g>')
grad = ""

paths = re.findall(r"<path [^>]*/>", raw[raw.index("<path"):])
def info(p):
    c = re.search(r'fill="#([0-9A-Fa-f]{6})"', p).group(1)
    col = tuple(int(c[i:i + 2], 16) for i in (0, 2, 4))
    tx, ty = (float(v) for v in re.search(r"translate\(([-\d.]+),([-\d.]+)\)", p).groups())
    nums = [float(v) for v in re.findall(r"-?\d+\.?\d*", re.search(r' d="([^"]*)"', p).group(1))]
    xs, ys = nums[0::2], nums[1::2]
    return col, (min(xs) + tx, min(ys) + ty, max(xs) + tx, max(ys) + ty)
infos = [info(p) for p in paths]
def lum(c): return 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]
def overlap(b1, b2): return b1[0] < b2[2] and b2[0] < b1[2] and b1[1] < b2[3] and b2[1] < b1[3]
def nearest(i):
    ci, bi = infos[i]; best = 999
    for j, (cj, bj) in enumerate(infos):
        if j != i and overlap(bi, bj):
            best = min(best, sum((x - y) ** 2 for x, y in zip(ci, cj)) ** 0.5)
    return best
body = []
for i, p in enumerate(paths):
    l = lum(infos[i][0])
    f = ("line" if l < DARK else None if l > LIGHT
         else "edge" if l > HIGHLIGHT or nearest(i) >= BAND_DIST else "band")
    body.append(p if f is None else p.replace("<path ", f'<path filter="url(#{f})" ', 1))

svg = (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}">\n'
       f'<!-- Cabeça do mascote: gerado por design/brand/trace-mascot.py a partir de mascot-head.png. Não editar à mão. -->\n'
       f'<defs>{blur("band", BAND_BLUR)}{blur("edge", EDGE_BLUR)}{blur("line", LINE_BLUR)}'
       f'{grad}{"".join(vdefs)}'
       f'<clipPath id="silhouette">{clip}</clipPath><clipPath id="visor" transform="scale(0.5)">{vclip}</clipPath></defs>\n'
       f'<g clip-path="url(#silhouette)">{"".join(body)}\n{visor_group}\n'
       f'<g transform="scale(0.5)" filter="url(#line)">{ink_paths}</g></g>\n</svg>\n')
out.write_text(svg)
tmp.unlink(missing_ok=True)
