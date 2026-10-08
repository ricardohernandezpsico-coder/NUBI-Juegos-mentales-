"""Lámina de «La estación de correo» (8-oct-2026; id correo): las 12 cartas (4 planetas × normal, sello dorado y lazo), los 4 planetas-buzón, la caja fuerte (dial con marcas y manija), el faro apagado y encendido, la nave del correo
con su saco dorado, y un cuadro del momento del faro (la luz DETRÁS de la estación y la nave DELANTE), con los sprites REALES horneados (tools/art-preview) en las posiciones del boceto aprobado
(docs/previews/correo-estacion-boceto.html). No es una captura del juego: el movimiento no se ve en una imagen quieta.

Uso:  python tools/art-preview/correo.py <raw> [--out docs/previews]
      (<raw> = la carpeta que vuelca ArtPreview: `dotnet run --project tools/art-preview -- <raw>`; con solo el runtime 10 de .NET: DOTNET_ROLL_FORWARD=LatestMajor)
Genera docs/previews/correo-estacion.png. (correo-escudo.png y correo-estelar.png son del vuelo y quedan como historia.)
"""
import argparse
import math
import os
import struct

from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
FB = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
FS = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
S = 2            # píxeles por dp
GOLD = (255, 201, 74)
CYAN = (127, 216, 255)
LAV = (171, 165, 210)
TEXT = (237, 234, 251)
INK = (26, 18, 64)
PLANETS = ['Coralia', 'Celesta', 'Lima', 'Uva']
SHAPES = ['círculo', 'triángulo', 'cuadrado', 'gota']


def load(raw, name):
    b = open(os.path.join(raw, name + '.raw'), 'rb').read()
    n = struct.unpack('<i', b[:4])[0]
    w = -n
    h = struct.unpack('<i', b[4:8])[0]
    return Image.frombytes('RGBA', (w, h), b[8:8 + w * h * 4]).transpose(Image.FLIP_TOP_BOTTOM)


def font(size, bold=True):
    return ImageFont.truetype(FB if bold else FS, int(size * S))


def put(img, sprite, cx, cy, wdp, hdp=None, rot=0.0, alpha=1.0):
    """Pega un sprite centrado en (cx, cy) dp, con ancho/alto en dp (si no, la proporción del sprite)."""
    hdp = hdp if hdp is not None else wdp * sprite.height / sprite.width
    sp = sprite.resize((max(1, int(round(wdp * S))), max(1, int(round(hdp * S)))), Image.LANCZOS)
    if rot:
        sp = sp.rotate(-math.degrees(rot), resample=Image.BICUBIC, expand=True)
    if alpha < 1.0:
        a = sp.getchannel('A').point(lambda v: int(v * alpha))
        sp.putalpha(a)
    img.alpha_composite(sp, (int(round(cx * S - sp.width / 2)), int(round(cy * S - sp.height / 2))))


def night(w, h):
    img = Image.new('RGBA', (int(w * S), int(h * S)), (7, 10, 38, 255))
    d = ImageDraw.Draw(img)
    for y in range(int(h * S)):
        t = y / (h * S)
        d.line([(0, y), (w * S, y)], fill=(int(3 + 8 * t), int(4 + 12 * t), int(26 + 22 * t), 255))
    import random
    r = random.Random(11)
    for _ in range(int(w * h / 700)):
        x, y = r.random() * w * S, r.random() * h * S
        rad = 0.5 + r.random() * 1.2
        a = int(60 + r.random() * 120)
        d.ellipse([x - rad, y - rad, x + rad, y + rad], fill=(255, 255, 255, a))
    return img


def glow(img, cx, cy, radius, rgb, alpha):
    """Un resplandor radial (el baño de luz tibia, el destello de la lámpara)."""
    size = int(radius * 2 * S)
    g = Image.radial_gradient('L').resize((size, size), Image.BICUBIC)
    a = g.point(lambda v: int(max(0, 255 - v * 1.0) * alpha * (1 - v / 255.0) ** 0.5))
    layer = Image.new('RGBA', (size, size), rgb + (255,))
    layer.putalpha(a)
    img.alpha_composite(layer, (int(cx * S - size / 2), int(cy * S - size / 2)))


def rr(img, x, y, w, h, r, fill, outline=None, width=0, shadow=False):
    d = ImageDraw.Draw(img, 'RGBA')
    if shadow:
        d.rounded_rectangle([(x + 3) * S, (y + 6) * S, (x + w + 3) * S, (y + h + 6) * S], radius=r * S, fill=(0, 0, 0, 90))
    d.rounded_rectangle([x * S, y * S, (x + w) * S, (y + h) * S], radius=r * S, fill=fill, outline=outline, width=int(width * S))


def text(img, xy, s, size, fill, bold=True, anchor='mm'):
    ImageDraw.Draw(img).text((xy[0] * S, xy[1] * S), s, font=font(size, bold), fill=fill, anchor=anchor)


def sheet_letters(raw, w):
    pad = 14
    img = night(w, 456)
    text(img, (pad, 20), 'Las cartas: un sello por planeta', 17, TEXT, anchor='lm')
    kinds = [('normal', 0), ('sello dorado = encargo', 1), ('lazo = encargo (no focal)', 2)]
    for r, (label, cue) in enumerate(kinds):
        y = 96 + r * 102
        text(img, (pad, y - 46), label, 14, LAV, bold=False, anchor='lm')
        for pl in range(4):
            put(img, load(raw, f'mail_letter_{pl}_{cue}'), 52 + pl * 88, y, 82)
    text(img, (pad, 350), 'Los planetas-buzón: color y figura propios', 14, LAV, bold=False, anchor='lm')
    for pl in range(4):
        put(img, load(raw, f'mail_planet_{pl}'), 52 + pl * 88, 386, 52)
        text(img, (52 + pl * 88, 420), PLANETS[pl], 13, TEXT)
        text(img, (52 + pl * 88, 436), SHAPES[pl], 12, LAV, bold=False)
    return img


def safe_and_beacon(img, raw, x, y, on=False):
    """La caja fuerte (dial con marcas y manija) y el faro (apagado o encendido), como en el boceto."""
    # caja fuerte
    rr(img, x, y, 158, 88, 20, (58, 51, 82, 255), INK, 3, shadow=True)
    rr(img, x + 10, y + 10, 62, 68, 12, (90, 82, 128, 255), INK, 2.5)
    put(img, load(raw, 'mail_dial'), x + 41, y + 36, 32)
    rr(img, x + 27, y + 56, 28, 7, 3.5, GOLD + (255,), INK, 2)
    text(img, (x + 82, y + 33), 'Caja', 17, TEXT, anchor='lm')
    text(img, (x + 82, y + 55), 'fuerte', 17, TEXT, anchor='lm')
    # faro
    bx = x + 170
    rr(img, bx, y, 158, 88, 20, (58, 51, 82, 255), INK, 3, shadow=True)
    fx, fy = bx + 40, y + 48
    if on:
        glow(img, fx, fy - 20, 40, (255, 226, 122), 0.9)
    put(img, load(raw, 'mail_tower'), fx, fy, 44)
    put(img, load(raw, 'mail_lamp_on' if on else 'mail_lamp_off'), fx, fy - 20, 28)
    text(img, (bx + 70, y + 44), 'Faro', 17, TEXT, anchor='lm')


def mailbox(img, raw, i, n, y0):
    gap, w = 10, {2: 150, 3: 102, 4: 78}[n]
    tot = n * w + (n - 1) * gap
    x = 180 - tot / 2 + i * (w + gap)
    rr(img, x, y0, w, 112, 18, (35, 40, 80, 255), INK, 3, shadow=True)
    rr(img, x + 12, y0 + 12, w - 24, 12, 6, (11, 10, 38, 255))
    R = min(28, w * 0.3)
    put(img, load(raw, f'mail_planet_{i}'), x + w / 2, y0 + 60, 64 * R / 28)
    text(img, (x + w / 2, y0 + 98), PLANETS[i], 14, (214, 209, 242), bold=False)


def station_panel(raw, w=360, h=600, show=True):
    """Un cuadro del momento del faro: la luz va DETRÁS de la estación; delante, la lámpara, la nave y el saco."""
    img = night(w, h)
    belt_y = 252
    lx, ly = 186 + 40, 468 + 44 - 16 - 14        # la lámpara del faro (coordenadas del cuadro)
    # --- DETRÁS: el baño de luz tibia y el haz que barre el cielo
    if show:
        glow(img, lx, ly, 330, (255, 226, 150), 0.62)
        beam = Image.new('RGBA', img.size, (0, 0, 0, 0))
        bd = ImageDraw.Draw(beam)
        ang = -math.pi / 2 + 0.55
        for half, a in ((0.34, 38), (0.15, 85)):
            pts = [(lx * S, ly * S), ((lx + math.cos(ang - half) * 700) * S, (ly + math.sin(ang - half) * 700) * S), ((lx + math.cos(ang + half) * 700) * S, (ly + math.sin(ang + half) * 700) * S)]
            bd.polygon(pts, fill=(255, 240, 190, a))
        beam = beam.filter(ImageFilter.GaussianBlur(6))
        img.alpha_composite(beam)
    # --- la estación
    d = ImageDraw.Draw(img, 'RGBA')
    text(img, (16, 82), 'Día 2 de 4', 18, TEXT, anchor='lm')
    text(img, (16, 106), 'Cartas: 14', 14, LAV, bold=False, anchor='lm')
    d.ellipse([(316 - 28) * S, (100 - 28) * S, (316 + 28) * S, (100 + 28) * S], fill=(43, 49, 112, 255), outline=INK, width=3 * S)
    d.ellipse([(316 - 24) * S, (100 - 24) * S, (316 + 24) * S, (100 + 24) * S], fill=(183, 155, 255, 255))
    text(img, (316, 101), '?', 24, INK)
    text(img, (316, 142), 'reloj', 14, LAV, bold=False)
    d.rectangle([-10 * S, (belt_y - 50) * S, (w + 10) * S, (belt_y + 50) * S], fill=(20, 27, 58, 255))
    d.rectangle([0, (belt_y - 48) * S, w * S, (belt_y - 42) * S], fill=(35, 43, 87, 255))
    d.rectangle([0, (belt_y + 40) * S, w * S, (belt_y + 48) * S], fill=(35, 43, 87, 255))
    for k in range(14):
        d.rectangle([(-28 + 9 + k * 28) * S, (belt_y + 42) * S, (-28 + 9 + k * 28 + 14) * S, (belt_y + 46) * S], fill=(127, 216, 255, 30))
    frame = load(raw, 'mail_frame')
    tint = Image.new('RGBA', frame.size, CYAN + (255,))
    tint.putalpha(frame.getchannel('A').point(lambda v: int(v * 0.55)))
    put(img, tint, 150, belt_y, 140, 100)
    put(img, load(raw, 'mail_letter_1_1'), 150, belt_y, 128)
    put(img, load(raw, 'mail_letter_2_0'), 260, belt_y, 128 * 0.7)
    put(img, load(raw, 'mail_letter_0_2'), 330, belt_y, 128 * 0.7)
    for i in range(4):
        mailbox(img, raw, i, 4, belt_y + 76)
    safe_and_beacon(img, raw, 16, 468, on=show)
    text(img, (180, 578), 'Toca el buzón del sello', 14, (143, 138, 192), bold=False)
    # --- DELANTE: el destello de la lámpara, la nave del correo (siguiendo la luz) y el saco que cae
    if show:
        glow(img, lx, ly, 46, (255, 236, 170), 0.95)
        glow(img, lx, ly, 18, (255, 255, 235), 0.95)
        sx, sy = 150, belt_y - 100
        put(img, load(raw, 'mail_flame'), sx - 41, sy, 20, 16)
        glow(img, sx - 48, sy, 16, (255, 180, 110), 0.8)
        put(img, load(raw, 'mail_ship'), sx, sy, 100, rot=-0.05)
        glow(img, 180, belt_y - 52, 30, GOLD, 0.7)
        put(img, load(raw, 'mail_sack'), 180, belt_y - 52, 40)
        rr(img, 56, 440, 248, 30, 15, (8, 10, 34, 235))
        text(img, (180, 455), '¡Faro encendido a tiempo!', 15, GOLD)
    return img


def recap_panel(raw, w=360, h=600):
    img = night(w, h)
    d = ImageDraw.Draw(img, 'RGBA')
    d.rectangle([0, 0, w * S, h * S], fill=(2, 3, 15, 220))
    text(img, (180, 62), 'FIN DEL DÍA 2', 15, GOLD)
    text(img, (180, 94), 'Encargos: 2 de 3', 26, (255, 255, 255))
    rows = [('Sello dorado: caja fuerte', '2 de 2', True), ('Faro a media tarde · de todos los días', 'a tiempo: la nave del correo llegó', True), ('Faro al mediodía (cancelado)', 'lo hiciste igual', False)]
    y = 140
    for t, det, ok in rows:
        rr(img, 18, y - 26, w - 36, 52, 16, (20, 27, 58, 255))
        put(img, load(raw, 'mail_check_ok' if ok else 'mail_check_no'), 46, y, 28)
        text(img, (70, y - 8), t, 15, TEXT, anchor='lm')
        text(img, (70, y + 12), det, 14, LAV, bold=False, anchor='lm')
        y += 62
    text(img, (180, y + 8), 'Cartas bien puestas: 14 de 15 · racha mayor ×11', 15, (214, 209, 242), bold=False)
    text(img, (180, y + 32), 'Reloj: lo miraste 2 veces, 2 cerca de la hora', 15, (214, 209, 242), bold=False)
    text(img, (180, h - 130), 'Mañana: mismo ritmo', 15, LAV)
    rr(img, 90, h - 100, 180, 58, 29, CYAN + (255,), INK, 3)
    text(img, (180, h - 71), 'Siguiente día', 20, INK)
    return img


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=os.path.join(ROOT, 'docs', 'previews'))
    a = ap.parse_args()
    W = 360
    letters = sheet_letters(a.raw, W)
    quiet = station_panel(a.raw, show=False)
    show = station_panel(a.raw, show=True)
    recap = recap_panel(a.raw)
    gap = 24 * S
    total_w = letters.width + gap + quiet.width + gap + show.width + gap + recap.width
    total_h = max(letters.height, quiet.height, show.height, recap.height)
    sheet = Image.new('RGBA', (total_w, total_h), (3, 4, 20, 255))
    x = 0
    for p in (letters, quiet, show, recap):
        sheet.alpha_composite(p, (x, 0))
        x += p.width + gap
    os.makedirs(a.out, exist_ok=True)
    out = os.path.join(a.out, 'correo-estacion.png')
    sheet.convert('RGB').save(out)
    print('OK', out, sheet.size)


if __name__ == '__main__':
    main()
