"""Exporta o logo do mascote a partir do vetor (mascot-head-traced.svg) para os assets do app.
uso (com um Python que tenha resvg-py e Pillow): python design/brand/make-icons.py [pasta-de-saída]
Saída padrão: src/OwnExpeditor.App/Assets → mascot-96.png (logo do botão Início, 40 px até 2,4x) e
mascot.ico (janela e executável: 16–256 px). Cada tamanho é renderizado direto do vetor."""
import io, sys, pathlib, resvg_py
from PIL import Image
here = pathlib.Path(__file__).parent
out = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else here.parent.parent / "src/OwnExpeditor.App/Assets"
svg = (here / "mascot-head-traced.svg").read_text()
def render(n):
    return Image.open(io.BytesIO(bytes(resvg_py.svg_to_bytes(svg_string=svg, width=n, height=n)))).convert("RGBA")
render(96).save(out / "mascot-96.png", optimize=True)
ico = [16, 20, 24, 32, 40, 48, 64, 128, 256]
imgs = [render(n) for n in ico]
imgs[-1].save(out / "mascot.ico", sizes=[(n, n) for n in ico], append_images=imgs[:-1])
print("ok:", out)
