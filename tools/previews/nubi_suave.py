# Nubi "suave" (29-sep, 5.ª vuelta). Ricardo no quedó conforme con las propuestas planas y trajo una lámina de referencia
# (imagen generada): Nubi como la primera versión, pero con otro tono: nube esponjosa de algodón, con volumen suave,
# brillo alrededor, contorno fino morado (no el borde tinta grueso), ojos grandes con dos brillos y rubor; poses
# Enfoque, Explorador (casco), Coach y Súper celebración; e ideas para el planeta. Aquí se redibuja CON NUESTRAS
# HERRAMIENTAS (no se usa la imagen generada: sus derechos no son claros y así todo sale del mismo generador): la nube
# en ese tono, con el color corrido hacia el azul lavanda y toques celestes (más neutro que el lila), sus poses y el
# ícono. Maqueta PIL, NO la app.
# Uso: python3 tools/previews/nubi_suave.py  ->  docs/previews/nubi-suave.png (+ icono-nubi-suave.png)
import os, math, random, importlib.util
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('ni', os.path.join(HERE, 'nombre_icono.py'))
N = importlib.util.module_from_spec(spec); spec.loader.exec_module(N)
P = N.P
F, dp, W, S, u = P.F, P.dp, P.W, N.S, N.u
SUN, CREAM, WHITE, DIM, SKY, LIME, CORAL = P.SUN, P.CREAM, P.WHITE, P.DIM, P.SKY, P.LIME, P.CORAL

LINE = (74, 56, 150)          # contorno fino morado oscuro
BASE = (172, 168, 248)        # azul lavanda (pervinca)
LIGHT = (232, 230, 255)
SHADE = (118, 108, 214)
TINT_C = (140, 210, 255)      # celeste
TINT_V = (196, 160, 255)      # violeta suave
EYE = (32, 26, 78)
EYE_I = (84, 96, 200)
BLUSH = (255, 140, 170)

Y, X = np.mgrid[0:S, 0:S].astype(np.float32)
PUFFS = [(-24, 6, 11), (-19, -8, 12), (-7, -17, 13), (8, -17, 13), (20, -8, 12), (25, 6, 11),
         (16, 16, 12), (0, 19, 13), (-16, 16, 12)]


def smin(a, b, k):
    h = np.clip(0.5 + 0.5 * (b - a) / k, 0, 1)
    return b * (1 - h) + a * h - k * h * (1 - h)


def cloud(x, y):
    d = np.sqrt(x ** 2 + (y - 2) ** 2) - 22
    for px, py, r in PUFFS:
        d = smin(d, np.sqrt((x - px) ** 2 + (y - py) ** 2) - r, 1.6)
    return d


def cover(sdf_px, aa=1.3): return np.clip(0.5 - sdf_px / aa, 0, 1)


def to_img(alpha, rgb):
    lay = np.zeros(alpha.shape + (4,), np.uint8); lay[..., :3] = rgb
    lay[..., 3] = (np.clip(alpha, 0, 1) * 255).astype(np.uint8); return Image.fromarray(lay)


def blur(a, r):
    return np.asarray(Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(r)), np.float32) / 255


def glow(im, cx, cy, r, rgb, a):
    g = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(g).ellipse([cx - r, cy - r, cx + r, cy + r], fill=rgb + (a,))
    im.alpha_composite(g.filter(ImageFilter.GaussianBlur(r * 0.45)))


def sparkle(d, cx, cy, r, col):
    d.polygon(N.sparkle(cx, cy, r), fill=col)


def nubi(im, cx, cy, k, pose='hola', halo=True):
    """Nubi suave. (cx, cy) centro en px, k = px por unidad (la nube mide ~74 de ancho)."""
    x, y = (X - cx) / k, (Y - cy) / k
    d = cloud(x, y)
    inside = cover(d * k)
    if halo:
        im.alpha_composite(to_img(blur(cover((d - 6) * k), 9 * k) * 0.55, (150, 140, 255)))
    # contorno fino
    im.alpha_composite(to_img(cover((d - 1.7) * k), LINE))
    # relleno con volumen: de claro (arriba a la izquierda) a sombra (abajo a la derecha)
    t = np.clip((y + 0.35 * x + 20) / 44, 0, 1)[..., None]
    c0, c1, c2 = (np.array(c, np.float32) for c in (LIGHT, BASE, SHADE))
    col = np.where(t < 0.45, c0 + (c1 - c0) * (t / 0.45), c1 + (c2 - c1) * ((t - 0.45) / 0.55))
    lay = np.zeros((S, S, 4), np.float32); lay[..., :3] = col; lay[..., 3] = inside * 255
    im.alpha_composite(Image.fromarray(lay.astype(np.uint8)))
    # toques de color (celeste abajo a la izquierda, violeta a la derecha): nebulosa
    for bx, by, r, c, a in [(-14, 12, 12, TINT_C, 0.45), (15, 4, 11, TINT_V, 0.35), (4, -12, 8, TINT_C, 0.18)]:
        m = np.clip(1 - np.sqrt((x - bx) ** 2 + (y - by) ** 2) / r, 0, 1)
        im.alpha_composite(to_img(blur(m, 3 * k) * inside * a, c))
    # cada copo con su luz y su sombrita (algodón)
    lights = np.zeros((S, S), np.float32); shades = np.zeros((S, S), np.float32)
    for px, py, r in PUFFS:
        dist = np.sqrt((x - px + 0.3 * r) ** 2 + (y - py + 0.38 * r) ** 2)
        lights = np.maximum(lights, np.clip(1 - dist / (0.62 * r), 0, 1) ** 1.5)
        inner = np.sqrt((x - px) ** 2 + (y - py) ** 2) < r
        up = np.sqrt((x - px + 0.1 * r) ** 2 + (y - py + 0.28 * r) ** 2) < r * 0.95
        shades = np.maximum(shades, (inner & ~up).astype(np.float32))
    im.alpha_composite(to_img(blur(shades, 2.2 * k) * inside * 0.28, SHADE))
    im.alpha_composite(to_img(blur(lights, 1.5 * k) * inside * 0.55, WHITE))
    # grano de algodón muy sutil
    rnd = np.random.default_rng(3).normal(0, 1, (S, S)).astype(np.float32)
    im.alpha_composite(to_img(np.clip(blur(np.clip(rnd * 0.5 + 0.5, 0, 1), 0.8 * k) - 0.5, 0, 1) * inside * 0.35, WHITE))
    # estrellitas que nacen dentro
    dd = ImageDraw.Draw(im)
    for sx, sy, r in [(-22, -5, 2.2), (22, -3, 1.9), (7, -15, 1.5)]:
        sparkle(dd, cx + sx * k, cy + sy * k, r * 1.7 * k, (255, 250, 230))
    face(im, cx, cy, k, pose, inside)


def face(im, cx, cy, k, pose, inside):
    d = ImageDraw.Draw(im)
    ey, gap = cy + 2 * k, 10.5 * k
    rx, ry = 5.0 * k, 6.1 * k
    if pose in ('enfoque', 'celebra'):
        for sx in (-1, 1):
            ex = cx + sx * gap
            if pose == 'enfoque':   # ojos cerrados y tranquilos
                d.arc([ex - rx, ey - ry * 0.7, ex + rx, ey + ry * 0.9], 15, 165, fill=EYE, width=max(2, int(1.6 * k)))
            else:                   # ojos felices
                d.arc([ex - rx, ey - ry * 0.6, ex + rx, ey + ry * 1.2], 195, 345, fill=EYE, width=max(2, int(1.8 * k)))
    else:
        look = (1.2 * k, -0.6 * k) if pose == 'coach' else (0, 0)
        for sx in (-1, 1):
            ex, eyy = cx + sx * gap + look[0], ey + look[1]
            d.ellipse([ex - rx, eyy - ry, ex + rx, eyy + ry], fill=EYE)
            # iris azul abajo
            lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
            ld.ellipse([ex - rx * 0.8, eyy, ex + rx * 0.8, eyy + ry * 0.9], fill=EYE_I + (150,))
            im.alpha_composite(lay.filter(ImageFilter.GaussianBlur(1.2 * k)))
            d = ImageDraw.Draw(im)
            h1 = 2.2 * k; hx, hy = ex - rx * 0.32, eyy - ry * 0.4
            d.ellipse([hx - h1, hy - h1, hx + h1, hy + h1], fill=WHITE)
            h2 = 0.9 * k; hx, hy = ex + rx * 0.35, eyy + ry * 0.42
            d.ellipse([hx - h2, hy - h2, hx + h2, hy + h2], fill=WHITE)
    my = cy + 9 * k
    if pose == 'celebra':
        d.chord([cx - 4.5 * k, my - 3 * k, cx + 4.5 * k, my + 5 * k], 0, 180, fill=EYE)
        d.chord([cx - 2.6 * k, my + 1.4 * k, cx + 2.6 * k, my + 4.6 * k], 180, 360, fill=CORAL)
    else:
        w = 3.2 * k
        d.arc([cx - w, my - w * 0.8, cx + w, my + w * 0.8], 25, 155, fill=EYE, width=max(2, int(1.4 * k)))
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
    for sx in (-1, 1):
        bx, by = cx + sx * 16 * k, cy + 8.5 * k
        ld.ellipse([bx - 4 * k, by - 2.2 * k, bx + 4 * k, by + 2.2 * k], fill=BLUSH + (150,))
    im.alpha_composite(lay.filter(ImageFilter.GaussianBlur(1.3 * k)))


def pose_enfoque(im, cx, cy, k):
    """Enfoque: calma, con anillos de luz que respiran alrededor (para la pantalla de carga o el inicio de sesión)."""
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    for r, a in ((50, 70), (44, 110)):
        d.ellipse([cx - r * k, cy - r * 0.86 * k, cx + r * k, cy + r * 0.86 * k], outline=(170, 200, 255, a), width=int(1.3 * k))
    im.alpha_composite(lay.filter(ImageFilter.GaussianBlur(0.6 * k)))
    nubi(im, cx, cy, k, 'enfoque')
    d = ImageDraw.Draw(im)
    for ang, dist, r in [(-60, 48, 2.2), (200, 47, 1.8), (20, 50, 1.5)]:
        a = math.radians(ang); sparkle(d, cx + math.cos(a) * dist * k, cy + math.sin(a) * dist * 0.86 * k, r * 1.8 * k, (210, 230, 255))


def pose_explorador(im, cx, cy, k):
    """Explorador: dentro de un casco de vidrio (pantalla de carga de los juegos, viajes)."""
    R = 44 * k
    glow(im, cx, cy, R * 1.1, (120, 170, 255), 60)
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    d.ellipse([cx - R, cy - R, cx + R, cy + R], fill=(170, 215, 255, 40))
    im.alpha_composite(lay)
    nubi(im, cx, cy + 2 * k, k * 0.86, 'hola', halo=False)
    d = ImageDraw.Draw(im)
    # cuello del traje
    d.rounded_rectangle([cx - 30 * k, cy + R - 8 * k, cx + 30 * k, cy + R + 4 * k], radius=6 * k, fill=(214, 220, 238), outline=LINE, width=int(1.6 * k))
    d.rounded_rectangle([cx - 8 * k, cy + R - 5 * k, cx + 8 * k, cy + R + 1 * k], radius=2 * k, fill=SKY)
    # borde del casco y reflejos
    d.ellipse([cx - R, cy - R, cx + R, cy + R], outline=LINE, width=int(1.8 * k))
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
    ld.arc([cx - R * 0.86, cy - R * 0.86, cx + R * 0.86, cy + R * 0.86], 200, 250, fill=(255, 255, 255, 190), width=int(3 * k))
    ld.arc([cx - R * 0.86, cy - R * 0.86, cx + R * 0.86, cy + R * 0.86], 258, 268, fill=(255, 255, 255, 160), width=int(3 * k))
    im.alpha_composite(lay)


def pose_celebra(im, cx, cy, k):
    """Súper celebración: estallido sol detrás y chispas de colores (logros, récords, ascensos de liga)."""
    pts = []
    for i in range(24):
        a = -math.pi / 2 + 2 * math.pi * i / 24
        r = (46 if i % 2 == 0 else 34) * k
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    glow(im, cx, cy, 44 * k, SUN, 110)
    d = ImageDraw.Draw(im)
    d.polygon(pts, fill=(255, 214, 90), outline=LINE, width=int(1.6 * k))
    nubi(im, cx, cy, k * 0.92, 'celebra')
    d = ImageDraw.Draw(im)
    for ang, dist, r, c in [(-150, 50, 3, SKY), (-30, 52, 3.2, LIME), (160, 50, 2.6, CORAL), (15, 54, 2.4, WHITE), (-95, 54, 2.8, WHITE)]:
        a = math.radians(ang); sparkle(d, cx + math.cos(a) * dist * k, cy + math.sin(a) * dist * k, r * 1.8 * k, c)


def pose_coach(im, cx, cy, k):
    """Resumen de la semana: Nubi junto a una tarjetita con la línea de tu semana (sin evaluar: muestra y anima)."""
    nubi(im, cx - 10 * k, cy + 4 * k, k * 0.85, 'coach')
    d = ImageDraw.Draw(im)
    x0, y0, w, h = cx + 14 * k, cy - 34 * k, 34 * k, 40 * k
    d.rounded_rectangle([x0, y0 + 1.6 * k, x0 + w, y0 + h + 1.6 * k], radius=5 * k, fill=LINE)
    d.rounded_rectangle([x0, y0, x0 + w, y0 + h], radius=5 * k, fill=(250, 248, 255), outline=LINE, width=int(1.4 * k))
    vals = [0.3, 0.45, 0.4, 0.6, 0.72]
    pts = [(x0 + 6 * k + i * (w - 12 * k) / 4, y0 + h - 8 * k - v * (h - 18 * k)) for i, v in enumerate(vals)]
    d.line(pts, fill=SKY, width=int(2.2 * k), joint='curve')
    for px, py in pts: d.ellipse([px - 1.6 * k, py - 1.6 * k, px + 1.6 * k, py + 1.6 * k], fill=SKY)
    px, py = pts[-1]; sparkle(d, px, py - 5 * k, 3.4 * k, SUN)
    for i in range(2):
        d.rounded_rectangle([x0 + 6 * k, y0 + 5 * k + i * 4.5 * k, x0 + (22 - i * 6) * k, y0 + 7.4 * k + i * 4.5 * k], radius=1.2 * k, fill=(200, 196, 235))


POSES = [('Saluda', lambda im, cx, cy, k: nubi(im, cx, cy, k, 'hola')), ('Enfoque', pose_enfoque),
         ('Explorador', pose_explorador), ('Celebra', pose_celebra), ('Tu semana', pose_coach)]


def night_icon():
    t = np.linspace(1, 0, S)[:, None, None]
    top, bot = np.array([40, 44, 130.]), np.array([12, 14, 52.])
    bg = np.repeat(bot + (top - bot) * t, S, axis=1)
    im = Image.fromarray(bg.clip(0, 255).astype(np.uint8)).convert('RGBA')
    d = ImageDraw.Draw(im); rnd = random.Random(8)
    for _ in range(12):
        x0, y0 = rnd.uniform(u(18), S - u(18)), rnd.uniform(u(18), S - u(18))
        if abs(x0 - S / 2) < u(34) and abs(y0 - S / 2) < u(30): continue
        r = u(0.5 + rnd.random() * 0.5); d.ellipse([x0 - r, y0 - r, x0 + r, y0 + r], fill=(255, 255, 255, 200))
    return im


def icon_round():
    im = night_icon(); nubi(im, S / 2, S / 2 + u(1), u(0.78)); return im


def icon_explorer():
    im = night_icon(); pose_explorador(im, S / 2, S / 2 - u(2), u(0.68)); return im


def icon_mono():
    """Temático: silueta de un color con la cara calada."""
    k = u(0.78); cx, cy = S / 2, S / 2 + u(1)
    x, y = (X - cx) / k, (Y - cy) / k
    im = to_img(cover(cloud(x, y) * k), (255, 255, 255))
    d = ImageDraw.Draw(im)
    for sx in (-1, 1):
        ex, ey = cx + sx * 10.5 * k, cy + 2 * k
        d.ellipse([ex - 5.0 * k, ey - 6.1 * k, ex + 5.0 * k, ey + 6.1 * k], fill=(0, 0, 0, 0))
    w, my = 3.4 * k, cy + 9 * k
    d.arc([cx - w, my - w * 0.8, cx + w, my + w * 0.8], 25, 155, fill=(0, 0, 0, 0), width=int(1.8 * k))
    return im


def wallpaper(w, h, dark):
    t = np.linspace(0, 1, int(h))[:, None, None]
    a, b = (np.array([20, 24, 40.]), np.array([48, 40, 70.])) if dark else (np.array([236, 226, 214.]), np.array([196, 214, 232.]))
    return Image.fromarray(np.repeat(a + (b - a) * t, int(w), axis=1).astype(np.uint8)).convert('RGBA')


def render(fn, size, scale=1.0):
    c = Image.new('RGBA', (S, S), (0, 0, 0, 0)); fn(c, S / 2, S / 2, u(0.95) * scale)
    return c.resize((size, size), Image.LANCZOS)


def sheet():
    H = dp(1000)
    im = P.sky(41).resize((W, int(H))); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(28)), 'Nubi suave', font=F(26), fill=SUN)
    P.wrap(d, dp(20), dp(64), 'El tono de tu referencia, redibujado con nuestras herramientas: nube de algodón con volumen, '
           'contorno fino, brillo alrededor y ojos grandes. Color azul lavanda con toques celestes.', F(13, False), DIM, W - dp(40))
    # grande
    im.alpha_composite(render(lambda c, x, y, k: nubi(c, x, y, k, 'hola'), int(dp(250))), (int(W / 2 - dp(125)), int(dp(116))))
    # poses
    y = dp(376)
    d = ImageDraw.Draw(im)
    d.text((dp(20), y), 'Sus momentos', font=F(17), fill=SUN)
    cw = (W - dp(24)) / 4
    for i, (name, fn) in enumerate(POSES[1:]):
        x = dp(12) + cw * i + cw / 2
        im.alpha_composite(render(fn, int(dp(104)), 0.9), (int(x - dp(52)), int(y + dp(30))))
        d = ImageDraw.Draw(im)
        d.text((x, y + dp(138)), name, font=F(14), fill=WHITE, anchor='ma')
    notes = ['al empezar', 'pantalla de carga', 'logros y récords', 'resumen semanal']
    for i, t in enumerate(notes):
        d.text((dp(12) + cw * i + cw / 2, y + dp(160)), t, font=F(12, False), fill=DIM, anchor='ma')
    # íconos
    y = dp(580)
    d.text((dp(20), y), 'El ícono', font=F(17), fill=SUN)
    ics = [(icon_round(), 'circle', 'redondo'), (icon_explorer(), 'squircle', 'con casco')]
    for i, (ic, shape, lab) in enumerate(ics):
        im.alpha_composite(N.masked(ic, int(dp(84)), shape), (int(dp(20) + i * dp(104)), int(y + dp(30))))
        d = ImageDraw.Draw(im); d.text((dp(62) + i * dp(104), y + dp(122)), lab, font=F(12, False), fill=DIM, anchor='ma')
    mono = icon_mono()
    base = Image.new('RGBA', (S, S), (44, 48, 90, 255)); tint = Image.new('RGBA', (S, S), (214, 210, 255, 255))
    tint.putalpha(mono.getchannel('A')); base.alpha_composite(tint)
    im.alpha_composite(N.masked(base, int(dp(84)), 'circle'), (int(dp(228)), int(y + dp(30))))
    d = ImageDraw.Draw(im); d.text((dp(270), y + dp(122)), 'monocromo', font=F(12, False), fill=DIM, anchor='ma')
    d.text((dp(330), y + dp(56)), 'Nubi', font=F(22), fill=WHITE)
    d.text((dp(330), y + dp(84)), 'Brain Games', font=F(13, False), fill=DIM)
    # tamaño real
    y = dp(740)
    d.text((dp(20), y), 'En tu teléfono, tamaño real', font=F(17), fill=SUN)
    y += dp(30)
    for j, dark in enumerate((True, False)):
        wx = dp(20) + j * (W - dp(40)) / 2
        ww = (W - dp(52)) / 2
        wp = wallpaper(ww, dp(112), dark)
        m = Image.new('L', wp.size, 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, wp.size[0] - 1, wp.size[1] - 1], radius=dp(16), fill=255)
        im.paste(wp, (int(wx + j * dp(12)), int(y)), m)
        for i, (ic, shape, lab) in enumerate(ics):
            x = wx + j * dp(12) + ww * (i + 0.5) / 2
            im.alpha_composite(N.masked(ic, int(dp(48)), 'circle' if dark else 'squircle'), (int(x - dp(24)), int(y + dp(18))))
            d = ImageDraw.Draw(im)
            d.text((x, y + dp(74)), 'Nubi', font=F(13, False), fill=WHITE if dark else (30, 30, 40), anchor='ma')
    y += dp(130)
    P.wrap(d, dp(20), y, 'Nubi nunca se pone triste ni reacciona a cómo te fue: saluda, acompaña y celebra. En "Tu semana" '
           'muestra y anima, no evalúa.', F(13, False), DIM, W - dp(40))
    out = os.path.join(P.OUT, 'nubi-suave.png'); im.convert('RGB').save(out); print(out)
    icon_round().resize((512, 512), Image.LANCZOS).convert('RGB').save(os.path.join(P.OUT, 'icono-nubi-suave.png'))


if __name__ == '__main__':
    sheet()
