# -*- coding: utf-8 -*-
"""Acoplamiento — las hojas del estudio (Tarea 113, fase A): la pantalla con el fondo REAL del juego, las piezas, los acercamientos, la fuga del espejo y los bordes del hueco."""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from acopla_geo import *
from acopla_caminos import *
from acopla_medidas import *

AQUI = os.path.dirname(os.path.abspath(__file__)); REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
CAPT = os.path.join(REPO, 'unity', 'test-results', 'capturas', 'acoplamiento')
FB = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Fonts', 'Fredoka-SemiBold.ttf')
SKY = np.array([0x0A, 0x0B, 0x2A]) / 255.0; PANEL = np.array([0x2C, 0x2A, 0x6E]) / 255.0

def fuente(n):
    try: return ImageFont.truetype(FB, n)
    except Exception: return ImageFont.load_default()

def espejada(cells): return mirror(cells)
def arriba(a): return a[::-1]
def a_pil(a): return Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8), 'RGB')

def sep_hueco(H, cells):
    """El hueco de plasticina (fondo oscuro + aro claro en UN sprite) separado en (aro, fondo) por la luz: el aro es claro y el fondo oscuro; f = fracción de aro de cada píxel."""
    L = luma(H); gx, gy = grid(cells); d = piece_sdf(cells, gx, gy)
    Lf = float(np.median(L[(d < -0.25) & (H[..., 3] > 0.99)])); Lr = float(np.median(L[(d > 0.04) & (d < 0.08) & (H[..., 3] > 0.99)]))
    f = np.clip((L - Lf) / (Lr - Lf), 0.0, 1.0)[..., None] * (H[..., 3:4] > 0.001)
    return H * f, H * (1 - f), Lf, Lr

def pegado(dst, src, x0, y0):
    """Compone [src] (premultiplicado, fila 0 ARRIBA) sobre [dst] (RGB float) en (x0, y0)."""
    h, w = src.shape[:2]; ys0, xs0 = max(0, y0), max(0, x0); ys1, xs1 = min(dst.shape[0], y0 + h), min(dst.shape[1], x0 + w)
    if ys1 <= ys0 or xs1 <= xs0: return
    s = src[ys0 - y0:ys1 - y0, xs0 - x0:xs1 - x0]; d = dst[ys0:ys1, xs0:xs1]
    dst[ys0:ys1, xs0:xs1] = s[..., :3] + d * (1 - s[..., 3:4])

def tinta_capa(sprite, rgb, a=1.0):
    """Un sprite premultiplicado teñido como `Image.color` (rgb × rgb, alfa × a)."""
    o = sprite.copy(); o[..., :3] = o[..., :3] * np.asarray(rgb)[None, None, :] * a; o[..., 3] = o[..., 3] * a; return o

def hueco_en_pantalla(kind, layers, cells, bp, tinte, tinte_a, angulo=0.0):
    """Las capas del hueco como se ven en el puerto (fila 0 abajo). hoy: (borde, hueco) blancos teñidos; c: un sprite (aro claro + fondo oscuro) separado por la luz."""
    s = bp / PX
    if kind == 'hoy':
        edge, hole = layers[1], layers[2]
        ee = tinta_capa(resample(edge, s, angulo), tinte, tinte_a); hh = tinta_capa(resample(hole, s, angulo), HOLEC, 1.0)
        return hh + ee * (1 - hh[..., 3:4])
    rim, floor = layers[1], layers[2]                                              # (cuerpo, aro, fondo): el aro se tiñe y el fondo va encima
    r = tinta_capa(resample(rim, s, angulo), tinte, tinte_a); f = resample(floor, s, angulo)
    return f + r * (1 - f[..., 3:4])

def fondo_real():
    """El cielo y el puerto REALES (captura de «llegada»: el módulo todavía no llegó) con el hueco borrado (se rellena con la columna del panel a su izquierda); 1080 × 2400."""
    ruta = os.path.join(CAPT, '01-llegada.png')
    if not os.path.exists(ruta): return None
    im = np.asarray(Image.open(ruta).convert('RGB')).astype(np.float64) / 255.0
    cells = pick_pieces()['L4']; b, e, h = today_layers(cells)
    bp = 78.0; layer = resample(e, bp / PX, 0.0); n = layer.shape[0]; cx, cy = 540, 1650; x0 = cx - n // 2; y0 = cy - n // 2
    m = arriba(layer[..., 3]) > 0.01
    base = im.copy(); region = base[y0:y0 + n, x0:x0 + n]; col = im[y0:y0 + n, 330:331, :]
    mm = m[:region.shape[0], :region.shape[1]]; region[mm] = np.broadcast_to(col, region.shape)[mm]
    return base, im

def pantalla(base, camino, cells, angulo=45.0, flip=False, bp=78.0, estado='idle', cuerpo=None, hueco=None, hoy_layers=None):
    """Una pantalla (1080 × 2400, RGB 0..1) con el módulo y el hueco de [camino] ('hoy' | 'c1' | 'c2') sobre el fondo real."""
    img = base.copy(); tinte, ta = {'idle': (TEAL, TEAL_A), 'ok': (LIME, 1.0), 'show': (CYAN, 1.0)}[estado]
    hl = hueco_en_pantalla('hoy' if camino == 'hoy' else 'c', hoy_layers if camino == 'hoy' else hueco, cells, bp, tinte, ta)
    n = hl.shape[0]; pegado(img, arriba(hl), 540 - n // 2, 1650 - n // 2)
    mod, _ = module_on_screen(hoy_layers[0] if camino == 'hoy' else cuerpo, cells, bp, angle_deg=angulo, flip=flip)
    n = mod.shape[0]; pegado(img, arriba(mod), 540 - n // 2, 1098 - n // 2)
    return img

def celda(img_premult_screen, fondo, cells, bp):
    n = img_premult_screen.shape[0]; img = np.broadcast_to(fondo, (n, n, 3)).copy(); pegado(img, arriba(img_premult_screen), 0, 0)
    ccx, ccy, w, h = piece_box(cells)
    x0 = int(round(n / 2 - (w / 2 + 0.9) * bp)); x1 = int(round(n / 2 + (w / 2 + 0.9) * bp)); y0 = int(round(n / 2 - (h / 2 + 0.9) * bp)); y1 = int(round(n / 2 + (h / 2 + 0.9) * bp))
    return img[y0:y1, x0:x1]

def celda_modulo(body, cells, bp, angulo=0.0):
    mod, _ = module_on_screen(body, cells, bp, angle_deg=angulo); return celda(mod, SKY, cells, bp)

def celda_hueco(kind, layers, cells, bp, tinte=TEAL, ta=TEAL_A):
    return celda(hueco_en_pantalla(kind, layers, cells, bp, tinte, ta), PANEL, cells, bp)

def hoja_piezas(C, P, spr):
    bp = 52.0; ft = fuente(22); fs = fuente(17)
    ancho_col = int(5.8 * bp) + 14; ncol = 7; alto_fila = int(5.8 * bp) + 14
    hoja = Image.new('RGB', (ncol * ancho_col + 20, 130 + alto_fila * len(P)), (8, 10, 34)); d = ImageDraw.Draw(hoja)
    d.text((12, 8), 'Acoplamiento — estudio de la plasticina: las 5 piezas reales a 52 u por bloque, con la luz y la sombra fijas de pantalla (las pantallas a 78 u, al lado)', font=ft, fill=(237, 234, 251))
    d.text((12, 38), 'MÓDULO: hoy · camino 1 (baldosas) · camino 2 (SDF + perfil) · camino 2 con el relieve ×2,5    HUECO con aro teal: hoy · camino 1 · camino 2', font=fs, fill=(171, 165, 210))
    for i, t in enumerate(['hoy', 'camino 1', 'camino 2', 'camino 2, relieve ×2,5', 'hoy', 'camino 1', 'camino 2']): d.text((12 + i * ancho_col + 8, 72), t, font=fs, fill=(255, 201, 60))
    y = 100
    for k, c in P.items():
        b, e, h = spr[k]['hoy']; l1 = spr[k]['c1']; l2 = spr[k]['c2']
        b25 = compone_c2(c, C.pm.con_relieve(2.5))
        celdas = [celda_modulo(b, c, bp), celda_modulo(l1[0], c, bp), celda_modulo(l2[0], c, bp), celda_modulo(b25, c, bp), celda_hueco('hoy', (b, e, h), c, bp), celda_hueco('c', l1, c, bp), celda_hueco('c', l2, c, bp)]
        for i, im in enumerate(celdas): hoja.paste(a_pil(im), (12 + i * ancho_col + 8, y))
        d.text((14, y + celdas[0].shape[0] + 2), f'{k} ({len(c)} bloques)', font=fs, fill=(171, 165, 210))
        y += alto_fila - 20
    return hoja.crop((0, 0, hoja.width, y + 10))

def acercamientos(C, P, spr):
    """Los acercamientos ×4 (bloque de 47 u, el más chico): una esquina convexa y un borde (L4), la concavidad de un bloque (C6) y un 2 × 2 (H7), en los tres caminos."""
    bp = 47.0; z = 4; ft = fuente(17)
    casos = [('esquina convexa y borde (L4)', 'L4', (-0.2, 1.0, -0.2, 1.0)), ('concavidad de 1 bloque (C6)', 'C6', (0.2, 1.8, 0.8, 2.4)), ('un 2 × 2: ¿juntas o un «+»? (H7)', 'H7', (-0.3, 1.3, -0.3, 1.3))]
    lado = int(1.6 * bp * z); ancho = 3 * (lado + 20) + 20; fil = lado + 50
    hoja = Image.new('RGB', (ancho, 40 + fil * len(casos)), (8, 10, 34)); d = ImageDraw.Draw(hoja)
    d.text((12, 8), 'Acercamientos ×4 (bloque de 47 u, el más chico): hoy · camino 1 · camino 2', font=ft, fill=(237, 234, 251))
    for r, (titulo, k, (x0, x1, y0, y1)) in enumerate(casos):
        c = P[k]; cuerpos = [spr[k]['hoy'][0], spr[k]['c1'][0], spr[k]['c2'][0]]
        d.text((12, 36 + r * fil), titulo, font=ft, fill=(255, 201, 60))
        for i, cu in enumerate(cuerpos):
            mod, _ = module_on_screen(cu, c, bp); n = mod.shape[0]; img = np.broadcast_to(SKY, (n, n, 3)).copy(); pegado(img, arriba(mod), 0, 0)
            ccx, ccy, w, h = piece_box(c)
            px0 = int(round(n / 2 + (x0 - ccx) * bp)); px1 = int(round(n / 2 + (x1 - ccx) * bp)); py0 = int(round(n / 2 - (y1 - ccy) * bp)); py1 = int(round(n / 2 - (y0 - ccy) * bp))
            rec = Image.fromarray((np.clip(img[py0:py1, px0:px1], 0, 1) * 255).astype(np.uint8), 'RGB').resize(((px1 - px0) * z, (py1 - py0) * z), Image.NEAREST)
            hoja.paste(rec, (12 + i * (lado + 20), 62 + r * fil))
    return hoja

def hoja_pantallas(C, P, spr):
    fr = fondo_real()
    if fr is None: return None
    base, _ = fr; k = 'L4'; c = P[k]; hoy = spr[k]['hoy']; l1 = spr[k]['c1']; l2 = spr[k]['c2']
    pantallas = [('HOY (réplica del arte procedural)', pantalla(base, 'hoy', c, hoy_layers=hoy)), ('CAMINO 1 — sub-baldosas', pantalla(base, 'c1', c, cuerpo=l1[0], hueco=l1, hoy_layers=hoy)), ('CAMINO 2 — SDF + perfil', pantalla(base, 'c2', c, cuerpo=l2[0], hueco=l2, hoy_layers=hoy))]
    y0, y1 = 930, 1940; ft = fuente(30)
    hoja = Image.new('RGB', (3 * 1090 + 20, (y1 - y0) + 70), (8, 10, 34)); d = ImageDraw.Draw(hoja)
    for i, (t, im) in enumerate(pantallas):
        d.text((20 + i * 1090, 14), t, font=ft, fill=(237, 234, 251)); hoja.paste(a_pil(im[y0:y1]), (20 + i * 1090, 60))
    return hoja

def hoja_espejo(C, P, spr):
    """La fuga del espejo en barras: la modulación DIRECCIONAL del canto (primer armónico del azimut de la normal, en niveles de 255; el peor de las 5 piezas) de hoy, los dos caminos y el control de UNA sola luz sin simetrizar. Una luz con dirección cambia de lado al espejar el sprite (escala x −1) y lo delata."""
    ft = fuente(17); fs = fuente(15); nombres = ('hoy', 'camino 1', 'camino 2', 'control (1 luz)'); vals = []
    for nombre in nombres:
        a1 = []; a2 = []
        for k, c in P.items():
            S = spr[k]['hoy'][0] if nombre == 'hoy' else spr[k]['c1'][0] if nombre == 'camino 1' else spr[k]['c2'][0] if nombre == 'camino 2' else C.control(c)
            d1, d2, _ = direccionalidad(S, c); a1.append(d1); a2.append(d2)
        vals.append((nombre, max(a1), max(a2)))
    W, H = 4 * 400 + 20, 360; hoja = Image.new('RGB', (W, H), (8, 10, 34)); d = ImageDraw.Draw(hoja)
    d.text((12, 8), 'Fuga del espejo: modulación DIRECCIONAL del canto (1.er armónico del azimut de la normal, niveles de 255; el peor de las 5 piezas)', font=ft, fill=(237, 234, 251))
    d.text((12, 34), 'una luz con dirección cambia de lado al espejar el sprite (escala x −1) y lo delata; 0 = el espejo no se delata', font=fs, fill=(171, 165, 210))
    base = H - 50; escala = 220 / max(1.5, max(v[1] for v in vals))
    for i, (n, a1, a2) in enumerate(vals):
        x0 = 60 + i * 400; h = a1 * escala
        d.rectangle((x0, base - h, x0 + 200, base), fill=(255, 201, 60) if 'control' not in n else (255, 107, 74))
        d.text((x0, base + 6), n, font=ft, fill=(237, 234, 251)); d.text((x0 + 4, base - h - 22), f'{a1:.2f}', font=ft, fill=(237, 234, 251))
    d.line((40, base, W - 20, base), fill=(171, 165, 210), width=1)
    return hoja

def hoja_tintes(C, P, spr):
    """Los bordes del hueco en teal, lima y celeste (L4), hoy y los dos caminos."""
    bp = 78.0; k = 'L4'; c = P[k]; b, e, h = spr[k]['hoy']; l1 = spr[k]['c1']; l2 = spr[k]['c2']; ft = fuente(17); celdas = []
    for kind, layers in (('hoy', (b, e, h)), ('c', l1), ('c', l2)):
        for (t, ta) in ((TEAL, TEAL_A), (LIME, 1.0), (CYAN, 1.0)): celdas.append(celda_hueco(kind, layers, c, bp, t, ta))
    w = celdas[0].shape[1]; hh = celdas[0].shape[0]
    hoja = Image.new('RGB', (3 * (w + 12) + 12, 3 * (hh + 12) + 50), (8, 10, 34)); d = ImageDraw.Draw(hoja)
    d.text((12, 8), 'El hueco con su borde en teal (reposo), lima (encajó) y celeste (mostrar): fila 1 hoy · fila 2 camino 1 · fila 3 camino 2', font=ft, fill=(237, 234, 251))
    for i, im in enumerate(celdas): hoja.paste(a_pil(im), (12 + (i % 3) * (w + 12), 40 + (i // 3) * (hh + 12)))
    return hoja

def hoja_principal(C, P, spr):
    partes = [hoja_piezas(C, P, spr), acercamientos(C, P, spr), hoja_espejo(C, P, spr), hoja_tintes(C, P, spr)]
    ancho = max(p.width for p in partes); alto = sum(p.height for p in partes) + 20 * len(partes)
    hoja = Image.new('RGB', (ancho, alto), (8, 10, 34)); y = 0
    for p in partes: hoja.paste(p, (0, y)); y += p.height + 20
    return hoja
