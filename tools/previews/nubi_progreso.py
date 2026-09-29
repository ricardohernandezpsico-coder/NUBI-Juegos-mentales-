# Cómo mostrar el avance SIN planeta (29-sep). Ricardo aprobó Nubi y todo lo del mundo de Nubi menos el planeta: "siento
# que rompe el hilo conductor"; pide otro objeto o indicador. El hilo es la historia de Nubi: "la nebulosa donde nacen
# las estrellas; cada partida hace nacer una". Tres maneras de mostrar el avance que salen de esa historia (misma
# lógica de hoy, `data/Planet`: cada dominio crece con la práctica, nunca baja, 5 etapas):
#   A · Tu cielo: cada dominio es una CONSTELACIÓN de 5 estrellas que Nubi va encendiendo (las líneas se dibujan al
#       encenderse las dos puntas). Las de hoy recién nacidas, con destello.
#   B · Anillo de luz: Nubi al centro y un anillo de 6 tramos (uno por dominio) que se llena de a 5 partes.
#   C · Frascos de estrellas: Nubi guarda en un frasco por dominio las estrellas que nacen; se llenan.
# En las tres: los 3 juegos del día son satélites alrededor de Nubi y la zona del día va marcada.
# Uso: python3 tools/previews/nubi_progreso.py  ->  docs/previews/avance-sin-planeta.png
import os, math, random, importlib.util
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('npl', os.path.join(HERE, 'nubi_planetas.py'))
NP = importlib.util.module_from_spec(spec); spec.loader.exec_module(NP)
NM, NS, N, P = NP.NM, NP.NS, NP.N, NP.P
F, dp, W = P.F, P.dp, P.W
INK, SUN, CREAM, WHITE, DIM, LIME, SKY, CORAL = P.INK, P.SUN, P.CREAM, P.WHITE, P.DIM, P.LIME, P.SKY, P.CORAL
LINE, DOM = NS.LINE, P.DOM
PH = dp(560)
NP.PH = PH

ORDER = ['memoria', 'atencion', 'razonamiento', 'lenguaje', 'calculo', 'velocidad']
NAMES = {'memoria': 'Memoria', 'atencion': 'Atención', 'razonamiento': 'Razonamiento', 'lenguaje': 'Lenguaje',
         'calculo': 'Cálculo', 'velocidad': 'Velocidad'}
LEVEL = {'memoria': 4, 'atencion': 3, 'razonamiento': 2, 'lenguaje': 1, 'calculo': 3, 'velocidad': 4}
TODAY = {'memoria', 'atencion'}      # nació una estrella hoy
ZONE_DAY = 'atencion'
# colores claros de cada dominio (las estrellas brillan: versión clara del color del dominio)
LIGHT = {k: tuple(int(c + (255 - c) * 0.45) for c in v) for k, v in DOM.items()}

# constelaciones: 5 estrellas (en el orden en que se encienden) y sus líneas; figura que recuerda al dominio
SHAPES = {
    'memoria':      ([(0, -28), (20, -4), (0, 26), (-20, -4), (0, -4)], [(0, 1), (1, 2), (2, 3), (3, 0), (0, 4)]),     # cristal
    'atencion':     ([(-6, 26), (-4, -2), (0, -24), (4, -2), (6, 26)], [(0, 1), (1, 2), (2, 3), (3, 4), (1, 3)]),      # faro
    'razonamiento': ([(0, -26), (-16, 2), (16, 2), (-24, 26), (24, 26)], [(0, 1), (0, 2), (1, 2), (1, 3), (2, 4)]),   # torre
    'lenguaje':     ([(0, 28), (0, 6), (-18, -10), (18, -12), (0, -28)], [(0, 1), (1, 2), (1, 3), (2, 4), (3, 4)]),   # hoja
    'calculo':      ([(-28, 16), (-20, -8), (0, -20), (20, -8), (28, 16)], [(0, 1), (1, 2), (2, 3), (3, 4)]),         # domo
    'velocidad':    ([(12, -28), (-8, -4), (10, -2), (-10, 26), (18, 10)], [(0, 1), (1, 2), (2, 3), (2, 4)]),         # rayo
}


def ov(im, fn, blur=0):
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); fn(ImageDraw.Draw(lay))
    if blur: lay = lay.filter(ImageFilter.GaussianBlur(blur))
    im.alpha_composite(lay)


def star(im, x, y, r, col, new=False):
    NS.glow(im, x, y, r * (3.4 if new else 2.4), col, 170 if new else 110)
    d = ImageDraw.Draw(im)
    d.polygon(N.sparkle(x, y, r * 1.6), fill=col)
    d.ellipse([x - r * 0.5, y - r * 0.5, x + r * 0.5, y + r * 0.5], fill=WHITE)
    if new:
        ov(im, lambda dd: dd.ellipse([x - r * 2.4, y - r * 2.4, x + r * 2.4, y + r * 2.4], outline=WHITE + (170,), width=int(dp(1.2))))


def nubi_center(im, cx, cy, size):
    sp = NM.nubi_sprite(lambda c, x, y, kk: NS.nubi(c, x, y, kk, 'hola'), size)
    im.alpha_composite(sp, (int(cx - size / 2), int(cy - size / 2)))


def sats_around(im, cx, cy, rx, ry, back=True):
    if back:
        ov(im, lambda d: d.arc([cx - rx, cy - ry, cx + rx, cy + ry], 180, 360, fill=(200, 230, 255, 110), width=int(dp(1.5))))
        return
    ov(im, lambda d: d.arc([cx - rx, cy - ry, cx + rx, cy + ry], 0, 180, fill=(200, 230, 255, 150), width=int(dp(1.5))))
    for ang, col, done in [(190, DOM['memoria'], True), (350, DOM['atencion'], True), (95, DOM['velocidad'], False)]:
        a = math.radians(ang)
        NM.satellite(im, cx + math.cos(a) * rx, cy + math.sin(a) * ry, dp(13), col, done)


def day_mark(im, x, y, r):
    """Zona del día: aro sol punteado."""
    def f(d):
        n = 28
        for i in range(n):
            if i % 2: continue
            a0, a1 = 360 * i / n, 360 * (i + 1) / n
            d.arc([x - r, y - r, x + r, y + r], a0, a1, fill=SUN + (230,), width=int(dp(2)))
    ov(im, f)


def panel_a():
    im = NP.sky_panel(17)
    cx, cy = W / 2, dp(270)
    pos = {'memoria': (dp(84), dp(80)), 'atencion': (W - dp(84), dp(80)), 'razonamiento': (dp(62), dp(270)),
           'lenguaje': (W - dp(62), dp(270)), 'calculo': (dp(90), dp(440)), 'velocidad': (W - dp(90), dp(440))}
    for key in ORDER:
        x0, y0 = pos[key]; pts, lines = SHAPES[key]; lv = LEVEL[key]; k = dp(1.3)
        P_ = [(x0 + px * k, y0 + py * k) for px, py in pts]
        if key == ZONE_DAY: day_mark(im, x0, y0, dp(50))
        def draw_lines(d, P_=P_, lines=lines, lv=lv, key=key):
            for a, b in lines:
                lit = a < lv and b < lv
                if lit:
                    d.line([P_[a], P_[b]], fill=LIGHT[key] + (200,), width=int(dp(2)))
                else:
                    (xa, ya), (xb, yb) = P_[a], P_[b]; n = 9
                    for i in range(n):
                        if i % 2: continue
                        t0, t1 = i / n, (i + 1) / n
                        d.line([(xa + (xb - xa) * t0, ya + (yb - ya) * t0), (xa + (xb - xa) * t1, ya + (yb - ya) * t1)], fill=(255, 255, 255, 60), width=int(dp(1.2)))
        ov(im, draw_lines)
        for i, (x, y) in enumerate(P_):
            if i < lv:
                star(im, x, y, dp(5 if i == lv - 1 and key in TODAY else 4), LIGHT[key], new=(i == lv - 1 and key in TODAY))
            else:
                ov(im, lambda d, x=x, y=y: d.ellipse([x - dp(2.4), y - dp(2.4), x + dp(2.4), y + dp(2.4)], outline=(255, 255, 255, 110), width=int(dp(1.2))))
        d = ImageDraw.Draw(im)
        d.text((x0, y0 + dp(46)), NAMES[key], font=F(14), fill=SUN if key == ZONE_DAY else WHITE, anchor='ma')
        d.text((x0, y0 + dp(64)), f'{lv} de 5 estrellas', font=F(13, False), fill=DIM, anchor='ma')
    sats_around(im, cx, cy, dp(82), dp(24), True)
    nubi_center(im, cx, cy, dp(130))
    sats_around(im, cx, cy, dp(82), dp(24), False)
    d = ImageDraw.Draw(im)
    d.text((W / 2, PH - dp(24)), 'Hoy nacieron 2 estrellas', font=F(18), fill=SUN, anchor='mm')
    return im


def panel_b():
    im = NP.sky_panel(19)
    cx, cy, R, th = W / 2, dp(270), dp(128), dp(20)
    seg, gap = 60, 5
    for i, key in enumerate(ORDER):
        a0 = -90 - 30 + i * seg + gap / 2; a1 = a0 + seg - gap
        lv = LEVEL[key]
        for j in range(5):
            b0 = a0 + (a1 - a0) * j / 5 + 0.8; b1 = a0 + (a1 - a0) * (j + 1) / 5 - 0.8
            col = DOM[key] + (255,) if j < lv else (255, 255, 255, 38)
            ov(im, lambda d, b0=b0, b1=b1, col=col: d.arc([cx - R, cy - R, cx + R, cy + R], b0, b1, fill=col, width=int(th)))
        if key in TODAY:
            b = math.radians(a0 + (a1 - a0) * (lv - 0.5) / 5)
            star(im, cx + math.cos(b) * (R - th / 2), cy + math.sin(b) * (R - th / 2), dp(4), WHITE, new=True)
        am = math.radians((a0 + a1) / 2)
        lx, ly = cx + math.cos(am) * (R + dp(40)), cy + math.sin(am) * (R + dp(40))
        if key == ZONE_DAY: day_mark(im, lx, ly + dp(6), dp(40))
        d = ImageDraw.Draw(im)
        d.text((lx, ly - dp(4)), NAMES[key], font=F(14), fill=SUN if key == ZONE_DAY else WHITE, anchor='mm')
        d.text((lx, ly + dp(14)), f'{lv} de 5', font=F(13, False), fill=DIM, anchor='mm')
    ov(im, lambda d: d.ellipse([cx - R - dp(12), cy - R - dp(12), cx + R + dp(12), cy + R + dp(12)], outline=(255, 255, 255, 30), width=int(dp(1))))
    sats_around(im, cx, cy, dp(84), dp(24), True)
    nubi_center(im, cx, cy, dp(116))
    sats_around(im, cx, cy, dp(84), dp(24), False)
    d = ImageDraw.Draw(im)
    d.text((W / 2, PH - dp(40)), 'Hoy crecieron Memoria y Atención', font=F(18), fill=SUN, anchor='mm')
    return im


def jar(im, x, y, key, lv, new):
    w, h = dp(62), dp(74)
    x0, y0, x1, y1 = x - w / 2, y - h / 2, x + w / 2, y + h / 2
    col = DOM[key]
    # brillo del color adentro, según cuánto se llenó
    fill_top = y1 - dp(6) - (h - dp(16)) * lv / 5
    NS.glow(im, x, (fill_top + y1) / 2, dp(34), LIGHT[key], 90)
    ov(im, lambda d: d.rounded_rectangle([x0, y0, x1, y1], radius=dp(18), fill=(190, 210, 255, 40)))
    ov(im, lambda d: d.rounded_rectangle([x0 + dp(4), fill_top, x1 - dp(4), y1 - dp(4)], radius=dp(14), fill=col + (120,)))
    rnd = random.Random(hash(key) % 100)
    n = lv * 3
    for i in range(n):
        row = i // 3; sx = x0 + dp(14) + (i % 3) * dp(17) + rnd.uniform(-3, 3); sy = y1 - dp(13) - row * dp(11) + rnd.uniform(-2, 2)
        if sy < fill_top: break
        d = ImageDraw.Draw(im); d.polygon(N.star(sx, sy, dp(5.2), inner=0.5), fill=CREAM if i % 4 else SUN, outline=LINE)
    if new:
        star(im, x + dp(8), fill_top - dp(4), dp(4), WHITE, new=True)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([x0, y0, x1, y1], radius=dp(18), outline=LINE, width=int(dp(2.2)))
    d.rectangle([x - dp(18), y0 - dp(10), x + dp(18), y0 + dp(2)], fill=(215, 225, 255), outline=LINE, width=int(dp(2)))
    d.rounded_rectangle([x - dp(22), y0 - dp(20), x + dp(22), y0 - dp(9)], radius=dp(4), fill=col, outline=LINE, width=int(dp(2)))
    ov(im, lambda dd: dd.line([(x0 + dp(9), y0 + dp(12)), (x0 + dp(9), y1 - dp(18))], fill=(255, 255, 255, 150), width=int(dp(3))))


def panel_c():
    im = NP.sky_panel(23)
    cx, cy = W / 2, dp(120)
    sats_around(im, cx, cy, dp(96), dp(26), True)
    nubi_center(im, cx, cy, dp(120))
    sats_around(im, cx, cy, dp(96), dp(26), False)
    for i, key in enumerate(ORDER):
        x = dp(72) + (i % 3) * dp(134); y = dp(292) + (i // 3) * dp(142)
        if key == ZONE_DAY: day_mark(im, x, y + dp(6), dp(58))
        jar(im, x, y, key, LEVEL[key], key in TODAY)
        d = ImageDraw.Draw(im)
        d.text((x, y + dp(48)), NAMES[key], font=F(14), fill=SUN if key == ZONE_DAY else WHITE, anchor='ma')
    d = ImageDraw.Draw(im)
    d.text((W / 2, PH - dp(28)), 'Hoy guardaste 2 estrellas', font=F(18), fill=SUN, anchor='mm')
    return im


PANELS = [('A', 'Tu cielo', panel_a, 'Cada área es una constelación de 5 estrellas que Nubi va encendiendo. Sigue la historia de Nubi: '
           'cada partida hace nacer una estrella.'),
          ('B', 'Anillo de luz', panel_b, 'Nubi al centro y un anillo de 6 tramos que se llenan. El más claro para leer el avance de un vistazo.'),
          ('C', 'Frascos de estrellas', panel_c, 'Nubi guarda las estrellas que nacen en un frasco por área. Un objeto tierno y fácil de entender.')]


def sheet():
    gap = dp(96)
    H = dp(80) + len(PANELS) * (PH + gap)
    im = Image.new('RGBA', (W, int(H)), (10, 10, 30, 255)); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(26)), 'Tu avance, sin planeta · 3 ideas', font=F(22), fill=SUN)
    d.text((dp(20), dp(58)), 'Siguen la historia de Nubi: donde nacen las estrellas.', font=F(13, False), fill=DIM)
    y = dp(90)
    for letter, name, fn, txt in PANELS:
        pn = fn()
        m = Image.new('L', pn.size, 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, pn.size[0] - 1, pn.size[1] - 1], radius=dp(20), fill=255)
        im.paste(pn, (0, int(y)), m)
        d = ImageDraw.Draw(im)
        d.text((dp(20), y + PH + dp(12)), f'{letter} · {name}', font=F(18), fill=WHITE)
        P.wrap(d, dp(20), y + PH + dp(40), txt, F(13, False), DIM, W - dp(40))
        y += PH + gap
    out = os.path.join(P.OUT, 'avance-sin-planeta.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
