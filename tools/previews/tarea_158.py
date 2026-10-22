# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de la Tarea 158, «La isla es el mapa» (21-oct-2026; `auditoria-fable/arquitectura-pestanas.md`, opción 2): `docs/previews/tarea-158.png`.
Las pestañas pasan de Hoy · Juegos · Avance a **Hoy · Avance · Tú**: Juegos se va (la isla es el mapa), Hoy suma la losa «Todos los juegos» con su buscador, el edificio abre la lámina con UNA línea de avance, Tú es el perfil como pestaña (con la losa «Ajustes») y los dos botones de arriba a la derecha desaparecen.
CAPTURAS REALES de Roborazzi a 393 dp (`AppActualScreenshotTest`, con unos 20 días de uso de ejemplo): «HOY» = el commit `bb9bef7` (la app de la Tarea 156, antes de esta) y «PROPUESTA» = `docs/previews/app-actual/` ya regenerado. Abajo, la barra a tamaño real (1 px = 1 px del teléfono, 2,75 px por dp ≈ 420-440 ppp) con letra 1,0 y 1,3, y lo que se midió de los
tres íconos nuevos (contraste contra su fondo y «parecido tras el desenfoque», como en la lámina de la Tarea 157).
Uso:  python -I tools/previews/tarea_158.py      → docs/previews/tarea-158.png    (las tomas del emulador, si existen, son `docs/previews/emulador-barra-*.png`)
"""
import io, os, subprocess, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PREV = os.path.join(ROOT, 'docs', 'previews'); APP = os.path.join(PREV, 'app-actual'); OUT = os.path.join(PREV, 'tarea-158.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
BEFORE = 'bb9bef7'
NAVY, SUN, DIM, CREAM, LIME, CORAL = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100), (255, 120, 90)
PPD = 2.75; SIGMA = 5
CW = 420; GAP = 24; MG = 40; COLS = 5
W = MG * 2 + COLS * CW + (COLS - 1) * GAP


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def old(name):
    raw = subprocess.run(['git', '-C', ROOT, 'show', '%s:docs/previews/app-actual/%s.png' % (BEFORE, name)], capture_output=True, check=True).stdout
    return Image.open(io.BytesIO(raw)).convert('RGB')


def new(name): return Image.open(os.path.join(APP, name + '.png')).convert('RGB')


def wrap(d, text, fnt, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=fnt) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


def rotulo(d, x, y, texto, fnt, col):
    """Una etiqueta chica sobre una toma, con su fondo para que no se pierda sobre lo que hay debajo."""
    w = d.textlength(texto, font=fnt); d.rectangle((x, y, x + w + 12, y + 30), fill=NAVY); d.text((x + 6, y + 2), texto, font=fnt, fill=col)


def coma(x, d=1): return ('%.*f' % (d, x)).replace('.', ',')


# ------------------------------------------------------------------ la barra dentro de una toma
def bar_box(frame, scale=1.0):
    """La barra de pestañas dentro de una toma de teléfono (1080 × 2401): abajo, sobre los 48 dp de la barra del sistema; mide 7 + 48 + 4 + 21·letra + 8 dp."""
    h = round((7 + 48 + 4 + 21 * scale + 8) * PPD); bottom = frame.height - round(48 * PPD)
    return (0, bottom - h, frame.width, bottom)


def gris(im): return im.convert('L').convert('RGB')


def desenfoque(im): return im.filter(ImageFilter.GaussianBlur(SIGMA))


def lum(a):
    c = np.asarray(a, dtype=float) / 255.0
    c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    return 0.2126 * c[..., 0] + 0.7152 * c[..., 1] + 0.0722 * c[..., 2]


def ratio(l1, l2):
    hi, lo = np.maximum(l1, l2), np.minimum(l1, l2); return (hi + 0.05) / (lo + 0.05)


def centro(i): return int(round(360 * i + 180)), int(round((7 + 24) * PPD))


def ventana(im, i, r_dp):
    cx, cy = centro(i); r = int(round(r_dp * PPD))
    a = np.asarray(im.crop((cx - r, cy - r, cx + r + 1, cy + r + 1)), dtype=float)
    yy, xx = np.mgrid[-r:r + 1, -r:r + 1]; return a, (xx ** 2 + yy ** 2) <= r * r


def fondo(im, i, r0_dp, r1_dp):
    cx, cy = centro(i); r1 = int(round(r1_dp * PPD)); a = np.asarray(im.crop((cx - r1, cy - r1, cx + r1 + 1, cy + r1 + 1)), dtype=float)
    yy, xx = np.mgrid[-r1:r1 + 1, -r1:r1 + 1]; d = np.sqrt(xx ** 2 + yy ** 2); ring = (d >= r0_dp * PPD) & (d <= r1)
    return np.median(a[ring], axis=0)


def borde(mask, k=3):
    from numpy.lib.stride_tricks import sliding_window_view
    pad = np.pad(mask, k, constant_values=False); w = sliding_window_view(pad, (2 * k + 1, 2 * k + 1)).min(axis=(2, 3))
    return mask & ~w


def ncc(u, v):
    u = u - u.mean(); v = v - v.mean(); return float((u * v).sum() / (np.sqrt((u * u).sum() * (v * v).sum()) + 1e-9))


def mide(bar, i):
    """Lo que mide un ícono de la barra (el disco crema de 48 dp es su fondo): lado mayor del dibujo en dp, contraste del contorno y del cuerpo, y lo que queda tras el desenfoque."""
    bg = fondo(bar, i, 21.4, 22.6); a, circ = ventana(bar, i, 21.2)          # dentro del disco: más allá de ~21 dp caen su canto y su sombra
    dist = np.sqrt(((a - bg) ** 2).sum(axis=2)); mask = (dist >= 45) & circ
    lbg = float(lum(bg[None, None, :])[0, 0]); R = ratio(lum(a), lbg)
    from numpy.lib.stride_tricks import sliding_window_view
    # sin los hilos de un píxel (el canto del disco): se mide con lo que sobrevive a una erosión de 5 × 5 px
    er = sliding_window_view(np.pad(mask, 2, constant_values=False), (5, 5)).min(axis=(2, 3))     # los trazos miden ≥ 6 px: sobreviven; un hilo de 1-2 px no
    ys, xs = np.where(er); alto = (ys.max() - ys.min() + 1 + 4) / PPD; ancho = (xs.max() - xs.min() + 1 + 4) / PPD
    edge = borde(mask); inner = mask & ~edge
    fx = np.asarray(Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(SIGMA)), dtype=float)
    Lb = lum(fx); extremo = np.percentile(Lb[circ], 2)
    return {'ancho': ancho, 'alto': alto, 'lado': max(ancho, alto), 'borde_med': float(np.median(R[edge])), 'borde_3': float((R[edge] >= 3).mean()),
            'cuerpo_med': float(np.median(R[inner])) if inner.any() else float('nan'), 'blur': float(ratio(extremo, lbg)), 'gris': (Lb - lbg)[circ], 'color': (fx - bg)[circ].ravel()}


def medidas():
    """De las tres pestañas, sin elegir (tomas con otra elegida) y elegida (sobre la losa sol): peor caso de cada medida, y el parecido entre las tres formas tras el desenfoque (1 = la misma mancha)."""
    frames = {'hoy': new('barra'), 'avance': new('barra-avance'), 'tu': new('barra-tu')}
    bars = {k: f.crop(bar_box(f)) for k, f in frames.items()}
    keys = ['hoy', 'avance', 'tu']
    sel = {k: mide(bars[k], i) for i, k in enumerate(keys)}
    off = {k: mide(bars['avance' if k == 'hoy' else 'hoy'], i) for i, k in enumerate(keys)}
    pares = ((0, 1), (0, 2), (1, 2))
    return {
        'sel': sel, 'off': off,
        'lado_min': min(m['lado'] for m in list(sel.values()) + list(off.values())), 'lado_max': max(m['lado'] for m in list(sel.values()) + list(off.values())),
        'borde_off': min(m['borde_med'] for m in off.values()), 'borde_sel': min(m['borde_med'] for m in sel.values()), 'borde_3': min(m['borde_3'] for m in list(sel.values()) + list(off.values())),
        'cuerpo_min': min(m['cuerpo_med'] for m in list(sel.values()) + list(off.values()) if m['cuerpo_med'] == m['cuerpo_med']),
        'blur': min(m['blur'] for m in list(sel.values()) + list(off.values())),
        'parecido_gris': max(ncc(off[keys[a]]['gris'], off[keys[b]]['gris']) for a, b in pares), 'parecido_color': max(ncc(off[keys[a]]['color'], off[keys[b]]['color']) for a, b in pares)
    }


# ------------------------------------------------------------------ la lámina
FILAS = [
    ('Hoy', 'Arriba a la derecha ya no hay botones (el perfil es «Tú»); bajo la ruta y su botón, la losa crema «Todos los juegos»; y, una sola vez, el globo de Nubi.',
     [('HOY · la app de la Tarea 156', lambda: old('hoy'), 'a'), ('PROPUESTA', lambda: new('hoy'), 'b'), ('Con el globo de Nubi (una vez)', lambda: new('hoy-globo'), 'b'), ('Letra 1,3', lambda: new('hoy-letra13'), 'b')]),
    ('El edificio en Hoy abre la lámina del lugar con UNA línea de avance',
     'Sin la barra de las 5 etapas ni el gráfico («Hábil · 47 de 100» y nada más); Nubi, las filas y «Jugar …» se quedan. El detalle del avance vive solo en Avance.',
     [('HOY', lambda: old('hoy-lamina'), 'a'), ('PROPUESTA', lambda: new('hoy-lamina'), 'b'), ('Letra 1,3', lambda: new('hoy-lamina-letra13'), 'b')]),
    ('Juegos, ahora «Todos los juegos» (la ventana que abre la losa de Hoy)',
     'Sin la pestaña Juegos: un buscador grande (ignora tildes y mayúsculas), los chips de área y los 17 juegos con la misma fila de la lámina; tocar una fila abre la ficha y de ahí se juega. Sin la barra de abajo; X y Atrás la cierran.',
     [('HOY · la pestaña Juegos', lambda: old('juegos'), 'a'), ('PROPUESTA · sin filtro', lambda: new('todos-los-juegos'), 'b'), ('Con «Memoria»', lambda: new('todos-los-juegos-memoria'), 'b'), ('Letra 1,3', lambda: new('todos-los-juegos-letra13'), 'b')]),
    ('Avance (la misma lámina, con todo el detalle)',
     'Avance queda como estaba, sin la frase del descanso (solo la pastilla «Racha: N días»; Nubi ya no habla de la racha) y con «ver en Tú» en el enlace de las medallas; su lámina conserva la etapa, la barra, el gráfico, las filas y el botón.',
     [('HOY', lambda: old('avance'), 'a'), ('PROPUESTA', lambda: new('avance'), 'b'), ('Con la lámina de un área', lambda: new('avance-lamina'), 'b'), ('Letra 1,3', lambda: new('avance-lamina-letra13'), 'b')]),
    ('Tú (el perfil como pestaña; antes un panel con X, escondido tras la inicial)',
     'Título «Tú», tu isla con sus piezas, tu nombre con «Cambiar», Racha · Partidas · Medallas, las medallas y, al final, la losa «Ajustes». Con la barra de abajo y su franja libre.',
     [('HOY · el panel «Tu perfil»', lambda: old('perfil'), 'a'), ('PROPUESTA', lambda: new('tu'), 'b'), ('Letra 1,3', lambda: new('tu-letra13'), 'b'), ('Sin partidas', lambda: new('tu-vacio'), 'b'), ('Entera: abajo, «Ajustes»', lambda: bottom_of('tu-completa'), 'b')]),
    ('Ajustes, abierto desde la losa de Tú (antes, desde el engranaje de arriba)',
     'El panel de Ajustes es el mismo, con su flecha de volver; Licencias sigue adentro.',
     [('HOY', lambda: old('ajustes'), 'a'), ('PROPUESTA', lambda: new('ajustes'), 'b')]),
]


def bottom_of(name):
    im = new(name)
    return im.crop((0, max(0, im.height - 2401), im.width, im.height))


def fit(im, w=CW):
    return im.resize((w, int(round(im.height * w / im.width))), Image.LANCZOS)


def main():
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    F_T, F_H, F_P, F_C, F_B = font(62), font(40), font(27, False), font(23, False), font(26, False)
    M = medidas()
    sheets = []
    for titulo, desc, celdas in FILAS:
        imgs = [(lab, fit(f()), tipo) for lab, f, tipo in celdas]
        sheets.append((titulo, wrap(probe, desc, F_P, W - 2 * MG), imgs))
    # la barra: recortes 1:1
    def barra(name, scale): f = new(name) if name else None; return f.crop(bar_box(f, scale))
    pares = [('HOY · letra 1,0', old('hoy').crop(bar_box(old('hoy'))), 'a'), ('PROPUESTA · letra 1,0 · Hoy elegida', new('barra').crop(bar_box(new('barra'))), 'b'),
             ('HOY · letra 1,3', old('hoy-letra13').crop(bar_box(old('hoy-letra13'), 1.3)), 'a'), ('PROPUESTA · letra 1,3 · Hoy elegida', new('barra-letra13').crop(bar_box(new('barra-letra13'), 1.3)), 'b'),
             ('PROPUESTA · Avance elegida', new('barra-avance').crop(bar_box(new('barra-avance'))), 'b'), ('PROPUESTA · Tú elegida', new('barra-tu').crop(bar_box(new('barra-tu'))), 'b'),
             ('PROPUESTA · Avance elegida · letra 1,3', new('barra-avance-letra13').crop(bar_box(new('barra-avance-letra13'), 1.3)), 'b'), ('PROPUESTA · Tú elegida · letra 1,3', new('barra-tu-letra13').crop(bar_box(new('barra-tu-letra13'), 1.3)), 'b')]
    pruebas = [('desenfoque gaussiano, radio 5 px', desenfoque(new('barra').crop(bar_box(new('barra'))))), ('escala de grises', gris(new('barra').crop(bar_box(new('barra')))))]
    emul = [(t, os.path.join(PREV, f)) for t, f in (('Emulador · 3 botones', 'emulador-barra-3botones.png'), ('Emulador · gestos', 'emulador-barra-gestos.png'), ('Emulador · letra 2,0', 'emulador-barra-letra20.png')) if os.path.exists(os.path.join(PREV, f))]
    intro = ('Ricardo eligió la arquitectura «La isla es el mapa»: tres pestañas con un trabajo cada una. Hoy = jugar (la isla es el mapa de los juegos; quien busca uno por su nombre usa «Todos los juegos»), Avance = ver cómo voy, Tú = lo mío. La pestaña Juegos desaparece, '
             'el perfil deja de ser un panel escondido tras un botón chico y los dos botones de arriba a la derecha se van de todas las pestañas. La vuelta tras una partida se simplifica a dos orígenes (Hoy y Avance) y deja lo abierto como estaba: la lámina del lugar, «Todos los juegos» con su filtro, su búsqueda y la fila marcada, o la lámina de Avance. '
             'Los íconos de la barra son vectores de línea de tinta (la isla de Hoy en miniatura, tres barras, una persona) en una caja de 40 dp dentro del disco crema de 48 dp (la isla de Hoy, un bloque isométrico con su ola, es la más alta: Opus pidió cambiar la losa plana, que se leía como un hoyo de golf). La frase de la racha («Descansaste el domingo: tu racha sigue.») vive solo en Hoy: ni Avance ni Tú la repiten (Avance conserva la pastilla «Racha: N días» y Tú las cifras). Todas las tomas son capturas reales de Roborazzi a 393 dp con unos 20 días de uso de ejemplo.')
    intro_l = wrap(probe, intro, F_P, W - 2 * MG)
    # alto
    H = MG + 80 + 34 * len(intro_l) + 30
    for titulo, dl, imgs in sheets: H += 56 + 32 * len(dl) + 10 + max(i.height for _, i, _ in imgs) + 36 + 24
    H += 60 + 36 * 8 + 40
    H += 56 + 36 * 3 + sum(1 for _ in pares) // 2 * 340 + 20 + 56 + 340 + 40 + (60 + 340 if emul else 0) + 40
    H += 2600                                                                  # holgura: al final se recorta a lo dibujado
    sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet); y = MG
    d.text((MG, y), 'Hoy · Avance · Tú: «La isla es el mapa» (Tarea 158): hoy | propuesta', font=F_T, fill=SUN); y += 80
    for ln in intro_l: d.text((MG, y), ln, font=F_P, fill=CREAM); y += 34
    y += 30
    for titulo, dl, imgs in sheets:
        d.text((MG, y), titulo, font=F_H, fill=SUN); y += 56
        for ln in dl: d.text((MG, y), ln, font=F_P, fill=DIM); y += 32
        y += 10; x = MG; h = max(i.height for _, i, _ in imgs)
        for lab, im, tipo in imgs:
            sheet.paste(im, (x, y)); d.rectangle((x, y, x + im.width - 1, y + im.height - 1), outline=(70, 74, 130), width=1)
            d.text((x, y + h + 6), lab, font=F_C, fill=(DIM if tipo == 'a' else LIME)); x += CW + GAP
        y += h + 36 + 24
    # --- la barra
    d.text((MG, y), 'La barra a tamaño real (1 px = 1 px del teléfono; mírala al 100 % de zoom)', font=F_H, fill=SUN); y += 56
    for ln in wrap(probe, 'Tres discos crema de 48 dp con un ícono vectorial de línea de tinta (trazo de 2,5 dp, dibujo visible de 28 a 35 dp), el rótulo de 16 sp, la elegida sobre la losa sol; 88 dp de alto (94 con letra 1,3), como antes.', F_P, W - 2 * MG): d.text((MG, y), ln, font=F_P, fill=DIM); y += 32
    y += 10
    colw = 1080
    for k in range(0, len(pares), 2):
        for j in range(2):
            lab, im, tipo = pares[k + j]; x = MG + j * (colw + GAP)
            sheet.paste(im, (x, y)); d.rectangle((x, y, x + im.width - 1, y + im.height - 1), outline=(70, 74, 130), width=1)
            rotulo(d, x, y, lab, F_C, DIM if tipo == 'a' else LIME)
        y += max(pares[k][1].height, pares[k + 1][1].height) + 16
    for j, (lab, im) in enumerate(pruebas):
        x = MG + j * (colw + GAP); sheet.paste(im, (x, y)); d.rectangle((x, y, x + im.width - 1, y + im.height - 1), outline=(70, 74, 130), width=1); rotulo(d, x, y, lab, F_C, DIM)
    y += pruebas[0][1].height + 20
    d.text((MG, y), 'Lo que se midió de los tres íconos nuevos (de los píxeles de las tomas, como en la lámina de la Tarea 157)', font=F_H, fill=SUN); y += 56
    ok = M['borde_off'] >= 3 and M['borde_sel'] >= 3 and M['lado_min'] >= 28
    lineas = [
        'Dibujo visible: de %s a %s dp de lado mayor (regla: 28 dp o más).' % (coma(M['lado_min']), coma(M['lado_max'])),
        'Contraste del contorno contra el disco crema: sin elegir %s:1 y elegida sobre la losa sol %s:1 (la mediana de los píxeles de la orilla; %d %% de ellos llega a 3:1; regla: 3:1 o más).' % (coma(M['borde_off']), coma(M['borde_sel']), round(100 * M['borde_3'])),
        'Tras el desenfoque (radio de 5 px) lo que queda de la mancha contra su fondo: %s:1. Parecido entre las tres formas tras el desenfoque (1 = la misma mancha): %s en grises y %s en color (el ícono de hoy, de churro oscuro, daba 0,94).' % (coma(M['blur']), coma(M['parecido_gris'], 2), coma(M['parecido_color'], 2)),
        'La isla es la única con relleno (cara lima, costados de arena y banderín coral; el relleno da hasta %s:1 contra el crema): el contorno carga la forma, el color no es información.' % coma(M['cuerpo_min']),
        ('CUMPLE las tres reglas de los íconos.' if ok else 'ATENCIÓN: alguna regla de los íconos no se cumple; revisar.')
    ]
    for t in lineas:
        for ln in wrap(probe, t, F_B, W - 2 * MG): d.text((MG, y), ln, font=F_B, fill=CREAM if not t.startswith(('CUMPLE', 'ATENCIÓN')) else (LIME if ok else CORAL)); y += 34
    y += 20
    if emul:
        d.text((MG, y), 'En el emulador medium_phone (API 36): la barra sobre las barras del sistema', font=F_H, fill=SUN); y += 56
        for ln in wrap(probe, 'Capturas reales del emulador (1080 px de ancho, 420 ppp): el pie de Hoy, con la barra de pestañas encima de la barra del sistema, con 3 botones, con gestos y con la letra del sistema al 200 % (la barra crece lo que crece el rótulo y sigue sobre la barra del sistema). '
                       'También se comprobó a mano: «Todos los juegos» se abre, «rescate relampago» encuentra «Rescate relámpago» y Atrás la cierra; Tú abre los Ajustes y Atrás vuelve a Tú.', F_P, W - 2 * MG): d.text((MG, y), ln, font=F_P, fill=DIM); y += 32
        y += 10; x = MG; rowh = 0
        for lab, path in emul:
            im = Image.open(path).convert('RGB')
            if x + im.width > W - MG: x = MG; y += rowh + 16; rowh = 0
            sheet.paste(im, (x, y)); d.rectangle((x, y, x + im.width - 1, y + im.height - 1), outline=(70, 74, 130), width=1); rotulo(d, x, y, lab, F_C, DIM)
            x += im.width + GAP; rowh = max(rowh, im.height)
        y += rowh + 20
    sheet = sheet.crop((0, 0, W, y + 30)); sheet.save(OUT)
    print('ok', OUT, sheet.size)
    print('medidas:', {k: (round(v, 2) if isinstance(v, float) else v) for k, v in M.items() if not isinstance(v, dict)})


if __name__ == '__main__':
    main()
