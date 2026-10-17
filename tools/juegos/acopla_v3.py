# -*- coding: utf-8 -*-
"""Acoplamiento v3 (Tarea 117, SOLO PROPUESTA): las herramientas numpy con que `propuesta_acoplamiento_v3.py` arma la hoja. Nada de esto está en el juego.
Todo se evalúa EN LA PANTALLA (fila 0 ARRIBA, 3 px por dp): el SDF de la pieza se muestrea ya girado y espejado, y la luz (la lámpara, FIJA en la pantalla: arriba a la izquierda) se calcula sobre esa silueta. Así la luz NUNCA viaja pegada al marco de la pieza: lo único que sabe el sombreado es la silueta que se ve (la fuga del espejo es 0 por construcción; ver `fuga_espejo`).
  · `sdf_pantalla`: la distancia con signo (en bloques) de una pieza vista en pantalla, con el arreglo del alfiler de las cajas de 2 × 2 (`sdf_robusto`).
  · `pieza`: el objeto de plasticina (almohada con canto redondo, lámpara fija, grano fino isótropo, celeste cálido saturado, canto de 5 dp hacia abajo y sombra de contacto).
  · `puerto`: la cavidad (cordón de plasticina en el borde, pared interior iluminada por la misma lámpara, fondo oscuro) con la pieza asentada opcional.
  · escultura de la estación para la pantalla final.
"""
import os, sys, math
import numpy as np
from PIL import Image
AQUI = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, AQUI)
from acopla_geo import normalize, mirror, rot90, pick_pieces, piece_box, round_box, piece_sdf, cover, SIDE, R as RAD, INK as INK_BLOQUE
from acopla_caminos import sdf_robusto
from propuesta_util import (DP, blur, shift, over, pega, distancia_adentro, grano, rampa, canto_y_sombra, rgb_a_hsv, hsv_a_rgb, LUZ, lum, dibuja, texto, rect_redondeado, edt)

def blur_fft(a, sigma):
    """Un desenfoque gaussiano por FFT (rápido para sigmas grandes): [a] 2-D float; los bordes se reflejan."""
    if sigma <= 0: return a
    r = int(math.ceil(sigma * 3)); ap = np.pad(a, r, mode='edge'); H, W = ap.shape
    fy = np.fft.fftfreq(H)[:, None]; fx = np.fft.rfftfreq(W)[None, :]
    g = np.exp(-2.0 * (math.pi * sigma) ** 2 * (fx * fx + fy * fy))
    out = np.fft.irfft2(np.fft.rfft2(ap) * g, s=ap.shape); return out[r:-r, r:-r]

BLOQUE_DP = 38.0                                           # el bloque de la pieza y del hueco en la v3 (hoy 26)
BP = BLOQUE_DP * DP                                        # 114 px por bloque

L = LUZ / np.linalg.norm(LUZ)                              # la lámpara: (x a la derecha, y ARRIBA, z al ojo)
V = np.array([0.0, 0.0, 1.0]); HALF = (L + V) / np.linalg.norm(L + V)

# el celeste cálido y saturado de la pieza: de la sombra profunda a la luz de plasticina (t = 0,62 ≈ la cara plana: 118, 208, 244)
RAMPA_PIEZA = [(0.00, (14, 52, 128)), (0.28, (34, 104, 192)), (0.62, (118, 208, 244)), (0.84, (168, 232, 255)), (1.00, (240, 252, 255))]

# ------------------------------------------------------------------ la geometría en pantalla
def marco(bp=BP, margen_bloques=0.8):
    """El lado (px, par) del cuadro donde cabe la pieza con cualquier giro: el círculo de 4 × 4 bloques más un margen."""
    n = int(math.ceil((math.sqrt(32.0) + 2 * margen_bloques) * bp)); return n + n % 2

def sdf_pantalla(P, bp=BP, angulo=0.0, flip=False, n=None, robusto=True, desplaza=(0.0, 0.0)):
    """La distancia con signo (en BLOQUES; fila 0 ARRIBA) de la pieza [P] vista en pantalla: girada [angulo]° (antihorario) y espejada si [flip], centrada en el centro de su caja; [desplaza] (px) corre el dibujo."""
    n = n or marco(bp)
    ys, xs = np.mgrid[0:n, 0:n]; X = xs - (n - 1) / 2.0 - desplaza[0]; Y = (n - 1) / 2.0 - ys - desplaza[1]
    th = math.radians(angulo); c, s = math.cos(th), math.sin(th)
    xi = (X * c + Y * s) / bp; yi = (-X * s + Y * c) / bp
    if flip: xi = -xi
    ccx, ccy, cw, ch = piece_box(P)
    return (sdf_robusto if robusto else piece_sdf)(P, ccx + xi, ccy + yi)

def alfa_de(d, bp=BP, nivel=0.0): return cover(d - nivel, 1.0 / bp)

# ------------------------------------------------------------------ el relieve y la lámpara
def normales(h, ssigma=0.8):
    """Las normales (x derecha, y ARRIBA, z al ojo) de una altura [h] (px; fila 0 ARRIBA)."""
    hs = blur(h, ssigma) if ssigma > 0 else h
    gy, gx = np.gradient(hs)
    nx, ny, nz = -gx, gy, np.ones_like(hs)                                 # (la fila crece hacia abajo: y arriba = −fila)
    m = np.sqrt(nx * nx + ny * ny + nz * nz); return np.stack([nx / m, ny / m, nz / m], axis=-1)

def lampara(n, brillo=0.55, dureza=48.0):
    """La luz de la lámpara sobre unas normales: (difuso normalizado a la cara plana: 1 plano, 0 de espaldas a la lámpara; brillo especular 0..1)."""
    ndl = np.clip(np.tensordot(n, L, axes=([-1], [0])), 0, 1); nh = np.clip(np.tensordot(n, HALF, axes=([-1], [0])), 0, 1)
    return ndl / L[2], (nh ** dureza) * brillo

def almohada_altura(dpx, ancho_px, alto_px):
    """La altura de una almohada: sube de 0 en el borde a [alto_px] a [ancho_px] píxeles adentro con un cuarto de círculo (el canto redondo) y sigue plana."""
    t = np.clip(dpx / ancho_px, 0, 1); return alto_px * np.sqrt(1 - (1 - t) ** 2)

def domo_altura(alfa, sigma_px, alto_px):
    """La altura de una almohada como la silueta DESENFOCADA (sin las aristas diagonales de la distancia al borde: en las esquinas y en los cuellos de la pieza la cara queda lisa, como plasticina inflada): 0 en el borde, [alto_px] adentro."""
    b = blur_fft(alfa, sigma_px); u = np.clip((b - 0.5) * 2.0, 0, 1) * (alfa > 0.001)
    return alto_px * np.sqrt(np.clip(1 - (1 - u) ** 2, 0, 1))

def cara_pieza(d, bp=BP, semilla=7, ancho_dp=15.0, alto=1.0, grano_amp=0.020, asentada=False):
    """La CARA de la pieza (RGBA premultiplicado, fila 0 ARRIBA) con la silueta [d]: almohada con el canto redondo, la lámpara fija (brillo arriba a la izquierda, sombra abajo a la derecha), grano fino isótropo y celeste cálido saturado."""
    a = alfa_de(d, bp); dpx = np.maximum(-d, 0) * bp
    w = ancho_dp * DP; h = domo_altura(a, w * 0.5, alto * w)
    n = normales(h, 1.2); difuso, spec = lampara(n, 0.20, 12.0)
    ao = 1.0 - 0.14 * np.clip(1 - dpx / (w * 0.55), 0, 1)                    # la plasticina asentada: un poco más oscuro en el filo
    t = np.clip(0.60 + 0.28 * (difuso - 1.0) * (1.0 if not asentada else 0.8), 0.40, 1.0) * ao + spec * 0.55        # el piso de 0,40: el lado de la sombra sigue siendo celeste (no se funde con el cielo)
    g = grano(a.shape, semilla, grano_amp, 0.9, 0.024)
    rgb = rampa(RAMPA_PIEZA, np.clip(t, 0, 1) * (0.94 + 0.06 * g))
    rgb = np.clip(rgb * (0.985 + 0.015 * g)[..., None], 0, 1)
    return np.concatenate([rgb * a[..., None], a[..., None]], axis=2)

COLOR_CANTO = np.array([40, 104, 176]) / 255.0

def pieza(P, bp=BP, angulo=0.0, flip=False, canto_dp=5.0, sombra=True, d=None, semilla=7, **kw):
    """El objeto de plasticina: sombra de contacto + canto de [canto_dp] hacia abajo (en la PANTALLA) + la cara. Devuelve (RGBA premultiplicado fila 0 ARRIBA, alfa de la cara, d)."""
    d = d if d is not None else sdf_pantalla(P, bp, angulo, flip)
    cara = cara_pieza(d, bp, semilla, **kw); alfa = cara[..., 3]
    if canto_dp <= 0 and not sombra: return cara, alfa, d
    sh, cn = canto_y_sombra(alfa, canto_dp * DP, COLOR_CANTO, sombra_alfa=0.50 if sombra else 0.0)
    if canto_dp <= 0: cn = np.zeros_like(cn)
    return over(over(sh, cn), cara), alfa, d

# ------------------------------------------------------------------ la fuga del espejo con la lámpara
def fuga_espejo(P, bp=BP, angulo=225.0):
    """La pieza espejada EN PANTALLA contra la de celdas reflejadas sin espejar (la misma silueta por dos caminos): la diferencia máxima del color de la cara sobre 255. La lámpara es fija y solo ve la silueta, así que es 0."""
    a = pieza(P, bp, angulo, True, canto_dp=0, sombra=False)[0]
    Pm = normalize([(-x, y) for x, y in P])                                    # las celdas reflejadas
    b = pieza(Pm, bp, angulo, False, canto_dp=0, sombra=False)[0]
    return float(np.abs(a - b).max() * 255.0), a, b

def fuga_luz_pegada(P, bp=BP, angulo=225.0):
    """Lo que pasa si la luz se hornea en el marco de la pieza y el espejo se hace con la escala x = −1: el brillo cae al lado contrario. Devuelve (diferencia máxima sobre 255, desplazamiento horizontal del centro del brillo en px) entre la cara con la luz fija en pantalla y la que sale de espejar la cara horneada sin espejar."""
    fija = cara_pieza(sdf_pantalla(P, bp, angulo, True), bp)
    sin = cara_pieza(sdf_pantalla(P, bp, angulo, False), bp)
    pegada = sin[:, ::-1]                                                      # la cara horneada del lado sin espejar, volteada como lo haría una escala x = −1
    # (la silueta de `pegada` coincide con la de `fija` solo si la pieza es simétrica; se compara el CENTRO DE LUZ de cada una)
    def centro_brillo(c):
        y = lum(c[..., :3]); m = y > np.percentile(y[c[..., 3] > 0.5], 96); ys, xs = np.where(m); return xs.mean(), ys.mean()
    cf, cp = centro_brillo(fija), centro_brillo(pegada)
    return cf[0] - cp[0], cf, cp

# ------------------------------------------------------------------ el puerto: cavidad con cordón
VERDE_AGUA = np.array([92, 200, 194]) / 255.0; LIMA = np.array([166, 227, 107]) / 255.0; CELESTE = np.array([127, 216, 255]) / 255.0
ESTADOS = {'reposo': VERDE_AGUA, 'ok': LIMA, 'mostrar': CELESTE}
FONDO_HUECO = np.array([7, 8, 24]) / 255.0; PARED_HUECO = np.array([112, 96, 224]) / 255.0

def rampa_color(col):
    """La rampa de un color de estado (verde agua, lima, celeste): sombra profunda más saturada, el color, la luz y el brillo."""
    c = np.asarray(col, float); hsv = rgb_a_hsv(c[None, None, :]); prof = hsv.copy(); prof[..., 1] = np.clip(prof[..., 1] * 1.15 + 0.05, 0, 1); prof[..., 2] *= 0.30
    luz = hsv.copy(); luz[..., 1] *= 0.55; luz[..., 2] = np.clip(luz[..., 2] * 1.12, 0, 1)
    p0 = hsv_a_rgb(prof)[0, 0]; pl = hsv_a_rgb(luz)[0, 0]
    return [(0.0, tuple(p0 * 255)), (0.35, tuple(c * 0.62 * 255)), (0.62, tuple(c * 255)), (0.86, tuple(pl * 255)), (1.0, (255, 255, 255))]

def cordon_y_cavidad(d, bp=BP, estado='reposo', cordon_dp=4.6, pared_dp=6.0, semilla=31):
    """La cavidad del puerto vista en pantalla para la silueta [d] (la abertura es d ≤ 0): (cordón, cavidad, abertura) como RGBA premultiplicados, fila 0 ARRIBA.
      · el cordón: un TUBO de plasticina de [cordon_dp] de ancho justo por fuera de la abertura (verde agua / lima / celeste), iluminado por la lámpara: luz en el lomo de arriba-izquierda, sombra en el de abajo-derecha;
      · la cavidad: una pared inclinada de [pared_dp] y el fondo oscuro; la lámpara ilumina la pared de ABAJO a la derecha (mira hacia la luz) y deja en sombra la de ARRIBA a la izquierda, que además tira su sombra sobre el fondo."""
    col = ESTADOS[estado]; ab = alfa_de(d, bp, 0.0)                                  # la abertura
    w_c = cordon_dp * DP; dpx_out = np.maximum(d, 0) * bp
    ac = alfa_de(d, bp, w_c / bp); anillo = np.clip(ac - ab, 0, 1)                   # la silueta engordada menos la abertura
    u = np.clip(dpx_out / w_c, 0, 1); hc = w_c * 0.95 * np.sqrt(np.clip(1 - (2 * u - 1) ** 2, 0, 1)) * (ac > 0.001)
    din = np.maximum(-d, 0) * bp; pw = pared_dp * DP
    s = np.clip(din / pw, 0, 1); s = s * s * (3 - 2 * s); prof = -pw * 1.4 * s
    h = np.where(d > 0, hc, prof)
    n = normales(h, 0.8); dif, spec = lampara(n, 0.6, 30.0)
    g = grano(d.shape, semilla, 0.012, 0.9, 0.02)
    t = np.clip(0.60 + 0.45 * (dif - 1.0), 0.0, 1.0) + spec
    cor_rgb = np.clip(rampa(rampa_color(col), np.clip(t, 0, 1)) * g[..., None], 0, 1)
    cor = np.concatenate([cor_rgb * anillo[..., None], anillo[..., None]], axis=2)
    # la cavidad: pared violeta (iluminada abajo-derecha, en sombra arriba-izquierda) y fondo oscuro
    wall = np.clip(1 - din / (pw * 1.05), 0, 1)
    tw = np.clip(0.30 + 0.85 * dif, 0.0, 1.6)
    pared = np.clip(PARED_HUECO[None, None, :] * tw[..., None], 0, 1)
    piso = np.broadcast_to(FONDO_HUECO[None, None, :], pared.shape)
    mezcla = (np.clip(wall * 1.15, 0, 1) ** 0.9)[..., None]
    interior = pared * mezcla + piso * (1 - mezcla)
    sombra = np.clip(ab - shift(ab, int(round(4.6 * DP)), int(round(3.8 * DP))), 0, 1) * (din > pw * 0.5)
    interior = interior * (1 - 0.55 * blur_fft(sombra, 2.2)[..., None])
    cav = np.concatenate([interior * ab[..., None], ab[..., None]], axis=2)
    return cor, cav, ab

def puerto(P, bp=BP, angulo=0.0, flip=False, estado='reposo', **kw):
    """El hueco completo (cordón sobre la cavidad) para la pieza [P] vista como se muestra el hueco. (RGBA premultiplicado, abertura alfa, d)."""
    d = sdf_pantalla(P, bp, angulo, flip)
    cor, cav, ab = cordon_y_cavidad(d, bp, estado, **kw)
    return over(cav, cor), ab, d

def pieza_asentada(P, bp=BP, angulo=0.0, flip=False, estado='ok', hundida=1.0, **kw):
    """La pieza metida en el hueco (la misma escala que la abertura): sin canto ni sombra (queda a ras del fondo), con la pared de arriba-izquierda echándole una sombra de [hundida] × 4 dp y el cordón del estado por encima. (RGBA premultiplicado, abertura alfa)."""
    d = sdf_pantalla(P, bp, angulo, flip)
    cor, cav, ab = cordon_y_cavidad(d, bp, estado, **kw)
    cara = cara_pieza(d, bp, asentada=True)
    # la sombra de la pared sobre la pieza: la abertura corrida hacia abajo-derecha deja una banda a lo largo de los bordes de arriba y de la izquierda
    banda = np.clip(ab - shift(ab, int(round(4.0 * DP * hundida)), int(round(3.2 * DP * hundida))), 0, 1)
    cara[..., :3] = cara[..., :3] * (1 - 0.55 * blur(banda, 1.6)[..., None])
    return over(cav, over(cara, cor)), ab
