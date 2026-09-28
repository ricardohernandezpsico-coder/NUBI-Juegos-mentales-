# Radar rediseñado: "Rescate relámpago" (28-sep; Ricardo no aprobó el retoque de sí/no y pidió reestructurar el
# juego con evidencia y sin rozar la patente US 8,348,671). Maqueta PIL, NO el juego.
# Idea: informe total ("whole report", Sperling 1960; teoría de la atención visual TVA, Bundesen 1990): en un destello
# aparecen VARIOS astronautas repartidos por el radar (centro y borde); después de la interferencia se tocan todos los
# lugares donde se vieron. El destello se acorta con la escalera. Desde el nivel 5 hay robots parecidos que no se
# rescatan (informe parcial: "tu filtro"). El destello llega en un momento imprevisible (alerta, Penning et al. 2021).
# Uso: python3 tools/previews/radar_rescate.py  ->  docs/previews/radar-rescate.png
import os, math, random, importlib.util
from PIL import Image, ImageDraw, ImageFilter, ImageFont
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('prop', os.path.join(HERE, 'inicio_propuestas.py'))
P = importlib.util.module_from_spec(spec); spec.loader.exec_module(P)
INK, SUN, CREAM, WHITE, DIM, SOFT, LIME, CORAL, GRAPE, SKY = P.INK, P.SUN, P.CREAM, P.WHITE, P.DIM, P.SOFT, P.LIME, P.CORAL, P.GRAPE, P.SKY

SRC = Image.open(os.path.join(P.OUT, 'radar.png')).convert('RGBA')
BASE = SRC.crop((23, 23, 564, 983))          # panel 1 real del juego (541 x 960)
PW, PH = BASE.size
CX, CY, R = 271, 413, 203                      # centro y radio interior del radar en el panel
RINGS = (95, 170)                              # 2 anillos x 8 direcciones = 16 lugares
ASTRO = BASE.crop((375, 513, 420, 560))        # el astronauta real del juego


def font(sz, bold=True): return ImageFont.truetype(P.FB if bold else P.FS, sz)


def slot_xy(d, ring):
    a = math.radians(-90 + 45 * d)
    return CX + math.cos(a) * RINGS[ring], CY + math.sin(a) * RINGS[ring]


def empty_radar(sweep=None):
    im = BASE.copy(); d = ImageDraw.Draw(im)
    d.ellipse([CX - R, CY - R, CX + R, CY + R], fill=(20, 28, 86))
    for r in (45, 95, 132, 170):
        d.ellipse([CX - r, CY - r, CX + r, CY + r], outline=(58, 70, 150), width=2)
    for k in range(8):
        a = math.radians(-90 + 45 * k)
        d.line([(CX + math.cos(a) * 45, CY + math.sin(a) * 45), (CX + math.cos(a) * R, CY + math.sin(a) * R)], fill=(44, 54, 126), width=2)
    if sweep is not None:   # haz que gira (en el juego se mueve; aquí, quieto)
        lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
        ld.pieslice([CX - R, CY - R, CX + R, CY + R], sweep - 40, sweep, fill=(155, 229, 100, 70))
        im.alpha_composite(lay.filter(ImageFilter.GaussianBlur(6)))
    return im


def prompt(im, text, col=WHITE, sub=None):
    d = ImageDraw.Draw(im)
    top = [(x, y) for x in range(4, 16) for y in (120,)]
    d.rectangle([10, 116, PW - 10, 172], fill=im.getpixel((8, 130)))
    d.text((PW / 2, 142), text, font=font(28), fill=col, anchor='mm')
    if sub:
        d.rectangle([10, 660, PW - 10, 720], fill=im.getpixel((8, 690)))
        d.text((PW / 2, 690), sub, font=font(23, False), fill=DIM, anchor='mm')


def put_astro(im, x, y, s=1.0):
    a = ASTRO.resize((int(45 * s), int(47 * s)), Image.LANCZOS)
    im.alpha_composite(a, (int(x - a.width / 2), int(y - a.height / 2)))


def put_robot(im, x, y):
    """Robot de rescate apagado (distractor): casco cuadrado gris, visera recta. Mismo tamaño que el astronauta."""
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([x - 19, y - 17 + 4, x + 19, y + 17 + 4], radius=9, fill=INK)
    d.rounded_rectangle([x - 19, y - 17, x + 19, y + 17], radius=9, fill=(176, 184, 206), outline=INK, width=3)
    d.rectangle([x - 12, y - 7, x + 12, y + 3], fill=(60, 66, 110), outline=INK, width=2)
    d.line([(x, y - 17), (x, y - 25)], fill=INK, width=3); d.ellipse([x - 4, y - 30, x + 4, y - 22], fill=CORAL, outline=INK, width=2)


def check(d, cx, cy, s, col=INK):
    d.line([(cx - s * .5, cy), (cx - s * .12, cy + s * .4), (cx + s * .55, cy - s * .45)], fill=col, width=max(3, int(s * 0.28)), joint='curve')


def cross(d, cx, cy, s, col=INK):
    d.line([(cx - s * .4, cy - s * .4), (cx + s * .4, cy + s * .4)], fill=col, width=max(3, int(s * .26)))
    d.line([(cx - s * .4, cy + s * .4), (cx + s * .4, cy - s * .4)], fill=col, width=max(3, int(s * .26)))


def slots(im, marked=(), style=None):
    d = ImageDraw.Draw(im)
    for dd in range(8):
        for rg in (0, 1):
            x, y = slot_xy(dd, rg)
            st = (style or {}).get((dd, rg))
            if st == 'hit':
                P.disc(d, x, y, 30, LIME, 3, 2); put_astro(im, x, y - 2, 0.9); d = ImageDraw.Draw(im)
                P.disc(d, x + 24, y - 24, 12, LIME, 2, 1); check(d, x + 24, y - 24, 12)
            elif st == 'miss':
                d.ellipse([x - 32, y - 32, x + 32, y + 32], outline=SUN, width=4)
                a = ASTRO.resize((40, 42)).copy(); a.putalpha(a.split()[3].point(lambda v: v * 0.55))
                im.alpha_composite(a, (int(x - 20), int(y - 21))); d = ImageDraw.Draw(im)
            elif st == 'false':
                P.disc(d, x, y, 26, (120, 110, 190), 3, 2); cross(d, x, y, 22, CREAM)
            elif (dd, rg) in marked:
                P.disc(d, x, y, 28, SKY, 3, 3)
                d.ellipse([x - 9, y - 9, x + 9, y + 9], fill=CREAM, outline=INK, width=2)
            else:
                d.ellipse([x - 28, y - 28, x + 28, y + 28], outline=(120, 132, 210), width=3)


def button(im, label, fill=SUN):
    d = ImageDraw.Draw(im)
    x0, y0, x1, y1 = 120, 740, PW - 120, 820
    d.rounded_rectangle([x0, y0 + 8, x1, y1 + 8], radius=28, fill=INK)
    d.rounded_rectangle([x0, y0, x1, y1], radius=28, fill=fill, outline=INK, width=5)
    d.text(((x0 + x1) / 2, (y0 + y1) / 2), label, font=font(32), fill=INK, anchor='mm')


TARGETS = [(1, 1), (3, 0), (5, 1), (6, 0)]      # dónde estaban los 4 astronautas (dirección, anillo)
ROBOTS = [(0, 0), (4, 1)]                       # robots (desde el nivel 5)


def p_wait():
    im = empty_radar(sweep=-60); prompt(im, 'Atento al radar...', sub='El destello llega en cualquier momento')
    return im


def p_flash():
    im = empty_radar()
    for t in TARGETS: put_astro(im, *slot_xy(*t))
    for t in ROBOTS: put_robot(im, *slot_xy(*t))
    prompt(im, '¡Destello!', SUN, sub='120 ms · 4 astronautas y 2 robots')
    return im


def p_mask():
    im = SRC.crop((587, 23, 1128, 983)).copy(); prompt(im, '¡Destello!', SUN)
    return im


def p_answer():
    im = empty_radar(); prompt(im, 'Toca donde viste astronautas')
    slots(im, marked={(1, 1), (3, 0), (6, 0), (2, 1)})
    d = ImageDraw.Draw(im); d.rectangle([10, 660, PW - 10, 720], fill=im.getpixel((8, 690)))
    d.text((PW / 2, 690), '4 balizas puestas · toca de nuevo para sacar', font=font(22, False), fill=DIM, anchor='mm')
    button(im, '¡RESCATAR!')
    return im


def p_reveal():
    im = empty_radar()
    prompt(im, '¡Rescate triple!', LIME)
    slots(im, style={(1, 1): 'hit', (3, 0): 'hit', (6, 0): 'hit', (5, 1): 'miss', (2, 1): 'false'})
    for t in ROBOTS:
        x, y = slot_xy(*t); put_robot(im, x, y)
    d = ImageDraw.Draw(im); d.rectangle([10, 660, PW - 10, 720], fill=im.getpixel((8, 690)))
    d.text((PW / 2, 690), '3 de 4 · uno se escapó (aro sol)', font=font(22, False), fill=DIM, anchor='mm')
    return im


def p_final():
    """Final (en la app es Compose; aquí, idea): tu vistazo, tu captura y tu radar."""
    im = Image.new('RGBA', (PW, PH), (0, 0, 0, 0)); im.alpha_composite(P.sky(12).resize((PW, PH)))
    d = ImageDraw.Draw(im)
    d.text((32, 50), 'Rescate relámpago', font=font(34), fill=WHITE)
    y = 130
    d.text((32, y), 'TU VISTAZO', font=font(20), fill=SUN)
    d.text((32, y + 28), '96 ms', font=font(56), fill=WHITE)
    d.text((32, y + 96), 'con 3 astronautas a la vez, rescatas', font=font(21, False), fill=DIM)
    d.text((32, y + 122), 'casi todos 4 de cada 5 veces', font=font(21, False), fill=DIM)
    y = 330
    d.text((32, y), 'TU CAPTURA', font=font(20), fill=SUN)
    d.text((32, y + 28), '3,4 de un vistazo', font=font(40), fill=WHITE)
    for k in range(6):
        x = 50 + k * 72; full = k < 3; half = k == 3
        P.disc(d, x, y + 118, 26, LIME if full else (60, 70, 140), 3, 3)
        if full or half:
            put_astro(im, x, y + 116, 0.85) if full else None
            if half:
                d = ImageDraw.Draw(im); d.pieslice([x - 26, y + 92, x + 26, y + 144], 90, 270, fill=LIME, outline=INK, width=3)
        d = ImageDraw.Draw(im)
    d.text((32, y + 160), 'cuántos agarras en destellos largos', font=font(21, False), fill=DIM)
    y = 560
    d.text((32, y), 'TU FILTRO', font=font(20), fill=SUN)
    d.text((32, y + 28), 'Robots tocados: 1 de 14', font=font(30), fill=WHITE)
    d.text((32, y + 70), 'ignoras bien lo que no es tuyo', font=font(21, False), fill=DIM)
    y = 700
    d.text((32, y), 'TU RADAR', font=font(20), fill=SUN)
    rc = (PW - 150, y + 110)
    for k, v in enumerate((0.9, 0.8, 0.85, 0.6, 0.7, 0.55, 0.75, 0.9)):
        a0 = -90 + 45 * k - 20; rr = 20 + 90 * v
        d.pieslice([rc[0] - rr, rc[1] - rr, rc[0] + rr, rc[1] + rr], a0, a0 + 40, fill=(155, 229, 100, 255), outline=INK, width=2)
    d.ellipse([rc[0] - 112, rc[1] - 112, rc[0] + 112, rc[1] + 112], outline=(120, 132, 210), width=2)
    d.text((32, y + 40), 'por dirección y', font=font(21, False), fill=DIM)
    d.text((32, y + 66), 'cerca / lejos del centro', font=font(21, False), fill=DIM)
    d.text((32, PH - 60), 'Medida de esta partida. No es un diagnóstico.', font=font(19, False), fill=SOFT)
    return im


def arrow(d, x, y):
    d.line([(x - 12, y), (x + 10, y)], fill=SUN, width=5)
    d.polygon([(x + 16, y), (x + 5, y - 8), (x + 5, y + 8)], fill=SUN)


def sheet():
    sc = 0.5; pw, ph = int(PW * sc), int(PH * sc); gap = 34
    panels = [p_wait(), p_flash(), p_mask(), p_answer(), p_reveal(), p_final()]
    caps = [('1. Atento', 'el haz gira; el destello', 'llega cuando no se espera'),
            ('2. Destello', 'varios astronautas a la', 'vez, en todo el radar'),
            ('3. Interferencia', 'igual que hoy', ''),
            ('4. ¿Dónde estaban?', 'se tocan TODOS los lugares', '(16 casillas grandes)'),
            ('5. Revelación', 'lima = rescatado · aro sol = se', 'escapó · cruz = baliza de más'),
            ('6. El final', 'vistazo · captura', 'filtro · radar')]
    cols = 3; W = 40 + cols * pw + (cols - 1) * gap + 40
    H = 150 + 2 * (ph + 110) + 40
    im = P.sky(21).resize((W, H)); d = ImageDraw.Draw(im)
    d.text((40, 34), 'Radar rediseñado: «Rescate relámpago»', font=font(38), fill=WHITE)
    d.text((40, 88), 'Varios astronautas en un destello; se tocan todos los que se vieron. El destello se acorta.', font=font(21, False), fill=DIM)
    for i, pn in enumerate(panels):
        r, c = divmod(i, cols); x = 40 + c * (pw + gap); y = 140 + r * (ph + 110)
        im.alpha_composite(pn.resize((pw, ph), Image.LANCZOS), (x, y))
        d = ImageDraw.Draw(im)
        t, a, b = caps[i]
        d.text((x + pw / 2, y + ph + 12), t, font=font(22), fill=WHITE, anchor='ma')
        d.text((x + pw / 2, y + ph + 42), a, font=font(18, False), fill=DIM, anchor='ma')
        d.text((x + pw / 2, y + ph + 66), b, font=font(18, False), fill=DIM, anchor='ma')
        if c < cols - 1: arrow(d, x + pw + gap / 2, y + ph / 2)
    out = os.path.join(P.OUT, 'radar-rescate.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
