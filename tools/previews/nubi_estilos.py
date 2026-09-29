# Nubi replanteada (29-sep). Ricardo: el ícono de la 2.ª vuelta "no me gusta"; pide otros estilos que calcen con la app y
# repensar la nube misma. Lectura del estilo REAL de la app (planeta de Hoy, íconos de juegos): colores planos y
# saturados, borde tinta grueso, sombra dura hacia abajo, un brillo blanco simple arriba a la izquierda y formas
# geométricas redondeadas. Las dos vueltas anteriores eran pastel, con degradés y brillos difusos ("gelatina"): por eso
# no calzaban. Aquí cinco personajes nuevos, todos en ese estilo plano, cada uno con su ícono en la misma familia que
# los íconos de los juegos (fondo de color de la app + brillo arriba) y en tamaño real.
# Uso: python3 tools/previews/nubi_estilos.py  ->  docs/previews/nubi-estilos.png (+ nubi-estilo-<n>.png)
import os, math, importlib.util
import numpy as np
from PIL import Image, ImageDraw
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('ni', os.path.join(HERE, 'nombre_icono.py'))
N = importlib.util.module_from_spec(spec); spec.loader.exec_module(N)
P = N.P
F, dp, W, S = P.F, P.dp, P.W, N.S
INK, SUN, WHITE, DIM = P.INK, P.SUN, P.WHITE, P.DIM
CORAL, SKY, LIME = P.CORAL, P.SKY, P.LIME
CREAM = (255, 251, 242)
DOM = {'blue': (59, 130, 246), 'orange': (245, 158, 11), 'violet': (139, 92, 246), 'green': (16, 185, 129),
       'teal': (13, 148, 136), 'rose': (244, 63, 94)}
NIGHT = (40, 44, 120)
PINK = (255, 138, 190)
LILAC = (196, 176, 255)

Y, X = np.mgrid[0:S, 0:S].astype(np.float32)


# ---------- campos de distancia (en unidades locales; el personaje mide ~60 de ancho) ----------
def smin(a, b, k):
    h = np.clip(0.5 + 0.5 * (b - a) / k, 0, 1)
    return b * (1 - h) + a * h - k * h * (1 - h)


def circle(x, y, cx, cy, r): return np.sqrt((x - cx) ** 2 + (y - cy) ** 2) - r


def capsule(x, y, x0, x1, cy, r):
    bx = np.maximum(np.maximum(x0 - x, x - x1), 0)
    return np.sqrt(bx ** 2 + (y - cy) ** 2) - r


def polyline(x, y, pts, r):
    d = np.full(x.shape, 1e9, np.float32)
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        vx, vy = bx - ax, by - ay
        t = np.clip(((x - ax) * vx + (y - ay) * vy) / (vx * vx + vy * vy + 1e-9), 0, 1)
        d = np.minimum(d, np.sqrt((x - ax - vx * t) ** 2 + (y - ay - vy * t) ** 2))
    return d - r


def cover(sdf_px, aa=1.3): return np.clip(0.5 - sdf_px / aa, 0, 1)


def paint(im, alpha, rgb):
    lay = np.zeros((S, S, 4), np.uint8); lay[..., :3] = rgb; lay[..., 3] = (np.clip(alpha, 0, 1) * 255).astype(np.uint8)
    im.alpha_composite(Image.fromarray(lay))


class Char:
    """Un personaje: forma (campo de distancia local) + relleno plano + cara. (cx, cy) en px, k = px por unidad."""
    def __init__(s, im, cx, cy, k): s.im, s.cx, s.cy, s.k = im, cx, cy, k; s.x = (X - cx) / k; s.y = (Y - cy) / k

    def at(s, dy=0.0): return s.x, (Y - s.cy - dy) / s.k

    def body(s, sdf_fn, fill, border=2.4, drop=2.6):
        k = s.k
        xs, ys = s.at(drop * k); paint(s.im, cover((sdf_fn(xs, ys) - border) * k), INK)   # sombra dura
        d = sdf_fn(s.x, s.y)
        paint(s.im, cover((d - border) * k), INK)                                        # borde
        s.inside = cover(d * k); s.d = d
        paint(s.im, s.inside, fill)

    def region(s, sdf_fn, fill, line=1.2):
        """Segunda zona de color dentro del cuerpo, separada por una línea tinta fina."""
        r = sdf_fn(s.x, s.y)
        paint(s.im, s.inside * cover(r * s.k), fill)
        paint(s.im, s.inside * cover((np.abs(r) - line / 2) * s.k), INK)

    def P(s, x, y): return (s.cx + x * s.k, s.cy + y * s.k)

    def face(s, fy=0.0, gap=8.0, er=3.4, pose='hola', cheek=True, eye=INK):
        d = ImageDraw.Draw(s.im); k = s.k
        for sx in (-1, 1):
            x, y = s.P(sx * gap, fy)
            r = er * k
            if pose == 'celebra':
                d.arc([x - r, y - r, x + r, y + r * 1.3], 200, 340, fill=eye, width=max(2, int(1.5 * k)))
            else:
                d.ellipse([x - r, y - r * 1.2, x + r, y + r * 1.2], fill=eye)
                hx, hy = x - r * 0.3, y - r * 0.45
                d.ellipse([hx - r * 0.38, hy - r * 0.38, hx + r * 0.38, hy + r * 0.38], fill=WHITE)
        mx, my = s.P(0, fy + 5.2)
        w = 3.8 * k
        if pose == 'celebra':
            d.chord([mx - w, my - w * 0.6, mx + w, my + w * 1.1], 0, 180, fill=eye)
        else:
            d.arc([mx - w, my - w * 0.8, mx + w, my + w * 0.8], 25, 155, fill=eye, width=max(2, int(1.5 * k)))
        if cheek:
            lay = Image.new('RGBA', s.im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
            for sx in (-1, 1):
                x, y = s.P(sx * (gap + 5.2), fy + 4.2)
                ld.ellipse([x - 2.9 * k, y - 1.7 * k, x + 2.9 * k, y + 1.7 * k], fill=CORAL + (150,))
            s.im.alpha_composite(lay)

    def flat_shine(s, cx, cy, rx, ry, a=140):
        lay = Image.new('RGBA', s.im.size, (0, 0, 0, 0))
        x0, y0 = s.P(cx - rx, cy - ry); x1, y1 = s.P(cx + rx, cy + ry)
        ImageDraw.Draw(lay).ellipse([x0, y0, x1, y1], fill=(255, 255, 255, a))
        m = Image.fromarray((s.inside * 255).astype(np.uint8))
        lay.putalpha(Image.fromarray(np.minimum(np.asarray(lay.getchannel('A')), np.asarray(m))))
        s.im.alpha_composite(lay)

    def sparkle(s, x, y, r, col=CREAM, outline=False):
        d = ImageDraw.Draw(s.im); cx, cy = s.P(x, y)
        if outline:
            d.polygon(N.sparkle(cx, cy + 1.2 * s.k, r * s.k * 1.25), fill=INK)
            d.polygon(N.sparkle(cx, cy, r * s.k * 1.25), fill=INK)
        d.polygon(N.sparkle(cx, cy, r * s.k), fill=col)

    def star(s, x, y, r, col=SUN):
        d = ImageDraw.Draw(s.im); cx, cy = s.P(x, y)
        pts = N.star(cx, cy, r * s.k, inner=0.52)
        d.polygon([(px, py + 2 * s.k) for px, py in pts], fill=INK)
        d.polygon(pts, fill=col, outline=INK, width=max(2, int(2 * s.k)))
        hx, hy = s.P(x - r * 0.25, y - r * 0.2)
        d.ellipse([hx - r * 0.18 * s.k, hy - r * 0.12 * s.k, hx + r * 0.18 * s.k, hy + r * 0.12 * s.k], fill=(255, 255, 255, 170))


# ---------- los cinco personajes ----------
def pebble_sdf(x, y):
    a = np.arctan2(y, x)
    r = 27 * (1 + 0.055 * np.cos(3 * a + 0.6) + 0.035 * np.cos(2 * a - 0.4))
    return np.sqrt(x ** 2 + (y / 0.84) ** 2) - r


def c1_guijarro(im, cx, cy, k, pose='hola'):
    """1 · Guijarro de cielo: una piedrita de cielo como las zonas del planeta, con estrellas dentro."""
    c = Char(im, cx, cy, k); c.body(pebble_sdf, DOM['violet'])
    c.flat_shine(-10, -14, 8, 3.6)
    for x, y, r in [(-17, -5, 2.4), (15, -12, 1.8), (19, 6, 2.2), (-8, 15, 1.6), (5, -17, 1.4)]:
        c.sparkle(x, y, r * 1.5)
    c.face(fy=1.5, pose=pose)
    if pose == 'celebra':
        c.star(-24, -24, 5); c.star(24, -22, 4, LIME)


def cloud_sdf(x, y):
    d = capsule(x, y, -17, 17, 8, 10)
    for px, py, r in [(-15, 0, 11), (0, -8, 15), (15, 0, 11)]:
        d = smin(d, circle(x, y, px, py, r), 3)
    return d


def sprout_pts():
    return [(2, -21), (3, -28), (6, -33), (11, -36), (16, -35)]


def c2_brote(im, cx, cy, k, pose='hola'):
    """2 · Nube con brote: una nube de arcilla blanca a la que le brota una estrella."""
    c = Char(im, cx, cy + 4 * k, k)
    stem = sprout_pts()
    # tallito: tinta gruesa + lima encima (como los trazos de los íconos de juegos)
    d = ImageDraw.Draw(im)
    pts = [c.P(x, y) for x, y in stem]
    d.line([(px, py + 2 * k) for px, py in pts], fill=INK, width=int(6.4 * k), joint='curve')
    d.line(pts, fill=INK, width=int(6.4 * k), joint='curve')
    d.line(pts, fill=LIME, width=int(3 * k), joint='curve')
    c.body(cloud_sdf, CREAM)
    c.flat_shine(-6, -15, 7, 3, 200)
    c.face(fy=2, pose=pose)
    c.star(18, -34, 9 if pose == 'hola' else 10.5)
    if pose == 'celebra':
        c.sparkle(-24, -18, 3.4, SUN, True); c.sparkle(28, -8, 2.6, SKY, True)


def c3_cielo(im, cx, cy, k, pose='hola'):
    """3 · Nube con cielo adentro: arriba nube blanca, la barriga es un pedazo de noche con estrellas."""
    c = Char(im, cx, cy + 2 * k, k); c.body(cloud_sdf, NIGHT)
    for x, y, r in [(-19, 10, 1.9), (-8, 14, 1.4), (6, 12, 2.4), (19, 9, 1.6)]:
        c.sparkle(x, y, r * 1.5, SUN if r > 2 else CREAM)

    def top(x, y):   # la nube de arriba: copos que bajan hasta media altura
        d = circle(x, y, 0, -9, 14.5)
        for px, py, r in [(-15, -3, 10), (15, -3, 10), (-7, -1, 8.5), (7, -1, 8.5)]:
            d = smin(d, circle(x, y, px, py, r), 2.2)
        return d
    c.region(top, CREAM)
    c.flat_shine(-6, -15, 7, 3, 200)
    c.face(fy=-4.5, pose=pose, gap=7.5, er=3.1)
    if pose == 'celebra':
        c.star(-26, -20, 4.5); c.star(26, -18, 3.6, LIME)


def pompom_sdf(x, y):
    d = circle(x, y, 0, 1, 21)
    n = 11
    for i in range(n):
        a = -math.pi / 2 + 2 * math.pi * i / n
        d = smin(d, circle(x, y, math.cos(a) * 21, 1 + math.sin(a) * 19.5, 7.2), 1.6)
    return d


def c4_pompon(im, cx, cy, k, pose='hola'):
    """4 · Pompón: una bolita esponjosa y redonda, rosa, con un anillo de polvo de estrellas."""
    c = Char(im, cx, cy, k)
    c.body(pompom_sdf, PINK)
    c.region(lambda x, y: circle(x, y, 0, 6, 15.5), (255, 176, 212), line=0.001)
    c.flat_shine(-9, -13, 7, 3.2, 170)
    c.face(fy=3, pose=pose)
    c.sparkle(-24, -22, 3.6 if pose == 'hola' else 4.4, SUN, True)
    c.sparkle(25, -19, 2.4, CREAM, True)
    if pose == 'celebra':
        c.sparkle(28, 12, 2.8, SKY, True)


def curl_pts():
    pts = []
    for i in range(40):     # rizo: sube desde la cabeza y se enrolla (espiral de Arquímedes, 1,1 vueltas)
        t = i / 39
        a = math.pi + t * 2 * math.pi * 1.35
        r = 9.5 * (1 - 0.8 * t)
        pts.append((9.5 + math.cos(a) * r, -30 + math.sin(a) * r))
    return [(-2, -18), (-1, -25)] + pts


def c5_remolino(im, cx, cy, k, pose='hola'):
    """5 · Remolino: una bolita de nebulosa con un rizo arriba (el remolino donde se enciende una estrella)."""
    c = Char(im, cx, cy + 5 * k, k)
    curl = curl_pts()
    sd = lambda x, y: smin(circle(x, y, 0, 2, 23), polyline(x, y, curl, 2.8), 3.5)
    c.body(sd, SKY)
    c.region(lambda x, y: circle(x, y, 0, 13, 13), (120, 222, 250), line=0.001)
    c.flat_shine(-9, -10, 7, 3.2, 170)
    c.face(fy=3, pose=pose)
    ex, ey = curl[-1]
    c.sparkle(ex, ey, 3.2, SUN)
    if pose == 'celebra':
        c.sparkle(-25, -12, 3.4, SUN, True); c.sparkle(26, -4, 2.6, CREAM, True)


CHARS = [
    ('1', 'Guijarro de cielo', c1_guijarro, 'orange',
     'Una piedrita del cielo, hermana de las zonas de tu planeta, con estrellas dentro.'),
    ('2', 'Nube con brote', c2_brote, 'violet',
     'Nube blanca a la que le brota una estrella: cada partida, un brote nuevo.'),
    ('3', 'Cielo adentro', c3_cielo, 'rose',
     'Nube por arriba y un pedazo de noche en la barriga, donde nacen sus estrellas.'),
    ('4', 'Pompón', c4_pompon, 'blue',
     'Una bolita esponjosa y redonda: la silueta más simple, se reconoce de lejos.'),
    ('5', 'Remolino', c5_remolino, 'violet',
     'Una bolita de nebulosa con un rizo que gira y enciende una estrella en la punta.'),
]


def icon_bg(col):
    """Fondo del ícono: un color plano de la app (el de los planetas de los juegos)."""
    return Image.new('RGBA', (S, S), col + (255,))


def make_icon(fn, col):
    im = icon_bg(DOM[col]); fn(im, S / 2, S / 2 + N.u(3), N.u(1.0)); return im


def make_art(fn, pose, bg=None):
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0)) if bg is None else bg
    fn(im, S / 2, S / 2 + N.u(2), N.u(1.25), pose)
    return im


def wallpaper(w, h, dark):
    t = np.linspace(0, 1, int(h))[:, None, None]
    a, b = (np.array([20, 24, 40.]), np.array([48, 40, 70.])) if dark else (np.array([236, 226, 214.]), np.array([196, 214, 232.]))
    return Image.fromarray(np.repeat(a + (b - a) * t, int(w), axis=1).astype(np.uint8)).convert('RGBA')


def sheet():
    H = dp(124) + len(CHARS) * dp(224) + dp(360)
    im = P.sky(31).resize((W, int(H))); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(28)), 'Nubi replanteada · 5 ideas', font=F(24), fill=SUN)
    P.wrap(d, dp(20), dp(62), 'En el estilo real de la app: colores planos, borde tinta, sombra dura y un brillo simple, como '
           'el planeta de Hoy y los íconos de los juegos.', F(13, False), DIM, W - dp(40))
    y = dp(124)
    icons = []
    for num, name, fn, col, txt in CHARS:
        d = ImageDraw.Draw(im)
        d.text((dp(20), y), f'{num} · {name}', font=F(18), fill=WHITE)
        P.wrap(d, dp(20), y + dp(28), txt, F(13, False), DIM, W - dp(40))
        ay = y + dp(66)
        big = make_art(fn, 'hola')
        im.alpha_composite(big.resize((int(dp(140)), int(dp(140))), Image.LANCZOS), (int(dp(10)), int(ay - dp(10))))
        cel = make_art(fn, 'celebra')
        im.alpha_composite(cel.resize((int(dp(96)), int(dp(96))), Image.LANCZOS), (int(dp(150)), int(ay + dp(12))))
        ic = make_icon(fn, col); icons.append((num, ic))
        im.alpha_composite(N.masked(ic, int(dp(88)), 'squircle'), (int(dp(248)), int(ay + dp(16))))
        im.alpha_composite(N.masked(ic, int(dp(48)), 'circle'), (int(dp(348)), int(ay + dp(36))))
        d = ImageDraw.Draw(im)
        d.text((dp(198), ay + dp(116)), 'celebra', font=F(12, False), fill=DIM, anchor='ma')
        d.text((dp(320), ay + dp(116)), 'ícono', font=F(12, False), fill=DIM, anchor='ma')
        d.line([(dp(20), y + dp(208)), (W - dp(20), y + dp(208))], fill=(255, 255, 255, 30), width=int(dp(1)))
        y += dp(224)
        ic.resize((512, 512), Image.LANCZOS).convert('RGB').save(os.path.join(P.OUT, f'nubi-estilo-{num}.png'))

    d.text((dp(20), y + dp(2)), 'En tu teléfono, tamaño real', font=F(17), fill=SUN)
    y += dp(34)
    for dark in (True, False):
        wp = wallpaper(W - dp(40), dp(118), dark)
        m = Image.new('L', wp.size, 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, wp.size[0] - 1, wp.size[1] - 1], radius=dp(18), fill=255)
        im.paste(wp, (int(dp(20)), int(y)), m)
        step = (W - dp(40)) / len(icons)
        for i, (num, ic) in enumerate(icons):
            x = dp(20) + step * i + step / 2
            im.alpha_composite(N.masked(ic, int(dp(50)), 'circle' if dark else 'squircle'), (int(x - dp(25)), int(y + dp(20))))
            d = ImageDraw.Draw(im)
            d.text((x, y + dp(78)), 'Nubi', font=F(13, False), fill=WHITE if dark else (30, 30, 40), anchor='ma')
            d.text((x, y + dp(97)), num, font=F(12), fill=SUN if dark else (120, 80, 20), anchor='ma')
        y += dp(132)
    P.wrap(d, dp(20), y + dp(4), 'El color de fondo de cada ícono es uno de los colores de la app y se puede cambiar. Los '
           'personajes se pueden mezclar (por ejemplo, el brote de la 2 en el pompón de la 4).', F(13, False), DIM, W - dp(40))
    out = os.path.join(P.OUT, 'nubi-estilos.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
