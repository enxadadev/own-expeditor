"""Recorta a cabeça do mascote (capacete + rosto) do original e remove o fundo claro, sem reamostrar.
uso: python3 design/brand/crop-mascot-head.py <original.jpg> <saida.png>   (só Pillow)

Os números são para mascot-original.jpg (1024x1024): `box` enquadra a cabeça com as asas; o retângulo até
y=512 e a elipse do queixo cortam o macacão logo abaixo do capacete. Saída: PNG quadrado com alfa."""
import sys
from PIL import Image, ImageDraw, ImageFilter
src, out = sys.argv[1], sys.argv[2]
im = Image.open(src).convert("RGB")
box = (60, 5, 630, 532)  # esquerda, topo, direita, base: cabeça com as asas, até o queixo
im = im.crop(box)
w, h = im.size
px = im.load()
# fundo: claro e pouco saturado, conectado às bordas
def isbg(c):
    r,g,b = c; mx, mn = max(c), min(c)
    return mn > 200 and mx - mn < 28
mask = Image.new("L", (w, h), 255)
m = mask.load()
stack = [(x, 0) for x in range(w)] + [(0, y) for y in range(h)] + [(w-1, y) for y in range(h)]
seen = set()
while stack:
    x, y = stack.pop()
    if (x, y) in seen or not (0 <= x < w and 0 <= y < h): continue
    seen.add((x, y))
    if not isbg(px[x, y]): continue
    m[x, y] = 0
    stack += [(x+1,y),(x-1,y),(x,y+1),(x,y-1)]
# corta o macacão abaixo do queixo: tudo abaixo de y=515 fora da elipse do capacete
d = ImageDraw.Draw(mask)
cut = Image.new("L", (w, h), 0); dc = ImageDraw.Draw(cut)
dc.rectangle((0, 0, w, 512), fill=255)
dc.ellipse((150, 330, 560, 527), fill=255)  # queixo/borda inferior do capacete
mask = Image.composite(mask, Image.new("L", (w, h), 0), cut)
mask = mask.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.8))
rgba = im.copy(); rgba.putalpha(mask)
rgba = rgba.crop(rgba.getbbox())
s = max(rgba.size)
sq = Image.new("RGBA", (s, s), (0,0,0,0))
sq.paste(rgba, ((s-rgba.width)//2, (s-rgba.height)//2))
sq.save(out)  # resolução nativa do original, sem reamostrar
