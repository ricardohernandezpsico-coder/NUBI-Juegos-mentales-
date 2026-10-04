# Propuestas para la pestaña "Juegos" (28-sep, pedido de Ricardo: los juegos se ven claros pero "desordenados, poco
# atractivos y poco ordenados para una persona que intenta saber qué hacer"). Maquetas PIL, NO la app: 6 estilos con el
# sello noche + arcilla. Las notas en sol con flechita curva indican el movimiento (no son parte de la pantalla).
# Uso: python3 tools/previews/juegos_propuestas.py  ->  docs/previews/juegos-propuesta-N.png + juegos-propuestas.png
import os, math, random, importlib.util
from PIL import Image, ImageDraw, ImageFont
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('prop', os.path.join(HERE, 'inicio_propuestas.py'))
P = importlib.util.module_from_spec(spec); spec.loader.exec_module(P)
W, H, dp, F = P.W, P.H, P.dp, P.F
INK, SUN, WHITE, DIM, SOFT, LIME, CORAL, SKY, CREAM, GRAPE = P.INK, P.SUN, P.WHITE, P.DIM, P.SOFT, P.LIME, P.CORAL, P.SKY, P.CREAM, P.GRAPE
DOM = P.DOM
ic = P.ic
NAMES = {g[0]: g[1] for g in ic.GAMES}
DOMOF = {g[0]: g[2] for g in ic.GAMES}
STAR = {'piloto', 'radar', 'satelites', 'freno', 'aterrizaje', 'acoplamiento', 'trafico', 'bitacora', 'rumbo', 'correo'}
DOMS = [('memoria', 'Memoria'), ('atencion', 'Atención'), ('razonamiento', 'Razonamiento'), ('lenguaje', 'Lenguaje'),
        ('calculo', 'Cálculo'), ('velocidad', 'Velocidad')]
BYDOM = {k: [g[0] for g in ic.GAMES if g[2] == k] for k, _ in DOMS}
for k in BYDOM:  # los juegos estrella primero
    BYDOM[k].sort(key=lambda g: g not in STAR)
MEASURE = {'radar': '84 ms', 'freno': '212 ms', 'satelites': '2,6 a la vez', 'aterrizaje': 'a 3,8%', 'acoplamiento': '96°/s',
           'trafico': '5 cápsulas', 'piloto': '12% costo', 'rumbo': 'a 18% de casa', 'bitacora': '5 de 6', 'correo': '8 de 9'}
LEVEL = {'secuencia': 9, 'parejas': 5, 'rutatesoro': 4, 'stroop': 7, 'series': 5, 'anagramas': 3,
         'calculo': 6}
NEVER = {'satelites', 'acoplamiento', 'correo'}


def light(dom):
    c = DOM[dom]
    return tuple(int(v + (255 - v) * 0.45) for v in c)


def title(im, t='Juegos', sub=None):
    d = ImageDraw.Draw(im)
    d.text((dp(20), dp(34)), t, font=F(28), fill=WHITE, anchor='lm')
    if sub: d.text((dp(20), dp(60)), sub, font=F(14, False), fill=DIM, anchor='lm')


def star_badge(d, cx, cy, s):
    P.star5(d, cx, cy, s, SUN)


def ring(d, cx, cy, r, frac, col, w=3):
    d.arc([cx - r, cy - r, cx + r, cy + r], -90, -90 + 360, fill=(255, 255, 255, 40), width=int(dp(w)))
    d.arc([cx - r, cy - r, cx + r, cy + r], -90, -90 + 360 * frac, fill=col, width=int(dp(w)))


def planet(im, cx, cy, r, gid, faded=False, badge=True, lvl_ring=None):
    if lvl_ring is not None:
        d = ImageDraw.Draw(im); ring(d, cx, cy, r + dp(6), lvl_ring, light(DOMOF[gid]))
    P.game_planet(im, cx, cy, r, gid, faded=faded)
    d = ImageDraw.Draw(im)
    if badge and gid in STAR: star_badge(d, cx + r * 0.78, cy - r * 0.78, r * 0.32)
    return d


def two_lines(d, cx, y, text, font, fill, maxw):
    words = text.split(); lines = []; cur = ''
    for w_ in words:
        n = (cur + ' ' + w_).strip()
        if d.textlength(n, font=font) <= maxw: cur = n
        else: lines.append(cur); cur = w_
    lines.append(cur)
    for ln in lines[:2]:
        d.text((cx, y), ln, font=font, fill=fill, anchor='ma'); y += font.size * 1.15
    return y


# ------------------------------------------------------------------ 1. Galaxias por área (filas que se deslizan)

def s1():
    im = P.sky(31); title(im, 'Juegos', '6 áreas · 19 juegos')
    d = ImageDraw.Draw(im)
    y = dp(92)
    for dom, name in DOMS[:5]:
        games = BYDOM[dom]
        d.ellipse([dp(20), y + dp(4), dp(32), y + dp(16)], fill=DOM[dom], outline=INK, width=int(dp(2)))
        d.text((dp(40), y + dp(10)), name, font=F(19), fill=light(dom), anchor='lm')
        d.text((dp(40) + d.textlength(name, font=F(19)) + dp(10), y + dp(11)), f'{len(games)} juego' + ('s' if len(games) > 1 else ''),
               font=F(13, False), fill=SOFT, anchor='lm')
        if len(games) > 4: d.text((W - dp(20), y + dp(11)), 'ver todo ›', font=F(13), fill=SUN, anchor='rm')
        x = dp(56)
        for g in games[:5]:
            planet(im, x, y + dp(64), dp(28), g, lvl_ring=(LEVEL.get(g, 6) / 12))
            d = ImageDraw.Draw(im)
            two_lines(d, x, y + dp(104), NAMES[g], F(11), WHITE, dp(84))
            x += dp(92)
        y += dp(148)
    P.note(im, W - dp(16), dp(70), 'cada fila se desliza de lado', 'ra')
    P.navbar(im, 1); return im


# ------------------------------------------------------------------ 2. Para ti + catálogo (decidir en un toque)

def s2():
    im = P.sky(32); title(im)
    d = ImageDraw.Draw(im)
    y = dp(78)
    d.text((dp(20), y), 'PARA TI HOY', font=F(11), fill=SUN)
    P.glow(im, dp(80), y + dp(78), dp(80), DOM['lenguaje'], 90)
    planet(im, dp(80), y + dp(78), dp(50), 'anagramas')
    d = ImageDraw.Draw(im)
    d.text((dp(150), y + dp(30)), 'Anagramas', font=F(24), fill=WHITE)
    P.wrap(d, dp(150), y + dp(62), 'Tu zona de Lenguaje está quieta hace 6 días.', F(14, False), DIM, W - dp(170))
    bx0, by0 = dp(150), y + dp(104)
    d.rounded_rectangle([bx0, by0 + dp(4), bx0 + dp(150), by0 + dp(44)], radius=dp(16), fill=INK)
    d.rounded_rectangle([bx0, by0, bx0 + dp(150), by0 + dp(40)], radius=dp(16), fill=SUN, outline=INK, width=int(dp(3)))
    P.play_tri(d, bx0 + dp(26), by0 + dp(20), dp(12)); d.text((bx0 + dp(86), by0 + dp(20)), 'Jugar', font=F(17), fill=INK, anchor='mm')
    P.note(im, W - dp(16), y + dp(156), 'cambia cada día según tu planeta', 'ra')
    # estrella
    y = dp(270)
    d.text((dp(20), y), 'Juegos estrella', font=F(19), fill=WHITE)
    d.text((dp(20), y + dp(24)), 'con una medida tuya al final', font=F(12, False), fill=SOFT)
    x = dp(52)
    for g in ['radar', 'freno', 'piloto', 'rumbo', 'trafico']:
        planet(im, x, y + dp(80), dp(30), g, faded=g in NEVER)
        d = ImageDraw.Draw(im)
        two_lines(d, x, y + dp(118), NAMES[g], F(11), WHITE, dp(84))
        d.text((x, y + dp(150)), MEASURE[g], font=F(11), fill=SUN, anchor='ma')
        x += dp(88)
    # clásicos
    y = dp(460)
    d.text((dp(20), y), 'Clásicos', font=F(19), fill=WHITE)
    d.text((dp(20), y + dp(24)), 'cortos, para calentar', font=F(12, False), fill=SOFT)
    classic = ['secuencia', 'parejas', 'stroop', 'series', 'calculo']
    for i, g in enumerate(classic):
        cx = dp(70) + (i % 3) * dp(136); cy = y + dp(82) + (i // 3) * dp(118)
        planet(im, cx, cy, dp(28), g, lvl_ring=LEVEL[g] / 12)
        d = ImageDraw.Draw(im)
        two_lines(d, cx, cy + dp(38), NAMES[g], F(11), WHITE, dp(110))
        d.text((cx, cy + dp(66)), f'nivel {LEVEL[g]}', font=F(11, False), fill=SOFT, anchor='ma')
    P.navbar(im, 1); return im


# ------------------------------------------------------------------ 3. Lista ordenada con tu progreso

def s3():
    im = P.sky(33); title(im)
    d = ImageDraw.Draw(im)
    y = dp(66); x = dp(20)
    for i, lab in enumerate(['Por área', 'Más jugados', 'Olvidados', 'Sin probar']):
        w = d.textlength(lab, font=F(13)) + dp(24)
        if i == 0:
            d.rounded_rectangle([x, y + dp(3), x + w, y + dp(33)], radius=dp(15), fill=INK)
            d.rounded_rectangle([x, y, x + w, y + dp(30)], radius=dp(15), fill=SUN, outline=INK, width=int(dp(2)))
            d.text((x + w / 2, y + dp(15)), lab, font=F(13), fill=INK, anchor='mm')
        else:
            d.text((x + w / 2, y + dp(15)), lab, font=F(13, False), fill=DIM, anchor='mm')
        x += w + dp(6)
    y = dp(112)
    last = {'bitacora': 'hoy', 'rumbo': 'ayer', 'correo': 'sin probar', 'secuencia': 'hace 3 días', 'parejas': 'hace 9 días',
            'rutatesoro': 'hace 5 días', 'piloto': 'ayer', 'freno': 'hoy', 'satelites': 'sin probar', 'stroop': 'hace 2 días',
            }
    for dom, name in DOMS[:2]:
        d.text((dp(20), y), f'{name.upper()} · {len(BYDOM[dom])}', font=F(12), fill=light(dom))
        y += dp(24)
        for g in BYDOM[dom]:
            planet(im, dp(44), y + dp(26), dp(21), g, faded=g in NEVER)
            d = ImageDraw.Draw(im)
            d.text((dp(78), y + dp(8)), NAMES[g], font=F(16), fill=WHITE if g not in NEVER else DIM)
            sub = (('Tu marca: ' + MEASURE[g]) if g in MEASURE and g not in NEVER else (f'nivel {LEVEL[g]}' if g in LEVEL else 'Nuevo para ti'))
            d.text((dp(78), y + dp(30)), sub, font=F(12, False), fill=SUN if g in MEASURE and g not in NEVER else SOFT)
            d.text((W - dp(20), y + dp(16)), last.get(g, ''), font=F(12, False), fill=SOFT, anchor='ra')
            if g not in NEVER:
                frac = (LEVEL.get(g, 6)) / 12
                bx = W - dp(96)
                d.rounded_rectangle([bx, y + dp(34), W - dp(20), y + dp(40)], radius=dp(3), fill=(255, 255, 255, 30))
                d.rounded_rectangle([bx, y + dp(34), bx + (W - dp(20) - bx) * frac, y + dp(40)], radius=dp(3), fill=light(dom))
            d.line([(dp(78), y + dp(54)), (W - dp(20), y + dp(54))], fill=(255, 255, 255, 26), width=int(dp(1)))
            y += dp(58)
        y += dp(10)
    P.navbar(im, 1); return im


# ------------------------------------------------------------------ 4. Constelaciones (colección que se completa)

def s4():
    im = P.sky(34); title(im, 'Tus constelaciones')
    d = ImageDraw.Draw(im)
    d.text((dp(20), dp(62)), 'Descubriste 16 de 19 juegos', font=F(14), fill=SUN, anchor='lm')
    bx0, bx1 = dp(20), W - dp(20)
    d.rounded_rectangle([bx0, dp(76), bx1, dp(84)], radius=dp(4), fill=(255, 255, 255, 30))
    d.rounded_rectangle([bx0, dp(76), bx0 + (bx1 - bx0) * 16 / 19, dp(84)], radius=dp(4), fill=SUN)
    shapes = {
        'memoria': [(0.1, 0.55), (0.3, 0.2), (0.55, 0.35), (0.8, 0.15), (0.72, 0.62), (0.4, 0.8)],
        'atencion': [(0.12, 0.3), (0.35, 0.7), (0.55, 0.25), (0.78, 0.6), (0.9, 0.2)],
        'razonamiento': [(0.2, 0.7), (0.5, 0.2), (0.8, 0.65)],
        'lenguaje': [(0.5, 0.45)],
        'calculo': [(0.25, 0.3), (0.75, 0.65)],
        'velocidad': [(0.25, 0.65), (0.75, 0.3)],
    }
    boxes = [(0, 0), (1, 0), (0, 1), (1, 1), (0, 2), (1, 2)]
    for (dom, name), (col, row) in zip(DOMS, boxes):
        x0 = dp(16) + col * (W / 2 - dp(8)); y0 = dp(104) + row * dp(208)
        bw, bh = W / 2 - dp(24), dp(150)
        pts = [(x0 + dp(24) + px * (bw - dp(48)), y0 + dp(34) + py * (bh - dp(40))) for px, py in shapes[dom]]
        games = BYDOM[dom]
        for (a, b) in zip(pts, pts[1:]):
            d.line([a, b], fill=light(dom) + (170,), width=int(dp(2)))
        for p_, g in zip(pts, games):
            faded = g in NEVER
            if not faded: P.glow(im, p_[0], p_[1], dp(26), DOM[dom], 90)
            planet(im, p_[0], p_[1], dp(17), g, faded=faded, badge=False)
            d = ImageDraw.Draw(im)
            if faded:
                d.text((p_[0], p_[1]), '?', font=F(16), fill=CREAM, anchor='mm')
        d.text((x0 + dp(8), y0 + dp(10)), name, font=F(16), fill=light(dom), anchor='lm')
        found = sum(1 for g in games if g not in NEVER)
        d.text((x0 + dp(8), y0 + bh + dp(18)), f'{found} de {len(games)} descubiertos', font=F(11, False), fill=SOFT, anchor='lm')
    P.note(im, W / 2, H - dp(122), 'las estrellas titilan; tocar una abre su juego', 'ma')
    P.navbar(im, 1); return im


# ------------------------------------------------------------------ 5. ¿Qué quieres hoy? (por objetivo)

def s5():
    im = P.sky(35); title(im, '¿Qué quieres hoy?')
    d = ImageDraw.Draw(im)
    goals = [('Algo rápido', '2 minutos', SKY, 'clock'), ('Concentrarme', 'atención', SUN, 'eye'),
             ('Recordar mejor', 'memoria', GRAPE, 'star'), ('Pensar y planificar', 'razonamiento', LIME, 'path'),
             ('Moverme rápido', 'velocidad', CORAL, 'bolt'), ('Con números y letras', 'cálculo · lenguaje', (120, 230, 200), 'abc')]
    for i, (t, s, col, icn) in enumerate(goals):
        cx0 = dp(20) + (i % 2) * (W / 2 - dp(14)); cy0 = dp(66) + (i // 2) * dp(78)
        w, h = W / 2 - dp(34), dp(66)
        sel = i == 2
        d.rounded_rectangle([cx0, cy0 + dp(4), cx0 + w, cy0 + h + dp(4)], radius=dp(18), fill=INK)
        d.rounded_rectangle([cx0, cy0, cx0 + w, cy0 + h], radius=dp(18), fill=col if sel else (40, 48, 110), outline=INK, width=int(dp(3)))
        P.disc(d, cx0 + dp(26), cy0 + h / 2, dp(14), col, 2, 2)
        tc = INK if sel else WHITE
        d.text((cx0 + dp(48), cy0 + dp(22)), t, font=F(14), fill=tc, anchor='lm')
        d.text((cx0 + dp(48), cy0 + dp(44)), s, font=F(11, False), fill=INK if sel else SOFT, anchor='lm')
    y = dp(310)
    d.text((dp(20), y), 'Para recordar mejor', font=F(19), fill=WHITE)
    d.text((dp(20), y + dp(24)), 'empieza por el primero', font=F(12, False), fill=SOFT)
    y += dp(52)
    picks = [('rumbo', 'Orientación: volver a casa sin verla', '4 min'), ('correo', 'Acordarte de hacer algo a tiempo · nuevo', '3 min'),
             ('bitacora', 'Qué, dónde y en qué orden', 'en tu sesión'), ('secuencia', 'Repetir la secuencia de luces', '2 min')]
    for i, (g, why, dur) in enumerate(picks):
        planet(im, dp(44), y + dp(26), dp(22), g)
        d = ImageDraw.Draw(im)
        d.text((dp(78), y + dp(8)), NAMES[g], font=F(16), fill=WHITE)
        d.text((dp(78), y + dp(30)), why, font=F(12, False), fill=DIM)
        d.text((W - dp(20), y + dp(16)), dur, font=F(12), fill=SUN, anchor='ra')
        d.line([(dp(78), y + dp(54)), (W - dp(20), y + dp(54))], fill=(255, 255, 255, 26), width=int(dp(1)))
        y += dp(62)
    d.text((W / 2, y + dp(8)), 'Ver los 19 juegos ›', font=F(14), fill=SUN, anchor='ma')
    P.navbar(im, 1); return im


# ------------------------------------------------------------------ 6. Tu planeta, sus zonas (igual que Hoy)

def s6():
    im = P.sky(36); title(im)
    cx, cy, r = W / 2, dp(190), dp(96)
    P.blob_planet(im, cx, cy, r, spark='memoria')
    d = ImageDraw.Draw(im)
    P.note(im, W - dp(16), dp(80), 'desliza el planeta para girar de zona', 'ra')
    # zonas como puntos con nombre (la elegida, grande)
    y = dp(304); x = dp(20)
    for dom, name in DOMS:
        sel = dom == 'memoria'
        f = F(14) if sel else F(13, False)
        w = d.textlength(name, font=f) + dp(22)
        d.ellipse([x, y + dp(4), x + dp(12), y + dp(16)], fill=DOM[dom], outline=INK, width=int(dp(2)))
        d.text((x + dp(16), y + dp(10)), name, font=f, fill=light(dom) if sel else SOFT, anchor='lm')
        if sel: d.line([(x + dp(16), y + dp(22)), (x + w - dp(8), y + dp(22))], fill=light(dom), width=int(dp(3)))
        x += w
        if x > W - dp(80): x = dp(20); y += dp(28)
    y = dp(372)
    d.text((dp(20), y), 'Zona de Memoria · 6 juegos', font=F(19), fill=WHITE)
    d.text((dp(20), y + dp(24)), 'creció 3 veces esta semana', font=F(12, False), fill=SOFT)
    y += dp(50)
    for g in BYDOM['memoria'][:5]:
        planet(im, dp(44), y + dp(24), dp(20), g, faded=g in NEVER)
        d = ImageDraw.Draw(im)
        d.text((dp(78), y + dp(6)), NAMES[g], font=F(16), fill=WHITE if g not in NEVER else DIM)
        sub = ('Tu marca: ' + MEASURE[g]) if g in MEASURE and g not in NEVER else (f'nivel {LEVEL[g]}' if g in LEVEL else 'Nuevo para ti')
        d.text((dp(78), y + dp(28)), sub, font=F(12, False), fill=SUN if 'marca' in sub else SOFT)
        P.play_tri(d, W - dp(30), y + dp(24), dp(12), col=SUN)
        d.line([(dp(78), y + dp(50)), (W - dp(20), y + dp(50))], fill=(255, 255, 255, 26), width=int(dp(1)))
        y += dp(56)
    P.navbar(im, 1); return im


if __name__ == '__main__':
    shots = [s1(), s2(), s3(), s4(), s5(), s6()]
    names = ['Galaxias por área', 'Para ti + catálogo', 'Lista con tu progreso', 'Tus constelaciones',
             '¿Qué quieres hoy?', 'Tu planeta, por zonas']
    for i, s in enumerate(shots, 1):
        s.convert('RGB').resize((W * 3 // 4, H * 3 // 4), Image.LANCZOS).save(f'{P.OUT}/juegos-propuesta-{i}.png')
    k = 0.5; sw, sh = int(W * k), int(H * k); gap = 30; cap = 60
    sheet = Image.new('RGB', (3 * sw + 4 * gap, 2 * (sh + cap) + 3 * gap), (2, 3, 16)); sd = ImageDraw.Draw(sheet)
    for i, s in enumerate(shots):
        x = gap + (i % 3) * (sw + gap); y = gap + (i // 3) * (sh + cap + gap)
        sd.text((x + sw / 2, y + cap / 2), f'{i + 1}. {names[i]}', font=ImageFont.truetype(P.FB, 30), fill=WHITE, anchor='mm')
        sheet.paste(s.convert('RGB').resize((sw, sh), Image.LANCZOS), (x, y + cap))
    sheet.save(f'{P.OUT}/juegos-propuestas.png')
    print('OK')
