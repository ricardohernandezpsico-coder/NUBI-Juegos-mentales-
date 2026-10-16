# -*- coding: utf-8 -*-
"""Arma `docs/previews/plasticina/correo-piezas.png`: las 12 cartas sobre la cinta como las ve el juego, el sello dorado de cerca (a tamaño real, a ×0,7 como las cartas de atrás, en grises y con protanopía, deuteranopía y tritanopía), los 4 planetas con su
figura, los cuerpos de la estación (buzones, caja fuerte, faro y reloj, con sus losas del kit y su estado presionado), la nave y el saco, y la marca de raya del resumen, con los sprites REALES del atlas (`Assets/Resources/Plasticina/correo` y las losas
de `comun`), a 3 píxeles por dp (el tamaño del teléfono: 1 dp = 3 unidades del lienzo de 1080).
Uso: python -I tools/juegos/hoja_correo.py [salida.png]"""
import json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
import paleta_daltonismo as pd
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
PLAST = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina')
SALIDA = sys.argv[1] if len(sys.argv) > 1 else os.path.join(REPO, 'docs', 'previews', 'plasticina', 'correo-piezas.png')
PX = 3                                                       # píxeles por dp de la hoja (el teléfono: 1080 / 360)
CIELO = (0x0A, 0x0F, 0x33, 255); CINTA = (20, 27, 58, 255)
NOMBRES = ('Coralia', 'Celesta', 'Lima', 'Uva')

def abre(grupo):
    man = json.load(open(os.path.join(PLAST, grupo, 'manifest.json'), encoding='utf-8')); at = Image.open(os.path.join(PLAST, grupo, 'atlas.png')).convert('RGBA')
    return man, at
MAN, ATLAS = abre('correo'); MCOM, ACOM = abre('comun')

def sprite(n, grupo='correo'):
    man, at = (MAN, ATLAS) if grupo == 'correo' else (MCOM, ACOM)
    e = next(s for s in man['sprites'] if s['n'] == n)
    return at.crop((e['x'], at.height - e['y'] - e['h'], e['x'] + e['w'], at.height - e['y'])), e

def a_px(im, e, escala=1.0):
    """Un sprite a la hoja: [e['ppu']] píxeles por unidad → [PX] píxeles por dp (los sprites de 4 px por dp se reducen; los de 3 quedan igual)."""
    k = PX / e['ppu'] * escala
    return im if abs(k - 1) < 1e-6 else im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)

def pega(base, im, cx, cy):
    base.alpha_composite(im, (int(round(cx - im.width / 2)), int(round(cy - im.height / 2))))

def losa(ancho_dp, alto_dp, tono='noche', presionada=False):
    """Una losa del kit (nine-slice, como Unity) con el CUERPO de [ancho_dp] × [alto_dp]; devuelve la imagen con su sombra y el desplazamiento del cuerpo."""
    n = f'losa_md_{tono}' + ('_p' if presionada else '')
    im, e = sprite(n, 'comun'); b = e['borde']; m = e['margen']                          # borde: izq, abajo, der, arr (px); margen: izq, arr, der, abajo (px)
    W = round(ancho_dp * PX) + m[0] + m[2]; H = round(alto_dp * PX) + m[1] + m[3]
    bl, bb, br, bt = b; w0, h0 = im.size
    xs = [0, bl, w0 - br, w0]; ys = [0, bt, h0 - bb, h0]                                  # en la imagen (y hacia abajo): arriba bt, abajo bb
    xd = [0, bl, W - br, W]; yd = [0, bt, H - bb, H]
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    for i in range(3):
        for j in range(3):
            parte = im.crop((xs[i], ys[j], xs[i + 1], ys[j + 1])); tam = (max(1, xd[i + 1] - xd[i]), max(1, yd[j + 1] - yd[j]))
            out.alpha_composite(parte.resize(tam, Image.LANCZOS) if parte.size != tam else parte, (xd[i], yd[j]))
    return out, (m[0], m[1]), e.get('baja', 0)

def cvd(im, v):
    a = np.asarray(im).astype(np.float32) / 255.0; M = pd.M[v]
    lin = np.where(a[..., :3] <= 0.04045, a[..., :3] / 12.92, ((a[..., :3] + 0.055) / 1.055) ** 2.4)
    s = np.clip(lin @ M.T, 0, 1); srgb = np.where(s <= 0.0031308, s * 12.92, 1.055 * np.power(s, 1 / 2.4) - 0.055)
    return Image.fromarray((np.concatenate([srgb, a[..., 3:4]], axis=2) * 255 + 0.5).astype(np.uint8), 'RGBA')

def grises(im):
    a = np.asarray(im).astype(np.float32); g = 0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]
    return Image.fromarray(np.stack([g, g, g, a[..., 3]], axis=2).astype(np.uint8), 'RGBA')

def sobre(color, w, h):
    return Image.new('RGBA', (w, h), color)

lienzos = []                                                  # (título, imagen)

# ---- 1. las 12 cartas sobre la cinta
fila = sobre(CINTA, 12 * 130 * PX // 3 * 3 // 3 + 20, 100 * PX)
W = 4 * 130 * PX + 20; H = 3 * 96 * PX + 20; cinta = sobre(CINTA, W, H)
for c in range(3):
    for p in range(4):
        im, e = sprite(f'carta_{p}_{c}'); pega(cinta, a_px(im, e), 10 + 65 * PX + p * 130 * PX, 10 + 48 * PX + c * 96 * PX)
lienzos.append(('las 12 cartas: normal, sello dorado, lazo (a tamaño real, 3 px por dp)', cinta))

# ---- 2. el sello dorado de cerca
def recorte_sello(p, c, escala=1.0):
    im, e = sprite(f'carta_{p}_{c}'); im = a_px(im, e, escala)
    cx, cy = (32 if c == 1 else 34), (-14 if c == 1 else -16)                          # centro del sello en dp (y hacia abajo), origen en el centro de la carta
    W, H = im.size; px = PX * escala; x = W / 2 + cx * px; y = H / 2 + cy * px; r = 24 * px
    return im.crop((int(x - r), int(y - r), int(x + r), int(y + r)))
cel = 48 * PX; cuad = sobre((0xF6, 0xEF, 0xDC, 255), 1, 1)
filas = []
for etiqueta, f in (('tamaño real', lambda im: im), ('grises', grises), ('protanopía', lambda im: cvd(im, 'protan')), ('deuteranopía', lambda im: cvd(im, 'deutan')), ('tritanopía', lambda im: cvd(im, 'tritan'))):
    fila = Image.new('RGBA', (4 * (cel + 12) * 2 + 12, cel + 14), CINTA)
    for p in range(4):
        for k, c in enumerate((0, 1)):
            s = recorte_sello(p, c); fondo = Image.new('RGBA', s.size, (0xF6, 0xEF, 0xDC, 255)); fondo.alpha_composite(s)
            fila.alpha_composite(f(fondo), (6 + (p * 2 + k) * (cel + 12), 6))
    filas.append((etiqueta, fila))
sello = Image.new('RGBA', (filas[0][1].width + 120, sum(f.height + 18 for _, f in filas)), CINTA); d = ImageDraw.Draw(sello); y = 0
for etiqueta, f in filas:
    d.text((6, y + f.height // 2), etiqueta, fill=(220, 220, 240, 255)); sello.alpha_composite(f, (110, y)); y += f.height + 18
lienzos.append(('el sello normal y el dorado de cada planeta (ancho de 48 dp), con visión típica, en grises y con protanopía, deuteranopía y tritanopía', sello))

# ---- 3. los planetas
fila = sobre(CIELO, 4 * 70 * PX + 20, 70 * PX)
for p in range(4):
    im, e = sprite(f'planeta_{p}'); pega(fila, a_px(im, e), 10 + 35 * PX + p * 70 * PX, 35 * PX)
lienzos.append(('los 4 planetas de los buzones (círculo, triángulo, cuadrado redondeado y gota)', fila))

# ---- 4. los cuerpos de la estación: buzones, caja fuerte y faro, reposo y presionados
def buzon(p, ancho, presionado):
    """Un buzón como lo arma el juego: la losa «noche» (cuerpo de [ancho] × 112 dp), la boca (12 dp de alto, a 18 dp del borde de arriba), el planeta (a 60 dp del borde de arriba) y el nombre (a 14 dp del borde de abajo); si está presionado, todo lo de encima baja lo que baja la losa."""
    cuerpo, (ox, oy), baja = losa(ancho, 112, 'noche', presionado); lienzo = Image.new('RGBA', cuerpo.size, (0, 0, 0, 0)); lienzo.alpha_composite(cuerpo)
    b = baja                                                                          # `baja` ya está en píxeles del lienzo (3 por dp)
    boca = Image.new('RGBA', (round((ancho - 24) * PX), 12 * PX), (11, 10, 38, 255)); lienzo.alpha_composite(boca, (ox + round(12 * PX), oy + round(12 * PX + b)))
    im, e = sprite(f'planeta_{p}'); R = min(28.0, ancho * 0.3); pl = a_px(im, e, R / 28.0); pega(lienzo, pl, ox + ancho * PX / 2, oy + 60 * PX + b)
    ImageDraw.Draw(lienzo).text((ox + ancho * PX / 2 - 14, oy + 98 * PX + b - 6), NOMBRES[p], fill=(214, 209, 242, 255))
    return lienzo
fila = sobre(CIELO, 6 * 120 * PX, 140 * PX)
for k, (p, pres) in enumerate(((0, False), (1, True), (2, False), (3, False))):
    lb = buzon(p, 102, pres); pega(fila, lb, 70 * PX + k * 120 * PX, 70 * PX)
lienzos.append(('buzones de 102 dp: el segundo, presionado (la losa «noche» presionada: más baja, más oscura, y lo de encima baja 2 dp)', fila))

def boton(rotulo, presionado, torre=False, lamp=False):
    cuerpo, (ox, oy), baja = losa(158, 88, 'noche', presionado); lienzo = Image.new('RGBA', cuerpo.size, (0, 0, 0, 0)); lienzo.alpha_composite(cuerpo); b = baja
    cx0, cy0 = ox, oy + 44 * PX + b
    if not torre:
        pu, ep = sprite('caja_puerta'); pega(lienzo, a_px(pu, ep), cx0 + (10 + 31) * PX, cy0)
        di, ed = sprite('dial'); pega(lienzo, a_px(di, ed), cx0 + 41 * PX, cy0 - 8 * PX)
        ma, em = sprite('manija'); pega(lienzo, a_px(ma, em), cx0 + 41 * PX, cy0 + (-8 + 8 + 20 - 4) * PX)
    else:
        to, et = sprite('torre'); pega(lienzo, a_px(to, et), cx0 + 40 * PX, cy0 + 4 * PX)
        la, el = sprite('lampara_1' if lamp else 'lampara_0'); pega(lienzo, a_px(la, el), cx0 + 40 * PX, cy0 + (4 - 20) * PX)
    d = ImageDraw.Draw(lienzo); d.text((cx0 + 82 * PX + 6, cy0 - 14), rotulo, fill=(237, 234, 251, 255))
    return lienzo
fila = sobre(CIELO, 4 * 175 * PX, 110 * PX)
for k, (rot, pres, tor, lam) in enumerate((('Caja fuerte', False, False, False), ('(presionada)', True, False, False), ('Faro', False, True, False), ('Faro encendido', False, True, True))):
    pega(fila, boton(rot, pres, tor, lam), 88 * PX + k * 175 * PX, 55 * PX)
lienzos.append(('la caja fuerte (dial de 8 marcas, puerta y manija), su presionada, el faro y el faro encendido (lámpara)', fila))

# ---- 5. reloj, nave, saco, raya
fila = sobre(CIELO, 8 * 80 * PX, 80 * PX)
ba, eb = sprite('reloj_base'); ta, et = sprite('reloj_tapa'); tp, _ = sprite('reloj_tapa_p')
c = 0
for nombre in ('tapado', 'presionado', 'destapado'):
    x = 40 * PX + c * 80 * PX; pega(fila, a_px(ba, eb), x, 40 * PX)
    if nombre == 'tapado': pega(fila, a_px(ta, et), x, 40 * PX)
    elif nombre == 'presionado': pega(fila, a_px(tp, et), x, 40 * PX)
    ImageDraw.Draw(fila).text((x - 28, 74 * PX), nombre, fill=(220, 220, 240, 255)); c += 1
na, en = sprite('nave'); pega(fila, a_px(na, en), 40 * PX + 3 * 80 * PX + 44 * PX, 40 * PX)
sa, es = sprite('saco'); pega(fila, a_px(sa, es), 40 * PX + 5 * 80 * PX + 10 * PX, 40 * PX)
ra, er = sprite('marca_raya'); pega(fila, a_px(ra, er), 40 * PX + 6 * 80 * PX + 14 * PX, 40 * PX)
oc, eo = sprite('marca_ok', 'comun'); oc = oc.resize((32 * PX, 32 * PX), Image.LANCZOS); pega(fila, oc, 40 * PX + 7 * 80 * PX + 14 * PX, 40 * PX)
lienzos.append(('el reloj (tapado, presionado, destapado), la nave, el saco, la raya del resumen y la moneda ✓ de lo común', fila))

# ---- la hoja
ancho = max(l.width for _, l in lienzos) + 24; alto = sum(l.height + 34 for _, l in lienzos) + 10
hoja = Image.new('RGBA', (ancho, alto), (0x08, 0x0A, 0x22, 255)); d = ImageDraw.Draw(hoja); y = 6
for titulo, l in lienzos:
    d.text((12, y), titulo, fill=(171, 165, 210, 255)); hoja.alpha_composite(l, (12, y + 16)); y += l.height + 34
os.makedirs(os.path.dirname(SALIDA), exist_ok=True)
hoja.convert('RGB').save(SALIDA, optimize=True); print('hoja', SALIDA, hoja.size)
