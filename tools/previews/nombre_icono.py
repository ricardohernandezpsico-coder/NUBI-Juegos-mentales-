# Nombre e ícono de la app (28-sep, pedido de Ricardo: "NeuroVida" ya es marca registrada de otros y hay una app
# "NEUROVIDA PSICOLOGIA" en Google Play). Maqueta PIL, NO la app: 5 nombres propuestos, cada uno con su ícono de
# arcilla (noche + arcilla, sin cerebros ni emojis), en grande (redondo y cuadrado redondeado, como los recorta
# Android) y en tamaño real en la pantalla de inicio de un teléfono, con el nombre debajo.
# Uso: python3 tools/previews/nombre_icono.py  ->  docs/previews/nombre-icono.png
import os, math, random, importlib.util
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('prop', os.path.join(HERE, 'inicio_propuestas.py'))
P = importlib.util.module_from_spec(spec); spec.loader.exec_module(P)
F, dp, W = P.F, P.dp, P.W
INK, SUN, CREAM, WHITE, DIM, SOFT = P.INK, P.SUN, P.CREAM, P.WHITE, P.DIM, P.SOFT
LIME, CORAL, SKY, GRAPE = P.LIME, P.CORAL, P.SKY, P.GRAPE

S = 1024          # lienzo del ícono (se reduce al final: bordes suaves)
U = S / 108       # 1 "dp" del ícono adaptativo de Android (108 x 108; la zona segura es el círculo de 66)


def u(v): return v * U


def night():
    """Fondo del ícono: cielo nocturno de la app con pocas estrellas (se leen a 48 dp)."""
    t = np.linspace(1, 0, S)[:, None, None]
    top, bot = np.array([22, 32, 104.]), np.array([6, 9, 36.])
    bg = np.repeat(bot + (top - bot) * t, S, axis=1)
    Y, X = np.mgrid[0:S, 0:S]
    d = np.clip(1 - np.sqrt((X - 0.8 * S) ** 2 + (Y - 0.15 * S) ** 2) / (0.8 * S), 0, 1) ** 2 * 0.25
    bg[:] = bg * (1 - d[..., None]) + np.array(SKY) * d[..., None]
    im = Image.fromarray(bg.clip(0, 255).astype(np.uint8)).convert('RGBA')
    dr = ImageDraw.Draw(im); rnd = random.Random(7)
    for _ in range(26):
        x, y, r = rnd.random() * S, rnd.random() * S, u(0.4 + rnd.random() * 0.7)
        dr.ellipse([x - r, y - r, x + r, y + r], fill=(255, 255, 255, int(90 + rnd.random() * 120)))
    return im


def glow(im, cx, cy, r, rgb, a):
    g = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(g).ellipse([cx - r, cy - r, cx + r, cy + r], fill=rgb + (a,))
    im.alpha_composite(g.filter(ImageFilter.GaussianBlur(r * 0.45)))


def clay_poly(im, pts, fill, drop=2.2, border=2.2):
    """Una pieza de arcilla: sombra dura que cae hacia abajo, borde tinta grueso."""
    d = ImageDraw.Draw(im)
    d.polygon([(x, y + u(drop)) for x, y in pts], fill=INK)
    d.polygon(pts, fill=fill, outline=INK, width=int(u(border)))


def clay_disc(im, cx, cy, r, fill, drop=2.2, border=2.2):
    d = ImageDraw.Draw(im)
    d.ellipse([cx - r, cy - r + u(drop), cx + r, cy + r + u(drop)], fill=INK)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=fill, outline=INK, width=int(u(border)))


def shine(im, cx, cy, rx, ry, a=90):
    h = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(h).ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=(255, 255, 255, a))
    im.alpha_composite(h.filter(ImageFilter.GaussianBlur(rx * 0.25)))


def star(cx, cy, r, inner=0.45, n=5, rot=-90):
    return [(cx + math.cos(math.radians(rot + i * 180 / n)) * (r if i % 2 == 0 else r * inner),
             cy + math.sin(math.radians(rot + i * 180 / n)) * (r if i % 2 == 0 else r * inner)) for i in range(2 * n)]


def sparkle(cx, cy, r):
    """Destello de 4 puntas con lados curvos."""
    pts = []
    for i in range(64):
        a = 2 * math.pi * i / 64
        rr = r * (0.3 + 0.7 * (abs(math.cos(2 * a)) ** 3))
        pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))
    return pts


def ring(im, cx, cy, rx, ry, col, w, front):
    """Anillo de planeta: la mitad de atrás va antes que el planeta, la de adelante después."""
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    a0, a1 = (0, 180) if front else (180, 360)
    box = [cx - rx, cy - ry, cx + rx, cy + ry]
    d.arc([box[0], box[1] + u(2), box[2], box[3] + u(2)], a0, a1, fill=INK, width=int(w + u(4.4)))
    d.arc(box, a0, a1, fill=INK, width=int(w + u(4.4)))
    d.arc(box, a0, a1, fill=col, width=int(w))
    rot = lay.rotate(18, center=(cx, cy), resample=Image.BICUBIC)
    im.alpha_composite(rot)


# ------------------------------------------------------------------ los 5 íconos

def icon_cosmente():
    """Tu planeta: el planeta de la pestaña Hoy (zonas de colores) con anillo sol y una estrella que orbita."""
    im = night(); cx, cy, r = S / 2, S / 2 + u(1), u(22)
    glow(im, cx, cy, r * 1.7, SKY, 90)
    ring(im, cx, cy, r * 1.5, r * 0.44, SUN, u(4.2), front=False)
    clay_disc(im, cx, cy, r, (70, 86, 178), drop=3)
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0))
    for (ang, dist, zr, col) in [(-40, 0.52, 0.36, LIME), (60, 0.5, 0.34, CORAL), (170, 0.55, 0.3, GRAPE), (0, 0, 0.22, SKY)]:
        a = math.radians(ang); zx, zy = cx + math.cos(a) * r * dist, cy + math.sin(a) * r * dist
        clay_disc(lay, zx, zy, r * zr, col, drop=1.2, border=1.6)
    mask = Image.new('L', im.size, 0); ImageDraw.Draw(mask).ellipse([cx - r + u(1.2), cy - r + u(1.2), cx + r - u(1.2), cy + r - u(1.2)], fill=255)
    im.paste(lay, (0, 0), Image.fromarray(np.minimum(np.array(mask), np.array(lay.split()[3]))))
    shine(im, cx - r * 0.45, cy - r * 0.55, r * 0.28, r * 0.16)
    ring(im, cx, cy, r * 1.5, r * 0.44, SUN, u(4.2), front=True)
    clay_poly(im, star(cx + r * 1.2, cy - r * 1.05, u(6.5)), CREAM, drop=1.4, border=1.8)
    return im


def icon_luminautas():
    """El casco del astronauta (el de Radar) con la visera que refleja una estrella."""
    im = night(); cx, cy = S / 2, S / 2 + u(2)
    glow(im, cx, cy, u(30), GRAPE, 80)
    # hombros / cuello
    clay_poly(im, [(cx - u(20), cy + u(24)), (cx - u(17), cy + u(15)), (cx + u(17), cy + u(15)), (cx + u(20), cy + u(24))], (200, 206, 232))
    clay_disc(im, cx, cy - u(2), u(20), CREAM, drop=3)
    # visera
    d = ImageDraw.Draw(im)
    vis = [cx - u(14), cy - u(12), cx + u(14), cy + u(8)]
    d.rounded_rectangle([vis[0], vis[1] + u(1.4), vis[2], vis[3] + u(1.4)], radius=u(9), fill=INK)
    d.rounded_rectangle(vis, radius=u(9), fill=(52, 44, 130), outline=INK, width=int(u(2)))
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
    ld.ellipse([cx - u(20), cy - u(4), cx + u(16), cy + u(24)], fill=(90, 78, 190, 255))
    m = Image.new('L', im.size, 0); ImageDraw.Draw(m).rounded_rectangle([vis[0] + u(2), vis[1] + u(2), vis[2] - u(2), vis[3] - u(2)], radius=u(7), fill=255)
    im.paste(lay, (0, 0), Image.fromarray(np.minimum(np.array(m), np.array(lay.split()[3]))))
    glow(im, cx + u(5), cy - u(4), u(7), SUN, 170)
    d = ImageDraw.Draw(im); d.polygon(sparkle(cx + u(5), cy - u(4), u(6.5)), fill=SUN)
    shine(im, cx - u(8), cy - u(8), u(3.5), u(2), 150)
    # antena con luz
    d.line([(cx + u(13), cy - u(16)), (cx + u(17), cy - u(26))], fill=INK, width=int(u(2.2)))
    clay_disc(im, cx + u(17), cy - u(27), u(3.2), LIME, drop=1, border=1.6)
    return im


def icon_astromente():
    """Un destello sol grande (la chispa) sobre una órbita, con un planeta coral pequeño."""
    im = night(); cx, cy = S / 2, S / 2
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    d.ellipse([cx - u(30), cy - u(12), cx + u(30), cy + u(12)], outline=(255, 255, 255, 110), width=int(u(1.6)))
    im.alpha_composite(lay.rotate(-20, center=(cx, cy), resample=Image.BICUBIC))
    glow(im, cx, cy, u(24), SUN, 120)
    clay_poly(im, sparkle(cx, cy, u(24)), SUN, drop=2.6, border=2.2)
    shine(im, cx - u(3), cy - u(6), u(2.6), u(4), 150)
    a = math.radians(-20 + 205); px, py = cx + math.cos(a) * u(30), cy + math.sin(a) * u(12)
    px, py = cx - u(27), cy + u(12)
    clay_disc(im, px, py, u(6.5), CORAL, drop=1.6, border=1.8)
    clay_poly(im, star(cx + u(24), cy - u(20), u(4)), CREAM, drop=1, border=1.4)
    return im


def icon_mentaluna():
    """Luna creciente de arcilla crema con cráteres y una estrella lima en el hueco."""
    im = night(); cx, cy, r = S / 2 - u(3), S / 2, u(24)
    glow(im, cx, cy, r * 1.5, CREAM, 70)
    moon = []
    for i in range(80):          # borde de afuera
        a = math.radians(70 + 220 * i / 79); moon.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    ox, oy, orr = cx + r * 0.55, cy - r * 0.18, r * 0.82
    for i in range(80):          # borde de adentro (se recorre al revés)
        a0 = math.atan2(moon[-1][1] - oy, moon[-1][0] - ox); a1 = math.atan2(moon[0][1] - oy, moon[0][0] - ox)
        if a1 > a0: a1 -= 2 * math.pi
        a = a0 + (a1 - a0) * i / 79; moon.append((ox + math.cos(a) * orr, oy + math.sin(a) * orr))
    clay_poly(im, moon, CREAM, drop=3, border=2.4)
    d = ImageDraw.Draw(im)
    for (mx, my, mr) in [(-0.62, 0.1, 0.13), (-0.35, 0.55, 0.1), (-0.72, -0.35, 0.08)]:
        x, y, rr = cx + mx * r, cy + my * r, mr * r
        d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=(236, 222, 196), outline=(200, 184, 160), width=int(u(1)))
    shine(im, cx - r * 0.7, cy - r * 0.45, r * 0.12, r * 0.2, 120)
    glow(im, cx + r * 0.62, cy - r * 0.05, u(8), LIME, 140)
    clay_poly(im, star(cx + r * 0.62, cy - r * 0.05, u(8.5)), LIME, drop=1.6, border=1.8)
    return im


def icon_orbimente():
    """Un sol pequeño con dos órbitas que se cruzan y dos planetas de colores (coral y lima)."""
    im = night(); cx, cy = S / 2, S / 2
    for rot in (30, -30):
        lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
        d.ellipse([cx - u(30), cy - u(11), cx + u(30), cy + u(11)], outline=INK, width=int(u(5.2)))
        d.ellipse([cx - u(30), cy - u(11), cx + u(30), cy + u(11)], outline=(160, 150, 240), width=int(u(2.4)))
        im.alpha_composite(lay.rotate(rot, center=(cx, cy), resample=Image.BICUBIC))
    glow(im, cx, cy, u(15), SUN, 150)
    clay_disc(im, cx, cy, u(11), SUN, drop=2.4)
    shine(im, cx - u(4), cy - u(4), u(3), u(2), 150)
    for rot, t, col in ((30, 200, CORAL), (-30, 20, LIME)):
        a = math.radians(t); x, y = math.cos(a) * u(30), math.sin(a) * u(11)
        rr = math.radians(-rot); X = cx + x * math.cos(rr) - y * math.sin(rr); Y = cy + x * math.sin(rr) + y * math.cos(rr)
        clay_disc(im, X, Y, u(6), col, drop=1.6, border=1.8)
    return im


NAMES = [
    ('Cosmente', 'cosmos + mente · "cósmicamente"', icon_cosmente, 'Tu planeta: el de la pestaña Hoy, con sus zonas'),
    ('Luminautas', 'navegantes de la luz · "¡Hola, luminauta!"', icon_luminautas, 'El casco del astronauta, con una estrella en la visera'),
    ('Astromente', 'astro + mente', icon_astromente, 'La chispa: un destello sol sobre una órbita'),
    ('Mentaluna', 'mente + luna', icon_mentaluna, 'Luna creciente de arcilla con una estrella'),
    ('Orbimente', 'órbita + mente', icon_orbimente, 'Un sol con dos órbitas y dos planetas'),
]


def masked(ic, size, shape):
    """Recorte de Android: el lanzador del teléfono decide la forma (círculo, cuadrado redondeado...)."""
    c = S * 18 / 108   # el lanzador muestra solo el centro (72 de 108 dp); el resto es margen para su animación
    im = ic.crop((int(c), int(c), int(S - c), int(S - c))).resize((size, size), Image.LANCZOS)
    m = Image.new('L', (size * 4, size * 4), 0); d = ImageDraw.Draw(m)
    if shape == 'circle': d.ellipse([0, 0, size * 4 - 1, size * 4 - 1], fill=255)
    else: d.rounded_rectangle([0, 0, size * 4 - 1, size * 4 - 1], radius=size * 4 * 0.3, fill=255)
    out = Image.new('RGBA', (size, size), (0, 0, 0, 0)); out.paste(im, (0, 0), m.resize((size, size), Image.LANCZOS))
    return out


def sheet():
    Hs = dp(250) * len(NAMES) + dp(470)
    im = P.sky(3).resize((W, int(Hs)))
    d = ImageDraw.Draw(im)
    d.text((dp(20), dp(40)), 'Nombre e ícono: 5 propuestas', font=F(26), fill=WHITE)
    P.wrap(d, dp(20), dp(80), 'Cada ícono va con su nombre, pero se pueden mezclar. Arriba grande (el teléfono lo recorta en círculo o en cuadrado redondeado); a la derecha, en tamaño real.', F(14, False), DIM, W - dp(40))
    y = dp(150)
    icons = []
    for name, idea, fn, desc in NAMES:
        ic = fn(); icons.append((name, ic))
        big = masked(ic, int(dp(120)), 'circle'); im.alpha_composite(big, (int(dp(20)), int(y)))
        sq = masked(ic, int(dp(76)), 'squircle'); im.alpha_composite(sq, (int(dp(152)), int(y + dp(44))))
        d = ImageDraw.Draw(im)
        d.text((dp(244), y + dp(6)), name, font=F(24), fill=SUN)
        P.wrap(d, dp(244), y + dp(40), idea, F(13, False), DIM, W - dp(260))
        P.wrap(d, dp(244), y + dp(86), desc, F(13, False), SOFT, W - dp(260))
        # tamaño real (48 dp) con el nombre debajo, como en la pantalla de inicio
        small = masked(ic, int(dp(48)), 'circle'); im.alpha_composite(small, (int(dp(244)), int(y + dp(150))))
        d = ImageDraw.Draw(im); d.text((dp(268), y + dp(204)), name, font=F(12, False), fill=WHITE, anchor='ma')
        d.line([(dp(20), y + dp(236)), (W - dp(20), y + dp(236))], fill=(255, 255, 255, 30), width=int(dp(1)))
        y += dp(250)
    # pantalla de inicio de un teléfono: los 5 íconos entre apps comunes (grises), a 48 dp
    d.text((dp(20), y + dp(4)), 'En la pantalla de inicio', font=F(18), fill=WHITE)
    y += dp(44)
    d.rounded_rectangle([dp(20), y, W - dp(20), y + dp(250)], radius=dp(20), fill=(34, 40, 60))
    cols = 4; cw = (W - dp(40)) / cols
    for i in range(8):
        cx = dp(20) + cw * (i % cols) + cw / 2; cy = y + dp(28) + (i // cols) * dp(110)
        k = [0, None, 1, 2, None, 3, None, 4][i]
        if k is None:
            d.ellipse([cx - dp(24), cy, cx + dp(24), cy + dp(48)], fill=(120, 128, 150)); lbl = ['', 'Cámara', '', '', 'Reloj', '', 'Mapas', ''][i]
        else:
            name, ic = icons[k]; im.alpha_composite(masked(ic, int(dp(48)), 'circle'), (int(cx - dp(24)), int(cy))); lbl = name
            d = ImageDraw.Draw(im)
        d.text((cx, cy + dp(56)), lbl, font=F(12, False), fill=WHITE, anchor='ma')
    out = os.path.join(P.OUT, 'nombre-icono.png')
    im.convert('RGB').save(out); print(out)
    for name, ic in icons:   # cada ícono solo, 512 px, por si se quiere mirar de cerca
        ic.resize((512, 512), Image.LANCZOS).convert('RGB').save(os.path.join(P.OUT, f'icono-{name.lower()}.png'))


if __name__ == '__main__':
    sheet()
