"""Lámina «símbolos neutros» (4-oct-2026, regla de Ricardo: nada de estrella con puntas, media luna ni cruz).

Uso:  python3 tools/art-preview/simbolos_neutros.py <raw> [--out docs/previews]
      (<raw> = la carpeta que vuelca ArtPreview: `dotnet run --project tools/art-preview -- <raw>`)
Genera docs/previews/simbolos-neutros.png con:
  1. Los 8 planetas-puerto de Bitácora de Misión y Correo Estelar (corazón, hexágono, rombo, triángulo, gota, ola, cuadrado, aro), grandes y en tamaño chico.
  2. Las cartas de Parejas que cambiaron: galaxia espiral y luna llena con cráteres (en las cartas, con la cara crema).
  3. Las señales de Piloto: hexágono y gota junto a las otras cuatro.
"""
import argparse
import os
import struct

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
FB = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
FS = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
PORTS = ['Corazón', 'Hexágono', 'Rombo', 'Triángulo', 'Gota', 'Ola', 'Cuadrado', 'Aro']


def load(path):
    b = open(path, 'rb').read()
    n = struct.unpack('<i', b[:4])[0]
    return Image.frombytes('RGBA', (n, n), b[4:4 + n * n * 4]).transpose(Image.FLIP_TOP_BOTTOM)


def night(w, h):
    im = Image.new('RGBA', (w, h), (0x05, 0x08, 0x23, 255))
    return im


def put(im, spr, cx, cy, size):
    s = spr.resize((int(size), int(size)), Image.LANCZOS)
    im.alpha_composite(s, (int(cx - size / 2), int(cy - size / 2)))


def text(d, xy, t, size, fill=(255, 255, 255), anchor='mm', bold=True):
    d.text(xy, t, font=ImageFont.truetype(FB if bold else FS, size), fill=fill, anchor=anchor)


def card(im, cx, cy, w, h):
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((cx - w / 2, cy - h / 2 + 8, cx + w / 2, cy + h / 2 + 8), 28, fill=(0x1A, 0x12, 0x40))
    d.rounded_rectangle((cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2), 28, fill=(0xFF, 0xF8, 0xEA), outline=(0x1A, 0x12, 0x40), width=6)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    W, H = 1500, 1280
    im = night(W, H)
    d = ImageDraw.Draw(im)
    text(d, (W / 2, 50), 'Símbolos neutros (4-oct): sin estrella con puntas, media luna ni cruz', 40)

    # 1. puertos
    text(d, (W / 2, 120), 'Planetas-puerto de Bitácora de Misión y Correo Estelar', 30, (0xFF, 0xC9, 0x4A))
    for i, name in enumerate(PORTS):
        cx = 100 + i * 185
        put(im, load(f'{a.raw}/port_{i}.raw'), cx, 250, 170)
        put(im, load(f'{a.raw}/port_{i}.raw'), cx, 400, 56)
        text(d, (cx, 490), name, 24, (0xD9, 0xD4, 0xF5), bold=False)
    # 8 puertos en fila chica, con el borde para ver cómo se distinguen de a pares
    # 2. cartas de Parejas
    text(d, (W / 2, 560), 'Cartas de Parejas que cambiaron (antes: estrella y media luna)', 30, (0xFF, 0xC9, 0x4A))
    cards = [('Galaxia espiral', 'Galaxy', 0), ('Galaxia (otro color)', 'Galaxy', 1), ('Luna llena', 'FullMoon', 2), ('Luna llena (otro color)', 'FullMoon', 1),
             ('Constelación (luceros)', 'Constellation', 0), ('Cristal (brillo redondo)', 'Crystal', 1)]
    for i, (name, kind, var) in enumerate(cards):
        cx = 135 + i * 245
        card(im, cx, 730, 200, 230)
        put(im, load(f'{a.raw}/sym_{kind}_{var}.raw'), cx, 735, 180)
        text(d, (cx, 880), name, 20, (0xD9, 0xD4, 0xF5), bold=False)
    # 3. señales de Piloto
    text(d, (W / 2, 960), 'Señales de Piloto Estelar: hexágono y gota reemplazan a la estrella y la media luna', 30, (0xFF, 0xC9, 0x4A))
    sig = [('Hexágono', 'Hexagon', 0), ('Cometa', 'Comet', 0), ('Planeta', 'Planet', 0), ('Gota', 'Drop', 0), ('Cohete', 'Rocket', 0), ('Platillo', 'Ufo', 0)]
    for i, (name, kind, var) in enumerate(sig):
        cx = 135 + i * 245
        put(im, load(f'{a.raw}/sym_{kind}_{var}.raw'), cx, 1090, 150)
        put(im, load(f'{a.raw}/sym_{kind}_{var}.raw'), cx, 1195, 54)
        text(d, (cx, 1260), name, 20, (0xD9, 0xD4, 0xF5), bold=False)
    os.makedirs(a.out, exist_ok=True)
    out = os.path.join(a.out, 'simbolos-neutros.png')
    im.convert('RGB').save(out)
    print(out)


if __name__ == '__main__':
    main()
