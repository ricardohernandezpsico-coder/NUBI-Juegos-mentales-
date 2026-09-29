# Nubi al centro con su halo de áreas (29-sep). Ricardo descartó la galaxia ("estrellas difuminadas de colores, aunque
# tengan el nombre abajo, no dan a entender el concepto; el razonamiento no tiene una matriz lógica") y pidió reforzar a
# Nubi, con su lámina de referencia "protagonismo central y halo de progreso". Maqueta PIL, NO la app.
# La matriz lógica (una sola idea por cosa que se ve):
#   · ORBE = un área, siempre con su ÍCONO y su NOMBRE (nunca solo color).
#   · ANILLO del orbe = su avance: 5 tramos = 5 etapas (Inicio…Maestro); el tramo actual se llena. Nunca baja.
#   · Debajo del nombre, la ETAPA en palabras.
#   · SELLO sol "hoy" = área jugada hoy. ARO SOL punteado = la que Nubi propone hoy.
#   · NUBI = quien explica: su globo dice qué pasa; al tocar un orbe, lo mira y lo explica.
#   · Nubi CRECE CONTIGO (idea de Finch): con las partidas acumuladas gana brillo y estrellas; nunca baja ni se pone triste.
# Uso: python3 tools/previews/nubi_halo.py  ->  docs/previews/nubi-halo.png
import os, math, importlib.util
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('al', os.path.join(HERE, 'anillo_luz.py'))
AL = importlib.util.module_from_spec(spec); spec.loader.exec_module(AL)
NM, NS, N, P = AL.NM, AL.NS, AL.N, AL.P
F, dp, W = P.F, P.dp, P.W
INK, SUN, CREAM, WHITE, DIM, LIME, CORAL = P.INK, P.SUN, P.CREAM, P.WHITE, P.DIM, P.LIME, P.CORAL
LINE, COL, LIGHT, NAMES, ov = NS.LINE, AL.COL, AL.LIGHT, AL.NAMES, AL.ov
STAGE = {'memoria': 3, 'atencion': 3, 'razonamiento': 2, 'lenguaje': 1, 'calculo': 2, 'velocidad': 4}
FRAC = {'memoria': 0.6, 'atencion': 0.35, 'razonamiento': 0.8, 'lenguaje': 0.3, 'calculo': 0.5, 'velocidad': 0.2}
STAGE_NAME = ['Inicio', 'Base', 'Intermedio', 'Avanzado', 'Maestro']
TODAY = {'memoria', 'velocidad'}
ZONE = 'atencion'
# posición en el halo (grados; 0 = derecha, en sentido horario)
SLOT = {'memoria': -120, 'atencion': -60, 'razonamiento': 0, 'calculo': 60, 'lenguaje': 120, 'velocidad': 180}


# ---------- íconos de área (blancos, simples, se reconocen sin color) ----------
def glyph(d, key, x, y, s):
    wl = max(2, int(s * 0.11))
    if key == 'memoria':      # cristal
        pts = [(x, y - s * 0.62), (x + s * 0.48, y - s * 0.12), (x, y + s * 0.62), (x - s * 0.48, y - s * 0.12)]
        d.polygon(pts, fill=WHITE, outline=LINE, width=wl)
        d.line([(x - s * 0.48, y - s * 0.12), (x + s * 0.48, y - s * 0.12)], fill=LINE, width=wl)
        d.line([(x, y - s * 0.62), (x - s * 0.16, y - s * 0.12), (x, y + s * 0.62)], fill=LINE, width=wl)
    elif key == 'atencion':   # ojo
        pts = [(x - s * 0.62 + s * 1.24 * t / 30, y - math.sin(math.pi * t / 30) * s * 0.36) for t in range(31)]
        pts += [(x + s * 0.62 - s * 1.24 * t / 30, y + math.sin(math.pi * t / 30) * s * 0.36) for t in range(31)]
        d.polygon(pts, fill=WHITE, outline=LINE, width=wl)
        d.ellipse([x - s * 0.24, y - s * 0.24, x + s * 0.24, y + s * 0.24], fill=LINE)
        d.ellipse([x - s * 0.13, y - s * 0.17, x - s * 0.02, y - s * 0.06], fill=WHITE)
    elif key == 'razonamiento':   # pieza de rompecabezas
        a = s * 0.42
        d.rounded_rectangle([x - a, y - a + s * 0.08, x + a - s * 0.1, y + a], radius=s * 0.08, fill=WHITE, outline=LINE, width=wl)
        d.ellipse([x - s * 0.17, y - a - s * 0.14, x + s * 0.13, y - a + s * 0.16], fill=WHITE, outline=LINE, width=wl)
        d.ellipse([x + a - s * 0.22, y - s * 0.11, x + a + s * 0.12, y + s * 0.21], fill=WHITE, outline=LINE, width=wl)
        d.rectangle([x - s * 0.13, y - a + s * 0.1, x + s * 0.09, y - a + s * 0.22], fill=WHITE)
        d.rectangle([x + a - s * 0.2, y - s * 0.07, x + a - s * 0.06, y + s * 0.17], fill=WHITE)
    elif key == 'lenguaje':   # globo de diálogo con Aa
        d.rounded_rectangle([x - s * 0.6, y - s * 0.46, x + s * 0.6, y + s * 0.3], radius=s * 0.22, fill=WHITE, outline=LINE, width=wl)
        d.polygon([(x - s * 0.28, y + s * 0.26), (x - s * 0.34, y + s * 0.58), (x - s * 0.04, y + s * 0.28)], fill=WHITE)
        d.line([(x - s * 0.28, y + s * 0.3), (x - s * 0.34, y + s * 0.58), (x - s * 0.04, y + s * 0.3)], fill=LINE, width=wl)
        d.text((x, y - s * 0.07), 'Aa', font=ImageFont_size(s * 0.52), fill=LINE, anchor='mm')
    elif key == 'calculo':    # + − × ÷ en cuadrícula
        q = s * 0.26; t = max(2, int(s * 0.13))
        for (cx, cy, k) in [(x - q, y - q, '+'), (x + q, y - q, '-'), (x - q, y + q, 'x'), (x + q, y + q, '=')]:
            r = s * 0.17
            if k == '+': d.line([(cx - r, cy), (cx + r, cy)], fill=WHITE, width=t); d.line([(cx, cy - r), (cx, cy + r)], fill=WHITE, width=t)
            if k == '-': d.line([(cx - r, cy), (cx + r, cy)], fill=WHITE, width=t)
            if k == 'x': d.line([(cx - r * 0.8, cy - r * 0.8), (cx + r * 0.8, cy + r * 0.8)], fill=WHITE, width=t); d.line([(cx - r * 0.8, cy + r * 0.8), (cx + r * 0.8, cy - r * 0.8)], fill=WHITE, width=t)
            if k == '=': d.line([(cx - r, cy - r * 0.4), (cx + r, cy - r * 0.4)], fill=WHITE, width=t); d.line([(cx - r, cy + r * 0.4), (cx + r, cy + r * 0.4)], fill=WHITE, width=t)
    elif key == 'velocidad':  # rayo
        pts = [(x + s * 0.12, y - s * 0.62), (x - s * 0.38, y + s * 0.08), (x - s * 0.02, y + s * 0.08),
               (x - s * 0.14, y + s * 0.62), (x + s * 0.38, y - s * 0.1), (x + s * 0.03, y - s * 0.1)]
        d.polygon(pts, fill=WHITE, outline=LINE, width=wl)


def ImageFont_size(px):
    from PIL import ImageFont
    return ImageFont.truetype(P.FB, int(px))


# ---------- orbe ----------
def orb(im, x, y, r, key, stage, frac, today=False, zone=False, burst=False, dimmed=False):
    col = COL[key]
    NS.glow(im, x, y, r * (2.2 if burst else 1.6), LIGHT[key], 200 if burst else 110)
    if zone:
        def f(d):
            R = r + dp(17)
            for i in range(24):
                if i % 2: continue
                d.arc([x - R, y - R, x + R, y + R], 360 * i / 24, 360 * (i + 1) / 24, fill=SUN + (240,), width=int(dp(2.2)))
        ov(im, f)
    # anillo de avance: 5 tramos = 5 etapas
    R = r + dp(8); th = dp(5)
    for i in range(5):
        a0 = -90 + i * 72 + 5; a1 = a0 + 62
        ov(im, lambda d, a0=a0, a1=a1: d.arc([x - R, y - R, x + R, y + R], a0, a1, fill=(255, 255, 255, 46), width=int(th)))
        fill = 1.0 if i < stage - 1 else (frac if i == stage - 1 else 0)
        if fill > 0:
            ov(im, lambda d, a0=a0, a1=a1, fill=fill: d.arc([x - R, y - R, x + R, y + R], a0, a0 + (a1 - a0) * fill, fill=LIGHT[key] + (255,), width=int(th)))
    # esfera de color con volumen
    S = int(r * 2 + 4); sph = Image.new('RGBA', (S * 4, S * 4), (0, 0, 0, 0)); sd = ImageDraw.Draw(sph)
    base = tuple(int(c * 0.55 + 90 * 0.45) for c in col) if dimmed else col
    for k in range(40, 0, -1):
        t = k / 40; rr = S * 2 * t * 0.98
        cx, cy = S * 2 - (1 - t) * S * 0.5, S * 2 - (1 - t) * S * 0.6
        c = tuple(int(b * (0.7 + 0.5 * (1 - t)) if (1 - t) < 0.6 else min(255, b + (255 - b) * ((1 - t) - 0.6) * 1.4)) for b in base)
        sd.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=c + (255,))
    mask = Image.new('L', (S * 4, S * 4), 0); ImageDraw.Draw(mask).ellipse([4, 4, S * 4 - 4, S * 4 - 4], fill=255)
    sph.putalpha(mask); sph = sph.resize((S, S), Image.LANCZOS)
    im.alpha_composite(sph, (int(x - S / 2), int(y - S / 2)))
    d = ImageDraw.Draw(im)
    d.ellipse([x - r, y - r, x + r, y + r], outline=LINE, width=int(dp(2)))
    ov(im, lambda dd: dd.ellipse([x - r * 0.62, y - r * 0.78, x - r * 0.05, y - r * 0.42], fill=(255, 255, 255, 110)))
    glyph(ImageDraw.Draw(im), key, x, y + r * 0.04, r * 0.95)
    if today:   # sello "hoy"
        sx, sy = x + r * 0.78, y - r * 0.78; rs = dp(10)
        d = ImageDraw.Draw(im)
        d.ellipse([sx - rs, sy - rs + dp(1.5), sx + rs, sy + rs + dp(1.5)], fill=INK)
        d.ellipse([sx - rs, sy - rs, sx + rs, sy + rs], fill=SUN, outline=INK, width=int(dp(1.6)))
        d.line([(sx - rs * 0.45, sy), (sx - rs * 0.1, sy + rs * 0.4), (sx + rs * 0.5, sy - rs * 0.35)], fill=INK, width=int(dp(2)), joint='curve')


def label(im, x, y, key, above=False, extra=None):
    d = ImageDraw.Draw(im)
    name = NAMES[key]; sub = extra or STAGE_NAME[STAGE[key] - 1]
    if above:
        d.text((x, y - dp(20)), name, font=F(14), fill=SUN if key == ZONE else WHITE, anchor='mm')
        d.text((x, y - dp(3)), sub, font=F(13, False), fill=DIM, anchor='mm')
    else:
        d.text((x, y + dp(3)), name, font=F(14), fill=SUN if key == ZONE else WHITE, anchor='mm')
        d.text((x, y + dp(20)), sub, font=F(13, False), fill=DIM, anchor='mm')


def nubi_sprite(pose, size, scale=1.0):
    fn = {'hola': lambda c, x, y, k: NS.nubi(c, x, y, k, 'hola'), 'coach': lambda c, x, y, k: NS.nubi(c, x, y, k, 'coach'),
          'celebra': NS.pose_celebra}[pose]
    return NM.nubi_sprite(fn, size, scale)


def bubble(im, cx, y, l1, l2, tail_x=None, w=None):
    d = ImageDraw.Draw(im); f1, f2 = F(13, False), F(16)
    w = w or max(d.textlength(l1, font=f1), d.textlength(l2, font=f2)) + dp(28); h = dp(56)
    x0 = cx - w / 2
    d.rounded_rectangle([x0, y + dp(3), x0 + w, y + h + dp(3)], radius=dp(18), fill=LINE)
    d.rounded_rectangle([x0, y, x0 + w, y + h], radius=dp(18), fill=(252, 250, 255), outline=LINE, width=int(dp(2)))
    tx = tail_x or cx
    d.polygon([(tx - dp(8), y + h - dp(2)), (tx + dp(8), y + h - dp(2)), (tx, y + h + dp(12))], fill=(252, 250, 255))
    d.line([(tx - dp(8), y + h), (tx, y + h + dp(12)), (tx + dp(8), y + h)], fill=LINE, width=int(dp(2)))
    d.text((x0 + dp(14), y + dp(9)), l1, font=f1, fill=(96, 88, 140))
    d.text((x0 + dp(14), y + dp(27)), l2, font=f2, fill=INK)


def screen():
    H = dp(915)
    im = NM.nebula_bg(W, int(H), 52, stars=120)
    P.header(im)
    cx, cy, rx, ry = W / 2, dp(372), dp(150), dp(168)
    pos = {}
    for key, a in SLOT.items():
        x, y = cx + math.cos(math.radians(a)) * rx, cy + math.sin(math.radians(a)) * ry
        pos[key] = (x, y)
    # hilos suaves de Nubi a cada área
    for key, (x, y) in pos.items():
        ov(im, lambda d, x=x, y=y: d.line([(cx, cy), (x, y)], fill=(210, 220, 255, 30), width=int(dp(2))))
    im.alpha_composite(nubi_sprite('hola', dp(178)), (int(cx - dp(89)), int(cy - dp(89))))
    for key, (x, y) in pos.items():
        orb(im, x, y, dp(28), key, STAGE[key], FRAC[key], today=key in TODAY, zone=key == ZONE)
        label(im, x, y + (-dp(46) if y < cy - dp(20) else dp(48)), key, above=y < cy - dp(20))
    bubble(im, cx, dp(92), 'Nubi', 'Hoy te propongo Atención', tail_x=cx)
    # tu semana
    d = ImageDraw.Draw(im); y = dp(640)
    d.text((dp(24), y), 'Tu semana', font=F(15), fill=WHITE)
    d.text((W - dp(24), y), '5 de 7 días', font=F(14, False), fill=DIM, anchor='ra')
    for i, pl in enumerate([1, 1, 0, 1, 1, 1, 0]):
        x = dp(40) + i * (W - dp(80)) / 6; yy = y + dp(42); r = dp(9) if i != 5 else dp(11)
        if pl:
            NS.glow(im, x, yy, r * 2.2, SUN, 110); d = ImageDraw.Draw(im)
            d.polygon(N.star(x, yy, r * 1.25, inner=0.5), fill=SUN, outline=INK)
        else:
            ov(im, lambda dd, x=x, yy=yy, r=r: dd.ellipse([x - r * 0.6, yy - r * 0.6, x + r * 0.6, yy + r * 0.6], outline=(255, 255, 255, 110), width=int(dp(1.5))))
        d = ImageDraw.Draw(im)
        d.text((x, yy + dp(22)), 'LMMJVSD'[i], font=F(13, i == 5), fill=SUN if i == 5 else DIM, anchor='ma')
    # botón
    y = dp(726); x0, x1, h = dp(20), W - dp(20), dp(56)
    d.rounded_rectangle([x0, y + dp(5), x1, y + h + dp(5)], radius=dp(20), fill=INK)
    d.rounded_rectangle([x0, y, x1, y + h], radius=dp(20), fill=SUN, outline=INK, width=int(dp(3)))
    orb(im, x0 + dp(34), y + h / 2, dp(15), 'atencion', 3, 0.35)
    d = ImageDraw.Draw(im); d.text((x0 + dp(62), y + h / 2), 'Entrenar Atención', font=F(19), fill=INK, anchor='lm')
    P.play_tri(d, x1 - dp(30), y + h / 2, dp(16))
    nb = Image.new('RGBA', (W, P.H), (0, 0, 0, 0)); P.navbar(nb, 0)
    im.alpha_composite(nb.crop((0, int(P.H - dp(92)), W, P.H)), (0, int(H - dp(92))))
    return im


# ---------- momentos ----------
def fr_tap(im, w, h):
    x, y = w * 0.74, dp(92)
    im.alpha_composite(nubi_sprite('coach', dp(110)), (int(w * 0.02), int(dp(52))))
    orb(im, x, y, dp(30), 'memoria', 3, 0.6, today=True, burst=True)
    ImageDraw.Draw(im).text((x, y + dp(50)), 'Memoria', font=F(14), fill=WHITE, anchor='mm')
    bubble(im, w / 2, dp(2), 'Memoria · Intermedio', 'Te falta poco para Avanzado', tail_x=w * 0.3, w=w - dp(16))


def fr_back(im, w, h):
    cx, cy = w * 0.26, dp(104); x, y = w * 0.76, dp(80)
    orb(im, x, y, dp(30), 'velocidad', 4, 0.35)
    R = dp(38); a = math.radians(-90 + 3 * 72 + 5 + 62 * 0.35)
    ex, ey = x + math.cos(a) * R, y + math.sin(a) * R
    for t in range(14):
        tt = t / 13; px = cx + (ex - cx) * tt; py = cy + (ey - cy) * tt - math.sin(tt * math.pi) * dp(30)
        ov(im, lambda d, px=px, py=py, tt=tt: d.ellipse([px - dp(2.4) * tt - 1, py - dp(2.4) * tt - 1, px + dp(2.4) * tt + 1, py + dp(2.4) * tt + 1], fill=SUN + (int(60 + 190 * tt),)))
    NS.glow(im, ex, ey, dp(14), SUN, 220)
    d = ImageDraw.Draw(im); d.polygon(N.sparkle(ex, ey, dp(9)), fill=WHITE)
    d.text((ex + dp(10), ey + dp(8)), '+1', font=F(15), fill=SUN)
    im.alpha_composite(nubi_sprite('hola', dp(96)), (int(cx - dp(48)), int(cy - dp(48))))


def fr_level(im, w, h):
    x, y = w * 0.7, dp(90)
    for k in range(12):
        a = math.radians(k * 30); r0, r1 = dp(44), dp(58)
        ov(im, lambda d, a=a: d.line([(x + math.cos(a) * r0, y + math.sin(a) * r0), (x + math.cos(a) * r1, y + math.sin(a) * r1)], fill=SUN + (230,), width=int(dp(2.5))))
    orb(im, x, y, dp(32), 'memoria', 4, 0.0, burst=True)
    ImageDraw.Draw(im).text((x, y + dp(70)), 'Avanzado', font=F(14), fill=SUN, anchor='mm')
    im.alpha_composite(nubi_sprite('celebra', dp(108), 0.9), (int(w * 0.02), int(dp(40))))


def fr_grow(im, w, h):
    for i, (n, sz, sp) in enumerate([(10, 56, 0), (100, 72, 1), (500, 88, 2)]):
        x = w * (0.18 + 0.32 * i); y = dp(100)
        sp_img = nubi_sprite('hola', dp(sz))
        if sp:
            NS.glow(im, x, y, dp(sz) * 0.8, (170, 150, 255), 90 + 50 * sp)
        im.alpha_composite(sp_img, (int(x - dp(sz) / 2), int(y - dp(sz) / 2)))
        d = ImageDraw.Draw(im)
        for k in range(sp * 3):   # corona de estrellas ganadas
            a = math.radians(-90 + (k - (sp * 3 - 1) / 2) * 24)
            sx, sy = x + math.cos(a) * dp(sz) * 0.62, y + math.sin(a) * dp(sz) * 0.62
            d.polygon(N.star(sx, sy, dp(5), inner=0.5), fill=SUN, outline=INK)
        d.text((x, dp(158)), f'{n}', font=F(15), fill=WHITE, anchor='mm')
    ImageDraw.Draw(im).text((w / 2, dp(180)), 'estrellas nacidas', font=F(13, False), fill=DIM, anchor='mm')


FRAMES = [(fr_tap, 'Tocas un área', 'Nubi la mira y te explica en qué vas. Se abre su ventana con los juegos.'),
          (fr_back, 'Al volver de un juego', 'Nace una estrella de Nubi y llena un poco el anillo del área.'),
          (fr_level, 'Etapa nueva', 'El orbe destella y Nubi celebra contigo.'),
          (fr_grow, 'Nubi crece contigo', 'Con tus partidas gana brillo y estrellas. Nunca baja ni se pone triste.')]


def legend(im, y):
    d = ImageDraw.Draw(im)
    d.text((dp(20), y), 'Cómo se lee (una idea por cosa)', font=F(18), fill=SUN); y += dp(36)
    rows = [('orbe', 'Cada orbe es un área: su ícono y su nombre, siempre.'),
            ('anillo', 'El anillo es tu avance: 5 tramos = 5 etapas. Nunca baja.'),
            ('hoy', 'Sello amarillo: esa área la jugaste hoy.'),
            ('zona', 'Aro punteado: el área que Nubi te propone hoy.'),
            ('nubi', 'Nubi explica: tócala o toca un área.')]
    for i, (k, txt) in enumerate(rows):
        yy = y + i * dp(52)
        x = dp(44)
        if k == 'orbe': orb(im, x, yy + dp(14), dp(16), 'lenguaje', 1, 0.0)
        elif k == 'anillo':
            orb(im, x, yy + dp(14), dp(12), 'calculo', 3, 0.5)
        elif k == 'hoy': orb(im, x, yy + dp(14), dp(14), 'velocidad', 4, 0.2, today=True)
        elif k == 'zona': orb(im, x, yy + dp(14), dp(12), 'atencion', 3, 0.35, zone=True)
        else: im.alpha_composite(nubi_sprite('hola', dp(44)), (int(x - dp(22)), int(yy - dp(8))))
        d = ImageDraw.Draw(im)
        P.wrap(d, dp(88), yy + dp(4), txt, F(14, False), WHITE, W - dp(108))
    return y + len(rows) * dp(52)


def sheet():
    scr = screen()
    fw, fh = (W - dp(52)) / 2, dp(290)
    H = dp(84) + scr.size[1] + dp(24) + dp(330) + dp(40) + 2 * (fh + dp(12)) + dp(30)
    im = Image.new('RGBA', (W, int(H)), (10, 10, 30, 255)); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(26)), 'Nubi al centro · tus áreas alrededor', font=F(22), fill=SUN)
    d.text((dp(20), dp(58)), 'Hoy, con Nubi como protagonista y un halo de 6 áreas.', font=F(13, False), fill=DIM)
    y = dp(84)
    AL.paste_round(im, scr, 0, y, dp(24)); y += scr.size[1] + dp(24)
    y = legend(im, y) + dp(20)
    d = ImageDraw.Draw(im); d.text((dp(20), y), 'Momentos', font=F(18), fill=SUN); y += dp(36)
    for i, (fn, cap, sub) in enumerate(FRAMES):
        fr = NM.nebula_bg(int(fw), int(fh), 80 + i, stars=30)
        fn(fr, fw, fh)
        dd = ImageDraw.Draw(fr)
        dd.text((dp(12), fh - dp(92)), cap, font=F(16), fill=WHITE)
        P.wrap(dd, dp(12), fh - dp(68), sub, F(13, False), DIM, fw - dp(24))
        AL.paste_round(im, fr, dp(20) + (i % 2) * (fw + dp(12)), y + (i // 2) * (fh + dp(12)))
    out = os.path.join(P.OUT, 'nubi-halo.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
