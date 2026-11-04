# -*- coding: utf-8 -*-
"""Lámina de la Tarea 223 (3-nov-2026): las llamitas (A) y el logotipo «Nubilus» (L1) que Ricardo eligió en la lámina de la Tarea 222, ya en la app. ANTES y DESPUÉS con los componentes reales: Hoy a 393 × 873 con letra 1,0, franjas a 360 dp y con
letra 2,0, la semana ampliada (con hoy encendido y con hoy aún apagado), los cinco cuadros del destello de la llama de hoy, el resumen del día, «Tu semana» de Avance, la meta semanal de Ajustes y el cuadro de la racha de Tú; y abajo las medidas, los
contrastes y lo que se conserva.

Las capturas salen de `Tarea223CapturasTest` y `Tarea223DestelloCapturasTest` (Roborazzi, `app/src/test/screenshots/t223/`, fuera de git): «antes» se grabó con el código de la Tarea 222 (T223_FASE=antes) y «después» con el nuevo. Cada grupo de capturas se grabó
en su propia corrida de Gradle porque, en una corrida con varias pruebas de capturas, Robolectric a veces deja la letra Fredoka sin su peso negrita (pasa también con el código de antes; es de la herramienta, no de la app).
Uso:  python -I tools/previews/tarea_223.py      → docs/previews/tarea-223.png
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 't223')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-223.png')
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


def load(name):
    path = os.path.join(SHOTS, name + '.png')
    if not os.path.exists(path):
        im = Image.new('RGB', (COLW, 300), (40, 36, 100)); ImageDraw.Draw(im).text((20, 120), 'falta ' + name, font=font(20), fill=SUN); return im
    return Image.open(path).convert('RGB')


def to_dp(im, dp_width):
    return im.resize((dp_width, int(round(im.height * dp_width / im.width))), Image.LANCZOS)


def crop_dp(name, x0, y0, x1, y1, dp_width=None, frame_dp=393.0):
    """Recorta una captura en dp (la captura mide `frame_dp` dp de ancho) y la lleva a `dp_width` dp de ancho (por omisión, el recorte tal cual)."""
    im = load(name); k = im.width / float(frame_dp)
    c = im.crop((int(x0 * k), int(y0 * k), int(x1 * k), int(y1 * k)))
    return to_dp(c, dp_width or int(x1 - x0))


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
    im = load('despues_hoy_393_10'); k = im.width / 393.0
    px = [im.getpixel((x, y)) for y in range(int(112 * k), int(150 * k)) for x in range(int(368 * k), int(390 * k))]
    px.sort(key=lum); return px[len(px) // 2]


def letters_gap():
    """El hueco de verdad entre las letras del logotipo y las del saludo (dp): se buscan las filas de píxeles con el crema de las letras arriba y se mide entre el último renglón del logotipo y el primero del saludo."""
    im = load('despues_hoy_393_10'); k = im.width / 393.0
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
    return (runs[1][0] - runs[0][1]) / k


# ---- los números que da `NubilusLogoTest`, `HoyTarjetaTest` y `HoyHistoryTest` (se actualizan con cada corrida)
MEDIDAS = {
    'baja': 20,                # dp que baja Hoy con el logotipo
    'titulo': ('«Tus últimas partidas» (Tarea 221): a 393 × 873 con letra 1,0 el título sigue asomando sin deslizar en 13 de los 14 estados (de 601 a 702 dp contra 710 de la barra). '
               'La excepción, que Ricardo acepta: «marca nueva con la ruta a medias», donde el título termina en 722 dp (antes 702) y ya no asoma. A 360 dp no asomaba y sigue igual. '
               'Hoy sin la tarjeta cabe entero en los 15 estados con letra 1,0 (el que menos sobra, 39 dp; antes 59); con 1,3 se pasan 14 · 31 · 33 · 56 dp en cuatro estados. '
               'A 360 × 780 con 1,0, «Otra vez» de «marca nueva» queda 20 dp bajo la barra (se alcanza deslizando; decisión de Opus con Ricardo) y el recuadro «Marca nueva», lo primero del bloque del Contrarreloj, sigue entero a la vista.'),
    'zona': ('Zona sin luceros (3 a 28 % del alto): el saludo, la fecha y la semana terminan en el 18,7 % a 393 × 873 con letra 1,0, 19,7 % con 1,3, 20,9 % a 360 × 780 y 25,3 % con 1,3; '
             'la nota de debajo, de dos renglones a 360 dp, pasa del 28 % (ya pasaba antes: 28,8 %), y su texto lleva orilla. '),
}


def main():
    sf = font(15, bold=False)
    d0 = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    sheet_w = MARGIN * 2 + 4 * COLW + 3 * GAP
    sky = sky_near_week()
    LG = letters_gap()
    cream_panel = (255, 235, 210)
    colors = [('llama encendida (naranja fuego)', (255, 138, 31), sky), ('centro de la llama (amarillo claro)', (255, 230, 128), sky),
              ('llama apagada, contorno gris lila (cielo)', (162, 159, 208), sky), ('aro lima de «hoy»', (155, 229, 100), sky), ('letras del logotipo (crema)', (246, 235, 211), sky),
              ('llama apagada en tinta suave (losa crema)', (81, 75, 120), cream_panel)]

    # 1) Hoy entera, antes y después, y a la derecha las franjas a 360 dp y con letra 2,0
    full_a, full_d = to_dp(load('antes_hoy_393_10'), 393), to_dp(load('despues_hoy_393_10'), 393)
    def strip(name, w, dp_h): return crop_dp(name, 0, 0, w, dp_h, dp_width=w, frame_dp=w)
    side = [
        ('Hoy ahora · 360 dp', 'Letra 1,0: saludo, fecha y semana con las farolas.', strip('antes_hoy_360_10', 360, 232)),
        ('Hoy con llamitas y logotipo · 360 dp', 'El logotipo cabe en una línea; las 7 llamitas, en una fila y «3 días» debajo, igual que antes a este ancho.', strip('despues_hoy_360_10', 360, 252)),
        ('Hoy ahora · letra 2,0', 'Letra del sistema al doble: la semana y «3 días» bajan a dos filas.', strip('antes_hoy_393_20', 393, 300)),
        ('Hoy con llamitas y logotipo · letra 2,0', 'El logotipo NO crece con la letra (es una marca): sigue de 30 dp y lo de abajo crece sin taparse.', strip('despues_hoy_393_20', 393, 322)),
    ]
    zoom = lambda n: to_dp(load(n), 393)
    ROWS = [
        # fila 2: la semana ampliada, con hoy encendido y con hoy aún apagado
        [('La semana ahora · con hoy', 'Farolas del kit: encendida y apagada casi no se distinguen y la racha es una gota distinta.', zoom('antes_zoom_semana_con_hoy')),
         ('La semana con llamitas · con hoy', 'Una llama de tres lenguas: encendida grande, naranja con el centro amarillo, contorno de tinta y halo; apagada chica, solo contorno gris lila. La racha usa la MISMA llama.', zoom('despues_zoom_semana_con_hoy')),
         ('La semana ahora · hoy sin jugar', 'Con la racha en 0 la gota va apagada.', zoom('antes_zoom_semana_sin_hoy')),
         ('La semana con llamitas · hoy sin jugar', 'Hoy apagada (con su aro lima) y la llama de la racha apagada: se distinguen por tamaño y relleno, no solo por color.', zoom('despues_zoom_semana_sin_hoy'))],
        # fila 3: resumen, Avance, Ajustes, Tú
        [('Resumen del día · ahora', 'Las 7 farolas y la gota bajo la losa crema.', crop_dp('antes_resumen_393_10', 0, 600, 393, 720)),
         ('Resumen del día · con llamitas', 'Las mismas 7 marcas, a 32 dp, con la llamita de la racha al lado.', crop_dp('despues_resumen_393_10', 0, 600, 393, 720)),
         ('Avance · «Tu semana» ahora', 'Farolas a 26 dp y «3 de 4 días de tu meta».', to_dp(load('antes_avance_semana'), 393)),
         ('Avance · «Tu semana» con llamitas', 'Llamitas a 26 dp: el aro lima de hoy y el texto no se tocan.', to_dp(load('despues_avance_semana'), 393))],
        [('Ajustes · meta semanal ahora', 'Siete farolas con su número; la elegida, en un chip sol.', crop_dp('antes_ajustes_meta', 0, 0, 393, 262)),
         ('Ajustes · meta semanal con llamitas', 'Se conservan el fundido al encender, el pulso de la tocada y el halo sol de las encendidas (más fuerte en la elegida). Sobre el crema la apagada va en tinta suave.', crop_dp('despues_ajustes_meta', 0, 0, 393, 262)),
         ('Tú · cuadro de la racha ahora', 'La gota de plasticina sobre la losa crema.', crop_dp('antes_tu_cuadros', 0, 0, 393, 250)),
         ('Tú · cuadro de la racha con llamitas', 'La misma llamita a 48 dp en la caja de 40: los cuatro cuadros siguen del mismo alto.', crop_dp('despues_tu_cuadros', 0, 0, 393, 250))],
    ]

    # 2) el destello: cinco cuadros de la llama de hoy
    glint = []
    for ms, scale in ((0, '0,80'), (150, '0,95'), (300, '1,10'), (450, '1,05'), (600, '1,00')):
        im = load('despues_glint_%d' % ms); k = im.width / 300.0
        glint.append((ms, scale, im.crop((int(222 * k), int(2 * k), int(290 * k), int(62 * k)))))

    heights = []
    for row in ROWS:
        shots = max(im.height for _, _, im in row)
        cap = max(len(wrap(d0, t, sf, COLW)) * 20 for _, t, _ in row)
        heights.append(26 + shots + 12 + cap + 34)

    side_h = []
    for i in range(0, 4, 2):
        shots = max(side[i][2].height, side[i + 1][2].height)
        cap = max(len(wrap(d0, side[j][1], sf, COLW)) * 20 for j in (i, i + 1))
        side_h.append(26 + shots + 12 + cap + 30)
    row1_h = max(26 + max(full_a.height, full_d.height) + 12 + 2 * 20 + 34, sum(side_h))

    gl_w = glint[0][2].width; gl_h = glint[0][2].height
    glint_h = 26 + gl_h + 30 + 20 * 3 + 20

    notes = [
        ('EL DESTELLO («de un color destellante»)', 'Solo en Hoy y solo en la llama de hoy, cuando pasa de apagada a encendida con Hoy a la vista (al volver de la primera partida del día): crece de 0,8× a 1,1× y vuelve a 1× desde su base en 0,6 s (`NubiMotion.CELEBRATE_MS`) y su halo late una vez. '
         'NO se repite al girar la pantalla, al cambiar de pestaña ni al abrir la app con el día ya encendido (se «arma» solo al ver hoy apagado con Hoy a la vista; `rememberTodayGlint`, `LlamitaTest`). Si la primera partida del día se jugó desde otra pestaña, el destello espera a que Hoy se vea. Con «quitar animaciones» aparece encendida de una vez.'),
        ('LO QUE MIDE', 'Hoy baja %d dp con el logotipo (6 dp de aire + 40 dp de caja + 2 dp hasta el saludo, menos los 28 dp de antes sobre el saludo). %s' % (MEDIDAS['baja'], MEDIDAS.get('titulo', ''))),
        ('CONTRASTE (cielo %d·%d·%d y losa crema)' % sky, ' · '.join('%s %.1f:1' % (n, contrast(c, bg)) for n, c, bg in colors) + '.  Regla: texto 4,5:1 y marca de estado al menos 3:1; sobre el crema la llama apagada cambia al gris lila del cielo por la tinta suave (el gris lila sobre crema daba 2,2:1).'),
        ('QUE NADA SE TAPE', ('Entre las letras del logotipo y las del saludo quedan %.1f dp (con el contorno de 2,5 dp y la sombra de 4 dp del logotipo, %.1f dp de aire visible). ' % (LG, LG - 6.5) if LG is not None else '') +
         MEDIDAS.get('zona', '') + ' Las 7 llamitas caben en una fila a 393 dp, a 360 dp y con letra 2,0; «3 días» va en la misma fila a 393 dp con letra 1,0 y baja a una segunda a 360 dp y con letra 2,0, igual que antes. Ninguna palabra queda partida.'),
        ('SE CONSERVA', 'El Primer vuelo mantiene sus farolas (marcan estaciones de vuelo, no días jugados); el lector sigue diciendo «farolas encendidas» en la semana (el texto no cambia); el aro lima de «hoy», los números 1 a 7 de Ajustes, el fundido y el halo de la meta. Juegos, Contrarreloj, Avance y Tú no llevan logotipo.'),
    ]
    note_h = 0
    for t, body in notes:
        note_h += 24 + len(wrap(d0, body, sf, sheet_w - 2 * MARGIN)) * 20 + 14
    head = 74
    sheet = Image.new('RGB', (sheet_w, MARGIN + head + row1_h + sum(heights) + glint_h + note_h + MARGIN), NAVY)
    d = ImageDraw.Draw(sheet)
    d.text((MARGIN, MARGIN), 'Tarea 223 · las llamitas y el logotipo «Nubilus», ya en la app (antes | después)', font=font(26), fill=SUN)
    sub = 'A tamaño real (1 dp = 1 px). Ricardo eligió en la lámina de la Tarea 222: «me quedo con las llamitas y el logotipo». Todo son los componentes REALES de la app, sin Blender: una sola llamita para los días jugados y la racha, y el logotipo solo en Hoy.'
    for i, line in enumerate(wrap(d, sub, sf, sheet_w - 2 * MARGIN)): d.text((MARGIN, MARGIN + 38 + i * 20), line, font=sf, fill=DIM)

    def cell(x, y, title, text, im):
        size = 17
        while size > 12 and d.textlength(title, font=font(size)) > COLW: size -= 1
        d.text((x, y), title, font=font(size), fill=SUN)
        sheet.paste(im, (x, y + 26))
        d.rectangle((x - 1, y + 25, x + im.width, y + 26 + im.height), outline=LINE)
        cy = y + 26 + im.height + 12
        for i, line in enumerate(wrap(d, text, sf, COLW)): d.text((x, cy + i * 20), line, font=sf, fill=DIM)

    y = MARGIN + head
    cell(MARGIN, y, 'Hoy ahora · 393 × 873 · letra 1,0', 'Farolas del kit casi iguales encendidas y apagadas; la racha con una gota; arriba solo el saludo.', full_a)
    cell(MARGIN + COLW + GAP, y, 'Hoy con llamitas y logotipo', 'Logotipo «Nubilus» arriba a la izquierda; la semana con llamitas; Hoy baja %d dp.' % MEDIDAS['baja'], full_d)
    sx = MARGIN + 2 * (COLW + GAP)
    yy = y
    for i in range(0, 4, 2):
        cell(sx, yy, side[i][0], side[i][1], side[i][2])
        cell(sx + COLW + GAP, yy, side[i + 1][0], side[i + 1][1], side[i + 1][2])
        yy += side_h[i // 2]
    y += row1_h
    for row, hrow in zip(ROWS, heights):
        x = MARGIN
        for title, text, im in row:
            cell(x, y, title, text, im)
            x += COLW + GAP
        y += hrow
    d.text((MARGIN, y), 'El destello de la llama de hoy (cinco cuadros de 0,6 s; la escala sale de `glintScale`)', font=font(17), fill=SUN)
    gx = MARGIN
    for ms, scale, im in glint:
        sheet.paste(im, (gx, y + 26))
        d.rectangle((gx - 1, y + 25, gx + im.width, y + 26 + im.height), outline=LINE)
        d.text((gx, y + 26 + im.height + 6), '%d ms · %s×' % (ms, scale), font=sf, fill=WHITE)
        gx += im.width + 24
    # al lado de los cuadros, lo que se ve
    tx = gx + 16
    lines = wrap(d, 'La llama nace chica (0,8×), crece hasta 1,1× a los 300 ms y se asienta en 1× a los 600 ms; el halo naranja late una vez en el medio. El aro lima de hoy no se mueve.', sf, sheet_w - MARGIN - tx)
    for i, line in enumerate(lines): d.text((tx, y + 30 + i * 20), line, font=sf, fill=DIM)
    y += glint_h
    for t, body in notes:
        d.text((MARGIN, y), t, font=font(16), fill=SUN)
        for i, line in enumerate(wrap(d, body, sf, sheet_w - 2 * MARGIN)): d.text((MARGIN, y + 24 + i * 20), line, font=sf, fill=WHITE)
        y += 24 + len(wrap(d, body, sf, sheet_w - 2 * MARGIN)) * 20 + 14
    sheet.save(OUT)
    print(OUT, sheet.size, 'cielo', sky, 'hueco letras', LG)
    for n, c, bg in colors: print('  %-48s %.2f:1' % (n, contrast(c, bg)))


main()
