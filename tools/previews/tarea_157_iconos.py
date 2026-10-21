# -*- coding: utf-8 -*-
"""Lámina de la Tarea 157, «Íconos de la barra que se reconozcan» (21-oct-2026): `docs/previews/tarea-157-iconos.png`.
La queja de Ricardo sobre la barra de la Tarea 156: «no se reconoce el cohete ni la farola; se ve algo gris oscuro, con una forma poco delimitada».
Compara, con las MISMAS metáforas (isla con banderita, cohete, farola) y la misma barra, el ícono de hoy (churro tinta de 32 dp) con tres estilos: E1 «Objetos de color» (los de plasticina de la barra de antes de la Tarea 156, sueltos y a su tamaño, con y sin disco),
E2 «Línea fina» (vector de tinta de 2,5 dp) y E3 «Dos tonos» (contorno de tinta de 2 dp y relleno de color del kit); y un bloque de metáforas (Juegos: cohete, fichas, mando; Avance: farola, barras, brote) en el estilo que mejor se lee.
Las tomas son REALES de Roborazzi (`NavIconosPreviewTest`, 393 dp de ancho, 2,75 px por dp; la barra de cada estilo) pegadas sobre la toma real de Hoy (`docs/previews/app-actual/hoy.png`, letra 1,0 y 1,3) donde va la barra. La maqueta de «HOY» se comprueba contra esa toma real (diferencia mostrada).
Las medidas salen de esos mismos píxeles: contraste (WCAG) del dibujo contra su fondo, el mismo contraste tras un desenfoque gaussiano de radio de 5 px (1,8 dp: la «prueba de entrecerrar los ojos») y cuánto se parecen las tres formas entre sí tras el desenfoque.
Uso (después de `./gradlew :app:recordRoborazziDebug --tests "*NavIconosPreviewTest*"`):  python -I tools/previews/tarea_157_iconos.py      → docs/previews/tarea-157-iconos.png
"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SHOTS = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'nav-iconos'); PREV = os.path.join(ROOT, 'docs', 'previews'); OUT = os.path.join(PREV, 'tarea-157-iconos.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LIME, CORAL = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100), (255, 120, 90)
PPD = 2.75                                  # píxeles por dp de las tomas (440 ppp ≈ los ~420 del teléfono)
SIGMA = 5                                   # desenfoque de la «prueba de entrecerrar los ojos»: 5 px = 1,8 dp (los rótulos casi se pierden; las formas se quedan)
TABS = [('hoy', 'Hoy'), ('juegos', 'Juegos'), ('avance', 'Avance')]
# (clave de la toma, título, descripción, color del título)
ESTILOS = [
    ('actual', 'HOY · la barra de la Tarea 156', 'el churro tinta de 32 dp (un asset de 102 px renderizado para 24 dp y ampliado) en el disco crema de 48 dp: lo que probó Ricardo', DIM),
    ('e1-sin-disco', 'E1 · Objetos de color', 'los objetos de plasticina a color de la barra de antes de la Tarea 156 (isla en miniatura, cohete, farol), sueltos, de 36-39 dp de alto y renderizados a su tamaño (3 px por dp, nada se amplía). SIN disco: es la variante que mejor contrasta (sobre el disco crema, el crema, el lila y el dorado de los objetos se funden)', LIME),
    ('e1-noche', 'E1b · Objetos de color en una ventanilla noche', 'lo que encontré para que E1 se vea también ELEGIDA: la misma barra con cada objeto sobre un disco noche (regla R4 del kit: lo calibrado sobre oscuro va en su ventanilla noche)', LIME),
    ('e2-linea', 'E2 · Línea fina', 'vector de tinta, trazo de 2,5 dp, huecos interiores abiertos, dibujo de 28-31 dp, en el disco crema de 48 dp (la lámina C de Fable bien hecha)', LIME),
    ('e3-dos-tonos', 'E3 · Dos tonos', 'vector con contorno de tinta de 2 dp y relleno de color del kit (lima, coral, lila, sol, cielo), dibujo de 30-34 dp, en el disco crema de 48 dp', LIME),
]
ESTILO_BLOQUE = 'e3-dos-tonos'
METAFORAS = [('cohete', 'Juegos · cohete'), ('fichas', 'Juegos · 4 fichas'), ('mando', 'Juegos · mando'), ('farola', 'Avance · farola'), ('barras', 'Avance · barras que suben'), ('brote', 'Avance · brote')]


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def shot(name): return Image.open(os.path.join(SHOTS, name + '.png')).convert('RGB')


def lum(a):
    c = np.asarray(a, dtype=float) / 255.0
    c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    return 0.2126 * c[..., 0] + 0.7152 * c[..., 1] + 0.0722 * c[..., 2]


def ratio(l1, l2):
    hi, lo = np.maximum(l1, l2), np.minimum(l1, l2); return (hi + 0.05) / (lo + 0.05)


def coma(x, d=1): return ('%.*f' % (d, x)).replace('.', ',')


# ------------------------------------------------------------------ pegar la barra sobre la toma real de Hoy
def registra(page, bar):
    """Dónde cae la barra en la toma de la página: la barra va sobre los 48 dp de la barra del sistema; se busca el desfase de menor diferencia (y esa diferencia, para comprobar que la maqueta es la barra real)."""
    base = page.height - round(48 * PPD) - bar.height
    p = np.asarray(page, dtype=float); b = np.asarray(bar, dtype=float); best = None
    for y in range(base - 8, base + 9):
        d = float(np.abs(p[y:y + bar.height, 200:880] - b[:, 200:880]).mean())
        if best is None or d < best[0]: best = (d, y)
    return best[1], best[0]


def pegada(page, bar, arriba=150, abajo=60):
    y, _ = registra(page, bar)
    im = page.copy(); im.paste(bar, (0, y))
    return im.crop((0, y - arriba, page.width, min(page.height, y + bar.height + abajo)))


# ------------------------------------------------------------------ medidas sobre los píxeles de la barra
def centro(i): return int(round(360 * i + 180)), int(round((7 + 24) * PPD))      # un tercio de ancho cada pestaña; 7 dp de margen arriba y un disco de 48 dp


def ventana(im, i, r_dp):
    cx, cy = centro(i); r = int(round(r_dp * PPD))
    a = np.asarray(im.crop((cx - r, cy - r, cx + r + 1, cy + r + 1)), dtype=float)
    yy, xx = np.mgrid[-r:r + 1, -r:r + 1]; return a, (xx ** 2 + yy ** 2) <= r * r


def fondo(im, i, r0_dp, r1_dp):
    """El color de fondo del ícono: la mediana de un anillo alrededor (el crema del disco, el tono de la barra, el disco noche o la losa sol)."""
    cx, cy = centro(i); r1 = int(round(r1_dp * PPD)); a = np.asarray(im.crop((cx - r1, cy - r1, cx + r1 + 1, cy + r1 + 1)), dtype=float)
    yy, xx = np.mgrid[-r1:r1 + 1, -r1:r1 + 1]; d = np.sqrt(xx ** 2 + yy ** 2); ring = (d >= r0_dp * PPD) & (d <= r1)
    return np.median(a[ring], axis=0)


def borde(mask, k=3):
    """Los píxeles del dibujo pegados a su orilla (a menos de k px del fondo): ahí se decide si la forma está delimitada."""
    from numpy.lib.stride_tricks import sliding_window_view
    pad = np.pad(mask, k, constant_values=False); w = sliding_window_view(pad, (2 * k + 1, 2 * k + 1)).min(axis=(2, 3))
    return mask & ~w


def mide_icono(im, i, estilo):
    sin_disco = estilo == 'e1-sin-disco'
    r_win = 23 if sin_disco else 21; ring = (25, 27.5) if sin_disco else (21.5, 23)
    bg = fondo(im, i, *ring); a, circ = ventana(im, i, r_win)
    dist = np.sqrt(((a - bg) ** 2).sum(axis=2)); mask = (dist >= 45) & circ
    lbg = float(lum(bg[None, None, :])[0, 0]); L = lum(a); R = ratio(L, lbg)
    ys, xs = np.where(mask); alto = (ys.max() - ys.min() + 1) / PPD; ancho = (xs.max() - xs.min() + 1) / PPD
    edge = borde(mask); inner = mask & ~edge
    fx = np.asarray(Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(SIGMA)), dtype=float)
    Lb = lum(fx); Lw = Lb[circ]; extremo = np.percentile(Lw, 2) if lbg > 0.3 else np.percentile(Lw, 98)
    pct = lambda r, q: float(np.percentile(r, q))
    return {
        'ancho': ancho, 'alto': alto, 'lado': max(ancho, alto),
        'borde_med': float(np.median(R[edge])), 'borde_3': float((R[edge] >= 3).mean()),
        'cuerpo_med': float(np.median(R[inner])) if inner.any() else float('nan'), 'cuerpo_3': float((R[inner] >= 3).mean()) if inner.any() else float('nan'),
        'blur': float(ratio(extremo, lbg)), 'blur_gris': (Lb - lbg)[circ], 'blur_color': (fx - bg)[circ].ravel()
    }


def ncc(u, v):
    u = u - u.mean(); v = v - v.mean(); return float((u * v).sum() / (np.sqrt((u * u).sum() * (v * v).sum()) + 1e-9))


def mide_estilo(estilo):
    """Las medidas de un estilo: de cada ícono, sin elegir (la barra con otra pestaña elegida) y elegido (sobre la losa sol); y cuánto se parecen entre sí las tres formas tras el desenfoque (1 = la misma mancha)."""
    off = {}; sel = {}
    for i, (key, _) in enumerate(TABS):
        sel[key] = mide_icono(shot('barra-%s-%s' % (estilo, key)), i, estilo)
        off[key] = mide_icono(shot('barra-%s-%s' % (estilo, 'juegos' if key == 'hoy' else 'hoy')), i, estilo)
    pares = ((0, 1), (0, 2), (1, 2)); ks = [k for k, _ in TABS]
    gris = max(ncc(off[ks[a]]['blur_gris'], off[ks[b]]['blur_gris']) for a, b in pares)
    color = max(ncc(off[ks[a]]['blur_color'], off[ks[b]]['blur_color']) for a, b in pares)
    todos = list(off.values()) + list(sel.values()); nz = lambda f, xs: min(x[f] for x in xs if x[f] == x[f])
    return {
        'off': off, 'sel': sel, 'parecido_gris': gris, 'parecido_color': color,
        'lado_min': min(m['lado'] for m in todos), 'lado_max': max(m['lado'] for m in todos),
        'borde_off': nz('borde_med', off.values()), 'borde_sel': nz('borde_med', sel.values()), 'borde_3': nz('borde_3', todos),
        'cuerpo_off': nz('cuerpo_med', off.values()), 'cuerpo_sel': nz('cuerpo_med', sel.values()), 'cuerpo_max': max(x['cuerpo_med'] for x in todos if x['cuerpo_med'] == x['cuerpo_med']),
        'blur_off': nz('blur', off.values()), 'blur_sel': nz('blur', sel.values())
    }


# ------------------------------------------------------------------ texto y composición
def wrap(d, text, fnt, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=fnt) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


def gris(im): return im.convert('L').convert('RGB')


def desenfoque(im): return im.filter(ImageFilter.GaussianBlur(SIGMA))


def texto_medidas(clave, m):
    """Las medidas de un estilo en dos o tres renglones; todo sale de los píxeles de las tomas."""
    L = []
    L.append('Dibujo visible: %s a %s dp de lado mayor.  Contraste del contorno contra su fondo: sin elegir %s:1, ELEGIDA sobre la losa sol %s:1 (la mediana de los píxeles de la orilla; %d %% de ellos llega a 3:1 o más).' % (
        coma(m['lado_min']), coma(m['lado_max']), coma(m['borde_off']), coma(m['borde_sel']), round(100 * m['borde_3'])))
    if clave == 'e3-dos-tonos':
        L.append('El relleno de color NO carga la forma (%s a %s:1 contra el crema): la carga el contorno de tinta; el color sirve para distinguir una pestaña de otra.' % (coma(min(m['cuerpo_off'], m['cuerpo_sel'])), coma(m['cuerpo_max'])))
    elif clave == 'actual':
        L.append('El cuerpo oscuro contra el crema da %s:1, pero su ORILLA es blanda (sombra y bordes difusos): ahí está la forma «poco delimitada».' % coma(m['cuerpo_off']))
    L.append('Tras el desenfoque (radio de 5 px) lo que queda de la mancha contra su fondo: %s:1.  Parecido entre las tres formas tras el desenfoque (1 = la misma mancha): %s en grises, %s en color.' % (
        coma(min(m['blur_off'], m['blur_sel'])), coma(m['parecido_gris'], 2), coma(m['parecido_color'], 2)))
    ok = m['borde_off'] >= 3 and m['borde_sel'] >= 3
    L.append(('CUMPLE el contraste ≥ 3:1 del contorno en las dos situaciones.' if ok else 'NO CUMPLE el contraste ≥ 3:1: ' + ('el contorno sale blando.' if clave == 'actual' else 'elegida sobre la losa sol queda en %s:1.' % coma(m['borde_sel']) if m['borde_sel'] < 3 and m['borde_off'] >= 3 else 'queda en %s:1.' % coma(min(m['borde_off'], m['borde_sel'])))))
    return L


def main():
    page10 = Image.open(os.path.join(PREV, 'app-actual', 'hoy.png')).convert('RGB'); page13 = Image.open(os.path.join(PREV, 'app-actual', 'hoy-letra13.png')).convert('RGB')
    _, d10 = registra(page10, shot('barra-actual-hoy')); _, d13 = registra(page13, shot('barra-actual-hoy-letra13'))
    print('maqueta HOY contra la toma real: diferencia media %.3f (letra 1,0) y %.3f (letra 1,3) sobre 255' % (d10, d13))
    M = {}
    for clave in [e[0] for e in ESTILOS] + ['e1-disco']:
        M[clave] = m = mide_estilo(clave)
        print('%-13s lado %.1f-%.1f dp | contorno sin elegir %.1f:1, elegida %.1f:1 (%.0f %% >= 3:1) | cuerpo %.1f/%.1f:1 | desenfoque %.1f/%.1f:1 | parecido gris %.2f color %.2f' % (
            clave, m['lado_min'], m['lado_max'], m['borde_off'], m['borde_sel'], 100 * m['borde_3'], m['cuerpo_off'], m['cuerpo_sel'], m['blur_off'], m['blur_sel'], m['parecido_gris'], m['parecido_color']))
        for k, v in list(m['off'].items()) + [(k + ' (elegida)', v) for k, v in m['sel'].items()]:
            print('     %-16s %.1f x %.1f dp  contorno %.1f:1  cuerpo %.1f:1  desenfoque %.1f:1' % (k, v['ancho'], v['alto'], v['borde_med'], v['cuerpo_med'], v['blur']))
    return M, page10, page13, d10, d13


def rotulo(d, x, y, texto, fnt):
    """Una etiqueta chica sobre una toma (con su fondo, para que no se pierda sobre lo que hay debajo)."""
    w = d.textlength(texto, font=fnt); d.rectangle((x, y, x + w + 16, y + 32), fill=(20, 18, 63)); d.text((x + 8, y + 2), texto, font=fnt, fill=DIM)


FILAS_TABLA = [('actual', 'HOY · Tarea 156', 'orilla blanda'), ('e1-disco', 'E1 con disco crema', 'claros sobre crema'), ('e1-sin-disco', 'E1 sin disco', 'elegida sobre sol'),
               ('e1-noche', 'E1b ventanilla noche', ''), ('e2-linea', 'E2 línea fina', ''), ('e3-dos-tonos', 'E3 dos tonos', '')]

FAVORITO = [
    ('Mi favorito: E3 «Dos tonos».', True),
    ('1) Se reconoce a tamaño real: la isla verde con su banderita, el cohete lila y la farola de vidrio amarillo se distinguen por forma Y por color, y siguen distinguiéndose entrecerrando los ojos: el color sobrevive al desenfoque; el churro oscuro de hoy se vuelve la misma mancha (parecido 0,94 contra 0,66).', False),
    ('2) La forma queda delimitada: el contorno de tinta da 14,7:1 contra el crema y 14,7:1 también con la losa sol detrás (hoy: 2,2:1 en la orilla). El relleno de color es claro (1,3 a 1,8:1) y no carga la forma, así que nunca depende de él.', False),
    ('3) Habla el idioma del kit (tinta y los colores de las losas), es vector (nítido a cualquier tamaño y letra, y se retoca sin volver a renderizar en Blender) y cabe en el disco de 48 dp con 30-34 dp de dibujo. Si Fable cambia los nombres o las metáforas de las pestañas, el estilo se queda y solo se dibuja otro objeto (el bloque de arriba ya trae seis).', False),
    ('Lo que E2 «Línea fina» hace mejor: separa más las tres formas tras el desenfoque (0,53 contra 0,66). Pero sin color vuelve a ser un ícono oscuro y delgado, justo lo que Ricardo describió; queda como alternativa sobria. E1 «Objetos de color» solo cumple dentro de una ventanilla noche (5,5 a 9,8:1) y entonces es una miniatura con mucho detalle, no un ícono; sin ella, el objeto elegido sobre la losa sol cae a 1,4:1.', False),
    ('Lo que falta si se aprueba E3: pasar las rutas de NavIconosPreview.kt a la app como ImageVector, cambiar KitGlyph por ellos en NeuroNavBar y retirar kit_i_isla, kit_i_cohete y kit_i_farola. Nada de eso está hecho: la barra real sigue como en la Tarea 156.', False),
]


def lamina(M, page10, page13, d10, d13):
    W = 2280; MG = 40; COLW = 1080; X2 = MG + COLW + MG
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    F_T = font(66); F_H = font(44); F_P = font(28, False); F_S = font(26, False); F_C = font(24, False); F_M = font(27, False); F_TB = font(27, False); F_TBB = font(27)
    intro = [
        'Ricardo probó la barra de la Tarea 156 en su teléfono: «no se reconoce el cohete ni la farola; se ve algo gris oscuro, con una forma poco delimitada». Lo que mido en la fila HOY lo confirma: el cuerpo es oscuro (alto contraste), pero la orilla es blanda y, tras un desenfoque, las tres formas se vuelven la misma mancha.',
        'Cada fila es la MISMA barra (tercios, disco de 48 dp, rótulo de 16 sp, losa sol de la elegida) con otro estilo de ícono y las mismas metáforas: isla con banderita, cohete y farola. Las tomas son capturas reales de Roborazzi a 393 dp de ancho y 2,75 px por dp (≈ 420-440 ppp), de la barra de cada estilo pegada sobre la toma real de Hoy (en HOY la maqueta coincide con la app: diferencia media %s y %s sobre 255).' % (coma(d10, 2), coma(d13, 2)),
        'Lectura: en cada fila, a la izquierda la barra a letra 1,0 con Hoy elegida y, debajo, con desenfoque gaussiano (radio de 5 px = 1,8 dp: la prueba de entrecerrar los ojos); a la derecha la barra a letra 1,3 y, debajo, en escala de grises. TODO va a tamaño real: 1 px de esta lámina = 1 px del teléfono (a 100 % de zoom).',
        'Reglas que se miden: dibujo ≥ 28 dp visibles, discos y toques ≥ 48 dp, rótulo de 16 sp, la elegida sobre la losa sol (nunca solo por color), contraste del ícono contra su fondo ≥ 3:1, sin estrellas con puntas, medialunas ni cruces y sin caras.'
    ]
    intro_l = [wrap(probe, t, F_P, W - 2 * MG) for t in intro]
    sets = []
    for clave, titulo, desc, col in ESTILOS:
        f10 = pegada(page10, shot('barra-%s-hoy' % clave)); f13 = pegada(page13, shot('barra-%s-hoy-letra13' % clave))
        bar = shot('barra-%s-hoy' % clave)
        sets.append((clave, titulo, desc, col, f10, f13, desenfoque(bar), gris(bar), wrap(probe, desc, F_S, W - 2 * MG), [wrap(probe, t, F_M, W - 2 * MG) for t in texto_medidas(clave, M[clave])]))
    tile = shot('tile-%s-cohete-off' % ESTILO_BLOQUE); th = tile.height
    fav_l = [(wrap(probe, t, F_P if not b else F_H, W - 2 * MG), b) for t, b in FAVORITO]
    TB_ROWS = len(FILAS_TABLA)
    H = 40 + 90 + sum(30 * len(l) + 12 for l in intro_l) + 30 + (62 + 46 * (TB_ROWS + 1)) + 50
    for s_ in sets:
        H += 70 + 32 * len(s_[8]) + 12 + max(s_[4].height, s_[5].height) + 14 + max(s_[6].height, s_[7].height) + 24 + sum(34 * len(l) for l in s_[9]) + 50
    H += 62 + 32 * 5 + 16 + 30 + 4 * (th + 40) + 40
    H += sum((54 if b else 30 * len(l) + 14) for l, b in fav_l) + 100
    sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet); y = 40
    d.text((MG, y), 'Íconos de la barra que se reconozcan (Tarea 157)', font=F_T, fill=SUN); y += 90
    for lines in intro_l:
        for ln in lines: d.text((MG, y), ln, font=F_P, fill=CREAM); y += 30
        y += 12
    y += 30
    # --- tabla resumen
    d.text((MG, y), 'Resumen de lo medido (de los píxeles de las tomas; el peor caso de las tres pestañas)', font=F_H, fill=SUN); y += 62
    cols_x = [MG, MG + 440, MG + 650, MG + 900, MG + 1130, MG + 1370, MG + 1650]
    for x, t in zip(cols_x, ['Estilo', 'Dibujo (dp)', 'Contorno sin elegir', 'Contorno elegida', 'Tras desenfoque', 'Parecido gris / color', 'Contraste 3:1 o más']):
        d.text((x, y), t, font=F_TBB, fill=SUN)
    y += 46
    for n, (clave, nombre, razon) in enumerate(FILAS_TABLA):
        m = M[clave]; ok = m['borde_off'] >= 3 and m['borde_sel'] >= 3
        d.rectangle((MG - 8, y - 4, W - MG + 8, y + 38), fill=(30, 28, 82) if n % 2 == 0 else NAVY)
        vals = [nombre, '%s a %s' % (coma(m['lado_min'], 0), coma(m['lado_max'], 0)), coma(m['borde_off']) + ':1', coma(m['borde_sel']) + ':1', coma(min(m['blur_off'], m['blur_sel'])) + ':1',
                '%s / %s' % (coma(m['parecido_gris'], 2), coma(m['parecido_color'], 2)), ('CUMPLE' if ok else 'NO CUMPLE' + (' (' + razon + ')' if razon else ''))]
        for x, t in zip(cols_x, vals): d.text((x, y), t, font=F_TB, fill=(LIME if (ok and x == cols_x[-1]) else (CORAL if (not ok and x == cols_x[-1]) else CREAM)))
        y += 46
    y += 50
    for clave, titulo, desc, col, f10, f13, bl, gr, dl, ml in sets:
        d.text((MG, y), titulo, font=F_H, fill=col); y += 70
        for ln in dl: d.text((MG, y), ln, font=F_S, fill=DIM); y += 32
        y += 12; top = y
        sheet.paste(f10, (MG, top)); sheet.paste(f13, (X2, top))
        for im, x in ((f10, MG), (f13, X2)): d.rectangle((x, top, x + im.width - 1, top + im.height - 1), outline=(70, 74, 130), width=1)
        rotulo(d, MG, top, 'letra 1,0 · toma real', F_C); rotulo(d, X2, top, 'letra 1,3 · toma real', F_C)
        y = top + max(f10.height, f13.height) + 14
        sheet.paste(bl, (MG, y)); sheet.paste(gr, (X2, y))
        for im, x in ((bl, MG), (gr, X2)): d.rectangle((x, y, x + im.width - 1, y + im.height - 1), outline=(70, 74, 130), width=1)
        rotulo(d, MG, y, 'desenfoque gaussiano, radio 5 px', F_C); rotulo(d, X2, y, 'escala de grises', F_C)
        y += max(bl.height, gr.height) + 24
        for lines in ml:
            for ln in lines: d.text((MG, y), ln, font=F_M, fill=CREAM); y += 34
        y += 50
    # --- metáforas
    est = [e for e in ESTILOS if e[0] == ESTILO_BLOQUE][0]
    d.text((MG, y), 'Metáforas posibles en «%s»' % est[1].split(' · ')[1], font=F_H, fill=SUN); y += 62
    for ln in wrap(probe, 'Juegos: cohete, cuadrícula de 4 fichas, mando de juego. Avance: farola, barras que suben, brote que crece. Cada una en su pestaña (rótulo y losa sol de la elegida), a tamaño real; abajo, con desenfoque (radio 5 px) y en grises. Ninguna lleva estrellas con puntas, medialunas, cruces ni caras (el mando usa un stick y dos botones redondos, no una cruz).', F_S, W - 2 * MG):
        d.text((MG, y), ln, font=F_S, fill=DIM); y += 32
    y += 16
    cols = len(METAFORAS); gap = (W - 2 * MG - cols * 360) // (cols - 1)
    for k, (key, nom) in enumerate(METAFORAS): d.text((MG + k * (360 + gap), y), nom, font=F_C, fill=CREAM)
    y += 30
    for sub, lab in (('off', 'sin elegir'), ('sel', 'elegida'), ('blur', 'desenfoque'), ('gris', 'grises')):
        for k, (key, nom) in enumerate(METAFORAS):
            t = shot('tile-%s-%s-%s' % (ESTILO_BLOQUE, key, 'sel' if sub in ('sel', 'blur') else 'off'))
            if sub == 'blur': t = desenfoque(t)
            if sub == 'gris': t = gris(t)
            x = MG + k * (360 + gap); sheet.paste(t, (x, y)); d.rectangle((x, y, x + 359, y + t.height - 1), outline=(70, 74, 130), width=1)
        y += th + 8; d.text((MG, y - 2), lab, font=F_C, fill=SUN); y += 32
    y += 40
    for lines, b in fav_l:
        if b: d.text((MG, y), lines[0], font=F_H, fill=LIME); y += 54
        else:
            for ln in lines: d.text((MG, y), ln, font=F_P, fill=CREAM); y += 30
            y += 14
    sheet = sheet.crop((0, 0, W, y + 30))
    sheet.save(OUT); print('ok', OUT, sheet.size)


if __name__ == '__main__':
    M, page10, page13, d10, d13 = main()
    lamina(M, page10, page13, d10, d13)
