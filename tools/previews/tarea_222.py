# -*- coding: utf-8 -*-
"""Lámina de la Tarea 222 (3-nov-2026): opciones para los farolitos de la semana de Hoy (A «llamitas» y B «solcitos») y para un logotipo «Nubilus» arriba de Hoy (L1 solo las letras, L2 con una estrellita en el punto de la «i»). SOLO LÁMINA: nada de esto está en la app.
Hoy de ahora frente a la propuesta a 393 × 873 con letra 1,0 (A + L1, A + L2 y B + L1), una franja a 360 dp y otra con letra 2,0 de A + L1, recortes ampliados de las marcas y del logotipo, las medidas (cuánto baja Hoy, si «Tus últimas partidas» sigue asomando) y los contrastes.
Las capturas salen de `Tarea222CapturasTest` (Roborazzi, `app/src/test/screenshots/t222/`, fuera de git). La propuesta es el Hoy de verdad con lo de arriba reemplazado y lo demás empujado hacia abajo: se empalman la parte de arriba dibujada con la propuesta y el resto de la captura real.
Uso:  python -I tools/previews/tarea_222.py      → docs/previews/tarea-222.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't222')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-222.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, LINE, WHITE = (20, 18, 63), (255, 203, 69), (200, 196, 235), (90, 86, 160), (240, 244, 255)
GAP, MARGIN, COLW = 36, 24, 393


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(draw, text, f, width):
    lines = []
    for para in text.split('\n'):
        cur = ''
        for word in para.split(' '):
            t = (cur + ' ' + word).strip()
            if draw.textlength(t, font=f) <= width: cur = t
            else: lines.append(cur); cur = word
        lines.append(cur)
    return lines


def meta(name):
    out = {}
    for line in open(os.path.join(SHOTS, name + '.txt'), encoding='utf-8'):
        k, v = line.strip().split('='); out[k] = int(v)
    return out


def load(name):
    path = os.path.join(SHOTS, name + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (COLW, 300), (40, 36, 100)); ImageDraw.Draw(im).text((20, 120), 'falta ' + name, font=font(20), fill=SUN); return im
    return Image.open(path).convert('RGB')


def to_dp(im, dp_width):
    return im.resize((dp_width, int(round(im.height * dp_width / im.width))), Image.LANCZOS)


def splice(prop_name, actual_name):
    """Hoy con la propuesta: la parte de arriba dibujada con la propuesta y el resto del Hoy real (de la tarjeta de la ruta hacia abajo) empujado lo que mide el logotipo; la barra de abajo no se mueve."""
    prop, act = load(prop_name), load(actual_name)
    mp, ma = meta(prop_name), meta(actual_name)
    delta = mp['card_top_px'] - ma['card_top_px']
    new = prop.copy()
    lower = act.crop((0, ma['card_top_px'], act.width, ma['bar_top_px'] - delta))
    new.paste(lower, (0, mp['card_top_px']))
    new.paste(act.crop((0, ma['bar_top_px'], act.width, act.height)), (0, ma['bar_top_px']))
    return new, delta / (act.width / 393.0)


def lum(c):
    def f(v):
        v /= 255.0
        return v / 12.92 if v <= 0.03928 else ((v + 0.055) / 1.055) ** 2.4
    return 0.2126 * f(c[0]) + 0.7152 * f(c[1]) + 0.0722 * f(c[2])


def contrast(a, b):
    la, lb = lum(a), lum(b)
    return (max(la, lb) + 0.05) / (min(la, lb) + 0.05)


def sky_near_week():
    """El color del cielo donde van las marcas (mediana de un trozo sin luceros, a la derecha de la racha)."""
    im = load('propuesta_A_L1_393_10'); k = im.width / 393.0
    px = []
    for y in range(int(150 * k), int(172 * k)):
        for x in range(int(330 * k), int(380 * k)): px.append(im.getpixel((x, y)))
    px.sort(key=lum); return px[len(px) // 2]


def letters_gap():
    """El hueco de verdad entre las letras del logotipo y las del saludo (dp): se buscan las filas de píxeles con el crema de las letras en la parte de arriba y se mide entre el último renglón del logotipo y el primero del saludo."""
    im = load('propuesta_A_L1_393_10'); k = im.width / 393.0
    cream = (246, 235, 211)
    rows = []
    for y in range(int(24 * k), int(110 * k)):
        n = sum(1 for x in range(int(20 * k), int(250 * k)) if all(abs(im.getpixel((x, y))[i] - cream[i]) <= 14 for i in range(3)))
        rows.append(n >= 3)
    runs, start = [], None
    for i, on in enumerate(rows + [False]):
        if on and start is None: start = i
        if not on and start is not None: runs.append((start, i)); start = None
    runs = [r for r in runs if r[1] - r[0] >= 3]
    if len(runs) < 2: return None
    logo, greeting = runs[0], runs[1]
    return (greeting[0] - logo[1]) / k


def main():
    sf = font(15, bold=False)
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    sheet_w = MARGIN * 2 + 4 * COLW + 3 * GAP

    sky = sky_near_week()
    LG = letters_gap()
    lg_txt = 'Entre las letras del logotipo y las del saludo quedan %s dp (descontando el contorno de 2,5 dp y la sombra de 4 dp del logotipo, %s dp de aire visible). ' % (('%.1f' % LG) if LG is not None else '?', ('%.1f' % (LG - 6.5)) if LG is not None else '?')
    colors = [('llama encendida (naranja fuego)', (255, 138, 31)), ('centro de la llama (amarillo claro)', (255, 230, 128)), ('solcito encendido (amarillo sol)', (255, 201, 60)),
              ('llama / solcito apagado (contorno gris lila)', (162, 159, 208)), ('aro lima de «hoy»', (155, 229, 100)), ('letras del logotipo (crema)', (246, 235, 211))]

    s1, d1 = splice('propuesta_A_L1_393_10', 'actual_393_10')
    s2, _ = splice('propuesta_A_L2_393_10', 'actual_393_10')
    s3, _ = splice('propuesta_B_L1_393_10', 'actual_393_10')

    def strip(name, w, crop_dp):
        im = load(name); k = im.width / float(w)
        return to_dp(im.crop((0, 0, im.width, int(crop_dp * k))), w)

    ROWS = [
        # fila 1: Hoy de ahora y las tres propuestas (a tamaño real)
        [('Hoy ahora · 393 × 873 · letra 1,0', 'Los farolitos casi no se distinguen entre encendido y apagado (solo cambia el brillo del vidrio) y arriba no hay nada de marca. Una semana con 3 días jugados y hoy encendido (el aro lima).', to_dp(load('actual_393_10'), 393)),
         ('A + L1 · llamitas y solo las letras', 'LA QUE RECOMIENDO. Llama de tres lenguas: encendida naranja con el centro amarillo y un halo cálido; apagada más chica, solo el contorno gris lila, sin halo. La racha («3 días») usa la MISMA llama. Hoy baja %.0f dp.' % d1, to_dp(s1, 393)),
         ('A + L2 · llamitas y estrellita en la «i»', 'Lo mismo, con una estrellita amarilla donde iba el punto de la «i» (es la «ı» sin punto de Fredoka más una estrella dibujada: si se elige, va a Blender).', to_dp(s2, 393)),
         ('B + L1 · solcitos y solo las letras', 'Sol con rayos (encendido) frente a un disco sin rayos (apagado). Ojo: el sol ya es lo que toca ahora (un solo sol en Hoy, P7): el botón de la ruta es el sol de la pantalla; con solcitos en la semana ya no lo es.', to_dp(s3, 393))],
        # fila 2: franjas
        [('Hoy ahora · franja a 360 dp', 'Letra 1,0: arriba la fecha y la semana, sin logotipo.', strip('actual_360_10', 360, 215)),
         ('A + L1 · franja a 360 dp', 'El logotipo cabe en una línea; las 7 marcas, en una fila y «3 días» debajo, igual que hoy a este ancho.', strip('propuesta_A_L1_360_10', 360, 235)),
         ('Hoy ahora · franja con letra 2,0', 'Con la letra del sistema al doble la semana y «3 días» bajan a dos filas.', strip('actual_393_20', 393, 300)),
         ('A + L1 · franja con letra 2,0', 'El logotipo NO crece con la letra (es una marca, no un texto): sigue de ~30 dp y el saludo y la semana crecen debajo, sin taparse.', strip('propuesta_A_L1_393_20', 393, 320))],
        # fila 3: recortes ampliados
        [('Marcas A · llamitas (ampliadas)', 'Encendida · apagada · encendida de hoy con su aro lima · la llama de la racha. Encendida y apagada se distinguen por forma (grande con tres lenguas frente a chica de contorno), relleno (naranja y amarillo frente a vacío) y halo, no solo por color.', to_dp(load('zoom_marcas_A'), 393)),
         ('Marcas B · solcitos (ampliadas)', 'Encendido · apagado · encendido de hoy con su aro · la gota de la racha de siempre. Se distinguen por los rayos y el relleno. Los rayos quedan dentro del aro de hoy.', to_dp(load('zoom_marcas_B'), 393)),
         ('Logotipo L1 (ampliado)', 'Fredoka Bold de ~30 dp, relleno crema, contorno grueso de tinta y sombra dura corrida. Las letras van un poco separadas. Lo lee el lector como «Nubilus» y como encabezado.', to_dp(load('zoom_logo_L1'), 393)),
         ('Logotipo L2 (ampliado)', 'La «i» sin punto y una estrellita amarilla (contorno de tinta y sombra dura) donde iba el punto.', to_dp(load('zoom_logo_L2'), 393))],
    ]

    heights = []
    for row in ROWS:
        shots = max(im.height for _, _, im in row)
        cap = max(len(wrap(d0, t, sf, COLW)) * 20 for _, t, _ in row)
        heights.append(26 + shots + 12 + cap + 34)

    # el bloque de texto de abajo: destello, medidas y contrastes
    notes = [
        ('EL DESTELLO («de un color destellante»)', 'No se anima ahora. Idea: un destello breve (~0,6 s) SOLO en la llama que se enciende hoy al volver de una partida: la llama pasa de apagada a encendida creciendo un poco (de 0,8× a 1,1× y de vuelta a 1×) mientras el halo late una vez y un brillo claro la recorre. Nada de movimiento con «quitar animaciones»: la llama aparece encendida de golpe. El resto de la semana queda quieto.'),
        ('LO QUE MIDE', 'Hoy baja 20 dp con el logotipo (6 de aire + 40 del logotipo + 2 hasta el saludo, menos los 28 dp de aire de antes sobre el saludo). A 393 × 873 con letra 1,0, «Tus últimas partidas» sigue asomando sin deslizar en 13 de los 14 estados de la §31; DEJA de asomar solo en «marca nueva con la ruta a medias»: su título pasaba de 702 a 722 dp y la barra empieza en 710 (antes asomaba con 8 dp de sobra). El que queda más justo es «partida en pausa»: 703 de 710.'),
        ('CONTRASTE (sobre el cielo, %d·%d·%d)' % sky, ' · '.join('%s %.1f:1' % (n, contrast(c, sky)) for n, c in colors) + '.  Regla: texto 4,5:1 y la marca encendida al menos 3:1: la llama (%.1f:1) y el solcito (%.1f:1) pasan con holgura; las letras crema del logotipo (%.1f:1) pasan el 4,5:1 del texto. El apagado va en gris lila (%.1f:1) para que se vea el contorno sin competir con el encendido.' % (contrast((255, 138, 31), sky), contrast((255, 201, 60), sky), contrast((246, 235, 211), sky), contrast((162, 159, 208), sky))),
        ('QUE NADA SE TAPE', lg_txt + 'Las 7 marcas caben en una fila a 393 dp, a 360 dp y con letra 2,0; «3 días» va en la misma fila a 393 dp con letra 1,0 y baja a una segunda a 360 dp y con letra 2,0, igual que hoy; nada se tapa. Ninguna palabra queda sola: el logotipo es una sola palabra y «3 días» va junto.'),
    ]
    note_h = 0
    for t, body in notes:
        note_h += 24 + len(wrap(d0, body, sf, sheet_w - 2 * MARGIN)) * 20 + 14
    head = 74
    sheet = Image.new('RGB', (sheet_w, MARGIN + head + sum(heights) + note_h + MARGIN), NAVY)
    d = ImageDraw.Draw(sheet)
    d.text((MARGIN, MARGIN), 'Tarea 222 · farolitos de la semana y logotipo «Nubilus» en Hoy (solo lámina: nada está en la app)', font=font(26), fill=SUN)
    sub = 'A tamaño real (1 dp = 1 px). Ricardo: «unas llamitas o solcitos», «de un color destellante»; y el logotipo «como cuando entras al Instagram». La propuesta es el Hoy de verdad con lo de arriba reemplazado y todo lo demás empujado hacia abajo.'
    for i, line in enumerate(wrap(d, sub, sf, sheet_w - 2 * MARGIN)): d.text((MARGIN, MARGIN + 38 + i * 20), line, font=sf, fill=DIM)
    y = MARGIN + head
    for row, hrow in zip(ROWS, heights):
        x = MARGIN
        for title, text, im in row:
            size = 17
            while size > 12 and d.textlength(title, font=font(size)) > COLW: size -= 1
            d.text((x, y), title, font=font(size), fill=SUN)
            sheet.paste(im, (x, y + 26))
            d.rectangle((x - 1, y + 25, x + im.width, y + 26 + im.height), outline=LINE)
            cy = y + 26 + im.height + 12
            for i, line in enumerate(wrap(d, text, sf, COLW)):
                d.text((x, cy + i * 20), line, font=sf, fill=DIM)
            x += COLW + GAP
        y += hrow
    for t, body in notes:
        d.text((MARGIN, y), t, font=font(16), fill=SUN)
        for i, line in enumerate(wrap(d, body, sf, sheet_w - 2 * MARGIN)): d.text((MARGIN, y + 24 + i * 20), line, font=sf, fill=WHITE)
        y += 24 + len(wrap(d, body, sf, sheet_w - 2 * MARGIN)) * 20 + 14
    sheet.save(OUT)
    print(OUT, sheet.size, 'delta %.1f dp' % d1, 'cielo', sky)
    for n, c in colors: print('  %-48s %.2f:1' % (n, contrast(c, sky)))


main()
