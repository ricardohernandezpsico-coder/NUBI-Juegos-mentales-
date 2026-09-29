# Nubi como NEBULOSA (29-sep, 4.ª vuelta). Ricardo: "se supone que es una nebulosa; colores que no evoquen un género, de
# interpretación mixta; sin excentricidades, pero llamativa". Qué hace que una nebulosa se lea como tal, en el estilo
# plano de la app: una nube de gas con CAPAS de color (afuera más oscuro, adentro un núcleo luminoso), un halo que brilla
# alrededor y estrellas que nacen dentro. Misma silueta simple en todas (nube redonda, sin brotes ni rizos) y colores
# sin carga de género: turquesa, celeste, sol, naranja, lima, azul (nada de rosa ni lila). Seis combinaciones.
# Uso: python3 tools/previews/nubi_nebulosas.py  ->  docs/previews/nubi-nebulosas.png (+ nubi-nebulosa-<n>.png)
import os, math, random, importlib.util
import numpy as np
from PIL import Image, ImageDraw
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('ne', os.path.join(HERE, 'nubi_estilos.py'))
E = importlib.util.module_from_spec(spec); spec.loader.exec_module(E)
N, P = E.N, E.P
F, dp, W, S, u = P.F, P.dp, P.W, E.S, N.u
X, Y = E.X, E.Y
INK, WHITE, DIM, SUN, CREAM = E.INK, E.WHITE, E.DIM, E.SUN, E.CREAM
circle, smin, cover, paint, Char = E.circle, E.smin, E.cover, E.paint, E.Char

TEAL, TEAL_L = (13, 148, 136), (45, 196, 178)
SKY, SKY_L = (76, 201, 240), (150, 228, 250)
BLUE, BLUE_D = (59, 130, 246), (44, 70, 190)
ORANGE, CORAL = (245, 158, 11), (255, 107, 74)
LIME = (155, 229, 100)
GOLD_L = (255, 226, 130)


def neb_sdf(x, y):
    """La silueta: una nube redonda y ancha, de copos suaves (igual en las seis)."""
    d = circle(x, y, 0, 4, 21)
    for px, py, r in [(-18, 5, 11.5), (-10, -9, 12.5), (7, -11, 13.5), (19, 0, 11.5), (-4, 14, 13), (11, 13, 12)]:
        d = smin(d, circle(x, y, px, py, r), 3.2)
    return d


def mid_sdf(x, y):
    """Capa del medio: una nube más chica y más lisa, corrida hacia la luz (arriba a la izquierda)."""
    d = circle(x, y, -1, 2, 16.5)
    for px, py, r in [(-12, -3, 9), (4, -7, 10), (13, 4, 8.5), (-8, 11, 9), (6, 11, 8.5)]:
        d = smin(d, circle(x, y, px, py, r), 3)
    return d


def core_sdf(x, y):
    """El núcleo luminoso: una nube ancha e irregular corrida hacia la luz; la cara queda sobre él y la capa del medio."""
    d = circle(x, y, -2, 2, 10.5)
    for px, py, r in [(-11, 3, 7.5), (8, 0, 8), (-4, -6.5, 7), (4, 8, 7), (14, 6, 5), (-9, 10, 5)]:
        d = smin(d, circle(x, y, px, py, r), 2.6)
    return d


MID = mid_sdf
CORE = core_sdf
GLOW = lambda x, y: core_sdf(x, y) - 3


def flat(c, sdf_fn, col, a=1.0):
    paint(c.im, c.inside * cover(sdf_fn(c.x, c.y) * c.k) * a, col)


def halo(im, cx, cy, k, col):
    """Halo de nebulosa en capas planas (no difuminado): dos siluetas más anchas y transparentes."""
    x, y = (X - cx) / k, (Y - cy) / k
    for grow, a in ((9, 0.14), (5, 0.22)):
        paint(im, cover((neb_sdf(x, y) - grow) * k) * a, col)


def stars(c, pts):
    for x, y, r, col in pts:
        c.sparkle(x, y, r, col)


def base(im, cx, cy, k, outer, halo_col):
    halo(im, cx, cy, k, halo_col)
    c = Char(im, cx, cy, k); c.body(neb_sdf, outer)
    return c


def face(c, pose): c.face(fy=4, gap=7.6, er=3.2, pose=pose)


def n1(im, cx, cy, k, pose='hola'):
    """Frío y cálido: turquesa afuera, celeste, núcleo sol."""
    c = base(im, cx, cy, k, TEAL, SKY)
    flat(c, MID, SKY)
    flat(c, CORE, GOLD_L)
    c.flat_shine(-11, -14, 6, 2.6, 150)
    stars(c, [(-21, 3, 2.6, CREAM), (20, -4, 2.2, CREAM), (4, -15, 1.8, CREAM)])
    face(c, pose)


def n2(im, cx, cy, k, pose='hola'):
    """Dos corrientes: una mitad celeste, otra naranja, y se mezclan en un núcleo claro."""
    c = base(im, cx, cy, k, SKY, (120, 200, 230))
    # la mitad derecha naranja, con un borde que ondula (dos corrientes de gas)
    edge = c.x - 1 + 0.3 * c.y - 2.2 * np.sin(c.y / 4.5)
    paint(c.im, c.inside * cover(-edge * c.k), ORANGE)
    flat(c, CORE, CREAM)
    c.flat_shine(-11, -14, 6, 2.6, 150)
    stars(c, [(-20, 2, 2.6, CREAM), (21, -3, 2.4, CREAM), (-6, -15, 1.8, CREAM)])
    face(c, pose)


def n3(im, cx, cy, k, pose='hola'):
    """Luz dentro: azul noche afuera, azul, y un núcleo dorado que brilla."""
    c = base(im, cx, cy, k, BLUE_D, SKY)
    flat(c, MID, BLUE)
    flat(c, GLOW, (255, 214, 90), 0.45)
    flat(c, CORE, GOLD_L)
    c.flat_shine(-11, -14, 6, 2.6, 130)
    stars(c, [(-21, 3, 2.6, SUN), (20, -4, 2.2, CREAM), (5, -15, 1.9, CREAM), (-9, -12, 1.4, CREAM)])
    face(c, pose)


def n4(im, cx, cy, k, pose='hola'):
    """Aurora: turquesa con franjas lima, como una aurora dentro de la nube."""
    c = base(im, cx, cy, k, TEAL, LIME)
    band = np.sin((c.y + 0.55 * c.x) / 5.2 + 0.8 * np.sin(c.x / 7.0))
    paint(c.im, c.inside * np.clip((band - 0.35) * 6, 0, 1) * (c.y < 0), TEAL_L)
    flat(c, CORE, (205, 245, 170))
    c.flat_shine(-11, -14, 6, 2.6, 150)
    stars(c, [(-21, 4, 2.6, CREAM), (20, -3, 2.2, CREAM)])
    face(c, pose)


def n5(im, cx, cy, k, pose='hola'):
    """Fuego estelar: naranja coral afuera, naranja, núcleo crema: una estrella a punto de nacer."""
    c = base(im, cx, cy, k, CORAL, ORANGE)
    flat(c, MID, ORANGE)
    flat(c, CORE, (255, 236, 180))
    c.flat_shine(-11, -14, 6, 2.6, 150)
    stars(c, [(-21, 3, 2.6, CREAM), (20, -4, 2.2, CREAM), (4, -15, 1.8, CREAM)])
    face(c, pose)


def n6(im, cx, cy, k, pose='hola'):
    """Remolino interior: azul con dos brazos celestes que giran hacia un núcleo sol (la silueta sigue simple)."""
    c = base(im, cx, cy, k, BLUE, SKY)
    r = np.sqrt(c.x ** 2 + (c.y - 3) ** 2) + 1e-3
    th = np.arctan2(c.y - 3, c.x)
    arm = np.cos(2 * th - np.log(r) * 2.6)
    paint(c.im, c.inside * np.clip((arm - 0.25) * 5, 0, 1) * (r > 10), SKY)
    flat(c, CORE, GOLD_L)
    c.flat_shine(-11, -14, 6, 2.6, 150)
    stars(c, [(-21, 4, 2.4, CREAM), (21, -3, 2.2, CREAM)])
    face(c, pose)


OPTS = [
    ('1', 'Frío y cálido', n1, 'Turquesa, celeste y un núcleo sol.'),
    ('2', 'Dos corrientes', n2, 'Celeste y naranja que se mezclan al centro.'),
    ('3', 'Luz dentro', n3, 'Azul noche con un núcleo dorado.'),
    ('4', 'Aurora', n4, 'Turquesa con franjas lima.'),
    ('5', 'Fuego estelar', n5, 'Coral y naranja: una estrella por nacer.'),
    ('6', 'Remolino', n6, 'Azul con brazos celestes que giran.'),
]


def sky_bg():
    """Fondo del ícono: la noche de la app, un poco más clara que el cielo de las pantallas para no perderse."""
    t = np.linspace(1, 0, S)[:, None, None]
    top, bot = np.array([44, 52, 150.]), np.array([22, 24, 82.])
    im = Image.fromarray(np.repeat(bot + (top - bot) * t, S, axis=1).clip(0, 255).astype(np.uint8)).convert('RGBA')
    d = ImageDraw.Draw(im); rnd = random.Random(4)
    for _ in range(9):
        x, y = rnd.uniform(u(20), S - u(20)), rnd.uniform(u(20), S - u(20))
        if abs(x - S / 2) < u(33) and abs(y - S / 2) < u(30): continue
        r = u(0.5 + rnd.random() * 0.5); d.ellipse([x - r, y - r, x + r, y + r], fill=(255, 255, 255, 190))
    return im


def make_icon(fn):
    im = sky_bg(); fn(im, S / 2, S / 2 + u(1), u(0.92)); return im


def make_art(fn, pose):
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0)); fn(im, S / 2, S / 2, u(1.2), pose); return im


def sheet():
    cols, cw, ch = 2, (W - dp(40)) / 2, dp(300)
    H = dp(110) + 3 * ch + dp(370)
    im = P.sky(37).resize((W, int(H))); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(28)), 'Nubi, nebulosa · 6 opciones', font=F(24), fill=E.SUN)
    P.wrap(d, dp(20), dp(62), 'Misma silueta simple; capas de color con un núcleo que brilla, halo y estrellas que nacen. '
           'Colores sin carga de género (nada de rosa ni lila).', F(13, False), DIM, W - dp(40))
    icons = []
    for i, (num, name, fn, txt) in enumerate(OPTS):
        x0 = dp(20) + (i % cols) * cw; y0 = dp(118) + (i // cols) * ch
        art = make_art(fn, 'hola')
        im.alpha_composite(art.resize((int(dp(150)), int(dp(150))), Image.LANCZOS), (int(x0 + cw / 2 - dp(75)), int(y0)))
        d = ImageDraw.Draw(im)
        d.text((x0 + cw / 2, y0 + dp(150)), f'{num} · {name}', font=F(16), fill=WHITE, anchor='ma')
        P.wrap(d, x0 + cw / 2, y0 + dp(174), txt, F(13, False), DIM, cw - dp(16), anchor='center')
        ic = make_icon(fn); icons.append((num, ic))
        cel = make_art(fn, 'celebra')
        im.alpha_composite(cel.resize((int(dp(64)), int(dp(64))), Image.LANCZOS), (int(x0 + dp(4)), int(y0 + dp(214))))
        im.alpha_composite(N.masked(ic, int(dp(56)), 'squircle'), (int(x0 + dp(74)), int(y0 + dp(218))))
        im.alpha_composite(N.masked(ic, int(dp(40)), 'circle'), (int(x0 + dp(140)), int(y0 + dp(226))))
        ic.resize((512, 512), Image.LANCZOS).convert('RGB').save(os.path.join(P.OUT, f'nubi-nebulosa-{num}.png'))
    y = dp(118) + 3 * ch
    d = ImageDraw.Draw(im)
    d.text((dp(20), y), 'En tu teléfono, tamaño real', font=F(17), fill=E.SUN)
    y += dp(32)
    for dark in (True, False):
        wp = E.wallpaper(W - dp(40), dp(116), dark)
        m = Image.new('L', wp.size, 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, wp.size[0] - 1, wp.size[1] - 1], radius=dp(18), fill=255)
        im.paste(wp, (int(dp(20)), int(y)), m)
        step = (W - dp(40)) / len(icons)
        for i, (num, ic) in enumerate(icons):
            x = dp(20) + step * i + step / 2
            im.alpha_composite(N.masked(ic, int(dp(48)), 'circle' if dark else 'squircle'), (int(x - dp(24)), int(y + dp(20))))
            d = ImageDraw.Draw(im)
            d.text((x, y + dp(76)), 'Nubi', font=F(13, False), fill=WHITE if dark else (30, 30, 40), anchor='ma')
            d.text((x, y + dp(95)), num, font=F(12), fill=E.SUN if dark else (120, 80, 20), anchor='ma')
        y += dp(130)
    P.wrap(d, dp(20), y + dp(2), 'Abajo de cada una: celebrando, su ícono cuadrado y redondo. Los colores y el tipo de '
           'capas se pueden combinar entre opciones.', F(13, False), DIM, W - dp(40))
    out = os.path.join(P.OUT, 'nubi-nebulosas.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
