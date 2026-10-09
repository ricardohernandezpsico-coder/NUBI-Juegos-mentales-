"""Lámina de PIEZAS de «Satélites: enciende tu planeta» (9-oct-2026; id satelites): el planeta a oscuras, el satélite (todos iguales: la señal es el aro dorado y el sobre), el sobre, la nube de polvo, el aro punteado del que faltó, el medio disco de
las rondas parciales y las marcas de la revelación (✓ y la aspa diagonal), con los sprites REALES horneados (tools/art-preview) a su tamaño en el juego (1 dp = 2 px). No es una captura: las pantallas salen de capturas reales (tools/capturas/hoja.py →
docs/previews/capturas/satelites.png). El boceto aprobado es docs/previews/satelites-orbitas-boceto.html. (docs/previews/satelites.png es del juego ANTERIOR y queda como historia.)

Uso:  python tools/art-preview/satelites.py <raw> [--out docs/previews]
      (<raw> = la carpeta que vuelca ArtPreview: `dotnet run --project tools/art-preview -- <raw>`; con solo el runtime 10 de .NET: DOTNET_ROLL_FORWARD=LatestMajor)
Genera docs/previews/satelites-piezas.png.
"""
import argparse
import math
import os
import struct

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
FB = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
FS = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
S = 2                       # píxeles por dp
GOLD = (255, 201, 74)
CYAN = (127, 216, 255)
CORAL = (255, 138, 107)
LAV = (171, 165, 210)
TEXT = (237, 234, 251)


def load(raw, name):
    b = open(os.path.join(raw, name + '.raw'), 'rb').read()
    n = struct.unpack('<i', b[:4])[0]
    if n > 0:                                  # cuadrado: int32 lado + RGBA
        w = h = n
        data = b[4:4 + w * h * 4]
    else:                                      # rectangular: int32 -ancho, int32 alto + RGBA
        w = -n
        h = struct.unpack('<i', b[4:8])[0]
        data = b[8:8 + w * h * 4]
    return Image.frombytes('RGBA', (w, h), data).transpose(Image.FLIP_TOP_BOTTOM)


def font(size, bold=True):
    return ImageFont.truetype(FB if bold else FS, int(size * S))


def put(img, sprite, cx, cy, wdp, hdp=None):
    hdp = hdp if hdp is not None else wdp * sprite.height / sprite.width
    sp = sprite.resize((max(1, int(round(wdp * S))), max(1, int(round(hdp * S)))), Image.LANCZOS)
    img.alpha_composite(sp, (int(round(cx * S - sp.width / 2)), int(round(cy * S - sp.height / 2))))


def tint(sprite, rgb):
    """El sprite blanco (aro punteado, medio disco) teñido con un color, como hace Image.color en el juego."""
    r, g, b, a = sprite.split()
    solid = Image.new('RGBA', sprite.size, rgb + (255,))
    solid.putalpha(a)
    return solid


def ring(img, cx, cy, rdp, rgb, wdp=3.8):
    layer = Image.new('RGBA', img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.ellipse(((cx - rdp) * S, (cy - rdp) * S, (cx + rdp) * S, (cy + rdp) * S), outline=rgb + (255,), width=int(wdp * S))
    img.alpha_composite(layer)


def label(d, x, y, text, size=14, rgb=LAV):
    f = font(size, bold=False)
    w = d.textlength(text, font=f)
    d.text((x * S - w / 2, y * S), text, font=f, fill=rgb + (255,))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    W, H = 380, 470
    img = Image.new('RGBA', (W * S, H * S), (7, 10, 38, 255))
    d = ImageDraw.Draw(img)
    for y in range(H * S):
        t = y / (H * S)
        d.line([(0, y), (W * S, y)], fill=(int(3 + 8 * t), int(4 + 12 * t), int(26 + 22 * t), 255))
    d = ImageDraw.Draw(img)
    d.text((18 * S, 12 * S), 'Satélites: enciende tu planeta · piezas', font=font(20), fill=TEXT + (255,))

    body = load(a.raw, 'sat_body')
    side = 14 * 4.55                                   # el lado del sprite del satélite en dp (BodySpriteSideInDiscRadii)
    # fila 1: el planeta a oscuras, el satélite, el satélite con la señal (aro dorado y sobre)
    put(img, load(a.raw, 'sat_planet'), 80, 120, 44 * 2 * 1.14 / 0.9)
    put(img, body, 205, 120, side)
    put(img, body, 310, 120, side)
    ring(img, 310, 120, 20, GOLD)
    put(img, load(a.raw, 'sat_envelope'), 310, 92, 36.7)
    d = ImageDraw.Draw(img)
    label(d, 80, 186, 'planeta a oscuras')
    label(d, 205, 186, 'satélite')
    label(d, 310, 186, 'con mensaje')
    # fila 2: marcado (aro celeste), ✓, aspa diagonal, aro punteado del que faltó
    put(img, body, 55, 262, side)
    ring(img, 55, 262, 21, CYAN)
    put(img, body, 148, 262, side)
    ring(img, 148, 262, 20, GOLD)
    put(img, load(a.raw, 'mark_check'), 163, 247, 22)
    put(img, body, 241, 262, side)
    ring(img, 241, 262, 21, CORAL)
    put(img, load(a.raw, 'mark_cross'), 256, 247, 22)
    put(img, body, 334, 262, side)
    ring(img, 334, 262, 20, GOLD)
    put(img, tint(load(a.raw, 'sat_dashed'), CORAL), 334, 262, 25 / 0.47 * 1.0)
    d = ImageDraw.Draw(img)
    for x, t in ((55, 'marcado'), (148, 'entregado'), (241, 'sin mensaje'), (334, 'faltó')):
        label(d, x, 300, t)
    # fila 3: la nube, las rondas (lleno / medio / vacío)
    put(img, load(a.raw, 'sat_cloud'), 110, 390, 200 * 0.8)
    d = ImageDraw.Draw(img)
    label(d, 110, 440, 'nube de polvo (plana y opaca)')
    gx = 246
    ring(img, gx, 380, 8.5, LAV, wdp=2.0)
    ring(img, gx + 26, 380, 8.5, LAV, wdp=2.0)
    ring(img, gx + 52, 380, 8.5, CYAN, wdp=2.0)
    disc = Image.new('RGBA', (96, 96), (0, 0, 0, 0))
    ImageDraw.Draw(disc).ellipse((6, 6, 90, 90), fill=(166, 227, 107, 255))
    put(img, disc, gx, 380, 14)
    put(img, tint(load(a.raw, 'sat_half'), GOLD), gx + 26, 380, 15)
    d = ImageDraw.Draw(img)
    label(d, gx + 26, 404, 'rondas: lleno · medio · actual')
    os.makedirs(a.out, exist_ok=True)
    img.convert('RGB').save(os.path.join(a.out, 'satelites-piezas.png'))
    print('OK')


if __name__ == '__main__':
    main()
