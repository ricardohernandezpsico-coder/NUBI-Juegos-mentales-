# Avance nuevo (30-sep). Ricardo: "información innecesaria y pobre, muchos datos en filas que no traen información real
# para el usuario". Propuesta: que la pestaña responda "¿qué descubrí de mí y cómo voy cambiando?" con pocas piezas:
#   1. Nubi científica con el hallazgo de la semana (en palabras).
#   2. Tu semana: días jugados contra tu meta (de la bienvenida), racha y desafíos.
#   3. Lo que descubriste: las medidas de los juegos estrella con su evolución dicha en palabras + mini gráfico.
#   4. Desde tu punto de partida: cada área, dónde empezaste (marca) y dónde estás (barra).
#   5. Tu liga en una línea, con compartir.
# Se van: la campana de percentiles (supuesto, no dato), el radar de áreas, la maestría en XP, la lista de 19 juegos
# con liga y etapa, y el historial en "puntos".
# Maqueta PIL, NO la app.  Uso: python3 tools/previews/avance_nuevo.py  ->  docs/previews/avance-nuevo.png
import os, importlib.util
from PIL import Image, ImageDraw
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('nv', os.path.join(HERE, 'navegacion_nubi.py'))
NV = importlib.util.module_from_spec(spec); spec.loader.exec_module(NV)
JN, NS, NM, P, ov = NV.JN, NV.NS, NV.NM, NV.P, NV.ov
F, dp, W = P.F, P.dp, P.W
INK, SUN, WHITE, DIM, CORAL, LIME, SKY = P.INK, P.SUN, P.WHITE, P.DIM, P.CORAL, P.LIME, P.SKY
BAR, LINE = JN.BAR, NS.LINE
PH = dp(1400)
STAGES = JN.STAGES
JN.PH = PH     # teléfonos altos: la pantalla se ve entera (en la app se desplaza)


def section(d, y, title, hint=None):
    d.text((dp(20), y), title, font=F(14), fill=SUN)
    if hint: d.text((W - dp(20), y + dp(1)), hint, font=F(14, False), fill=DIM, anchor='ra')
    return y + dp(28)


def navbar(im):
    NV.PH = PH
    NV.navbar3(im, 2)


def nubi_says(im, y, title, text):
    im.alpha_composite(JN.nubi_sprite(JN.scientist, dp(130)), (int(dp(4)), int(y)))
    d = ImageDraw.Draw(im)
    bx0, by0, bw, bh = dp(138), y + dp(18), W - dp(154), dp(94)
    d.rounded_rectangle([bx0, by0 + dp(3), bx0 + bw, by0 + bh + dp(3)], radius=dp(18), fill=LINE)
    d.rounded_rectangle([bx0, by0, bx0 + bw, by0 + bh], radius=dp(18), fill=(252, 250, 255), outline=LINE, width=int(dp(2)))
    d.polygon([(bx0 + dp(2), by0 + dp(36)), (bx0 - dp(12), by0 + dp(48)), (bx0 + dp(2), by0 + dp(58))], fill=(252, 250, 255))
    d.line([(bx0, by0 + dp(36)), (bx0 - dp(12), by0 + dp(48)), (bx0, by0 + dp(58))], fill=LINE, width=int(dp(2)))
    d.text((bx0 + dp(14), by0 + dp(10)), title, font=F(14, False), fill=(96, 88, 140))
    P.wrap(d, bx0 + dp(14), by0 + dp(30), text, F(16), INK, bw - dp(28))
    return y + dp(136)


def week(im, y, played, goal, today=5):
    d = ImageDraw.Draw(im)
    days = ['L', 'M', 'M', 'J', 'V', 'S', 'D']
    x0, step, r = dp(34), dp(46), dp(15)
    for i, lb in enumerate(days):
        cx, cy = x0 + i * step, y + dp(16)
        if i in played:
            d.ellipse([cx - r, cy - r + dp(2), cx + r, cy + r + dp(2)], fill=INK)
            d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=BAR, outline=INK, width=int(dp(2)))
            d.line([(cx - r * 0.42, cy), (cx - r * 0.08, cy + r * 0.36), (cx + r * 0.46, cy - r * 0.34)], fill=INK, width=int(dp(2.6)), joint='curve')
        else:
            ov(im, lambda dd, cx=cx, cy=cy: dd.ellipse([cx - r, cy - r, cx + r, cy + r], outline=(255, 255, 255, 70), width=int(dp(1.6))))
        if i == today:
            ov(im, lambda dd, cx=cx, cy=cy: dd.ellipse([cx - r - dp(4), cy - r - dp(4), cx + r + dp(4), cy + r + dp(4)], outline=SUN + (255,), width=int(dp(2))))
        ImageDraw.Draw(im).text((cx, cy + dp(28)), lb, font=F(13, False), fill=DIM, anchor='mm')
    d = ImageDraw.Draw(im)
    n = len(played)
    msg = f'{n} de {goal} días de tu meta' + (' · ¡meta cumplida!' if n >= goal else '')
    d.text((dp(20), y + dp(56)), msg, font=F(16), fill=WHITE)
    return y + dp(86)


def spark(im, x, y, w, h, vals, lower_better, col):
    lo, hi = min(vals), max(vals); sp = (hi - lo) or 1
    pts = []
    for i, v in enumerate(vals):
        t = (v - lo) / sp
        pts.append((x + w * i / (len(vals) - 1), y + h * (t if lower_better else 1 - t)))
    d = ImageDraw.Draw(im)
    d.line(pts, fill=col, width=int(dp(2.4)), joint='curve')
    lx, ly = pts[-1]
    d.ellipse([lx - dp(4), ly - dp(4), lx + dp(4), ly + dp(4)], fill=SUN, outline=INK, width=int(dp(1.4)))


def measure(im, y, name, game, value, sentence, vals, lower_better, tag=None):
    d = ImageDraw.Draw(im)
    d.text((dp(20), y), name, font=F(17), fill=WHITE)
    d.text((dp(20), y + dp(22)), game, font=F(14, False), fill=DIM)
    d.text((W - dp(20), y), value, font=F(20), fill=SUN, anchor='ra')
    spark(im, W - dp(118), y + dp(30), dp(96), dp(26), vals, lower_better, BAR)
    d = ImageDraw.Draw(im)
    yy = P.wrap(d, dp(20), y + dp(46), sentence, F(15, False), WHITE, W - dp(160))
    if tag:
        tw = d.textlength(tag, font=F(13)) + dp(16)
        d.rounded_rectangle([dp(20), yy + dp(4), dp(20) + tw, yy + dp(26)], radius=dp(10), fill=LIME, outline=INK, width=int(dp(1.5)))
        d.text((dp(20) + tw / 2, yy + dp(15)), tag, font=F(13), fill=INK, anchor='mm')
        yy += dp(30)
    ov(im, lambda dd: dd.line([(dp(20), yy + dp(10)), (W - dp(20), yy + dp(10))], fill=(255, 255, 255, 30), width=1))
    return yy + dp(22)


def from_start(im, y, name, start, now):
    d = ImageDraw.Draw(im)
    s0, s1 = STAGES[min(4, int(start // 20))], STAGES[min(4, int(now // 20))]
    d.text((dp(20), y), name, font=F(16), fill=WHITE)
    right = f'de {s0} a {s1}' if s0 != s1 else (f'sigue en {s1}' if now >= start else s1)
    d.text((W - dp(20), y), right, font=F(14, s0 != s1), fill=SUN if s0 != s1 else DIM, anchor='ra')
    bx, by, bw, bh = dp(20), y + dp(26), W - dp(40), dp(8)
    JN.bar(im, bx, by, bw, now, bh)
    # dónde empezaste: marca crema con triangulito
    mx = bx + bw * start / 100
    d = ImageDraw.Draw(im)
    d.line([(mx, by - dp(5)), (mx, by + bh + dp(5))], fill=(255, 244, 214), width=int(dp(2.4)))
    d.polygon([(mx - dp(5), by - dp(10)), (mx + dp(5), by - dp(10)), (mx, by - dp(4))], fill=(255, 244, 214))
    return y + dp(52)


def league(im, y):
    d = ImageDraw.Draw(im)
    sx, sy = dp(36), y + dp(18)
    d.polygon([(sx - dp(16), sy - dp(16)), (sx + dp(16), sy - dp(16)), (sx + dp(16), sy + dp(4)), (sx, sy + dp(18)), (sx - dp(16), sy + dp(4))], fill=(200, 208, 230), outline=INK, width=int(dp(2)))
    d.text((dp(64), y + dp(6)), 'Liga Plata', font=F(17), fill=WHITE)
    d.text((dp(64), y + dp(28)), '1.240 trofeos · 260 para Oro', font=F(14, False), fill=DIM)
    d.text((W - dp(20), y + dp(16)), 'Compartir', font=F(15), fill=SUN, anchor='ra')
    return y + dp(64)


def phone_con_datos():
    im = JN.phone(44); d = ImageDraw.Draw(im)
    NV.header_left(im, dp(36), 'Tu avance'); NV.top_buttons(im, dp(36))
    y = nubi_says(im, dp(64), 'Nubi · tu hallazgo de la semana', 'Tu vistazo es más rápido: ves lo mismo en menos tiempo.')
    d = ImageDraw.Draw(im)
    y = section(d, y, 'TU SEMANA', 'racha 12 días')
    y = week(im, y, played={0, 1, 3, 5}, goal=4)
    d = ImageDraw.Draw(im)
    d.text((dp(20), y - dp(4)), 'Desafíos de la semana: 2 de 3', font=F(14, False), fill=DIM)
    y += dp(34)
    y = section(ImageDraw.Draw(im), y, 'LO QUE DESCUBRISTE')
    y = measure(im, y, 'Tu vistazo', 'Rescate relámpago', '84 ms', 'Bajó de 120 a 84 ms en 3 semanas: ves en menos tiempo.',
                [120, 116, 108, 104, 96, 84], True)
    y = measure(im, y, 'Tu brújula', 'Rumbo a Casa', 'a 18% de casa', 'Tu mejor llegada hasta ahora. Lo que más te aleja es la distancia, no el rumbo.',
                [31, 27, 29, 24, 22, 18], True, tag='Tu récord')
    y = measure(im, y, 'Tu freno', 'Freno de Emergencia', '230 ms', 'Se mantiene parejo en tus últimas 5 partidas.',
                [236, 228, 233, 231, 230], True)
    d = ImageDraw.Draw(im)
    y = P.wrap(d, dp(20), y, 'Por descubrir: tu seguimiento, tu giro mental y 5 más.', F(15, False), DIM, W - dp(40))
    d.text((dp(20), y + dp(2)), 'Ver cómo descubrirlos', font=F(15), fill=SUN)
    y += dp(40)
    y = section(ImageDraw.Draw(im), y, 'DESDE TU PUNTO DE PARTIDA', 'hace 3 semanas')
    for name, s, n in [('Memoria', 34, 52), ('Atención', 38, 47), ('Razonamiento', 30, 36), ('Lenguaje', 14, 12), ('Cálculo', 22, 30), ('Velocidad', 44, 64)]:
        y = from_start(im, y, name, s, n)
    d = ImageDraw.Draw(im)
    d.text((dp(20), y), 'La marca crema es dónde empezaste.', font=F(14, False), fill=DIM)
    y += dp(34)
    y = section(ImageDraw.Draw(im), y, 'TU LIGA')
    y = league(im, y)
    ImageDraw.Draw(im).text((W / 2, y + dp(6)), 'Repetir mi punto de partida', font=F(15), fill=SUN, anchor='ma')
    navbar(im)
    return JN.rounded(im)


def phone_nuevo():
    im = JN.phone(45); d = ImageDraw.Draw(im)
    NV.header_left(im, dp(36), 'Tu avance'); NV.top_buttons(im, dp(36))
    y = nubi_says(im, dp(64), 'Nubi', 'Juega unos días y aquí te cuento lo que vamos descubriendo de ti.')
    d = ImageDraw.Draw(im)
    y = section(d, y, 'TU SEMANA', 'racha 2 días')
    y = week(im, y, played={4, 5}, goal=4)
    y += dp(8)
    y = section(ImageDraw.Draw(im), y, 'LO QUE DESCUBRISTE')
    d = ImageDraw.Draw(im)
    for game, what in [('Rescate relámpago', 'tu vistazo'), ('Rumbo a Casa', 'tu brújula interna')]:
        d.text((dp(20), y), f'Juega {game}', font=F(16), fill=WHITE)
        d.text((dp(20), y + dp(22)), f'para descubrir {what}', font=F(15, False), fill=DIM)
        d.text((W - dp(20), y + dp(10)), 'Jugar', font=F(15), fill=SUN, anchor='ra')
        y += dp(58)
    P.wrap(d, dp(20), y, 'Cada juego estrella te muestra algo propio. Con 3 partidas ya se ve cómo cambia.', F(14, False), DIM, W - dp(40))
    y += dp(56)
    y = section(ImageDraw.Draw(im), y, 'DESDE TU PUNTO DE PARTIDA')
    d = ImageDraw.Draw(im)
    P.wrap(d, dp(20), y, '3 juegos cortos para saber dónde empiezas. Después aquí verás cuánto cambias.', F(15, False), WHITE, W - dp(40))
    x0, x1, h, by = dp(20), W - dp(20), dp(50), y + dp(58)
    d.rounded_rectangle([x0, by + dp(5), x1, by + h + dp(5)], radius=dp(20), fill=INK)
    d.rounded_rectangle([x0, by, x1, by + h], radius=dp(20), fill=SUN, outline=INK, width=int(dp(3)))
    d.text((W / 2, by + h / 2), 'Encontrar mi punto de partida', font=F(17), fill=INK, anchor='mm')
    y = by + h + dp(36)
    y = section(ImageDraw.Draw(im), y, 'TU LIGA')
    league(im, y)
    navbar(im)
    return JN.rounded(im)


def sheet():
    gap = dp(56); SW = int(dp(40) * 2 + W * 3 + gap * 2); SH = int(dp(150) + PH + dp(140))
    im = NM.nebula_bg(SW, SH, 12, stars=200); d = ImageDraw.Draw(im)
    d.text((dp(40), dp(44)), 'Avance: qué descubriste de ti y cómo vas cambiando', font=F(34), fill=SUN)
    d.text((dp(40), dp(92)), 'Menos filas y más significado: cada pieza dice algo en palabras. La pantalla se desplaza (aquí se ve entera).',
           font=F(17, False), fill=DIM)
    y = dp(140); xs = [dp(40) + i * (W + gap) for i in range(3)]
    im.alpha_composite(phone_con_datos(), (int(xs[0]), int(y)))
    im.alpha_composite(phone_nuevo(), (int(xs[1]), int(y)))
    d = ImageDraw.Draw(im)
    x = xs[2]; yy = y + dp(10)
    d.text((x, yy), 'A · Con 3 semanas de juego', font=F(20), fill=WHITE); yy += dp(34)
    for s in ['Nubi científica cuenta el hallazgo de la semana, en palabras.',
              'Tu semana: los días que jugaste contra la meta que elegiste en la bienvenida.',
              'Lo que descubriste: cada medida de los juegos estrella con su evolución dicha en palabras y un mini gráfico (mejor hacia arriba). Solo compara partidas parecidas.',
              'Desde tu punto de partida: la marca crema es dónde empezaste; la barra, dónde estás. Tocar un área abre su detalle.',
              'Tu liga en una línea, con compartir.']:
        yy = P.wrap(d, x, yy, '· ' + s, F(16, False), WHITE, W) + dp(10)
    yy += dp(20)
    d.text((x, yy), 'B · Recién empieza', font=F(20), fill=WHITE); yy += dp(34)
    for s in ['Sin datos no se inventa nada: invita a jugar para descubrir algo propio.',
              'Si no hizo el punto de partida, aquí está el botón para hacerlo.']:
        yy = P.wrap(d, x, yy, '· ' + s, F(16, False), WHITE, W) + dp(10)
    yy += dp(20)
    d.text((x, yy), 'Se va de Avance', font=F(20), fill=CORAL); yy += dp(34)
    for s in ['La campana de percentiles: compara con una referencia supuesta, no con datos reales.',
              'El radar de áreas y la maestría en XP: repiten lo mismo con otro dibujo.',
              'La lista de 19 juegos con liga y etapa: está en Juegos, dentro de cada área.',
              'El historial en "puntos": un número sin significado para la persona.',
              'Las 6 barras iguales a las de Hoy: aquí se ven contra tu punto de partida.']:
        yy = P.wrap(d, x, yy, '· ' + s, F(16, False), WHITE, W) + dp(10)
    out = os.path.join(P.OUT, 'avance-nuevo.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
