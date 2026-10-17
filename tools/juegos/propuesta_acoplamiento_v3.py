# -*- coding: utf-8 -*-
"""La hoja de Acoplamiento v3 (Tarea 117): SOLO PROPUESTA, no está integrada a ningún juego. Ricardo rechazó las bolitas («rellenar por rellenar») y pidió el objeto mismo bien hecho, la casilla bien definida y sacar la estación de la pantalla de juego.
Todo sale de numpy (`acopla_v3.py`, `acopla_v3_pantalla.py`) sobre las capturas REALES (`unity/test-results/capturas/acoplamiento`, `…/disparate`) y de los PNG de Blender (`acoplamiento_v3.py`).
Uso: blender -b -P tools/juegos/acoplamiento_v3.py -- --out tools/juegos/out/acoplamiento_v3   (una vez)
     python -I tools/juegos/propuesta_acoplamiento_v3.py   → docs/previews/plasticina/propuesta-acoplamiento-v3.png y las medidas en la consola"""
import os, sys, math, time
import numpy as np
from PIL import Image, ImageDraw, ImageFont
AQUI = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, AQUI); sys.stdout.reconfigure(encoding='utf-8')
import acopla_v3 as A
import acopla_v3_pantalla as S
from acopla_geo import normalize, pick_pieces, has_2x2, piece_sdf, grid, exact_coverage
from acopla_caminos import sdf_robusto
from propuesta_util import DP, over, pega, losa_kit, texto, lum

REPO = S.REPO; CAPT = S.CAPT
SALIDA = os.path.join(REPO, 'docs', 'previews', 'plasticina', 'propuesta-acoplamiento-v3.png')
FONTS = S.FONTS
def fuente(n, px): return ImageFont.truetype(os.path.join(FONTS, n), px)
TITULO = fuente('Fredoka-Bold.ttf', 42); SUB = fuente('Fredoka-SemiBold.ttf', 28); ETQ = fuente('Fredoka-SemiBold.ttf', 23); TXT = fuente('AtkinsonHyperlegible-Bold.ttf', 21); MONO = fuente('AtkinsonHyperlegible-Bold.ttf', 20)
FONDO = (16, 16, 24); BLANCO = (240, 238, 250); GRIS = (168, 164, 196); LIMA = (166, 227, 107); CORAL = (255, 128, 112); CIELO = (127, 216, 255)
W = 1920; M = 30; GAP = 24
SKY = np.array([8, 8, 28]) / 255.0

def u8(a): return Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8))
def carga(path): return S.carga(path)

def envuelve(texto, f, ancho):
    palabras = texto.split(' '); lineas = []; actual = ''
    for p in palabras:
        prueba = (actual + ' ' + p).strip()
        if f.getlength(prueba) <= ancho: actual = prueba
        else: lineas.append(actual); actual = p
    if actual: lineas.append(actual)
    return lineas

def parrafos(textos, ancho=W - 2 * M, f=TXT):
    ren = []
    for t in textos: ren += envuelve(t, f, ancho) + ['']
    return ren[:-1] if ren else ren

def sobre_cielo(rgba, fondo=SKY): return rgba[..., :3] + fondo[None, None, :] * (1 - rgba[..., 3:4])

def recorta_alfa(rgba, margen=26, fondo=SKY):
    """El recorte ajustado a la silueta (con su sombra y su canto) sobre el cielo de la captura."""
    a = rgba[..., 3] > 0.02; ys, xs = np.where(a); h, w = a.shape
    y0, y1, x0, x1 = max(0, ys.min() - margen), min(h, ys.max() + margen), max(0, xs.min() - margen), min(w, xs.max() + margen)
    return u8(sobre_cielo(rgba[y0:y1, x0:x1], fondo))

# ------------------------------------------------------------------ las medidas
def medidas():
    m = {}
    P = {k: v for k, v in pick_pieces().items()}
    # 1) IoU de la silueta de la v3 contra el SDF del juego (exacto, 4 × 4 sub-muestras), girada y espejada como en pantalla
    bp = 60.0; ss = 4; ious = {}
    for nombre, cells in P.items():
        Pn = normalize([(-x, -y) for x, y in cells]) if nombre == 'L4' else cells
        n = A.marco(bp); d = A.sdf_pantalla(Pn, bp, 225.0, True, n=n); alfa = A.alfa_de(d, bp)
        dss = A.sdf_pantalla(Pn, bp * ss, 225.0, True, n=n * ss, robusto=True); exacto = (dss <= 0).reshape(n, ss, n, ss).mean(axis=(1, 3))
        ious[nombre] = float(((alfa > 0.5) & (exacto > 0.5)).sum() / max(1, ((alfa > 0.5) | (exacto > 0.5)).sum()))
    m['iou'] = ious
    # 2) el alfiler de las cajas de 2 × 2: la distancia y el alfa en el centro del 2 × 2 con el SDF del juego y con el arreglado
    alf = {}
    for nombre in ('P5', 'H7'):
        cells = P[nombre]; s = set(cells)
        c2 = next((x + 0.5, y + 0.5) for (x, y) in cells if {(x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)} <= s)
        gx = np.array([[c2[0]]]); gy = np.array([[c2[1]]])
        dj = float(piece_sdf(cells, gx, gy)[0, 0]); dr = float(sdf_robusto(cells, gx, gy)[0, 0]); aa = 1.0 / A.BP
        alf[nombre] = (dj, float(np.clip(0.5 - dj / aa, 0, 1)), dr, float(np.clip(0.5 - dr / aa, 0, 1)))
    m['alfiler'] = alf
    # 3) la fuga del espejo con la lámpara: pieza espejada en pantalla contra celdas reflejadas
    fugas = {}
    for nombre, cells in P.items():
        Pn = normalize([(-x, -y) for x, y in cells]) if nombre == 'L4' else cells
        fugas[nombre] = max(A.fuga_espejo(Pn, 60.0, ang)[0] for ang in (30.0, 100.0, 225.0))
    m['fuga'] = fugas
    # 4) lo que pasaría con la luz horneada en el marco de la pieza (la alternativa descartada): el brillo cae del otro lado
    L4 = normalize([(-x, -y) for x, y in P['L4']]); dx, cf, cp = A.fuga_luz_pegada(L4, 60.0, 225.0)
    m['luz_pegada'] = (dx, cf, cp)
    return m

def lum_rel(rgb):
    c = np.asarray(rgb, float); c = np.where(c <= 0.03928, c / 12.92, ((c + 0.055) / 1.055) ** 2.4); return float(0.2126 * c[..., 0] + 0.7152 * c[..., 1] + 0.0722 * c[..., 2])
def razon(a, b):
    la, lb = lum_rel(a), lum_rel(b); hi, lo = max(la, lb), min(la, lb); return (hi + 0.05) / (lo + 0.05)

def contrastes(cara_rgba):
    """La CARA de la pieza (sin canto ni sombra) contra el cielo: el color medio, el filo más oscuro de la cara (la sombra de abajo a la derecha: el 5 % más oscuro de lo que está a 1-6 px del borde) y el canto aparte."""
    a = cara_rgba[..., 3]; rgb = cara_rgba[..., :3] / np.maximum(a[..., None], 1e-4)
    cara = a > 0.99; med = rgb[cara].mean(axis=0)
    from propuesta_util import edt
    din = edt(a > 0.5); sel = (din > 1) & (din <= 6) & cara; y = lum(rgb)
    oscuro = rgb[sel][np.argsort(y[sel])[:max(1, int(sel.sum() * 0.05))]].mean(axis=0)
    canto = A.COLOR_CANTO * 0.56                                                           # el canto de 5 dp (sus pasos van de 0,70 a 0,48 del color)
    return dict(media=razon(med, SKY), filo_oscuro=razon(oscuro, SKY), canto=razon(canto, SKY), color_medio=tuple(int(round(v * 255)) for v in med))

# ------------------------------------------------------------------ las secciones
def guardas_de_plano():
    """La franja de avisos, la pieza (el círculo de giro), la barra, el puerto, los botones y la pista no se pisan, en varias formas de teléfono; los botones ≥ 56 dp y el texto ≥ 14 dp."""
    res = []
    for h, guiado in ((640, False), (700, False), (740, False), (780, False), (800, False), (900, False), (780, True), (800, True)):
        p = S.plan_v3(h, guiado); eff = h - (96 if guiado else 0)
        franja = p['franja']; mod = (p['modulo_cy'] - p['modulo_r'], p['modulo_cy'] + p['modulo_r']); barra = (p['barra_cy'] - 6 * p['K'], p['barra_cy'] + 6 * p['K'])
        port = (p['puerto_cy'] - p['puerto_h'] / 2, p['puerto_cy'] + p['puerto_h'] / 2); bot = (p['boton_cy'] - p['boton_h'] / 2, p['boton_cy'] + p['boton_h'] / 2); pista = (p['pista_cy'] - 8, p['pista_cy'] + 8)
        orden = [franja, mod, barra, port, bot, pista]; ok = all(orden[i][1] <= orden[i + 1][0] + 1e-6 for i in range(len(orden) - 1)) and pista[1] <= eff and bot[1] - bot[0] >= 56 - 1e-6
        res.append((h, guiado, ok, p['bloque'], p['boton_h'], pista[1], eff))
    return res

def seccion_pantallas():
    hoy = u8(carga(os.path.join(CAPT, 'acoplamiento', '02-respuesta.png')))
    L4 = normalize([(-x, -y) for x, y in pick_pieces()['L4']])
    v3 = S.pantalla_v3(L4, L4, luces=3, altura=800)
    ph, pw = int(2400 * 0.5), int(1080 * 0.5)
    # la tabla de tamaños (dp)
    a800, b800, a640, b640 = S.plan_hoy(800), S.plan_v3(800), S.plan_hoy(640), S.plan_v3(640)
    f = lambda v: '%.0f' % v
    filas = [('', 'hoy', 'v3'),
             ('bloque (pieza y hueco)', f(a800['bloque']), f(b800['bloque'])),
             ('bloque en 16:9 (640 dp)', f(a640['bloque']), f(b640['bloque'])),
             ('círculo de giro (diámetro)', f(2 * a800['modulo_r']), f(2 * b800['modulo_r'])),
             ('panel «Puerto»', '%s × %s' % (f(a800['puerto_w']), f(a800['puerto_h'])), '%s × %s' % (f(b800['puerto_w']), f(b800['puerto_h']))),
             ('hueco (4 × 4 bloques)', f(4 * a800['bloque']), f(4 * b800['bloque'])),
             ('botones', '%s × %s' % (f(a800['boton_w']), f(a800['boton_h'])), '%s × %s' % (f(b800['boton_w']), f(b800['boton_h']))),
             ('alto del botón en 16:9', f(a640['boton_h']), f(b640['boton_h'])),
             ('estación (aro, cilindro)', 'y 130 a 230', 'fuera'),
             ('fila de luces (de 22 dp)', '—', 'y %s, cada 30' % f(b800['luces_cy'])),
             ('franja de avisos', 'y %s a %s' % (f(a800['franja'][0]), f(a800['franja'][1])), 'y %s a %s' % (f(b800['franja'][0]), f(b800['franja'][1]))),
             ('centro de la pieza', 'y %s' % f(a800['modulo_cy']), 'y %s' % f(b800['modulo_cy'])),
             ('barra «Tiempo»', 'y %s' % f(a800['barra_cy']), 'y %s' % f(b800['barra_cy'])),
             ('centro del puerto', 'y %s' % f(a800['puerto_cy']), 'y %s' % f(b800['puerto_cy'])),
             ('centro de los botones', 'y %s' % f(a800['boton_cy']), 'y %s' % f(b800['boton_cy'])),
             ('pista (texto de 14 dp)', 'y %s' % f(a800['pista_cy']), 'y %s' % f(b800['pista_cy']))]
    xtab = M + 2 * pw + 2 * GAP
    def f_dib(hj, dr, y):
        for i, (t, col, im) in enumerate((('HOY (captura real)', GRIS, hoy), ('V3 (maqueta sobre la misma captura)', LIMA, u8(v3)))):
            x = M + i * (pw + GAP); dr.text((x, y + 2), t, font=ETQ, fill=col); hj.paste(im.resize((pw, ph), Image.LANCZOS), (x, y + 36))
        dr.text((xtab, y + 2), 'TAMAÑOS EN dp (pantalla de 360 × 800)', font=ETQ, fill=BLANCO)
        yy = y + 44
        for k, (a, b, c) in enumerate(filas):
            dr.text((xtab, yy), a, font=MONO, fill=GRIS if k == 0 else BLANCO); dr.text((xtab + 330, yy), b, font=MONO, fill=GRIS); dr.text((xtab + 520, yy), c, font=MONO, fill=GRIS if k == 0 else LIMA); yy += 32
        yy += 14; dr.text((xtab, yy), 'NADA SE PISA (franja, pieza, barra, puerto, botones y pista)', font=ETQ, fill=BLANCO); yy += 34
        dr.text((xtab, yy), 'alto de pantalla en dp · bloque · alto del botón · fin de la pista', font=MONO, fill=GRIS); yy += 30
        for (h, g, ok, B, bh, pf, eff) in G:
            dr.text((xtab, yy), '%d%s · %.0f · %.0f · %.0f de %.0f: %s' % (h, ' con tutorial' if g else '', B, bh, pf, eff, 'OK' if ok else 'FALLA'), font=MONO, fill=LIMA if ok else CORAL); yy += 28
        yy += 12
        dr.text((xtab, yy), 'Texto más chico: 14 dp. Botones: ≥ 56 dp en todas.', font=MONO, fill=GRIS)
    G = guardas_de_plano()
    return (36 + ph + 24, f_dib), v3, G

def seccion_pieza():
    """La pieza de hoy, la v3 y su espejo en la misma orientación (100 %), y otras piezas."""
    piezas = pick_pieces(); L4 = normalize([(-x, -y) for x, y in piezas['L4']])
    hoy = u8(carga(os.path.join(CAPT, 'acoplamiento', '02-respuesta.png'))[880:1290, 330:780])
    a, _, _ = A.pieza(L4, angulo=225.0, flip=False); b, _, _ = A.pieza(L4, angulo=225.0, flip=True)
    ia, ib = recorta_alfa(a), recorta_alfa(b)
    otras = []
    for nombre in ('F5', 'P5', 'C6', 'H7'):
        pz, _, _ = A.pieza(piezas[nombre], angulo=0.0, flip=False, bp=A.BP * 0.62); otras.append((nombre, recorta_alfa(pz, 14)))
    h1 = max(hoy.size[1], ia.size[1], ib.size[1])
    def f(hj, dr, y):
        dr.text((M, y + 2), 'LA PIEZA al 100 %: hoy | v3 | v3 espejada (misma orientación; la lámpara no cambia de lado)', font=SUB, fill=BLANCO)
        x = M
        for t, col, im in (('HOY', GRIS, hoy), ('V3', LIMA, ia), ('V3 ESPEJADA', LIMA, ib)):
            dr.text((x, y + 40), t, font=ETQ, fill=col); hj.paste(im, (x, y + 72)); x += im.size[0] + GAP
        y2 = y + 72 + h1 + 24
        dr.text((M, y2), 'Otras piezas en v3 (0,62 del tamaño): un 2 × 2 (P5, H7) queda liso, sin alfiler ni «+»', font=SUB, fill=BLANCO)
        x = M
        for nombre, im in otras:
            hj.paste(im, (x, y2 + 40)); x += im.size[0] + GAP
    return (72 + h1 + 24 + 40 + max(im.size[1] for n, im in otras) + 20, f), (a, b)

def seccion_puerto(v3_pantallas):
    """El puerto en sus tres estados + la pieza asentada, al 100 % (recortes del panel)."""
    L4 = normalize([(-x, -y) for x, y in pick_pieces()['L4']]); plan = S.plan_v3(800)
    cx, cy = 540, plan['puerto_cy'] * DP; ancho, alto = 440, 660
    crops = []
    for titulo, est, asent in (('en reposo (verde agua)', 'reposo', False), ('al encajar (lima)', 'ok', False), ('al mostrar (celeste)', 'mostrar', False), ('pieza asentada, a ras (lima)', 'ok', True)):
        img = S.pantalla_v3(L4, L4, estado=est, asentada=asent, pieza_visible=False, marca_ok=(asent), luces=3, altura=800)
        x0 = int(cx - ancho / 2); y0 = int(cy - alto / 2)
        crops.append((titulo, u8(img[y0:y0 + alto, x0:x0 + ancho])))
    # el alfiler de 2 × 2: la P5 con el SDF del juego y con el arreglo, ampliado
    P5 = pick_pieces()['P5']; s = set(P5)
    c2 = next((x + 0.5, y + 0.5) for (x, y) in P5 if {(x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)} <= s)
    bp = A.BP; n = A.marco(bp)
    def mini(robusto):
        d = A.sdf_pantalla(P5, bp, 0.0, False, n=n, robusto=robusto)
        cor, cav, ab = A.cordon_y_cavidad(d, bp, 'reposo')
        capa = over(cav, cor); img = sobre_cielo(capa, np.array([46, 40, 120]) / 255.0)
        # el centro del 2 × 2 en la imagen (la rejilla va de abajo hacia arriba en bloques; la pantalla, de arriba hacia abajo)
        from acopla_geo import piece_box
        ccx, ccy, cw, ch = piece_box(P5); px = (n - 1) / 2.0 + (c2[0] - ccx) * bp; py = (n - 1) / 2.0 - (c2[1] - ccy) * bp
        r = 46; x0, y0 = int(px - r), int(py - r); z = img[y0:y0 + 2 * r, x0:x0 + 2 * r]; return u8(z).resize((2 * r * 3, 2 * r * 3), Image.NEAREST)
    pin = (mini(False), mini(True))
    def f(hj, dr, y):
        dr.text((M, y + 2), 'EL PUERTO al 100 %: cordón de plasticina, pared con la lámpara y fondo oscuro', font=SUB, fill=BLANCO)
        x = M
        for t, im in crops:
            dr.text((x, y + 40), t, font=ETQ, fill=GRIS); hj.paste(im, (x, y + 72)); x += im.size[0] + GAP // 2
        y2 = y + 72 + alto + 24
        dr.text((M, y2), 'El alfiler de 2 × 2 (×3): hoy un agujero translúcido; con la caja de 2 × 2 de más (`sdf_robusto`), lleno', font=SUB, fill=BLANCO)
        dr.text((M, y2 + 40), 'HOY (el SDF del juego)', font=ETQ, fill=CORAL); hj.paste(pin[0], (M, y2 + 72))
        dr.text((M + pin[0].size[0] + GAP, y2 + 40), 'V3 (arreglado)', font=ETQ, fill=LIMA); hj.paste(pin[1], (M + pin[0].size[0] + GAP, y2 + 72))
    return (72 + alto + 24 + 72 + pin[0].size[1] + 20, f)

def seccion_secuencia():
    """El «plop»: 3 cuadros (la pieza llega, se hunde, queda a ras con el ✓ y se enciende una luz)."""
    L4 = normalize([(-x, -y) for x, y in pick_pieces()['L4']]); plan = S.plan_v3(800)
    cy_port = plan['puerto_cy']; x0, x1, y0, y1 = 170, 910, 1040, 1950
    fondo = S.fondo_juego()
    cuadros = []
    # 1: llega (todavía sobre el hueco, con su canto y su sombra)
    c1 = S.pantalla_v3(L4, L4, estado='reposo', angulo=0.0, flip=False, hueco_angulo=0.0, luces=2, pieza_cy=cy_port - 70, altura=800, base=fondo, etiqueta=False)
    # 2: plop (se hunde: más chica un 4 %, canto de 1,6 dp, el borde ya lima, una onda)
    c2 = S.pantalla_v3(L4, L4, estado='ok', angulo=0.0, flip=False, hueco_angulo=0.0, luces=2, pieza_cy=cy_port - 3, pieza_escala=0.965, pieza_canto=1.6, altura=800, base=fondo, etiqueta=False)
    onda(c2, plan, L4)
    # 3: a ras, con el ✓ y la tercera luz encendida con su halo
    c3 = S.pantalla_v3(L4, L4, estado='ok', asentada=True, luces=3, destello=2, pieza_visible=False, marca_ok=True, altura=800, base=fondo, etiqueta=False)
    S.toast(c3, plan, '¡Encaja!')
    luces_y = plan['luces_cy'] * DP
    def corte(img):
        main = u8(img[y0:y1, x0:x1]); luces = u8(img[int(luces_y - 55):int(luces_y + 160), x0:x1]); return main, luces
    cs = [corte(c1), corte(c2), corte(c3)]
    esc = 0.74; w = int((x1 - x0) * esc); h = int((y1 - y0) * esc); hl = int(215 * esc)
    titulos = ('1. la pieza llega al hueco', '2. «plop»: se hunde, el borde se pone lima', '3. a ras: visto lima y se enciende una luz')
    def f(hj, dr, y):
        dr.text((M, y + 2), 'AL ENCAJAR: 3 cuadros (unos 0,7 s en total; con «quitar animaciones» salta al cuadro 3)', font=SUB, fill=BLANCO)
        for i, ((main, luces), t) in enumerate(zip(cs, titulos)):
            x = M + i * (w + GAP); dr.text((x, y + 40), t, font=ETQ, fill=LIMA if i == 2 else GRIS)
            hj.paste(luces.resize((w, hl), Image.LANCZOS), (x, y + 72)); hj.paste(main.resize((w, h), Image.LANCZOS), (x, y + 72 + hl + 6))
    return (72 + hl + 6 + h + 24, f)

def onda(img, plan, P):
    """Una onda de plasticina que se abre alrededor del hueco en el «plop»: el contorno del hueco engordado 7 dp, lima y semitransparente."""
    bp = plan['bloque'] * DP; d = A.sdf_pantalla(P, bp, 0.0, False)
    a1 = A.alfa_de(d, bp, 8.5 * DP / bp); a0 = A.alfa_de(d, bp, 6.0 * DP / bp); aro = np.clip(a1 - a0, 0, 1) * 0.70
    capa = np.concatenate([np.array([0.82, 1.0, 0.58])[None, None, :] * aro[..., None], aro[..., None]], axis=2)
    S.pon_centro(img, capa, 540, plan['puerto_cy'] * DP - 3 * DP)
    # (se pinta debajo de la pieza en el juego; aquí basta con que se vea)

def seccion_luces():
    """La fila de avance y el hito de cada 8."""
    fondo = S.fondo_juego(); plan = S.plan_v3(800); cy = plan['luces_cy'] * DP
    def frame(luces=0, lima=0, destello=None, toast=None, medalla=0):
        img = fondo.copy(); S.fila_luces(img, 540, cy, luces, False, destello, lima=lima)
        if toast: S.toast(img, plan, toast)
        if medalla: S.medalla_anillos(img, plan, medalla)
        return u8(img[int(cy - 70):int(cy + 170), 110:1010])
    frames = [('7 de 8 encendidas', frame(7)), ('la 8.ª se enciende', frame(8, 0, 7)), ('hito: una onda lima recorre la fila y el aviso dice «¡Anillo completo!»', frame(8, 5, None, '¡Anillo completo!')), ('el anillo siguiente empieza de cero; una medalla lima cuenta el anillo', frame(0, 0, None, None, 1))]
    wf, hf = frames[0][1].size
    def f(hj, dr, y):
        dr.text((M, y + 2), 'LA FILA DE AVANCE (8 luces = un anillo) y el hito de cada 8, al 100 %', font=SUB, fill=BLANCO)
        for i, (t, im) in enumerate(frames):
            x = M + (i % 2) * (wf + GAP); yy = y + 40 + (i // 2) * (hf + 58)
            dr.text((x, yy), t, font=ETQ, fill=GRIS if i < 2 else LIMA); hj.paste(im, (x, yy + 32))
    return (40 + 2 * (hf + 58) + 10, f)

def seccion_botones():
    """«Encaja» y «Espejo» con la receta de VERDAD / DISPARATE."""
    real = u8(carga(os.path.join(CAPT, 'disparate', '03-t08.png'))[1990:2372, 36:1044])
    plan = S.plan_v3(800)
    def mk(presionada):
        img = np.zeros((300, 1080, 3)) + SKY[None, None, :]
        gap = 12 * plan['K']; cy = 150
        S.boton_kit(img, 'lima', 'Encaja', 'ico_encaja', 540 - (gap + plan['boton_w']) * 0.5 * DP, cy, plan['boton_w'], plan['boton_h'], presionada)
        S.boton_kit(img, 'lila', 'Espejo', 'ico_espejo', 540 + (gap + plan['boton_w']) * 0.5 * DP, cy, plan['boton_w'], plan['boton_h'], presionada)
        return u8(img[10:290, 36:1044])
    rep, pres = mk(False), mk(True)
    ico = [S.sprite(n, 360) for n in ('ico_encaja', 'ico_espejo')]
    def f(hj, dr, y):
        dr.text((M, y + 2), '«ENCAJA» y «ESPEJO» con la receta EXACTA de VERDAD / DISPARATE (al 100 %)', font=SUB, fill=BLANCO)
        dr.text((M, y + 40), 'HOY: VERDAD y DISPARATE (captura real de ¿Verdad o disparate?)', font=ETQ, fill=GRIS); hj.paste(real, (M, y + 72)); yy = y + 72 + real.size[1] + 16
        dr.text((M, yy), 'V3: «Encaja» y «Espejo» en reposo', font=ETQ, fill=LIMA); hj.paste(rep, (M, yy + 32)); yy += 32 + rep.size[1] + 16
        dr.text((M, yy), 'V3: presionados (el texto y el ícono bajan 2 dp, la losa `_p` del kit)', font=ETQ, fill=LIMA); hj.paste(pres, (M, yy + 32))
        x = M + real.size[0] + 2 * GAP; yy = y + 40
        ren = parrafos(['La receta, igual que en VERDAD, DISPARATE y «Arrancar»: la losa lima y la uva del kit TAL CUAL (158 × 76 dp, el mismo labio, brillo y sombra); el ícono es un churro de plasticina de tinta con su sombra suave (la misma técnica de `ico_play`); el texto es tinta lisa de 22 dp, sin halo ni canto propio; al tocar, la losa pasa a su imagen presionada y el texto baja 2 dp; al soltar rebota (`PlasticTouch.Bounce`).',
                        '«Encaja» muestra tres bloques que bajan a su soporte; «Espejo», una L y su espejo con un eje de puntos. Los dos íconos, ×1,5:'], W - x - M, TXT)
        for l in ren: dr.text((x, yy), l, font=TXT, fill=GRIS); yy += 28
        for i, im in enumerate(ico):
            capa = np.zeros((360, 360, 3)) + (np.array([166, 227, 107]) / 255.0 if i == 0 else np.array([190, 166, 250]) / 255.0)[None, None, :]
            pega(capa, im, 0, 0); hj.paste(u8(capa), (x + i * (360 + GAP), yy + 12))
    return (72 + real.size[1] + 16 + 32 + rep.size[1] + 16 + 32 + pres.size[1] + 20, f)

def seccion_final():
    hoy = u8(carga(os.path.join(CAPT, 'acoplamiento', '11-final.png'))); v3 = u8(S.pantalla_final_v3(23))
    pw, ph = int(1080 * 0.5), int(2400 * 0.5)
    def f(hj, dr, y):
        dr.text((M, y + 2), 'LA PANTALLA FINAL: «tu estación» armada con las piezas que acopló la persona (aquí, 23)', font=SUB, fill=BLANCO)
        for i, (t, col, im) in enumerate((('HOY', GRIS, hoy), ('V3', LIMA, v3))):
            x = M + i * (pw + GAP); dr.text((x, y + 40), t, font=ETQ, fill=col); hj.paste(im.resize((pw, ph), Image.LANCZOS), (x, y + 72))
        x = M + 2 * (pw + GAP); sc = u8(S.pantalla_final_v3(23)[262:800, 100:980]); sc = sc.resize((int(sc.size[0] * 0.62), int(sc.size[1] * 0.62)), Image.LANCZOS)
        dr.text((x, y + 40), 'LA ESCULTURA a 0,62', font=ETQ, fill=LIMA); hj.paste(sc, (x, y + 72))
        ren = parrafos(['Cada pieza es una de las que acopló la persona (las cinco formas, con sus giros), hecha con la misma lámpara y el mismo celeste de la pieza de juego; los anillos de abajo (completos) llevan la pista lima y el de arriba (en curso) la lavanda. Las piezas de atrás van más chicas y oscuras. Todo es un horneado una sola vez al abrir la pantalla.'], W - x - M, TXT)
        for i, l in enumerate(ren): dr.text((x, y + 72 + sc.size[1] + 16 + i * 28), l, font=TXT, fill=GRIS)
    return (72 + ph + 20, f)

# ------------------------------------------------------------------ la hoja
def hoja():
    t0 = time.time(); secciones = []
    nota = 'SOLO PROPUESTA (Tarea 117): no está en ningún juego. Ricardo rechazó las bolitas («rellenar por rellenar»): aquí NO hay adornos; el objeto mismo bien hecho, el hueco bien definido, la estación fuera de la pantalla de juego. «hoy» = capturas reales; «v3» = maqueta sobre esas mismas capturas. Pantallas a 0,5 del tamaño de un teléfono; los recortes, al 100 % (3 px por dp).'
    def titulo(hj, dr, y):
        dr.text((M, y + 8), 'Acoplamiento v3: sin estación, la pieza y el puerto más grandes', font=TITULO, fill=BLANCO)
        for i, l in enumerate(envuelve(nota, TXT, W - 2 * M)): dr.text((M, y + 62 + i * 28), l, font=TXT, fill=GRIS)
    secciones.append((62 + 28 * len(envuelve(nota, TXT, W - 2 * M)) + 14, titulo))
    sec1, v3, G = seccion_pantallas(); secciones.append(sec1); print('pantallas', round(time.time() - t0, 1))
    sec_p, (pa, pb) = seccion_pieza(); secciones.append(sec_p); print('pieza', round(time.time() - t0, 1))
    secciones.append(seccion_puerto(v3)); print('puerto', round(time.time() - t0, 1))
    secciones.append(seccion_secuencia()); print('secuencia', round(time.time() - t0, 1))
    secciones.append(seccion_luces()); secciones.append(seccion_botones()); print('luces y botones', round(time.time() - t0, 1))
    secciones.append(seccion_final()); print('final', round(time.time() - t0, 1))
    m = medidas(); print('medidas', round(time.time() - t0, 1))
    L4 = normalize([(-x, -y) for x, y in pick_pieces()['L4']]); pz, _, _ = A.pieza(L4, angulo=225.0, flip=True, canto_dp=0, sombra=False); c = contrastes(pz)
    puerto_c = {est: razon(A.ESTADOS[est], np.array([42, 37, 102]) / 255.0) for est in A.ESTADOS}
    bloques = TEXTOS_PIE(m, c, puerto_c, G)
    ren = []
    for tit, t in bloques:
        ren.append((tit, True))
        for l in envuelve(t, TXT, W - 2 * M): ren.append((l, False))
        ren.append(('', False))
    secciones.append((sum(40 if tit else 28 for l, tit in ren) + 30, lambda hj, dr, y: _dibuja_bloques(dr, y, ren)))
    alto = sum(s[0] for s in secciones) + M
    hj = Image.new('RGB', (W, alto), FONDO); dr = ImageDraw.Draw(hj); y = 0
    for hh, fn in secciones:
        fn(hj, dr, y); y += hh
    return hj, m, c, puerto_c, G

def _dibuja_bloques(dr, y, ren):
    yy = y + 10
    for l, tit in ren:
        if tit: dr.text((M, yy), l, font=SUB, fill=BLANCO); yy += 40
        else: dr.text((M, yy), l, font=TXT, fill=GRIS); yy += 28

def TEXTOS_PIE(m, c, puerto_c, G):
    iou = m['iou']; fuga = m['fuga']; dx, cf, cp = m['luz_pegada']; alf = m['alfiler']
    guardas_ok = all(g[2] for g in G)
    return [
        ('1. La pantalla de juego sin estación, con la pieza y el puerto más grandes',
         'La estación (el aro, el cilindro y los casilleros, de y 130 a 230) sale de la pantalla de juego. Con ese espacio y el de la franja de avisos, el bloque de la pieza y del hueco sube de 26 a 38 dp (+46 %; en una pantalla de 16:9, de 20 a 31 dp, +57 %): la pieza gira dentro de un círculo de 215 dp y el hueco de 4 × 4 bloques mide 152 dp. La pieza y el hueco siguen a la MISMA escala entre sí. La franja de avisos sube (y 118 a 150) y sigue sin pisar la pieza, el puerto ni los botones; los botones se quedan en 158 × 76 dp (63 dp en 16:9, sobre los 56) y el texto más chico es de 14 dp (la pista, el rótulo «Tiempo» y los avisos; «Puerto» va en 15 y los botones en 22). Lo de abajo se achica parejo en pantallas más bajas (como mínimo 0,6) y lo que sobra en las altas se reparte hacia abajo, al pulgar, como hoy.'),
        ('2. El avance: una fila de 8 luces redondas de plasticina junto al HUD',
         'Cada módulo acoplado enciende una luz de oro (22 dp, cada 30 dp, sin puntas); los anillos y los casilleros ya no se dibujan durante la partida. El hito de cada 8 es el que hoy dice «¡Anillo completo!»: una onda lima recorre la fila de izquierda a derecha (40 ms por luz), el aviso «¡Anillo completo!» sale en la franja (la losa chica dorada del kit) y la fila empieza de cero con una medalla lima que cuenta los anillos. La telemetría no cambia (anillos = módulos acoplados ÷ 8): la app no se toca. Con «quitar animaciones» la fila cambia de golpe, sin onda.'),
        ('3. Al encajar: el «plop»',
         'La pieza baja al puerto a la misma escala del hueco (380 ms, como hoy); al llegar se hunde un 4 % con el canto de 5 dp apretándose a 1,6 dp, el cordón del hueco pasa de verde agua a lima y se abre una onda lima (120 ms); queda a ras del fondo (sin canto ni sombra, con la pared de arriba a la izquierda echándole su sombra), aparece la marca de visto lima del kit en la esquina del panel, los 4 chevrones pasan a lima y se enciende la luz de la fila con un halo (200 ms). Con «quitar animaciones» el cambio es inmediato: se pasa directo al cuadro 3. Si no encaja, el cordón se pone celeste («mostrar») como hoy y la pieza se aleja sin castigo.'),
        ('4. La pieza: un objeto de plasticina sin adornos',
         'Una almohada inflada (la silueta del SDF desenfocada como relieve: sin aristas diagonales en los cuellos y las esquinas), con el canto de 5 dp hacia abajo y la sombra de contacto, celeste cálido y saturado, grano fino isótropo y una lámpara FIJA en la pantalla: brillo suave arriba a la izquierda y sombra abajo a la derecha. No hay bolitas, remaches, juntas ni «+». La silueta es exactamente la del SDF de hoy (más el arreglo del 2 × 2).'),
        ('5. El puerto: una cavidad bien definida',
         'El borde es un cordón de plasticina (un tubo de 4,6 dp: verde agua en reposo, lima al encajar, celeste al mostrar) iluminado por la misma lámpara; la pared de adentro (6 dp) se ve en sombra arriba y a la izquierda y con luz abajo y a la derecha, y el fondo es oscuro (#070818) con la sombra de la pared de arriba. El panel «Puerto» es la losa «noche» del kit (la de las casillas de Rescate) y los 4 chevrones son un churro de plasticina (hechos en Blender con la luz del kit, en 4 tonos).'),
        ('6. «Encaja» y «Espejo»',
         'La receta EXACTA de VERDAD / DISPARATE y de «Arrancar» (Tarea 116): la losa lima y la uva del kit tal cual, ícono de churro de tinta, texto liso, estado presionado y rebote; nada propio encima.'),
        ('7. La pantalla final: «tu estación» como escultura',
         'Un núcleo de plasticina lavanda y las piezas de verdad que acopló la persona en anillos de 8 (pista lima en los completos, lavanda en el que va), con los datos de siempre debajo, sin cambios.'),
        ('La lámpara: hornear la luz, no un shader',
         'Se propone HORNEAR la luz en la orientación que se muestra y no un normal map con shader. (a) El juego sigue siendo `Image` + sprite: sin shader propio que se pueda quitar del APK o que falle en una GPU distinta. (b) `BakePiece` ya corre una vez por módulo: ahora recibe el giro y el espejo y evalúa el SDF directamente en la pantalla, así la luz sale de la silueta que se VE y solo de ella: nada delata el espejo durante la decisión (fuga 0, abajo). (c) Costo: un sprite de 560 px por lado (4,8 bloques × 114 px) con el SDF a media resolución (280 × 280): unas 2,1 veces los píxeles de hoy (384), un horneado por módulo y otro (0° y sin espejo, la imagen derecha) al empezar la revelación, que es el que baja al puerto. (d) Al responder, el sprite gira y se espeja con su transform como hoy (320 + 300 ms): la lámpara gira esos 0,6 s con la pieza, pero la persona ya contestó: no hay pista que delatar. Un normal map con shader evitaría el segundo horneado, pero a cambio de un shader nuevo en toda la UI del juego.'),
        ('MEDIDAS (se vuelven a sacar con `python -I tools/juegos/propuesta_acoplamiento_v3.py`)',
         'SILUETA: IoU de la cara v3 contra el SDF exacto del juego (4 × 4 sub-muestras, girada 225° y espejada) L4 %.4f, F5 %.4f, P5 %.4f, C6 %.4f, H7 %.4f: la silueta no cambia, la respuesta tampoco. FUGA DEL ESPEJO con la lámpara: la pieza espejada en pantalla contra la de celdas reflejadas sin espejar, diferencia máxima de color sobre 255 (a 30°, 100° y 225°): %s: es 0 (menos de 1e-10). Lo que se descartó: con la luz horneada en el marco de la pieza y el espejo por escala x = −1, el centro del brillo cae %.0f px al otro lado (de x = %.0f a x = %.0f en una pieza de 60 px por bloque): una pista a simple vista de que está espejada. ALFILER DE 2 × 2: en el centro de un 2 × 2 el SDF de hoy da distancia %.1f (alfa %.2f en P5 y %.2f en H7: un agujero translúcido de 1 píxel) y con la caja de 2 × 2 de más (`sdf_robusto`) da %.1f bloque (alfa %.2f): el agujero desaparece.'
         % (iou['L4'], iou['F5'], iou['P5'], iou['C6'], iou['H7'], ', '.join('%s %.0e' % (k, v) for k, v in fuga.items()), abs(dx), cf[0], cp[0], alf['P5'][0], alf['P5'][1], alf['H7'][1], alf['P5'][2], alf['P5'][3])),
        ('CONTRASTE y TAMAÑOS',
         'Razón de luminancia (WCAG): la cara de la pieza (color medio %s) contra el cielo, %.1f : 1 (hoy 11,5 : 1); su filo más oscuro (la sombra de abajo a la derecha, el 5 %% más oscuro de lo que está a 1-6 px del borde) %.1f : 1, sobre el 3 : 1 que se pide a un objeto; el canto de 5 dp (más oscuro a propósito) queda a %.1f : 1 del cielo: la separación la hace la cara, no el canto. El cordón del hueco contra el panel «noche»: verde agua %.1f : 1, lima %.1f : 1, celeste %.1f : 1; el estado no depende solo del color: la marca de visto y los chevrones lima acompañan al encajar. Tamaños y avisos: %s en las 8 formas de teléfono probadas (640 a 900 dp, con y sin tutorial): franja, pieza, barra, puerto, botones (≥ 56 dp) y pista no se pisan.'
         % (c['color_medio'], c['media'], c['filo_oscuro'], c['canto'], puerto_c['reposo'], puerto_c['ok'], puerto_c['mostrar'], 'TODO OK' if guardas_ok else 'HAY FALLAS')),
        ('Para decidir',
         'Si el bloque de 38 dp es el tamaño (cabe con 49 dp libres bajo la pista en 780 dp; subirlo a 40 dp deja unos 28), si la medalla de anillos queda o basta con el aviso, y si «¡Encaja!» sale en la franja nueva (y 118 a 150) o se omite durante el «plop». Nada de esto toca la telemetría, el DDA ni las reglas.'),
    ]

if __name__ == '__main__':
    hj, m, c, puerto_c, G = hoja(); print('hoja', hj.size)
    print('IoU', m['iou']); print('fuga', m['fuga']); print('luz pegada', m['luz_pegada']); print('alfiler', m['alfiler']); print('contraste', c); print('puerto', puerto_c)
    for g in G: print(g)
    q = hj.quantize(colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.FLOYDSTEINBERG)
    os.makedirs(os.path.dirname(SALIDA), exist_ok=True); q.save(SALIDA, optimize=True); print('guardada', SALIDA, os.path.getsize(SALIDA) // 1024, 'KB')
