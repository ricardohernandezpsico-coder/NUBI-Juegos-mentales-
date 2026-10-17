# -*- coding: utf-8 -*-
"""Diagnóstico de los BOTONES (Tarea 116): por qué «Arrancar» y la cuenta de llaves de Engranajes no se sienten como VERDAD / DISPARATE, el tablero de Rescate o «Continuar» de la pausa, que Ricardo aprobó.
Pone los botones lado a lado AL 100 % y A LA MISMA ESCALA (1 dp = 3 px) y los mide en dp:
  · LABIO: la pared de abajo del botón (la banda más oscura que la cara, entre la cara y la sombra); un nine-slice que adelgaza el bisel la achica;
  · BRILLO: el pico de luz del filo de arriba contra la cara (razón de luminancia) y cuántos dp mide;
  · TEXTURA visible: el contraste local (RMS de la luminancia menos su desenfoque de 4 px) en la cara;
  · SOMBRA: cuántos dp se corre hacia abajo y cuánto se desenfoca (de la parte translúcida de la imagen, no de la pantalla);
  · SATURACIÓN y luminancia de la cara; y el RELIEVE del texto y del ícono (el halo claro u oscuro que lo rodea, contra la cara).
Las losas del kit se reconstruyen del atlas `comun` con su nine-slice (lo que Unity hace con `Image.Type.Sliced`; réplica de `propuesta_util.losa_kit`) al tamaño REAL de cada botón, en reposo y presionadas; los botones «de verdad» salen de capturas reales del juego (`unity/test-results/capturas/<juego>/`).
Uso: python -I tools/juegos/diagnostico_botones.py [--despues <carpeta de capturas de Engranajes>]   → docs/previews/plasticina/diagnostico-botones.png y la tabla en consola."""
import os, sys, json
import numpy as np
from PIL import Image, ImageDraw, ImageFont
AQUI = os.path.dirname(os.path.abspath(__file__)); REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
sys.path.insert(0, AQUI); sys.stdout.reconfigure(encoding='utf-8')
from propuesta_util import losa_kit, blur, lum, rgb_a_hsv, edt

CAPT = os.path.join(REPO, 'unity', 'test-results', 'capturas')
FONTS = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Fonts')
SALIDA = os.path.join(REPO, 'docs', 'previews', 'plasticina', 'diagnostico-botones.png')
DP = 3.0                                                         # píxeles por dp

def fuente(n, px): return ImageFont.truetype(os.path.join(FONTS, n), px)
TIT = fuente('Fredoka-Bold.ttf', 36); SUB = fuente('Fredoka-SemiBold.ttf', 24); ETQ = fuente('Fredoka-SemiBold.ttf', 20); TXT = fuente('AtkinsonHyperlegible-Bold.ttf', 19); MONO = fuente('AtkinsonHyperlegible-Bold.ttf', 17)
FONDO = (16, 16, 24); BLANCO = (240, 238, 250); GRIS = (168, 164, 196); LIMA = (166, 227, 107); CORAL = (255, 128, 112)

# ------------------------------------------------------------------ los botones de referencia: tono del kit y tamaño real del cuerpo (px: 3 por dp)
REF = [                                                          # (nombre, losa del kit, cuerpo ancho × alto en px, de dónde)
    ('VERDAD (¿Verdad o disparate?)', 'lima', (459, 304), 'disparate'),
    ('DISPARATE (¿Verdad o disparate?)', 'coral', (459, 304), 'disparate'),
    ('Casilla del tablero de Rescate', 'noche', (230, 168), 'radar'),
    ('«¡Rescatar!» de Rescate', 'sol', (450, 120), 'radar'),
    ('«Continuar» de la pausa', 'sol', (634, 146), 'pausa'),
    ('«Reiniciar» de la pausa (el MISMO cielo de Arrancar)', 'cielo', (634, 146), 'pausa'),
    ('«Arrancar» de Engranajes', 'cielo', (647, 303), 'engranajes'),
    ('Cuenta de llaves de Engranajes', 'noche', (312, 305), 'engranajes'),
]

def replica(tono, cuerpo, presionada=False):
    """La losa del kit [tono] (md) al tamaño real del cuerpo, con el nine-slice de Unity: (RGBA premultiplicado, (ml, mt), (ancho cuerpo, alto cuerpo))."""
    nombre = 'losa_md_' + tono + ('_p' if presionada else '')
    im, (ml, mt) = losa_kit(nombre, cuerpo[0], cuerpo[1])
    return im, (ml, mt), cuerpo

def rgb_de(rgba):
    a = rgba[..., 3:4]; return np.where(a > 1e-4, rgba[..., :3] / np.maximum(a, 1e-4), 0.0)

def mide_losa(rgba, ml, mt, cuerpo):
    """Las medidas de una losa reconstruida (sobre su alfa y su color limpios). Todo en dp salvo lo que se dice."""
    w, h = cuerpo; a = rgba[..., 3]; rgb = rgb_de(rgba); L = lum(rgb)
    cx = int(ml + w * 0.25); cols = slice(cx - 4, cx + 5)                         # una columna a un cuarto del ancho: sin texto ni ícono
    col_a = a[:, cols].mean(axis=1); col_L = L[:, cols].mean(axis=1)
    cara_y0, cara_y1 = int(mt + h * 0.40), int(mt + h * 0.58)
    Lf = float(np.median(col_L[cara_y0:cara_y1]))
    # el brillo: el pico de luz en los 12 dp de arriba del cuerpo contra la cara (y cuánto dura por encima de la cara + 1 %)
    top = col_L[mt:mt + int(12 * DP)]; pico = float(top.max()) / Lf; alto_brillo = float((top > Lf * 1.012).sum()) / DP
    # la parte opaca: la última fila con alfa ≥ 0,97 es el pie del labio
    opacas = np.where(col_a >= 0.97)[0]; pie = int(opacas.max())
    y = pie
    while y > cara_y1 and col_L[y] < Lf * 0.975: y -= 1                            # el labio: de la cara hacia abajo, lo que está más oscuro que ella
    labio = (pie - y) / DP
    # la sombra: lo translúcido bajo el pie: su extensión (alfa > 2 %) y su centro de masa, y el desenfoque (del 90 % al 10 % del pico)
    bajo = col_a[pie + 1:]; ext = float((bajo > 0.02).sum()) / DP
    if bajo.sum() > 1e-6:
        ys = np.arange(bajo.size); centro = float((ys * bajo).sum() / bajo.sum()) / DP
        pk = bajo.max(); desenfoque = float(((bajo > 0.1 * pk) & (bajo < 0.9 * pk)).sum()) / DP
    else:
        centro = 0.0; desenfoque = 0.0
    # la textura: contraste local en la cara (ventana central, lejos de los filos)
    v = L[mt + int(h * 0.30):mt + int(h * 0.70), ml + int(w * 0.20):ml + int(w * 0.80)]
    hp = v - blur(v, 4.0); textura = float(hp[6:-6, 6:-6].std())
    hsv = rgb_a_hsv(rgb[mt + int(h * 0.30):mt + int(h * 0.70), ml + int(w * 0.20):ml + int(w * 0.80)])
    return dict(cuerpo=(w / DP, h / DP), labio=labio, brillo_pico=pico, brillo_dp=alto_brillo, textura=textura, sombra_desp=centro, sombra_ext=ext, sombra_desenf=desenfoque, sat=float(hsv[..., 1].mean()), lum=Lf)

# ------------------------------------------------------------------ las capturas reales
def carga(path): return np.asarray(Image.open(path).convert('RGB')).astype(np.float64) / 255.0

def recorte(img, caja): x0, y0, x1, y1 = caja; return img[y0:y1, x0:x1]

def mide_real(crop, ancla):
    """Medidas sobre un recorte REAL (lo que ve el jugador). [ancla] = (x, y0, y1): una columna sin texto y el rango de filas donde está el botón. Devuelve el labio (la pared más oscura bajo la cara, hasta el filo de la losa) y la banda MUY oscura que haya debajo (L < 0,35 de la cara) en dp."""
    x, y0, y1 = ancla; cols = slice(x - 3, x + 4)
    L = lum(crop[:, cols].mean(axis=1)); H = rgb_a_hsv(crop[:, cols].mean(axis=1))
    n = L.size; ys = np.arange(n)
    Lf = float(np.median(L[(ys >= y0 + (y1 - y0) * 0.35) & (ys <= y0 + (y1 - y0) * 0.55)]))
    arriba = L[y0:y0 + int(12 * DP)]; pico = float(arriba.max() / Lf)
    # la cara termina cuando la luz baja por debajo de 0,975 de la cara después de su filo de abajo
    y = int(y0 + (y1 - y0) * 0.55)
    while y < y1 and L[y] > Lf * 1.02 or (y < y1 and L[y] >= Lf * 0.975): y += 1
    ini = y
    while y < y1 and L[y] >= Lf * 0.35: y += 1                                       # el labio: hasta que cae a la sombra
    labio = (y - ini) / DP; ini2 = y
    while y < len(L) and L[y] < Lf * 0.35: y += 1                                    # la banda muy oscura (la sombra dura, si hay)
    oscura = (y - ini2) / DP
    sat = float(H[int(y0 + (y1 - y0) * 0.35):int(y0 + (y1 - y0) * 0.55), 1].mean())
    return dict(labio=labio, oscura=oscura, brillo_pico=pico, sat=sat, lum=Lf)

def textura_real(crop, ventana):
    x0, y0, x1, y1 = ventana; v = lum(crop[y0:y1, x0:x1]); hp = v - blur(v, 4.0); return float(hp[6:-6, 6:-6].std())

def relieve_glifo(crop, ventana, oscuro=0.30):
    """El RELIEVE de lo que hay dentro de [ventana] (un texto o un ícono oscuros sobre la cara): el halo de 3 px que lo rodea contra la cara, separado en lo de abajo (sombra o brillo del canto) y lo de arriba. Devuelve (abajo, arriba) en luminancia (positivo = más claro que la cara)."""
    x0, y0, x1, y1 = ventana; x0 -= 14; y0 -= 14; x1 += 14; y1 += 14                  # un margen para que el halo no se corte con la ventana
    w = crop[y0:y1, x0:x1]; L = lum(w); cara = float(np.median(L))
    g = L < oscuro
    if g.sum() < 20: return 0.0, 0.0
    d = edt(~g)                                                                          # distancia al glifo (px)
    baja = np.zeros_like(g); baja[3:] = g[:-3]; sube = np.zeros_like(g); sube[:-3] = g[3:]
    abajo = baja & ~g & (d > 2.0); arriba = sube & ~g & (d > 2.0)                       # lo que está 3 px debajo del glifo (la sombra o el brillo del canto) y 3 px encima
    f = lambda m: (float(L[m].mean()) - cara) / max(cara, 1e-6) if m.sum() > 5 else 0.0
    return f(abajo), f(arriba)

# ------------------------------------------------------------------ los botones reales: dónde están en cada captura (1080 × 2400)
# (nombre corto, marco, columna sin texto, fila de arriba y de abajo del cuerpo, caja del recorte, ventana del texto, ventana del ícono, ventana de la cara para la textura)
REAL = [
    ('VERDAD', 'disparate', 120, 2024, 2328, (30, 1990, 550, 2372), (170, 2205, 410, 2265), (250, 2090, 350, 2170), (140, 2050, 440, 2085)),
    ('DISPARATE', 'disparate', 620, 2023, 2329, (530, 1990, 1050, 2372), (625, 2205, 955, 2265), (720, 2090, 860, 2170), (640, 2050, 940, 2085)),
    ('Continuar', 'pausa', 300, 975, 1124, (200, 950, 880, 1160), (405, 1030, 675, 1080), None, (260, 985, 820, 1010)),
    ('Reiniciar (cielo)', 'pausa', 300, 1165, 1314, (200, 1140, 880, 1350), (425, 1220, 655, 1265), None, (260, 1175, 820, 1200)),
    ('Arrancar', 'engranajes', 470, 1727, 2031, (370, 1700, 1050, 2080), (655, 1860, 930, 1912), (560, 1855, 610, 1910), (400, 1765, 1000, 1800)),
]

def marcos(antes):
    return {'disparate': carga(os.path.join(CAPT, 'disparate', '03-t08.png')), 'pausa': carga(os.path.join(CAPT, 'engranajes', '05-pausa.png')),
            'engranajes': carga(os.path.join(antes, '07-rep.png')) if antes else carga(os.path.join(CAPT, 'engranajes', '04-t20.png'))}

def mide_frame(F, x, y0, y1, ventana_tex, win_txt, win_ico):
    """Las medidas de un botón real sobre su captura: labio, banda muy oscura debajo, brillo, saturación, textura, y el relieve del texto y del ícono."""
    col = F[:, x - 3:x + 4].mean(axis=1); L = lum(col); H = rgb_a_hsv(col)
    n = y1 - y0 + 1; Lf = float(np.median(L[y0 + int(n * 0.35):y0 + int(n * 0.55)]))
    pico = float(L[y0:y0 + int(12 * DP)].max() / Lf)
    y = y0 + int(n * 0.55)
    while y < y1 and L[y] >= Lf * 0.975: y += 1
    ini = y
    while y <= y1 and L[y] >= Lf * 0.35: y += 1
    labio = (y - ini) / DP
    Hh = rgb_a_hsv(col); filas = range(y1 + 1, min(F.shape[0], y1 + 1 + int(10 * DP)))                  # las 10 dp que siguen al cuerpo
    k = sum(1 for r in filas if Hh[r, 2] < 0.40 and Hh[r, 1] > 0.70 and 0.47 < Hh[r, 0] < 0.62)       # filas de canto oscuro y saturado (cian profundo): la sombra del kit es gris y poco saturada
    oscura = k / DP
    sat = float(H[y0 + int(n * 0.35):y0 + int(n * 0.55), 1].mean())
    x0, ya, x1, yb2 = ventana_tex; v = lum(F[ya:yb2, x0:x1]); hp = v - blur(v, 4.0); tex = float(hp[6:-6, 6:-6].std())
    rt = relieve_glifo(F, win_txt); ri = relieve_glifo(F, win_ico) if win_ico else (None, None)
    return dict(labio=labio, oscura=oscura, pico=pico, sat=sat, lum=Lf, tex=tex, rel_txt=rt, rel_ico=ri, alto=n / DP)

def fila_texto(nombre, d, r=None):
    f = lambda v: '—' if (v is None or v[0] is None) else '%+.2f/%+.2f' % v
    return '%-20s labio %.1f dp | banda muy oscura abajo %.1f dp | brillo %.3f× | textura %.4f | S %.2f L %.2f | relieve texto (abajo/arriba) %s | ícono %s' % (nombre, d['labio'], d['oscura'], d['pico'], d['tex'], d['sat'], d['lum'], f(d['rel_txt']), f(d['rel_ico']))

# ------------------------------------------------------------------ la hoja
def pil(a): return Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8))

def replica_rgb(tono, cuerpo, presionada=False, fondo=(16, 16, 40)):
    im, (ml, mt), c = replica(tono, cuerpo, presionada)
    bg = np.zeros(im.shape[:2] + (3,)) + np.array(fondo)[None, None, :] / 255.0
    return pil(bg * (1 - im[..., 3:4]) + im[..., :3])

def envuelve(texto, f, ancho):
    palabras = texto.split(' '); lineas = []; actual = ''
    for p in palabras:
        prueba = (actual + ' ' + p).strip()
        if f.getlength(prueba) <= ancho: actual = prueba
        else: lineas.append(actual); actual = p
    if actual: lineas.append(actual)
    return lineas

W = 1500; M = 30; GAP = 24

def construye_hoja(antes, despues, conclusion):
    Mm = marcos(antes)
    mid = {}
    for nombre, marco, x, y0, y1, caja, wt, wi, wx in REAL: mid[nombre] = mide_frame(Mm[marco], x, y0, y1, wx, wt, wi)
    if despues:                                                                                          # la captura de DESPUÉS (Tarea 116): el mismo botón, medido igual (la ventana del ícono es más ancha: el triángulo de plasticina trae su sombra)
        mid['Arrancar DESPUÉS'] = mide_frame(carga(os.path.join(despues, 'rep.png')), 470, 1727, 2031, (400, 1765, 1000, 1800), (655, 1860, 930, 1912), (535, 1845, 640, 1925))
    mk = {}
    for nombre, tono, cuerpo, donde in REF:
        im, (ml, mt), c = replica(tono, cuerpo); im_p, _, _ = replica(tono, cuerpo, True)
        mk[nombre] = (mide_losa(im, ml, mt, c), mide_losa(im_p, ml, mt, c))
    secciones = []
    def tit(hj, dr, y):
        dr.text((M, y + 4), 'Diagnóstico de botones: Engranajes contra los botones aprobados', font=TIT, fill=BLANCO)
        dr.text((M, y + 50), 'Todo al 100 % y a la misma escala (1 dp = 3 px). Las losas del kit se reconstruyen del atlas al tamaño real de cada botón; los «hoy» son capturas reales de Unity.', font=TXT, fill=GRIS)
    secciones.append((86, tit))
    ren = []
    for t in conclusion: ren += envuelve(t, TXT, W - 2 * M) + ['']
    secciones.append((len(ren) * 26 + 10, lambda hj, dr, y: [dr.text((M, y + i * 26), l, font=TXT, fill=BLANCO) for i, l in enumerate(ren)]))
    cols = [('Botón', 0), ('cuerpo dp', 330), ('labio dp', 420), ('brillo', 495), ('textura', 575), ('sombra dp', 655), ('S', 820), ('L', 870), ('banda oscura', 925), ('relieve texto', 1040), ('relieve ícono', 1155)]
    filas = []
    nombres_real = {'VERDAD (¿Verdad o disparate?)': 'VERDAD', 'DISPARATE (¿Verdad o disparate?)': 'DISPARATE', '«Continuar» de la pausa': 'Continuar', '«Reiniciar» de la pausa (el MISMO cielo de Arrancar)': 'Reiniciar (cielo)', '«Arrancar» de Engranajes': 'Arrancar'}
    for nombre, tono, cuerpo, donde in REF:
        k, kp = mk[nombre]; r = mid.get(nombres_real.get(nombre, ''), None)
        f = lambda v: '—' if (v is None or v[0] is None) else '%+.2f' % v[0]
        filas.append((nombre.replace('«', '').replace('»', ''), ['%.0f×%.0f' % k['cuerpo'], '%.1f' % k['labio'], '%.3f×' % k['brillo_pico'], '%.4f' % k['textura'], '%.1f / %.1f' % (k['sombra_desp'], k['sombra_desenf']), '%.2f' % k['sat'], '%.2f' % k['lum'],
                                                              ('%.1f dp' % r['oscura']) if r else '—', f(r['rel_txt']) if r else '—', f(r['rel_ico']) if r else '—'], nombres_real.get(nombre) == 'Arrancar'))
        if nombres_real.get(nombre) == 'Arrancar' and despues:
            r2 = mid['Arrancar DESPUÉS']
            filas.append(('Arrancar DESPUÉS (Tarea 116)', ['%.0f×%.0f' % k['cuerpo'], '%.1f' % k['labio'], '%.3f×' % k['brillo_pico'], '%.4f' % k['textura'], '%.1f / %.1f' % (k['sombra_desp'], k['sombra_desenf']), '%.2f' % r2['sat'], '%.2f' % r2['lum'],
                                                              '%.1f dp' % r2['oscura'], f(r2['rel_txt']), f(r2['rel_ico'])], 'despues'))
    def tabla(hj, dr, y):
        dr.text((M, y), 'MEDIDAS en dp (sombra = desplazamiento / desenfoque; labio, brillo, textura y sombra: de la losa reconstruida; banda oscura y relieves: de la captura real; relieve 0 = tinta lisa)', font=ETQ, fill=BLANCO)
        yy = y + 34
        for (t, x) in cols: dr.text((M + x, yy), t, font=MONO, fill=GRIS)
        yy += 26
        for nombre, vals, es_arr in filas:
            col = LIMA if es_arr == "despues" else CORAL if es_arr else BLANCO
            dr.text((M, yy), nombre[:44], font=MONO, fill=col)
            for (t, x), v in list(zip(cols[1:], vals)): dr.text((M + x, yy), v, font=MONO, fill=col)
            yy += 24
    secciones.append((34 + 26 + len(filas) * 24 + 20, tabla))
    def par(titulo, izq, der):
        def f(hj, dr, y):
            dr.text((M, y), titulo, font=SUB, fill=BLANCO)
            x = M; yy = y + 34
            for (rot, im) in (izq, der):
                dr.text((x, yy), rot, font=ETQ, fill=GRIS); hj.paste(im, (x, yy + 26)); x += im.size[0] + GAP
        return (34 + 26 + max(izq[1].size[1], der[1].size[1]) + 24, f)
    def real(marco, caja): x0, y0, x1, y1 = caja; return pil(Mm[marco][y0:y1, x0:x1])
    secciones.append(par('EN REPOSO (capturas reales, 100 %)', ('VERDAD', real('disparate', (30, 1990, 550, 2372))), ('DISPARATE', real('disparate', (530, 1990, 1050, 2372)))))
    secciones.append(par('EN REPOSO: el cielo del kit, en la pausa (211 dp) y en Engranajes (216 dp)', ('«Reiniciar»', real('pausa', (200, 1140, 880, 1350))), ('«Arrancar» hoy', real('engranajes', (370, 1700, 1050, 2080)))))
    secciones.append(par('EN REPOSO: la cuenta de llaves hoy y el tablero de Rescate', ('cuenta de llaves hoy', real('engranajes', (20, 1700, 360, 2080))), ('casilla y «¡Rescatar!» de Rescate', pil(carga(os.path.join(CAPT, 'radar', '04-respuesta.png'))[1700:2380, 40:800]))))
    if antes:
        secciones.append(par('PRESIONADO: «Arrancar» hoy (captura real) y la réplica del cielo del kit presionado al mismo tamaño', ('«Arrancar» hoy presionado', pil(carga(os.path.join(antes, '08-pres.png'))[1700:2080, 370:1050])), ('réplica losa_md_cielo_p (la del kit)', replica_rgb('cielo', (647, 303), True))))
    secciones.append(par('PRESIONADO: las losas del kit (réplicas del atlas al tamaño real; el cuerpo baja y se achica la sombra)', ('VERDAD presionado', replica_rgb('lima', (459, 304), True)), ('«Continuar» presionado', replica_rgb('sol', (634, 146), True))))
    if despues:
        secciones.append(par('DESPUÉS (Tarea 116): «Arrancar» en reposo y presionado (la losa del kit, texto liso y triángulo de plasticina)', ('en reposo', pil(carga(os.path.join(despues, 'rep.png'))[1700:2080, 370:1050])), ('presionado', pil(carga(os.path.join(despues, 'pres.png'))[1700:2080, 370:1050]))))
        secciones.append(par('DESPUÉS (Tarea 116): la cuenta de llaves (la losa «noche» sin teñir y llaves en moneda) y la casilla de Rescate de referencia', ('cuenta de llaves ahora', pil(carga(os.path.join(despues, 'rep.png'))[1700:2080, 20:360])), ('casilla y «¡Rescatar!» de Rescate', pil(carga(os.path.join(CAPT, 'radar', '04-respuesta.png'))[1700:2380, 40:800]))))
    alto = sum(s[0] for s in secciones) + M
    hj = Image.new('RGB', (W, alto), FONDO); dr = ImageDraw.Draw(hj); y = 0
    for hh, f in secciones:
        f(hj, dr, y); y += hh
    return hj

CONCLUSION = [
    '«Arrancar» y la cuenta de llaves salen de la MISMA losa del kit que VERDAD, DISPARATE, «Continuar» y el tablero de Rescate. El labio (3,3 dp), el brillo (1,025×), la textura y la sombra de la losa son los mismos y NO cambian con el tamaño: «Reiniciar» (211 × 49 dp, 4,3 : 1) y «Arrancar» (216 × 101 dp, 2,1 : 1) dan exactamente lo mismo, y «Continuar» (211 dp) tiene el mismo labio de 3,0 dp que «¡Rescatar!» (150 dp). La cara de «Arrancar» tiene la saturación y la luz de «Reiniciar» (S 0,61, L 0,65): no es «clara». Se DESCARTA que el estirado del nine-slice adelgace o lave el bisel o el brillo.',
    'Lo que sí cambia está encima de la losa y lo puse yo en la Tarea 115: (1) un canto de 4 dp de cian oscuro y saturado debajo de la losa, que los otros botones no tienen (su sombra del kit es gris y chica), y que se queda igual al presionarla; (2) el texto con un halo claro debajo (relieve +0,22 contra ±0,02 de los demás, que son tinta lisa); (3) un ícono plano con sombra dura (−0,29) en lugar del ícono de plasticina de churro con sombra suave de VERDAD/DISPARATE; (4) la cuenta de llaves teñida (×0,50, ×0,78, ×0,66) con monedas y llaves planas: su cara baja a L 0,12 contra 0,18-0,19 de la losa «noche» sin teñir de Rescate y pierde el brillo; y (5) falta el rebote al soltar que sí tienen Rescate y los demás (`PlasticTouch.Bounce`).']

DESPUES = ('QUÉ SE HIZO (Tarea 116): «Arrancar» queda con la losa Sky del kit tal cual: sin el canto de 4 dp (la banda oscura baja de 4,0 a 0,3 dp), sin el halo bajo el texto (el relieve pasa de +0,22 a 0,00: tinta lisa como VERDAD y «Reiniciar»), con el triángulo de plasticina de tinta y su sombra suave en el suelo (el relieve pasa de −0,29 a −0,08, entre los −0,15 y −0,12 de VERDAD y DISPARATE: el mismo tratamiento) y con el rebote al soltar; se atenúa igual cuando no se puede tocar. La cuenta de llaves es la losa «noche» del kit SIN teñir (L 0,19, la de las casillas de Rescate) con el texto liso y cada llave en una moneda de plasticina con la llave de churro (lila libre, dorada usada). Los carteles y el rótulo MOTOR son losas chicas del kit (noche, verde, granate; dorada para MOTOR cambiado) y el portón, el marco, la plataforma, la caja y la barra usan la receta de color de los engranajes. Los engranajes no se tocaron (siguen idénticos píxel a píxel en el atlas).')

if __name__ == '__main__':
    antes = os.path.join(AQUI, 'out', 'botones_antes') if os.path.isdir(os.path.join(AQUI, 'out', 'botones_antes')) else None
    despues = sys.argv[sys.argv.index('--despues') + 1] if '--despues' in sys.argv else None
    hj = construye_hoja(antes, despues, CONCLUSION + ([DESPUES] if despues else [])); print('hoja', hj.size)
    q = hj.quantize(colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.FLOYDSTEINBERG)
    os.makedirs(os.path.dirname(SALIDA), exist_ok=True); q.save(SALIDA, optimize=True); print('guardada', SALIDA, os.path.getsize(SALIDA) // 1024, 'KB')
