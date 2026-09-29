# Anillo de luz: propuestas (29-sep). Ricardo eligió el ANILLO DE LUZ para mostrar el avance (sin planeta), sin los
# satélites alrededor de Nubi ("está de más"), y pidió opciones: detalles, si es dinámico, cambios de color, que
# indique cambios. Maqueta PIL, NO la app. Misma lógica de avance que hoy (`data/Planet`: 6 áreas, 5 etapas, nunca
# baja). Colores de las áreas revisados con el validador de la skill dataviz (fondo oscuro): el verde de Lenguaje y el
# turquesa de Cálculo casi no se distinguían (ΔE 11,6 < 15); propuesta: Lenguaje verde lima, Cálculo celeste y
# Atención un ámbar un poco más hondo (todos pasan; azul y uva se confunden en deuteranopía, por eso cada tramo lleva
# siempre su nombre).
# Uso: python3 tools/previews/anillo_luz.py  ->  docs/previews/anillo-luz.png
import os, math, importlib.util
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('nm', os.path.join(HERE, 'nubi_mundo.py'))
NM = importlib.util.module_from_spec(spec); spec.loader.exec_module(NM)
NS, N, P = NM.NS, NM.N, NM.P
F, dp, W = P.F, P.dp, P.W
INK, SUN, CREAM, WHITE, DIM, LIME = P.INK, P.SUN, P.CREAM, P.WHITE, P.DIM, P.LIME
LINE = NS.LINE

ORDER = ['memoria', 'atencion', 'razonamiento', 'lenguaje', 'calculo', 'velocidad']
NAMES = {'memoria': 'Memoria', 'atencion': 'Atención', 'razonamiento': 'Razonamiento', 'lenguaje': 'Lenguaje',
         'calculo': 'Cálculo', 'velocidad': 'Velocidad'}
OLD = {'memoria': (59, 130, 246), 'atencion': (245, 158, 11), 'razonamiento': (139, 92, 246),
       'lenguaje': (16, 185, 129), 'calculo': (13, 148, 136), 'velocidad': (244, 63, 94)}
COL = {'memoria': (59, 130, 246), 'atencion': (207, 122, 6), 'razonamiento': (139, 92, 246),
       'lenguaje': (79, 174, 42), 'calculo': (28, 159, 206), 'velocidad': (244, 63, 94)}
LIGHT = {k: tuple(int(c + (255 - c) * 0.5) for c in v) for k, v in COL.items()}
LEVEL = {'memoria': 4, 'atencion': 3, 'razonamiento': 2, 'lenguaje': 1, 'calculo': 3, 'velocidad': 4}
FRAC = {'memoria': 0.6, 'atencion': 0.3, 'razonamiento': 0.8, 'lenguaje': 0.5, 'calculo': 0.2, 'velocidad': 0.4}
GAP = 9   # grados entre tramos


def ov(im, fn, blur=0):
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); fn(ImageDraw.Draw(lay))
    if blur: lay = lay.filter(ImageFilter.GaussianBlur(blur))
    im.alpha_composite(lay)


def span(i):
    a0 = -90 - 30 + i * 60 + GAP / 2
    return a0, a0 + 60 - GAP


def pt(cx, cy, r, a): return cx + math.cos(math.radians(a)) * r, cy + math.sin(math.radians(a)) * r


def nubi(im, cx, cy, size, pose='hola'):
    fn = {'hola': lambda c, x, y, k: NS.nubi(c, x, y, k, 'hola'), 'celebra': NS.pose_celebra}[pose]
    sp = NM.nubi_sprite(fn, size, 0.9 if pose == 'celebra' else 1.0)
    im.alpha_composite(sp, (int(cx - size / 2), int(cy - size / 2)))


def labels(im, cx, cy, R, zone=None, extra=None):
    d = ImageDraw.Draw(im)
    for i, key in enumerate(ORDER):
        a0, a1 = span(i); x, y = pt(cx, cy, R, (a0 + a1) / 2)
        col = SUN if key == zone else WHITE
        d.text((x, y - dp(8)), NAMES[key], font=F(14), fill=col, anchor='mm')
        sub = (extra or {}).get(key, f'etapa {LEVEL[key]}')
        d.text((x, y + dp(10)), sub, font=F(13, False), fill=DIM, anchor='mm')


# ---------- formas ----------
def beads(im, cx, cy, R, level=LEVEL, dim=(), flash=None, intensify=False, r=None):
    """Cuentas de luz: 5 perlas por tramo; las encendidas brillan del color del área."""
    r = r or R * 0.085
    for i, key in enumerate(ORDER):
        a0, a1 = span(i)
        ov(im, lambda d, a0=a0, a1=a1: d.arc([cx - R, cy - R, cx + R, cy + R], a0, a1, fill=(255, 255, 255, 34), width=max(2, int(r * 0.5))))
        for j in range(5):
            a = a0 + (a1 - a0) * (j + 0.5) / 5; x, y = pt(cx, cy, R, a)
            lit = j < level[key]
            if lit:
                col = COL[key]
                if intensify:   # el color se hace más hondo con cada etapa
                    t = 0.35 + 0.65 * (j + 1) / 5; col = tuple(int(255 - (255 - c) * t) for c in COL[key])
                a_glow = 60 if key in dim else (200 if key == flash else 120)
                NS.glow(im, x, y, r * (3.2 if key == flash else 2.3), LIGHT[key], a_glow)
                d = ImageDraw.Draw(im)
                fill = tuple(int(c * 0.55 + 20) for c in col) if key in dim else col
                d.ellipse([x - r, y - r, x + r, y + r], fill=fill, outline=LINE, width=max(2, int(dp(1.5))))
                d.ellipse([x - r * 0.55, y - r * 0.6, x - r * 0.05, y - r * 0.15], fill=(255, 255, 255, 170) if key not in dim else (255, 255, 255, 60))
            else:
                ov(im, lambda d, x=x, y=y: d.ellipse([x - r * 0.72, y - r * 0.72, x + r * 0.72, y + r * 0.72], outline=(255, 255, 255, 90), width=max(2, int(dp(1.4)))))


def petals(im, cx, cy, R):
    """Pétalos: cada área es un pétalo que crece desde Nubi hacia afuera (largo = etapa)."""
    r0 = R * 0.52
    for ring in range(1, 6):
        rr = r0 + (R - r0) * ring / 5
        ov(im, lambda d, rr=rr: d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], outline=(255, 255, 255, 26), width=max(1, int(dp(1)))))
    for i, key in enumerate(ORDER):
        a0, a1 = span(i)
        def wedge(r1, a0=a0, a1=a1):
            pts = [pt(cx, cy, r0, a0 + (a1 - a0) * t / 20) for t in range(21)]
            pts += [pt(cx, cy, r1, a1 - (a1 - a0) * t / 20) for t in range(21)]
            return pts
        ov(im, lambda d: d.polygon(wedge(R), fill=(255, 255, 255, 22)))
        r1 = r0 + (R - r0) * (LEVEL[key] + FRAC[key]) / 5
        NS.glow(im, *pt(cx, cy, (r0 + r1) / 2, (a0 + a1) / 2), (r1 - r0) * 0.6, LIGHT[key], 110)
        d = ImageDraw.Draw(im)
        d.polygon(wedge(r1), fill=COL[key], outline=LINE, width=max(2, int(dp(1.6))))
        ov(im, lambda d: d.line([pt(cx, cy, r1 - dp(4), a0 + 4), pt(cx, cy, r1 - dp(4), a1 - 4)], fill=(255, 255, 255, 140), width=int(dp(2))))


def continuous(im, cx, cy, R):
    """Luz continua: cada tramo se llena como una barra curva, con un "cometa" brillante en la punta."""
    th = R * 0.13
    for i, key in enumerate(ORDER):
        a0, a1 = span(i)
        ov(im, lambda d, a0=a0, a1=a1: d.arc([cx - R, cy - R, cx + R, cy + R], a0, a1, fill=(255, 255, 255, 36), width=int(th)))
        f = (LEVEL[key] + FRAC[key]) / 5; ae = a0 + (a1 - a0) * f
        NS.glow(im, *pt(cx, cy, R - th / 2, ae), th * 1.6, LIGHT[key], 170)
        steps = 24
        for s in range(steps):   # de tenue a brillante hacia la punta
            b0 = a0 + (ae - a0) * s / steps; b1 = a0 + (ae - a0) * (s + 1) / steps + 0.5
            t = 0.45 + 0.55 * (s + 1) / steps
            col = tuple(int(c * t + LIGHT[key][n] * (1 - t) * 0.0 + 0) for n, c in enumerate(COL[key]))
            ov(im, lambda d, b0=b0, b1=b1, col=col: d.arc([cx - R, cy - R, cx + R, cy + R], b0, b1, fill=col + (255,), width=int(th)))
        x, y = pt(cx, cy, R - th / 2, ae)
        d = ImageDraw.Draw(im); d.ellipse([x - th * 0.42, y - th * 0.42, x + th * 0.42, y + th * 0.42], fill=WHITE)
        for j in range(1, 5):   # marcas de etapa
            x0, y0 = pt(cx, cy, R + dp(2), a0 + (a1 - a0) * j / 5); x1, y1 = pt(cx, cy, R - th - dp(2), a0 + (a1 - a0) * j / 5)
            ov(im, lambda d, x0=x0, y0=y0, x1=x1, y1=y1: d.line([(x0, y0), (x1, y1)], fill=(10, 10, 40, 200), width=int(dp(1.6))))


def star_ring(im, cx, cy, R):
    """Anillo de estrellas: 30 estrellas en círculo; las encendidas se unen con hilos de luz (como una constelación)."""
    for i, key in enumerate(ORDER):
        a0, a1 = span(i); lv = LEVEL[key]
        pts = [pt(cx, cy, R + (dp(6) if j % 2 else -dp(6)), a0 + (a1 - a0) * (j + 0.5) / 5) for j in range(5)]
        ov(im, lambda d, pts=pts, lv=lv, key=key: d.line(pts[:lv], fill=LIGHT[key] + (200,), width=int(dp(1.6))) if lv > 1 else None)
        for j, (x, y) in enumerate(pts):
            if j < lv:
                NS.glow(im, x, y, dp(11), LIGHT[key], 150)
                d = ImageDraw.Draw(im); d.polygon(N.sparkle(x, y, dp(8)), fill=LIGHT[key]); d.ellipse([x - dp(2), y - dp(2), x + dp(2), y + dp(2)], fill=WHITE)
            else:
                ov(im, lambda d, x=x, y=y: d.ellipse([x - dp(2.2), y - dp(2.2), x + dp(2.2), y + dp(2.2)], fill=(255, 255, 255, 90)))


def week_ring(im, cx, cy, r, played=(1, 1, 0, 1, 1, 1, 0), today=5):
    """Tu semana, por dentro: 7 puntitos (L a D) que se encienden al jugar ese día."""
    for i in range(7):
        a = -90 - 60 + i * 20; x, y = pt(cx, cy, r, a)
        s = dp(4.2 if i == today else 3.2)
        if played[i]:
            NS.glow(im, x, y, s * 2.4, SUN, 110)
            d = ImageDraw.Draw(im); d.ellipse([x - s, y - s, x + s, y + s], fill=SUN, outline=LINE, width=int(dp(1.2)))
        else:
            ov(im, lambda d, x=x, y=y, s=s: d.ellipse([x - s, y - s, x + s, y + s], outline=(255, 255, 255, 110), width=int(dp(1.3))))
        d = ImageDraw.Draw(im)
        lx, ly = pt(cx, cy, r + dp(13), a)
        d.text((lx, ly), 'LMMJVSD'[i], font=F(13, i == today), fill=SUN if i == today else DIM, anchor='mm')


def zone_mark(im, cx, cy, R, key, width):
    i = ORDER.index(key); a0, a1 = span(i)
    def f(d):
        n = 16
        for s in range(n):
            if s % 2: continue
            b0 = a0 - 3 + (a1 - a0 + 6) * s / n; b1 = a0 - 3 + (a1 - a0 + 6) * (s + 1) / n
            d.arc([cx - R, cy - R, cx + R, cy + R], b0, b1, fill=SUN + (235,), width=int(width))
    ov(im, f)


# ---------- paneles ----------
def panel(w, h, seed):
    im = NM.nebula_bg(int(w), int(h), seed, stars=int(w * h / (W * dp(1180)) * 160))
    return im


def paste_round(dst, src, x, y, rad=dp(18)):
    m = Image.new('L', src.size, 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, src.size[0] - 1, src.size[1] - 1], radius=rad, fill=255)
    dst.paste(src, (int(x), int(y)), m)


def hero():
    w, h = W, dp(500)
    im = panel(w, h, 31)
    cx, cy, R = w / 2, dp(222), dp(118)
    zone_mark(im, cx, cy, R + dp(17), 'atencion', dp(3))
    beads(im, cx, cy, R, r=dp(10.5))
    week_ring(im, cx, cy, dp(78))
    nubi(im, cx, cy + dp(4), dp(104))
    labels(im, cx, cy, R + dp(52), zone='atencion', extra={'atencion': 'zona del día'})
    d = ImageDraw.Draw(im)
    d.text((w / 2, h - dp(50)), 'Hoy se encendieron 2 luces', font=F(18), fill=SUN, anchor='mm')
    d.text((w / 2, h - dp(22)), 'Memoria y Atención · 5 días jugados esta semana', font=F(14, False), fill=DIM, anchor='mm')
    return im


def small(fn, seed, caption, sub, draw_nubi=True, pose='hola'):
    w, h = (W - dp(52)) / 2, dp(286)
    im = panel(w, h, seed)
    cx, cy, R = w / 2, dp(100), dp(68)
    fn(im, cx, cy, R)
    if draw_nubi: nubi(im, cx, cy + dp(3), dp(62), pose)
    d = ImageDraw.Draw(im)
    d.text((dp(12), dp(190)), caption, font=F(16), fill=WHITE)
    P.wrap(d, dp(12), dp(214), sub, F(13, False), DIM, w - dp(24))
    return im


def st_after(im, cx, cy, R):
    beads(im, cx, cy, R, level={**LEVEL, 'memoria': 4})
    i = 0; a0, a1 = span(i); a = a0 + (a1 - a0) * 3.5 / 5; x, y = pt(cx, cy, R, a)
    for t in range(12):   # estela de la estrella que sale de Nubi
        tt = t / 11; px, py = cx + (x - cx) * tt, cy + (y - cy) * tt - math.sin(tt * math.pi) * dp(16)
        ov(im, lambda d, px=px, py=py, tt=tt: d.ellipse([px - dp(2) * tt - 1, py - dp(2) * tt - 1, px + dp(2) * tt + 1, py + dp(2) * tt + 1], fill=SUN + (int(60 + 180 * tt),)))
    NS.glow(im, x, y, dp(18), SUN, 200)
    d = ImageDraw.Draw(im); d.polygon(N.sparkle(x, y, dp(11)), fill=WHITE)
    d.text((x + dp(14), y - dp(14)), '+1', font=F(14), fill=SUN)


def st_level(im, cx, cy, R):
    lv = {**LEVEL, 'velocidad': 5}
    beads(im, cx, cy, R, level=lv, flash='velocidad')
    i = ORDER.index('velocidad'); a0, a1 = span(i)
    for k in range(7):
        a = a0 + (a1 - a0) * k / 6; x0, y0 = pt(cx, cy, R + dp(10), a); x1, y1 = pt(cx, cy, R + dp(22), a)
        ov(im, lambda d, x0=x0, y0=y0, x1=x1, y1=y1: d.line([(x0, y0), (x1, y1)], fill=SUN + (220,), width=int(dp(2))))


def st_zone(im, cx, cy, R):
    zone_mark(im, cx, cy, R + dp(13), 'atencion', dp(2.5))
    beads(im, cx, cy, R)
    i = ORDER.index('atencion'); a0, a1 = span(i); x, y = pt(cx, cy, R - dp(24), (a0 + a1) / 2)
    d = ImageDraw.Draw(im); d.text((x, y), 'hoy', font=F(13), fill=SUN, anchor='mm')


def st_rest(im, cx, cy, R):
    beads(im, cx, cy, R, dim=('lenguaje',))
    i = ORDER.index('lenguaje'); a0, a1 = span(i); x, y = pt(cx, cy, R + dp(20), (a0 + a1) / 2)
    d = ImageDraw.Draw(im)
    d.ellipse([x - dp(7), y - dp(7), x + dp(7), y + dp(7)], fill=(225, 225, 245))
    d.ellipse([x - dp(3), y - dp(9), x + dp(10), y + dp(4)], fill=(22, 24, 70))


def st_breath(im, cx, cy, R):
    for k, a in ((1.12, 40), (1.22, 22)):
        ov(im, lambda d, k=k, a=a: d.ellipse([cx - R * k, cy - R * k, cx + R * k, cy + R * k], outline=(190, 200, 255, a), width=int(dp(3))), blur=dp(2))
    beads(im, cx, cy, R)


def st_full(im, cx, cy, R):
    beads(im, cx, cy, R, level={k: max(v, 3) for k, v in LEVEL.items()})
    ov(im, lambda d: d.ellipse([cx - R - dp(12), cy - R - dp(12), cx + R + dp(12), cy + R + dp(12)], outline=SUN + (230,), width=int(dp(2.5))))
    ov(im, lambda d: d.ellipse([cx - R - dp(12), cy - R - dp(12), cx + R + dp(12), cy + R + dp(12)], outline=SUN + (120,), width=int(dp(6))), blur=dp(3))


FORMS = [(beads, 'Cuentas de luz', 'Cada partida enciende una perla. Se cuenta de un vistazo.'),
         (petals, 'Pétalos', 'Cada área crece desde Nubi hacia afuera: se ve la forma de tu perfil.'),
         (continuous, 'Luz continua', 'Se llena poco a poco, con un cometa en la punta: se nota cada avance.'),
         (star_ring, 'Anillo de estrellas', 'Estrellas que se unen con hilos de luz: sigue la historia de Nubi.')]

STATES = [(st_breath, 'Respira', 'Brilla suave y lento. Quieto si el teléfono quita animaciones.'),
          (st_after, 'Al volver de un juego', 'Sale una estrella de Nubi y enciende la perla nueva (+1).'),
          (st_level, 'Etapa nueva', 'El tramo destella y Nubi celebra: "¡Velocidad subió de etapa!".'),
          (st_zone, 'Zona del día', 'Aro sol punteado en el área que Nubi propone hoy.'),
          (st_rest, 'Quieta', 'Un área sin jugar hace días baja su brillo, con una luna. Nunca pierde perlas.'),
          (st_full, 'Anillo completo', 'Cuando las 6 llegan a una etapa, se cierra un aro sol alrededor.')]


def swatches(im, y):
    d = ImageDraw.Draw(im)
    d.text((dp(20), y), 'Colores de las áreas', font=F(17), fill=SUN)
    P.wrap(d, dp(20), y + dp(26), 'Revisados con un validador de color: el verde de Lenguaje y el turquesa de Cálculo casi no se '
           'distinguían. Propuesta: Lenguaje verde lima y Cálculo celeste (y un ámbar algo más hondo).', F(13, False), DIM, W - dp(40))
    y += dp(84)
    for row, (title, pal) in enumerate([('Hoy', OLD), ('Propuesta', COL)]):
        yy = y + row * dp(64)
        d.text((dp(20), yy + dp(14)), title, font=F(14), fill=WHITE, anchor='lm')
        for i, key in enumerate(ORDER):
            x = dp(110) + i * dp(49)
            d.ellipse([x - dp(15), yy - dp(1), x + dp(15), yy + dp(29)], fill=pal[key], outline=LINE, width=int(dp(1.6)))
            if row == 1:
                d.text((x, yy + dp(38)), NAMES[key][:4] + '.', font=F(13, False), fill=DIM, anchor='mm')
    y += dp(150)
    d.text((dp(20), y), 'Opcional: el color se hace más hondo con cada etapa', font=F(14), fill=WHITE)
    for i, key in enumerate(['memoria', 'velocidad']):
        cx, cy = dp(20) + dp(190) * i + dp(95), y + dp(58)
        R = dp(70)
        a0, a1 = -170, -10
        for j in range(5):
            a = a0 + (a1 - a0) * (j + 0.5) / 5; x, yy = pt(cx, cy + dp(40), R, a)
            t = 0.35 + 0.65 * (j + 1) / 5; col = tuple(int(255 - (255 - c) * t) for c in COL[key])
            NS.glow(im, x, yy, dp(16), LIGHT[key], 90)
            d = ImageDraw.Draw(im); d.ellipse([x - dp(10), yy - dp(10), x + dp(10), yy + dp(10)], fill=col, outline=LINE, width=int(dp(1.5)))
            d.text((x, yy + dp(22)), str(j + 1), font=F(13, False), fill=DIM, anchor='mm')
    return y + dp(130)


def sheet():
    H = dp(80) + dp(500) + dp(40) + 2 * dp(298) + dp(50) + 3 * dp(298) + dp(470)
    im = Image.new('RGBA', (W, int(H)), (10, 10, 30, 255)); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(26)), 'Anillo de luz · propuestas', font=F(24), fill=SUN)
    d.text((dp(20), dp(58)), 'Sin satélites. Nubi al centro; el anillo cuenta tu avance.', font=F(13, False), fill=DIM)
    y = dp(84)
    paste_round(im, hero(), 0, y)
    d = ImageDraw.Draw(im)
    y += dp(500) + dp(10)
    P.wrap(d, dp(20), y, 'Mi propuesta: cuentas de luz + tu semana por dentro + zona del día.', F(14), WHITE, W - dp(40))
    y += dp(50)
    d.text((dp(20), y - dp(6)), '1 · La forma del anillo', font=F(18), fill=SUN); y += dp(26)
    pw = (W - dp(52)) / 2
    for i, (fn, cap, sub) in enumerate(FORMS):
        paste_round(im, small(fn, 40 + i, cap, sub), dp(20) + (i % 2) * (pw + dp(12)), y + (i // 2) * dp(298))
    y += 2 * dp(298) + dp(14)
    d = ImageDraw.Draw(im)
    d.text((dp(20), y), '2 · Lo que cuenta el anillo (se mueve)', font=F(18), fill=SUN); y += dp(30)
    for i, (fn, cap, sub) in enumerate(STATES):
        pose = 'celebra' if fn in (st_level, st_full) else 'hola'
        paste_round(im, small(fn, 60 + i, cap, sub, pose=pose), dp(20) + (i % 2) * (pw + dp(12)), y + (i // 2) * dp(298))
    y += 3 * dp(298) + dp(14)
    d = ImageDraw.Draw(im)
    d.text((dp(20), y), '3 · Color', font=F(18), fill=SUN); y += dp(34)
    swatches(im, y)
    out = os.path.join(P.OUT, 'anillo-luz.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
