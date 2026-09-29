# Pestaña Juegos rehecha (29-sep). Ricardo, tras probar el deslizar entre pestañas: en Juegos también se desliza hacia
# los costados para cambiar de área, y los dos gestos se confunden. Su idea: todas las áreas en UNA pantalla; al tocar
# una se abre su ventana, que queda fija (no se desliza hacia los costados), con una X arriba a la derecha para cerrar
# y Nubi arriba, de estudiante o científica, invitando a elegir. Maqueta PIL, NO la app.
#   A · Juegos: las 6 áreas a la vez (planeta con ícono + nombre + la misma barra de avance lavanda de Hoy + etapa).
#       Sin flechas ni deslizar: el único gesto lateral de la pantalla es cambiar de pestaña.
#   B · Ventana del área: tapa todo (también la barra de pestañas), X arriba a la derecha, Nubi con su globo y los
#       juegos del área (las casillas de hoy). Mientras está abierta, el dedo NO cambia de pestaña.
#   C · Tocar un juego abre su ficha, como hoy (avance, marca, cómo quieres jugar, Jugar).
# Uso: python3 tools/previews/juegos_nubi.py  ->  docs/previews/juegos-nubi.png
import os, math, importlib.util
from PIL import Image, ImageDraw
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('ng', os.path.join(HERE, 'nubi_guia.py'))
NG = importlib.util.module_from_spec(spec); spec.loader.exec_module(NG)
spec = importlib.util.spec_from_file_location('nh', os.path.join(HERE, 'nubi_halo.py'))
NH = importlib.util.module_from_spec(spec); spec.loader.exec_module(NH)
NS, NM, P = NG.NS, NG.NM, NG.P
F, dp, W = P.F, P.dp, P.W
INK, SUN, WHITE, DIM, CORAL, LIME = P.INK, P.SUN, P.WHITE, P.DIM, P.CORAL, P.LIME
LINE, ov = NS.LINE, NH.ov
BAR = (196, 186, 255)
PH = dp(800)                   # alto de cada teléfono
REAL = os.path.join(P.OUT, 'juegos-lista-real.png')
# (clave, nombre, avance 0-100, juegos, jugada hoy)
AREAS = [('memoria', 'Memoria', 52, 6, True), ('atencion', 'Atención', 47, 5, False),
         ('razonamiento', 'Razonamiento', 36, 4, False), ('lenguaje', 'Lenguaje', 12, 2, False),
         ('calculo', 'Cálculo', 30, 2, False), ('velocidad', 'Velocidad', 64, 2, True)]
STAGES = ['Inicio', 'Aprendiz', 'Hábil', 'Experto', 'Maestro']


def stage(v): return STAGES[min(4, int(v // 20))]


def phone(seed):
    im = NM.nebula_bg(W, int(PH), seed, stars=70)
    return im


def rounded(im):
    m = Image.new('L', im.size, 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, im.size[0] - 1, im.size[1] - 1], radius=dp(26), fill=255)
    out = Image.new('RGBA', im.size, (0, 0, 0, 0)); out.paste(im, (0, 0), m); return out


def navbar(im, sel=1):
    tmp = Image.new('RGBA', (W, int(P.H)), (0, 0, 0, 0))
    P.navbar(tmp, sel)
    band = tmp.crop((0, int(P.H - dp(90)), W, int(P.H)))
    im.alpha_composite(band, (0, int(PH - dp(90))))


def bar(im, x, y, w, v, h=None):
    h = h or dp(7)
    ov(im, lambda d: d.rounded_rectangle([x, y, x + w, y + h], radius=h / 2, fill=(255, 255, 255, 38)))
    ImageDraw.Draw(im).rounded_rectangle([x, y, max(x + h, x + w * v / 100), y + h], radius=h / 2, fill=BAR)
    for k in range(1, 5):
        xx = x + w * k / 5
        ov(im, lambda dd, xx=xx: dd.line([(xx, y - dp(1.5)), (xx, y + h + dp(1.5))], fill=(10, 10, 40, 170), width=int(dp(1.4))))


def planet(im, x, y, r, key, today=False):
    """El orbe de nubi_halo sin su anillo de etapas (aquí el avance lo dice la barra, como en Hoy)."""
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0))
    NH.orb(lay, x, y, r, key, 0, 0, today=today)
    # borra el anillo: todo lo que quede fuera del disco + el sello
    m = Image.new('L', im.size, 0); md = ImageDraw.Draw(m)
    md.ellipse([x - r - dp(1), y - r - dp(1), x + r + dp(1), y + r + dp(1)], fill=255)
    if today:
        sx, sy = x + r * 0.78, y - r * 0.78; md.ellipse([sx - dp(12), sy - dp(12), sx + dp(12), sy + dp(14)], fill=255)
    NS.glow(im, x, y, r * 1.6, NH.LIGHT[key], 100)
    im.alpha_composite(Image.composite(lay, Image.new('RGBA', im.size, (0, 0, 0, 0)), m))


def hand_tap(im, x, y):
    ov(im, lambda d: d.ellipse([x - dp(18), y - dp(18), x + dp(18), y + dp(18)], fill=(255, 255, 255, 60), outline=WHITE, width=int(dp(2))))


# ---------- Nubi estudiante (birrete y libro) ----------
def student(im, cx, cy, k):
    NS.nubi(im, cx, cy + 3 * k, k, 'hola')
    d = ImageDraw.Draw(im)
    cap = (58, 50, 120); top = (78, 68, 156)
    # banda del birrete sobre la cabeza
    d.rounded_rectangle([cx - 14 * k, cy - 27 * k, cx + 14 * k, cy - 19 * k], radius=2 * k, fill=cap, outline=LINE, width=max(2, int(1.2 * k)))
    # tabla (rombo en perspectiva) con su sombra
    board = [(cx - 29 * k, cy - 29 * k), (cx, cy - 38 * k), (cx + 29 * k, cy - 29 * k), (cx, cy - 21 * k)]
    d.polygon([(x, y + 2 * k) for x, y in board], fill=LINE)
    d.polygon(board, fill=top, outline=LINE, width=max(2, int(1.2 * k)))
    d.line([(cx - 20 * k, cy - 31.5 * k), (cx - 4 * k, cy - 35.5 * k)], fill=(120, 110, 200), width=max(1, int(1 * k)))
    # borla sol
    d.ellipse([cx - 1.8 * k, cy - 31.8 * k, cx + 1.8 * k, cy - 28.2 * k], fill=SUN, outline=LINE, width=max(1, int(0.8 * k)))
    d.line([(cx, cy - 30 * k), (cx + 22 * k, cy - 28 * k), (cx + 23 * k, cy - 16 * k)], fill=SUN, width=max(2, int(1.3 * k)), joint='curve')
    d.polygon([(cx + 21 * k, cy - 17 * k), (cx + 25 * k, cy - 17 * k), (cx + 26 * k, cy - 10 * k), (cx + 20 * k, cy - 10 * k)], fill=SUN, outline=LINE)
    # libro abierto en la mano
    bx, by = cx - 30 * k, cy + 12 * k
    d.polygon([(bx - 11 * k, by - 5 * k), (bx, by - 2 * k), (bx + 11 * k, by - 5 * k), (bx + 11 * k, by + 8 * k), (bx, by + 11 * k), (bx - 11 * k, by + 8 * k)],
              fill=(252, 250, 255), outline=LINE, width=max(2, int(1.2 * k)))
    d.line([(bx, by - 2 * k), (bx, by + 11 * k)], fill=LINE, width=max(1, int(1 * k)))
    for i in range(3):
        d.line([(bx - 8 * k, by + (0 + i * 3) * k), (bx - 3 * k, by + (1.2 + i * 3) * k)], fill=(170, 166, 210), width=max(1, int(0.9 * k)))
        d.line([(bx + 3 * k, by + (1.2 + i * 3) * k), (bx + 8 * k, by + (0 + i * 3) * k)], fill=(170, 166, 210), width=max(1, int(0.9 * k)))
    NG.hand(im, bx + 9 * k, by + 5 * k, k, 4.4)


def scientist(im, cx, cy, k):
    NG.pose_cientifica(im, cx - 8 * k, cy, k * 0.95)


def nubi_sprite(fn, size):
    return NG.sprite(fn, size, 1.0)


# ---------- A · las 6 áreas ----------
def screen_a():
    im = phone(41); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(36)), '¿Qué quieres trabajar hoy?', font=F(24), fill=WHITE, anchor='lm')
    d.text((dp(20), dp(66)), 'Toca un área para ver sus juegos', font=F(15, False), fill=DIM, anchor='lm')
    cw, top, rh = (W - dp(40)) / 2, dp(96), dp(196)
    for i, (key, name, v, n, today) in enumerate(AREAS):
        col, row = i % 2, i // 2
        x0 = dp(20) + col * cw; cx = x0 + cw / 2; y = top + row * rh
        planet(im, cx, y + dp(58), dp(42), key, today)
        d = ImageDraw.Draw(im)
        d.text((cx, y + dp(122)), name, font=F(18), fill=WHITE, anchor='mm')
        bar(im, cx - dp(62), y + dp(140), dp(124), v)
        ImageDraw.Draw(im).text((cx, y + dp(164)), f'{stage(v)} · {n} juegos', font=F(14, False), fill=DIM, anchor='mm')
    hand_tap(im, dp(20) + cw / 2 + dp(26), top + dp(84))
    navbar(im, 1)
    return rounded(im)


# ---------- B · ventana del área ----------
def tiles():
    src = Image.open(REAL).convert('RGBA'); s = 1.098
    out = []
    for i in range(6):
        y0 = int((375 + 254 * i) * s)
        out.append(src.crop((int(40 * s), y0, int(935 * s), y0 + int(222 * s))))
    return out


def screen_b(pose):
    im = phone(41)
    # la pestaña de atrás queda tapada por un velo (no se ve ni se toca)
    ov(im, lambda d: d.rectangle([0, 0, W, PH], fill=(6, 8, 30, 150)))
    # la ventana: ocupa toda la pantalla, fondo de noche liso, borde fino arriba
    x0, y0 = 0, dp(10)
    win = Image.new('RGBA', (W, int(PH - y0)), (10, 16, 64, 255))
    im.alpha_composite(win, (x0, int(y0)))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([dp(-4), y0, W + dp(4), PH + dp(30)], radius=dp(28), outline=(70, 66, 150), width=int(dp(2)))
    # X arriba a la derecha (arcilla, 48 dp)
    xc, yc, r = W - dp(38), y0 + dp(34), dp(22)
    d.ellipse([xc - r, yc - r + dp(3), xc + r, yc + r + dp(3)], fill=INK)
    d.ellipse([xc - r, yc - r, xc + r, yc + r], fill=(252, 250, 255), outline=INK, width=int(dp(2.5)))
    q = dp(7.5); d.line([(xc - q, yc - q), (xc + q, yc + q)], fill=INK, width=int(dp(3))); d.line([(xc - q, yc + q), (xc + q, yc - q)], fill=INK, width=int(dp(3)))
    # cabecera: área
    planet(im, dp(44), y0 + dp(34), dp(20), 'memoria')
    d = ImageDraw.Draw(im)
    d.text((dp(74), y0 + dp(24)), 'Memoria', font=F(24), fill=WHITE, anchor='lm')
    d.text((dp(74), y0 + dp(50)), 'Hábil · 6 juegos', font=F(14, False), fill=DIM, anchor='lm')
    # Nubi con su globo
    ny = y0 + dp(78)
    NS.glow(im, dp(90), ny + dp(80), dp(80), (150, 140, 255), 90)
    im.alpha_composite(nubi_sprite(pose, dp(176)), (int(dp(2)), int(ny - dp(8))))
    d = ImageDraw.Draw(im)
    bx0, by0, bw, bh = dp(184), ny + dp(36), W - dp(200), dp(86)
    d.rounded_rectangle([bx0, by0 + dp(3), bx0 + bw, by0 + bh + dp(3)], radius=dp(18), fill=LINE)
    d.rounded_rectangle([bx0, by0, bx0 + bw, by0 + bh], radius=dp(18), fill=(252, 250, 255), outline=LINE, width=int(dp(2)))
    d.polygon([(bx0 + dp(2), by0 + dp(34)), (bx0 - dp(12), by0 + dp(46)), (bx0 + dp(2), by0 + dp(54))], fill=(252, 250, 255))
    d.line([(bx0, by0 + dp(34)), (bx0 - dp(12), by0 + dp(46)), (bx0, by0 + dp(54))], fill=LINE, width=int(dp(2)))
    d.text((bx0 + dp(14), by0 + dp(12)), 'Nubi', font=F(13, False), fill=(96, 88, 140))
    P.wrap(d, bx0 + dp(14), by0 + dp(32), '¿Con cuál entrenamos tu memoria?', F(17), INK, bw - dp(28))
    # juegos del área (las casillas reales de la app)
    ty = ny + dp(178); tw = W - dp(32)
    for i, t in enumerate(tiles()):
        th = tw * t.size[1] / t.size[0]
        if ty + i * (th + dp(8)) + th > PH: break
        im.alpha_composite(t.resize((int(tw), int(th)), Image.LANCZOS), (int(dp(16)), int(ty + i * (th + dp(8)))))
    # pie: se desplaza hacia abajo, con borde que se desvanece
    fade = Image.new('RGBA', (W, int(dp(60))), (0, 0, 0, 0)); fd = ImageDraw.Draw(fade)
    for i in range(int(dp(60))):
        fd.line([(0, i), (W, i)], fill=(10, 16, 64, int(255 * i / dp(60))))
    im.alpha_composite(fade, (0, int(PH - dp(60))))
    return rounded(im)


def arrow(im, x0, y, x1):
    d = ImageDraw.Draw(im)
    d.line([(x0, y), (x1 - dp(10), y)], fill=SUN, width=int(dp(4)))
    d.polygon([(x1, y), (x1 - dp(16), y - dp(11)), (x1 - dp(16), y + dp(11))], fill=SUN)


def sheet():
    gap = dp(56); SW = int(dp(40) * 2 + W * 3 + gap * 2); SH = int(dp(150) + PH + dp(330))
    im = NM.nebula_bg(SW, SH, 7, stars=160); d = ImageDraw.Draw(im)
    d.text((dp(40), dp(44)), 'Juegos: todas las áreas en una pantalla', font=F(34), fill=SUN)
    d.text((dp(40), dp(92)), 'Así el único gesto hacia los costados es cambiar de pestaña. Tocas un área, se abre su ventana fija con Nubi; la X o Atrás la cierran.',
           font=F(17, False), fill=DIM)
    y = dp(140)
    xs = [dp(40) + i * (W + gap) for i in range(3)]
    im.alpha_composite(screen_a(), (int(xs[0]), int(y)))
    im.alpha_composite(screen_b(scientist), (int(xs[1]), int(y)))
    im.alpha_composite(screen_b(student), (int(xs[2]), int(y)))
    arrow(im, xs[0] + W + dp(8), y + PH / 2, xs[1] - dp(8))
    d = ImageDraw.Draw(im)
    caps = [('A · Juegos', 'Las 6 áreas a la vez: planeta con su ícono, nombre, la misma barra de avance de Hoy y la etapa. '
                           'El sello sol = jugada hoy. Sin flechas ni deslizar.'),
            ('B · Ventana del área, Nubi científica', 'Tapa toda la pantalla (también la barra de abajo) y no se mueve hacia los '
                           'costados. X arriba a la derecha. Nubi pregunta; abajo, los juegos del área.'),
            ('B · La misma ventana, Nubi estudiante', 'Con birrete y su libro. Elige cuál te gusta más: la científica ya '
                           'aparece al explicar las medidas del final; la estudiante sería nueva.')]
    for x, (t, s) in zip(xs, caps):
        d.text((x, y + PH + dp(24)), t, font=F(20), fill=WHITE)
        P.wrap(d, x, y + PH + dp(56), s, F(15, False), DIM, W)
    yy = y + PH + dp(170)
    d.text((dp(40), yy), 'Cómo se usa', font=F(20), fill=SUN)
    steps = ['1. Tocas un área y se abre su ventana (la pestaña de atrás queda quieta).',
             '2. Tocas un juego y se abre su ficha, como hoy: avance, marca, "¿Cómo quieres jugar?" y Jugar.',
             '3. Al terminar la partida vuelves a esa misma ventana. La X o Atrás te devuelven a las 6 áreas.',
             '4. El detalle de un área en Hoy funciona igual: mientras está abierto, el dedo no cambia de pestaña.']
    for i, s in enumerate(steps):
        d.text((dp(40), yy + dp(36) + i * dp(28)), s, font=F(16, False), fill=WHITE)
    out = os.path.join(P.OUT, 'juegos-nubi.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
