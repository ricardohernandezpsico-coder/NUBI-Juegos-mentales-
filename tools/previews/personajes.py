# Personaje de la app (29-sep, Ricardo: le gustan Pulsi, Nubi y Kibo como nombre y quiere construir el personaje).
# Maqueta PIL, NO la app: los tres personajes en arcilla ("noche + arcilla"), cada uno en grande, como ícono de la app
# (48 dp, recortado como lo hace Android) y con su idea en una línea. Regla (patentes y tono): el personaje NUNCA se pone
# triste cuando a la persona le va mal; celebra y acompaña, no evalúa.
# Uso: python3 tools/previews/personajes.py  ->  docs/previews/personajes.png
import os, math, importlib.util
from PIL import Image, ImageDraw
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('ni', os.path.join(HERE, 'nombre_icono.py'))
N = importlib.util.module_from_spec(spec); spec.loader.exec_module(N)
P = N.P
F, dp, W, u, S = P.F, P.dp, P.W, N.u, N.S
INK, SUN, CREAM, WHITE, DIM, SOFT = P.INK, P.SUN, P.CREAM, P.WHITE, P.DIM, P.SOFT
LIME, CORAL, SKY, GRAPE = P.LIME, P.CORAL, P.SKY, P.GRAPE


def eyes(im, cx, cy, gap, r, look=(0, 0), fill=INK):
    d = ImageDraw.Draw(im)
    for sx in (-1, 1):
        x, y = cx + sx * gap, cy
        d.ellipse([x - r, y - r * 1.15, x + r, y + r * 1.15], fill=fill)
        hx, hy = x - r * 0.35 + look[0], y - r * 0.45 + look[1]
        d.ellipse([hx - r * 0.33, hy - r * 0.33, hx + r * 0.33, hy + r * 0.33], fill=WHITE)


def smile(im, cx, cy, w, t):
    d = ImageDraw.Draw(im)
    d.arc([cx - w, cy - w * 0.7, cx + w, cy + w * 0.7], 20, 160, fill=INK, width=int(t))


def cheeks(im, cx, cy, gap, r):
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    for sx in (-1, 1):
        d.ellipse([cx + sx * gap - r, cy - r * 0.6, cx + sx * gap + r, cy + r * 0.6], fill=CORAL + (110,))
    im.alpha_composite(lay)


def nubi():
    """Nubi: una pequeña nebulosa (nube de estrellas) de arcilla uva y celeste. Donde nacen las estrellas: cada partida
    hace nacer una en su interior."""
    im = N.night(); cx, cy = S / 2, S / 2 + u(4)
    N.glow(im, cx, cy, u(34), GRAPE, 110)
    puffs = [(-16, 4, 14), (-6, -8, 16), (9, -6, 15), (18, 6, 12), (0, 9, 17), (-19, 13, 9), (14, 15, 10)]
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    for x, y, r in puffs:   # sombra dura (hacia abajo)
        d.ellipse([cx + u(x - r), cy + u(y - r + 2.6), cx + u(x + r), cy + u(y + r + 2.6)], fill=INK)
    for x, y, r in puffs:   # borde tinta
        rr = r + 2.2
        d.ellipse([cx + u(x - rr), cy + u(y - rr), cx + u(x + rr), cy + u(y + rr)], fill=INK)
    for x, y, r in puffs:
        col = (196, 176, 255) if y < 0 else (170, 150, 250)
        d.ellipse([cx + u(x - r), cy + u(y - r), cx + u(x + r), cy + u(y + r)], fill=col)
    im.alpha_composite(lay)
    # nubes internas de color y estrellitas que nacen
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    for x, y, r, c in [(-10, 8, 7, SKY), (12, 10, 6, (255, 150, 200))]:
        d.ellipse([cx + u(x - r), cy + u(y - r), cx + u(x + r), cy + u(y + r)], fill=c + (120,))
    im.alpha_composite(lay)
    for x, y, r in [(-20, 2, 2.2), (19, -2, 1.8), (4, 16, 1.6)]:
        ImageDraw.Draw(im).polygon(N.sparkle(cx + u(x), cy + u(y), u(r * 1.6)), fill=CREAM)
    N.shine(im, cx - u(10), cy - u(14), u(6), u(3), 110)
    eyes(im, cx, cy - u(1), u(7.5), u(3.1))
    smile(im, cx, cy + u(3.5), u(4), u(1.3))
    cheeks(im, cx, cy + u(3), u(12), u(3))
    return im


def pulsi():
    """Pulsi: una estrella joven de arcilla sol que late con luz (un púlsar). Con cada racha, sus anillos de luz crecen."""
    im = N.night(); cx, cy = S / 2, S / 2 + u(2)
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    for r, a in ((34, 60), (29, 95)):
        d.ellipse([cx - u(r), cy - u(r), cx + u(r), cy + u(r)], outline=SUN + (a,), width=int(u(1.6)))
    im.alpha_composite(lay)
    N.glow(im, cx, cy, u(28), SUN, 150)
    body = []
    for i in range(160):     # estrella de 5 puntas muy redondeada (casi una flor)
        a = -math.pi / 2 + 2 * math.pi * i / 160
        k = 0.5 + 0.5 * math.cos(5 * (a + math.pi / 2))
        rr = u(18) + u(5) * k ** 1.3
        body.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))
    N.clay_poly(im, body, SUN, drop=2.8, border=2.4)
    N.shine(im, cx - u(8), cy - u(10), u(5), u(2.6), 140)
    eyes(im, cx, cy - u(1), u(6.5), u(2.9))
    smile(im, cx, cy + u(4), u(4.2), u(1.3))
    cheeks(im, cx, cy + u(3.5), u(11), u(2.8))
    return im


def kibo():
    """Kibo: el tripulante de la estación, un pequeño astronauta de arcilla crema (Kibō = "esperanza" en japonés y el
    nombre del módulo japonés de la Estación Espacial). Viaja contigo por los juegos y cuida tu planeta."""
    im = N.night(); cx, cy = S / 2, S / 2 + u(3)
    N.glow(im, cx, cy, u(30), SKY, 90)
    # mochila y cuerpo
    N.clay_poly(im, [(cx - u(15), cy + u(28)), (cx - u(13), cy + u(12)), (cx + u(13), cy + u(12)), (cx + u(15), cy + u(28))], (220, 226, 245))
    N.clay_disc(im, cx, cy - u(3), u(21), CREAM, drop=3)
    d = ImageDraw.Draw(im)
    vis = [cx - u(15), cy - u(14), cx + u(15), cy + u(8)]
    d.rounded_rectangle([vis[0], vis[1] + u(1.4), vis[2], vis[3] + u(1.4)], radius=u(10), fill=INK)
    d.rounded_rectangle(vis, radius=u(10), fill=(44, 52, 140), outline=INK, width=int(u(2)))
    N.shine(im, cx - u(9), cy - u(10), u(3.5), u(1.8), 150)
    eyes(im, cx, cy - u(3), u(6), u(2.6), fill=SKY)   # ojos de luz en la visera oscura
    ImageDraw.Draw(im).arc([cx - u(3.2), cy + u(2.4) - u(2.2), cx + u(3.2), cy + u(2.4) + u(2.2)], 20, 160, fill=SKY, width=int(u(1.1)))
    d = ImageDraw.Draw(im)
    d.line([(cx + u(12), cy - u(20)), (cx + u(16), cy - u(29))], fill=INK, width=int(u(2.2)))
    N.clay_disc(im, cx + u(16), cy - u(30), u(3.2), LIME, drop=1, border=1.6)
    N.clay_poly(im, N.star(cx - u(24), cy - u(22), u(4.5)), SUN, drop=1, border=1.4)
    return im


CHARS = [
    ('Nubi', nubi, 'Una nebulosa pequeña: la nube donde nacen las estrellas.',
     'Cada partida hace nacer una estrella dentro de Nubi y en tu planeta. Suave, tranquila, muy amigable.'),
    ('Pulsi', pulsi, 'Una estrella joven que late con luz (un púlsar).',
     'Con cada racha sus anillos de luz crecen. Enérgica, alegre: el "pulso" de tu práctica diaria.'),
    ('Kibo', kibo, 'El tripulante de tu estación: un astronauta pequeño.',
     'Kibō es "esperanza" en japonés y el módulo japonés de la Estación Espacial. Viaja contigo por los juegos.'),
]


def sheet():
    rows = len(CHARS); H = dp(80) + rows * dp(230) + dp(20)
    im = P.sky(17).resize((W, int(H))); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(30)), 'El personaje: 3 bocetos', font=F(26), fill=WHITE)
    y = dp(80)
    for name, fn, idea, bio in CHARS:
        ic = fn()
        im.alpha_composite(N.masked(ic, int(dp(150)), 'circle'), (int(dp(18)), int(y)))
        d = ImageDraw.Draw(im)
        d.text((dp(186), y + dp(4)), name, font=F(28), fill=SUN)
        yy = P.wrap(d, dp(186), y + dp(44), idea, F(14), WHITE, W - dp(206))
        P.wrap(d, dp(186), yy + dp(4), bio, F(13, False), DIM, W - dp(206))
        im.alpha_composite(N.masked(ic, int(dp(48)), 'squircle'), (int(dp(186)), int(y + dp(160))))
        d = ImageDraw.Draw(im)
        d.text((dp(244), y + dp(184)), f'{name} · Brain Games', font=F(14), fill=WHITE, anchor='lm')
        d.line([(dp(18), y + dp(222)), (W - dp(18), y + dp(222))], fill=(255, 255, 255, 30), width=int(dp(1)))
        y += dp(230)
        ic.resize((512, 512), Image.LANCZOS).convert('RGB').save(os.path.join(P.OUT, f'personaje-{name.lower()}.png'))
    out = os.path.join(P.OUT, 'personajes.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
