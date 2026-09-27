"""Maqueta de Acoplamiento (disposición según DockingGameController.Layout a medio canvas; arte exacto: bloques,
huecos, sombras e íconos salen de los .raw que vuelca ArtPreview; la pieza gira y se refleja como en el juego).

Uso: python3 tools/art-preview/acoplamiento.py <raw> [--out docs/previews]  ->  acoplamiento.png
"""
import argparse
import math
import os

from PIL import Image, ImageDraw, ImageFont

from juegos import FB, INK, W, H, ROOT, load, night, put, glow, clay_text, hexc, tinted
from piloto import hud, clay_box

LIME, SUN, CORAL, SKY, GRAPE, SURFACE = hexc(0x9BE564), hexc(0xFFC93C), hexc(0xFF6B4A), hexc(0x4CC9F0), hexc(0xB8A4FF), hexc(0x1B2466)
SHAPE = [(1, 0), (0, 1), (1, 1), (1, 2), (2, 2)]  # pentominó F (quiral), el mismo que vuelca ArtPreview
PORT_Y, ARRIVE_Y, FUEL_Y, BUTTONS_Y = 675, 376, 525, 882


def cell_size():
    ext = max(max(x for x, _ in SHAPE), max(y for _, y in SHAPE)) + 1
    return min(96, 400 / ext) / 2


def piece_px():
    ext = max(max(x for x, _ in SHAPE), max(y for _, y in SHAPE)) + 1
    return (ext + 0.5) * cell_size()


def draw_piece(im, raw, cx, cy, angle, mirror, sx=1.0, alpha=1.0, shadow=True):
    """La pieza entera (un solo sprite) girada y, si toca, reflejada; la sombra cae siempre hacia abajo."""
    size = int(piece_px())
    suffix = '_m' if mirror else ''
    layers = []
    if shadow:
        layers.append((tinted(load(f'{raw}/dock_piece_sil{suffix}.raw'), INK), 5))
    layers.append((load(f'{raw}/dock_piece{suffix}.raw'), 0))
    for img, off in layers:
        img = img.resize((size, size), Image.LANCZOS)
        if sx < 1:
            img = img.resize((max(1, int(size * abs(sx))), size), Image.LANCZOS)
        img = img.rotate(angle, resample=Image.BICUBIC, expand=True)
        if alpha < 1:
            img.putalpha(img.getchannel('A').point(lambda v: int(v * alpha)))
        im.alpha_composite(img, (int(cx - img.width / 2), int(cy + off - img.height / 2)))


def base(raw, seed, level, points, streak, docked, prompt='', prompt_col=(255, 255, 255), fuel=None, glow_col=None):
    im = night(seed)
    d = ImageDraw.Draw(im)
    d.ellipse((-80, 520, 110, 710), fill=SKY, outline=INK, width=4)
    hud(im, d, level, points, streak, 'Acoplamiento')
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((30, 98, W - 30, 106), 4, fill=(255, 255, 255, 30))
    d.rounded_rectangle((30, 98, 30 + (W - 60) * 0.58, 106), 4, fill=LIME)
    # Estación.
    d.text((30, 124), 'Tu estación · %d módulos' % docked if docked else 'Tu estación', font=ImageFont.truetype(FB, 17), fill=(255, 255, 255, 205), anchor='lm')
    d.rounded_rectangle((30, 151, W - 30, 156), 3, fill=(255, 255, 255, 40))
    mini = load(f'{raw}/dock_cell.raw').resize((26, 26), Image.LANCZOS)
    for i in range(docked):
        im.alpha_composite(mini, (int(30 + i * 34), 140))
    d = ImageDraw.Draw(im)
    if prompt:
        clay_text(d, (W / 2, 195), prompt, 27, prompt_col)
    # Puerto.
    if glow_col:
        glow(im, W / 2, PORT_Y, 190, glow_col, 150)
    clay_box(im, (W / 2 - 155, PORT_Y - 140, W / 2 + 155, PORT_Y + 140), 30, SURFACE)
    d = ImageDraw.Draw(im)
    d.text((W / 2, PORT_Y - 124), 'PUERTO', font=ImageFont.truetype(FB, 16), fill=(255, 255, 255, 150), anchor='mm')
    hole = load(f'{raw}/dock_piece_socket.raw').resize((int(piece_px()), int(piece_px())), Image.LANCZOS)
    im.alpha_composite(hole, (int(W / 2 - hole.width / 2), int(PORT_Y + 5 - hole.height / 2)))
    if fuel is not None:
        d.rounded_rectangle((W / 2 - 105, FUEL_Y - 4, W / 2 + 105, FUEL_Y + 4), 4, fill=(255, 255, 255, 30))
        d.rounded_rectangle((W / 2 - 105, FUEL_Y - 4, W / 2 - 105 + 210 * fuel, FUEL_Y + 4), 4, fill=SKY if fuel > 0.35 else SUN)
    # Botones.
    for i, (label, col, icon) in enumerate((('ENCAJA', LIME, 'dock_fit'), ('ESPEJO', GRAPE, 'dock_mirror'))):
        x = W / 2 + (-1 if i == 0 else 1) * 120
        clay_box(im, (x - 110, BUTTONS_Y - 47, x + 110, BUTTONS_Y + 47), 30, col)
        put(im, load(f'{raw}/{icon}.raw'), x - 66, BUTTONS_Y, 60)
        d = ImageDraw.Draw(im)
        d.text((x + 22, BUTTONS_Y), label, font=ImageFont.truetype(FB, 29), fill=INK, anchor='mm')
    return im


def frame_arrive(raw):
    """Llega el módulo girado 135° (es el reflejo): ¿encaja o es su espejo? Combustible bajando."""
    im = base(raw, 71, 5, 1320, 3, 3, '¿Encaja en el puerto o es su espejo?', fuel=0.62)
    draw_piece(im, raw, W / 2, ARRIVE_Y, 135, mirror=True)
    return im


def frame_flip(raw):
    """Respondió ESPEJO y acertó: el módulo queda derecho y se da vuelta como un espejo."""
    im = base(raw, 72, 5, 1320, 4, 3, '¡Bien visto! Era su reflejo', LIME)
    draw_piece(im, raw, W / 2, ARRIVE_Y + 40, 0, mirror=True, sx=0.35)
    return im


def frame_docked(raw):
    """¡Acoplado!: entra en el puerto, que se ilumina; suma un módulo a tu estación."""
    im = base(raw, 73, 5, 1448, 4, 4, '¡Acoplado! · racha 4', LIME, glow_col=LIME)
    draw_piece(im, raw, W / 2, PORT_Y + 5, 0, mirror=False, shadow=False)
    spark = load(f'{raw}/sym_Star_0.raw')
    for dx, dy, s in [(-150, -150, 24), (140, -160, 20), (-170, 20, 18), (165, 40, 22)]:
        put(im, spark, W / 2 + dx, PORT_Y + dy, s)
    d = ImageDraw.Draw(im)
    clay_text(d, (W / 2, PORT_Y - 190), '+128', 29, SUN)
    return im


def frame_wrong(raw):
    """Dijo ENCAJA y era el reflejo: baja, choca con el puerto y se ve que no calza."""
    im = base(raw, 74, 5, 1448, 0, 4, 'No encaja: era su reflejo', CORAL, glow_col=CORAL)
    draw_piece(im, raw, W / 2 + 8, PORT_Y - 150, 0, mirror=True)
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    panels = [frame_arrive(a.raw), frame_flip(a.raw), frame_docked(a.raw), frame_wrong(a.raw)]
    gap = 24
    sheet = Image.new('RGBA', (len(panels) * W + (len(panels) + 1) * gap, H + 2 * gap), (0x02, 0x03, 0x10, 255))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (gap + i * (W + gap), gap))
    os.makedirs(a.out, exist_ok=True)
    sheet.convert('RGB').save(os.path.join(a.out, 'acoplamiento.png'))
    print('OK')


if __name__ == '__main__':
    main()
