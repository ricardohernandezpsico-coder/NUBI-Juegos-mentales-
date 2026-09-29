# Nubi al centro con indicadores claros (29-sep). Ricardo: sin orbes (pueden confundir); Nubi en el centro, de mucha
# calidad, con un halo a sus espaldas, y los indicadores a su alrededor o a un costado, que se vean con claridad, con
# avances o retrocesos, que digan el estado actual, sin saturar de colores ni de indicadores. Maqueta PIL, NO la app.
# Reglas de estas propuestas:
#   · Un solo color para las barras (el lavanda de Nubi); el color NO distingue áreas: las distingue el NOMBRE.
#   · Barra = tu avance en el área (0-100; las marcas finas son las 5 etapas). Debajo, la etapa en palabras.
#   · Cambio de la semana con FORMA + NÚMERO + palabra: ▲ +4 (sube), ▼ −2 (baja un poco), = (igual). Sube en sol,
#     baja en lila suave (nunca rojo: sin culpa), igual en gris. Nunca solo color.
#   · Nubi resume en una frase; nada más compite con ella.
# Uso: python3 tools/previews/nubi_indicadores.py  ->  docs/previews/nubi-indicadores.png
import os, math, importlib.util
from PIL import Image, ImageDraw
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('nh', os.path.join(HERE, 'nubi_halo.py'))
NH = importlib.util.module_from_spec(spec); spec.loader.exec_module(NH)
AL, NM, NS, N, P = NH.AL, NH.NM, NH.NS, NH.N, NH.P
F, dp, W = P.F, P.dp, P.W
INK, SUN, WHITE, DIM = P.INK, P.SUN, P.WHITE, P.DIM
ov = AL.ov
BAR = (196, 186, 255)          # lavanda de Nubi
DOWN = (178, 168, 232)         # lila suave
SAME = (150, 156, 196)
AREAS = [('memoria', 'Memoria', 3, 60, 4), ('atencion', 'Atención', 3, 35, 2), ('razonamiento', 'Razonamiento', 2, 80, 0),
         ('lenguaje', 'Lenguaje', 1, 30, -2), ('calculo', 'Cálculo', 2, 50, 1), ('velocidad', 'Velocidad', 4, 20, 6)]
STAGES = ['Inicio', 'Base', 'Intermedio', 'Avanzado', 'Maestro']


def avance(stage, pct): return (stage - 1) * 20 + pct * 0.2


def halo(im, cx, cy, r):
    """El halo a espaldas de Nubi: resplandor suave, dos aros finos y unas pocas chispas."""
    NS.glow(im, cx, cy, r * 1.15, (150, 140, 255), 120)
    NS.glow(im, cx, cy, r * 0.7, (190, 225, 255), 90)
    for rr, a in ((r * 0.92, 70), (r * 1.04, 36)):
        ov(im, lambda d, rr=rr, a=a: d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], outline=(220, 215, 255, a), width=int(dp(1.6))))
    d = ImageDraw.Draw(im)
    for ang, s in [(-58, 5), (-128, 3.5), (18, 3), (152, 4)]:
        a = math.radians(ang); x, y = cx + math.cos(a) * r * 0.98, cy + math.sin(a) * r * 0.98
        NS.glow(im, x, y, dp(s * 2.2), (230, 225, 255), 150)
        d = ImageDraw.Draw(im); d.polygon(N.sparkle(x, y, dp(s)), fill=WHITE)


def nubi(im, cx, cy, size, pose='hola'):
    halo(im, cx, cy, size * 0.62)
    im.alpha_composite(NH.nubi_sprite(pose, size), (int(cx - size / 2), int(cy - size / 2)))


def trend(d, x, y, delta, big=False, anchor='l'):
    """▲ +4 / ▼ −2 / = : forma + número (+ palabra si big)."""
    f = F(15 if big else 13)
    s = dp(5.5 if big else 4.5)
    if delta > 0: col, word, txt = SUN, 'sube', f'+{delta}'
    elif delta < 0: col, word, txt = DOWN, 'baja un poco', f'−{-delta}'
    else: col, word, txt = SAME, 'igual', ''
    label = (txt + ('  ' + word if big else '')).strip() if delta else ('igual' if True else '')
    tw = d.textlength(label, font=f) + s * 2.6
    x0 = x - tw if anchor == 'r' else x
    cy = y + dp(8 if big else 7)
    if delta > 0: d.polygon([(x0, cy + s * 0.8), (x0 + s * 2, cy + s * 0.8), (x0 + s, cy - s * 0.9)], fill=col)
    elif delta < 0: d.polygon([(x0, cy - s * 0.8), (x0 + s * 2, cy - s * 0.8), (x0 + s, cy + s * 0.9)], fill=col)
    else:
        d.line([(x0, cy - s * 0.35), (x0 + s * 2, cy - s * 0.35)], fill=col, width=int(dp(1.8)))
        d.line([(x0, cy + s * 0.45), (x0 + s * 2, cy + s * 0.45)], fill=col, width=int(dp(1.8)))
    d.text((x0 + s * 2.6, cy), label, font=f, fill=col, anchor='lm')


def bar(im, x, y, w, value, h=None):
    h = h or dp(7)
    ov(im, lambda d: d.rounded_rectangle([x, y, x + w, y + h], radius=h / 2, fill=(255, 255, 255, 38)))
    d = ImageDraw.Draw(im)
    fw = max(h, w * value / 100)
    NS.glow(im, x + fw, y + h / 2, h * 1.6, BAR, 120)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([x, y, x + fw, y + h], radius=h / 2, fill=BAR)
    for k in range(1, 5):   # marcas de las 5 etapas
        xx = x + w * k / 5
        ov(im, lambda dd, xx=xx: dd.line([(xx, y - dp(2)), (xx, y + h + dp(2))], fill=(10, 10, 40, 170), width=int(dp(1.4))))


def area_block(im, x, y, w, a, align='l'):
    key, name, st, pct, dl = a
    d = ImageDraw.Draw(im)
    tx = x if align == 'l' else x + w
    d.text((tx, y), name, font=F(15), fill=WHITE, anchor='la' if align == 'l' else 'ra')
    bar(im, x, y + dp(24), w, avance(st, pct))
    d = ImageDraw.Draw(im)
    if align == 'l':
        d.text((x, y + dp(38)), STAGES[st - 1], font=F(13, False), fill=DIM, anchor='la')
        trend(d, x + w, y + dp(37), dl, anchor='r')
    else:
        d.text((x + w, y + dp(38)), STAGES[st - 1], font=F(13, False), fill=DIM, anchor='ra')
        trend(d, x, y + dp(37), dl, anchor='l')


def base_panel(seed, h):
    im = NM.nebula_bg(W, int(h), seed, stars=90)
    d = ImageDraw.Draw(im)
    d.text((dp(20), dp(34)), 'Hoy', font=F(28), fill=WHITE, anchor='lm')
    d.text((W - dp(20), dp(34)), 'racha 12 días', font=F(14), fill=SUN, anchor='rm')
    return im


def button(im, y, text):
    d = ImageDraw.Draw(im); x0, x1, h = dp(20), W - dp(20), dp(54)
    d.rounded_rectangle([x0, y + dp(5), x1, y + h + dp(5)], radius=dp(20), fill=INK)
    d.rounded_rectangle([x0, y, x1, y + h], radius=dp(20), fill=SUN, outline=INK, width=int(dp(3)))
    d.text((W / 2, y + h / 2), text, font=F(18), fill=INK, anchor='mm')


# ---------- A · Dos alas ----------
def prop_a():
    H = dp(570); im = base_panel(61, H)
    cx, cy = W / 2, dp(262)
    nubi(im, cx, cy, dp(176))
    NH.bubble(im, cx, dp(64), 'Nubi', '4 áreas subieron esta semana', tail_x=cx)
    colw = dp(104)
    for i, a in enumerate(AREAS[:3]):
        area_block(im, dp(16), dp(170) + i * dp(86), colw, a, 'l')
    for i, a in enumerate(AREAS[3:]):
        area_block(im, W - dp(16) - colw, dp(170) + i * dp(86), colw, a, 'r')
    d = ImageDraw.Draw(im)
    d.text((W / 2, dp(446)), 'Toca un área para ver sus juegos y su historia', font=F(13, False), fill=DIM, anchor='mm')
    button(im, dp(486), 'Entrenar hoy')
    return im


# ---------- B · Nubi resume, la lista debajo ----------
def prop_b():
    H = dp(700); im = base_panel(63, H)
    cx, cy = W / 2, dp(176)
    nubi(im, cx, cy, dp(140))
    NH.bubble(im, cx, dp(56), 'Nubi', 'Velocidad es la que más subió', tail_x=cx)
    y0 = dp(282)
    d = ImageDraw.Draw(im)
    d.text((dp(20), y0), 'TU AVANCE', font=F(13), fill=SUN)
    d.text((W - dp(20), y0), 'esta semana', font=F(13), fill=SUN, anchor='ra')
    for i, (key, name, st, pct, dl) in enumerate(AREAS):
        y = y0 + dp(30) + i * dp(50)
        d = ImageDraw.Draw(im)
        d.text((dp(20), y), name, font=F(15), fill=WHITE)
        d.text((dp(20), y + dp(20)), STAGES[st - 1], font=F(13, False), fill=DIM)
        bar(im, dp(136), y + dp(12), dp(170), avance(st, pct))
        d = ImageDraw.Draw(im); trend(d, W - dp(20), y + dp(5), dl, anchor='r')
        if i < 5: ov(im, lambda dd, y=y: dd.line([(dp(20), y + dp(44)), (W - dp(20), y + dp(44))], fill=(255, 255, 255, 22), width=int(dp(1))))
    button(im, dp(620), 'Entrenar hoy')
    return im


# ---------- C · Un área a la vez ----------
def prop_c():
    H = dp(700); im = base_panel(65, H)
    cx, cy = W / 2, dp(214)
    nubi(im, cx, cy, dp(170), 'coach')
    NH.bubble(im, cx, dp(58), 'Nubi', 'Memoria subió 4 esta semana', tail_x=cx)
    d = ImageDraw.Draw(im)
    for sx in (-1, 1):   # flechas para cambiar de área
        x = cx + sx * dp(160); y = cy
        ov(im, lambda dd, x=x: dd.ellipse([x - dp(20), y - dp(20), x + dp(20), y + dp(20)], fill=(255, 255, 255, 30)))
        d = ImageDraw.Draw(im)
        d.polygon([(x - sx * dp(4), y - dp(8)), (x + sx * dp(6), y), (x - sx * dp(4), y + dp(8))], fill=WHITE)
    key, name, st, pct, dl = AREAS[0]
    y = dp(338)
    d.text((W / 2, y), name, font=F(26), fill=WHITE, anchor='mm')
    d.text((W / 2, y + dp(28)), f'Etapa {STAGES[st - 1]} · {int(avance(st, pct))} de 100', font=F(15, False), fill=DIM, anchor='mm')
    bar(im, dp(40), y + dp(50), W - dp(80), avance(st, pct), h=dp(12))
    d = ImageDraw.Draw(im)
    for k, nm in enumerate(STAGES):
        d.text((dp(40) + (W - dp(80)) * (k + 0.5) / 5, y + dp(72), ), nm, font=F(13, False), fill=WHITE if k == st - 1 else DIM, anchor='mm')
    trend(d, W / 2 - dp(62), y + dp(94), dl, big=True)
    # últimas 4 semanas (línea simple, un color)
    gx0, gx1, gy0, gy1 = dp(70), W - dp(70), y + dp(136), y + dp(196)
    vals = [44, 47, 48, 52]
    pts = [(gx0 + (gx1 - gx0) * i / 3, gy1 - (gy1 - gy0) * (v - 40) / 15) for i, v in enumerate(vals)]
    d.line(pts, fill=BAR, width=int(dp(2.5)), joint='curve')
    for i, (x, yy) in enumerate(pts):
        d.ellipse([x - dp(4), yy - dp(4), x + dp(4), yy + dp(4)], fill=BAR if i < 3 else SUN, outline=INK, width=int(dp(1)))
        d.text((x, gy1 + dp(10)), ['hace 3', 'hace 2', 'pasada', 'esta'][i], font=F(13, False), fill=DIM, anchor='ma')
    for i in range(6):   # puntos de las 6 áreas
        x = W / 2 + (i - 2.5) * dp(16); r = dp(4 if i else 5)
        if i == 0: d.ellipse([x - r, y + dp(232) - r, x + r, y + dp(232) + r], fill=WHITE)
        else: ov(im, lambda dd, x=x, r=r: dd.ellipse([x - r, y + dp(232) - r, x + r, y + dp(232) + r], outline=(255, 255, 255, 120), width=int(dp(1.4))))
    button(im, dp(620), 'Jugar Memoria')
    return im


PROPS = [('A', 'Dos alas', prop_a, 'Nubi al centro con su halo; tres áreas a cada lado, cada una con su barra, su etapa y cómo cambió '
          'esta semana. Todo a la vista, sin orbes.'),
         ('B', 'Nubi resume, la lista abajo', prop_b, 'Nubi arriba dice lo más importante; debajo, las 6 áreas en filas limpias. '
          'La más fácil de leer, pensando en adultos mayores.'),
         ('C', 'Un área a la vez', prop_c, 'Nubi grande con una sola área debajo, en detalle; con las flechas o deslizando se '
          'pasa a la siguiente. La que menos satura.')]


def legend(im, y):
    d = ImageDraw.Draw(im)
    d.text((dp(20), y), 'Cómo se lee (igual en las tres)', font=F(18), fill=SUN); y += dp(38)
    bar(im, dp(24), y + dp(8), dp(120), 52)
    d = ImageDraw.Draw(im)
    P.wrap(d, dp(160), y, 'Barra: tu avance en el área, de 0 a 100. Las marcas finas separan las 5 etapas. Un solo color: '
           'el nombre dice qué área es.', F(14, False), WHITE, W - dp(180))
    y += dp(76)
    for i, (dl, txt) in enumerate([(4, 'Subió 4 puntos esta semana.'), (-2, 'Bajó un poco (2 puntos). Sin rojo y sin culpa: se recupera jugando.'),
                                   (0, 'Igual que la semana pasada.')]):
        yy = y + i * dp(44)
        trend(d, dp(24), yy, dl)
        P.wrap(d, dp(110), yy, txt, F(14, False), WHITE, W - dp(130))
    return y + dp(140)


def sheet():
    panels = [(l, n, fn(), t) for l, n, fn, t in PROPS]
    H = dp(84) + sum(p.size[1] + dp(110) for _, _, p, _ in panels) + dp(230)
    im = Image.new('RGBA', (W, int(H)), (10, 10, 30, 255)); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(26)), 'Nubi al centro · indicadores claros', font=F(22), fill=SUN)
    d.text((dp(20), dp(58)), 'Sin orbes. Barras de un color; sube o baja con flecha y número.', font=F(13, False), fill=DIM)
    y = dp(84)
    for letter, name, pn, txt in panels:
        AL.paste_round(im, pn, 0, y, dp(24))
        d = ImageDraw.Draw(im)
        d.text((dp(20), y + pn.size[1] + dp(12)), f'{letter} · {name}', font=F(18), fill=WHITE)
        P.wrap(d, dp(20), y + pn.size[1] + dp(40), txt, F(13, False), DIM, W - dp(40))
        y += pn.size[1] + dp(110)
    legend(im, y)
    out = os.path.join(P.OUT, 'nubi-indicadores.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
