# Pestaña "Hoy": mezcla elegida por Ricardo (28-sep) de las propuestas 1 (Tu planeta) y 2 (el dato del día).
# Maqueta PIL, NO la app. Tres momentos: antes de jugar, al terminar la sesión y al tocar una zona del planeta.
# Uso: python3 tools/previews/inicio_planeta.py  ->  docs/previews/inicio-planeta.png
import os, math, importlib.util
from PIL import Image, ImageDraw, ImageFont
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('prop', os.path.join(HERE, 'inicio_propuestas.py'))
P = importlib.util.module_from_spec(spec); spec.loader.exec_module(P)
W, H, dp, F = P.W, P.H, P.dp, P.F
INK, SUN, WHITE, DIM, SOFT, LIME, CORAL, SKY, CREAM = P.INK, P.SUN, P.WHITE, P.DIM, P.SOFT, P.LIME, P.CORAL, P.SKY, P.CREAM
DOM = P.DOM
MEM = (150, 190, 255); LEN = (120, 230, 170)


def orbit_ships(im, cx, cy, r, ships):
    """Las 3 partidas del día orbitan el planeta; las jugadas ya aterrizaron en su zona (con ✓)."""
    d = ImageDraw.Draw(im)
    # mitad de adelante de la órbita (la de atrás se dibuja antes que el planeta, en orbit_back)
    d.arc([cx - r * 1.5, cy - r * 0.5, cx + r * 1.5, cy + r * 0.5], 0, 180, fill=(255, 255, 255, 70), width=int(dp(2)))
    for gid, ang, landed in ships:
        a = math.radians(ang)
        x, y = (cx + math.cos(a) * r * 1.5, cy + math.sin(a) * r * 0.5) if not landed else landed
        P.game_planet(im, x, y, dp(16 if not landed else 13), gid)
        d = ImageDraw.Draw(im)
        if landed:
            P.disc(d, x + dp(12), y - dp(12), dp(7), LIME, 2, 1); P.check(d, x + dp(12), y - dp(12), dp(7))


def orbit_back(im, cx, cy, r):
    d = ImageDraw.Draw(im)
    d.arc([cx - r * 1.5, cy - r * 0.5, cx + r * 1.5, cy + r * 0.5], 180, 360, fill=(255, 255, 255, 45), width=int(dp(2)))


def zones_line(d, y, left, right):
    """Una línea bajo el planeta: lo que creció y lo que está quieto (texto suelto, sin recuadros)."""
    (l1, l2, lc), (r1, r2, rc) = left, right
    d.text((dp(24), y), l1, font=F(12, False), fill=DIM)
    d.text((dp(24), y + dp(15)), l2, font=F(17), fill=lc)
    d.text((W - dp(24), y), r1, font=F(12, False), fill=DIM, anchor='ra')
    d.text((W - dp(24), y + dp(15)), r2, font=F(17), fill=rc, anchor='ra')


def discovery(im, y, gid, what, big, unit, vals, lower_better, caption, badge=None):
    """El dato del día: una medida de un juego estrella con su evolución breve (6 partidas)."""
    d = ImageDraw.Draw(im)
    d.line([(dp(24), y - dp(10)), (W - dp(24), y - dp(10))], fill=(255, 255, 255, 36), width=int(dp(1)))
    d.text((dp(24), y), 'DESCUBRIMIENTO DEL DÍA', font=F(11), fill=SUN)
    P.game_planet(im, dp(38), y + dp(40), dp(14), gid); d = ImageDraw.Draw(im)
    d.text((dp(58), y + dp(32)), what, font=F(13, False), fill=DIM, anchor='lm')
    bf = F(34); d.text((dp(24), y + dp(52)), big, font=bf, fill=WHITE)
    bw = d.textlength(big, font=bf)
    d.text((dp(24) + bw + dp(6), y + dp(70)), unit, font=F(16), fill=DIM)
    if badge:
        bx = dp(24) + bw + dp(10) + d.textlength(unit, font=F(16)) + dp(8)
        d.rounded_rectangle([bx, y + dp(66), bx + dp(62), y + dp(86)], radius=dp(10), fill=SUN, outline=INK, width=int(dp(2)))
        d.text((bx + dp(31), y + dp(76)), badge, font=F(11), fill=INK, anchor='mm')
    # evolución: línea suave con la primera y la última, "mejor" siempre hacia arriba
    gx0, gx1, gy0, gy1 = W - dp(150), W - dp(28), y + dp(34), y + dp(84)
    lo, hi = min(vals), max(vals)
    def Y(v):
        k = (v - lo) / (hi - lo or 1)
        return gy0 + k * (gy1 - gy0) if lower_better else gy1 - k * (gy1 - gy0)
    pts = [(gx0 + (gx1 - gx0) * i / (len(vals) - 1), Y(v)) for i, v in enumerate(vals)]
    d.line([(gx0, gy1 + dp(6)), (gx1, gy1 + dp(6))], fill=(255, 255, 255, 40), width=int(dp(1)))
    d.line(pts, fill=SKY, width=int(dp(3)), joint='curve')
    for i, (x, yy) in enumerate(pts):
        last = i == len(pts) - 1
        rr = dp(5 if last else 3)
        d.ellipse([x - rr, yy - rr, x + rr, yy + rr], fill=SUN if last else SKY, outline=INK, width=int(dp(1.5)))
    fmt = lambda v: f'{v:g}'.replace('.', ',')
    d.text((gx0, gy1 + dp(10)), fmt(vals[0]), font=F(11, False), fill=SOFT)
    d.text((gx1, gy1 + dp(10)), fmt(vals[-1]), font=F(11), fill=SUN, anchor='ra')
    d.text((gx1, gy0 - dp(16)), f'tus últimas {len(vals)} partidas', font=F(11, False), fill=SOFT, anchor='ra')
    d.text((dp(24), y + dp(114)), caption, font=F(13, False), fill=DIM)


def frame_morning():
    im = P.sky(21); P.header(im, 'Tu planeta')
    cx, cy, r = W / 2, dp(252), dp(110)
    orbit_back(im, cx, cy, r)
    P.blob_planet(im, cx, cy, r)
    orbit_ships(im, cx, cy, r, [('series', 200, None), ('radar', 330, None), ('rumbo', 60, None)])
    P.note(im, W - dp(16), cy + r + dp(6), 'gira despacio', 'ra')
    d = ImageDraw.Draw(im)
    zones_line(d, dp(402), ('Esta semana creció', 'Memoria', MEM), ('Quieta hace 5 días', 'Lenguaje', LEN))
    discovery(im, dp(458), 'radar', 'Tu vistazo en Radar', '84', 'ms', [132, 121, 118, 104, 96, 84], True,
              'Tu mejor marca: ves más en menos tiempo.', 'Récord')
    P.action(im, 'Tu sesión de hoy · 3 juegos (cada uno aterriza en su zona)', 'series', 'Detective de Series', y=H - dp(262))
    P.navbar(im); return im


def frame_done():
    im = P.sky(22); P.header(im, 'Tu planeta')
    cx, cy, r = W / 2, dp(252), dp(110)
    grows = dict(P.DEFAULT_GROWS); grows['memoria'] = 1.25
    orbit_back(im, cx, cy, r)
    P.blob_planet(im, cx, cy, r, grows, spark='memoria')
    orbit_ships(im, cx, cy, r, [('series', 0, (cx + r * 0.52, cy + r * 0.52)), ('radar', 0, (cx + r * 0.15, cy - r * 0.05)),
                                ('rumbo', 0, (cx - r * 0.35, cy - r * 0.62))])
    d = ImageDraw.Draw(im)
    d.text((cx - r * 0.55, cy - r * 1.18), '¡Creció Memoria!', font=F(18), fill=SUN, anchor='mm', stroke_width=int(dp(2)), stroke_fill=INK)
    P.note(im, W - dp(16), cy + r + dp(6), 'la zona se ilumina y crece', 'ra')
    zones_line(d, dp(402), ('Hoy entrenaste', 'Memoria · Razonamiento · Velocidad', WHITE), ('', '', WHITE))
    discovery(im, dp(458), 'rumbo', 'Tu brújula en Rumbo a Casa', '18%', 'de casa', [34, 31, 27, 24, 21, 18], True,
              'Llegas cada vez más cerca de casa.')
    d = ImageDraw.Draw(im); y = H - dp(262)
    P.game_planet(im, dp(35), y + dp(12), dp(13), 'bitacora'); d = ImageDraw.Draw(im)
    d.text((dp(56), y + dp(12)), 'Bitácora: tu informe está listo', font=F(13), fill=SUN, anchor='lm')
    d.text((dp(24), y + dp(42)), '¡Sesión completa! Vuelve mañana: tu planeta te espera', font=F(13, False), fill=DIM, anchor='lm')
    x0, x1, yb, h = dp(20), W - dp(20), y + dp(56), dp(58)
    d.rounded_rectangle([x0, yb + dp(5), x1, yb + h + dp(5)], radius=dp(20), fill=INK)
    d.rounded_rectangle([x0, yb, x1, yb + h], radius=dp(20), fill=LIME, outline=INK, width=int(dp(3)))
    d.text(((x0 + x1) / 2, yb + h / 2), 'Otra ronda', font=F(20), fill=INK, anchor='mm')
    P.navbar(im); return im


def frame_zone():
    im = P.sky(23); P.header(im, 'Tu planeta')
    cx, cy, r = W / 2, dp(252), dp(110)
    P.blob_planet(im, cx, cy, r, spark='memoria')
    dim = Image.new('RGBA', im.size, (4, 6, 24, 150)); im.alpha_composite(dim)
    d = ImageDraw.Draw(im)
    # diálogo (las tarjetas solo van en diálogos): la zona de Memoria
    x0, x1, y0, y1 = dp(16), W - dp(16), dp(300), H - dp(20)
    d.rounded_rectangle([x0, y0 + dp(6), x1, y1 + dp(6)], radius=dp(28), fill=INK)
    d.rounded_rectangle([x0, y0, x1, y1], radius=dp(28), fill=CREAM, outline=INK, width=int(dp(3)))
    P.disc(d, x0 + dp(40), y0 + dp(40), dp(18), DOM['memoria'], 3, 3)
    d.polygon([(x0 + dp(40), y0 + dp(26)), (x0 + dp(49), y0 + dp(40)), (x0 + dp(40), y0 + dp(52)), (x0 + dp(31), y0 + dp(40))], fill=(190, 222, 255), outline=INK, width=int(dp(1.5)))
    d.text((x0 + dp(68), y0 + dp(28)), 'Zona de Memoria', font=F(22), fill=INK, anchor='lm')
    d.text((x0 + dp(68), y0 + dp(52)), 'nivel 7 · creció 3 veces esta semana', font=F(13, False), fill=(90, 84, 130), anchor='lm')
    # evolución del nivel (4 semanas)
    gx0, gx1, gy0, gy1 = x0 + dp(24), x1 - dp(24), y0 + dp(84), y0 + dp(140)
    vals = [4, 4, 5, 5, 6, 6, 7]
    pts = [(gx0 + (gx1 - gx0) * i / 6, gy1 - (v - 3.5) / 4 * (gy1 - gy0)) for i, v in enumerate(vals)]
    d.polygon(pts + [(gx1, gy1), (gx0, gy1)], fill=(214, 228, 255))
    d.line(pts, fill=DOM['memoria'], width=int(dp(3)), joint='curve')
    d.ellipse([pts[-1][0] - dp(5), pts[-1][1] - dp(5), pts[-1][0] + dp(5), pts[-1][1] + dp(5)], fill=SUN, outline=INK, width=int(dp(1.5)))
    d.text((gx0, gy1 + dp(6)), 'hace 4 semanas', font=F(11, False), fill=(110, 104, 150))
    d.text((gx1, gy1 + dp(6)), 'hoy', font=F(11), fill=INK, anchor='ra')
    # juegos de la zona con su medida
    y = gy1 + dp(34)
    rows = [('bitacora', 'Bitácora de Misión', 'recordaste 5 de 6 a los 12 min'), ('rumbo', 'Rumbo a Casa', 'a 18% de casa · tu mejor'),
            ('correo', 'Correo Estelar', '8 de 9 encargos'), ('secuencia', 'Secuencia Lumínica', 'nivel 9'), ('parejas', 'Parejas Ocultas', 'sin jugar esta semana')]
    for gid, name, m in rows:
        P.game_planet(im, x0 + dp(40), y + dp(18), dp(16), gid); d = ImageDraw.Draw(im)
        d.text((x0 + dp(68), y + dp(8)), name, font=F(16), fill=INK)
        d.text((x0 + dp(68), y + dp(28)), m, font=F(12, False), fill=(90, 84, 130))
        d.line([(x0 + dp(68), y + dp(46)), (x1 - dp(24), y + dp(46))], fill=(226, 218, 200), width=int(dp(1)))
        y += dp(54)
    yb, h = y1 - dp(84), dp(56)
    d.rounded_rectangle([x0 + dp(20), yb + dp(5), x1 - dp(20), yb + h + dp(5)], radius=dp(20), fill=INK)
    d.rounded_rectangle([x0 + dp(20), yb, x1 - dp(20), yb + h], radius=dp(20), fill=SUN, outline=INK, width=int(dp(3)))
    d.text(((x0 + x1) / 2, yb + h / 2), 'Jugar Parejas Ocultas', font=F(19), fill=INK, anchor='mm')
    return im


if __name__ == '__main__':
    shots = [frame_morning(), frame_done(), frame_zone()]
    names = ['Antes de jugar', 'Al terminar la sesión', 'Al tocar una zona']
    k = 0.6; sw, sh = int(W * k), int(H * k); gap = 30; cap = 64
    sheet = Image.new('RGB', (3 * sw + 4 * gap, sh + cap + 2 * gap), (2, 3, 16)); sd = ImageDraw.Draw(sheet)
    for i, s in enumerate(shots):
        x = gap + i * (sw + gap)
        sd.text((x + sw / 2, gap + cap / 2), names[i], font=ImageFont.truetype(P.FB, 34), fill=WHITE, anchor='mm')
        sheet.paste(s.convert('RGB').resize((sw, sh), Image.LANCZOS), (x, gap + cap))
        s.convert('RGB').resize((W * 3 // 4, H * 3 // 4), Image.LANCZOS).save(f'{P.OUT}/inicio-planeta-{i + 1}.png')
    sheet.save(f'{P.OUT}/inicio-planeta.png')
    print('OK')
