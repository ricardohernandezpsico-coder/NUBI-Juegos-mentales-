"""Lámina de los tutoriales guiados del inicio (3-oct, tarea 21a): Freno de Emergencia, Aterrizaje Lunar y Lluvia de meteoros.

Fila de arriba: la tarjeta de entrada de Nubi maestra de cada juego (título, la meta en una frase, «Probar una ronda» y «Saltar tutorial»).
Fila de abajo: un instante de la ronda guiada de cada juego (rótulo «Práctica: no cuenta», aro sol punteado, mensaje de Nubi y «Saltar tutorial»).
Las pantallas de fondo salen de freno.py, aterrizaje.py y meteoros.py (maquetas con los sprites REALES de Unity, sin Unity) y la Nubi maestra es el .raw real.
Es una maqueta de la disposición, NO una captura de la app (el teléfono estaba bloqueado): las medidas siguen las de `GuidedTutorial.cs` (lienzo 1080 × 1920, 0,5 px por unidad).

Uso: DOTNET_ROLL_FORWARD=LatestMajor dotnet run --project tools/art-preview -- <raw> && python tools/art-preview/tutoriales.py <raw>  ->  docs/previews/tutoriales-inicio.png
"""
import argparse
import os

from PIL import Image, ImageDraw, ImageFont

import aterrizaje as A
import freno as F
import juegos as J
import meteoros as M
from juegos import FB, INK, W, H, load, put, glow, hexc, tinted

FR = J.ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
SUN, GRAPE = hexc(0xFFC93C), hexc(0xB8A4FF)
SURFACE = (0x1B, 0x24, 0x66)
SOFT = (217, 212, 245)
K = 0.5  # px por unidad del lienzo en las pantallas de 540 x 960


def to_h(im, h=H):
    return im.convert('RGBA').resize((round(im.width * h / im.height), h), Image.LANCZOS)


def font(px, bold=True):
    return ImageFont.truetype(FB if bold else FR, int(px))


def clay_box(im, box, radius, fill, border=3, shadow=5):
    d = ImageDraw.Draw(im)
    x0, y0, x1, y1 = box
    d.rounded_rectangle((x0, y0 + shadow, x1, y1 + shadow), radius, fill=INK)
    d.rounded_rectangle(box, radius, fill=fill, outline=INK, width=border)


def wrap(d, text, f, maxw):
    words, cur, lines = text.split(), '', []
    for w in words:
        t = (cur + ' ' + w).strip()
        if d.textlength(t, font=f) > maxw and cur:
            lines.append(cur)
            cur = w
        else:
            cur = t
    lines.append(cur)
    return lines


def card(bg, raw, title, goal):
    """La tarjeta de entrada sobre la pantalla del juego (medidas de GuidedTutorial: tarjeta 930 x 1240 u, Nubi 620 u)."""
    im = to_h(bg)
    w, h = im.size
    k = min(h / 1920.0, w / 1080.0)
    im.alpha_composite(Image.new('RGBA', im.size, (2, 3, 15, 199)))
    cx, cy = w / 2, h / 2
    clay_box(im, (cx - 465 * k, cy - 620 * k, cx + 465 * k, cy + 620 * k), int(40 * k), SURFACE, border=max(2, int(7 * k)), shadow=int(18 * k))
    glow(im, cx, cy - 300 * k, 330 * k, (184, 164, 255), 40)
    put(im, load(os.path.join(raw, 'nubi_maestra.raw')), cx, cy - 300 * k, 620 * k)
    d = ImageDraw.Draw(im)
    d.text((cx, cy + 70 * k), title, font=font(72 * k), fill=SUN, anchor='mm')
    f = font(54 * k)
    lines = wrap(d, goal, f, 800 * k)
    for i, ln in enumerate(lines):
        d.text((cx, cy + 240 * k + (i - (len(lines) - 1) / 2) * 62 * k), ln, font=f, fill=(255, 255, 255), anchor='mm')
    clay_box(im, (cx - 360 * k, cy + 430 * k - 75 * k, cx + 360 * k, cy + 430 * k + 75 * k), int(30 * k), SUN, border=max(2, int(6 * k)), shadow=int(14 * k))
    d = ImageDraw.Draw(im)
    d.text((cx, cy + 430 * k), 'Probar una ronda', font=font(62 * k), fill=INK, anchor='mm')
    d.text((cx, cy + 560 * k), 'Saltar tutorial', font=font(54 * k), fill=SOFT, anchor='mm')
    return im


def practice_ui(im, top_u, skip_at_top=False, caption=None, caption_from_bottom_u=330, badge_at_bottom=False):
    """El rótulo «Práctica: no cuenta», «Saltar tutorial» y el mensaje de Nubi (posiciones de GuidedTutorial.PlaceControls)."""
    w, h = im.size
    k = min(h / 1920.0, w / 1080.0)
    d = ImageDraw.Draw(im)
    if badge_at_bottom:
        by = h - (36 + 132 + 14 + 48) * k
    else:
        by = (top_u + 48) * k
    clay_box(im, (w / 2 - 320 * k, by - 48 * k, w / 2 + 320 * k, by + 48 * k), int(24 * k), SURFACE, border=max(2, int(4 * k)), shadow=int(8 * k))
    d = ImageDraw.Draw(im)
    d.text((w / 2, by), 'Práctica: no cuenta', font=font(48 * k), fill=GRAPE, anchor='mm')
    sy = (top_u + 96 + 14 + 66) * k if skip_at_top else h - (12 * 3 + 66) * k
    clay_box(im, (w / 2 - 320 * k, sy - 66 * k, w / 2 + 320 * k, sy + 66 * k), int(24 * k), SURFACE, border=max(2, int(4 * k)), shadow=int(8 * k))
    d = ImageDraw.Draw(im)
    d.text((w / 2, sy), 'Saltar tutorial', font=font(54 * k), fill=SOFT, anchor='mm')
    if caption:
        f = font(60 * k)
        lines = wrap(d, caption, f, 960 * k)
        base = h - caption_from_bottom_u * k
        for i, ln in enumerate(lines):
            y = base + (i - (len(lines) - 1) / 2) * 70 * k
            d.text((w / 2, y), ln, font=f, fill=(255, 255, 255), anchor='mm', stroke_width=max(1, int(3 * k)), stroke_fill=INK)


def hint_ring(im, raw, x, y, size):
    put(im, tinted(load(os.path.join(raw, 'rastro_dashed.raw')), SUN), x, y, size)


def freno_round(raw):
    im = F.frame_go(raw).convert('RGBA')
    hint_ring(im, raw, F.LANES[1], F.BUTTON_Y, 150)
    practice_ui(im, top_u=190 + 150, skip_at_top=True, caption='Toca el cohete que se enciende', caption_from_bottom_u=700)
    return im


def aterrizaje_round(raw):
    im = A.base(raw, 61, 1, 0, 0, '', '4', '0', '10', mid=True).convert('RGBA')
    f = 0.4
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(lay).rounded_rectangle((A.rx(f - 0.12), A.RULER_Y - 38, A.rx(f + 0.12), A.RULER_Y + 22), 10, fill=SUN + (110,), outline=SUN + (255,), width=3)
    im.alpha_composite(lay)
    A.lander(im, raw, A.rx(0.5), 470, flame=0.5, beam=True)
    practice_ui(im, top_u=190 + 10, caption='La regla va de 0 (izquierda) a 10 (derecha). Aterriza en el 4', caption_from_bottom_u=1396)
    return im


def meteoros_round():
    img = M.sky(11)
    M.planet(img)
    M.meteor(img, 232, 430, 'ventana', M.SKY, 21, ang=16)
    im = to_h(img)
    k = im.height / 1920.0
    cx, cy = 232 / M.W * im.width, 430 / M.H * im.height
    d = ImageDraw.Draw(im)
    rx_, ry_ = 92 / M.W * im.width, 66 / M.H * im.height
    d.ellipse((cx - rx_, cy - ry_, cx + rx_, cy + ry_), outline=SUN + (255,), width=3)
    practice_ui(im, top_u=190 + 10, caption='Esta palabra existe: tócala', caption_from_bottom_u=430, badge_at_bottom=True)
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=J.ROOT + '/docs/previews')
    a = ap.parse_args()
    raw = a.raw
    fr, at = F.frame_go(raw), A.base(raw, 61, 1, 0, 0, '', '4', '0', '10', mid=True)
    mi = M.sky(11)
    M.planet(mi)
    top = [
        card(fr, raw, 'Freno de Emergencia', 'Lanza el cohete que se enciende. Si aparece ¡ALTO!, no toques.'),
        card(at, raw, 'Aterrizaje Lunar', 'Aterriza en el número que te piden. La regla solo marca sus dos extremos.'),
        card(mi, raw, 'Lluvia de meteoros', 'Toca las palabras que existen. Las inventadas, déjalas caer.'),
    ]
    bottom = [freno_round(raw), aterrizaje_round(raw), meteoros_round()]
    gap, head = 28, 70
    row_w = max(sum(i.width for i in top), sum(i.width for i in bottom)) + 4 * gap
    sheet = Image.new('RGBA', (row_w, 2 * H + 3 * gap + 2 * head), (0x02, 0x03, 0x10, 255))
    d = ImageDraw.Draw(sheet)
    for row, (ims, label) in enumerate(((top, 'Tarjeta de entrada (Nubi maestra)'), (bottom, 'Ronda guiada (no cuenta, se puede saltar)'))):
        y0 = gap + row * (H + head + gap)
        d.text((gap, y0 + head / 2), label, font=font(30), fill=(255, 255, 255), anchor='lm')
        x = gap
        for im in ims:
            sheet.alpha_composite(im, (x, y0 + head))
            x += im.width + gap
    os.makedirs(a.out, exist_ok=True)
    sheet.convert('RGB').save(os.path.join(a.out, 'tutoriales-inicio.png'))
    print('OK')


if __name__ == '__main__':
    main()
