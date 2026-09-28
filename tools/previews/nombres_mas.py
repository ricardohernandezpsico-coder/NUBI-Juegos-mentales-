# Más nombres (28-sep, Ricardo: "dame más opciones de nombres"). Maqueta PIL, NO la app: los nombres nuevos que
# pasaron la búsqueda rápida, cada uno con un ícono de ejemplo (varios reusan los de nombre_icono.py: nombre e ícono
# se pueden mezclar), cómo se ve el nombre bajo el ícono en un teléfono (celda de 84 dp: si no cabe, Android lo corta
# con "…"), y abajo los descartados con el motivo.
# Uso: python3 tools/previews/nombres_mas.py  ->  docs/previews/nombres-mas.png
import os, math, importlib.util
from PIL import Image, ImageDraw
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('ni', os.path.join(HERE, 'nombre_icono.py'))
N = importlib.util.module_from_spec(spec); spec.loader.exec_module(N)
P = N.P
F, dp, W, u, S = P.F, P.dp, P.W, N.u, N.S
INK, SUN, CREAM, WHITE, DIM, SOFT = P.INK, P.SUN, P.CREAM, P.WHITE, P.DIM, P.SOFT
LIME, CORAL, SKY, GRAPE = P.LIME, P.CORAL, P.SKY, P.GRAPE


def icon_planetea():
    """Tu planeta creciendo: planeta de arcilla con torres, cristales y un árbol, y una luna chica que orbita."""
    im = N.night(); cx, cy, r = S / 2, S / 2 + u(4), u(21)
    N.glow(im, cx, cy, r * 1.7, LIME, 70)
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    d.ellipse([cx - r * 1.55, cy - r * 0.5, cx + r * 1.55, cy + r * 0.5], outline=(255, 255, 255, 90), width=int(u(1.2)))
    im.alpha_composite(lay.rotate(-15, center=(cx, cy), resample=Image.BICUBIC))
    N.clay_disc(im, cx, cy, r, (70, 86, 178), drop=3)
    d = ImageDraw.Draw(im)
    # construcciones sobre el borde de arriba (crecen del planeta)
    for ang, kind, h in ((-112, 'torre', 12), (-90, 'cristal', 14), (-68, 'arbol', 11)):
        a = math.radians(ang); bx, by = cx + math.cos(a) * r * 0.86, cy + math.sin(a) * r * 0.86
        lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay); hh = u(h)
        if kind == 'torre':
            ld.polygon([(bx - hh * .3, by + u(2)), (bx - hh * .18, by - hh * .65), (bx, by - hh), (bx + hh * .18, by - hh * .65), (bx + hh * .3, by + u(2))], fill=(225, 212, 255), outline=INK, width=int(u(1.6)))
        elif kind == 'cristal':
            ld.polygon([(bx, by - hh), (bx + hh * .38, by - hh * .38), (bx, by + u(2)), (bx - hh * .38, by - hh * .38)], fill=(190, 222, 255), outline=INK, width=int(u(1.6)))
        else:
            ld.rectangle([bx - u(1.2), by - hh * .35, bx + u(1.2), by + u(2)], fill=INK)
            ld.ellipse([bx - hh * .42, by - hh, bx + hh * .42, by - hh * .25], fill=LIME, outline=INK, width=int(u(1.6)))
        im.alpha_composite(lay)
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0))
    for (ang, dist, zr, col) in [(30, 0.45, 0.34, CORAL), (150, 0.5, 0.28, SKY), (90, 0.1, 0.22, SUN)]:
        a = math.radians(ang); N.clay_disc(lay, cx + math.cos(a) * r * dist, cy + math.sin(a) * r * dist, r * zr, col, drop=1.2, border=1.6)
    import numpy as np
    m = Image.new('L', im.size, 0); ImageDraw.Draw(m).ellipse([cx - r + u(1.2), cy - r + u(1.2), cx + r - u(1.2), cy + r - u(1.2)], fill=255)
    im.paste(lay, (0, 0), Image.fromarray(np.minimum(np.array(m), np.array(lay.split()[3]))))
    N.shine(im, cx - r * 0.45, cy - r * 0.5, r * 0.26, r * 0.15)
    N.clay_disc(im, cx + r * 1.35, cy + r * 0.2, u(4.5), CREAM, drop=1.2, border=1.6)
    return im


def icon_estrellamente():
    """Una estrella sol grande y redondeada, con su resplandor y dos chispas."""
    im = N.night(); cx, cy = S / 2, S / 2 + u(1)
    N.glow(im, cx, cy, u(26), SUN, 120)
    pts = []
    for i in range(200):     # estrella de puntas redondeadas (arcilla)
        a = -math.pi / 2 + 2 * math.pi * i / 200
        k = 0.5 + 0.5 * math.cos(5 * (a + math.pi / 2))
        rr = u(12.5) + u(12.5) * k ** 1.6
        pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))
    N.clay_poly(im, pts, SUN, drop=2.8, border=2.4)
    N.shine(im, cx - u(5), cy - u(6), u(3.2), u(2), 150)
    d = ImageDraw.Draw(im)
    for (x, y, r) in ((cx + u(25), cy - u(20), u(4.5)), (cx - u(26), cy + u(18), u(3.2))):
        d.polygon(N.sparkle(x, y, r), fill=CREAM)
    return im


NEW = [
    ('Planetea', '"planetear": hacer crecer tu planeta. Une el nombre con la pestaña Hoy.', icon_planetea,
     'Sin apps ni marcas con ese nombre. Ojo: se parece a "Planeta" (Grupo Planeta, editorial grande).'),
    ('Lunamente', 'luna + mente; suena a adverbio: "vivir lunamente".', N.icon_mentaluna,
     'Solo una cuenta de Instagram en portugués. Ninguna app.'),
    ('Estrellamente', 'estrella + mente: "brillar estrellamente".', icon_estrellamente,
     'Sin resultados. Es largo: bajo el ícono puede cortarse.'),
    ('Pensastro', 'pensar + astro; juguetón: "¡piensa, astro!".', N.icon_astromente,
     'Sin resultados.'),
    ('Pensaluna', 'pensar + luna; suave y fácil de decir.', N.icon_mentaluna,
     'Sin resultados.'),
]

DISCARDED = [
    ('Constela', 'app de viajes y agencia en México'), ('Kosmi', 'app de juegos y fiestas virtuales'),
    ('Astrelia', 'app de astrología'), ('Estelaria', 'perfumes y autoayuda'),
    ('Mentaluz', 'empresa de salud mental en Chile'), ('Mentaria', 'proyecto "próximamente"'),
    ('Mentelar', 'organización de educación y salud'), ('Galaxio', 'juego de puzzle espacial'),
    ('Orbelia', 'sitio de bienestar'), ('Lunio', 'varias apps'),
    ('Mentenautas', 'podcast de salud mental'), ('Tripulia', 'jerga en portugués'),
]


def label(d, cx, y, text, cell):
    f = F(12, False); t = text
    while d.textlength(t, font=f) > cell and len(t) > 3: t = t[:-2] + '…'
    d.text((cx, y), t, font=f, fill=WHITE, anchor='ma')


def sheet():
    Hs = dp(190) * len(NEW) + dp(520)
    im = P.sky(5).resize((W, int(Hs))); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(40)), 'Más nombres', font=F(26), fill=WHITE)
    P.wrap(d, dp(20), dp(80), 'Los que pasaron la búsqueda rápida. El ícono es de ejemplo: nombre e ícono se pueden mezclar. A la derecha, el nombre bajo el ícono como en el teléfono.', F(14, False), DIM, W - dp(40))
    y = dp(150)
    for name, idea, fn, found in NEW:
        ic = fn()
        im.alpha_composite(N.masked(ic, int(dp(96)), 'circle'), (int(dp(20)), int(y)))
        d = ImageDraw.Draw(im)
        d.text((dp(132), y), name, font=F(24), fill=SUN)
        yy = P.wrap(d, dp(132), y + dp(34), idea, F(13, False), DIM, W - dp(250))
        P.wrap(d, dp(132), yy + dp(4), found, F(13, False), SOFT, W - dp(250))
        im.alpha_composite(N.masked(ic, int(dp(48)), 'circle'), (int(W - dp(88)), int(y + dp(8))))
        d = ImageDraw.Draw(im); label(d, W - dp(64), y + dp(62), name, dp(84))
        d.line([(dp(20), y + dp(172)), (W - dp(20), y + dp(172))], fill=(255, 255, 255, 30), width=int(dp(1)))
        y += dp(190)
    d.text((dp(20), y), 'Descartados (ya existen o chocan con el rubro)', font=F(17), fill=WHITE)
    y += dp(36)
    for i, (n, why) in enumerate(DISCARDED):
        yy = y + i * dp(24)
        d.text((dp(20), yy), n, font=F(15), fill=CORAL)
        d.text((dp(130), yy + dp(1)), why, font=F(13, False), fill=SOFT)
    out = os.path.join(P.OUT, 'nombres-mas.png'); im.convert('RGB').save(out); print(out)
    for name, _, fn, _ in NEW[:1] + NEW[2:3]:
        fn().resize((512, 512), Image.LANCZOS).convert('RGB').save(os.path.join(P.OUT, f'icono-{name.lower()}.png'))


if __name__ == '__main__':
    sheet()
