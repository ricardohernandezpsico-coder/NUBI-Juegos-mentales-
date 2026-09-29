# Nubi guía (29-sep). Ricardo: "me gusta la imagen de Nubi con un traje de científico o en otras posiciones dando a
# entender que te guía y te explica". Maqueta PIL, NO la app: Nubi con bata y lentes, explicando en una pizarra, con lupa,
# con una idea (consejo) y con su tablet; y dónde aparece cada uno (tutorial de la primera vez, la medida del final de
# los juegos estrella, el descubrimiento del día, la pantalla de carga). Regla: Nubi explica y acompaña; nunca evalúa
# ni pone cara triste por cómo le fue a la persona.
# Uso: python3 tools/previews/nubi_guia.py  ->  docs/previews/nubi-guia.png
import os, math, importlib.util
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('ns', os.path.join(HERE, 'nubi_suave.py'))
NS = importlib.util.module_from_spec(spec); spec.loader.exec_module(NS)
spec = importlib.util.spec_from_file_location('nm', os.path.join(HERE, 'nubi_mundo.py'))
NM = importlib.util.module_from_spec(spec); spec.loader.exec_module(NM)
N, P = NS.N, NS.P
F, dp, W, S = P.F, P.dp, P.W, NS.S
INK, SUN, CREAM, WHITE, DIM, SKY, LIME, CORAL = P.INK, P.SUN, P.CREAM, P.WHITE, P.DIM, P.SKY, P.LIME, P.CORAL
LINE, X, Y = NS.LINE, NS.X, NS.Y
COAT, COAT_S = (246, 248, 255), (200, 206, 235)


def hand(im, x, y, k, r=5.2):
    """Manito de nube (un copo chico con luz arriba)."""
    d = ImageDraw.Draw(im)
    d.ellipse([x - (r + 1.6) * k, y - (r + 1.6) * k, x + (r + 1.6) * k, y + (r + 1.6) * k], fill=LINE)
    d.ellipse([x - r * k, y - r * k, x + r * k, y + r * k], fill=NS.BASE)
    d.ellipse([x - r * 0.75 * k, y - r * 0.85 * k, x + r * 0.2 * k, y - r * 0.05 * k], fill=NS.LIGHT)


def coat(im, cx, cy, k):
    """Bata de científica: la parte de abajo de la nube se vuelve una bata blanca con solapas y bolsillo."""
    x, y = (X - cx) / k, (Y - cy) / k
    d = NS.cloud(x, y)
    inside = NS.cover(d * k)
    top = 13 + 1.6 * np.abs(x) * 0.18            # borde de arriba de la bata, casi recto
    m = inside * np.clip((y - top) * k / 1.3, 0, 1)
    t = np.clip((y - 13) / 18, 0, 1)[..., None]
    col = np.array(COAT, np.float32) + (np.array(COAT_S, np.float32) - np.array(COAT, np.float32)) * t
    lay = np.zeros((S, S, 4), np.float32); lay[..., :3] = col; lay[..., 3] = m * 255
    im.alpha_composite(Image.fromarray(lay.astype(np.uint8)))
    edge = inside * np.clip(1 - np.abs(y - top) * k / (1.2 * k), 0, 1)
    im.alpha_composite(NS.to_img(edge, LINE))
    dr = ImageDraw.Draw(im)
    def Pt(px, py): return (cx + px * k, cy + py * k)
    # solapas en V y botones
    dr.line([Pt(-7, 13.4), Pt(0, 23), Pt(7, 13.4)], fill=LINE, width=max(2, int(1.3 * k)))
    dr.line([Pt(0, 23), Pt(0, 31)], fill=LINE, width=max(2, int(1.1 * k)))
    for by in (25.5, 29):
        bx, byy = Pt(1.8, by); dr.ellipse([bx - 0.8 * k, byy - 0.8 * k, bx + 0.8 * k, byy + 0.8 * k], fill=LINE)
    # bolsillo con lápices
    x0, y0 = Pt(-19, 20)
    dr.rectangle([x0 + 2 * k, y0 - 5 * k, x0 + 3.4 * k, y0 + 1 * k], fill=SKY, outline=LINE, width=max(1, int(0.7 * k)))
    dr.rectangle([x0 + 4.6 * k, y0 - 4 * k, x0 + 6 * k, y0 + 1 * k], fill=CORAL, outline=LINE, width=max(1, int(0.7 * k)))
    dr.rounded_rectangle([x0, y0, x0 + 8.5 * k, y0 + 6 * k], radius=1.5 * k, fill=COAT, outline=LINE, width=max(1, int(1 * k)))


def glasses(im, cx, cy, k):
    d = ImageDraw.Draw(im)
    for sx in (-1, 1):
        ex, ey = cx + sx * 10.5 * k, cy + 2 * k
        d.ellipse([ex - 7.2 * k, ey - 7.2 * k, ex + 7.2 * k, ey + 7.2 * k], outline=LINE, width=max(2, int(1.3 * k)))
    d.arc([cx - 3.4 * k, cy - 1 * k, cx + 3.4 * k, cy + 3 * k], 200, 340, fill=LINE, width=max(2, int(1.3 * k)))
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
    for sx in (-1, 1):
        ex, ey = cx + sx * 10.5 * k, cy + 2 * k
        ld.arc([ex - 5.6 * k, ey - 5.6 * k, ex + 5.6 * k, ey + 5.6 * k], 200, 250, fill=(255, 255, 255, 170), width=max(2, int(1.2 * k)))
    im.alpha_composite(lay)


def scientist(im, cx, cy, k, with_glasses=True):
    NS.nubi(im, cx, cy, k, 'hola')
    coat(im, cx, cy, k)
    if with_glasses: glasses(im, cx, cy, k)


def pose_cientifica(im, cx, cy, k):
    """Científica con su tablilla: presenta la medida del final."""
    scientist(im, cx, cy, k)
    d = ImageDraw.Draw(im)
    x0, y0 = cx + 22 * k, cy + 2 * k
    d.rounded_rectangle([x0, y0 + 1.5 * k, x0 + 18 * k, y0 + 24 * k], radius=2 * k, fill=LINE)
    d.rounded_rectangle([x0, y0, x0 + 18 * k, y0 + 22.5 * k], radius=2 * k, fill=(214, 178, 120), outline=LINE, width=max(2, int(1.2 * k)))
    d.rectangle([x0 + 2 * k, y0 + 3 * k, x0 + 16 * k, y0 + 20.5 * k], fill=(252, 252, 255))
    d.rounded_rectangle([x0 + 6 * k, y0 - 1.5 * k, x0 + 12 * k, y0 + 2.5 * k], radius=1 * k, fill=(170, 176, 200), outline=LINE, width=max(1, int(0.8 * k)))
    for i in range(3):
        d.line([(x0 + 4 * k, y0 + (7 + i * 4) * k), (x0 + (14 - i * 3) * k, y0 + (7 + i * 4) * k)], fill=(170, 166, 210), width=max(1, int(1.1 * k)))
    d.line([(x0 + 4 * k, y0 + 18 * k), (x0 + 8 * k, y0 + 15 * k), (x0 + 13 * k, y0 + 16.5 * k)], fill=SKY, width=max(2, int(1.3 * k)))
    hand(im, x0 + 1 * k, y0 + 13 * k, k, 4.2)


def pose_pizarra(im, cx, cy, k):
    """Explica en una pizarra con puntero: tutorial de la primera vez."""
    d = ImageDraw.Draw(im)
    bx0, by0, bx1, by1 = cx - 4 * k, cy - 40 * k, cx + 46 * k, cy - 4 * k
    d.rounded_rectangle([bx0, by0 + 2 * k, bx1, by1 + 2 * k], radius=3 * k, fill=LINE)
    d.rounded_rectangle([bx0, by0, bx1, by1], radius=3 * k, fill=(40, 72, 96), outline=LINE, width=max(2, int(1.4 * k)))
    # dibujo en la pizarra: una ruta con flecha y dos planetas (como un tutorial)
    d.ellipse([bx0 + 6 * k, by1 - 14 * k, bx0 + 14 * k, by1 - 6 * k], outline=(240, 240, 240), width=max(2, int(1.1 * k)))
    d.ellipse([bx1 - 14 * k, by0 + 6 * k, bx1 - 6 * k, by0 + 14 * k], outline=SUN, width=max(2, int(1.1 * k)))
    pts = [(bx0 + 14 * k, by1 - 10 * k), (bx0 + 24 * k, by1 - 20 * k), (bx1 - 20 * k, by1 - 12 * k), (bx1 - 14 * k, by0 + 13 * k)]
    for a, b in zip(pts, pts[1:]):
        for t in np.linspace(0, 1, 5)[:-1]:
            x, y = a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t
            d.ellipse([x - 0.9 * k, y - 0.9 * k, x + 0.9 * k, y + 0.9 * k], fill=(240, 240, 240))
    NS.nubi(im, cx - 16 * k, cy + 8 * k, k * 0.82, 'hola')
    d = ImageDraw.Draw(im)
    hx, hy = cx + 6 * k, cy + 2 * k
    d.line([(hx, hy), (bx0 + 25 * k, by1 - 20 * k)], fill=LINE, width=max(3, int(2.2 * k)))
    d.line([(hx, hy), (bx0 + 25 * k, by1 - 20 * k)], fill=(210, 160, 100), width=max(2, int(1.2 * k)))
    hand(im, hx, hy, k, 4.8)


def pose_lupa(im, cx, cy, k):
    """Con lupa: el descubrimiento del día."""
    NS.nubi(im, cx - 4 * k, cy + 2 * k, k * 0.95, 'coach')
    d = ImageDraw.Draw(im)
    gx, gy, gr = cx + 22 * k, cy - 14 * k, 11 * k
    d.line([(gx - gr * 0.6, gy + gr * 0.6), (gx - gr * 1.6, gy + gr * 1.9)], fill=LINE, width=int(5.5 * k))
    d.line([(gx - gr * 0.6, gy + gr * 0.6), (gx - gr * 1.6, gy + gr * 1.9)], fill=(214, 150, 90), width=int(3 * k))
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
    ld.ellipse([gx - gr, gy - gr, gx + gr, gy + gr], fill=(190, 230, 255, 110))
    ld.arc([gx - gr * 0.7, gy - gr * 0.7, gx + gr * 0.7, gy + gr * 0.7], 200, 260, fill=(255, 255, 255, 220), width=int(1.6 * k))
    im.alpha_composite(lay)
    d = ImageDraw.Draw(im)
    d.ellipse([gx - gr, gy - gr, gx + gr, gy + gr], outline=LINE, width=int(2 * k))
    d.ellipse([gx - gr + 1.8 * k, gy - gr + 1.8 * k, gx + gr - 1.8 * k, gy + gr - 1.8 * k], outline=(200, 206, 235), width=int(1.2 * k))
    d.polygon(N.sparkle(gx + 1 * k, gy + 1 * k, 5 * k), fill=SUN)
    hand(im, gx - gr * 1.25, gy + gr * 1.45, k)


def pose_idea(im, cx, cy, k):
    """Una idea: el consejo concreto del final (sin culpa)."""
    NS.glow(im, cx + 20 * k, cy - 30 * k, 16 * k, SUN, 150)
    NS.nubi(im, cx, cy + 4 * k, k * 0.95, 'coach')
    d = ImageDraw.Draw(im)
    x, y = cx + 20 * k, cy - 30 * k
    pts = N.star(x, y, 9 * k, inner=0.5)
    d.polygon([(px, py + 1.4 * k) for px, py in pts], fill=LINE)
    d.polygon(pts, fill=SUN, outline=LINE, width=max(2, int(1.3 * k)))
    for a in range(0, 360, 45):
        r0, r1 = 12 * k, 15 * k; aa = math.radians(a)
        d.line([(x + math.cos(aa) * r0, y + math.sin(aa) * r0), (x + math.cos(aa) * r1, y + math.sin(aa) * r1)], fill=SUN, width=max(2, int(1.3 * k)))
    hand(im, cx + 26 * k, cy - 4 * k, k)


POSES = [('Científica', pose_cientifica, 'te explica tu medida'), ('Maestra', pose_pizarra, 'la primera vez en un juego'),
         ('Exploradora', pose_lupa, 'el descubrimiento del día'), ('Idea', pose_idea, 'un consejo concreto')]


def sprite(fn, size, scale=0.9):
    c = Image.new('RGBA', (S, S), (0, 0, 0, 0)); fn(c, S / 2, S / 2 + NS.u(4), NS.u(0.95) * scale)
    return c.resize((int(size), int(size)), Image.LANCZOS)


def phone(w, h, seed):
    bg = NM.nebula_bg(int(w), int(h), seed)
    m = Image.new('L', bg.size, 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, bg.size[0] - 1, bg.size[1] - 1], radius=dp(22), fill=255)
    return bg, m


def mini_result(im, x0, y0, w, h):
    """El final de Radar con Nubi científica explicando la medida (texto suelto, sin recuadros)."""
    bg, m = phone(w, h, 12); im.paste(bg, (int(x0), int(y0)), m)
    d = ImageDraw.Draw(im)
    d.text((x0 + dp(16), y0 + dp(20)), 'TU VISTAZO', font=F(13), fill=SUN)
    d.text((x0 + dp(16), y0 + dp(40)), '84 ms', font=F(34), fill=WHITE)
    d.text((x0 + dp(16), y0 + dp(84)), 'con 3 astronautas a la vez', font=F(14, False), fill=DIM)
    im.alpha_composite(sprite(pose_cientifica, dp(120), 0.85), (int(x0 - dp(4)), int(y0 + dp(112))))
    d = ImageDraw.Draw(im)
    d.text((x0 + dp(16), y0 + dp(236)), 'Nubi te explica', font=F(13), fill=SUN)
    P.wrap(d, x0 + dp(16), y0 + dp(258), 'Es el destello más corto que viste bien. Menos es mejor.', F(14, False), WHITE, w - dp(32))
    P.wrap(d, x0 + dp(16), y0 + h - dp(46), 'Medida de esta partida. No es un diagnóstico.', F(12, False), DIM, w - dp(32))


def mini_tutorial(im, x0, y0, w, h):
    """Primera vez en un juego: Nubi en la pizarra, en 3 pasos."""
    bg, m = phone(w, h, 14); im.paste(bg, (int(x0), int(y0)), m)
    d = ImageDraw.Draw(im)
    d.text((x0 + dp(16), y0 + dp(20)), 'PRIMERA VEZ', font=F(13), fill=SUN)
    d.text((x0 + dp(16), y0 + dp(42)), 'Tráfico Estelar', font=F(22), fill=WHITE)
    im.alpha_composite(sprite(pose_pizarra, dp(150), 0.8), (int(x0 + w / 2 - dp(75)), int(y0 + dp(66))))
    d = ImageDraw.Draw(im)
    P.wrap(d, x0 + dp(16), y0 + dp(222), 'Toca los desvíos para que cada cápsula llegue al planeta de su color.', F(14, False), WHITE, w - dp(32))
    for i in range(3):
        cx = x0 + w / 2 + (i - 1) * dp(16); r = dp(4.5)
        d.ellipse([cx - r, y0 + h - dp(62) - r, cx + r, y0 + h - dp(62) + r], fill=SUN if i == 0 else (120, 120, 170))
    bx0, bx1, by = x0 + dp(16), x0 + w - dp(16), y0 + h - dp(46)
    d.rounded_rectangle([bx0, by + dp(4), bx1, by + dp(36)], radius=dp(14), fill=INK)
    d.rounded_rectangle([bx0, by, bx1, by + dp(32)], radius=dp(14), fill=SUN, outline=INK, width=int(dp(2.5)))
    d.text(((bx0 + bx1) / 2, by + dp(16)), 'Siguiente', font=F(15), fill=INK, anchor='mm')


def sheet():
    H = dp(920)
    im = NM.nebula_bg(W, int(H), 9); d = ImageDraw.Draw(im)
    d.text((dp(20), dp(28)), 'Nubi te guía', font=F(26), fill=SUN)
    P.wrap(d, dp(20), dp(64), 'Explica, muestra y acompaña; nunca evalúa ni pone cara triste por cómo te fue.',
           F(13, False), DIM, W - dp(40))
    im.alpha_composite(sprite(pose_cientifica, dp(220), 0.95), (int(W / 2 - dp(110)), int(dp(96))))
    y = dp(330); cw = (W - dp(24)) / 4
    for i, (name, fn, use) in enumerate(POSES):
        x = dp(12) + cw * i + cw / 2
        im.alpha_composite(sprite(fn, dp(108)), (int(x - dp(54)), int(y)))
        d = ImageDraw.Draw(im)
        d.text((x, y + dp(110)), name, font=F(14), fill=WHITE, anchor='ma')
        P.wrap(d, x, y + dp(130), use, F(12, False), DIM, cw - dp(10), anchor='center')
    y = dp(510)
    d.text((dp(20), y), 'Así aparece', font=F(17), fill=SUN)
    pw = (W - dp(52)) / 2
    mini_result(im, dp(20), y + dp(34), pw, dp(360))
    mini_tutorial(im, dp(32) + pw, y + dp(34), pw, dp(360))
    out = os.path.join(P.OUT, 'nubi-guia.png'); im.convert('RGB').save(out); print(out)


if __name__ == '__main__':
    sheet()
