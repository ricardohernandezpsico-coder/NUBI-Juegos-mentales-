# Navegación de Nubi (29-sep). Ricardo, tras ver la maqueta de Juegos (eligió a Nubi científica):
#   · El ícono de Atención (un ojo) no le gusta; los demás sí. -> 4 opciones.
#   · Quitar el botón "play" del centro de la barra: el desafío del día ya está en Hoy (sería repetir).
#   · Barra de abajo con 3: Hoy (casa), Juegos (un "player") y un cerebro para el avance.
#   · Arriba, un engranaje para las opciones y un ícono para el perfil.
# Maqueta PIL, NO la app.  Uso: python3 tools/previews/navegacion_nubi.py  ->  docs/previews/navegacion-nubi.png
import os, math, importlib.util
from PIL import Image, ImageDraw
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('jn', os.path.join(HERE, 'juegos_nubi.py'))
JN = importlib.util.module_from_spec(spec); spec.loader.exec_module(JN)
NH, NS, NM, P, ov = JN.NH, JN.NS, JN.NM, JN.P, JN.ov
F, dp, W, PH = P.F, P.dp, P.W, JN.PH
INK, SUN, WHITE, DIM, CORAL, SKY, CREAM = P.INK, P.SUN, P.WHITE, P.DIM, P.CORAL, P.SKY, P.CREAM
LINE = NS.LINE
MUTED = (107, 103, 144)
SEL_BG = (255, 225, 216)


# ---------- íconos de Atención ----------
def att_diana(d, x, y, s):
    wl = max(2, int(s * 0.1))
    for rr, f in ((0.6, WHITE), (0.4, LINE), (0.27, WHITE), (0.11, LINE)):
        d.ellipse([x - s * rr, y - s * rr, x + s * rr, y + s * rr], fill=f, outline=LINE, width=wl)


def att_faro(d, x, y, s):
    wl = max(2, int(s * 0.09))
    # haces de luz a los lados
    for sx in (-1, 1):
        d.polygon([(x + sx * s * 0.12, y - s * 0.38), (x + sx * s * 0.66, y - s * 0.62), (x + sx * s * 0.66, y - s * 0.2)], fill=(255, 244, 200))
    tower = [(x - s * 0.16, y - s * 0.3), (x + s * 0.16, y - s * 0.3), (x + s * 0.27, y + s * 0.6), (x - s * 0.27, y + s * 0.6)]
    d.polygon(tower, fill=WHITE, outline=LINE, width=wl)
    d.polygon([(x - s * 0.2, y), (x + s * 0.2, y), (x + s * 0.23, y + s * 0.2), (x - s * 0.23, y + s * 0.2)], fill=CORAL)
    d.rounded_rectangle([x - s * 0.17, y - s * 0.52, x + s * 0.17, y - s * 0.3], radius=s * 0.04, fill=SUN, outline=LINE, width=wl)
    d.polygon([(x - s * 0.22, y - s * 0.52), (x + s * 0.22, y - s * 0.52), (x, y - s * 0.7)], fill=WHITE, outline=LINE, width=wl)


def att_foco(d, x, y, s):
    """Un foco de luz que ilumina una estrella: poner la luz en lo importante."""
    wl = max(2, int(s * 0.09))
    d.polygon([(x - s * 0.36, y - s * 0.46), (x - s * 0.1, y - s * 0.6), (x + s * 0.5, y + s * 0.34), (x - s * 0.02, y + s * 0.56)], fill=(255, 244, 200))
    d.polygon(NS.N.star(x + s * 0.2, y + s * 0.3, s * 0.3, inner=0.5), fill=SUN, outline=LINE, width=wl)
    # la lámpara
    cx, cy = x - s * 0.3, y - s * 0.58
    d.polygon([(cx - s * 0.18, cy - s * 0.08), (cx + s * 0.08, cy - s * 0.24), (cx + s * 0.24, cy + s * 0.04), (cx - s * 0.02, cy + s * 0.2)], fill=WHITE, outline=LINE, width=wl)


def att_enfoque(d, x, y, s):
    """Esquinas de enfoque (como una cámara) alrededor de una estrella."""
    wl = max(3, int(s * 0.13)); a, b = s * 0.58, s * 0.24
    for sx in (-1, 1):
        for sy in (-1, 1):
            pts = [(x + sx * a, y + sy * (a - b)), (x + sx * a, y + sy * a), (x + sx * (a - b), y + sy * a)]
            d.line(pts, fill=LINE, width=wl + int(s * 0.08), joint='curve')
            d.line(pts, fill=WHITE, width=wl, joint='curve')
    d.polygon(NS.N.star(x, y, s * 0.3, inner=0.5), fill=WHITE, outline=LINE, width=max(2, int(s * 0.08)))


ATT = [('A', 'Diana', att_diana, 'apuntar a una cosa'), ('B', 'Faro', att_faro, 'la luz que busca y alerta'),
       ('C', 'Foco de luz', att_foco, 'poner la luz en lo importante'), ('D', 'Enfoque', att_enfoque, 'como una cámara que enfoca')]
_orig_glyph = NH.glyph


def use_att(fn):
    def g(d, key, x, y, s):
        if key == 'atencion': fn(d, x, y, s)
        else: _orig_glyph(d, key, x, y, s)
    NH.glyph = g


# ---------- barra de abajo (3) y cabecera ----------
def icon_home(d, cx, cy, s, col):
    d.polygon([(cx, cy - s), (cx + s, cy), (cx + s * .7, cy), (cx + s * .7, cy + s), (cx - s * .7, cy + s), (cx - s * .7, cy), (cx - s, cy)], fill=col)
    d.rounded_rectangle([cx - s * .22, cy + s * .35, cx + s * .22, cy + s], radius=s * .1, fill=WHITE)


def icon_play(d, cx, cy, s, col):
    d.ellipse([cx - s, cy - s, cx + s, cy + s], outline=col, width=int(s * 0.22))
    d.polygon([(cx - s * 0.3, cy - s * 0.45), (cx + s * 0.5, cy), (cx - s * 0.3, cy + s * 0.45)], fill=col)


def icon_brain(d, cx, cy, s, col, bg=WHITE):
    """Cerebro de perfil (la silueta que todos reconocen): lóbulos en nubecitas, cerebelo, tronco y pliegues."""
    for (x, y, r) in [(-0.58, 0.02, 0.4), (-0.3, -0.34, 0.44), (0.12, -0.42, 0.44), (0.52, -0.16, 0.42),
                      (0.5, 0.2, 0.34), (-0.02, 0.18, 0.46), (-0.44, 0.3, 0.3)]:
        d.ellipse([cx + (x - r) * s, cy + (y - r) * s, cx + (x + r) * s, cy + (y + r) * s], fill=col)
    d.ellipse([cx + 0.26 * s, cy + 0.36 * s, cx + 0.72 * s, cy + 0.72 * s], fill=col)       # cerebelo
    d.rounded_rectangle([cx + 0.02 * s, cy + 0.4 * s, cx + 0.26 * s, cy + 0.92 * s], radius=0.1 * s, fill=col)  # tronco
    w2 = max(2, int(s * 0.12))
    def wave(p0, p1, amp, n=2):   # surco ondulado (no arcos sueltos: parecían ojos y boca)
        pts = []
        for i in range(17):
            t = i / 16; x = p0[0] + (p1[0] - p0[0]) * t; y = p0[1] + (p1[1] - p0[1]) * t
            nx, ny = -(p1[1] - p0[1]), (p1[0] - p0[0]); L = math.hypot(nx, ny) or 1
            o = math.sin(t * math.pi * n) * amp
            pts.append((cx + (x + nx / L * o) * s, cy + (y + ny / L * o) * s))
        d.line(pts, fill=bg, width=w2, joint='curve')
    wave((-0.5, 0.2), (0.34, 0.02), 0.07, 2)        # cisura lateral
    wave((0.1, -0.74), (-0.06, 0.02), 0.08, 3)       # surco central
    wave((-0.74, -0.14), (-0.36, -0.2), 0.05, 1)
    wave((0.38, -0.6), (0.62, -0.24), 0.05, 1)
    wave((-0.36, -0.66), (-0.2, -0.36), 0.04, 1)
    d.line([(cx + 0.3 * s, cy + 0.52 * s), (cx + 0.66 * s, cy + 0.5 * s)], fill=bg, width=max(1, int(s * 0.08)))


TABS = [('Hoy', icon_home), ('Juegos', icon_play), ('Avance', icon_brain)]


def navbar3(im, sel):
    d = ImageDraw.Draw(im); y0 = PH - dp(86); x0, x1 = dp(14), W - dp(14)
    d.rounded_rectangle([x0, y0 + dp(4), x1, y0 + dp(72)], radius=dp(30), fill=INK)
    d.rounded_rectangle([x0, y0, x1, y0 + dp(68)], radius=dp(30), fill=WHITE, outline=INK, width=int(dp(3)))
    for i, (lb, fn) in enumerate(TABS):
        cx = x0 + (x1 - x0) * (i + 0.5) / 3; on = i == sel
        if on: d.rounded_rectangle([cx - dp(30), y0 + dp(9), cx + dp(30), y0 + dp(41)], radius=dp(16), fill=SEL_BG)
        fn(d, cx, y0 + dp(25), dp(12.5), CORAL if on else MUTED)
        d.text((cx, y0 + dp(54)), lb, font=F(13, on), fill=INK if on else MUTED, anchor='mm')


def icon_gear(d, cx, cy, s, col):
    for k in range(8):
        a = math.radians(k * 45)
        d.line([(cx + math.cos(a) * s * 0.55, cy + math.sin(a) * s * 0.55), (cx + math.cos(a) * s * 0.98, cy + math.sin(a) * s * 0.98)], fill=col, width=int(s * 0.36))
    d.ellipse([cx - s * 0.72, cy - s * 0.72, cx + s * 0.72, cy + s * 0.72], fill=col)
    d.ellipse([cx - s * 0.3, cy - s * 0.3, cx + s * 0.3, cy + s * 0.3], fill=WHITE)


def top_buttons(im, y):
    """Arriba a la derecha: perfil (inicial en arcilla celeste) y engranaje (opciones). Botones de 44 dp."""
    d = ImageDraw.Draw(im); r = dp(22)
    for cx, kind in ((W - dp(86), 'perfil'), (W - dp(36), 'gear')):
        d.ellipse([cx - r, y - r + dp(3), cx + r, y + r + dp(3)], fill=INK)
        d.ellipse([cx - r, y - r, cx + r, y + r], fill=SKY if kind == 'perfil' else WHITE, outline=INK, width=int(dp(2.5)))
        if kind == 'perfil': d.text((cx, y + dp(1)), 'R', font=F(20), fill=INK, anchor='mm')
        else: icon_gear(d, cx, y, dp(12), INK)


def header_left(im, y, title=None):
    d = ImageDraw.Draw(im)
    if title:
        d.text((dp(20), y), title, font=F(26), fill=WHITE, anchor='lm'); return
    # Hoy: liga y racha, como ahora
    d.polygon([(dp(20), y - dp(11)), (dp(40), y - dp(11)), (dp(40), y + dp(2)), (dp(30), y + dp(12)), (dp(20), y + dp(2))], fill=(200, 208, 230), outline=INK)
    d.text((dp(50), y), 'Plata', font=F(16), fill=WHITE, anchor='lm')
    d.text((dp(104), y), '·', font=F(16), fill=DIM, anchor='mm')
    NS.glow(im, dp(124), y, dp(12), SUN, 90); d = ImageDraw.Draw(im)
    d.polygon([(dp(124), y - dp(10)), (dp(131), y + dp(3)), (dp(124), y + dp(9)), (dp(117), y + dp(3))], fill=SUN)
    d.text((dp(138), y), '12 días', font=F(16), fill=SUN, anchor='lm')


# ---------- teléfonos ----------
def phone_hoy():
    im = JN.phone(41)
    header_left(im, dp(36)); top_buttons(im, dp(36))
    real = Image.open(os.path.join(P.OUT, 'hoy-nubi-real.png')).convert('RGBA')
    k = (W - dp(8)) / real.size[0]
    real = real.resize((int(real.size[0] * k), int(real.size[1] * k)), Image.LANCZOS)
    # el fondo de la captura es liso (10, 16, 64): se vuelve transparente para que se vea el cielo
    import numpy as np
    a = np.asarray(real, np.float32); dist = np.abs(a[..., :3] - np.array([10, 16, 64.])).sum(-1)
    a[..., 3] = np.clip(dist * 6, 0, 255); real = Image.fromarray(a.astype(np.uint8))
    im.alpha_composite(real, (int(dp(4)), int(dp(70))))
    d = ImageDraw.Draw(im); y = PH - dp(176)
    d.text((W / 2, y), 'Tu desafío de hoy · 3 juegos', font=F(15, False), fill=DIM, anchor='mm')
    x0, x1, h, by = dp(20), W - dp(20), dp(54), y + dp(16)
    d.rounded_rectangle([x0, by + dp(5), x1, by + h + dp(5)], radius=dp(20), fill=INK)
    d.rounded_rectangle([x0, by, x1, by + h], radius=dp(20), fill=SUN, outline=INK, width=int(dp(3)))
    P.play_tri(d, x0 + dp(34), by + h / 2, dp(18))
    d.text((W / 2 + dp(10), by + h / 2), 'Empezar: Rescate relámpago', font=F(17), fill=INK, anchor='mm')
    navbar3(im, 0)
    return JN.rounded(im)


def phone_juegos(att):
    use_att(att)
    im = JN.phone(41)
    JN.navbar = lambda im_, sel=1: navbar3(im_, 1)
    JN.hand_tap = lambda *a: None
    out = JN.screen_a('Juegos', '¿Qué quieres trabajar hoy?')
    d = ImageDraw.Draw(out)
    top_buttons(out, dp(36))
    return out


def phone_avance():
    im = JN.phone(43); d = ImageDraw.Draw(im)
    header_left(im, dp(36), 'Tu avance'); top_buttons(im, dp(36))
    d = ImageDraw.Draw(im); y = dp(84)
    # liga
    sx, sy = dp(56), y + dp(40)
    d.polygon([(sx - dp(30), sy - dp(32)), (sx + dp(30), sy - dp(32)), (sx + dp(30), sy + dp(6)), (sx, sy + dp(36)), (sx - dp(30), sy + dp(6))], fill=(200, 208, 230), outline=INK, width=int(dp(3)))
    d.polygon(NS.N.star(sx, sy - dp(4), dp(13), inner=0.5), fill=WHITE, outline=INK)
    d.text((dp(104), y + dp(20)), 'Liga Plata', font=F(22), fill=WHITE, anchor='lm')
    d.text((dp(104), y + dp(48)), '1.240 trofeos · te faltan 260 para Oro', font=F(14, False), fill=DIM, anchor='lm')
    d.text((dp(104), y + dp(72)), 'Compartir mi liga', font=F(14), fill=SUN, anchor='lm')
    y += dp(122)
    d.line([(dp(20), y), (W - dp(20), y)], fill=(255, 255, 255, 40), width=1)
    d.text((dp(20), y + dp(22)), 'TUS ÁREAS', font=F(14), fill=SUN, anchor='lm')
    y += dp(44)
    for key, name, v, n, _ in JN.AREAS:
        d.text((dp(20), y), name, font=F(16), fill=WHITE, anchor='lm')
        d.text((W - dp(20), y), JN.stage(v), font=F(14, False), fill=DIM, anchor='rm')
        JN.bar(im, dp(20), y + dp(14), W - dp(40), v); d = ImageDraw.Draw(im)
        y += dp(48)
    d.line([(dp(20), y), (W - dp(20), y)], fill=(255, 255, 255, 40), width=1)
    d.text((dp(20), y + dp(22)), 'TAMBIÉN AQUÍ', font=F(14), fill=SUN, anchor='lm')
    for i, s in enumerate(['Tus juegos, uno por uno (etapa y marca)', 'Tu punto de partida', 'Historial de partidas']):
        d.text((dp(20), y + dp(52) + i * dp(30)), s, font=F(15, False), fill=WHITE, anchor='lm')
        d.text((W - dp(20), y + dp(52) + i * dp(30)), '›', font=F(20), fill=DIM, anchor='rm')
    navbar3(im, 2)
    return JN.rounded(im)


def sheet():
    gap = dp(56); SW = int(dp(40) * 2 + W * 3 + gap * 2)
    SH = int(dp(590) + PH + dp(300))
    im = NM.nebula_bg(SW, SH, 8, stars=160); d = ImageDraw.Draw(im)
    d.text((dp(40), dp(44)), 'Navegación: 3 pestañas y, arriba, perfil y opciones', font=F(34), fill=SUN)
    d.text((dp(40), dp(92)), 'Se quita el botón "play" del centro: el desafío del día ya está en Hoy. Y un ícono nuevo para Atención.',
           font=F(17, False), fill=DIM)
    # 1 · Atención
    y = dp(150)
    d.text((dp(40), y), '1 · Ícono de Atención (el ojo se va): elige uno', font=F(22), fill=WHITE)
    cw = (SW - dp(80)) / 4
    for i, (letter, name, fn, why) in enumerate(ATT):
        use_att(fn)
        cx = dp(40) + cw * (i + 0.5); cy = y + dp(130)
        JN.planet(im, cx, cy, dp(62), 'atencion')
        d = ImageDraw.Draw(im)
        d.text((cx, cy + dp(94)), f'{letter} · {name}', font=F(20), fill=WHITE, anchor='mm')
        d.text((cx, cy + dp(122)), why, font=F(15, False), fill=DIM, anchor='mm')
    # 2 · teléfonos
    y = dp(470)
    d.text((dp(40), y), '2 · La barra de abajo: Hoy (casa), Juegos (player) y Avance (cerebro)', font=F(22), fill=WHITE)
    for i, (lb, fn) in enumerate(TABS):
        bx = SW - dp(40) - (2 - i) * dp(120) - dp(40); by = y + dp(12)
        fn(d, bx, by, dp(22), WHITE, bg=(40, 30, 100)) if fn is icon_brain else fn(d, bx, by, dp(22), WHITE)
        d.text((bx, by + dp(40)), lb, font=F(15), fill=WHITE, anchor='mm')
    y += dp(80)
    xs = [dp(40) + i * (W + gap) for i in range(3)]
    im.alpha_composite(phone_hoy(), (int(xs[0]), int(y)))
    im.alpha_composite(phone_juegos(att_diana), (int(xs[1]), int(y)))
    im.alpha_composite(phone_avance(), (int(xs[2]), int(y)))
    d = ImageDraw.Draw(im)
    caps = [('Hoy', 'Nubi con tus 6 áreas y el botón del desafío de hoy: es el único lugar para empezarlo.'),
            ('Juegos', 'Las 6 áreas; al tocar una se abre su ventana con Nubi científica (maqueta anterior).'),
            ('Avance (el cerebro)', 'Reúne lo de la pestaña Liga: tu liga, tus 6 áreas, cada juego, tu punto de partida y el historial.')]
    for x, (t, s) in zip(xs, caps):
        d.text((x, y + PH + dp(22)), t, font=F(20), fill=WHITE)
        P.wrap(d, x, y + PH + dp(52), s, F(15, False), DIM, W)
    yy = y + PH + dp(140)
    d.text((dp(40), yy), '3 · Arriba a la derecha, en las 3 pestañas', font=F(22), fill=WHITE)
    top = Image.new('RGBA', (int(dp(120)), int(dp(56))), (0, 0, 0, 0))
    lines = ['Tu inicial (celeste) = tu perfil: nombre, racha, partidas y logros.',
             'Engranaje = opciones (Ajustes): sonido, vibración, recordatorio, idioma, borrar datos.',
             'Los dos son botones de 44 dp con nombre para el lector de pantalla.']
    for i, s in enumerate(lines):
        d.text((dp(40), yy + dp(40) + i * dp(30)), s, font=F(16, False), fill=WHITE)
    out = os.path.join(P.OUT, 'navegacion-nubi.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
