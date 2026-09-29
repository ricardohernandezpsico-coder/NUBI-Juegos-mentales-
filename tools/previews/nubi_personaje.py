# Hoja del personaje Nubi (29-sep, Ricardo eligió Nubi como nombre y personaje de la app). Maqueta PIL, NO la app.
# Nubi = una nebulosa pequeña de arcilla: la nube donde nacen las estrellas. Arriba sus poses (saluda, celebra, piensa,
# duerme; NO hay pose triste: nunca reacciona mal a cómo le va a la persona), abajo dónde aparece en la app (Hoy,
# bienvenida, logro, notificación) y el ícono (redondo, cuadrado redondeado y monocromo de Android 13).
# Uso: python3 tools/previews/nubi_personaje.py  ->  docs/previews/nubi-personaje.png
import os, math, random, importlib.util
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('ni', os.path.join(HERE, 'nombre_icono.py'))
N = importlib.util.module_from_spec(spec); spec.loader.exec_module(N)
P = N.P
F, dp, W = P.F, P.dp, P.W
INK, SUN, CREAM, WHITE, DIM, SOFT = P.INK, P.SUN, P.CREAM, P.WHITE, P.DIM, P.SOFT
LIME, CORAL, SKY, GRAPE = P.LIME, P.CORAL, P.SKY, P.GRAPE
LILAC, LILAC_D, PINK = (196, 176, 255), (170, 150, 250), (255, 150, 200)

PUFFS = [(-16, 4, 14), (-6, -8, 16), (9, -6, 15), (18, 6, 12), (0, 9, 17), (-19, 13, 9), (14, 15, 10)]


def glow(im, cx, cy, r, rgb, a):
    g = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(g).ellipse([cx - r, cy - r, cx + r, cy + r], fill=rgb + (a,))
    im.alpha_composite(g.filter(ImageFilter.GaussianBlur(r * 0.45)))


def sparkle(d, cx, cy, r, col=CREAM):
    d.polygon(N.sparkle(cx, cy, r), fill=col)


def nubi(im, cx, cy, k, pose='hola', mono=False):
    """Dibuja a Nubi con centro (cx, cy) y escala k (1 = 1 unidad por px del boceto; el cuerpo mide ~76 unidades)."""
    def U(v): return v * k
    body = INK if mono else None
    if not mono:
        glow(im, cx, cy, U(42), GRAPE, 90)
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    puffs = list(PUFFS)
    if pose == 'hola':
        puffs.append((27, -8, 7))          # "bracito" que saluda
    if pose == 'celebra':
        puffs += [(-25, -8, 7), (25, -8, 7)]  # los dos arriba
    if not mono:
        for x, y, r in puffs:
            d.ellipse([cx + U(x - r), cy + U(y - r + 2.6), cx + U(x + r), cy + U(y + r + 2.6)], fill=INK)
        for x, y, r in puffs:
            rr = r + 2.2
            d.ellipse([cx + U(x - rr), cy + U(y - rr), cx + U(x + rr), cy + U(y + rr)], fill=INK)
    for x, y, r in puffs:
        col = (255, 255, 255) if mono else (LILAC if y < 0 else LILAC_D)
        d.ellipse([cx + U(x - r), cy + U(y - r), cx + U(x + r), cy + U(y + r)], fill=col)
    im.alpha_composite(lay)
    if mono:   # silueta de un color con los ojos calados (así se lee la cara en el ícono temático y en la notificación)
        d = ImageDraw.Draw(im)
        for sx in (-1, 1):
            x, y, r = cx + sx * U(7.5), cy - U(1), U(3.1)
            d.ellipse([x - r, y - r * 1.15, x + r, y + r * 1.15], fill=(0, 0, 0, 0))
        d.arc([cx - U(4), cy + U(0.7), cx + U(4), cy + U(6.3)], 20, 160, fill=(0, 0, 0, 0), width=max(2, int(U(1.3))))
        return
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    for x, y, r, c in [(-10, 8, 7, SKY), (12, 10, 6, PINK)]:
        d.ellipse([cx + U(x - r), cy + U(y - r), cx + U(x + r), cy + U(y + r)], fill=c + (120,))
    im.alpha_composite(lay)
    d = ImageDraw.Draw(im)
    for x, y, r in [(-20, 2, 2.2), (19, -2, 1.8), (4, 16, 1.6)]:
        sparkle(d, cx + U(x), cy + U(y), U(r * 1.6))
    N.shine(im, cx - U(10), cy - U(14), U(6), U(3), 110)
    d = ImageDraw.Draw(im)
    ey, gap, r = cy - U(1), U(7.5), U(3.1)
    if pose == 'duerme':
        for sx in (-1, 1):
            d.arc([cx + sx * gap - r, ey - r * 0.6, cx + sx * gap + r, ey + r * 0.9], 10, 170, fill=INK, width=max(2, int(U(1.2))))
    elif pose == 'celebra':
        for sx in (-1, 1):   # ojos felices (arcos hacia arriba)
            d.arc([cx + sx * gap - r, ey - r, cx + sx * gap + r, ey + r * 1.2], 190, 350, fill=INK, width=max(2, int(U(1.3))))
    else:
        look = (U(0.8), U(-1.2)) if pose == 'piensa' else (0, 0)
        for sx in (-1, 1):
            x = cx + sx * gap + look[0]
            d.ellipse([x - r, ey - r * 1.15 + look[1], x + r, ey + r * 1.15 + look[1]], fill=INK)
            hx, hy = x - r * 0.35, ey - r * 0.45 + look[1]
            d.ellipse([hx - r * 0.33, hy - r * 0.33, hx + r * 0.33, hy + r * 0.33], fill=WHITE)
    my = cy + U(3.5)
    if pose == 'celebra':
        d.chord([cx - U(4.5), my - U(3), cx + U(4.5), my + U(4)], 0, 180, fill=INK)
    elif pose == 'piensa':
        d.ellipse([cx - U(1.6), my - U(0.4), cx + U(1.6), my + U(2.4)], fill=INK)
    else:
        d.arc([cx - U(4), my - U(2.8), cx + U(4), my + U(2.8)], 20, 160, fill=INK, width=max(2, int(U(1.3))))
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
    for sx in (-1, 1):
        ld.ellipse([cx + sx * U(12) - U(3), cy + U(3) - U(1.8), cx + sx * U(12) + U(3), cy + U(3) + U(1.8)], fill=CORAL + (110,))
    im.alpha_composite(lay)
    d = ImageDraw.Draw(im)
    if pose == 'celebra':
        for ang, dist, rr, col in [(-140, 44, 4, SUN), (-40, 46, 5, SUN), (-90, 50, 3.5, CREAM), (-165, 36, 3, LIME), (-15, 38, 3, SKY)]:
            a = math.radians(ang)
            sparkle(d, cx + math.cos(a) * U(dist), cy + math.sin(a) * U(dist), U(rr), col)
    if pose == 'piensa':
        for i, (x, y, rr) in enumerate([(22, -22, 2), (27, -29, 3), (33, -38, 4.5)]):
            d.ellipse([cx + U(x - rr), cy + U(y - rr), cx + U(x + rr), cy + U(y + rr)], fill=CREAM, outline=INK, width=max(1, int(U(0.8))))
        sparkle(d, cx + U(33), cy + U(-38), U(3), SUN)
    if pose == 'duerme':
        for i, (x, y, s) in enumerate([(22, -20, 7), (30, -30, 9)]):
            d.text((cx + U(x), cy + U(y)), 'z', font=F(s * k / 2.2 * 2, True), fill=CREAM, anchor='mm')


def panel(w, h, seed):
    im = P.sky(seed).resize((int(w), int(h)))
    return im


def sheet():
    Wd = W; H = dp(1110)
    im = P.sky(23).resize((Wd, int(H))); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(34)), 'Nubi', font=F(34), fill=SUN)
    d.text((dp(20), dp(78)), 'La nebulosa donde nacen las estrellas', font=F(16), fill=WHITE)
    P.wrap(d, dp(20), dp(104), 'Vive en tu planeta. Cada partida hace nacer una estrella. Acompaña y celebra; nunca se pone triste por cómo te fue.', F(13, False), DIM, Wd - dp(40))

    # 1. Poses
    y0 = dp(160); cw = (Wd - dp(40)) / 4
    for i, (pose, label) in enumerate([('hola', 'Saluda'), ('celebra', 'Celebra'), ('piensa', 'Piensa'), ('duerme', 'Descansa')]):
        cx = dp(20) + cw * i + cw / 2
        nubi(im, cx, y0 + dp(62), dp(1.25), pose)
        d = ImageDraw.Draw(im)
        d.text((cx, y0 + dp(136)), label, font=F(14), fill=WHITE, anchor='ma')
    d.line([(dp(20), y0 + dp(168)), (Wd - dp(20), y0 + dp(168))], fill=(255, 255, 255, 30), width=int(dp(1)))

    # 2. En Hoy: junto al planeta, con las estrellas que nacieron hoy
    y = y0 + dp(186)
    d.text((dp(20), y), 'En Hoy', font=F(17), fill=SUN)
    d.text((dp(20), y + dp(24)), 'Flota junto a tu planeta: cada partida hace nacer una estrella.', font=F(13, False), fill=DIM)
    pcx, pcy, pr = Wd / 2 - dp(40), y + dp(150), dp(70)
    P.blob_planet(im, pcx, pcy, pr)
    nubi(im, pcx + dp(118), pcy - dp(40), dp(0.95), 'hola')
    d = ImageDraw.Draw(im)
    for i, (x, yy) in enumerate([(-60, -95), (40, -100), (95, 30)]):
        sparkle(d, pcx + dp(x), pcy + dp(yy), dp(7), SUN)
    d.text((Wd / 2, pcy + pr + dp(24)), 'Hoy nacieron 3 estrellas', font=F(15), fill=WHITE, anchor='ma')

    # 3. Bienvenida y logro, lado a lado
    y = pcy + pr + dp(64)
    half = (Wd - dp(52)) / 2
    for j, (title, sub, pose) in enumerate([('Hola, soy Nubi', 'Te acompaño en tus juegos para la mente.', 'hola'),
                                           ('¡Logro nuevo!', 'Racha de 7 días', 'celebra')]):
        x0 = dp(20) + j * (half + dp(12))
        d.text((x0, y), ['En la bienvenida', 'En los logros'][j], font=F(15), fill=SUN)
        nubi(im, x0 + half / 2, y + dp(80), dp(0.95), pose)
        d = ImageDraw.Draw(im)
        d.text((x0 + half / 2, y + dp(140)), title, font=F(16), fill=WHITE, anchor='ma')
        P.wrap(d, x0 + half / 2, y + dp(164), sub, F(13, False), DIM, half - dp(8), anchor='center')

    # 4. Notificación
    y += dp(214)
    d.text((dp(20), y), 'En los recordatorios', font=F(15), fill=SUN)
    ny = y + dp(28)
    d.rounded_rectangle([dp(20), ny, Wd - dp(20), ny + dp(74)], radius=dp(16), fill=(236, 238, 246))
    ic = Image.new('RGBA', (int(dp(40)), int(dp(40))), (0, 0, 0, 0))
    bg = Image.new('RGBA', im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse([dp(34), ny + dp(16), dp(74), ny + dp(56)], fill=(90, 70, 190))
    fg = Image.new('RGBA', im.size, (0, 0, 0, 0))
    nubi(fg, dp(54), ny + dp(37), dp(0.36), 'hola', mono=True)
    im.alpha_composite(fg)
    d = ImageDraw.Draw(im)
    d.text((dp(86), ny + dp(14)), 'Nubi', font=F(14), fill=(40, 40, 60))
    d.text((dp(86), ny + dp(36)), 'Hoy puede nacer otra estrella en tu planeta.', font=F(13, False), fill=(70, 70, 90))

    # 5. Ícono
    y = ny + dp(96)
    d.text((dp(20), y), 'El ícono', font=F(15), fill=SUN)
    icon = N.night()
    nubi(icon, N.S / 2, N.S / 2 + N.u(4), N.u(1.0), 'hola')
    for i, shape in enumerate(['circle', 'squircle']):
        im.alpha_composite(N.masked(icon, int(dp(72)), shape), (int(dp(20) + i * dp(92)), int(y + dp(28))))
    mono = Image.new('RGBA', (N.S, N.S), (40, 44, 70, 255))
    fg = Image.new('RGBA', (N.S, N.S), (0, 0, 0, 0))
    nubi(fg, N.S / 2, N.S / 2 + N.u(4), N.u(0.8), 'hola', mono=True)
    mono.alpha_composite(fg)
    im.alpha_composite(N.masked(mono, int(dp(72)), 'circle'), (int(dp(204)), int(y + dp(28))))
    d = ImageDraw.Draw(im)
    for i, t in enumerate(['redondo', 'cuadrado', 'monocromo']):
        d.text((dp(56) + i * dp(92), y + dp(106)), t, font=F(12, False), fill=DIM, anchor='ma')
    d.text((dp(300), y + dp(52)), 'Nubi', font=F(22), fill=WHITE)
    d.text((dp(300), y + dp(80)), 'Brain Games', font=F(14, False), fill=DIM)

    out = os.path.join(P.OUT, 'nubi-personaje.png'); im.convert('RGB').save(out); print(out)
    icon.resize((512, 512), Image.LANCZOS).convert('RGB').save(os.path.join(P.OUT, 'icono-nubi.png'))


if __name__ == '__main__':
    sheet()
