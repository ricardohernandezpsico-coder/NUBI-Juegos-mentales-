"""Lámina de PIEZAS de «Rescate relámpago: qué cápsulas viste» (v4, 9-oct-2026; id radar): el radar luminoso con su estática y su haz, las seis cápsulas (forma, color y emblema fijos), la roca gris, el botón del tablero con su aro de marcado y su aro punteado, la nave
protagonista con sus diez ventanas, «¡Rescatar!», el planeta, la estación accidentada y los restos que flotan, con los sprites REALES horneados (tools/art-preview) a su tamaño en el juego (1 dp = 2 px; el radar y la nave a escala reducida para que quepan). No es una captura:
las pantallas salen de capturas reales (tools/capturas/hoja.py → docs/previews/capturas/radar.png). El boceto aprobado es docs/previews/rescate-boceto-v4.html. (docs/previews/radar.png y radar-rescate.png son del juego ANTERIOR y quedan como historia.)

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
TEAL = (120, 240, 220)
LAV = (171, 165, 210)
TEXT = (237, 234, 251)
NAMES = ['Hexágono', 'Gota', 'Círculo', 'Cuadrado', 'Triángulo', 'Rombo']
# medidas del juego (RadarSprites / RadarLayout), en dp
V4_GLASS_R = 146.0
BEZEL_R = V4_GLASS_R * 141.0 / 128.0           # el sprite guarda la proporción bisel/vidrio del arte (141/128)
SCOPE_SIDE = 2 * 1.06 / 0.95 * BEZEL_R         # RadarSprites.ScopeSideInBezelRadii × bisel
GLASS_FRACTION = 0.95 * 128 / 141 / 1.06       # RadarSprites.GlassFraction: el vidrio dentro del sprite del radar
BUTTON_SIDE = 2 * 1.12 * 52                    # RadarSprites.ButtonSide (el botón mide 104 × 74)
GO_SIDE = 2 * 1.08 * 86                        # RadarSprites.GoSide («¡Rescatar!» mide 172 × 44)
SHIP_SIDE = 2 * 1.2 * 100                      # RadarSprites.ShipSide (el casco mide 200 × 38)
WINDOW_SIDE = 16.4 * 1.15                      # RadarSprites.WindowSide (r 7 dp con borde: 16,4 dp)
STATION_SIDE = 2 * 1.18 * 90                   # RadarSprites.StationSide


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
    W, H = 380, 660
    img = Image.new('RGBA', (W * S, H * S), (7, 10, 38, 255))
    d = ImageDraw.Draw(img)
    for y in range(H * S):
        t = y / (H * S)
        d.line([(0, y), (W * S, y)], fill=(int(3 + 8 * t), int(4 + 12 * t), int(26 + 22 * t), 255))
    d = ImageDraw.Draw(img)
    d.text((18 * S, 12 * S), 'Rescate relámpago v4 · piezas', font=font(20), fill=TEXT + (255,))

    # fila 1: el radar luminoso con el haz, y con la estática encima (a 0,48 del tamaño real: radio de pantalla de 146 dp)
    scope = load(a.raw, 'radar_scope')
    k = 0.48
    put(img, scope, 95, 120, SCOPE_SIDE * k)
    put(img, scope, 285, 120, SCOPE_SIDE * k)
    put(img, tint(load(a.raw, 'radar_sweep'), TEAL), 95, 120, SCOPE_SIDE * k * GLASS_FRACTION)
    put(img, load(a.raw, 'radar_mask_0'), 285, 120, SCOPE_SIDE * k * GLASS_FRACTION)
    d = ImageDraw.Draw(img)
    label(d, 95, 213, 'radar con el haz')
    label(d, 285, 213, 'radar con la estática')

    # fila 2: las seis cápsulas, cada una con su forma, su color y su nombre (58 dp en la pantalla alta)
    for i in range(6):
        put(img, load(a.raw, f'radar_capsule_{i}'), 38 + i * 61, 262, 58 * 0.95)
    d = ImageDraw.Draw(img)
    for i in range(6):
        label(d, 38 + i * 61, 294, NAMES[i], size=12)

    # fila 3: botones del tablero de 104 × 74 (sin marcar, marcado, y la que no estaba) y la roca
    btn = load(a.raw, 'radar_button')
    for x in (62, 160, 258):
        put(img, tint(btn, (237, 234, 251)), x, 350, BUTTON_SIDE * 0.85)
        put(img, load(a.raw, 'radar_capsule_1'), x, 344, 34)
    put(img, tint(load(a.raw, 'radar_button_ring'), LIME), 160, 350, BUTTON_SIDE * 0.85)
    put(img, tint(load(a.raw, 'radar_button_ring_dashed'), CORAL), 258, 350, BUTTON_SIDE * 0.85)
    d = ImageDraw.Draw(img)
    for x, t in ((62, 'sin marcar'), (160, 'marcada'), (258, 'no la elegiste')):
        label(d, x, 396, t, size=12)
    put(img, load(a.raw, 'radar_rock'), 340, 350, 50)
    d = ImageDraw.Draw(img)
    label(d, 340, 396, 'roca gris', size=12)

    # fila 4: la nave protagonista (casco de 200 × 38 dp) con seis ventanas encendidas, a 0,8
    kn = 0.8
    put(img, load(a.raw, 'radar_ship'), 190, 480, SHIP_SIDE * kn)
    colors = [(166, 227, 107), (127, 216, 255), (255, 138, 107), (255, 201, 74), (183, 155, 255), (166, 227, 107)]
    win = load(a.raw, 'radar_window')
    for j in range(10):
        wx = 190 + (-81 + j * 18) * kn
        put(img, tint(win, colors[j] if j < 6 else (237, 234, 251)), wx, 480 - 2 * kn, WINDOW_SIDE * kn * (1.0 if j < 6 else 0.55))
    d = ImageDraw.Draw(img)
    label(d, 190, 525, 'nave de rescate (diez ventanas, seis encendidas)', size=12)

    # fila 5: «¡Rescatar!», el planeta, la estación accidentada y un resto
    put(img, tint(load(a.raw, 'radar_go'), (255, 201, 74)), 88, 590, GO_SIDE * 0.7)
    put(img, load(a.raw, 'radar_planet'), 190, 585, 60)
    put(img, load(a.raw, 'radar_station'), 280, 585, STATION_SIDE * 0.5)
    put(img, load(a.raw, 'radar_debris'), 352, 585, 20)
    d = ImageDraw.Draw(img)
    label(d, 88, 622, '¡Rescatar!', size=12)
    label(d, 190, 622, 'planeta', size=12)
    label(d, 280, 640, 'estación accidentada', size=12)
    label(d, 352, 622, 'resto', size=12)
    os.makedirs(a.out, exist_ok=True)
    img.convert('RGB').save(os.path.join(a.out, 'rescate-piezas.png'))
    print('OK')


if __name__ == '__main__':
    main()
