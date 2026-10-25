# -*- coding: utf-8 -*-
"""Las 17 portadas de la ficha del juego (Tarea 171, 25-oct-2026; rehechas en la Tarea 172: «Portadas que se entienden»).

Cada portada se arma con una captura REAL del juego corriendo en Unity (`unity/test-results/capturas/<juego>/`, fuera de git; se sacan con `bash tools/verificar-todo.sh --capturas todos`, 1080 × 2400):
un momento de plena partida, SIN el marcador, la consigna ni avisos dentro. Ricardo (25-oct): «algunas imágenes se ven cortadas y no se aprecia con claridad los objetos que el usuario usará o con los que interactuará en el juego».
Por eso cada entrada de PORTADAS lista los OBJETOS que tienen que verse para entender el juego (con su recuadro en la captura) y el script:
  1. los ENCUADRA: calcula la escala más grande a la que todos caben ENTEROS dentro de la zona VISIBLE de la ventanilla (la ventanilla menos los márgenes, la X de arriba a la derecha y lo que asoma por abajo: el emblema y «Sobre este juego»);
  1b. si entre dos objetos hay una franja de cielo VACÍO (la afirmación y sus botones en ¿Verdad o disparate?, el meteoro y el cañón), la QUITA (`cortes`: dos filas de la captura; se difuminan 30 filas de la costura): los objetos no se tocan ni se deforman y quedan más juntos, así que caben más grandes;
  2. si hace falta ALEJAR (escala < 1), completa los costados con el MISMO cielo del juego: el degradado de las orillas de la captura (color por fila, sin barras) más estrellas del mismo tamaño que las de la captura;
  3. MIDE: dice, objeto por objeto, si queda entero y dónde (informe en `out/informe.json`, que usa `tools/previews/tarea_172.py`).
Salen de 1200 × 626 px en webp a `app/src/main/res/drawable-nodpi/portada_<id>.webp`: UNA imagen sirve a la ventanilla ADAPTATIVA de la ficha (Tarea 172): 200 dp de alto con letra hasta 1,3 (se recorta un 5 % a cada lado) y 180 dp con letra mayor, para que la ficha a 2,0 siga cabiendo (se ve entera, un 10 % más chica).
Los objetos van dentro de la zona segura de LAS DOS ventanillas, de los dos tamaños de letra y de un teléfono de 360 dp (que recorta los costados). Un objeto entero pero de menos de ~24 dp no se aprecia: el informe lo marca como «chico».
Se regeneran con este script cuando cambia el arte de un juego, como las láminas de `docs/previews/`.
Uso:  python -I tools/previews/portadas.py                  → escribe las 17 portadas, el informe y la hoja de contacto tools/previews/out/portadas.png
      python -I tools/previews/portadas.py --solo piloto correo   → solo esas
      python -I tools/previews/portadas.py --zonas           → la hoja marca en rojo lo que la ficha le pone ENCIMA (X, emblema, «Sobre este juego»), en verde el recuadro de cada objeto y en amarillo la zona segura
      python -I tools/previews/portadas.py --sin-escribir    → solo mide y arma la hoja (no toca las imágenes de la app)
"""
import json, os, random, sys
from PIL import Image, ImageChops, ImageDraw, ImageFilter

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
CAPT = os.path.join(ROOT, 'unity', 'test-results', 'capturas')
RES = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'drawable-nodpi')
OUT = os.path.join(HERE, 'out')

# La ventanilla de la ficha (Tarea 172, ADAPTATIVA): 345 dp de ancho (la tarjeta de 365 dp menos 10 de cada lado) por 200 dp de alto con letra hasta 1,3 y 180 dp con letra mayor (`CoverHeight` en GameCover.kt).
# UNA imagen de 1200 × 626 px (la proporción de 345 × 180 dp) sirve a las dos: a 180 dp se ve entera (3,48 px por dp); a 200 dp la imagen se llena a lo alto y se recorta 60 px de cada lado (3,13 px por dp).
ANCHO_DP = 345
ALTO_GRANDE_DP, ALTO_CHICO_DP = 200, 180
W = 1200
PX = 1080 / ANCHO_DP                                          # 3,13 px por dp en la ventanilla de 200 dp
H = round(ALTO_GRANDE_DP * PX)                                # 626
PX_C = W / ANCHO_DP                                           # 3,48 px por dp en la ventanilla de 180 dp
OFF = (W - 1080) // 2                                         # 60 px: lo que la ventanilla de 200 dp recorta de cada lado
# Lo que la ficha dibuja sobre la ventanilla, en px de la imagen: la X (48 dp, a 8 de la esquina de arriba a la derecha) y, abajo, el emblema (56 dp) y «Sobre este juego» (48 dp), que asoman 28 dp por el borde de abajo y por dentro tapan los
# 28 dp de abajo. «Sobre este juego» mide 175 dp de ancho con letra 1,0 y ~222 con 1,3 (el alto es el mismo: una línea de 48 dp); con letra 2,0 pasa a dos líneas y tapa algo más: ahí se acepta un roce.
ZONAS_X = [(OFF + round((ANCHO_DP - 8 - 48) * PX), round(8 * PX), OFF + round((ANCHO_DP - 8) * PX), round(56 * PX)), (round(W - 56 * PX_C), round(8 * PX_C), round(W - 8 * PX_C), round(56 * PX_C))]
BAJO_GRANDE = H - round(28 * PX)                              # 538: de aquí para abajo, en la ventanilla de 200 dp, tapan el emblema y «Sobre este juego»
BAJO_CHICO = H - round(28 * PX_C)                             # 529: lo mismo en la de 180 dp
ZONA_EMBLEMA = (OFF + round(8 * PX), BAJO_GRANDE, OFF + round(64 * PX), H)
ZONA_SOBRE = (OFF + round((ANCHO_DP - 8 - 222) * PX), BAJO_GRANDE, OFF + round((ANCHO_DP - 8) * PX), H)
# La zona segura (lo que se ve de verdad en las dos ventanillas, con las dos letras y en cualquier teléfono): a los costados, el 6 % de la ventanilla de 200 dp (un teléfono de 360 dp recorta los costados); arriba, el 3 %; abajo,
# lo que deja libre «Sobre este juego» en la ventanilla de 180 dp, con un poco de aire.
MARGEN_X = round(1080 * 0.06)
SEGURA = (OFF + MARGEN_X, round(H * 0.03), OFF + 1080 - MARGEN_X, min(BAJO_GRANDE, BAJO_CHICO) - 9)

# juego: captura, objetos que deben verse (nombre, recuadro (x0, y0, x1, y1) EN LA CAPTURA, medido a ojo sobre la rejilla de la captura), de dónde sacar el cielo de los costados ('ambos', 'izq' o 'der'),
# y, si hace falta, 'escala' (tope), 'dx' y 'dy' (px de la imagen) para mover el conjunto, 'cortes' (franjas de cielo vacío que se quitan: (fila a, fila b) de la captura) y 'muestra' (columnas ((x, x), (x, x)) de la captura de donde sacar el cielo de la izquierda y de la derecha si la orilla lleva una franja de la interfaz). El objeto es lo que el jugador usa o toca; lo que solo decora (la antena, la tarjeta de la definición…) no cuenta.
PORTADAS = {
    'parejas': dict(frame='09-t40.png', fill='ambos', objetos=[
        ('la luz elegida, con su aro dorado', (143, 1089, 422, 1368)), ('el objeto que escondía (el cristal)', (556, 1470, 787, 1700)),
        ('luces dormidas', (776, 1263, 1002, 1490)), ('luces dormidas', (146, 1519, 372, 1743))]),
    'secuencia': dict(frame='05-t08.png', fill='ambos', objetos=[
        ('los 9 luceros en círculo', (104, 948, 1024, 1863))]),
    'bodega': dict(frame='05-t08.png', fill='ambos', objetos=[
        ('los 8 compartimentos del dial', (115, 817, 965, 1790)), ('el robot del centro', (461, 1194, 613, 1377))]),
    'correo': dict(frame='04-cinta-dorado.png', fill='izq', objetos=[
        ('la carta de la cinta', (278, 835, 620, 1115)), ('los 4 buzones', (26, 1259, 1044, 1580))]),
    'stroop': dict(frame='06-t20.png', fill='ambos', muestra=((50, 90), (992, 1025)), cortes=[(1150, 1570)], objetos=[
        ('la palabra en su tarjeta', (143, 529, 935, 1115)), ('los 4 botones de color (Rojo, Azul, Amarillo, Blanco)', (96, 1608, 985, 2185))]),
    'piloto': dict(frame='08-hiperimpulso.png', fill='ambos', cortes=[(1600, 1740)], objetos=[
        ('la nave', (537, 1780, 624, 1917)), ('las dos líneas de balizas', (390, 1383, 840, 1930)), ('la señal que se atrapa (el círculo con anillo)', (439, 1383, 602, 1569))]),
    'freno': dict(frame='06-t20.png', fill='ambos', objetos=[
        ('las dos naves', (102, 1460, 982, 1920)), ('los dos botones', (178, 1776, 907, 1972))]),
    'satelites': dict(frame='02-seguimiento.png', fill='ambos', objetos=[
        ('los satélites', (226, 752, 1028, 1329)), ('el planeta del centro', (402, 1177, 674, 1449))]),
    'radar': dict(frame='02-destello.png', fill='ambos', objetos=[
        ('el radar entero', (61, 392, 1022, 1368)), ('las cápsulas que se ven un instante', (309, 497, 939, 1203))]),
    'calculo': dict(frame='05-t08.png', fill='ambos', cortes=[(1130, 1325)], objetos=[
        ('el dial con el número que hay que formar', (287, 593, 800, 1100)), ('las tres fichas con número', (228, 1355, 854, 1573))]),
    'engranajes': dict(frame='11-t40.png', fill='ambos', objetos=[
        ('el motor con su flecha y su cartel', (70, 784, 265, 1083)), ('la cadena de ruedas, del motor a la turbina', (243, 784, 722, 1362)), ('la turbina con su cartel', (743, 1000, 1037, 1416))]),
    'acoplamiento': dict(frame='05-luz-encendida.png', fill='der', objetos=[
        ('el puerto con su pieza', (170, 1224, 906, 1867)), ('los botones Encaja y Espejo', (48, 1928, 1032, 2157))]),
    'aterrizaje': dict(frame='02-acierto.png', fill='ambos', objetos=[
        ('la nave con su bandera', (337, 1580, 570, 1826)), ('la regla de 0 a 100 con su marca', (98, 1862, 1000, 2040))]),
    'anagramas': dict(frame='05-fichas.png', fill='ambos', objetos=[
        ('las casillas de la palabra', (148, 1056, 935, 1150)), ('las fichas con letras', (109, 1246, 967, 1623))]),
    'meteoros': dict(frame='05-t08.png', fill='ambos', cortes=[(1560, 1740)], objetos=[
        ('el meteoro con su palabra', (439, 1089, 798, 1525)), ('el cañón', (406, 1782, 663, 2037))]),
    'disparate': dict(frame='05-t08.png', fill='ambos', cortes=[(1480, 1980)], objetos=[
        ('la afirmación', (104, 1237, 967, 1438)), ('los botones Verdad y Disparate', (59, 2026, 1015, 2327))]),
    'intrusa': dict(frame='05-t08.png', fill='ambos', cortes=[(975, 1195)], objetos=[
        ('las 5 palabras con su estrella', (22, 854, 967, 1547))]),
}


COSTURA = 30                                                  # filas que se difuminan en cada costura de un corte


def mapy(y, cortes):
    """La fila `y` de la captura, ya quitados los cortes (cada corte (a, b) quita de la fila a a la b + COSTURA)."""
    return y - sum(min(max(y - a, 0), b + COSTURA - a) for a, b in cortes)


def stitch(frame, cortes):
    """La captura sin las franjas de cielo vacío que dicen los cortes; en cada costura, 30 filas pasan de una orilla a la otra (el cielo es un degradado suave: la costura no se ve)."""
    cortes = sorted(cortes)
    out = Image.new('RGB', (frame.width, frame.height - sum(b - a + COSTURA for a, b in cortes)))
    y = 0; prev = 0
    for a, b in cortes:
        top = frame.crop((0, prev, frame.width, a - COSTURA)); out.paste(top, (0, y)); y += top.height
        up = frame.crop((0, a - COSTURA, frame.width, a)); down = frame.crop((0, b, frame.width, b + COSTURA))
        out.paste(Image.composite(down, up, Image.linear_gradient('L').resize((frame.width, COSTURA))), (0, y)); y += COSTURA
        prev = b + COSTURA
    out.paste(frame.crop((0, prev, frame.width, frame.height)), (0, y))
    return out


def union(boxes):
    return (min(b[0] for b in boxes), min(b[1] for b in boxes), max(b[2] for b in boxes), max(b[3] for b in boxes))


def hits(a, b):
    return a[0] < b[2] and b[0] < a[2] and a[1] < b[3] and b[1] < a[3]


def edge_column(img, side, muestra=None):
    """El color de cada fila en la orilla de la captura (franja de 8 px), sin lo que NO es cielo: la mediana de 101 filas quita lo angosto (un planeta del borde, una estrella) y deja el degradado y las bandas anchas (la cinta de Correo)."""
    import numpy as np
    if muestra: strip = img.crop((muestra[0], 0, max(muestra[1], muestra[0] + 1), img.height))                  # unas columnas de cielo que el juego indica (cuando la orilla lleva una franja de la interfaz)
    else: strip = img.crop((0, 0, 8, img.height)) if side == 'izq' else img.crop((img.width - 8, 0, img.width, img.height))
    col = np.asarray(strip.resize((1, img.height), Image.BOX), dtype=float)[:, 0, :]          # (alto, 3)
    k = 101; pad = k // 2
    padded = np.pad(col, ((pad, pad), (0, 0)), mode='edge')
    win = np.lib.stride_tricks.sliding_window_view(padded, k, axis=0)                           # (alto, 3, k)
    med = np.median(win, axis=2)
    out = Image.fromarray(med.reshape(-1, 1, 3).astype('uint8'), 'RGB')
    return out.filter(ImageFilter.GaussianBlur(6))


def sky_fill(scaled, ox, oy, fill, game, s, muestra=None):
    """El cielo de los costados (y de arriba o abajo si la captura no llega): el color de la orilla de cada fila, con un grano finísimo y estrellas del tamaño de las de la captura."""
    fh = scaled.height
    ml = mr = None
    if muestra: ml = tuple(round(v * s) for v in muestra[0]); mr = tuple(round(v * s) for v in muestra[1])
    left = edge_column(scaled, 'izq', ml); right = edge_column(scaled, 'der', mr)
    if fill == 'izq': right = left
    if fill == 'der': left = right

    def column_for_canvas(col):
        img = Image.new('RGB', (1, H))
        for y in range(H):
            sy = min(max(y - oy, 0), fh - 1)
            img.putpixel((0, y), col.getpixel((0, sy)))
        return img

    lc, rc = column_for_canvas(left).resize((W, H), Image.NEAREST), column_for_canvas(right).resize((W, H), Image.NEAREST)
    ramp = Image.linear_gradient('L').rotate(90).resize((W, H))            # 255 a la izquierda, 0 a la derecha
    base = Image.composite(lc, rc, ramp)
    noise = Image.effect_noise((W, H), 12).convert('RGB')
    base = Image.blend(base, ImageChops.add(base, noise, scale=2.0, offset=-64), 0.10)
    rnd = random.Random(game)
    d = ImageDraw.Draw(base, 'RGBA')
    n = int(W * H / (7000 * max(s, 0.3) ** 2) * 0.35)                       # unas 150 estrellas en la captura de 1080 × 2400; al achicarla, más juntas
    for _ in range(n):
        x, y = rnd.uniform(0, W), rnd.uniform(0, H)
        r = rnd.choice((0.8, 1.0, 1.2, 1.5, 2.0)) * max(s, 0.55)
        a = rnd.choice((90, 140, 190, 230))
        d.ellipse((x - r, y - r, x + r, y + r), fill=(235, 240, 255, a))
    return base


def place(game, spec):
    """Elige la escala y el lugar para que todos los objetos queden dentro de la zona segura, fuera de la X; devuelve (s, ox, oy)."""
    objetos = [o[1] for o in spec['objetos']]
    g = union(objetos)
    bw, bh = g[2] - g[0], g[3] - g[1]
    sx0, sy0, sx1, sy1 = SEGURA
    s_max = min((sx1 - sx0) / bw, (sy1 - sy0) / bh, spec.get('escala', 1.25))
    cx, cy = (sx0 + sx1) / 2 + spec.get('dx', 0), (sy0 + sy1) / 2 + spec.get('dy', 0)
    s = s_max
    while s > 0.2:
        ox = cx - s * (g[0] + g[2]) / 2; oy = cy - s * (g[1] + g[3]) / 2
        box = (g[0] * s + ox, g[1] * s + oy, g[2] * s + ox, g[3] * s + oy)
        if not any(hits(box, z) for z in ZONAS_X): return s, ox, oy
        s *= 0.98
    return s, ox, oy


def compose(game):
    spec = PORTADAS[game]
    path = os.path.join(CAPT, game, spec['frame'])
    if not os.path.exists(path): sys.exit(f'falta la captura {path}: sacarlas con `bash tools/verificar-todo.sh --capturas todos`')
    frame = Image.open(path).convert('RGB')
    cortes = sorted(spec.get('cortes', []))
    if cortes:
        frame = stitch(frame, cortes)
        spec = dict(spec, objetos=[(n, (b[0], mapy(b[1], cortes), b[2], mapy(b[3], cortes))) for n, b in spec['objetos']])
    s, ox, oy = place(game, spec)
    scaled = frame.resize((round(frame.width * s), round(frame.height * s)), Image.LANCZOS)
    ox_i, oy_i = round(ox), round(oy)
    canvas = sky_fill(scaled, ox_i, oy_i, spec['fill'], game, s, spec.get('muestra'))
    # la captura sobre el cielo, con los costados difuminados 36 px para que no se vea la costura
    mask = Image.new('L', scaled.size, 255); md = ImageDraw.Draw(mask)
    f = 56
    for i in range(f):
        a = int(255 * i / f)
        md.line([(i, 0), (i, scaled.height)], fill=a); md.line([(scaled.width - 1 - i, 0), (scaled.width - 1 - i, scaled.height)], fill=a)
    canvas.paste(scaled, (ox_i, oy_i), mask)
    # el informe: cada objeto, ya en la imagen final, y si queda entero
    informe = []
    for nombre, b in spec['objetos']:
        t = (round(b[0] * s + ox), round(b[1] * s + oy), round(b[2] * s + ox), round(b[3] * s + oy))
        problemas = []
        if t[0] < SEGURA[0]: problemas.append('pasa del margen izquierdo')
        if t[2] > SEGURA[2]: problemas.append('pasa del margen derecho')
        if t[1] < SEGURA[1]: problemas.append('pasa del margen de arriba')
        if t[3] > SEGURA[3]: problemas.append('queda bajo el borde (lo tapan el emblema o «Sobre este juego»)')
        if any(hits(t, z) for z in ZONAS_X): problemas.append('lo tapa la X')
        ancho_dp, alto_dp = round((t[2] - t[0]) / PX), round((t[3] - t[1]) / PX)
        informe.append(dict(nombre=nombre, caja=t, entero=not problemas, problemas=problemas, ancho_dp=ancho_dp, alto_dp=alto_dp, chico=min(ancho_dp, alto_dp) < 24 and max(ancho_dp, alto_dp) < 100))
    return canvas, dict(juego=game, captura=spec['frame'], escala=round(s, 3), objetos=informe), spec


def main():
    args = sys.argv[1:]
    only = args[args.index('--solo') + 1:] if '--solo' in args else list(PORTADAS)
    only = [a for a in only if not a.startswith('--')]
    os.makedirs(OUT, exist_ok=True)
    total = 0; made = []; reports = []
    for g in only:
        canvas, rep, spec = compose(g)
        if '--sin-escribir' not in args:
            dst = os.path.join(RES, f'portada_{g}.webp')
            canvas.save(dst, 'WEBP', quality=82, method=6)
            total += os.path.getsize(dst)
        made.append((g, canvas, rep)); reports.append(rep)
        malos = [o['nombre'] + ': ' + ', '.join(o['problemas']) for o in rep['objetos'] if not o['entero']] + [o['nombre'] + ': CHICO (' + str(o['ancho_dp']) + ' × ' + str(o['alto_dp']) + ' dp)' for o in rep['objetos'] if o['chico']]
        print(f"{g:13s} {rep['captura']:24s} escala {rep['escala']:.2f}  {'todo entero' if not malos else 'FALLA: ' + '; '.join(malos)}")
    with open(os.path.join(OUT, 'informe.json'), 'w', encoding='utf-8') as fh: json.dump(dict(ventana_dp=[ALTO_GRANDE_DP, ALTO_CHICO_DP], ancho=W, alto=H, portadas=reports), fh, ensure_ascii=False, indent=1)
    # hoja de contacto: las portadas a tamaño real, de dos en dos
    cols = 2; gap = 24
    rows = (len(made) + cols - 1) // cols
    sheet = Image.new('RGB', (cols * W + (cols + 1) * gap, rows * (H + gap) + gap), (20, 18, 63))
    for i, (g, c, rep) in enumerate(made):
        if '--zonas' in args:
            c = c.convert('RGBA'); ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); od = ImageDraw.Draw(ov)
            for z in ZONAS_X + [ZONA_EMBLEMA, ZONA_SOBRE]: od.rectangle(z, fill=(255, 0, 0, 90), outline=(255, 60, 60, 255), width=3)
            od.line([(OFF, 0), (OFF, H)], fill=(255, 255, 255, 200), width=2); od.line([(W - OFF, 0), (W - OFF, H)], fill=(255, 255, 255, 200), width=2)      # lo que ve la ventanilla de 200 dp
            od.rectangle(SEGURA, outline=(255, 220, 0, 255), width=3)
            for o in rep['objetos']: od.rectangle(o['caja'], outline=(60, 255, 120, 255) if o['entero'] else (255, 60, 255, 255), width=3)
            c = Image.alpha_composite(c, ov).convert('RGB')
        sheet.paste(c, (gap + (i % cols) * (W + gap), gap + (i // cols) * (H + gap)))
    sheet.save(os.path.join(OUT, 'portadas.png'), optimize=True)
    print(f'{len(made)} portadas de {W} × {H} px (ventanilla de {ALTO_GRANDE_DP} dp con letra hasta 1,3 y de {ALTO_CHICO_DP} con letra mayor), {total // 1024} KB en total; hoja: {os.path.join(OUT, "portadas.png")}')


if __name__ == '__main__':
    main()
