# Hoy con Nubi: versión elegida (29-sep). Ricardo eligió A (Nubi al centro, tres áreas a cada lado) y que al tocar un
# área se abra la vista C (el detalle). Dos arreglos que pidió:
#   1. "Subió 4… ¿4 qué?": la escala es el AVANCE de 0 a 100 (cada etapa = 20). Nubi ya no dice números sueltos (dice en
#      palabras qué pasó); en el detalle se dice DE DÓNDE A DÓNDE: "Esta semana avanzó de 46 a 52 (de 100)" y "Te faltan
#      8 para Avanzado".
#   2. En A, los "▲ +4 / ▼ −2" molestaban a la vista: se sacan. El cambio se ve EN LA BARRA: lo avanzado esta semana es
#      el tramo sol del final (con una chispa); lo que bajó, un tramo punteado lila después del relleno. Sin números.
# Maqueta PIL, NO la app.  Uso: python3 tools/previews/nubi_hoy.py  ->  docs/previews/nubi-hoy.png
import os, math, importlib.util
from PIL import Image, ImageDraw
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('ni2', os.path.join(HERE, 'nubi_indicadores.py'))
NI = importlib.util.module_from_spec(spec); spec.loader.exec_module(NI)
NH, AL, NM, NS, N, P = NI.NH, NI.AL, NI.NM, NI.NS, NI.N, NI.P
F, dp, W = P.F, P.dp, P.W
INK, SUN, WHITE, DIM = P.INK, P.SUN, P.WHITE, P.DIM
BAR, DOWN, ov, STAGES = NI.BAR, NI.DOWN, NI.ov, NI.STAGES
# (clave, nombre, avance 0-100 hoy, cambio de la semana en puntos de avance)
AREAS = [('memoria', 'Memoria', 52, 6), ('atencion', 'Atención', 47, 3), ('razonamiento', 'Razonamiento', 36, 0),
         ('lenguaje', 'Lenguaje', 12, -5), ('calculo', 'Cálculo', 30, 2), ('velocidad', 'Velocidad', 64, 8)]


def stage_of(v): return min(4, int(v // 20))


def bar(im, x, y, w, v, dv, h=None):
    """Barra de avance 0-100 con el cambio de la semana dibujado en ella (sin números)."""
    h = h or dp(8)
    ov(im, lambda d: d.rounded_rectangle([x, y, x + w, y + h], radius=h / 2, fill=(255, 255, 255, 38)))
    before = max(0.0, v - dv) if dv > 0 else v
    X = lambda val: x + w * val / 100
    d = ImageDraw.Draw(im)
    if before > 0.5:
        d.rounded_rectangle([x, y, max(X(before), x + h), y + h], radius=h / 2, fill=BAR)
    if dv > 0:   # lo avanzado esta semana: tramo sol con chispa
        NS.glow(im, X(v), y + h / 2, h * 1.4, P.SUN, 110)
        d = ImageDraw.Draw(im)
        d.rounded_rectangle([max(x, X(before) - h / 2), y, X(v), y + h], radius=h / 2, fill=SUN)
        d.ellipse([X(v) - h * 0.22, y + h * 0.28, X(v) + h * 0.22, y + h * 0.72], fill=WHITE)
    elif dv < 0:  # lo que bajó esta semana: tramo punteado lila
        x0, x1 = X(v), X(v - dv)
        ov(im, lambda dd: dd.rounded_rectangle([x0 - h / 2, y, x1, y + h], radius=h / 2, fill=DOWN + (70,)))
        d = ImageDraw.Draw(im)
        n = max(3, int((x1 - x0) / dp(3)))
        for i in range(n + 1):   # contorno punteado
            xx = x0 + (x1 - x0) * i / n
            d.ellipse([xx - dp(0.9), y - dp(0.9), xx + dp(0.9), y + dp(0.9)], fill=DOWN)
            d.ellipse([xx - dp(0.9), y + h - dp(0.9), xx + dp(0.9), y + h + dp(0.9)], fill=DOWN)
    for k in range(1, 5):
        xx = x + w * k / 5
        ov(im, lambda dd, xx=xx: dd.line([(xx, y - dp(2)), (xx, y + h + dp(2))], fill=(10, 10, 40, 170), width=int(dp(1.4))))


def block(im, x, y, w, a, align):
    key, name, v, dv = a
    d = ImageDraw.Draw(im)
    anc = 'la' if align == 'l' else 'ra'; tx = x if align == 'l' else x + w
    d.text((tx, y), name, font=F(15), fill=WHITE, anchor=anc)
    bar(im, x, y + dp(24), w, v, dv)
    ImageDraw.Draw(im).text((tx, y + dp(40)), STAGES[stage_of(v)], font=F(13, False), fill=DIM, anchor=anc)


def screen_a():
    H = dp(560); im = NI.base_panel(61, H)
    cx, cy = W / 2, dp(262)
    NI.nubi(im, cx, cy, dp(176))
    NH.bubble(im, cx, dp(64), 'Nubi', 'Velocidad y Memoria avanzaron', tail_x=cx)
    colw = dp(104)
    for i, a in enumerate(AREAS[:3]):
        block(im, dp(16), dp(170) + i * dp(86), colw, a, 'l')
    for i, a in enumerate(AREAS[3:]):
        block(im, W - dp(16) - colw, dp(170) + i * dp(86), colw, a, 'r')
    # la mano que toca Memoria (solo en la maqueta)
    hx, hy = dp(100), dp(200)
    ov(im, lambda d: d.ellipse([hx - dp(16), hy - dp(16), hx + dp(16), hy + dp(16)], fill=(255, 255, 255, 70), outline=WHITE, width=int(dp(2))))
    NI.button(im, dp(470), 'Entrenar hoy')
    return im


def screen_c():
    H = dp(806); im = NI.base_panel(65, H)
    key, name, v, dv = AREAS[0]
    d = ImageDraw.Draw(im)
    d.text((W - dp(20), dp(34)), '✕' if False else '', font=F(14), fill=WHITE)
    cx, cy = W / 2, dp(200)
    NI.nubi(im, cx, cy, dp(160), 'coach')
    NH.bubble(im, cx, dp(52), 'Nubi', 'Esta semana Memoria avanzó', tail_x=cx)
    for sx in (-1, 1):
        x = cx + sx * dp(160); y = cy
        ov(im, lambda dd, x=x: dd.ellipse([x - dp(20), y - dp(20), x + dp(20), y + dp(20)], fill=(255, 255, 255, 30)))
        d = ImageDraw.Draw(im)
        d.polygon([(x - sx * dp(4), y - dp(8)), (x + sx * dp(6), y), (x - sx * dp(4), y + dp(8))], fill=WHITE)
    y = dp(318); st = stage_of(v)
    d.text((W / 2, y), name, font=F(26), fill=WHITE, anchor='mm')
    d.text((W / 2, y + dp(28)), f'Etapa {STAGES[st]} · {v} de 100', font=F(15, False), fill=DIM, anchor='mm')
    bar(im, dp(40), y + dp(52), W - dp(80), v, dv, h=dp(14))
    d = ImageDraw.Draw(im)
    for k, nm in enumerate(STAGES):
        d.text((dp(40) + (W - dp(80)) * (k + 0.5) / 5, y + dp(80)), nm, font=F(13, False), fill=WHITE if k == st else DIM, anchor='mm')
    yy = y + dp(112)
    d.polygon([(W / 2 - dp(128), yy + dp(12)), (W / 2 - dp(116), yy + dp(12)), (W / 2 - dp(122), yy + dp(2))], fill=SUN)
    d.text((W / 2 + dp(6), yy + dp(8)), f'Esta semana avanzó de {v - dv} a {v}', font=F(16), fill=WHITE, anchor='mm')
    nxt = (st + 1) * 20
    d.text((W / 2, yy + dp(34)), f'Te faltan {nxt - v} para {STAGES[st + 1]}', font=F(15, False), fill=DIM, anchor='mm')
    # las últimas 4 semanas, en la misma escala
    gx0, gx1, gy0, gy1 = dp(70), W - dp(70), yy + dp(70), yy + dp(140)
    vals = [40, 43, 46, 52]
    for g in (40, 60):
        gyy = gy1 - (gy1 - gy0) * (g - 38) / 16
        if gy0 - dp(4) <= gyy <= gy1:
            ov(im, lambda dd, gyy=gyy: dd.line([(gx0, gyy), (gx1, gyy)], fill=(255, 255, 255, 30), width=int(dp(1))))
    pts = [(gx0 + (gx1 - gx0) * i / 3, gy1 - (gy1 - gy0) * (val - 38) / 16) for i, val in enumerate(vals)]
    d = ImageDraw.Draw(im)
    d.line(pts, fill=BAR, width=int(dp(2.5)), joint='curve')
    for i, (x, py) in enumerate(pts):
        d.ellipse([x - dp(4.5), py - dp(4.5), x + dp(4.5), py + dp(4.5)], fill=BAR if i < 3 else SUN, outline=INK, width=int(dp(1)))
        d.text((x, py - dp(20)), str(vals[i]), font=F(13), fill=WHITE if i == 3 else DIM, anchor='mm')
        d.text((x, gy1 + dp(12)), ['hace 3', 'hace 2', 'pasada', 'esta'][i], font=F(13, False), fill=DIM, anchor='ma')
    for i in range(6):
        x = W / 2 + (i - 2.5) * dp(16); r = dp(4 if i else 5); py = gy1 + dp(52)
        if i == 0: d.ellipse([x - r, py - r, x + r, py + r], fill=WHITE)
        else: ov(im, lambda dd, x=x, r=r, py=py: dd.ellipse([x - r, py - r, x + r, py + r], outline=(255, 255, 255, 120), width=int(dp(1.4))))
    # invitación en vez de "Jugar Memoria": una pregunta de Nubi, el botón en primera persona y qué juego toca
    d = ImageDraw.Draw(im)
    d.text((W / 2, dp(656)), '¿Trabajamos tu memoria ahora?', font=F(17), fill=WHITE, anchor='mm')
    NI.button(im, dp(680), 'Entrenar mi memoria')
    d = ImageDraw.Draw(im)
    d.text((W / 2, dp(760)), 'Nubi eligió Secuencia Lumínica: hace días que no la juegas', font=F(14, False), fill=DIM, anchor='mm')
    return im


def legend(im, y):
    d = ImageDraw.Draw(im)
    d.text((dp(20), y), 'Cómo se lee la barra', font=F(18), fill=SUN); y += dp(36)
    rows = [(52, 6, 'Tu avance en el área, de 0 a 100. Cada etapa son 20 (las marcas finas).'),
            (52, 6, 'El tramo amarillo con chispa: lo que avanzaste esta semana.'),
            (36, -9, 'El tramo punteado: lo que bajó esta semana. Se recupera jugando.'),
            (36, 0, 'Sin tramo extra: esta semana quedó igual.')]
    for i, (v, dv, txt) in enumerate(rows):
        yy = y + i * dp(58)
        bar(im, dp(24), yy + dp(10), dp(110), v, dv if i else 0)
        d = ImageDraw.Draw(im)
        P.wrap(d, dp(150), yy, txt, F(14, False), WHITE, W - dp(170))
    return y + len(rows) * dp(58)


def options(im, y):
    """Otras formas de decirlo (en todas: pregunta + acción; nunca "Jugar Memoria")."""
    d = ImageDraw.Draw(im)
    d.text((dp(20), y), 'Otras formas de decirlo', font=F(18), fill=SUN); y += dp(36)
    for i, (q, b) in enumerate([('¿Trabajamos tu memoria ahora?', 'Entrenar mi memoria'),
                               ('¿Le damos un empujón a tu memoria?', 'Sí, vamos'),
                               ('Tu memoria está cerca de Avanzado', 'Seguir con memoria')]):
        yy = y + i * dp(92)
        d = ImageDraw.Draw(im)
        d.text((dp(24), yy), f'{i + 1}.  {q}', font=F(15), fill=WHITE)
        x0, x1, h = dp(40), dp(300), dp(40)
        d.rounded_rectangle([x0, yy + dp(28) + dp(4), x1, yy + dp(28) + h + dp(4)], radius=dp(16), fill=INK)
        d.rounded_rectangle([x0, yy + dp(28), x1, yy + dp(28) + h], radius=dp(16), fill=SUN, outline=INK, width=int(dp(2.5)))
        d.text(((x0 + x1) / 2, yy + dp(28) + h / 2), b, font=F(16), fill=INK, anchor='mm')


def sheet():
    a, c = screen_a(), screen_c()
    H = dp(84) + a.size[1] + dp(70) + c.size[1] + dp(40) + dp(300) + dp(330)
    im = Image.new('RGBA', (W, int(H)), (10, 10, 30, 255)); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(26)), 'Hoy con Nubi · versión elegida', font=F(22), fill=SUN)
    d.text((dp(20), dp(58)), 'Sin números sueltos: el cambio se ve en la barra.', font=F(13, False), fill=DIM)
    y = dp(84)
    AL.paste_round(im, a, 0, y, dp(24)); y += a.size[1] + dp(16)
    d = ImageDraw.Draw(im)
    d.text((W / 2, y + dp(18)), 'Al tocar Memoria se abre su detalle', font=F(16), fill=WHITE, anchor='mm')
    y += dp(54)
    AL.paste_round(im, c, 0, y, dp(24)); y += c.size[1] + dp(30)
    y = legend(im, y) + dp(30)
    options(im, y)
    out = os.path.join(P.OUT, 'nubi-hoy.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
