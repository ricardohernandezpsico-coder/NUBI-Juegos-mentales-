"""Láminas de las ROCAS de Lluvia de meteoros (1-oct, versión final = opción B "brasa y estela de calor", elegida por Ricardo):
las 4 formas x 3 colores (lila, celeste, coral) x 2 tamaños (palabra corta y palabra larga), con placa, palabra en Atkinson
Hyperlegible Bold y estela de calor, sobre el cielo del juego. Usa el arte REAL: los .raw salen de ArtPreview (los mismos
generadores de C# que usa el juego). Medidas como en el juego: placa = texto + 64 u de ancho y 1,5 x la letra de alto; roca =
placa x 1,45 + 60 u de ancho y 3,2 x el alto de la placa; el sprite se dibuja con zoom 1,3/1,08.

  cd tools/art-preview && dotnet run -- <carpeta_raw>
  python tools/art-preview/meteoros_rocas.py <carpeta_raw>  ->  docs/previews/meteoros-rocas.png
"""
import os
import random
import sys

from PIL import Image, ImageDraw, ImageFont

from juegos import load, ROOT

OUT = os.path.join(ROOT, "docs", "previews", "meteoros-rocas.png")
ATKINSON = os.path.join(ROOT, "tools", "previews", "fuentes-letra", "AtkinsonHyperlegible-Bold.ttf")
from juegos import FB as FREDOKA
INK = (26, 18, 64)
PLACA = (255, 248, 236)
SUN = (255, 201, 60)
SOFT = (214, 208, 245)
WHITE = (255, 255, 255)
S = 0.78  # píxeles por unidad del canvas del juego (3 unidades = 1 dp)
GRAD = [(2, 3, 15), (5, 8, 35), (10, 15, 51)]
ANG = 16
ZOOM_K = 1.3 / 1.08


def sky(w, h, seed):
    img = Image.new("RGB", (1, h))
    px = img.load()
    for y in range(h):
        t = y / (h - 1) * 2
        i = min(int(t), 1)
        f = t - i
        px[0, y] = tuple(int(GRAD[i][k] + (GRAD[i + 1][k] - GRAD[i][k]) * f) for k in range(3))
    im = img.resize((w, h)).convert("RGBA")
    d = ImageDraw.Draw(im, "RGBA")
    rnd = random.Random(seed)
    for _ in range(int(w * h / 9000)):
        x, y, r = rnd.random() * w, rnd.random() * h, 0.6 + rnd.random() * 1.4
        d.ellipse([x - r, y - r, x + r, y + r], fill=(255, 255, 255, int(70 + rnd.random() * 130)))
    return im


def paste_rot(dst, layer, anchor, at, angle):
    """Pega [layer] girada [angle] grados (antihorario) apoyando su punto [anchor] en [at]."""
    ax, ay = anchor
    big = Image.new("RGBA", (layer.width * 2, layer.height * 2), (0, 0, 0, 0))
    big.alpha_composite(layer, (layer.width - ax, layer.height - ay))
    rot = big.rotate(angle, resample=Image.BICUBIC)
    dst.alpha_composite(rot, (int(at[0] - layer.width), int(at[1] - layer.height)))


def font(path, px):
    return ImageFont.truetype(path, int(px))


def meteor(img, raw, shape, tint, word, cx, cy):
    f = font(ATKINSON, 78 * S)
    d0 = ImageDraw.Draw(img)
    tw = d0.textlength(word, font=f)
    pw, ph = tw + 64 * S, 117 * S
    rw, rh = pw * 1.45 + 60 * S, ph * 3.2
    rock = load(os.path.join(raw, f"rock_{shape}_{tint}.raw")).resize((int(rw * ZOOM_K), int(rh * ZOOM_K)), Image.LANCZOS)
    tr = load(os.path.join(raw, "heat_trail.raw"))
    tr = tr.resize((int(rw * 0.55), int(430 * S)), Image.LANCZOS)
    paste_rot(img, tr, (tr.width // 2, tr.height - int(8 * S)), (cx, cy), ANG)
    img.alpha_composite(rock, (int(cx - rock.width / 2), int(cy - rock.height / 2)))
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = cx - pw / 2, cy - ph / 2, cx + pw / 2, cy + ph / 2
    d.rounded_rectangle([x0, y0 + 7, x1, y1 + 7], 20, fill=INK)
    d.rounded_rectangle([x0, y0, x1, y1], 20, fill=PLACA, outline=INK, width=5)
    d.text((cx, cy + 2), word, font=f, fill=INK, anchor="mm")


def main():
    raw = sys.argv[1]
    colw, margin, rowh = 560, 30, 380
    W = colw * 4 + margin * 2
    H = 130 + 2 * (3 * rowh + 90) + 120
    img = sky(W, H, 5)
    d = ImageDraw.Draw(img)
    d.text((margin, 24), "Rocas de Lluvia de meteoros: brasa y estela de calor (arte real de los generadores)", font=font(FREDOKA, 40), fill=WHITE)
    names = ["lila", "celeste", "coral"]
    blocks = [("Palabra corta (26 dp)", ["casa", "luna", "ala", "sol"]), ("Palabra larga (12 letras)", ["murciélago", "inolvidable", "maravilloso", "sorprendente"])]
    y0 = 110
    for title, words in blocks:
        d = ImageDraw.Draw(img)
        d.text((margin + 10, y0), title, font=font(FREDOKA, 34), fill=SUN)
        for tint in range(3):
            for shape in range(4):
                cx = margin + shape * colw + colw // 2 + 20
                cy = y0 + 70 + tint * rowh + rowh // 2 - 20
                meteor(img, raw, shape, tint, words[(shape + tint) % 4], cx, cy)
        y0 += 3 * rowh + 90
    d = ImageDraw.Draw(img)
    notes = ["4 formas (columnas) x 3 colores (filas): lila, celeste y coral; el dorado es la misma roca en color sol. La brasa mira hacia donde cae.",
             "La palabra va en Atkinson Hyperlegible Bold (26 dp; 30 en mayores): la 'a' y la 'o' no se parecen. La roca mide ~3 placas de alto."]
    for k, line in enumerate(notes):
        d.text((margin + 10, H - 100 + k * 40), line, font=font(FREDOKA, 24), fill=SOFT)
    img.convert("RGB").save(OUT)
    print(OUT, img.size)


if __name__ == "__main__":
    main()
