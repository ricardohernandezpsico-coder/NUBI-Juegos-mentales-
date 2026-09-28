# Pestaña "Juegos", cuarta ronda (28-sep): de las 5 propuestas de "¿Qué quieres trabajar hoy?" Ricardo eligió la 4
# (cartas): "entrega información clara". No le gustaron los circulitos de arriba ("Mem", "Ate"...) y pide rediseñarlos,
# sumar el avance / porcentaje de habilidad y "alguna otra cosa más". Maquetas PIL, NO la app: la misma carta completa
# con 4 formas de elegir el área, y una quinta imagen con el reverso de la carta (tocarla la da vuelta).
# Uso: python3 tools/previews/juegos_cartas.py -> docs/previews/juegos-cartas.png (+ juegos-cartas-N.png)
import os, math, importlib.util
from PIL import Image, ImageDraw, ImageFont
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('ja', os.path.join(HERE, 'juegos_areas.py'))
A = importlib.util.module_from_spec(spec); spec.loader.exec_module(A)
J, P = A.J, A.P
W, H, dp, F = P.W, P.H, P.dp, P.F
INK, SUN, WHITE, DIM, SOFT, LIME, CORAL, SKY, CREAM = P.INK, P.SUN, P.WHITE, P.DIM, P.SOFT, P.LIME, P.CORAL, P.SKY, P.CREAM
DOM, DOMS, light, AREA_PCT, STAGES = J.DOM, J.DOMS, J.light, A.AREA_PCT, A.STAGES
MUTED = (90, 84, 130)          # texto secundario sobre crema (contraste 5,9:1)
TRACK = (226, 218, 200)
MEM = DOM['memoria']
MEM_DARK = (38, 92, 190)       # azul de memoria oscurecido para texto sobre crema (≥ 4,5:1)
CW = W - dp(90); CX0 = dp(45)


# ------------------------------------------------------------------ la carta (igual en las 4)

def card_shell(d, cy0, chh):
    for off in (-1, 1):
        x0 = CX0 + off * (CW + dp(14))
        d.rounded_rectangle([x0, cy0 + dp(24), x0 + CW, cy0 + chh - dp(24)], radius=dp(26), fill=(34, 42, 104), outline=INK, width=int(dp(3)))
    d.rounded_rectangle([CX0, cy0 + dp(6), CX0 + CW, cy0 + chh + dp(6)], radius=dp(28), fill=INK)
    d.rounded_rectangle([CX0, cy0, CX0 + CW, cy0 + chh], radius=dp(28), fill=CREAM, outline=INK, width=int(dp(3)))


def stage_line_cream(d, x0, y, w, pct):
    """Tu habilidad en 5 etapas; la actual con su nombre fuerte, las demás tenues."""
    k = min(4, int(pct / 20.01))
    d.line([(x0, y), (x0 + w, y)], fill=TRACK, width=int(dp(4)))
    d.line([(x0, y), (x0 + w * pct / 100, y)], fill=MEM, width=int(dp(4)))
    for i in range(5):
        x = x0 + w * i / 4
        on = i <= k
        r = dp(7 if i == k else 5)
        d.ellipse([x - r, y - r, x + r, y + r], fill=MEM if on else TRACK, outline=INK, width=int(dp(1.5)))
        d.text((x, y + dp(12)), STAGES[i], font=F(10 if i == k else 9, i == k), fill=MEM_DARK if i == k else MUTED, anchor='ma')
    # marca de "estás aquí" sobre la línea
    xm = x0 + w * pct / 100
    d.polygon([(xm - dp(5), y - dp(13)), (xm + dp(5), y - dp(13)), (xm, y - dp(7))], fill=INK)


def card(im, cy0, chh=dp(520)):
    d = ImageDraw.Draw(im)
    card_shell(d, cy0, chh)
    x0, x1, mx = CX0 + dp(24), CX0 + CW - dp(24), CX0 + CW / 2
    # esquina: juego estrella / cuál carta
    P.star5(d, x0 + dp(8), cy0 + dp(26), dp(8), SUN)
    d.text((x0 + dp(22), cy0 + dp(26)), 'Juego estrella', font=F(11), fill=MUTED, anchor='lm')
    d.text((x1, cy0 + dp(26)), '2 de 6', font=F(11), fill=MUTED, anchor='rm')
    # planeta del juego y nombre
    P.glow(im, mx, cy0 + dp(78), dp(52), MEM, 100)
    J.planet(im, mx, cy0 + dp(78), dp(40), 'rumbo', badge=False)
    d = ImageDraw.Draw(im)
    d.text((mx, cy0 + dp(128)), 'Rumbo a Casa', font=F(22), fill=INK, anchor='ma')
    d.text((mx, cy0 + dp(156)), 'Orientación: volver a casa sin verla', font=F(12, False), fill=MUTED, anchor='ma')
    # tu habilidad
    y = cy0 + dp(186)
    d.text((x0, y), 'Tu habilidad', font=F(12), fill=MUTED)
    d.text((x0, y + dp(16)), '45%', font=F(30), fill=INK)
    d.text((x0 + dp(80), y + dp(20)), 'Hábil', font=F(15), fill=MEM_DARK)
    d.text((x0 + dp(80), y + dp(40)), 'a 5 puntos de Experto', font=F(11, False), fill=MUTED)
    stage_line_cream(d, x0 + dp(10), y + dp(84), x1 - x0 - dp(20), 45)
    # tu marca + su evolución
    y = cy0 + dp(304)
    d.line([(x0, y - dp(8)), (x1, y - dp(8))], fill=TRACK, width=int(dp(1)))
    d.text((x0, y), 'Tu brújula', font=F(12), fill=MUTED)
    d.text((x0, y + dp(16)), 'a 18% de casa', font=F(16), fill=INK)
    A.tag(d, x0, y + dp(40), 'Récord ayer', LIME)
    sx0, sw = x0 + dp(150), x1 - x0 - dp(150)
    d.text((sx0 + sw, y), 'últimas 6', font=F(9, False), fill=MUTED, anchor='ra')
    P.spark(d, sx0, y + dp(16), sw, dp(38), [31, 27, 29, 24, 22, 18], MEM, lower_is_better=True)
    d.text((sx0, y + dp(58)), 'mejor hacia arriba', font=F(9, False), fill=MUTED)
    # fortalezas
    y = cy0 + dp(384)
    d.line([(x0, y - dp(8)), (x1, y - dp(8))], fill=TRACK, width=int(dp(1)))
    d.text((x0, y), 'Tus fortalezas aquí', font=F(12), fill=MUTED)
    for i, (lab, n) in enumerate([('Distancia', 4), ('Constancia', 3), ('Rumbo', 2)]):
        yy = y + dp(20) + i * dp(18)
        d.text((x0, yy), lab, font=F(11, False), fill=INK)
        for j in range(5):
            xx = x0 + dp(76) + j * dp(13)
            d.ellipse([xx, yy + dp(2), xx + dp(10), yy + dp(12)], fill=MEM if j < n else TRACK, outline=INK if j < n else None, width=int(dp(1)))
    # jugar
    bx1, by1 = x1 + dp(4), cy0 + chh - dp(20)
    d.rounded_rectangle([bx1 - dp(96), by1 - dp(36) + dp(4), bx1, by1 + dp(4)], radius=dp(17), fill=INK)
    d.rounded_rectangle([bx1 - dp(96), by1 - dp(36), bx1, by1], radius=dp(17), fill=SUN, outline=INK, width=int(dp(3)))
    P.play_tri(d, bx1 - dp(74), by1 - dp(18), dp(9)); d.text((bx1 - dp(42), by1 - dp(18)), 'Jugar', font=F(15), fill=INK, anchor='mm')
    d.text((x0, by1 - dp(18)), 'Jugaste ayer · unos 4 min', font=F(11, False), fill=MUTED, anchor='lm')
    return d


def footer(im, y, text='desliza para pasar de juego · tócala para darla vuelta'):
    d = ImageDraw.Draw(im)
    for i in range(6):   # posición en el mazo: rayitas, no círculos
        xx = W / 2 - dp(66) + i * dp(23)
        d.rounded_rectangle([xx, y, xx + dp(18), y + dp(5)], radius=dp(3), fill=SUN if i == 1 else (70, 78, 140))
    P.note(im, W / 2, y + dp(16), text, 'ma')


# ------------------------------------------------------------------ 1. Pestañas de texto

def c1():
    im = P.sky(61); d = A.heading(im, '¿Qué quieres trabajar hoy?')
    x = dp(20); y = dp(80)
    for dom, name in DOMS:
        sel = dom == 'memoria'
        f = F(16 if sel else 15, sel)
        w = d.textlength(name, font=f)
        d.text((x, y), name, font=f, fill=WHITE if sel else SOFT, anchor='lm')
        if sel: d.rounded_rectangle([x, y + dp(14), x + w, y + dp(19)], radius=dp(3), fill=light(dom))
        x += w + dp(22)
    # la fila sigue a la derecha: se desliza
    fade = Image.new('RGBA', im.size, (0, 0, 0, 0)); fd = ImageDraw.Draw(fade)
    for i in range(30): fd.rectangle([W - dp(30) + i * dp(1), y - dp(14), W - dp(29) + i * dp(1), y + dp(20)], fill=(10, 16, 60, int(i * 8)))
    im.alpha_composite(fade); d = ImageDraw.Draw(im)
    d.line([(dp(20), y + dp(26)), (W - dp(20), y + dp(26))], fill=(255, 255, 255, 30), width=int(dp(1)))
    d.text((dp(20), dp(126)), 'Tu avance en Memoria', font=F(12, False), fill=DIM, anchor='lm')
    d.text((W - dp(20), dp(126)), '51%', font=F(14), fill=light('memoria'), anchor='rm')
    d.rounded_rectangle([dp(20), dp(138), W - dp(20), dp(143)], radius=dp(3), fill=(255, 255, 255, 36))
    d.rounded_rectangle([dp(20), dp(138), dp(20) + (W - dp(40)) * 0.51, dp(143)], radius=dp(3), fill=light('memoria'))
    card(im, dp(170)); footer(im, dp(712))
    P.navbar(im, 1); return im


# ------------------------------------------------------------------ 2. Una sola área grande con flechas

def c2():
    im = P.sky(62); d = A.heading(im, '¿Qué quieres trabajar hoy?')
    cy = dp(96)
    for side in (-1, 1):
        bx = W / 2 + side * dp(158)
        P.disc(d, bx, cy, dp(18), (40, 48, 110), 2, 3)
        s = dp(6)
        d.line([(bx - side * s * 0.5, cy - s), (bx + side * s * 0.6, cy), (bx - side * s * 0.5, cy + s)], fill=WHITE, width=int(dp(3)), joint='curve')
    P.glow(im, W / 2 - dp(62), cy, dp(30), MEM, 120); d = ImageDraw.Draw(im)
    P.disc(d, W / 2 - dp(62), cy, dp(20), MEM, 3, 3)
    A.pct_ring(d, W / 2 - dp(62), cy, dp(26), 51, light('memoria'), 3)
    d.text((W / 2 - dp(30), cy - dp(10)), 'Memoria', font=F(24), fill=WHITE, anchor='lm')
    d.text((W / 2 - dp(30), cy + dp(14)), '51% de avance · 6 juegos', font=F(12, False), fill=DIM, anchor='lm')
    # qué área es entre las 6: rayitas de colores
    for i, (dom, _) in enumerate(DOMS):
        xx = W / 2 - dp(66) + i * dp(23)
        d.rounded_rectangle([xx, dp(138), xx + dp(18), dp(143)], radius=dp(3), fill=DOM[dom] if dom == 'memoria' else light(dom) + (90,))
    card(im, dp(170)); footer(im, dp(712))
    P.navbar(im, 1); return im


# ------------------------------------------------------------------ 3. Fichas de arcilla con nombre completo

def c3():
    im = P.sky(63); d = A.heading(im, '¿Qué quieres trabajar hoy?')
    x = dp(20); y = dp(64)
    for dom, name in DOMS:
        sel = dom == 'memoria'
        f = F(14)
        w = d.textlength(name, font=f) + d.textlength('51%', font=F(11)) + dp(50)
        fill = DOM[dom] if sel else (34, 42, 104)
        d.rounded_rectangle([x, y + dp(4), x + w, y + dp(44)], radius=dp(20), fill=INK)
        d.rounded_rectangle([x, y, x + w, y + dp(40)], radius=dp(20), fill=fill, outline=INK, width=int(dp(2.5)))
        d.ellipse([x + dp(12), y + dp(14), x + dp(24), y + dp(26)], fill=CREAM if sel else DOM[dom], outline=INK, width=int(dp(1.5)))
        d.text((x + dp(32), y + dp(20)), name, font=f, fill=WHITE if sel else light(dom), anchor='lm')
        d.text((x + w - dp(12), y + dp(20)), f'{AREA_PCT[dom]}%', font=F(11), fill=WHITE if sel else SOFT, anchor='rm')
        x += w + dp(8)
    d.text((dp(20), dp(128)), 'Memoria · 6 juegos', font=F(16), fill=light('memoria'), anchor='lm')
    P.note(im, W - dp(20), dp(120), 'la fila se desliza', 'ra')
    card(im, dp(170)); footer(im, dp(712))
    P.navbar(im, 1); return im


# ------------------------------------------------------------------ 4. El horizonte de Tu planeta

def c4():
    im = P.sky(64); d = A.heading(im, '¿Qué quieres trabajar hoy?')
    # borde del planeta: una curva de arcilla abajo del selector
    R = dp(560); cxp, cyp = W / 2, dp(156) + R
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
    ld.ellipse([cxp - R, cyp - R + dp(5), cxp + R, cyp + R + dp(5)], fill=INK)
    ld.ellipse([cxp - R, cyp - R, cxp + R, cyp + R], fill=(52, 62, 140), outline=INK, width=int(dp(3)))
    mask = Image.new('L', im.size, 0); ImageDraw.Draw(mask).rectangle([0, 0, W, dp(190)], fill=255)
    im.paste(lay, (0, 0), Image.composite(lay, Image.new('RGBA', im.size), mask).split()[3]); d = ImageDraw.Draw(im)
    xs = [dp(48) + (W - dp(96)) * i / 5 for i in range(6)]
    for (dom, name), x in zip(DOMS, xs):
        sel = dom == 'memoria'
        ground = cyp - math.sqrt(R * R - (x - cxp) ** 2)
        r = dp(22 if sel else 15)
        cy = ground - r - (dp(12) if sel else dp(2))
        if sel: P.glow(im, x, cy, dp(34), DOM[dom], 130); d = ImageDraw.Draw(im)
        P.disc(d, x, cy, r, DOM[dom], 3 if sel else 2, 3)
        if sel:
            A.pct_ring(d, x, cy, r + dp(6), 51, light(dom), 3)
            d.text((x, cy), '51%', font=F(11), fill=WHITE, anchor='mm')
        d.text((x, cy - r - dp(8 if not sel else 12)), name, font=F(11 if not sel else 13, sel), fill=WHITE if sel else SOFT, anchor='md')
    P.note(im, W / 2, dp(196), 'toca un planeta: se eleva y cambian las cartas', 'ma')
    card(im, dp(226)); footer(im, dp(762))
    P.navbar(im, 1); return im


# ------------------------------------------------------------------ 5. El reverso de la carta (tu historia)

def c5():
    im = P.sky(65); d = A.heading(im, '¿Qué quieres trabajar hoy?')
    d.text((dp(20), dp(80)), 'Memoria', font=F(16), fill=WHITE, anchor='lm')
    d.rounded_rectangle([dp(20), dp(94), dp(20) + d.textlength('Memoria', font=F(16)), dp(99)], radius=dp(3), fill=light('memoria'))
    cy0, chh = dp(130), dp(540)
    card_shell(d, cy0, chh)
    x0, x1, mx = CX0 + dp(24), CX0 + CW - dp(24), CX0 + CW / 2
    J.planet(im, x0 + dp(20), cy0 + dp(40), dp(20), 'rumbo', badge=False); d = ImageDraw.Draw(im)
    d.text((x0 + dp(50), cy0 + dp(28)), 'Tu historia en', font=F(12, False), fill=MUTED)
    d.text((x0 + dp(50), cy0 + dp(44)), 'Rumbo a Casa', font=F(18), fill=INK)
    # tu brújula en el tiempo (grande)
    y = cy0 + dp(86)
    d.text((x0, y), 'Tu brújula, partida a partida', font=F(12), fill=MUTED)
    vals = [34, 31, 27, 29, 26, 24, 22, 23, 18]
    gx0, gw, gy0, gh = x0 + dp(6), x1 - x0 - dp(12), y + dp(26), dp(110)
    for k in range(3): d.line([(gx0, gy0 + gh * k / 2), (gx0 + gw, gy0 + gh * k / 2)], fill=TRACK, width=int(dp(1)))
    lo, hi = min(vals), max(vals)
    pts = [(gx0 + gw * i / (len(vals) - 1), gy0 + (v - lo) / (hi - lo) * gh) for i, v in enumerate(vals)]
    d.line(pts, fill=MEM, width=int(dp(3)), joint='curve')
    for i, (px, py) in enumerate(pts):
        last = i == len(pts) - 1
        r = dp(6 if last else 4)
        d.ellipse([px - r, py - r, px + r, py + r], fill=SUN if last else MEM, outline=INK, width=int(dp(1.5)))
    d.text((pts[-1][0], pts[-1][1] - dp(12)), '18%', font=F(12), fill=INK, anchor='md')
    d.text((pts[0][0] + dp(10), pts[0][1]), '34%', font=F(10, False), fill=MUTED, anchor='lm')
    d.text((gx0, gy0 + gh + dp(14)), 'hace 3 semanas', font=F(9, False), fill=MUTED)
    d.text((gx0 + gw, gy0 + gh + dp(14)), 'ayer', font=F(9, False), fill=MUTED, anchor='ra')
    d.text((gx0 + gw / 2, gy0 + gh + dp(14)), 'más cerca de casa = más arriba', font=F(9, False), fill=MUTED, anchor='ma')
    # tres datos sueltos
    y = cy0 + dp(268)
    d.line([(x0, y - dp(8)), (x1, y - dp(8))], fill=TRACK, width=int(dp(1)))
    cols = [('9', 'partidas'), ('18%', 'tu récord'), ('3', 'días seguidos')]
    for i, (big, small) in enumerate(cols):
        cx = x0 + (x1 - x0) * (i + 0.5) / 3
        d.text((cx, y + dp(6)), big, font=F(24), fill=INK, anchor='ma')
        d.text((cx, y + dp(38)), small, font=F(11, False), fill=MUTED, anchor='ma')
    # hitos: la línea de etapas con fecha
    y = cy0 + dp(338)
    d.line([(x0, y - dp(8)), (x1, y - dp(8))], fill=TRACK, width=int(dp(1)))
    d.text((x0, y), 'Tus etapas', font=F(12), fill=MUTED)
    for i, (st, when) in enumerate([('Aprendiz', 'lo lograste el 14-sep'), ('Hábil', 'lo lograste el 24-sep'), ('Experto', 'a 5 puntos')]):
        yy = y + dp(22) + i * dp(22)
        done = i < 2
        if done:
            d.ellipse([x0, yy, x0 + dp(16), yy + dp(16)], fill=LIME, outline=INK, width=int(dp(1.5)))
            P.check(d, x0 + dp(8), yy + dp(8), dp(9))
        else:
            d.ellipse([x0 + dp(1), yy + dp(1), x0 + dp(15), yy + dp(15)], outline=MUTED, width=int(dp(2)))
        d.text((x0 + dp(24), yy), st, font=F(13), fill=INK if done else MUTED)
        d.text((x1, yy + dp(1)), when, font=F(11, False), fill=MUTED, anchor='ra')
    # un consejo concreto
    y = cy0 + dp(440)
    d.line([(x0, y - dp(8)), (x1, y - dp(8))], fill=TRACK, width=int(dp(1)))
    d.text((x0, y), 'Un truco para la próxima', font=F(12), fill=MUTED)
    P.wrap(d, x0, y + dp(18), 'Cuenta los segundos de cada tramo de ida: te ayuda con la distancia de vuelta.', F(12, False), INK, x1 - x0)
    d.text((mx, cy0 + chh - dp(22)), 'toca para volver al frente', font=F(10, False), fill=MUTED, anchor='ma')
    P.note(im, W / 2, dp(690), 'la carta gira como una moneda al tocarla', 'ma')
    P.navbar(im, 1); return im


if __name__ == '__main__':
    shots = [c1(), c2(), c3(), c4(), c5()]
    names = ['1. Pestañas de texto', '2. Un área a la vez', '3. Fichas con nombre', '4. Horizonte del planeta', 'Reverso: tu historia']
    for i, s in enumerate(shots, 1):
        s.convert('RGB').resize((W * 3 // 4, H * 3 // 4), Image.LANCZOS).save(f'{P.OUT}/juegos-cartas-{i}.png')
    k = 0.5; sw, sh = int(W * k), int(H * k); gap = 30; cap = 60
    cols = 3
    sheet = Image.new('RGB', (cols * sw + (cols + 1) * gap, 2 * (sh + cap) + 3 * gap), (2, 3, 16)); sd = ImageDraw.Draw(sheet)
    for i, s in enumerate(shots):
        x = gap + (i % cols) * (sw + gap); y = gap + (i // cols) * (sh + cap + gap)
        sd.text((x + sw / 2, y + cap / 2), names[i], font=ImageFont.truetype(P.FB, 28), fill=WHITE, anchor='mm')
        sheet.paste(s.convert('RGB').resize((sw, sh), Image.LANCZOS), (x, y + cap))
    sheet.save(f'{P.OUT}/juegos-cartas.png')
    print('OK')
