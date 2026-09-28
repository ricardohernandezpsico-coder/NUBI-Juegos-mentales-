# Pestaña "Juegos", segunda ronda (28-sep): a Ricardo le gustó la LISTA (propuesta 3: "súper ordenada", con la barra al
# costado) pero "le falta algo"; pidió verla más llamativa y original, y ver también la recomendación (para ti + filas por
# área). Maquetas PIL, NO la app. Uso: python3 tools/previews/juegos_final.py -> docs/previews/juegos-final.png (+ -1, -2).
import os, math, importlib.util
from PIL import Image, ImageDraw, ImageFont, ImageFilter
HERE = os.path.dirname(__file__)
spec = importlib.util.spec_from_file_location('jp', os.path.join(HERE, 'juegos_propuestas.py'))
J = importlib.util.module_from_spec(spec); spec.loader.exec_module(J)
P = J.P
W, H, dp, F = P.W, P.H, P.dp, P.F
INK, SUN, WHITE, DIM, SOFT, LIME, CORAL, SKY, CREAM = P.INK, P.SUN, P.WHITE, P.DIM, P.SOFT, P.LIME, P.CORAL, P.SKY, P.CREAM
DOM, NAMES, STAR, BYDOM, MEASURE, LEVEL, NEVER, light = J.DOM, J.NAMES, J.STAR, J.BYDOM, J.MEASURE, J.LEVEL, J.NEVER, J.light
SPARK = {'bitacora': [3, 4, 4, 5, 5], 'rumbo': [34, 30, 27, 24, 18], 'freno': [248, 240, 229, 222, 212],
         'piloto': [22, 19, 17, 14, 12], 'radar': [132, 118, 104, 96, 84], 'trafico': [3, 3, 4, 4, 5]}
LOWER = {'rumbo', 'freno', 'piloto', 'radar'}
LAST = {'bitacora': 'hoy', 'rumbo': 'ayer', 'secuencia': 'hace 3 días', 'parejas': 'hace 9 días', 'rutatesoro': 'hace 5 días',
        'piloto': 'ayer', 'freno': 'hoy', 'stroop': 'hace 2 días', 'cambiochip': 'hace 12 días'}
TODAY = {'bitacora', 'freno'}


def sparkline(d, x0, y0, w, h, vals, lower):
    lo, hi = min(vals), max(vals)
    pts = []
    for i, v in enumerate(vals):
        k = (v - lo) / (hi - lo or 1)
        pts.append((x0 + w * i / (len(vals) - 1), y0 + (k * h if lower else (1 - k) * h)))
    d.line(pts, fill=SKY, width=int(dp(2.5)), joint='curve')
    x, y = pts[-1]
    d.ellipse([x - dp(3.5), y - dp(3.5), x + dp(3.5), y + dp(3.5)], fill=SUN, outline=INK, width=int(dp(1.2)))


def check_badge(d, cx, cy, s):
    P.disc(d, cx, cy, s, LIME, 2, 1); P.check(d, cx, cy, s)


def horizon(im, dom, y, grow):
    """Cabecera de área: el borde de SU planeta asoma por la derecha, con sus construcciones (como en Tu planeta)."""
    r = dp(150); cx, cy = W - dp(30), y + dp(168)
    base = im
    im = Image.new('RGBA', base.size, (0, 0, 0, 0))
    P.glow(im, cx, y + dp(40), dp(90), DOM[dom], 90)
    d = ImageDraw.Draw(im)
    d.ellipse([cx - r, cy - r + dp(5), cx + r, cy + r + dp(5)], fill=INK)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=DOM[dom], outline=INK, width=int(dp(3)))
    # construcciones en el borde (las mismas de la zona del planeta de Hoy)
    for k in range(3):
        a = math.radians(236 + k * 14)
        bx, by = cx + math.cos(a) * r * 0.86, cy + math.sin(a) * r * 0.86
        h = dp(12 + 6 * grow)
        if dom == 'memoria':
            d.polygon([(bx, by - h), (bx + h * .35, by - h * .35), (bx, by), (bx - h * .35, by - h * .35)], fill=(190, 222, 255), outline=INK, width=int(dp(1.5)))
        elif dom == 'atencion':
            d.rectangle([bx - h * .15, by - h * .75, bx + h * .15, by], fill=CREAM, outline=INK, width=int(dp(1.5)))
            d.ellipse([bx - h * .22, by - h, bx + h * .22, by - h * .6], fill=SUN, outline=INK, width=int(dp(1.5)))
        else:
            d.polygon([(bx - h * .25, by), (bx - h * .15, by - h * .65), (bx, by - h), (bx + h * .15, by - h * .65), (bx + h * .25, by)], fill=(225, 212, 255), outline=INK, width=int(dp(1.5)))
    mask = Image.new('L', base.size, 0)
    ImageDraw.Draw(mask).rectangle([W * 0.45, y - dp(10), W, y + dp(56)], fill=255)
    a = im.split()[3].point(lambda v: v)
    from PIL import ImageChops
    im.putalpha(ImageChops.multiply(a, mask.filter(ImageFilter.GaussianBlur(dp(6)))))
    base.alpha_composite(im)
    return ImageDraw.Draw(base)


def lista():
    """Plantilla 3, más llamativa: ruta luminosa con cometa, horizonte del planeta en cada área, evolución e índice."""
    im = P.sky(41)
    d = ImageDraw.Draw(im)
    d.text((dp(20), dp(34)), 'Juegos', font=F(28), fill=WHITE, anchor='lm')
    d.text((dp(20), dp(60)), '19 juegos · descubriste 16', font=F(14, False), fill=DIM, anchor='lm')
    y = dp(78); x = dp(20)
    for i, (lab, n) in enumerate([('Por área', None), ('Olvidados', 3), ('Nuevos', 3), ('Más jugados', None)]):
        f = F(13)
        w = d.textlength(lab, font=f) + dp(24) + (dp(22) if n else 0)
        if i == 0:
            d.rounded_rectangle([x, y + dp(3), x + w, y + dp(33)], radius=dp(15), fill=INK)
            d.rounded_rectangle([x, y, x + w, y + dp(30)], radius=dp(15), fill=SUN, outline=INK, width=int(dp(2)))
            d.text((x + w / 2, y + dp(15)), lab, font=f, fill=INK, anchor='mm')
        else:
            d.text((x + dp(12), y + dp(15)), lab, font=F(13, False), fill=DIM, anchor='lm')
            if n:
                bx = x + dp(16) + d.textlength(lab, font=F(13, False))
                d.ellipse([bx, y + dp(6), bx + dp(18), y + dp(24)], fill=CORAL, outline=INK, width=int(dp(1.5)))
                d.text((bx + dp(9), y + dp(15)), str(n), font=F(11), fill=WHITE, anchor='mm')
        x += w - dp(4)

    RAIL = dp(44)
    top = dp(124)
    sections = [('memoria', 'Memoria', 1.0, 'creció 3 veces esta semana'), ('atencion', 'Atención', 0.75, 'jugaste Freno hoy')]
    y = top
    rows = []
    for dom, name, grow, note in sections:
        d = horizon(im, dom, y - dp(8), grow)
        d.text((dp(20), y + dp(14)), name.upper(), font=F(22), fill=light(dom), anchor='lm')
        d.text((dp(20), y + dp(38)), f'{len(BYDOM[dom])} juegos · {note}', font=F(12, False), fill=SOFT, anchor='lm')
        y += dp(62)
        for g in BYDOM[dom]:
            rows.append((g, dom, y))
            y += dp(62)
        y += dp(6)
    # ruta luminosa que une los planetas (color de cada área) + cometa
    for (g1, d1, y1), (g2, d2, y2) in zip(rows, rows[1:]):
        if d1 != d2: continue
        P.glow(im, RAIL, (y1 + y2) / 2 + dp(24), dp(18), DOM[d1], 40)
        d = ImageDraw.Draw(im)
        d.line([(RAIL, y1 + dp(24)), (RAIL, y2 + dp(24))], fill=light(d1) + (200,), width=int(dp(3)))
    cy = rows[3][2] + dp(24) + dp(31)
    for k in range(12):
        P.glow(im, RAIL, cy - k * dp(7), dp(12 - k * 0.8), SUN, 200 - k * 15)
    d = ImageDraw.Draw(im)
    d.ellipse([RAIL - dp(5), cy - dp(5), RAIL + dp(5), cy + dp(5)], fill=(255, 245, 200))
    right = W - dp(44)
    for g, dom, yy in rows:
        new = g in NEVER
        if new:
            d = ImageDraw.Draw(im)
            d.ellipse([RAIL - dp(24), yy + dp(0), RAIL + dp(24), yy + dp(48)], outline=SUN, width=int(dp(2)))
        J.planet(im, RAIL, yy + dp(24), dp(21), g, faded=new)
        d = ImageDraw.Draw(im)
        if g in TODAY: check_badge(d, RAIL - dp(17), yy + dp(8), dp(7))
        d.text((dp(78), yy + dp(8)), NAMES[g], font=F(16), fill=WHITE if not new else DIM)
        if new:
            d.rounded_rectangle([dp(78), yy + dp(30), dp(78) + dp(88), yy + dp(48)], radius=dp(9), fill=SUN, outline=INK, width=int(dp(2)))
            d.text((dp(78) + dp(44), yy + dp(39)), 'Nuevo para ti', font=F(10), fill=INK, anchor='mm')
            d.text((right, yy + dp(24)), 'pruébalo ›', font=F(12), fill=SUN, anchor='rm')
        elif g in SPARK:
            d.text((dp(78), yy + dp(30)), 'Tu marca: ' + MEASURE[g], font=F(12), fill=SUN)
            sparkline(d, right - dp(70), yy + dp(10), dp(64), dp(20), SPARK[g], g in LOWER)
            d.text((right, yy + dp(36)), LAST.get(g, ''), font=F(11, False), fill=SOFT, anchor='ra')
        else:
            lv = LEVEL.get(g, 5)
            d.text((dp(78), yy + dp(30)), f'nivel {lv}', font=F(12, False), fill=light(dom))
            # nivel en 12 peldaños (como la escalera de dificultad)
            for k in range(12):
                px = right - dp(70) + k * dp(5.6)
                on = k < lv
                d.rounded_rectangle([px, yy + dp(12), px + dp(4), yy + dp(26)], radius=dp(2), fill=light(dom) if on else (255, 255, 255, 34))
            d.text((right, yy + dp(36)), LAST.get(g, ''), font=F(11, False), fill=SOFT, anchor='ra')
        d.line([(dp(78), yy + dp(56)), (right, yy + dp(56))], fill=(255, 255, 255, 22), width=int(dp(1)))
    # índice de áreas al costado (la "barra": toca un punto y salta a esa área; marca dónde vas)
    ix = W - dp(14); iy0 = dp(250)
    d.rounded_rectangle([ix - dp(7), iy0 - dp(12), ix + dp(7), iy0 + dp(5 * 34) + dp(12)], radius=dp(7), fill=(255, 255, 255, 20))
    for k, (dom, name) in enumerate(J.DOMS):
        yy = iy0 + k * dp(34)
        sel = k == 0
        rr = dp(7 if sel else 4.5)
        d.ellipse([ix - rr, yy - rr, ix + rr, yy + rr], fill=DOM[dom], outline=INK if sel else None, width=int(dp(2)))
    P.navbar(im, 1); return im


def recomendacion():
    """Para ti hoy (decide por la persona) + filas por área (ordena y deja explorar)."""
    im = P.sky(42)
    d = ImageDraw.Draw(im)
    d.text((dp(20), dp(34)), 'Juegos', font=F(28), fill=WHITE, anchor='lm')
    y = dp(64)
    d.text((dp(20), y), 'PARA TI HOY', font=F(11), fill=SUN)
    P.glow(im, dp(78), y + dp(74), dp(78), DOM['lenguaje'], 100)
    J.planet(im, dp(78), y + dp(74), dp(48), 'anagramas')
    d = ImageDraw.Draw(im)
    d.text((dp(146), y + dp(26)), 'Anagramas', font=F(24), fill=WHITE)
    P.wrap(d, dp(146), y + dp(58), 'Tu zona de Lenguaje está quieta hace 6 días.', F(14, False), DIM, W - dp(166))
    bx0, by0 = dp(146), y + dp(100)
    d.rounded_rectangle([bx0, by0 + dp(4), bx0 + dp(140), by0 + dp(44)], radius=dp(16), fill=INK)
    d.rounded_rectangle([bx0, by0, bx0 + dp(140), by0 + dp(40)], radius=dp(16), fill=SUN, outline=INK, width=int(dp(3)))
    P.play_tri(d, bx0 + dp(24), by0 + dp(20), dp(12)); d.text((bx0 + dp(82), by0 + dp(20)), 'Jugar', font=F(17), fill=INK, anchor='mm')
    d.text((bx0 + dp(152), by0 + dp(20)), 'otro ›', font=F(13), fill=SUN, anchor='lm')
    y = dp(240)
    for dom, name in J.DOMS[:4]:
        games = BYDOM[dom]
        d.ellipse([dp(20), y + dp(4), dp(32), y + dp(16)], fill=DOM[dom], outline=INK, width=int(dp(2)))
        d.text((dp(40), y + dp(10)), name, font=F(19), fill=light(dom), anchor='lm')
        d.text((dp(40) + d.textlength(name, font=F(19)) + dp(10), y + dp(11)), f'{len(games)} juego' + ('s' if len(games) > 1 else ''),
               font=F(13, False), fill=SOFT, anchor='lm')
        x = dp(56)
        for g in games[:5]:
            new = g in NEVER
            J.planet(im, x, y + dp(62), dp(28), g, faded=new, lvl_ring=None if new else LEVEL.get(g, 6) / 12)
            d = ImageDraw.Draw(im)
            if g in TODAY: check_badge(d, x - dp(22), y + dp(40), dp(7))
            J.two_lines(d, x, y + dp(100), NAMES[g], F(11), WHITE, dp(84))
            sub, col = ('Nuevo', SUN) if new else ((MEASURE[g], SUN) if g in MEASURE else (f'nivel {LEVEL.get(g, 5)}', SOFT))
            d.text((x, y + dp(128)), sub, font=F(11), fill=col, anchor='ma')
            x += dp(92)
        y += dp(150)
    P.navbar(im, 1); return im


if __name__ == '__main__':
    shots = [recomendacion(), lista()]
    names = ['Mi recomendación: Para ti + áreas', 'Tu lista (3), más llamativa']
    for i, s in enumerate(shots, 1):
        s.convert('RGB').resize((W * 3 // 4, H * 3 // 4), Image.LANCZOS).save(f'{P.OUT}/juegos-final-{i}.png')
    k = 0.62; sw, sh = int(W * k), int(H * k); gap = 30; cap = 64
    sheet = Image.new('RGB', (2 * sw + 3 * gap, sh + cap + 2 * gap), (2, 3, 16)); sd = ImageDraw.Draw(sheet)
    for i, s in enumerate(shots):
        x = gap + i * (sw + gap)
        sd.text((x + sw / 2, gap + cap / 2), names[i], font=ImageFont.truetype(P.FB, 30), fill=WHITE, anchor='mm')
        sheet.paste(s.convert('RGB').resize((sw, sh), Image.LANCZOS), (x, gap + cap))
    sheet.save(f'{P.OUT}/juegos-final.png')
    print('OK')
