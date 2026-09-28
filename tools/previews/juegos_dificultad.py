# Pestaña "Juegos", quinta ronda (28-sep): Ricardo eligió la carta con "un área a la vez" (flechas y deslizar),
# sin el rótulo "Juego estrella", y pide poder elegir la dificultad antes de jugar (fácil, moderado, difícil, experto)
# con un razonamiento que se sostenga según la edad: "un hilo conductor de la dificultad". Maquetas PIL, NO la app.
# Uso: python3 tools/previews/juegos_dificultad.py -> docs/previews/juegos-dificultad.png (+ juegos-dificultad-N.png)
import os, importlib.util
from PIL import Image, ImageDraw, ImageFont
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('jc', os.path.join(HERE, 'juegos_cartas.py'))
C = importlib.util.module_from_spec(spec); spec.loader.exec_module(C)
A, J, P = C.A, C.J, C.P
W, H, dp, F = P.W, P.H, P.dp, P.F
INK, SUN, WHITE, DIM, SOFT, LIME, CORAL, SKY, CREAM, GRAPE = P.INK, P.SUN, P.WHITE, P.DIM, P.SOFT, P.LIME, P.CORAL, P.SKY, P.CREAM, P.GRAPE
DOM, DOMS, light = J.DOM, J.DOMS, J.light
MUTED, TRACK, MEM, MEM_DARK, CW, CX0 = C.MUTED, C.TRACK, C.MEM, C.MEM_DARK, C.CW, C.CX0

MODES = [  # nombre, pasos (barras), qué es, aciertos de cada 10
    ('Suave', 1, 'Un paso más fácil. Para entrar en calor o un día cansado.', '9 de 10'),
    ('A tu medida', 2, 'Justo en tu nivel. Aquí se mide tu avance.', '8 de 10'),
    ('Desafío', 3, 'Un paso más difícil. Si lo superas, tu avance sube.', '7 de 10'),
    ('Experto', 4, 'Dos pasos más. Para probar tus límites.', '6 de 10'),
]


def bars(d, x, y, n, col, off=(214, 206, 188)):
    """Cuántos pasos de dificultad: 4 barras que crecen; se lee por cantidad, no solo por color."""
    for i in range(4):
        h = dp(6 + i * 4)
        d.rounded_rectangle([x + i * dp(8), y - h, x + i * dp(8) + dp(5), y], radius=dp(2), fill=col if i < n else off, outline=INK if i < n else None, width=int(dp(1)))


def header(im):
    d = A.heading(im, '¿Qué quieres trabajar hoy?')
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
    for i, (dom, _) in enumerate(DOMS):
        xx = W / 2 - dp(66) + i * dp(23)
        d.rounded_rectangle([xx, dp(138), xx + dp(18), dp(143)], radius=dp(3), fill=DOM[dom] if dom == 'memoria' else light(dom) + (90,))
    return d


def card(im, cy0, chh=dp(548)):
    d = ImageDraw.Draw(im)
    C.card_shell(d, cy0, chh)
    x0, x1, mx = CX0 + dp(24), CX0 + CW - dp(24), CX0 + CW / 2
    d.text((x0, cy0 + dp(26)), 'Jugaste ayer · unos 4 min', font=F(11, False), fill=MUTED, anchor='lm')
    d.text((x1, cy0 + dp(26)), '2 de 6', font=F(11), fill=MUTED, anchor='rm')
    P.glow(im, mx, cy0 + dp(84), dp(52), MEM, 100)
    J.planet(im, mx, cy0 + dp(84), dp(42), 'rumbo', badge=False)
    d = ImageDraw.Draw(im)
    d.text((mx, cy0 + dp(136)), 'Rumbo a Casa', font=F(22), fill=INK, anchor='ma')
    d.text((mx, cy0 + dp(164)), 'Orientación: volver a casa sin verla', font=F(12, False), fill=MUTED, anchor='ma')
    y = cy0 + dp(194)
    d.text((x0, y), 'Tu avance', font=F(12), fill=MUTED)
    d.text((x0, y + dp(16)), '45%', font=F(30), fill=INK)
    d.text((x0 + dp(80), y + dp(20)), 'Hábil', font=F(15), fill=MEM_DARK)
    d.text((x0 + dp(80), y + dp(40)), 'a 5 puntos de Experto', font=F(11, False), fill=MUTED)
    C.stage_line_cream(d, x0 + dp(10), y + dp(84), x1 - x0 - dp(20), 45)
    y = cy0 + dp(312)
    d.line([(x0, y - dp(8)), (x1, y - dp(8))], fill=TRACK, width=int(dp(1)))
    d.text((x0, y), 'Tu brújula', font=F(12), fill=MUTED)
    d.text((x0, y + dp(16)), 'a 18% de casa', font=F(16), fill=INK)
    A.tag(d, x0, y + dp(40), 'Récord ayer', LIME)
    sx0, sw = x0 + dp(150), x1 - x0 - dp(150)
    d.text((sx0 + sw, y), 'últimas 6', font=F(9, False), fill=MUTED, anchor='ra')
    P.spark(d, sx0, y + dp(16), sw, dp(38), [31, 27, 29, 24, 22, 18], MEM, lower_is_better=True)
    d.text((sx0, y + dp(58)), 'mejor hacia arriba', font=F(9, False), fill=MUTED)
    y = cy0 + dp(392)
    d.line([(x0, y - dp(8)), (x1, y - dp(8))], fill=TRACK, width=int(dp(1)))
    d.text((x0, y), 'Tus fortalezas aquí', font=F(12), fill=MUTED)
    for i, (lab, n) in enumerate([('Distancia', 4), ('Constancia', 3), ('Rumbo', 2)]):
        yy = y + dp(20) + i * dp(18)
        d.text((x0, yy), lab, font=F(11, False), fill=INK)
        for j in range(5):
            xx = x0 + dp(76) + j * dp(13)
            d.ellipse([xx, yy + dp(2), xx + dp(10), yy + dp(12)], fill=MEM if j < n else TRACK, outline=INK if j < n else None, width=int(dp(1)))
    # abajo: dificultad (secundario) + Jugar a tu medida
    by1 = cy0 + chh - dp(20); bx1 = x1 + dp(4)
    d.rounded_rectangle([bx1 - dp(118), by1 - dp(40) + dp(4), bx1, by1 + dp(4)], radius=dp(19), fill=INK)
    d.rounded_rectangle([bx1 - dp(118), by1 - dp(40), bx1, by1], radius=dp(19), fill=SUN, outline=INK, width=int(dp(3)))
    P.play_tri(d, bx1 - dp(96), by1 - dp(20), dp(9)); d.text((bx1 - dp(52), by1 - dp(20)), 'Jugar', font=F(16), fill=INK, anchor='mm')
    bx0 = x0 - dp(4)
    d.rounded_rectangle([bx0, by1 - dp(40) + dp(4), bx0 + dp(150), by1 + dp(4)], radius=dp(19), fill=INK)
    d.rounded_rectangle([bx0, by1 - dp(40), bx0 + dp(150), by1], radius=dp(19), fill=(240, 230, 212), outline=INK, width=int(dp(2.5)))
    bars(d, bx0 + dp(14), by1 - dp(12), 2, MEM)
    d.text((bx0 + dp(52), by1 - dp(28)), 'A tu medida', font=F(12), fill=INK, anchor='lm')
    d.text((bx0 + dp(52), by1 - dp(12)), 'cambiar', font=F(10, False), fill=MUTED, anchor='lm')
    return d


# ------------------------------------------------------------------ 1. La carta final

def d1():
    im = P.sky(71); header(im)
    card(im, dp(170))
    C.footer(im, dp(738), 'desliza la carta: otro juego · desliza "Memoria": otra área')
    P.navbar(im, 1); return im


# ------------------------------------------------------------------ 2. Elegir cómo jugar (ventana)

def d2():
    im = P.sky(72); header(im); card(im, dp(170))
    P.navbar(im, 1)
    shade = Image.new('RGBA', im.size, (4, 6, 24, 190)); im.alpha_composite(shade)
    d = ImageDraw.Draw(im)
    x0, x1, y0, y1 = dp(16), W - dp(16), dp(110), dp(836)
    d.rounded_rectangle([x0, y0 + dp(6), x1, y1 + dp(6)], radius=dp(28), fill=INK)
    d.rounded_rectangle([x0, y0, x1, y1], radius=dp(28), fill=CREAM, outline=INK, width=int(dp(3)))
    ix0, ix1 = x0 + dp(20), x1 - dp(20)
    J.planet(im, ix0 + dp(22), y0 + dp(42), dp(22), 'rumbo', badge=False); d = ImageDraw.Draw(im)
    d.text((ix0 + dp(56), y0 + dp(28)), 'Rumbo a Casa', font=F(18), fill=INK)
    d.text((ix0 + dp(56), y0 + dp(52)), 'Tu nivel: Hábil · 45%', font=F(12, False), fill=MUTED)
    d.text((ix0, y0 + dp(92)), '¿Cómo quieres jugar hoy?', font=F(18), fill=INK)
    y = y0 + dp(124); sel = 'Desafío'
    for name, n, what, hits in MODES:
        on = name == sel
        rh = dp(84)
        fill = (255, 240, 200) if on else (246, 238, 222)
        d.rounded_rectangle([ix0, y + dp(3), ix1, y + rh + dp(3)], radius=dp(18), fill=INK)
        d.rounded_rectangle([ix0, y, ix1, y + rh], radius=dp(18), fill=fill, outline=INK, width=int(dp(3 if on else 1.5)))
        bars(d, ix0 + dp(14), y + dp(34), n, SUN if on else MEM)
        d.text((ix0 + dp(56), y + dp(12)), name, font=F(16), fill=INK)
        if name == 'A tu medida': A.tag(d, ix0 + dp(56) + d.textlength(name, font=F(16)) + dp(8), y + dp(13), 'Recomendado', LIME)
        P.wrap(d, ix0 + dp(56), y + dp(36), what, F(11, False), MUTED, ix1 - ix0 - dp(120))
        d.text((ix1 - dp(14), y + dp(22)), hits, font=F(13), fill=INK, anchor='ra')
        d.text((ix1 - dp(14), y + dp(40)), 'aciertos', font=F(10, False), fill=MUTED, anchor='ra')
        if on:
            d.ellipse([ix1 - dp(34), y + rh - dp(30), ix1 - dp(12), y + rh - dp(8)], fill=LIME, outline=INK, width=int(dp(2)))
            P.check(d, ix1 - dp(23), y + rh - dp(19), dp(11))
        y += rh + dp(10)
    # reloj
    y += dp(6)
    d.text((ix0, y), 'Reloj', font=F(13), fill=INK)
    segw = (ix1 - ix0) / 2
    for i, (lab, sub) in enumerate([('Sin reloj', 'a tu ritmo'), ('Con reloj', 'partida de 2 min')]):
        on = i == 0
        sx = ix0 + i * segw
        d.rounded_rectangle([sx + dp(2), y + dp(22), sx + segw - dp(2), y + dp(66)], radius=dp(16), fill=(255, 240, 200) if on else (246, 238, 222), outline=INK, width=int(dp(3 if on else 1.5)))
        d.text((sx + segw / 2, y + dp(36)), lab, font=F(13), fill=INK, anchor='mm')
        d.text((sx + segw / 2, y + dp(54)), sub, font=F(10, False), fill=MUTED, anchor='mm')
    y += dp(84)
    d.rounded_rectangle([ix0, y + dp(4), ix1, y + dp(52)], radius=dp(22), fill=INK)
    d.rounded_rectangle([ix0, y, ix1, y + dp(48)], radius=dp(22), fill=SUN, outline=INK, width=int(dp(3)))
    P.play_tri(d, ix0 + dp(92), y + dp(24), dp(9))
    d.text((W / 2 + dp(10), y + dp(24)), 'Jugar en Desafío', font=F(17), fill=INK, anchor='mm')
    y += dp(62)
    P.wrap(d, ix0, y, 'Cada paso se ajusta a tu edad (18 a 64). Suave, Desafío y Experto nunca bajan tu avance.', F(11, False), MUTED, ix1 - ix0)
    return im


# ------------------------------------------------------------------ 3. El hilo conductor (lámina para Ricardo)

def d3():
    im = P.sky(73); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(34)), 'Cómo se decide la dificultad', font=F(22), fill=WHITE, anchor='lm')
    d.text((dp(20), dp(60)), 'Tres capas, siempre en este orden', font=F(13, False), fill=DIM, anchor='lm')
    x0, x1 = dp(16), W - dp(16)

    def block(y, num, col, title, lines, h):
        d.rounded_rectangle([x0, y + dp(5), x1, y + h + dp(5)], radius=dp(22), fill=INK)
        d.rounded_rectangle([x0, y, x1, y + h], radius=dp(22), fill=CREAM, outline=INK, width=int(dp(3)))
        P.disc(d, x0 + dp(30), y + dp(30), dp(16), col, 2, 2)
        d.text((x0 + dp(30), y + dp(30)), num, font=F(16), fill=INK, anchor='mm')
        d.text((x0 + dp(56), y + dp(30)), title, font=F(16), fill=INK, anchor='lm')
        yy = y + dp(56)
        for ln in lines:
            yy = P.wrap(d, x0 + dp(20), yy, ln, F(11, False), MUTED, x1 - x0 - dp(40)) + dp(4)
        return yy

    def arrow(y):
        d.polygon([(W / 2 - dp(10), y), (W / 2 + dp(10), y), (W / 2, y + dp(12))], fill=SUN)

    # 1. edad
    y = dp(84)
    yy = block(y, '1', SKY, 'Tu edad pone la regla', [
        'Decide cuántos aciertos busca el juego "a tu medida" y qué tan rápido sube.',
        'Ya existe hoy (lo eliges al comenzar): es igual en todos los juegos.'], dp(200))
    cols = ['', 'Menos de 18', '18 a 64', '65 o más']
    rows = [('Aciertos buscados', '80%', '80%', '85%'), ('Velocidad al subir', 'rápida', 'normal', 'pausada'),
            ('Peso del tiempo', 'medio', 'medio', 'bajo')]
    tx = [x0 + dp(20), x0 + dp(150), x0 + dp(235), x0 + dp(310)]
    ty = yy + dp(2)
    for c, t in zip(cols, tx): d.text((t, ty), c, font=F(10), fill=INK)
    for r in rows:
        ty += dp(16)
        for c, t, i in zip(r, tx, range(4)): d.text((t, ty), c, font=F(10, i == 0), fill=MUTED if i else INK)
    arrow(y + dp(206))
    # 2. nivel
    y = dp(304)
    block(y, '2', MEM, 'Tu nivel en cada juego es tu avance', [
        'El juego se ajusta solo mientras juegas (sube si aciertas, baja si fallas) y guarda dónde quedaste: '
        'ese es el % de la carta y la línea de etapas.',
        'Se mueve con las partidas "A tu medida" y con un Desafío superado. Así el avance compara siempre '
        'lo mismo, igual que las marcas (tu brújula, tu vistazo...).'], dp(158))
    arrow(y + dp(164))
    # 3. hoy eliges
    y = dp(478)
    yy = block(y, '3', SUN, 'Hoy eliges cómo jugar', [
        'Parte desde tu nivel y cambia cuántos aciertos busca el juego. Los pasos se ajustan a la edad.'], dp(254))
    cols = ['', 'Menos de 18', '18 a 64', '65 o más']
    rows = [('Suave', '90% · −1', '90% · −1', '92% · −1'), ('A tu medida', '80%', '80%', '85%'),
            ('Desafío', '70% · +1', '70% · +1', '75% · +½'), ('Experto', '60% · +2', '60% · +2', '65% · +1')]
    ty = yy + dp(2)
    for c, t in zip(cols, tx): d.text((t, ty), c, font=F(10), fill=INK)
    for r in rows:
        ty += dp(18)
        for c, t, i in zip(r, tx, range(4)): d.text((t, ty), c, font=F(11 if i == 0 else 10, i == 0), fill=MUTED if i else INK)
    ty += dp(24)
    P.wrap(d, x0 + dp(20), ty, 'aciertos buscados · niveles desde donde parte. Experto se abre al llegar a "Hábil" en ese juego.', F(10, False), MUTED, x1 - x0 - dp(40))
    # pie
    y = dp(748)
    d.text((W / 2, y), 'Cualquier modo hace crecer tu planeta,', font=F(13), fill=WHITE, anchor='ma')
    d.text((W / 2, y + dp(20)), 'tu racha y tu liga.', font=F(13), fill=WHITE, anchor='ma')
    d.text((W / 2, y + dp(50)), 'Base: regla del 85% (Wilson et al., 2019); escalera', font=F(10, False), fill=DIM, anchor='ma')
    d.text((W / 2, y + dp(64)), 'ponderada (Kaernbach, 1991); aprendizaje con pocos', font=F(10, False), fill=DIM, anchor='ma')
    d.text((W / 2, y + dp(78)), 'errores en mayores; elegir da motivación (Ryan y Deci).', font=F(10, False), fill=DIM, anchor='ma')
    return im


if __name__ == '__main__':
    shots = [d1(), d2(), d3()]
    names = ['1. La carta', '2. Cómo quieres jugar', '3. El hilo conductor']
    for i, s in enumerate(shots, 1):
        s.convert('RGB').resize((W * 3 // 4, H * 3 // 4), Image.LANCZOS).save(f'{P.OUT}/juegos-dificultad-{i}.png')
    k = 0.5; sw, sh = int(W * k), int(H * k); gap = 30; cap = 60
    sheet = Image.new('RGB', (3 * sw + 4 * gap, sh + cap + 2 * gap), (2, 3, 16)); sd = ImageDraw.Draw(sheet)
    for i, s in enumerate(shots):
        x = gap + i * (sw + gap); y = gap
        sd.text((x + sw / 2, y + cap / 2), names[i], font=ImageFont.truetype(P.FB, 28), fill=WHITE, anchor='mm')
        sheet.paste(s.convert('RGB').resize((sw, sh), Image.LANCZOS), (x, y + cap))
    sheet.save(f'{P.OUT}/juegos-dificultad.png')
    print('OK')
