# Propuestas para la pestaña "Hoy" (28-sep, pedido de Ricardo: el camino de días en perspectiva se ve bien pero
# "lo que muestra es pobre"). Maquetas PIL, NO la app: 6 ideas con el estilo noche + arcilla, cada una con información
# que le interese a la persona. Las flechas y notas en cursiva sol indican el movimiento de fondo.
# Uso: python3 tools/previews/inicio_propuestas.py  ->  docs/previews/inicio-propuesta-N.png + inicio-propuestas.png
import os, math, random, importlib.util
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
HERE = os.path.dirname(__file__); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
spec = importlib.util.spec_from_file_location('iconos', os.path.join(HERE, 'iconos_juegos.py'))
ic = importlib.util.module_from_spec(spec); spec.loader.exec_module(ic)
FB = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
FS = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
DP = 2; W, H = 412 * DP, 915 * DP
INK = (26, 18, 64); SUN = (255, 201, 60); CREAM = (255, 248, 236); WHITE = (255, 255, 255); DIM = (199, 208, 255)
LIME = (155, 229, 100); CORAL = (255, 107, 74); SKY = (76, 201, 240); GRAPE = (184, 164, 255); SOFT = (150, 160, 210)
DOM = ic.DOM
OUT = ROOT + '/docs/previews'


def F(sz, b=True): return ImageFont.truetype(FB if b else FS, int(sz * DP))
def dp(v): return v * DP


def sky(seed):
    t = np.linspace(1, 0, H)[:, None, None]; b = np.array([4, 6, 28.]); m = np.array([8, 14, 58.]); tp = np.array([16, 26, 88.])
    bg = np.repeat(np.where(t < 0.5, b + (m - b) * (t * 2), m + (tp - m) * ((t - 0.5) * 2)), W, axis=1); Y, X = np.mgrid[0:H, 0:W]
    for cx, cy, r, rgb, a in [(0.9 * W, 0.1 * H, 0.9 * W, (76, 201, 240), .22), (0.05 * W, 0.95 * H, 0.85 * W, (255, 107, 74), .2)]:
        d = np.clip(1 - np.sqrt((X - cx) ** 2 + (Y - cy) ** 2) / r, 0, 1) ** 2 * a; bg[:] = bg * (1 - d[..., None]) + np.array(rgb) * d[..., None]
    im = Image.fromarray(bg.clip(0, 255).astype(np.uint8)).convert('RGBA'); d = ImageDraw.Draw(im); rnd = random.Random(seed)
    for _ in range(120):
        x, y = rnd.random() * W, rnd.random() * H; r = rnd.random() * 2.2 + 0.8
        d.ellipse([x - r, y - r, x + r, y + r], fill=(255, 255, 255, int(80 + rnd.random() * 150)))
    return im


def glow(im, cx, cy, r, rgb, a):
    g = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(g).ellipse([cx - r, cy - r, cx + r, cy + r], fill=rgb + (a,))
    im.alpha_composite(g.filter(ImageFilter.GaussianBlur(r * 0.45)))


def disc(d, cx, cy, r, fill, border=3, drop=4):
    d.ellipse([cx - r, cy - r + dp(drop), cx + r, cy + r + dp(drop)], fill=INK)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=fill, outline=INK, width=int(dp(border)))


def game_planet(im, cx, cy, r, gid, dom=None, faded=False):
    dom = dom or next(g[2] for g in ic.GAMES if g[0] == gid)
    d = ImageDraw.Draw(im)
    disc(d, cx, cy, r, DOM[dom], 3 if r > dp(14) else 2, 4 if r > dp(14) else 2)
    k = r * 1.3 / 100; ic.draw(ic.Icon(im, cx - 50 * k, cy - 50 * k, k), gid)
    if faded:
        o = Image.new('RGBA', im.size, (0, 0, 0, 0)); ImageDraw.Draw(o).ellipse([cx - r - 2, cy - r - 2, cx + r + 2, cy + r + dp(5)], fill=(8, 12, 40, 150)); im.alpha_composite(o)


def flame(d, cx, cy, s, col=SUN):
    pts = []
    for i in range(40):
        a = 2 * math.pi * i / 40
        r = s * (0.55 + 0.45 * max(0, math.sin(a)) ** 3 * 0.9)
        pts.append((cx + math.cos(a) * s * 0.55 * (1 - 0.35 * max(0, math.sin(a))), cy - math.sin(a) * r))
    d.polygon(pts, fill=col)


def shield(d, cx, cy, s, col=(200, 212, 230)):
    pts = [(cx, cy - s), (cx + s * 0.8, cy - s * 0.65), (cx + s * 0.75, cy + s * 0.1), (cx, cy + s), (cx - s * 0.75, cy + s * 0.1), (cx - s * 0.8, cy - s * 0.65)]
    d.polygon([(x, y + dp(2)) for x, y in pts], fill=INK); d.polygon(pts, fill=col, outline=INK, width=int(dp(2)))


def header(im, title=None):
    d = ImageDraw.Draw(im); y = dp(30)
    shield(d, dp(32), y, dp(12))
    x = dp(50)
    for txt, col in (('Plata', WHITE), ('  ·  ', DIM), ('34 nivel', WHITE), ('  ·  ', DIM)):
        d.text((x, y), txt, font=F(15, False), fill=col, anchor='lm'); x += d.textlength(txt, font=F(15, False))
    flame(d, x + dp(8), y + dp(2), dp(9)); d.text((x + dp(18), y), '12', font=F(15), fill=SUN, anchor='lm')
    d.text((W - dp(20), y), 'Desafíos 2/3', font=F(13), fill=SUN, anchor='rm')
    if title: d.text((dp(20), dp(66)), title, font=F(28), fill=WHITE, anchor='lm')


def play_tri(d, cx, cy, s, col=INK):
    d.polygon([(cx - s * 0.45, cy - s * 0.6), (cx + s * 0.6, cy), (cx - s * 0.45, cy + s * 0.6)], fill=col)


def action(im, label, gid, name, y=None, mission=True):
    """Acción de hoy (la de ahora): línea de la Bitácora + botón de arcilla."""
    d = ImageDraw.Draw(im); y = y or H - dp(250)
    if mission:
        game_planet(im, dp(35), y + dp(12), dp(13), 'bitacora')
        d = ImageDraw.Draw(im); d.text((dp(56), y + dp(12)), 'Bitácora: la transmisión del día llega al empezar', font=F(13, False), fill=DIM, anchor='lm')
        y += dp(34)
    d.text((dp(24), y + dp(8)), label, font=F(13, False), fill=DIM, anchor='lm'); y += dp(22)
    x0, x1, h = dp(20), W - dp(20), dp(58)
    d.rounded_rectangle([x0, y + dp(5), x1, y + h + dp(5)], radius=dp(20), fill=INK)
    d.rounded_rectangle([x0, y, x1, y + h], radius=dp(20), fill=SUN, outline=INK, width=int(dp(3)))
    game_planet(im, x0 + dp(36), y + h / 2, dp(17), gid)
    d = ImageDraw.Draw(im); d.text((x0 + dp(64), y + h / 2), name, font=F(20), fill=INK, anchor='lm')
    play_tri(d, x1 - dp(32), y + h / 2, dp(16))


def navbar(im, sel=0):
    d = ImageDraw.Draw(im); y0 = H - dp(86); x0, x1 = dp(14), W - dp(14)
    d.rounded_rectangle([x0, y0 + dp(4), x1, y0 + dp(72)], radius=dp(30), fill=INK)
    d.rounded_rectangle([x0, y0, x1, y0 + dp(68)], radius=dp(30), fill=WHITE, outline=INK, width=int(dp(3)))
    labels = ['Hoy', 'Juegos', '', 'Liga', 'Perfil']
    for i, lb in enumerate(labels):
        cx = x0 + (x1 - x0) * (i + 0.5) / 5
        if i == 2:
            disc(d, cx, y0 + dp(18), dp(32), SUN, 3, 4); play_tri(d, cx + dp(3), y0 + dp(18), dp(24)); continue
        col = CORAL if i == sel else (107, 103, 144)
        if i == sel: d.rounded_rectangle([cx - dp(26), y0 + dp(10), cx + dp(26), y0 + dp(40)], radius=dp(15), fill=(255, 225, 216))
        s = dp(10); cy = y0 + dp(25)
        if i == 0: d.polygon([(cx, cy - s), (cx + s, cy), (cx + s * .7, cy), (cx + s * .7, cy + s), (cx - s * .7, cy + s), (cx - s * .7, cy), (cx - s, cy)], fill=col)
        elif i == 1: d.rounded_rectangle([cx - s, cy - s * .6, cx + s, cy + s * .6], radius=dp(5), fill=col)
        elif i == 3: d.polygon([(cx - s * .8, cy - s), (cx + s * .8, cy - s), (cx + s * .5, cy + s * .2), (cx - s * .5, cy + s * .2)], fill=col); d.rectangle([cx - dp(2), cy, cx + dp(2), cy + s], fill=col)
        else: d.ellipse([cx - s * .5, cy - s, cx + s * .5, cy], fill=col); d.pieslice([cx - s, cy + dp(1), cx + s, cy + s * 2], 180, 360, fill=col)
        d.text((cx, y0 + dp(52)), lb, font=F(11, i == sel), fill=INK if i == sel else (107, 103, 144), anchor='mm')


def note(im, x, y, text, anchor='la'):
    """Nota de movimiento (no es parte de la app): cursiva sol con una flechita curva."""
    d = ImageDraw.Draw(im); f = F(12, False)
    w = d.textlength(text, font=f) + dp(20)
    x0 = x if anchor[0] == 'l' else x - w if anchor[0] == 'r' else x - w / 2
    cy = y + dp(8); r = dp(6)
    d.arc([x0, cy - r, x0 + 2 * r, cy + r], 40, 320, fill=SUN, width=int(dp(2)))
    d.polygon([(x0 + 2 * r - dp(1), cy - dp(6)), (x0 + 2 * r + dp(4), cy - dp(1)), (x0 + 2 * r - dp(5), cy + dp(1))], fill=SUN)
    d.text((x0 + dp(20), y), text, font=f, fill=SUN)


def wrap(d, x, y, s, font, fill, maxw, lh=1.3, anchor='la'):
    cur = ''; lines = []
    for w_ in s.split():
        n = (cur + ' ' + w_).strip()
        if d.textlength(n, font=font) <= maxw: cur = n
        else: lines.append(cur); cur = w_
    lines.append(cur)
    for ln in lines:
        if anchor == 'center': d.text((x - d.textlength(ln, font=font) / 2, y), ln, font=font, fill=fill)
        else: d.text((x, y), ln, font=font, fill=fill)
        y += font.size * lh
    return y


def check(d, cx, cy, s, col=INK):
    d.line([(cx - s * .5, cy), (cx - s * .12, cy + s * .4), (cx + s * .55, cy - s * .45)], fill=col, width=int(max(2, s * 0.28)), joint='curve')


# ------------------------------------------------------------------ 1. Tu planeta

DEFAULT_GROWS = {'memoria': 1.0, 'atencion': 0.75, 'razonamiento': 0.55, 'lenguaje': 0.2, 'calculo': 0.6, 'velocidad': 0.85}


def blob_planet(im, cx, cy, r, grows=None, spark=None):
    """Planeta propio de arcilla con 6 zonas (una por dominio) que crecen al entrenar. [spark] = zona que acaba de
    crecer (brilla con destellos)."""
    grows = grows or DEFAULT_GROWS
    d = ImageDraw.Draw(im)
    glow(im, cx, cy, r * 1.5, SKY, 70)
    d = ImageDraw.Draw(im)
    d.ellipse([cx - r, cy - r + dp(8), cx + r, cy + r + dp(8)], fill=INK)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(58, 70, 150), outline=INK, width=int(dp(4)))
    # zonas: casquetes de color en el borde, cada uno con sus "construcciones"
    zones = [(k, ang, grows[k]) for k, ang in (('memoria', -35), ('atencion', 35), ('razonamiento', 105), ('lenguaje', 180), ('calculo', 250), ('velocidad', 0))]
    rnd = random.Random(4)
    for dom, ang, grow in zones:
        if dom == 'velocidad':
            zx, zy = cx, cy   # el centro
        else:
            a = math.radians(ang - 90); zx, zy = cx + math.cos(a) * r * 0.56, cy + math.sin(a) * r * 0.56
        zr = r * (0.2 + 0.13 * grow)
        if dom == spark:
            glow(im, zx, zy, zr * 1.9, SUN, 150); d = ImageDraw.Draw(im)
        pts = [(zx + math.cos(t) * zr * (1 + 0.12 * math.sin(3 * t + ang)), zy + math.sin(t) * zr * 0.8 * (1 + 0.12 * math.cos(2 * t + ang))) for t in [2 * math.pi * i / 36 for i in range(36)]]
        d.polygon([(x, y + dp(3)) for x, y in pts], fill=INK)
        d.polygon(pts, fill=DOM[dom], outline=INK, width=int(dp(2)))
        n = 1 + int(grow * 3)
        for k in range(n):
            bx = zx + (k - (n - 1) / 2) * zr * 0.5; by = zy + zr * 0.25
            h = dp(9 + 11 * grow * (0.8 + 0.4 * rnd.random()))
            if dom == 'memoria':   # cristales
                d.polygon([(bx, by - h), (bx + h * .35, by - h * .35), (bx, by), (bx - h * .35, by - h * .35)], fill=(190, 222, 255), outline=INK, width=int(dp(1.5)))
            elif dom == 'lenguaje':  # árbol
                d.rectangle([bx - dp(1.5), by - h * .3, bx + dp(1.5), by], fill=INK)
                d.ellipse([bx - h * .4, by - h, bx + h * .4, by - h * .25], fill=(150, 240, 190), outline=INK, width=int(dp(1.5)))
            elif dom == 'atencion':  # faros
                d.rectangle([bx - h * .15, by - h * .75, bx + h * .15, by], fill=CREAM, outline=INK, width=int(dp(1.5)))
                d.ellipse([bx - h * .22, by - h, bx + h * .22, by - h * .6], fill=SUN, outline=INK, width=int(dp(1.5)))
            elif dom == 'razonamiento':  # torres
                d.polygon([(bx - h * .25, by), (bx - h * .15, by - h * .65), (bx, by - h), (bx + h * .15, by - h * .65), (bx + h * .25, by)], fill=(225, 212, 255), outline=INK, width=int(dp(1.5)))
            elif dom == 'calculo':  # domos
                d.pieslice([bx - h * .45, by - h * .8, bx + h * .45, by + h * .1], 180, 360, fill=(170, 245, 230), outline=INK, width=int(dp(1.5)))
            else:  # velocidad: antenas
                d.line([(bx, by), (bx, by - h * .7)], fill=INK, width=int(dp(2)))
                d.ellipse([bx - h * .2, by - h, bx + h * .2, by - h * .6], fill=(255, 170, 185), outline=INK, width=int(dp(1.5)))
    hl = Image.new('RGBA', im.size, (0, 0, 0, 0)); ImageDraw.Draw(hl).ellipse([cx - r * .72, cy - r * .82, cx - r * .38, cy - r * .62], fill=(255, 255, 255, 70)); im.alpha_composite(hl)



def p1():
    im = sky(11); header(im, 'Tu planeta')
    d = ImageDraw.Draw(im)
    cx, cy, r = W / 2, dp(300), dp(122)
    # órbita con las 3 naves del día (1 ya aterrizó)
    d.ellipse([cx - r * 1.45, cy - r * 0.55, cx + r * 1.45, cy + r * 0.55], outline=(255, 255, 255, 60), width=int(dp(2)))
    blob_planet(im, cx, cy, r)
    for gid, ang, done in (('stroop', 200, True), ('radar', 330, False), ('rutatesoro', 30, False)):
        a = math.radians(ang)
        x, y = cx + math.cos(a) * r * 1.45, cy + math.sin(a) * r * 0.55
        if done:
            x, y = cx - r * 0.35, cy - r * 0.95
        game_planet(im, x, y, dp(17), gid, faded=False)
        d = ImageDraw.Draw(im)
        if done:
            disc(d, x + dp(14), y - dp(14), dp(8), LIME, 2, 1); check(d, x + dp(14), y - dp(14), dp(8))
    note(im, W - dp(16), cy + r * 0.95, 'gira despacio', 'ra')
    d = ImageDraw.Draw(im)
    y = dp(468)
    d.text((dp(24), y), 'Esta semana creció', font=F(13, False), fill=DIM)
    d.text((dp(24), y + dp(17)), 'tu zona de Memoria', font=F(20), fill=(150, 190, 255))
    d.text((W - dp(24), y), 'Quieta hace 5 días', font=F(13, False), fill=DIM, anchor='ra')
    d.text((W - dp(24), y + dp(17)), 'Lenguaje', font=F(20), fill=(120, 230, 170), anchor='ra')
    d.line([(dp(24), y + dp(52)), (W - dp(24), y + dp(52))], fill=(255, 255, 255, 40), width=int(dp(1)))
    d.text((W / 2, y + dp(66)), 'Toca una zona para ver sus juegos', font=F(12, False), fill=SOFT, anchor='mm')
    action(im, 'Tu sesión de hoy · juego 2 de 3 (cada juego hace crecer su zona)', 'radar', 'Radar')
    navbar(im); return im


# ------------------------------------------------------------------ 2. Cabina: el dato del día

def ring(d, cx, cy, r, w, segs):
    for i, (col, on) in enumerate(segs):
        a0, a1 = -90 + i * 120 + 4, -90 + (i + 1) * 120 - 4
        d.arc([cx - r - dp(1), cy - r + dp(3), cx + r + dp(1), cy + r + dp(5)], a0, a1, fill=INK, width=int(w + dp(4)))
        d.arc([cx - r, cy - r, cx + r, cy + r], a0, a1, fill=col if on else (48, 58, 120), width=int(w))


def p2():
    im = sky(12); header(im, 'Hoy')
    d = ImageDraw.Draw(im)
    cx, cy, r = W / 2, dp(215), dp(92)
    glow(im, cx, cy, r * 1.2, SUN, 50); d = ImageDraw.Draw(im)
    ring(d, cx, cy, r, dp(18), [(LIME, True), (SUN, False), (SKY, False)])
    for i, gid in enumerate(('stroop', 'radar', 'rutatesoro')):
        a = math.radians(-90 + i * 120 + 60)
        game_planet(im, cx + math.cos(a) * (r + dp(34)), cy + math.sin(a) * (r + dp(34)), dp(18), gid, faded=i > 1)
    d = ImageDraw.Draw(im)
    d.text((cx, cy - dp(10)), '1 de 3', font=F(34), fill=WHITE, anchor='mm')
    d.text((cx, cy + dp(22)), 'juegos de hoy', font=F(13, False), fill=DIM, anchor='mm')
    note(im, W - dp(16), dp(96), 'el anillo se llena al jugar', 'ra')
    # descubrimiento del día
    y = dp(368)
    d = ImageDraw.Draw(im)
    d.text((dp(24), y), 'DESCUBRIMIENTO DEL DÍA', font=F(12), fill=SUN)
    y = wrap(d, dp(24), y + dp(20), 'Tu vistazo en Radar llegó a 84 ms: el más rápido de tus 6 partidas', F(19), WHITE, W - dp(48))
    # mini gráfico
    vals = [132, 121, 118, 104, 96, 84]; gx0, gx1, gy0, gy1 = dp(24), W - dp(110), y + dp(8), y + dp(62)
    pts = [(gx0 + (gx1 - gx0) * i / 5, gy1 - (140 - v) / 60 * (gy1 - gy0)) for i, v in enumerate(vals)]
    d.line(pts, fill=SKY, width=int(dp(3)), joint='curve')
    for i, (x, yy) in enumerate(pts): d.ellipse([x - dp(4), yy - dp(4), x + dp(4), yy + dp(4)], fill=SUN if i == 5 else SKY, outline=INK, width=int(dp(1.5)))
    d.text((W - dp(24), gy0 + dp(6)), 'más rápido', font=F(11, False), fill=SOFT, anchor='ra')
    d.text((W - dp(24), gy1 - dp(14)), '6 partidas', font=F(11, False), fill=SOFT, anchor='ra')
    y = gy1 + dp(14)
    wrap(d, dp(24), y, 'Truco: mira el centro y deja que el borde "llegue solo".', F(13, False), DIM, W - dp(48))
    # instrumentos (sin recuadros)
    y = dp(540); cols = [W * 1 / 6, W / 2, W * 5 / 6]
    flame(d, cols[0], y + dp(4), dp(14)); d.text((cols[0], y + dp(30)), '12 días', font=F(17), fill=WHITE, anchor='mm'); d.text((cols[0], y + dp(48)), 'racha', font=F(12, False), fill=SOFT, anchor='mm')
    shield(d, cols[1], y + dp(2), dp(14)); d.text((cols[1], y + dp(30)), '2º en Plata', font=F(17), fill=WHITE, anchor='mm'); d.text((cols[1], y + dp(48)), 'tu liga', font=F(12, False), fill=SOFT, anchor='mm')
    for k in range(3): disc(d, cols[2] - dp(16) + k * dp(16), y + dp(4), dp(6), SUN if k < 2 else (48, 58, 120), 2, 1)
    d.text((cols[2], y + dp(30)), '2 de 3', font=F(17), fill=WHITE, anchor='mm'); d.text((cols[2], y + dp(48)), 'desafíos', font=F(12, False), fill=SOFT, anchor='mm')
    action(im, 'Sigue tu sesión · juego 2 de 3', 'radar', 'Radar', y=H - dp(222), mission=False)
    navbar(im); return im


# ------------------------------------------------------------------ 3. Tu sistema (dominios en órbita)

def p3():
    im = sky(13); header(im, 'Tu sistema')
    d = ImageDraw.Draw(im)
    cx, cy = W / 2, dp(300)
    orbs = [(dp(62), dp(26)), (dp(100), dp(42)), (dp(138), dp(58)), (dp(176), dp(74))]
    for a, b in orbs: d.ellipse([cx - a, cy - b, cx + a, cy + b], outline=(255, 255, 255, 45), width=int(dp(1.5)))
    # sol = tú
    glow(im, cx, cy, dp(70), SUN, 120); d = ImageDraw.Draw(im)
    disc(d, cx, cy, dp(34), SUN, 3, 5)
    d.text((cx, cy - dp(4)), '34', font=F(24), fill=INK, anchor='mm'); d.text((cx, cy + dp(16)), 'nivel', font=F(10, False), fill=INK, anchor='mm')
    # planetas de dominio: tamaño = nivel, brillo = visitado hace poco
    doms = [('memoria', 'Memoria', 1, 150, 1.0, 'rutatesoro', True), ('atencion', 'Atención', 2, 300, 0.85, 'stroop', True),
            ('razonamiento', 'Razonamiento', 2, 62, 0.7, 'series', True), ('lenguaje', 'Lenguaje', 3, 188, 0.45, 'anagramas', False),
            ('calculo', 'Cálculo', 2, 232, 0.6, 'calculo', True), ('velocidad', 'Velocidad', 3, 345, 0.9, 'radar', True)]
    pos = {}
    for dom, name, o, ang, lv, gid, lit in doms:
        a, b = orbs[o]; t = math.radians(ang)
        pos[dom] = (cx + math.cos(t) * a, cy + math.sin(t) * b, dp(14 + 12 * lv))
    route = ['atencion', 'velocidad', 'memoria']
    pts = [pos[k][:2] for k in route]
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        n = 18
        for i in range(n):
            if i % 2: continue
            t0, t1 = i / n, (i + 1) / n
            mx, my = (x0 + x1) / 2, min(y0, y1) - dp(60)
            q = lambda t: ((1 - t) ** 2 * x0 + 2 * (1 - t) * t * mx + t * t * x1, (1 - t) ** 2 * y0 + 2 * (1 - t) * t * my + t * t * y1)
            d.line([q(t0), q(t1)], fill=SUN, width=int(dp(3)))
    for dom, name, o, ang, lv, gid, lit in sorted(doms, key=lambda z: pos[z[0]][1]):
        x, y, r = pos[dom]
        if lit: glow(im, x, y, r * 1.5, DOM[dom], 90)
        game_planet(im, x, y, r, gid, dom, faded=not lit)
        d = ImageDraw.Draw(im)
        d.text((x, y + r + dp(12)), name, font=F(12), fill=WHITE if lit else SOFT, anchor='mm')
        if dom in route:
            k = route.index(dom) + 1
            disc(d, x - r * 0.8, y - r * 0.8, dp(9), SUN, 2, 1); d.text((x - r * 0.8, y - r * 0.8), str(k), font=F(11), fill=INK, anchor='mm')
    note(im, W - dp(20), dp(410), 'los planetas orbitan', 'ra')
    d = ImageDraw.Draw(im); y = dp(448)
    d.text((dp(24), y), 'Ruta de hoy', font=F(13, False), fill=DIM)
    d.text((dp(24), y + dp(18)), 'Atención  ›  Velocidad  ›  Memoria', font=F(19), fill=WHITE)
    wrap(d, dp(24), y + dp(50), 'Lenguaje está lejos del sol: no lo visitas hace 6 días. Mañana entra en tu ruta.', F(14, False), SOFT, W - dp(48))
    action(im, 'Tu sesión de hoy · 3 juegos', 'stroop', 'Tinta o Palabra')
    navbar(im); return im


# ------------------------------------------------------------------ 4. Tu constelación de la semana

def star5(d, cx, cy, r, fill, outline=INK, w=2):
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5; rr = r if i % 2 == 0 else r * 0.45
        pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))
    d.polygon([(x, y + dp(2)) for x, y in pts], fill=INK)
    d.polygon(pts, fill=fill, outline=outline, width=int(dp(w)))


def p4():
    im = sky(14); header(im, 'Tu constelación')
    d = ImageDraw.Draw(im)
    # figura de la semana: "La Nave" (7 estrellas = 7 días; se completa con 5)
    pts = [(0.50, 0.00), (0.66, 0.30), (0.62, 0.62), (0.80, 0.88), (0.38, 0.62), (0.20, 0.88), (0.34, 0.30)]
    days = ['L', 'M', 'M', 'J', 'V', 'S', 'D']
    lit = [True, True, False, True, True, False, False]
    today = 5
    ox, oy, sw, sh = dp(66), dp(110), W - dp(132), dp(300)
    P = [(ox + x * sw, oy + y * sh) for x, y in pts]
    order = [0, 1, 2, 3, 2, 4, 5, 4, 6, 0]
    for a, b in zip(order, order[1:]):
        on = lit[a] and lit[b]
        d.line([P[a], P[b]], fill=(255, 235, 170, 230) if on else (255, 255, 255, 50), width=int(dp(3 if on else 2)))
    for i, (x, y) in enumerate(P):
        if lit[i]:
            glow(im, x, y, dp(30), SUN, 110); d = ImageDraw.Draw(im); star5(d, x, y, dp(18), SUN)
        elif i == today:
            d.ellipse([x - dp(20), y - dp(20), x + dp(20), y + dp(20)], outline=SUN, width=int(dp(3)))
            star5(d, x, y, dp(12), (70, 80, 150), (120, 130, 200), 1)
        else:
            star5(d, x, y, dp(10), (60, 70, 140), (110, 120, 190), 1)
        d.text((x + dp(24), y), days[i], font=F(12), fill=SUN if i == today else SOFT, anchor='lm')
    note(im, W / 2, oy + sh + dp(26), 'las estrellas titilan; las líneas se dibujan al jugar', 'ma')
    y = oy + sh + dp(56)
    d = ImageDraw.Draw(im)
    d.text((W / 2, y), '«La Nave»: 4 de 5 estrellas', font=F(22), fill=WHITE, anchor='ma')
    wrap(d, W / 2, y + dp(32), 'Juega hoy y la completas. No hace falta los 7 días: con 5 queda en tu cielo.', F(14, False), DIM, W - dp(70), anchor='center')
    # colección de semanas anteriores
    y2 = y + dp(96)
    d.text((dp(24), y2), 'Tu cielo · 3 semanas completas seguidas', font=F(13, False), fill=SOFT)
    rnd = random.Random(5)
    for k in range(5):
        bx = dp(34) + k * dp(74); by = y2 + dp(26)
        m = [(bx + rnd.random() * dp(50), by + rnd.random() * dp(34)) for _ in range(5)]
        full = k < 3
        d.line(m, fill=(255, 235, 170, 200) if full else (255, 255, 255, 50), width=int(dp(1.5)))
        for (x, yy) in m: d.ellipse([x - dp(3), yy - dp(3), x + dp(3), yy + dp(3)], fill=SUN if full else (110, 120, 190))
    action(im, 'Tu sesión de hoy · 3 juegos = la estrella del sábado', 'freno', 'Freno de Emergencia', y=H - dp(222), mission=False)
    navbar(im); return im


# ------------------------------------------------------------------ 5. Tu evolución (diario de descubrimientos)

def spark(d, x0, y0, w, h, vals, col, lower_is_better=False):
    lo, hi = min(vals), max(vals)
    pts = [(x0 + w * i / (len(vals) - 1), y0 + h - (v - lo) / (hi - lo or 1) * h if not lower_is_better else y0 + (v - lo) / (hi - lo or 1) * h) for i, v in enumerate(vals)]
    d.line(pts, fill=col, width=int(dp(2.5)), joint='curve')
    x, y = pts[-1]; d.ellipse([x - dp(4), y - dp(4), x + dp(4), y + dp(4)], fill=SUN, outline=INK, width=int(dp(1.5)))


def p5():
    im = sky(15); header(im, 'Tu evolución')
    d = ImageDraw.Draw(im); y = dp(100)
    items = [
        ('freno', 'Tu freno', '212 ms', 'antes 248 ms · 7 partidas', [248, 240, 236, 229, 222, 215, 212], True, 'Récord'),
        ('trafico', 'Tu carga en Tráfico', '5 cápsulas', 'a la vez, sin errores', [3, 3, 4, 4, 4, 5], False, None),
        ('aterrizaje', 'Tu estimación', 'a 3,8% del blanco', 'mejor en el centro de la regla', [6.2, 5.9, 5.1, 4.8, 4.1, 3.8], True, None),
    ]
    for gid, title, big, sub, vals, lower, badge in items:
        game_planet(im, dp(44), y + dp(26), dp(22), gid); d = ImageDraw.Draw(im)
        d.text((dp(78), y + dp(4)), title, font=F(13, False), fill=DIM)
        d.text((dp(78), y + dp(22)), big, font=F(21), fill=WHITE)
        d.text((dp(78), y + dp(50)), sub, font=F(12, False), fill=SOFT)
        spark(d, W - dp(118), y + dp(12), dp(92), dp(36), vals, LIME if lower else SKY, lower)
        if badge:
            d.rounded_rectangle([W - dp(92), y - dp(8), W - dp(28), y + dp(10)], radius=dp(9), fill=SUN, outline=INK, width=int(dp(2)))
            d.text((W - dp(60), y + dp(1)), badge, font=F(11), fill=INK, anchor='mm')
        d.line([(dp(78), y + dp(76)), (W - dp(24), y + dp(76))], fill=(255, 255, 255, 30), width=int(dp(1)))
        y += dp(92)
    # bitácora: colección
    game_planet(im, dp(44), y + dp(26), dp(22), 'bitacora'); d = ImageDraw.Draw(im)
    d.text((dp(78), y + dp(4)), 'Tu bitácora', font=F(13, False), fill=DIM)
    d.text((dp(78), y + dp(22)), '14 hallazgos', font=F(21), fill=WHITE)
    d.text((dp(78), y + dp(50)), 'recordaste 5 de 6 ayer, al día siguiente', font=F(12, False), fill=SOFT)
    for k in range(6):
        disc(d, W - dp(110) + k * dp(16), y + dp(28), dp(6), (170, 210, 255) if k < 5 else (60, 70, 140), 2, 1)
    y += dp(92)
    note(im, W / 2, y - dp(6), 'desliza: debajo, lo de semanas anteriores', 'ma')
    action(im, 'Tu sesión de hoy · 3 juegos', 'freno', 'Freno de Emergencia', y=H - dp(222), mission=False)
    navbar(im); return im


# ------------------------------------------------------------------ 6. El camino, pero con contenido

def p6():
    im = sky(16); header(im, 'Tu camino')
    d = ImageDraw.Draw(im)
    rows = [  # (y, x, tipo, texto, sub)
        (dp(130), 0.62, 'flag', 'Racha de 14 días', 'faltan 2 · premio: nave nueva'),
        (dp(215), 0.40, 'future', 'Mañana', 'vuelve Rumbo a Casa'),
        (dp(330), 0.55, 'today', None, None),
        (dp(470), 0.38, 'past', 'Ayer', 'Récord en Freno · 212 ms'),
        (dp(570), 0.60, 'past2', 'Lunes', 'Subiste a Plata'),
    ]
    xs = [r[1] * W for r in rows]; ys = [r[0] for r in rows]
    for i in range(len(rows) - 1):
        n = 14
        for k in range(n):
            if k % 2: continue
            t0, t1 = k / n, (k + 1) / n
            f = lambda t: (xs[i] + (xs[i + 1] - xs[i]) * (3 * t * t - 2 * t ** 3), ys[i] + (ys[i + 1] - ys[i]) * t)
            col = (255, 255, 255, 70) if i < 2 else (LIME if i >= 2 else SUN)
            d.line([f(t0), f(t1)], fill=col, width=int(dp(4)))
    for (y, fx, kind, t, s), x in zip(rows, xs):
        left = fx < 0.5; tx = x + dp(48) if left else x - dp(48); anc = 'la' if left else 'ra'
        if kind == 'flag':
            disc(d, x, y, dp(24), CORAL, 3, 4); d.polygon([(x - dp(6), y - dp(10)), (x + dp(10), y - dp(4)), (x - dp(6), y + dp(2))], fill=WHITE); d.line([(x - dp(6), y - dp(12)), (x - dp(6), y + dp(12))], fill=WHITE, width=int(dp(3)))
        elif kind == 'future':
            game_planet(im, x, y, dp(18), 'rumbo', faded=True); d = ImageDraw.Draw(im)
        elif kind == 'today':
            glow(im, x, y, dp(90), SUN, 70); d = ImageDraw.Draw(im)
            d.ellipse([x - dp(64), y - dp(64), x + dp(64), y + dp(64)], outline=SUN + (150,), width=int(dp(3)))
            disc(d, x, y, dp(34), SUN, 3, 5); play_tri(d, x + dp(4), y, dp(26))
            for k, (gid, done) in enumerate((('stroop', True), ('radar', False), ('rutatesoro', False))):
                a = math.radians(200 + k * 70)
                game_planet(im, x + math.cos(a) * dp(64), y + math.sin(a) * dp(64), dp(17), gid, faded=not done); d = ImageDraw.Draw(im)
                if done:
                    gx, gy = x + math.cos(a) * dp(64), y + math.sin(a) * dp(64)
                    disc(d, gx + dp(13), gy - dp(13), dp(7), LIME, 2, 1); check(d, gx + dp(13), gy - dp(13), dp(7))
            d.text((x - dp(84), y + dp(62)), 'Hoy · 1 de 3', font=F(20), fill=WHITE, anchor='ra')
            d.text((x - dp(84), y + dp(86)), 'sigue: Radar', font=F(13, False), fill=DIM, anchor='ra')
            continue
        else:
            gids = ('freno', 'series', 'parejas') if kind == 'past' else ('trafico', 'calculo', 'anagramas')
            for k, gid in enumerate(gids):
                game_planet(im, x + (k - 1) * dp(26), y, dp(14), gid); d = ImageDraw.Draw(im)
            if kind == 'past': star5(d, x + dp(40), y - dp(18), dp(10), SUN)
            else: shield(d, x - dp(40), y - dp(18), dp(10))
            tx = x + dp(62) if left else x - dp(62)
        d.text((tx, y - dp(12)), t, font=F(17), fill=WHITE, anchor=anc)
        d.text((tx, y + dp(10)), s, font=F(12, False), fill=SUN if kind in ('past', 'flag') else DIM, anchor=anc)
    note(im, dp(20), dp(640), 'el camino avanza hacia el horizonte (como hoy)', 'la')
    action(im, 'Tu sesión de hoy · juego 2 de 3', 'radar', 'Radar', y=H - dp(222), mission=False)
    navbar(im); return im


if __name__ == '__main__':
    shots = [p1(), p2(), p3(), p4(), p5(), p6()]
    names = ['Tu planeta', 'El dato del día', 'Tu sistema', 'Tu constelación', 'Tu evolución', 'El camino, con contenido']
    os.makedirs(OUT, exist_ok=True)
    for i, s in enumerate(shots, 1):
        s.convert('RGB').resize((W * 3 // 4, H * 3 // 4), Image.LANCZOS).save(f'{OUT}/inicio-propuesta-{i}.png')
    k = 0.5; sw, sh = int(W * k), int(H * k); gap = 30; cap = 60
    sheet = Image.new('RGB', (3 * sw + 4 * gap, 2 * (sh + cap) + 3 * gap), (2, 3, 16)); sd = ImageDraw.Draw(sheet)
    for i, s in enumerate(shots):
        x = gap + (i % 3) * (sw + gap); y = gap + (i // 3) * (sh + cap + gap)
        sd.text((x + sw / 2, y + cap / 2), f'{i + 1}. {names[i]}', font=ImageFont.truetype(FB, 30), fill=WHITE, anchor='mm')
        sheet.paste(s.convert('RGB').resize((sw, sh), Image.LANCZOS), (x, y + cap))
    sheet.save(f'{OUT}/inicio-propuestas.png')
    print('OK')
