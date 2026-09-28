# Pestaña "Juegos", tercera ronda (28-sep): Ricardo la instaló y "se ve bien", pero es demasiada información en una
# pantalla. Pide empezar por "¿Qué quieres trabajar hoy?" (las áreas), que al tocar una se desplieguen sus juegos de
# forma llamativa pero clara, y que cada juego muestre su avance (porcentaje, una línea con puntos, fortalezas).
# Maquetas PIL, NO la app. Uso: python3 tools/previews/juegos_areas.py -> docs/previews/juegos-areas.png (+ -N).
import os, math, importlib.util
from PIL import Image, ImageDraw, ImageFont
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('jp', os.path.join(HERE, 'juegos_propuestas.py'))
J = importlib.util.module_from_spec(spec); spec.loader.exec_module(J)
P = J.P
W, H, dp, F = P.W, P.H, P.dp, P.F
INK, SUN, WHITE, DIM, SOFT, LIME, CORAL, SKY, CREAM = P.INK, P.SUN, P.WHITE, P.DIM, P.SOFT, P.LIME, P.CORAL, P.SKY, P.CREAM
DOM, NAMES, BYDOM, NEVER, light, DOMS = J.DOM, J.NAMES, J.BYDOM, J.NEVER, J.light, J.DOMS
AREA_PCT = {'memoria': 51, 'atencion': 58, 'razonamiento': 40, 'lenguaje': 25, 'calculo': 47, 'velocidad': 66}
PCT = {'bitacora': 62, 'rumbo': 45, 'correo': 0, 'secuencia': 75, 'parejas': 40, 'rutatesoro': 33}
STAGES = ['Inicio', 'Aprendiz', 'Hábil', 'Experto', 'Maestro']
STRENGTH = {'bitacora': 'Mejorando', 'rumbo': 'Récord ayer', 'secuencia': 'Tu fuerte', 'parejas': 'Constante',
            'rutatesoro': 'Para retomar', 'correo': 'Nuevo'}
MEM = BYDOM['memoria']


def pct_ring(d, cx, cy, r, pct, col, w=4):
    d.arc([cx - r, cy - r, cx + r, cy + r], 0, 360, fill=(255, 255, 255, 40), width=int(dp(w)))
    if pct > 0: d.arc([cx - r, cy - r, cx + r, cy + r], -90, -90 + 3.6 * pct, fill=col, width=int(dp(w)))


def stage_line(d, x0, y, w, pct, col, labels=False):
    """La "línea de avance": 5 puntos (Inicio → Maestro); llenos hasta donde vas."""
    k = min(4, int(pct / 20.01)) if pct > 0 else -1
    d.line([(x0, y), (x0 + w, y)], fill=(255, 255, 255, 50), width=int(dp(3)))
    if k >= 0: d.line([(x0, y), (x0 + w * k / 4, y)], fill=col, width=int(dp(3)))
    for i in range(5):
        x = x0 + w * i / 4
        on = i <= k
        r = dp(6 if i == k else 4)
        d.ellipse([x - r, y - r, x + r, y + r], fill=col if on else (40, 48, 110), outline=INK, width=int(dp(1.5)))
        if labels and i == k:
            d.text((x, y + dp(9)), STAGES[i], font=F(10), fill=col, anchor='ma')
    return STAGES[k] if k >= 0 else 'Sin empezar'


def heading(im, t, sub=None):
    d = ImageDraw.Draw(im)
    d.text((dp(20), dp(34)), t, font=F(24), fill=WHITE, anchor='lm')
    if sub: d.text((dp(20), dp(58)), sub, font=F(13, False), fill=DIM, anchor='lm')
    return d


def tag(d, x, y, text, col, anchor='la'):
    f = F(10)
    w = d.textlength(text, font=f) + dp(14)
    x0 = x if anchor == 'la' else x - w
    d.rounded_rectangle([x0, y, x0 + w, y + dp(18)], radius=dp(9), fill=col, outline=INK, width=int(dp(1.5)))
    d.text((x0 + w / 2, y + dp(9)), text, font=f, fill=INK, anchor='mm')


# ------------------------------------------------------------------ 1. Seis botones que se abren (acordeón)

def a1():
    im = P.sky(51); d = heading(im, '¿Qué quieres trabajar hoy?')
    for i, (dom, name) in enumerate(DOMS):
        col_, row = i % 3, i // 3
        w = (W - dp(52)) / 3; h = dp(78)
        x0 = dp(20) + col_ * (w + dp(6)); y0 = dp(62) + row * (h + dp(10))
        sel = dom == 'memoria'
        d.rounded_rectangle([x0, y0 + dp(4), x0 + w, y0 + h + dp(4)], radius=dp(18), fill=INK)
        d.rounded_rectangle([x0, y0, x0 + w, y0 + h], radius=dp(18), fill=DOM[dom] if sel else (34, 42, 104), outline=INK, width=int(dp(3)))
        pct_ring(d, x0 + dp(24), y0 + dp(26), dp(13), AREA_PCT[dom], CREAM if sel else light(dom), 3)
        P.disc(d, x0 + dp(24), y0 + dp(26), dp(7), DOM[dom] if not sel else CREAM, 2, 1)
        d.text((x0 + w - dp(10), y0 + dp(26)), f'{AREA_PCT[dom]}%', font=F(14), fill=WHITE, anchor='rm')
        d.text((x0 + dp(12), y0 + dp(58)), name, font=F(13), fill=WHITE if sel else light(dom), anchor='lm')
    y = dp(250)
    # triángulo que une el botón elegido con su lista
    d.polygon([(dp(56), y - dp(14)), (dp(70), y - dp(2)), (dp(42), y - dp(2))], fill=light('memoria'))
    d.line([(dp(20), y), (W - dp(20), y)], fill=light('memoria'), width=int(dp(2)))
    d.text((dp(20), y + dp(18)), 'Memoria · 6 juegos', font=F(18), fill=WHITE, anchor='lm')
    y += dp(38)
    for g in MEM:
        new = g in NEVER
        J.planet(im, dp(44), y + dp(26), dp(21), g, faded=new)
        d = ImageDraw.Draw(im)
        d.text((dp(76), y + dp(6)), NAMES[g], font=F(15), fill=WHITE if not new else DIM)
        if new:
            tag(d, dp(76), y + dp(30), 'Nuevo para ti', SUN)
        else:
            stage_line(d, dp(80), y + dp(38), dp(150), PCT[g], light('memoria'))
        d.text((W - dp(20), y + dp(10)), f'{PCT[g]}%' if not new else '—', font=F(18), fill=SUN if not new else SOFT, anchor='ra')
        if not new: tag(d, W - dp(20), y + dp(32), STRENGTH[g], (170, 230, 140) if STRENGTH[g] in ('Mejorando', 'Récord ayer', 'Tu fuerte') else (200, 208, 240), 'ra')
        y += dp(60)
    P.navbar(im, 1); return im


# ------------------------------------------------------------------ 2. Tu sistema: el área al centro y sus juegos en órbita

def a2():
    im = P.sky(52); d = heading(im, '¿Qué quieres trabajar hoy?')
    # selector: 6 planetas en fila, el elegido más grande
    x = dp(36)
    for dom, name in DOMS:
        sel = dom == 'memoria'
        r = dp(20 if sel else 14)
        if sel: P.glow(im, x, dp(96), dp(34), DOM[dom], 120)
        d = ImageDraw.Draw(im)
        P.disc(d, x, dp(96), r, DOM[dom], 3 if sel else 2, 3 if sel else 2)
        d.text((x, dp(96) + r + dp(10)), name, font=F(11 if not sel else 12), fill=light(dom) if sel else SOFT, anchor='ma')
        x += dp(68)
    # sistema: el área al centro, sus juegos en una órbita amplia (cada uno con su nombre, sin taparse)
    cx, cy, rx, ry = W / 2, dp(292), dp(150), dp(92)
    for k in (1.0, 0.62):
        d.ellipse([cx - rx * k, cy - ry * k, cx + rx * k, cy + ry * k], outline=(255, 255, 255, 50 if k == 1 else 28), width=int(dp(1.5)))
    P.glow(im, cx, cy, dp(64), DOM['memoria'], 150); d = ImageDraw.Draw(im)
    P.disc(d, cx, cy, dp(40), DOM['memoria'], 3, 5)
    d.text((cx, cy - dp(8)), '51%', font=F(22), fill=WHITE, anchor='mm')
    d.text((cx, cy + dp(14)), 'Memoria', font=F(11), fill=CREAM, anchor='mm')
    for g, a in zip(MEM, (270, 330, 30, 90, 150, 210)):
        t = math.radians(a)
        px, py = cx + math.cos(t) * rx, cy + math.sin(t) * ry
        new = g in NEVER
        sel = g == 'rumbo'
        if sel: P.glow(im, px, py, dp(38), SUN, 110); d = ImageDraw.Draw(im)
        pct_ring(d, px, py, dp(25), PCT[g], SUN if sel else light('memoria'))
        J.planet(im, px, py, dp(19), g, faded=new, badge=False)
        d = ImageDraw.Draw(im)
        d.text((px, py + dp(30)), NAMES[g].split(' ')[0], font=F(11), fill=SUN if sel else (DIM if new else WHITE), anchor='ma')
    # detalle del juego tocado
    y = dp(446)
    d.line([(dp(20), y), (W - dp(20), y)], fill=(255, 255, 255, 36), width=int(dp(1)))
    J.planet(im, dp(52), y + dp(52), dp(30), 'rumbo')
    d = ImageDraw.Draw(im)
    d.text((dp(96), y + dp(22)), 'Rumbo a Casa', font=F(20), fill=WHITE)
    d.text((dp(96), y + dp(50)), '45% · Hábil · tu marca: a 18% de casa', font=F(12, False), fill=DIM)
    stage_line(d, dp(100), y + dp(84), dp(200), 45, light('memoria'), labels=True)
    d.rounded_rectangle([W - dp(104), y + dp(68), W - dp(20), y + dp(104)], radius=dp(16), fill=SUN, outline=INK, width=int(dp(3)))
    P.play_tri(d, W - dp(84), y + dp(86), dp(10)); d.text((W - dp(54), y + dp(86)), 'Jugar', font=F(14), fill=INK, anchor='mm')
    P.note(im, W / 2, dp(566), 'los juegos giran; toca uno para verlo aquí', 'ma')
    P.navbar(im, 1); return im


# ------------------------------------------------------------------ 3. El sendero del área (camino con paradas)

def a3():
    im = P.sky(53); d = heading(im, '¿Qué quieres trabajar hoy?')
    x = dp(20); y = dp(62)
    for dom, name in DOMS:
        sel = dom == 'memoria'
        f = F(13) if sel else F(12, False)
        w = d.textlength(name, font=f) + dp(34)
        if sel:
            d.rounded_rectangle([x, y + dp(3), x + w, y + dp(33)], radius=dp(15), fill=INK)
            d.rounded_rectangle([x, y, x + w, y + dp(30)], radius=dp(15), fill=DOM[dom], outline=INK, width=int(dp(2)))
        d.ellipse([x + dp(10), y + dp(10), x + dp(20), y + dp(20)], fill=DOM[dom] if not sel else CREAM, outline=INK, width=int(dp(1.5)))
        d.text((x + dp(26), y + dp(15)), name, font=f, fill=WHITE if sel else light(dom), anchor='lm')
        x += w + dp(4)
        if x > W - dp(90): x = dp(20); y += dp(36)
    # sendero serpenteante
    nodes = [(0.28, 190), (0.66, 270), (0.3, 350), (0.68, 430), (0.3, 510), (0.66, 590)]
    pts = [(fx * W, dp(yy)) for fx, yy in nodes]
    for (a, b), g in zip(zip(pts, pts[1:]), MEM):
        n = 16
        for k in range(n):
            if k % 2: continue
            t0, t1 = k / n, (k + 1) / n
            f = lambda t: (a[0] + (b[0] - a[0]) * (3 * t * t - 2 * t ** 3), a[1] + (b[1] - a[1]) * t)
            d.line([f(t0), f(t1)], fill=light('memoria') + (200,), width=int(dp(4)))
    for (x, y), g in zip(pts, MEM):
        new = g in NEVER
        pct_ring(d, x, y, dp(30), PCT[g], SUN, 5)
        J.planet(im, x, y, dp(23), g, faded=new)
        d = ImageDraw.Draw(im)
        left = x > W / 2
        tx = x - dp(42) if left else x + dp(42); anc = 'ra' if left else 'la'
        d.text((tx, y - dp(20)), NAMES[g], font=F(15), fill=WHITE if not new else DIM, anchor=anc)
        d.text((tx, y + dp(2)), ('Nuevo para ti' if new else f'{PCT[g]}% · {STRENGTH[g]}'), font=F(12), fill=SUN if new or STRENGTH[g] in ('Tu fuerte', 'Récord ayer', 'Mejorando') else DIM, anchor=anc)
    d.text((W / 2, dp(640)), 'Memoria · 51% del camino', font=F(14), fill=light('memoria'), anchor='ma')
    P.note(im, W / 2, dp(662), 'al elegir un área, el sendero se dibuja de arriba abajo', 'ma')
    P.navbar(im, 1); return im


# ------------------------------------------------------------------ 4. Cartas que se deslizan (una a la vez, grande)

def a4():
    im = P.sky(54); d = heading(im, '¿Qué quieres trabajar hoy?')
    x = dp(20)
    for dom, name in DOMS:
        sel = dom == 'memoria'
        r = dp(22)
        P.disc(d, x + r, dp(92), r, DOM[dom] if sel else (40, 48, 110), 3 if sel else 2, 3 if sel else 2)
        pct_ring(d, x + r, dp(92), r + dp(5), AREA_PCT[dom], light(dom), 3)
        d.text((x + r, dp(92)), name[:3], font=F(12), fill=WHITE, anchor='mm')
        x += dp(64)
    d.text((dp(20), dp(138)), 'Memoria · 51%', font=F(18), fill=light('memoria'))
    # carta grande + dos asomando
    cw, chh = W - dp(90), dp(390); cx0 = dp(45); cy0 = dp(176)
    for off, alpha in ((-1, 0), (1, 0)):
        x0 = cx0 + off * (cw + dp(14))
        d.rounded_rectangle([x0, cy0 + dp(20), x0 + cw, cy0 + chh - dp(20)], radius=dp(26), fill=(34, 42, 104), outline=INK, width=int(dp(3)))
    d.rounded_rectangle([cx0, cy0 + dp(6), cx0 + cw, cy0 + chh + dp(6)], radius=dp(28), fill=INK)
    d.rounded_rectangle([cx0, cy0, cx0 + cw, cy0 + chh], radius=dp(28), fill=CREAM, outline=INK, width=int(dp(3)))
    mx = cx0 + cw / 2
    P.glow(im, mx, cy0 + dp(84), dp(60), DOM['memoria'], 110)
    J.planet(im, mx, cy0 + dp(84), dp(50), 'rumbo')
    d = ImageDraw.Draw(im)
    d.text((mx, cy0 + dp(152)), 'Rumbo a Casa', font=F(22), fill=INK, anchor='ma')
    d.text((mx, cy0 + dp(180)), 'Volver a casa sin verla', font=F(13, False), fill=(90, 84, 130), anchor='ma')
    d.text((mx, cy0 + dp(212)), '45%', font=F(34), fill=INK, anchor='ma')
    k_line = stage_line(d, cx0 + dp(34), cy0 + dp(268), cw - dp(68), 45, DOM['memoria'], labels=True)
    d.text((cx0 + dp(26), cy0 + dp(300)), 'Tus fortalezas aquí', font=F(12), fill=(90, 84, 130))
    for i, (lab, n) in enumerate([('Constancia', 3), ('Rumbo', 2), ('Distancia', 4)]):
        yy = cy0 + dp(320) + i * dp(18)
        d.text((cx0 + dp(26), yy), lab, font=F(11, False), fill=INK)
        for j in range(5):
            xx = cx0 + dp(110) + j * dp(14)
            d.ellipse([xx, yy + dp(2), xx + dp(10), yy + dp(12)], fill=DOM['memoria'] if j < n else (226, 218, 200))
    d.rounded_rectangle([cx0 + cw - dp(112), cy0 + chh - dp(56), cx0 + cw - dp(20), cy0 + chh - dp(20)], radius=dp(16), fill=SUN, outline=INK, width=int(dp(3)))
    d.text((cx0 + cw - dp(66), cy0 + chh - dp(38)), 'Jugar', font=F(15), fill=INK, anchor='mm')
    # puntitos: cuál carta
    for i in range(6):
        xx = W / 2 - dp(40) + i * dp(16)
        d.ellipse([xx - dp(4), dp(590) - dp(4), xx + dp(4), dp(590) + dp(4)], fill=SUN if i == 1 else (80, 88, 150))
    P.note(im, W / 2, dp(608), 'desliza para pasar de juego', 'ma')
    P.navbar(im, 1); return im


# ------------------------------------------------------------------ 5. Tu mapa de fortalezas (hexágono)

def a5():
    im = P.sky(55); d = heading(im, '¿Qué quieres trabajar hoy?', 'Toca un área de tu mapa')
    cx, cy, R = W / 2, dp(242), dp(100)
    pts = []
    for i in range(6):
        a = math.radians(-90 + i * 60)
        pts.append((cx + math.cos(a) * R, cy + math.sin(a) * R))
    for f in (0.33, 0.66, 1.0):
        d.polygon([(cx + (x - cx) * f, cy + (y - cy) * f) for x, y in pts], outline=(255, 255, 255, 40))
    for x, y in pts: d.line([(cx, cy), (x, y)], fill=(255, 255, 255, 30), width=int(dp(1)))
    val = [(cx + (x - cx) * AREA_PCT[dom] / 100 * 1.25, cy + (y - cy) * AREA_PCT[dom] / 100 * 1.25) for (x, y), (dom, _) in zip(pts, DOMS)]
    over = Image.new('RGBA', im.size, (0, 0, 0, 0)); ImageDraw.Draw(over).polygon(val, fill=SKY + (70,), outline=SKY)
    im.alpha_composite(over); d = ImageDraw.Draw(im)
    d.line(val + [val[0]], fill=SKY, width=int(dp(3)))
    for (x, y), (dom, name), (vx, vy) in zip(pts, DOMS, val):
        sel = dom == 'memoria'
        d.ellipse([vx - dp(5), vy - dp(5), vx + dp(5), vy + dp(5)], fill=SUN if sel else SKY, outline=INK, width=int(dp(1.5)))
        lx, ly = cx + (x - cx) * 1.32, cy + (y - cy) * 1.32
        if sel:
            P.glow(im, lx, ly, dp(36), DOM[dom], 130); d = ImageDraw.Draw(im)
        P.disc(d, lx, ly, dp(19 if sel else 15), DOM[dom], 3 if sel else 2, 3 if sel else 2)
        d.text((lx, ly), f'{AREA_PCT[dom]}', font=F(12), fill=WHITE, anchor='mm')
        d.text((lx, ly + dp(24 if sel else 20)), name, font=F(12 if sel else 11), fill=light(dom) if sel else SOFT, anchor='ma')
    d.text((W / 2, dp(414)), 'Tu fuerte hoy: Velocidad · para crecer: Lenguaje', font=F(12), fill=DIM, anchor='ma')
    y = dp(440)
    d.text((dp(20), y), 'Memoria · 6 juegos', font=F(18), fill=WHITE)
    y += dp(30)
    for i, g in enumerate(MEM):
        new = g in NEVER
        J.planet(im, dp(40), y + dp(20), dp(17), g, faded=new)
        d = ImageDraw.Draw(im)
        d.text((dp(66), y + dp(8)), NAMES[g], font=F(14), fill=WHITE if not new else DIM)
        bx0, bx1 = dp(210), W - dp(64)
        d.rounded_rectangle([bx0, y + dp(14), bx1, y + dp(24)], radius=dp(5), fill=(255, 255, 255, 30))
        if PCT[g]: d.rounded_rectangle([bx0, y + dp(14), bx0 + (bx1 - bx0) * PCT[g] / 100, y + dp(24)], radius=dp(5), fill=light('memoria'))
        d.text((W - dp(20), y + dp(19)), f'{PCT[g]}%' if not new else 'Nuevo', font=F(13), fill=SUN, anchor='rm')
        y += dp(44)
    P.navbar(im, 1); return im


if __name__ == '__main__':
    shots = [a1(), a2(), a3(), a4(), a5()]
    names = ['1. Seis botones que se abren', '2. Tu sistema', '3. El sendero del área', '4. Cartas', '5. Tu mapa de fortalezas']
    for i, s in enumerate(shots, 1):
        s.convert('RGB').resize((W * 3 // 4, H * 3 // 4), Image.LANCZOS).save(f'{P.OUT}/juegos-areas-{i}.png')
    k = 0.5; sw, sh = int(W * k), int(H * k); gap = 30; cap = 60
    cols = 3
    sheet = Image.new('RGB', (cols * sw + (cols + 1) * gap, 2 * (sh + cap) + 3 * gap), (2, 3, 16)); sd = ImageDraw.Draw(sheet)
    for i, s in enumerate(shots):
        x = gap + (i % cols) * (sw + gap); y = gap + (i // cols) * (sh + cap + gap)
        sd.text((x + sw / 2, y + cap / 2), names[i], font=ImageFont.truetype(P.FB, 28), fill=WHITE, anchor='mm')
        sheet.paste(s.convert('RGB').resize((sw, sh), Image.LANCZOS), (x, y + cap))
    sheet.save(f'{P.OUT}/juegos-areas.png')
    print('OK')
