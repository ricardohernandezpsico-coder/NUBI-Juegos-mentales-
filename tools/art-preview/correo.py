"""Maqueta de Correo Estelar (propuesta, antes de programar): el vuelo de Piloto Estelar con ENCARGOS que hay que
recordar en el momento justo (memoria prospectiva). Arte de los .raw de ArtPreview (nave, planetas-puerto de Tráfico,
marcas); el sobre, la radio y el reloj tapado se dibujan acá.

Tres momentos: la hoja de ruta (los encargos), el vuelo con un planeta coral que pasa (¡tócalo!) y la entrega con el
reloj destapado.

Uso: python3 tools/art-preview/correo.py <raw> [--out docs/previews]  ->  correo-estelar.png
"""
import argparse
import os

from PIL import Image, ImageDraw, ImageFont

from juegos import INK, W, H, ROOT, FB, load, night, put, glow, clay_text, hexc
from piloto import clay_box, lane, ship, control, SKY, CORAL, SUN, LIME, SURFACE

CREAM = (255, 248, 236)
GRAPE = hexc(0xB8A4FF)


def font(size):
    return ImageFont.truetype(FB, size)


def hud(im, level, info):
    d = ImageDraw.Draw(im)
    d.text((30, 38), 'Correo Estelar', font=font(34), fill=(255, 255, 255), anchor='lm', stroke_width=2, stroke_fill=INK)
    clay_box(im, (30, 62, 132, 90), 14, SURFACE)
    d = ImageDraw.Draw(im)
    d.text((81, 76), f'Nivel {level}', font=font(20), fill=SKY, anchor='mm')
    w = 40 + 11 * len(info)
    clay_box(im, (142, 62, 142 + w, 90), 14, SURFACE)
    d = ImageDraw.Draw(im)
    d.text((142 + w / 2, 76), info, font=font(20), fill=SUN, anchor='mm')


def envelope(im, cx, cy, s=1.0, col=CREAM):
    """Sobre de arcilla (lo que se recoge al volar)."""
    d = ImageDraw.Draw(im)
    w, h = 34 * s, 24 * s
    d.rounded_rectangle((cx - w / 2, cy - h / 2 + 3, cx + w / 2, cy + h / 2 + 3), 5, fill=INK)
    d.rounded_rectangle((cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2), 5, fill=col, outline=INK, width=3)
    d.line((cx - w / 2 + 3, cy - h / 2 + 3, cx, cy + 2 * s, cx + w / 2 - 3, cy - h / 2 + 3), fill=INK, width=3)


def package(im, cx, cy, s=1.0):
    d = ImageDraw.Draw(im)
    r = 16 * s
    d.rounded_rectangle((cx - r, cy - r + 3, cx + r, cy + r + 3), 5, fill=INK)
    d.rounded_rectangle((cx - r, cy - r, cx + r, cy + r), 5, fill=hexc(0xD9A066), outline=INK, width=3)
    d.line((cx, cy - r, cx, cy + r), fill=SUN, width=4)
    d.line((cx - r, cy, cx + r, cy), fill=SUN, width=4)


def radio(im, cx, cy, lit=False):
    """Botón de la radio (encargo por hora): disco de arcilla con antena y ondas."""
    if lit:
        glow(im, cx, cy, 60, SUN, 90)
    d = ImageDraw.Draw(im)
    r = 38
    d.ellipse((cx - r, cy - r + 5, cx + r, cy + r + 5), fill=INK)
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=GRAPE, outline=INK, width=4)
    d.rounded_rectangle((cx - 15, cy - 4, cx + 15, cy + 18), 5, fill=CREAM, outline=INK, width=3)
    d.line((cx + 6, cy - 4, cx + 14, cy - 22), fill=INK, width=4)
    d.ellipse((cx + 10, cy - 27, cx + 18, cy - 19), fill=SUN, outline=INK, width=2)
    for k, rr in enumerate((10, 17)):
        d.arc((cx - 4 - rr, cy - 22 - rr, cx - 4 + rr, cy - 22 + rr), 200, 250, fill=CREAM, width=3)
    d.text((cx, cy + r + 14), 'radio', font=font(15), fill=(255, 255, 255, 200), anchor='mm')


def clock(im, cx, cy, shown=None):
    """Reloj de la misión: tapado (hay que tocarlo para mirar) o destapado un momento con el tiempo."""
    d = ImageDraw.Draw(im)
    r = 30
    d.ellipse((cx - r, cy - r + 4, cx + r, cy + r + 4), fill=INK)
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=SURFACE if shown is None else CREAM, outline=INK, width=4)
    if shown is None:
        d.text((cx, cy + 1), '?', font=font(30), fill=(255, 255, 255, 170), anchor='mm')
        d.text((cx, cy + r + 13), 'reloj', font=font(14), fill=(255, 255, 255, 180), anchor='mm')
    else:
        d.line((cx, cy, cx, cy - r + 9), fill=INK, width=4)
        d.line((cx, cy, cx + 12, cy + 6), fill=INK, width=4)
        d.text((cx, cy + r + 15), shown, font=font(20), fill=SUN, anchor='mm', stroke_width=2, stroke_fill=INK)


def frame_brief(raw):
    """Antes de salir: la hoja de ruta con los encargos (sin recuadros: ícono + texto)."""
    im = night(31)
    glow(im, W / 2, 0, 280, SKY, 45)
    d = ImageDraw.Draw(im)
    hud(im, 3, 'Encargos 2')
    d = ImageDraw.Draw(im)
    clay_text(d, (W / 2, 190), 'Tu hoja de ruta', 36)
    d.text((W / 2, 232), 'Recuerda tus encargos mientras vuelas', font=font(18), fill=(230, 232, 250), anchor='mm')
    # Encargo por lugar.
    glow(im, 110, 350, 60, CORAL, 70)
    put(im, load(f'{raw}/traffic_port_0.raw'), 110, 350, 100)
    package(im, 150, 385, 0.8)
    d = ImageDraw.Draw(im)
    d.text((185, 332), 'Cuando pases un', font=font(22), fill=(255, 255, 255), anchor='lm')
    d.text((185, 360), 'planeta coral, tócalo', font=font(22), fill=CORAL, anchor='lm', stroke_width=1, stroke_fill=INK)
    d.text((185, 390), 'le entregas su paquete', font=font(16), fill=(215, 220, 245), anchor='lm')
    # Encargo por hora.
    radio(im, 110, 505)
    d = ImageDraw.Draw(im)
    d.text((185, 488), 'Cada 30 segundos,', font=font(22), fill=(255, 255, 255), anchor='lm')
    d.text((185, 516), 'toca la radio', font=font(22), fill=GRAPE, anchor='lm', stroke_width=1, stroke_fill=INK)
    d.text((185, 546), 'avisas a la base que vas bien', font=font(16), fill=(215, 220, 245), anchor='lm')
    clock(im, 110, 650)
    d = ImageDraw.Draw(im)
    d.text((185, 640), 'El reloj va tapado:', font=font(20), fill=(255, 255, 255), anchor='lm')
    d.text((185, 668), 'tócalo si quieres mirar la hora', font=font(16), fill=(215, 220, 245), anchor='lm')
    # Botón.
    clay_box(im, (W / 2 - 120, 790, W / 2 + 120, 850), 30, LIME)
    d = ImageDraw.Draw(im)
    d.text((W / 2, 820), '¡A volar!', font=font(28), fill=INK, anchor='mm')
    return im


def frame_flight(raw):
    """En vuelo: la ruta con sobres, un planeta coral que pasa a la derecha (¿te acordarás?) y uno celeste que no."""
    im = night(32)
    glow(im, W / 2, 0, 260, SKY, 50)
    d = ImageDraw.Draw(im)
    hud(im, 3, 'Entregas 2')
    clock(im, W - 60, 60)
    c = lane(im, 3900, 703, 0.14, 0.24, False)
    for (x, y) in ((0.47, 560), (0.52, 430), (0.5, 300)):
        envelope(im, x * W, y)
    glow(im, 470, 330, 70, CORAL, 55)
    put(im, load(f'{raw}/traffic_port_0.raw'), 470, 330, 96)
    put(im, load(f'{raw}/traffic_port_2.raw'), 70, 480, 86)
    ship(im, raw, c * W + 4, 703, -6)
    d = ImageDraw.Draw(im)
    clay_text(d, (W / 2 - 60, 640), '+10', 24, SUN)
    radio(im, W - 62, 700)
    d = ImageDraw.Draw(im)
    control(im, d, '‹  Desliza aquí para guiar la nave  ›', 'En la ruta', LIME)
    return im


def frame_deliver(raw):
    """Tocaste el planeta coral: el paquete vuela hasta él (¡Entregado!); miraste el reloj (0:27): falta poco para la radio."""
    im = night(33)
    glow(im, W / 2, 0, 260, SKY, 50)
    d = ImageDraw.Draw(im)
    hud(im, 3, 'Entregas 3')
    clock(im, W - 60, 60, shown='0:27')
    c = lane(im, 4150, 703, 0.14, 0.24, False)
    envelope(im, 0.5 * W, 420)
    glow(im, 440, 440, 90, CORAL, 90)
    put(im, load(f'{raw}/traffic_port_0.raw'), 440, 440, 100)
    put(im, load(f'{raw}/mark_check.raw'), 478, 400, 40)
    ship(im, raw, c * W, 703, 4)
    # Estela del paquete desde la nave hasta el planeta.
    d = ImageDraw.Draw(im)
    sx, sy_ = c * W + 10, 660
    for k in range(7):
        t = k / 7
        x = sx + (440 - sx) * t
        y = sy_ + (445 - sy_) * t - 60 * (4 * t * (1 - t))
        d.ellipse((x - 4, y - 4, x + 4, y + 4), fill=SUN + (int(80 + 150 * t),))
    package(im, 420, 470, 0.8)
    d = ImageDraw.Draw(im)
    clay_text(d, (410, 360), '¡Entregado! +100', 26, LIME)
    radio(im, W - 62, 700, lit=True)
    d = ImageDraw.Draw(im)
    control(im, d, '', 'Encargo cumplido', LIME)
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('raw')
    ap.add_argument('--out', default=ROOT + '/docs/previews')
    a = ap.parse_args()
    panels = [frame_brief(a.raw), frame_flight(a.raw), frame_deliver(a.raw)]
    gap = 24
    sheet = Image.new('RGBA', (len(panels) * W + (len(panels) + 1) * gap, H + 2 * gap), (0x02, 0x03, 0x10, 255))
    for i, p in enumerate(panels):
        sheet.alpha_composite(p, (gap + i * (W + gap), gap))
    os.makedirs(a.out, exist_ok=True)
    sheet.convert('RGB').save(os.path.join(a.out, 'correo-estelar.png'))
    print('OK')


if __name__ == '__main__':
    main()
