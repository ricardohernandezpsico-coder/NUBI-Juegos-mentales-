# Ícono de Nubi, segunda vuelta (29-sep, Ricardo aprobó el personaje pero "el ícono no me gusta del todo, siento que no se
# ajusta a la perfección"). Maqueta PIL, NO la app. Qué se corrige del primero: la silueta era un racimo de bolitas
# irregular con un "bracito" que parecía un bulto, las nubes internas se veían como manchas, los destellos cruzaban la
# cara y Nubi quedaba chica y corrida dentro del círculo. Aquí la silueta es una sola forma (campo de distancia con
# uniones suaves, como arcilla), simétrica, centrada y del tamaño de la zona segura de Android; la nebulosa interior va
# difuminada y la cara es más grande. Cuatro variantes, en grande y en tamaño real en la pantalla de inicio.
# Uso: python3 tools/previews/nubi_iconos.py  ->  docs/previews/nubi-iconos.png (+ icono-nubi-<letra>.png)
import os, math, random, importlib.util
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('ni', os.path.join(HERE, 'nombre_icono.py'))
N = importlib.util.module_from_spec(spec); spec.loader.exec_module(N)
spec = importlib.util.spec_from_file_location('np_', os.path.join(HERE, 'nubi_personaje.py'))
NP = importlib.util.module_from_spec(spec); spec.loader.exec_module(NP)
P = N.P
F, dp, W, S, u = P.F, P.dp, P.W, N.S, N.u
INK, SUN, CREAM, WHITE, DIM = P.INK, P.SUN, P.CREAM, P.WHITE, P.DIM
CORAL, SKY, GRAPE = P.CORAL, P.SKY, P.GRAPE
LILAC_T, LILAC, LILAC_D, PINK = (214, 200, 255), (190, 170, 255), (150, 126, 240), (255, 150, 200)

Y, X = np.mgrid[0:S, 0:S].astype(np.float32)


def smin(a, b, k):
    """Unión suave de dos campos de distancia (las bolitas se funden como arcilla, sin muescas)."""
    h = np.clip(0.5 + 0.5 * (b - a) / k, 0, 1)
    return b * (1 - h) + a * h - k * h * (1 - h)


def cloud_sdf(cx, cy, k, dy=0.0):
    """Silueta de Nubi (unidades del ícono de 108; k = escala): base redonda y tres copos, simétrica."""
    x = (X - cx) / k
    y = (Y - cy - dy) / k
    # base: cápsula horizontal
    bx = np.maximum(np.abs(x) - 17, 0)
    d = np.sqrt(bx ** 2 + (y - 8) ** 2) - 9.5
    for px, py, r in [(-15, -1, 11), (0, -9, 15.5), (15, -1, 11)]:
        d = smin(d, np.sqrt((x - px) ** 2 + (y - py) ** 2) - r, 3.5)
    return d * k


def cover(sdf, px=1.2):
    """Cobertura con borde suave (antialias) a partir de la distancia en píxeles."""
    return np.clip(0.5 - sdf / px, 0, 1)


def paint(im, alpha, rgb):
    lay = np.zeros((S, S, 4), np.uint8)
    lay[..., :3] = rgb
    lay[..., 3] = (alpha * 255).astype(np.uint8)
    im.alpha_composite(Image.fromarray(lay))


def soft_blob(cx, cy, r, blur):
    g = Image.new('L', (S, S), 0)
    ImageDraw.Draw(g).ellipse([cx - r, cy - r, cx + r, cy + r], fill=255)
    return np.asarray(g.filter(ImageFilter.GaussianBlur(blur)), np.float32) / 255


def nubi_icon(im, cx, cy, k, pal=(LILAC_T, LILAC, LILAC_D), stars=True, face=1.0, glow_rgb=GRAPE):
    """Nubi para el ícono: centro (cx, cy) en px, k = px por unidad del ícono de 108."""
    NP.glow(im, cx, cy, 44 * k, glow_rgb, 80)
    border, drop = 2.3 * k, 2.8 * k
    body = cloud_sdf(cx, cy, k)
    paint(im, cover(cloud_sdf(cx, cy, k, dy=drop) - border), INK)      # sombra dura hacia abajo
    paint(im, cover(body - border), INK)                               # borde tinta
    inside = cover(body)
    # relleno: degradé vertical de arriba (claro) a abajo (más hondo)
    top, bot = cy - 25 * k, cy + 18 * k
    t = np.clip((Y - top) / (bot - top), 0, 1)[..., None]
    c0, c1, c2 = (np.array(c, np.float32) for c in pal)
    col = np.where(t < 0.5, c0 + (c1 - c0) * (t / 0.5), c1 + (c2 - c1) * ((t - 0.5) / 0.5))
    lay = np.zeros((S, S, 4), np.float32); lay[..., :3] = col; lay[..., 3] = inside * 255
    im.alpha_composite(Image.fromarray(lay.astype(np.uint8)))
    # volumen de arcilla: canto de abajo más oscuro, canto de arriba con luz
    rim_dark = inside * (1 - cover(cloud_sdf(cx, cy, k, dy=-3.2 * k)))
    paint(im, rim_dark * 0.35, (90, 60, 190))
    rim_light = inside * (1 - cover(cloud_sdf(cx, cy, k, dy=2.4 * k)))
    paint(im, rim_light * 0.45, (255, 255, 255))
    # nebulosa interior: nubes de color difuminadas (no manchas) abajo a los lados
    for bx, by, r, c, a in [(-15, 9, 9, SKY, 0.55), (15, 9, 8, PINK, 0.55), (0, 14, 7, (140, 120, 255), 0.35)]:
        paint(im, soft_blob(cx + bx * k, cy + by * k, r * k, 4.5 * k) * inside * a, c)
    # brillo grande arriba a la izquierda
    N.shine(im, cx - 8 * k, cy - 16 * k, 6.5 * k, 3 * k, 120)
    d = ImageDraw.Draw(im)
    if stars:   # estrellitas que nacen, lejos de la cara
        for sx, sy, r in [(-20, 5, 2.4), (21, 3, 1.9)]:
            d.polygon(N.sparkle(cx + sx * k, cy + sy * k, r * 1.7 * k), fill=CREAM)
    # cara
    f = face
    ey, gap, r = cy + 1.2 * k, 8.2 * k * f, 3.5 * k * f
    for sx in (-1, 1):
        x = cx + sx * gap
        d.ellipse([x - r, ey - r * 1.18, x + r, ey + r * 1.18], fill=INK)
        hx, hy = x - r * 0.3, ey - r * 0.45
        d.ellipse([hx - r * 0.36, hy - r * 0.36, hx + r * 0.36, hy + r * 0.36], fill=WHITE)
        d.ellipse([x + r * 0.2, ey + r * 0.35, x + r * 0.42, ey + r * 0.57], fill=(255, 255, 255, 200))
    my = cy + 6.4 * k * (1 + (f - 1) * 0.6)
    w = 4.2 * k * f
    d.arc([cx - w, my - w * 0.75, cx + w, my + w * 0.75], 25, 155, fill=INK, width=max(2, int(1.5 * k * f)))
    cheeks = np.zeros((S, S), np.float32)
    for sx in (-1, 1):
        cheeks = np.maximum(cheeks, soft_blob(cx + sx * 13.2 * k * f, cy + 6.3 * k * f, 3.1 * k * f, 1.2 * k))
    paint(im, cheeks * inside * 0.6, CORAL)


def star_born(im, x, y, k, r=6.0):
    """La estrella que acaba de nacer: destello sol con halo y dos chispitas."""
    NP.glow(im, x, y, r * 2.6 * k, SUN, 150)
    d = ImageDraw.Draw(im)
    pts = N.sparkle(x, y + 1.2 * k, r * k)
    d.polygon(pts, fill=INK)
    d.polygon(N.sparkle(x, y, r * k * 1.18), fill=INK)
    d.polygon(N.sparkle(x, y, r * k), fill=SUN)
    d.polygon(N.sparkle(x - r * 0.18 * k, y - r * 0.2 * k, r * k * 0.38), fill=(255, 236, 170))
    for sx, sy, rr in [(-8.5, 5.5, 1.6), (6.5, 8, 1.2)]:
        d.polygon(N.sparkle(x + sx * k, y + sy * k, rr * 1.6 * k), fill=CREAM)


def night_plain():
    """Cielo del ícono con menos estrellas (a 48 dp muchas estrellas ensucian)."""
    t = np.linspace(1, 0, S)[:, None, None]
    top, bot = np.array([30, 36, 118.]), np.array([8, 10, 42.])
    bg = np.repeat(bot + (top - bot) * t, S, axis=1)
    d = np.clip(1 - np.sqrt((X - 0.5 * S) ** 2 + (Y - 0.5 * S) ** 2) / (0.55 * S), 0, 1) ** 2 * 0.35
    bg = bg * (1 - d[..., None]) + np.array([70, 60, 170.]) * d[..., None]
    im = Image.fromarray(bg.clip(0, 255).astype(np.uint8)).convert('RGBA')
    dr = ImageDraw.Draw(im); rnd = random.Random(11)
    for _ in range(14):
        x, y, r = rnd.random() * S, rnd.random() * S, u(0.45 + rnd.random() * 0.6)
        if abs(x - S / 2) < u(30) and abs(y - S / 2) < u(26): continue
        dr.ellipse([x - r, y - r, x + r, y + r], fill=(255, 255, 255, int(110 + rnd.random() * 110)))
    return im


def grape_bg():
    """Fondo uva luminoso: resalta entre otros íconos, sigue siendo "noche"."""
    R = np.sqrt((X - 0.5 * S) ** 2 + (Y - 0.42 * S) ** 2) / (0.75 * S)
    c0, c1 = np.array([128, 96, 236.]), np.array([46, 30, 128.])
    t = np.clip(R, 0, 1)[..., None] ** 1.2
    bg = c0 + (c1 - c0) * t
    im = Image.fromarray(bg.clip(0, 255).astype(np.uint8)).convert('RGBA')
    dr = ImageDraw.Draw(im); rnd = random.Random(5)
    for _ in range(10):
        x, y, r = rnd.random() * S, rnd.random() * S, u(0.5 + rnd.random() * 0.6)
        if abs(x - S / 2) < u(30) and abs(y - S / 2) < u(26): continue
        dr.ellipse([x - r, y - r, x + r, y + r], fill=(255, 255, 255, 170))
    return im


C = S / 2


def icon_a():
    im = night_plain(); nubi_icon(im, C, C + u(3), u(1.0)); return im


def icon_b():
    im = night_plain(); nubi_icon(im, C - u(1), C + u(6), u(0.92), stars=False); star_born(im, C + u(19), C - u(20), u(1.0)); return im


def icon_c():
    im = night_plain(); nubi_icon(im, C, C + u(16), u(1.45), stars=False); return im


def icon_d():
    im = grape_bg()
    nubi_icon(im, C - u(1), C + u(6), u(0.92), pal=((238, 230, 255), (212, 196, 255), (172, 150, 250)), glow_rgb=(220, 200, 255), stars=False)
    star_born(im, C + u(19), C - u(20), u(1.0)); return im


def mono(fn_center):
    """Ícono temático de Android 13: silueta de un color con la cara calada."""
    cx, cy, k = fn_center
    m = cover(cloud_sdf(cx, cy, k))
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0)); paint(im, m, (255, 255, 255))
    d = ImageDraw.Draw(im)
    ey, gap, r = cy + 1.2 * k, 8.2 * k, 3.5 * k
    for sx in (-1, 1):
        x = cx + sx * gap
        d.ellipse([x - r, ey - r * 1.18, x + r, ey + r * 1.18], fill=(0, 0, 0, 0))
    w, my = 4.2 * k, cy + 6.4 * k
    d.arc([cx - w, my - w * 0.75, cx + w, my + w * 0.75], 25, 155, fill=(0, 0, 0, 0), width=int(1.8 * k))
    d.polygon(N.sparkle(cx + 19 * k, cy - 26 * k, 6 * k), fill=(255, 255, 255))
    return im


def old_icon():
    icon = N.night(); NP.nubi(icon, S / 2, S / 2 + u(4), u(1.0), 'hola'); return icon


VARIANTS = [
    ('A', 'Limpio', icon_a, 'Nubi sola, simétrica y centrada, del tamaño justo de la zona segura.'),
    ('B', 'Nace una estrella', icon_b, 'Nubi con la estrella sol que acaba de nacer: cuenta la idea de la app.'),
    ('C', 'Primer plano', icon_c, 'La cara bien grande, cortada por el borde: se reconoce de lejos.'),
    ('D', 'Fondo uva', icon_d, 'Como B, sobre uva luminoso: resalta más entre otros íconos.'),
]


def wallpaper(w, h, dark):
    t = np.linspace(0, 1, int(h))[:, None, None]
    if dark: a, b = np.array([20, 24, 40.]), np.array([48, 40, 70.])
    else: a, b = np.array([236, 226, 214.]), np.array([196, 214, 232.])
    bg = np.repeat(a + (b - a) * t, int(w), axis=1)
    return Image.fromarray(bg.astype(np.uint8)).convert('RGBA')


def sheet():
    icons = [(l, n, fn(), txt) for l, n, fn, txt in VARIANTS]
    H = dp(90) + len(icons) * dp(150) + dp(540)
    im = P.sky(29).resize((W, int(H))); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(30)), 'Ícono de Nubi · segunda vuelta', font=F(24), fill=SUN)
    P.wrap(d, dp(20), dp(64), 'Silueta de una sola pieza, simétrica y centrada; nebulosa difuminada; cara más grande.',
           F(13, False), DIM, W - dp(40))
    y = dp(100)
    for letter, name, ic, txt in icons:
        im.alpha_composite(N.masked(ic, int(dp(120)), 'circle'), (int(dp(20)), int(y)))
        im.alpha_composite(N.masked(ic, int(dp(72)), 'squircle'), (int(dp(152)), int(y + dp(24))))
        d = ImageDraw.Draw(im)
        d.text((dp(240), y + dp(18)), f'{letter} · {name}', font=F(18), fill=WHITE)
        P.wrap(d, dp(240), y + dp(48), txt, F(13, False), DIM, W - dp(260))
        y += dp(150)
        ic.resize((512, 512), Image.LANCZOS).convert('RGB').save(os.path.join(P.OUT, f'icono-nubi-{letter.lower()}.png'))

    # En la pantalla de inicio, tamaño real (48 dp), sobre fondo oscuro y claro; primero el ícono anterior
    d.text((dp(20), y + dp(4)), 'En tu teléfono, tamaño real', font=F(17), fill=SUN)
    y += dp(36)
    row = [('antes', old_icon())] + [(l, ic) for l, _, ic, _ in icons]
    for dark in (True, False):
        wp = wallpaper(W - dp(40), dp(120), dark)
        m = Image.new('L', wp.size, 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, wp.size[0] - 1, wp.size[1] - 1], radius=dp(18), fill=255)
        im.paste(wp, (int(dp(20)), int(y)), m)
        step = (W - dp(40)) / len(row)
        for i, (lab, ic) in enumerate(row):
            x = dp(20) + step * i + step / 2
            shape = 'circle' if dark else 'squircle'
            im.alpha_composite(N.masked(ic, int(dp(50)), shape), (int(x - dp(25)), int(y + dp(22))))
            d = ImageDraw.Draw(im)
            d.text((x, y + dp(80)), 'Nubi', font=F(13, False), fill=WHITE if dark else (30, 30, 40), anchor='ma')
            d.text((x, y + dp(100)), lab, font=F(12), fill=SUN if dark else (120, 80, 20), anchor='ma')
        y += dp(136)

    # Monocromo (ícono temático de Android 13+) y notificación
    d.text((dp(20), y + dp(4)), 'Temático (Android 13+) y notificación', font=F(17), fill=SUN)
    y += dp(38)
    fg = mono((C - u(1), C + u(6), u(0.92)))
    for i, (bg, fgc) in enumerate([((44, 48, 76), (206, 198, 255)), ((226, 222, 246), (70, 60, 130))]):
        base = Image.new('RGBA', (S, S), bg + (255,))
        tint = Image.new('RGBA', (S, S), fgc + (255,)); tint.putalpha(fg.getchannel('A'))
        base.alpha_composite(tint)
        im.alpha_composite(N.masked(base, int(dp(72)), 'circle'), (int(dp(20) + i * dp(90)), int(y)))
    small = fg.crop((int(u(18)), int(u(18)), int(S - u(18)), int(S - u(18)))).resize((int(dp(26)), int(dp(26))), Image.LANCZOS)
    ny = y + dp(8)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([dp(200), ny, W - dp(20), ny + dp(58)], radius=dp(14), fill=(236, 238, 246))
    d.ellipse([dp(210), ny + dp(13), dp(242), ny + dp(45)], fill=(90, 70, 190))
    im.alpha_composite(small, (int(dp(213)), int(ny + dp(16))))
    d = ImageDraw.Draw(im)
    d.text((dp(252), ny + dp(10)), 'Nubi', font=F(14), fill=(40, 40, 60))
    d.text((dp(252), ny + dp(32)), '¿Una partida hoy?', font=F(13, False), fill=(70, 70, 90))
    y += dp(96)
    P.wrap(d, dp(20), y, 'Android recorta el ícono con la forma que elige cada teléfono (círculo, cuadrado redondeado...): '
           'se muestran las dos. El temático usa solo la silueta, por eso la cara va calada.', F(13, False), DIM, W - dp(40))
    out = os.path.join(P.OUT, 'nubi-iconos.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
