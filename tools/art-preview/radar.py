"""Lámina de PIEZAS de «Rescate relámpago: qué cápsulas viste» (9-oct-2026; id radar): el radar con su estática y su haz, las seis cápsulas (forma, color y emblema fijos), la roca gris, la nave con sus ventanas, el botón del tablero con su aro de marcado y su aro
punteado, y «¡Rescatar!», con los sprites REALES horneados (tools/art-preview) a su tamaño en el juego (1 dp = 2 px). No es una captura: las pantallas salen de capturas reales (tools/capturas/hoja.py → docs/previews/capturas/radar.png). El boceto aprobado es
docs/previews/rescate-boceto.html. (docs/previews/radar.png y radar-rescate.png son del juego ANTERIOR y quedan como historia.)

Uso:  python tools/art-preview/radar.py <raw> [--out docs/previews]
      (<raw> = la carpeta que vuelca ArtPreview: `dotnet run --project tools/art-preview -- <raw>`; con solo el runtime 10 de .NET: DOTNET_ROLL_FORWARD=LatestMajor)
Genera docs/previews/rescate-piezas.png.
"""
import argparse
import os
import struct

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
FB = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
FS = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
S = 2                       # píxeles por dp
LIME = (166, 227, 107)
CYAN = (127, 216, 255)
CORAL = (255, 138, 107)
LAV = (171, 165, 210)
TEXT = (237, 234, 251)
NAMES = ['Hexágono', 'Gota', 'Círculo', 'Cuadrado', 'Triángulo', 'Rombo']


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
    """El sprite blanco (haz, aros, ventana) teñido con un color, como hace Image.color en el juego."""
    r, g, b, a = sprite.split()
    solid = Image.new('RGBA', sprite.size, rgb + (255,))
    solid.putalpha(a)
    return solid


def label(d, x, y, text, size=14, rgb=LAV):
    f = font(size, bold=False)
    w = d.textlength(text, font=f)
    d.text((x * S - w / 2, y * S), text, font=f, fill=rgb + (255,))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    W, H = 380, 520
    img = Image.new('RGBA', (W * S, H * S), (7, 10, 38, 255))
    d = ImageDraw.Draw(img)
    for y in range(H * S):
        t = y / (H * S)
        d.line([(0, y), (W * S, y)], fill=(int(3 + 8 * t), int(4 + 12 * t), int(26 + 22 * t), 255))
    d = ImageDraw.Draw(img)
    d.text((18 * S, 12 * S), 'Rescate relámpago · piezas', font=font(20), fill=TEXT + (255,))

    # fila 1: el radar solo, con el haz, y con la estática encima
    scope = load(a.raw, 'radar_scope')
    side = 128 * 2 * 1.16                              # el sprite del radar con su bisel (ScopeZoom) a radio 128 dp
    k = 0.55
    put(img, scope, 95, 110, side * k)
    put(img, scope, 285, 110, side * k)
    put(img, tint(load(a.raw, 'radar_sweep'), LIME), 95, 110, side * k * 0.78)
    put(img, load(a.raw, 'radar_mask_0'), 285, 110, side * k * 0.78)
    d = ImageDraw.Draw(img)
    label(d, 95, 196, 'radar con el haz')
    label(d, 285, 196, 'radar con la estática')

    # fila 2: las seis cápsulas, cada una con su forma, su color y su nombre
    for i in range(6):
        x = 38 + i * 61
        put(img, load(a.raw, f'radar_capsule_{i}'), x, 250, 55 * 0.95)
    d = ImageDraw.Draw(img)
    for i in range(6):
        label(d, 38 + i * 61, 282, NAMES[i], size=12)

    # fila 3: botones del tablero (sin marcar, marcado, y la que no estaba) y la roca
    btn = load(a.raw, 'radar_button')
    for i, (x, lbl) in enumerate(((62, 'sin marcar'), (160, 'marcada'), (258, 'estaba y no la elegiste'))):
        put(img, tint(btn, (237, 234, 251)), x, 340, 104 * 0.95)
        put(img, load(a.raw, 'radar_capsule_1'), x, 336, 34)
    put(img, tint(load(a.raw, 'radar_button_ring'), LIME), 160, 340, 104 * 0.95)
    put(img, tint(load(a.raw, 'radar_button_ring_dashed'), CORAL), 258, 340, 104 * 0.95)
    d = ImageDraw.Draw(img)
    for x, t in ((62, 'sin marcar'), (160, 'marcada'), (258, 'no la elegiste')):
        label(d, x, 375, t, size=12)
    put(img, load(a.raw, 'radar_rock'), 340, 340, 50)
    d = ImageDraw.Draw(img)
    label(d, 340, 375, 'roca gris', size=12)

    # fila 4: la nave con ventanas encendidas, «¡Rescatar!» y el planeta de fondo
    put(img, load(a.raw, 'radar_ship'), 80, 450, 130)
    for j in range(5):
        put(img, tint(load(a.raw, 'radar_window'), [(166, 227, 107), (127, 216, 255), (255, 138, 107), (255, 201, 74), (183, 155, 255)][j]), 53 + j * 13, 452, 10)
    put(img, tint(load(a.raw, 'radar_go'), (255, 201, 74)), 215, 450, 140)
    put(img, load(a.raw, 'radar_planet'), 340, 450, 60)
    d = ImageDraw.Draw(img)
    label(d, 80, 492, 'nave de rescate', size=12)
    label(d, 215, 482, '¡Rescatar!', size=12)
    label(d, 340, 484, 'planeta', size=12)
    os.makedirs(a.out, exist_ok=True)
    img.convert('RGB').save(os.path.join(a.out, 'rescate-piezas.png'))
    print('OK')


if __name__ == '__main__':
    main()
