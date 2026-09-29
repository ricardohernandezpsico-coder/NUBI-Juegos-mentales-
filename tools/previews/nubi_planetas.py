# El planeta de Hoy, 3 caminos (29-sep). Ricardo aprobó todo lo del mundo de Nubi (fondo de nebulosas, satélites, Nubi
# guía, tu semana, mejoras) MENOS el planeta: "no me convence aún". El de la maqueta anterior eran manchas de color
# planas (parecía un gráfico de torta). Tres caminos, con el mismo cielo, satélites y Nubi:
#   A · Globo con relieve: continentes de bordes naturales proyectados sobre la esfera, biomas que se funden, relieve con
#       luz, nubes, montañas nevadas, costa clara (lo más cercano a la referencia).
#   B · Planeta pequeño: se ve solo la parte de arriba del planeta, como un horizonte curvo, con las 6 zonas DE PIE en
#       fila (grandes y claras en un teléfono); se desliza para girarlo.
#   C · El de arcilla actual: el planeta que ya está en la app, con el cielo nuevo, los satélites y Nubi.
# Uso: python3 tools/previews/nubi_planetas.py  ->  docs/previews/planeta-caminos.png
import os, math, random, importlib.util
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('nm', os.path.join(HERE, 'nubi_mundo.py'))
NM = importlib.util.module_from_spec(spec); spec.loader.exec_module(NM)
NS, N, P = NM.NS, NM.N, NM.P
F, dp, W = P.F, P.dp, P.W
INK, SUN, CREAM, WHITE, DIM, LIME, SKY, CORAL = P.INK, P.SUN, P.CREAM, P.WHITE, P.DIM, P.LIME, P.SKY, P.CORAL
LINE = NS.LINE
to_img, fbm = NM.to_img, NM.fbm
PH = dp(520)

# zonas en longitud/latitud (grados) sobre la cara visible
ZONES = {
    'memoria':      (-38, -22, (112, 200, 120)),
    'atencion':     (20, -36, (150, 160, 140)),
    'lenguaje':     (42, 8, (58, 150, 88)),
    'calculo':      (-40, 26, (222, 192, 122)),
    'velocidad':    (-4, 2, (206, 124, 92)),
    'razonamiento': (18, 40, None),       # el mar
}


def globe(size, grow, seed=21):
    S = size; C = S / 2; R = S * 0.36
    Y, X = np.mgrid[0:S, 0:S].astype(np.float32)
    nx, ny = (X - C) / R, (Y - C) / R
    r2 = nx ** 2 + ny ** 2
    disc = np.clip((1 - np.sqrt(r2)) * R / 1.5, 0, 1)
    lat = np.arcsin(np.clip(ny, -1, 1))
    lon = np.arcsin(np.clip(nx / np.sqrt(np.clip(1 - ny ** 2, 1e-4, 1)), -1, 1))
    # texturas en coordenadas de la superficie
    T = 700
    def tex(seed_, oct_, base):
        t = fbm(T, T, seed_, oct_, base)
        iu = np.clip(((lon / math.pi + 0.5) * (T - 1)).astype(int), 0, T - 1)
        iv = np.clip(((lat / math.pi + 0.5) * (T - 1)).astype(int), 0, T - 1)
        return t[iv, iu]
    elev = tex(seed, 6, 3)
    # dónde hay tierra: el ruido + un empujón en cada zona (y un hundimiento en el mar)
    bias = np.zeros_like(elev)
    wsum = np.zeros_like(elev); col = np.zeros(elev.shape + (3,), np.float32)
    for key, (lo, la, rgb) in ZONES.items():
        dl = np.sqrt((np.degrees(lon) - lo) ** 2 + (np.degrees(lat) - la) ** 2)
        g = np.exp(-(dl / 20) ** 2)
        if rgb is None: bias -= g * 0.35; continue
        bias += g * 0.28
        w = np.exp(-(dl / 16) ** 2) + 1e-4; wsum += w; col += w[..., None] * np.array(rgb, np.float32)
    col /= wsum[..., None]
    h = elev + bias
    land = np.clip((h - 0.62) * 30, 0, 1) * disc
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    # atmósfera
    atm = np.clip((1.07 - np.sqrt(r2)) * 18, 0, 1) * (np.sqrt(r2) > 0.9)
    im.alpha_composite(to_img(np.asarray(Image.fromarray((atm * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(S * 0.02)), np.float32) / 255 * 0.9, (120, 205, 255)))
    # océano con profundidad
    depth = np.clip((0.62 - h) * 3, 0, 1)[..., None]
    ocean = np.array([70, 170, 225.]) + (np.array([28, 70, 170.]) - np.array([70, 170, 225.])) * depth
    lay = np.zeros((S, S, 4), np.float32); lay[..., :3] = ocean; lay[..., 3] = disc * 255
    im.alpha_composite(Image.fromarray(lay.astype(np.uint8)))
    # arena de la costa
    beach = np.clip((h - 0.6) * 30, 0, 1) * (1 - land) * disc
    im.alpha_composite(to_img(beach * 0.9, (236, 222, 170)))
    # tierra con relieve (luz de arriba a la izquierda)
    gy, gx = np.gradient(elev * 60)
    hill = np.clip(0.5 - (gx + gy) * 0.9, 0, 1)[..., None]
    snow = np.clip((h - 0.95) * 8, 0, 1)[..., None]
    lc = col * (0.7 + 0.6 * hill)
    lc = lc * (1 - snow) + np.array([245, 248, 255.]) * snow
    lay = np.zeros((S, S, 4), np.float32); lay[..., :3] = np.clip(lc, 0, 255); lay[..., 3] = land * 255
    im.alpha_composite(Image.fromarray(lay.astype(np.uint8)))
    # rasgos (por zona)
    d = ImageDraw.Draw(im); rnd = random.Random(3)
    def at(lo, la):
        a, b = math.radians(lo), math.radians(la)
        return C + math.cos(b) * math.sin(a) * R, C + math.sin(b) * R
    sc = S / 1100
    def cluster(key, n, fn, spread=7):
        lo, la, _ = ZONES[key]
        pts = sorted([at(lo + rnd.uniform(-spread, spread), la + rnd.uniform(-spread * 0.7, spread * 0.7)) for _ in range(n)], key=lambda p: p[1])
        for x, y in pts: fn(x, y)
    cluster('memoria', 1 + int(grow['memoria'] * 3), lambda x, y: NM.crystal(d, x, y, 40 * sc, (120, 220, 250)))
    cluster('atencion', 2 + int(grow['atencion'] * 2), lambda x, y: NM.mountain(d, x, y, 70 * sc), 9)
    x, y = at(34, -30); NM.lighthouse(d, x, y, 70 * sc)
    cluster('lenguaje', 3 + int(grow['lenguaje'] * 4), lambda x, y: NM.tree(d, x, y, 44 * sc, (60, 170 + rnd.randint(-20, 20), 96)), 9)
    cluster('calculo', 1 + int(grow['calculo'] * 3), lambda x, y: NM.dome(d, x, y, 40 * sc))
    cluster('velocidad', 1 + int(grow['velocidad'] * 2), lambda x, y: NM.antenna(d, x, y, 70 * sc), 5)
    for lo, la in [(10, 38), (28, 44)][: 1 + int(grow['razonamiento'] * 2)]:
        x, y = at(lo, la); NM.island_tower(d, x, y, 44 * sc)
    # nubes
    cl = tex(77, 5, 5)
    wisp = np.clip((cl - 0.58) * 4.5, 0, 1) * disc * np.clip(np.abs(np.sin(lat * 9 + lon * 1.5)) * 1.5, 0, 1)
    im.alpha_composite(to_img(wisp * 0.6, (252, 252, 255)))
    # terminador, borde con luz y brillo
    shade = np.clip((nx * 0.55 + ny * 0.6 + 0.2), 0, 1) ** 1.4 * disc
    im.alpha_composite(to_img(shade * 0.6, (8, 10, 44)))
    rim = np.clip(1 - np.abs(np.sqrt(r2) - 0.975) * 30, 0, 1) * np.clip(-(nx + ny), 0, 1)
    im.alpha_composite(to_img(rim * 0.8, (210, 244, 255)))
    ImageDraw.Draw(im).ellipse([C - R, C - R, C + R, C + R], outline=LINE, width=max(3, int(S * 0.005)))
    return im, at, R


def sky_panel(seed):
    return NM.nebula_bg(W, int(PH), seed)


def orbit_sats(im, cx, cy, rx, ry, front_only=False):
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
    ld.arc([cx - rx, cy - ry, cx + rx, cy + ry], 0 if front_only else 180, 180 if front_only else 360, fill=(200, 230, 255, 140), width=int(dp(2)))
    im.alpha_composite(lay)


def put_sats(im, cx, cy, rx, ry):
    for ang, col, done in [(200, P.DOM['atencion'], True), (340, P.DOM['memoria'], True), (95, P.DOM['velocidad'], False)]:
        a = math.radians(ang)
        NM.satellite(im, cx + math.cos(a) * rx, cy + math.sin(a) * ry, dp(16), col, done)


def nubi_bubble(im, text2='Picos de la Atención'):
    sp = NM.nubi_sprite(lambda c, x, y, kk: NS.nubi(c, x, y, kk, 'hola'), dp(86))
    im.alpha_composite(sp, (int(W - dp(92)), int(dp(20))))
    NM.bubble(im, W - dp(86), dp(18), ['Zona del día', text2], tail='right')


def panel_a(grow):
    im = sky_panel(5)
    cx, cy = W / 2, dp(300)
    orbit_sats(im, cx, cy, dp(186), dp(44))
    g, at, R = globe(1400, grow)
    size = int(dp(300) * 1400 / (2 * R))
    gi = g.resize((size, size), Image.LANCZOS)
    im.alpha_composite(gi, (int(cx - size / 2), int(cy - size / 2)))
    orbit_sats(im, cx, cy, dp(186), dp(44), True)
    put_sats(im, cx, cy, dp(186), dp(44))
    nubi_bubble(im)
    return im


def landmark(key, s, grow):
    """Una zona de pie, dibujada derecha en su propio lienzo (base al centro de abajo)."""
    c = Image.new('RGBA', (int(s * 3), int(s * 3)), (0, 0, 0, 0)); d = ImageDraw.Draw(c)
    bx, by = s * 1.5, s * 2.4; k = s / 100
    if key == 'memoria':
        for dx, sz in [(-34, 34), (30, 30), (0, 46)][: 1 + int(grow * 2) + 0]:
            NM.crystal(d, bx + dx * k, by, sz * k, (120, 220, 250))
    elif key == 'atencion':
        NM.mountain(d, bx - 22 * k, by, 52 * k); NM.mountain(d, bx + 26 * k, by, 40 * k)
        NM.lighthouse(d, bx + 8 * k, by - 2 * k, 62 * k)
    elif key == 'lenguaje':
        for dx, sz in [(-30, 42), (26, 38), (0, 54)][: 2 + int(grow)]:
            NM.tree(d, bx + dx * k, by, sz * k, (60, 175, 98))
    elif key == 'calculo':
        NM.dome(d, bx - 22 * k, by, 30 * k); NM.dome(d, bx + 20 * k, by, 38 * k)
    elif key == 'velocidad':
        NM.antenna(d, bx - 16 * k, by, 70 * k); NM.antenna(d, bx + 20 * k, by, 52 * k)
    elif key == 'razonamiento':
        NM.island_tower(d, bx - 16 * k, by + 2 * k, 40 * k); NM.island_tower(d, bx + 26 * k, by + 4 * k, 30 * k)
    return c


def panel_b(grow):
    im = sky_panel(8)
    R = dp(240); cx = W / 2; top = dp(262); cy = top + R
    # satélites en el cielo sobre el horizonte
    orbit_sats(im, cx, top + dp(10), dp(200), dp(120))
    put_sats(im, cx, top + dp(10), dp(200), dp(120))
    d = ImageDraw.Draw(im)
    NS.glow(im, cx, cy, R * 1.08, (120, 205, 255), 120)
    # cuerpo del planeta
    S2 = int(2 * R); Y, X = np.mgrid[0:S2, 0:S2].astype(np.float32)
    nx, ny = (X - R) / R, (Y - R) / R; rr = np.sqrt(nx ** 2 + ny ** 2)
    disc = np.clip((1 - rr) * R / 1.5, 0, 1)
    body = np.array([70, 64, 150.]) + (np.array([30, 26, 80.]) - np.array([70, 64, 150.])) * np.clip(rr, 0, 1)[..., None]
    lay = np.zeros((S2, S2, 4), np.float32); lay[..., :3] = body; lay[..., 3] = disc * 255
    pl = Image.fromarray(lay.astype(np.uint8))
    # franja de pasto y el tramo de mar
    ang = np.degrees(np.arctan2(nx, -ny))
    sea = (ang > 15) & (ang < 33)
    grass = (rr > 0.94) & (rr <= 1.0)
    band = np.where(sea[..., None], np.array([70, 170, 230.]), np.array([120, 205, 110.]))
    lay2 = np.zeros((S2, S2, 4), np.float32); lay2[..., :3] = band; lay2[..., 3] = (grass * disc) * 255
    pl.alpha_composite(Image.fromarray(lay2.astype(np.uint8)))
    soil = (rr > 0.9) & (rr <= 0.94)
    pl.alpha_composite(to_img((soil * disc).astype(np.float32) * 0.8, (110, 84, 150)))
    tex = fbm(S2, S2, 5, 4, 8)
    pl.alpha_composite(to_img(np.clip(tex - 0.55, 0, 1) * 2 * disc * (rr < 0.9) * 0.5, (120, 110, 200)))
    ImageDraw.Draw(pl).ellipse([2, 2, S2 - 3, S2 - 3], outline=LINE, width=int(dp(2.5)))
    im.alpha_composite(pl, (int(cx - R), int(cy - R)))
    # las 6 zonas de pie
    order = [('memoria', -41), ('atencion', -24.5), ('velocidad', -8), ('lenguaje', 8), ('razonamiento', 24), ('calculo', 41)]
    names = {'memoria': 'Memoria', 'atencion': 'Atención', 'velocidad': 'Velocidad', 'lenguaje': 'Lenguaje',
             'razonamiento': 'Razonamiento', 'calculo': 'Cálculo'}
    for i, (key, a) in enumerate(order):
        s = dp(58)
        if key == 'atencion':
            gx, gy = cx + math.sin(math.radians(a)) * R, cy - math.cos(math.radians(a)) * R
            NS.glow(im, gx, gy - dp(30), dp(52), SUN, 150)
        lm = landmark(key, s, grow[key]).rotate(-a, resample=Image.BICUBIC, center=(s * 1.5, s * 2.4))
        bx, by = cx + math.sin(math.radians(a)) * (R - dp(4)), cy - math.cos(math.radians(a)) * (R - dp(4))
        im.alpha_composite(lm, (int(bx - s * 1.5), int(by - s * 2.4)))
        lx, ly = cx + math.sin(math.radians(a)) * (R - dp(36 if i % 2 == 0 else 66)), cy - math.cos(math.radians(a)) * (R - dp(36 if i % 2 == 0 else 66))
        d = ImageDraw.Draw(im)
        d.text((lx, ly), names[key], font=F(13), fill=SUN if key == 'atencion' else WHITE, anchor='mm')
    nubi_bubble(im)
    d = ImageDraw.Draw(im)
    d.text((W / 2, PH - dp(120)), '¡Creció la Memoria!', font=F(18), fill=SUN, anchor='mm')
    d.text((W / 2, PH - dp(30)), '‹  desliza para girar tu planeta  ›', font=F(13, False), fill=DIM, anchor='mm')
    return im


def panel_c(grow):
    im = sky_panel(11)
    cx, cy, r = W / 2, dp(300), dp(140)
    orbit_sats(im, cx, cy, dp(186), dp(44))
    P.blob_planet(im, cx, cy, r, grow, spark='atencion')
    orbit_sats(im, cx, cy, dp(186), dp(44), True)
    put_sats(im, cx, cy, dp(186), dp(44))
    nubi_bubble(im)
    return im


PANELS = [('A', 'Globo con relieve', panel_a, 'Continentes naturales, biomas que se funden, montañas nevadas y nubes. El más cercano a tu referencia.'),
          ('B', 'Planeta pequeño', panel_b, 'Se ve la parte de arriba como un horizonte: las 6 zonas de pie, grandes y con su nombre. Se desliza para girarlo.'),
          ('C', 'El de arcilla actual', panel_c, 'El planeta que ya está en la app, con el cielo nuevo, los satélites y Nubi.')]


def sheet():
    grow = {'memoria': 0.9, 'atencion': 0.7, 'lenguaje': 0.4, 'calculo': 0.5, 'velocidad': 0.8, 'razonamiento': 0.5}
    gap = dp(96)
    H = dp(80) + len(PANELS) * (PH + gap)
    im = Image.new('RGBA', (W, int(H)), (10, 10, 30, 255)); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(26)), 'Tu planeta · 3 caminos', font=F(24), fill=SUN)
    d.text((dp(20), dp(58)), 'Mismo cielo, satélites y Nubi; cambia el planeta.', font=F(13, False), fill=DIM)
    y = dp(90)
    for letter, name, fn, txt in PANELS:
        pn = fn(grow)
        m = Image.new('L', pn.size, 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, pn.size[0] - 1, pn.size[1] - 1], radius=dp(20), fill=255)
        im.paste(pn, (0, int(y)), m)
        d = ImageDraw.Draw(im)
        d.text((dp(20), y + PH + dp(12)), f'{letter} · {name}', font=F(18), fill=WHITE)
        P.wrap(d, dp(20), y + PH + dp(40), txt, F(13, False), DIM, W - dp(40))
        y += PH + gap
    out = os.path.join(P.OUT, 'planeta-caminos.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
