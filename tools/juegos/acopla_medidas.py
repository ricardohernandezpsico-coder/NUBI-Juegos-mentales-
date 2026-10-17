# -*- coding: utf-8 -*-
"""Acoplamiento — las medidas del estudio (Tarea 113, fase A). Funciones puras que reciben sprites (384 × 384, premultiplicados, fila 0 abajo) y devuelven números."""
import math
import numpy as np
from acopla_geo import *
from acopla_caminos import luma

def blur(a, sigma):
    """Desenfoque gaussiano separable (a: 2-D o h × w × c)."""
    r = int(math.ceil(sigma * 3)); k = np.exp(-0.5 * (np.arange(-r, r + 1) / sigma) ** 2); k /= k.sum()
    def pasa(x, eje):
        pad = [(0, 0)] * x.ndim; pad[eje] = (r, r); xp = np.pad(x, pad, mode='edge'); out = np.zeros_like(x)
        for i, w in enumerate(k): out += w * np.take(xp, np.arange(i, i + x.shape[eje]), axis=eje)
        return out
    return pasa(pasa(a, 0), 1)

def vertices_concavos(cells):
    """Los vértices cóncavos (en coordenadas de bloque): los de la rejilla con exactamente 3 de los 4 bloques de alrededor ocupados."""
    s = set(cells); out = []
    for x in range(-1, 5):
        for y in range(-1, 5):
            n = sum(((x + dx, y + dy) in s) for dx in (0, 1) for dy in (0, 1))
            if n == 3: out.append((x + 0.5, y + 0.5))
    return out

def muestrea_alfa(alfa, ns):
    """El alfa de un sprite de 384 px visto a [ns] px (mismo cuadro de 4,8 bloques) con muestreo bilineal sin prefiltro (la GPU)."""
    ys, xs = np.mgrid[0:ns, 0:ns]; X = (xs + 0.5) / ns * SIZE - 0.5; Y = (ys + 0.5) / ns * SIZE - 0.5
    return bilinear(alfa[..., None], X, Y)[..., 0]

def cruces(A, thr=0.5):
    """Los puntos (x, y en px, centro del píxel = índice) donde el alfa [A] cruza [thr], por interpolación lineal entre píxeles vecinos (precisión de sub-píxel)."""
    a = A - thr
    ii, jj = np.nonzero(a[:, :-1] * a[:, 1:] < 0); t = a[ii, jj] / (a[ii, jj] - a[ii, jj + 1]); xh = jj + t; yh = ii.astype(float)
    i2, j2 = np.nonzero(a[:-1, :] * a[1:, :] < 0); t2 = a[i2, j2] / (a[i2, j2] - a[i2 + 1, j2]); xv = j2.astype(float); yv = i2 + t2
    return np.concatenate([xh, xv]), np.concatenate([yh, yv])

def fidelidad(alfa, cells, nivel, bp=47):
    """La silueta de un sprite contra el SDF (d ≤ nivel) a [bp] px por bloque: IoU (Σ mín / Σ máx contra la cobertura EXACTA) y el desvío del borde en px: la distancia del SDF en cada punto donde el alfa cruza 0,5 (interpolado), en general y cerca de los vértices cóncavos."""
    ns = int(round(SIDE * bp)); ac = muestrea_alfa(alfa, ns); ag = exact_coverage(cells, nivel, ns)
    iou = float(np.minimum(ac, ag).sum() / np.maximum(ac, ag).sum())
    xs, ys = cruces(ac); ccx, ccy, w, h = piece_box(cells)
    xb = ccx + ((xs + 0.5) / ns - 0.5) * SIDE; yb = ccy + ((ys + 0.5) / ns - 0.5) * SIDE
    err = np.abs(piece_sdf(cells, xb, yb) - nivel) * bp
    cerca = np.zeros(err.shape, bool)
    for (vx, vy) in vertices_concavos(cells): cerca |= ((xb - vx) ** 2 + (yb - vy) ** 2) < 0.45 ** 2
    return {'iou': iou, 'max': float(err.max()), 'p99': float(np.percentile(err, 99)), 'media': float(err.mean()),
            'max_conc': float(err[cerca].max()) if cerca.any() else None, 'n_conc': len(vertices_concavos(cells))}

def espejo(sprite): return sprite[:, ::-1]

def fuga_espejo(S, Sm, cells):
    """Fuga del espejo: [S] es el sprite de la pieza y [Sm] el de su pieza ESPEJADA hecho con el mismo camino. El juego muestra el espejo como el sprite con escala x −1: se compara ese (`espejo(S)`) con [Sm].
    Devuelve el alfa máximo distinto, la diferencia de luz (en niveles de 255) con un desenfoque de 2,5 px —máxima y media dentro del cuerpo— y la modulación DIRECCIONAL del canto (primer y segundo armónico del azimut de la normal, en niveles) de [S]."""
    F = espejo(S); da = float(np.abs(Sm[..., 3] - F[..., 3]).max())
    ok = (blur(F[..., 3], 1.0) > 0.99) & (blur(Sm[..., 3], 1.0) > 0.99)
    lf = blur(luma(F) * 255.0 * (F[..., 3] > 0.5), 2.5); lm = blur(luma(Sm) * 255.0 * (Sm[..., 3] > 0.5), 2.5)
    interior = blur(ok.astype(float), 3.0) > 0.99
    dl = np.abs(lf - lm)[interior]
    return {'alfa': da, 'luz_max': float(dl.max()) if dl.size else 0.0, 'luz_media': float(dl.mean()) if dl.size else 0.0}

def asimetria_lr(S):
    """La asimetría izquierda-derecha de la LUZ de una pieza simétrica (el cuerpo [S] de una pieza igual a su espejo): |L(S) − L(espejo(S))| con un desenfoque de 1,5 px, dentro del cuerpo, en niveles de 255. Si es ≠ 0 el sprite lleva una luz o un relieve con dirección, y al espejarlo (escala x −1) cambia de lado y se delata."""
    L = blur(luma(S) * 255.0 * (S[..., 3] > 0.5), 1.5); Lm = L[:, ::-1]
    interior = (blur((S[..., 3] > 0.99).astype(float), 2.0) > 0.99) & (blur((S[:, ::-1, 3] > 0.99).astype(float), 2.0) > 0.99)
    d = np.abs(L - Lm)[interior]
    return float(d.max()), float(d.mean())

def direccionalidad(S, cells, banda=(-0.14, -0.02)):
    """La modulación direccional del canto: la luminosidad (0..255) de los píxeles de una banda DENTRO del contorno, según el azimut de la normal exterior (16 casilleros). Devuelve la amplitud del primer armónico (luz con dirección) y del segundo."""
    gx, gy = grid(cells); d = piece_sdf(cells, gx, gy); gyy, gxx = np.gradient(d); phi = np.arctan2(gyy, gxx)
    sel = (d > banda[0]) & (d < banda[1]) & (S[..., 3] > 0.99)
    L = luma(S)[sel] * 255.0; ph = phi[sel]; L = L - L.mean()
    a1 = 2 * abs((L * np.exp(1j * ph)).mean()); a2 = 2 * abs((L * np.exp(2j * ph)).mean())
    return float(a1), float(a2), float(luma(S)[sel].mean() * 255.0)

def salto(S, linea_x=None, linea_y=None, ancho=1):
    """El salto de luz (media de |ΔL| entre píxeles vecinos, en niveles de 255) a ambos lados de una línea vertical x = [linea_x] (px del sprite) u horizontal y = [linea_y], solo dentro del cuerpo (lejos de todo borde)."""
    L = luma(S) * 255.0; a = S[..., 3] > 0.99
    interior = blur(a.astype(float), 6.0) > 0.99
    if linea_x is not None:
        i = int(round(linea_x)); dif = np.abs(L[:, i + ancho] - L[:, i - ancho - 1 + 1]); ok = interior[:, i + ancho] & interior[:, i - ancho]
        return float(dif[ok].mean()) if ok.any() else None
    j = int(round(linea_y)); dif = np.abs(L[j + ancho, :] - L[j - ancho, :]); ok = interior[j + ancho, :] & interior[j - ancho, :]
    return float(dif[ok].mean()) if ok.any() else None

def fondo_saltos(S):
    """El salto «de fondo»: media de |L(x+2) − L(x−2)| en TODO el interior lejos del borde (en niveles de 255): lo que cambia la luz sin que haya una junta."""
    L = luma(S) * 255.0; interior = blur((S[..., 3] > 0.99).astype(float), 6.0) > 0.99
    dif = np.abs(L[:, 4:] - L[:, :-4]); ok = interior[:, 4:] & interior[:, :-4]
    return float(dif[ok].mean()) if ok.any() else 0.0

def centro_en_cruz(S, cells):
    """El «+»: en cada punto donde se encuentran los vértices de 4 bloques de la pieza (un 2 × 2), la luz de un disco de 5 px menos la de un anillo de 9 a 13 px (en niveles de 255). Devuelve la lista (una por cada 2 × 2)."""
    s = set(cells); ccx, ccy, w, h = piece_box(cells); L = luma(S) * 255.0; out = []
    ys, xs = np.mgrid[0:SIZE, 0:SIZE]
    for (x, y) in cells:
        if {(x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)} <= s:
            px = SIZE / 2 + (x + 0.5 - ccx) * PX; py = SIZE / 2 + (y + 0.5 - ccy) * PX
            r = np.sqrt((xs + 0.5 - px) ** 2 + (ys + 0.5 - py) ** 2)
            out.append(float(L[r < 5].mean() - L[(r > 9) & (r < 13)].mean()))
    return out

def sigma_rgb(a): return luma(a)
