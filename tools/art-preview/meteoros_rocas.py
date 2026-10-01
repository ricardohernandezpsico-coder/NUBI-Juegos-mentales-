"""Maqueta de las ROCAS de los meteoros (1-oct, "se ven muy básicas"): la roca de HOY (MeteorSprites) y tres propuestas de
MeteorSpritesV2 (A arcilla con volumen, B = A con borde al rojo vivo y estela de calor, C cristal), cada una en 2 tamaños,
con placa y palabra, sobre el cielo del juego; más una fila con las 4 formas de la opción B. Usa el arte REAL: los .raw
salen de ArtPreview (los mismos generadores de C# que usa el juego).

  cd tools/art-preview && dotnet run -- <carpeta_raw>
  python tools/art-preview/meteoros_rocas.py <carpeta_raw>  ->  docs/previews/meteoros-rocas.png
"""
import math
import os
import random
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

from juegos import load, FB, ROOT

OUT = os.path.join(ROOT, "docs", "previews", "meteoros-rocas.png")
INK = (26, 18, 64)
PLACA = (255, 248, 236)
SUN = (255, 201, 60)
SOFT = (214, 208, 245)
WHITE = (255, 255, 255)
TINTS = [(184, 164, 255), (76, 201, 240), (255, 107, 74)]
S = 0.78  # píxeles por unidad del canvas del juego (3 unidades = 1 dp)
GRAD = [(2, 3, 15), (5, 8, 35), (10, 15, 51)]
ANG = 16


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


def soft_trail(size, color, length):
    """La estela de HOY: una mancha de luz alargada y suave del color de la roca."""
    w, h = size
    layer = Image.new("RGBA", (int(w * 1.4), int(length * 2)), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    cx = layer.width // 2
    for i in range(18):
        t = i / 18
        y = layer.height // 2 - int(length * t)
        r = w * 0.42 * (1 - t * 0.85)
        d.ellipse([cx - r, y - r, cx + r, y + r], fill=color + (int(110 * (1 - t) ** 1.4),))
    return layer.filter(ImageFilter.GaussianBlur(6)), (cx, layer.height // 2)


def paste_rot(dst, layer, anchor, at, angle):
    """Pega [layer] girada [angle] grados (antihorario) apoyando su punto [anchor] en [at]."""
    ax, ay = anchor
    big = Image.new("RGBA", (layer.width * 2, layer.height * 2), (0, 0, 0, 0))
    big.alpha_composite(layer, (layer.width - ax, layer.height - ay))
    rot = big.rotate(angle, resample=Image.BICUBIC)
    dst.alpha_composite(rot, (int(at[0] - layer.width), int(at[1] - layer.height)))


def font(px):
    return ImageFont.truetype(FB, int(px))


def meteor(img, raw, kind, shape, tint, word, cx, cy, heat=False):
    f = font(78 * S)
    d0 = ImageDraw.Draw(img)
    tw = d0.textlength(word, font=f)
    pw, ph = tw + 64 * S, 117 * S
    if kind == "cur":
        rw, rh = pw + 100 * S, ph + 130 * S        # lo que hace el juego hoy: la roca casi no asoma detrás de la placa
    else:
        rw, rh = pw * 1.45 + 60 * S, ph * 3.2        # propuesta: la roca crece (la placa ocupa ~1/3 de su altura)
    k = 1.3 / 1.08 if kind == "B" else 1.0           # B se dibuja con más margen (resplandor de la brasa)
    rock = load(os.path.join(raw, f"rock_{kind}_{shape}_{tint}.raw")).resize((int(rw * k), int(rh * k)), Image.LANCZOS)
    if heat:
        tr = load(os.path.join(raw, "heat_trail.raw"))
        th = int(430 * S)
        tr = tr.resize((int(rw * 0.55), th), Image.LANCZOS)
        paste_rot(img, tr, (tr.width // 2, tr.height - int(8 * S)), (cx, cy), ANG)
    else:
        trail, anc = soft_trail((int(pw), int(ph)), TINTS[tint], 300 * S)
        paste_rot(img, trail, anc, (cx, cy), ANG)
    img.alpha_composite(rock, (int(cx - rock.width / 2), int(cy - rock.height / 2)))
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = cx - pw / 2, cy - ph / 2, cx + pw / 2, cy + ph / 2
    d.rounded_rectangle([x0, y0 + 7, x1, y1 + 7], 20, fill=INK)
    d.rounded_rectangle([x0, y0, x1, y1], 20, fill=PLACA, outline=INK, width=5)
    d.text((cx, cy + 2), word, font=f, fill=INK, anchor="mm")
    return rw, rh


def main():
    raw = sys.argv[1]
    cols = [("Hoy", "cur", False), ("A · Arcilla con volumen", "A", False), ("B · A + brasa y estela de calor", "B", True),
            ("C · Cristal", "C", False)]
    colw, margin = 640, 30
    W = colw * 4 + margin * 2
    H = 1440
    img = sky(W, H, 5)
    d = ImageDraw.Draw(img)
    big = font(40)
    small = ImageFont.truetype(FB, 24)
    d.text((margin, 24), "Rocas de Lluvia de meteoros: la de hoy y tres propuestas (arte real de los generadores)", font=big, fill=WHITE)
    rows = [("ventana", 0, 0, 320), ("murciélago", 1, 1, 640)]
    for ci, (title, kind, heat) in enumerate(cols):
        x = margin + ci * colw
        d.text((x + 10, 100), title, font=font(34), fill=SUN)
        for word, shape, tint, y in rows:
            meteor(img, raw, kind, shape + (ci % 2), tint if ci != 3 else (tint + 1) % 3, word, x + colw // 2 + 20, 260 + (y - 320) + 160, heat)
    # fila con las 4 formas de la opción B
    d = ImageDraw.Draw(img)
    d.text((margin + 10, 960), "B · las 4 formas (y los 3 colores)", font=font(34), fill=SUN)
    words = ["casa", "luna", "sendero", "efímero"]
    for i in range(4):
        meteor(img, raw, "B", i, i % 3, words[i], margin + i * colw + colw // 2 + 20, 1160, True)
    d = ImageDraw.Draw(img)
    notes = ["Hoy: la roca casi no asoma detrás de la placa (se ve un borde fino).  A: volumen con luz arriba a la izquierda, cráteres con borde y grano.",
             "B: A + brasa en el borde que va por delante y estela de calor con chispas.  C: facetas lila-celeste con un cristal interior.  A, B y C usan una roca más grande que la de hoy."]
    for k, line in enumerate(notes):
        d.text((margin + 10, 1350 + k * 40), line, font=small, fill=SOFT)
    img.convert("RGB").save(OUT)
    print(OUT, img.size)


if __name__ == "__main__":
    main()
