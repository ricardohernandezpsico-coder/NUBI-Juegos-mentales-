# Tu galaxia (29-sep). Ricardo trajo otra lámina de referencia y propuso: un MAPA DE ESTRELLAS como galaxia ovalada que
# gira lento; Nubi al centro; cada área un cúmulo de estrellas que brilla más o menos según lo trabajado y se va
# apagando si pasa tiempo sin jugar; se gira con el dedo; el cúmulo más cercano se toca y muestra datos del área;
# sirve también para revisar estadísticas. Maqueta PIL, NO la app.
# Decisión propuesta: DOS canales separados. TAMAÑO (cuántas estrellas) = lo logrado, la etapa del área: nunca baja
# (regla de la app). BRILLO = lo reciente (partidas de los últimos 14 días): se apaga despacio sin jugar y se enciende
# al volver; nunca se apaga del todo (sin culpa). Colores de las áreas: los revisados en anillo_luz.py.
# Uso: python3 tools/previews/galaxia.py  ->  docs/previews/galaxia.png
import os, math, random, importlib.util
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('al', os.path.join(HERE, 'anillo_luz.py'))
AL = importlib.util.module_from_spec(spec); spec.loader.exec_module(AL)
NM, NS, N, P = AL.NM, AL.NS, AL.N, AL.P
F, dp, W = P.F, P.dp, P.W
INK, SUN, CREAM, WHITE, DIM, LIME = P.INK, P.SUN, P.CREAM, P.WHITE, P.DIM, P.LIME
LINE, COL, LIGHT, NAMES, ORDER, ov = NS.LINE, AL.COL, AL.LIGHT, AL.NAMES, AL.ORDER, AL.ov
STAGE = {'memoria': 4, 'atencion': 3, 'razonamiento': 2, 'lenguaje': 1, 'calculo': 3, 'velocidad': 4}
BRIGHT = {'memoria': 1.0, 'atencion': 0.9, 'razonamiento': 0.6, 'lenguaje': 0.22, 'calculo': 0.5, 'velocidad': 0.8}
STAGE_NAME = ['Inicio', 'Base', 'Intermedio', 'Avanzado', 'Maestro']


def galaxy(im, cx, cy, rx, ry, rot, sel=None, stars=True, labels=True, only=None, seed=3):
    """Disco de la galaxia visto inclinado (óvalo); rot = giro en grados. Devuelve la posición de cada cúmulo."""
    rnd = random.Random(seed)
    # polvo de los brazos espirales (atrás y adelante se dibujan juntos: es tenue)
    NS.glow(im, cx, cy, rx * 0.55, (150, 140, 255), 90)
    NS.glow(im, cx, cy, rx * 0.25, (255, 230, 200), 110)
    def dust(d):
        for arm in range(2):
            for k in range(420):
                t = k / 420
                r = 0.12 + t * 0.95
                th = math.radians(rot) + arm * math.pi + t * 3.6 + rnd.gauss(0, 0.22)
                rr = r + rnd.gauss(0, 0.05)
                x, y = cx + math.cos(th) * rr * rx, cy + math.sin(th) * rr * ry
                a = int(40 + 120 * (1 - t) * rnd.random())
                s = dp(0.6 + rnd.random() * 1.1)
                col = rnd.choice([(200, 200, 255), (170, 220, 255), (255, 225, 200), (215, 190, 255)])
                d.ellipse([x - s, y - s, x + s, y + s], fill=col + (a,))
    if stars: ov(im, dust)
    ov(im, lambda d: d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], outline=(200, 210, 255, 40), width=int(dp(1.2))))
    pos = {}
    items = []
    for i, key in enumerate(ORDER):
        th = math.radians(rot + i * 60 + 90)
        x, y = cx + math.cos(th) * 0.8 * rx, cy + math.sin(th) * 0.8 * ry
        depth = (math.sin(th) + 1) / 2        # 0 = atrás (arriba), 1 = adelante (abajo)
        items.append((depth, key, x, y))
    for depth, key, x, y in sorted(items):
        if only and key not in only: continue
        cluster(im, x, y, key, 0.75 + 0.6 * depth, depth, sel == key)
        pos[key] = (x, y, depth)
        if labels:
            d = ImageDraw.Draw(im); s = 0.75 + 0.6 * depth
            d.text((x, y + dp(26) * s + dp(8)), NAMES[key], font=F(14 if depth > 0.45 else 13), fill=SUN if sel == key else (WHITE if depth > 0.35 else DIM), anchor='ma')
    return pos


def cluster(im, x, y, key, scale, depth, selected=False, bright=None):
    b = BRIGHT[key] if bright is None else bright
    n = 3 + STAGE[key] * 3
    rnd = random.Random(hash(key) % 997)
    col = tuple(int(c * (0.35 + 0.65 * b) + 70 * (1 - b)) for c in LIGHT[key])
    NS.glow(im, x, y, dp(30) * scale, LIGHT[key], int(40 + 150 * b * (0.6 + 0.4 * depth)))
    if selected:
        ov(im, lambda d: d.ellipse([x - dp(34) * scale, y - dp(28) * scale, x + dp(34) * scale, y + dp(28) * scale], outline=SUN + (230,), width=int(dp(2.2))))
    d = ImageDraw.Draw(im)
    for k in range(n):
        a = rnd.random() * 2 * math.pi; r = dp(20) * scale * math.sqrt(rnd.random())
        sx, sy = x + math.cos(a) * r, y + math.sin(a) * r * 0.8
        s = dp(2.2 + rnd.random() * 2.6) * scale
        if k < 3: s *= 1.5
        d.polygon(N.sparkle(sx, sy, s * 1.6), fill=col)
        if b > 0.4:
            d.ellipse([sx - s * 0.35, sy - s * 0.35, sx + s * 0.35, sy + s * 0.35], fill=WHITE)


def nubi(im, cx, cy, size):
    sp = NM.nubi_sprite(lambda c, x, y, k: NS.nubi(c, x, y, k, 'hola'), size)
    im.alpha_composite(sp, (int(cx - size / 2), int(cy - size / 2)))


def spark(d, x0, y0, w, h, vals, col):
    mx = max(vals) or 1
    pts = [(x0 + w * i / (len(vals) - 1), y0 + h - h * v / mx) for i, v in enumerate(vals)]
    d.line(pts, fill=col, width=int(dp(2.5)), joint='curve')
    for i, (x, y) in enumerate(pts):
        d.ellipse([x - dp(3.5), y - dp(3.5), x + dp(3.5), y + dp(3.5)], fill=col if i < len(pts) - 1 else SUN, outline=INK, width=int(dp(1)))


def screen():
    H = dp(960)
    im = NM.nebula_bg(W, int(H), 44, stars=180)
    d = ImageDraw.Draw(im)
    d.text((dp(20), dp(40)), 'Tu galaxia', font=F(28), fill=WHITE, anchor='lm')
    d.text((W - dp(20), dp(40)), 'Hoy nacieron 2 estrellas', font=F(14), fill=SUN, anchor='rm')
    cx, cy, rx, ry = W / 2, dp(226), dp(170), dp(96)
    galaxy(im, cx, cy, rx, ry, rot=-18, sel='memoria')
    nubi(im, cx, cy - dp(4), dp(92))
    d = ImageDraw.Draw(im)
    d.text((W / 2, dp(378)), '‹  desliza para girar  ·  toca un cúmulo  ›', font=F(13, False), fill=DIM, anchor='mm')
    # ventana del área tocada (diálogo: aquí sí va tarjeta)
    y0 = dp(404); x0, x1 = dp(16), W - dp(16)
    d.rounded_rectangle([x0, y0 + dp(5), x1, H - dp(20) + dp(5)], radius=dp(26), fill=LINE)
    d.rounded_rectangle([x0, y0, x1, H - dp(20)], radius=dp(26), fill=(26, 24, 70), outline=LINE, width=int(dp(2.5)))
    cluster(im, x0 + dp(46), y0 + dp(44), 'memoria', 0.85, 1.0)
    d = ImageDraw.Draw(im)
    d.text((x0 + dp(88), y0 + dp(26)), 'Memoria', font=F(22), fill=WHITE)
    d.text((x0 + dp(88), y0 + dp(56)), 'Etapa 4 · Avanzado · 15 estrellas', font=F(14, False), fill=DIM)
    y = y0 + dp(96)
    d.text((x0 + dp(20), y), 'BRILLO', font=F(13), fill=SUN)
    d.text((x0 + dp(20), y + dp(20)), 'Brilla fuerte: jugaste 4 veces esta semana', font=F(15, False), fill=WHITE)
    bx0, bx1 = x0 + dp(20), x1 - dp(20)
    ov(im, lambda dd: dd.rounded_rectangle([bx0, y + dp(48), bx1, y + dp(58)], radius=dp(5), fill=(255, 255, 255, 40)))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([bx0, y + dp(48), bx0 + (bx1 - bx0) * 0.9, y + dp(58)], radius=dp(5), fill=LIGHT['memoria'])
    y += dp(80)
    d.text((x0 + dp(20), y), 'PARTIDAS POR SEMANA', font=F(13), fill=SUN)
    spark(d, x0 + dp(28), y + dp(26), dp(150), dp(46), [2, 3, 3, 4], LIGHT['memoria'])
    for i, lb in enumerate(['hace 3', 'hace 2', 'pasada', 'esta']):
        d.text((x0 + dp(28) + dp(150) * i / 3, y + dp(84)), lb, font=F(13, False), fill=DIM, anchor='ma')
    d.text((x0 + dp(210), y + dp(30)), '12 partidas', font=F(18), fill=WHITE)
    d.text((x0 + dp(210), y + dp(56)), 'en 4 semanas', font=F(14, False), fill=DIM)
    y += dp(116)
    d.text((x0 + dp(20), y), 'TUS JUEGOS DE MEMORIA', font=F(13), fill=SUN)
    rows = [('bitacora', 'Bitácora de Misión', 'tu memoria a los 12 min: 5 de 6'), ('rumbo', 'Rumbo a Casa', 'a 18% de casa · hace 2 días'),
            ('secuencia', 'Secuencia Lumínica', 'sin jugar aún')]
    for i, (gid, name, sub) in enumerate(rows):
        yy = y + dp(28) + i * dp(50)
        P.game_planet(im, x0 + dp(36), yy + dp(14), dp(14), gid)
        d = ImageDraw.Draw(im)
        d.text((x0 + dp(60), yy), name, font=F(15), fill=WHITE)
        d.text((x0 + dp(60), yy + dp(20)), sub, font=F(14, False), fill=DIM)
    y += dp(186)
    bx0, bx1, h = x0 + dp(20), x1 - dp(20), dp(50)
    d.rounded_rectangle([bx0, y + dp(4), bx1, y + h + dp(4)], radius=dp(18), fill=INK)
    d.rounded_rectangle([bx0, y, bx1, y + h], radius=dp(18), fill=SUN, outline=INK, width=int(dp(3)))
    d.text(((bx0 + bx1) / 2, y + h / 2), 'Jugar Secuencia Lumínica', font=F(17), fill=INK, anchor='mm')
    return im


def frame(w, h, seed, draw, caption, sub):
    im = NM.nebula_bg(int(w), int(h), seed, stars=40)
    draw(im, w, h)
    d = ImageDraw.Draw(im)
    d.text((dp(12), h - dp(96)), caption, font=F(16), fill=WHITE)
    P.wrap(d, dp(12), h - dp(72), sub, F(13, False), DIM, w - dp(24))
    return im


def f_channels(im, w, h):
    for i, (b, lab) in enumerate([(1.0, 'hoy'), (0.55, '8 días'), (0.2, '20 días')]):
        x = w * (i + 0.5) / 3
        cluster(im, x, dp(70), 'velocidad', 0.8, 1, bright=b)
        d = ImageDraw.Draw(im); d.text((x, dp(108)), lab, font=F(13, False), fill=DIM, anchor='ma')
    d = ImageDraw.Draw(im)
    d.text((w / 2, dp(140)), 'mismo tamaño, distinto brillo', font=F(13), fill=SUN, anchor='ma')


def f_born(im, w, h):
    cx, cy = w * 0.3, dp(84); tx, ty = w * 0.78, dp(60)
    cluster(im, tx, ty, 'atencion', 0.85, 1)
    for t in range(14):
        tt = t / 13; px = cx + (tx - cx) * tt; py = cy + (ty - cy) * tt - math.sin(tt * math.pi) * dp(26)
        ov(im, lambda d, px=px, py=py, tt=tt: d.ellipse([px - dp(2.2) * tt - 1, py - dp(2.2) * tt - 1, px + dp(2.2) * tt + 1, py + dp(2.2) * tt + 1], fill=SUN + (int(50 + 190 * tt),)))
    NS.glow(im, tx, ty - dp(4), dp(16), SUN, 200)
    d = ImageDraw.Draw(im); d.polygon(N.sparkle(tx, ty - dp(4), dp(10)), fill=WHITE)
    nubi(im, cx, cy, dp(62))


def f_rotate(im, w, h):
    cx, cy = w / 2, dp(80)
    galaxy(im, cx, cy, w * 0.42, dp(40), rot=20, labels=False, seed=7)
    nubi(im, cx, cy - dp(2), dp(44))
    ov(im, lambda d: d.arc([cx - w * 0.36, cy + dp(6), cx + w * 0.36, cy + dp(66)], 20, 160, fill=WHITE + (200,), width=int(dp(2.5))))
    d = ImageDraw.Draw(im)
    x, y = cx - w * 0.36 * math.cos(math.radians(20)) + dp(4), cy + dp(36) + dp(30) * math.sin(math.radians(20))
    d.polygon([(x - dp(8), y - dp(2)), (x + dp(4), y - dp(8)), (x + dp(2), y + dp(6))], fill=WHITE)
    d.ellipse([cx + dp(20) - dp(12), cy + dp(58) - dp(12), cx + dp(20) + dp(12), cy + dp(58) + dp(12)], fill=(255, 255, 255, 60), outline=WHITE, width=int(dp(1.5)))


def f_quiet(im, w, h):
    cx, cy = w / 2, dp(70)
    cluster(im, cx, cy, 'lenguaje', 0.95, 1, bright=0.22)
    d = ImageDraw.Draw(im)
    d.text((cx, dp(112)), 'Lenguaje descansa', font=F(14), fill=WHITE, anchor='ma')
    d.text((cx, dp(132)), 'hace 12 días', font=F(13, False), fill=DIM, anchor='ma')


FRAMES = [(f_channels, 'Tamaño y brillo', 'Tamaño = lo logrado (nunca baja). Brillo = lo reciente: se apaga despacio sin jugar.'),
          (f_born, 'Nace una estrella', 'Al volver de un juego, sale de Nubi y llega a su cúmulo.'),
          (f_rotate, 'Se gira con el dedo', 'Gira sola y muy lenta; quieta si el teléfono quita animaciones.'),
          (f_quiet, 'No se apaga del todo', 'Un área sin jugar queda tenue, con un texto amable. Una partida la enciende.')]


def sheet():
    scr = screen()
    fw, fh = (W - dp(52)) / 2, dp(270)
    H = dp(84) + scr.size[1] + dp(60) + 2 * (fh + dp(12)) + dp(40)
    im = Image.new('RGBA', (W, int(H)), (10, 10, 30, 255)); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(26)), 'Tu galaxia · maqueta', font=F(24), fill=SUN)
    d.text((dp(20), dp(58)), 'Hoy con la galaxia y la ventana de Memoria abierta.', font=F(13, False), fill=DIM)
    y = dp(84)
    AL.paste_round(im, scr, 0, y, dp(24))
    y += scr.size[1] + dp(24)
    d = ImageDraw.Draw(im); d.text((dp(20), y), 'Cómo se comporta', font=F(18), fill=SUN); y += dp(36)
    for i, (fn, cap, sub) in enumerate(FRAMES):
        fr = frame(fw, fh, 70 + i, fn, cap, sub)
        AL.paste_round(im, fr, dp(20) + (i % 2) * (fw + dp(12)), y + (i // 2) * (fh + dp(12)))
    out = os.path.join(P.OUT, 'galaxia.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
