# -*- coding: utf-8 -*-
"""Las mediciones que comparten los juegos con símbolos de silueta (`mide_radar.py`, `mide_piloto.py`): la silueta de HOY (la SDF de ClayRaster con su borde tinta) para comparar la huella, y el contraste WCAG.
Solo numpy: no necesita Blender ni Unity."""
import math
import numpy as np

LIENZO = 2.36                                   # ±1,18 unidades de diseño (Zoom 1,18 de RadarSprites y PilotSignalSprites)

def hexrgb(h): return np.array([int(h.lstrip('#')[i:i + 2], 16) for i in (0, 2, 4)], float)

def lum(rgb):
    c = np.asarray(rgb, float) / 255.0; c = np.where(c <= 0.03928, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    return 0.2126 * c[..., 0] + 0.7152 * c[..., 1] + 0.0722 * c[..., 2]

def contraste(a, b):
    la, lb = lum(a), lum(b); return (max(la, lb) + 0.05) / (min(la, lb) + 0.05)

def malla(n, lienzo=LIENZO):
    """Las coordenadas de diseño (y arriba) de cada píxel de un lienzo de [n] × [n]."""
    yy, xx = np.mgrid[0:n, 0:n]
    return ((xx + 0.5) / n * 2 - 1) * (lienzo / 2), -(((yy + 0.5) / n * 2 - 1) * (lienzo / 2))

def sdf_poligono(pts, X, Y):
    pts = np.array(pts, float); n = len(pts); d = np.full(X.shape, 1e9)
    for i in range(n):
        a, b = pts[i], pts[(i + 1) % n]; e = b - a
        t = np.clip(((X - a[0]) * e[0] + (Y - a[1]) * e[1]) / (e @ e), 0, 1)
        d = np.minimum(d, np.hypot(X - a[0] - e[0] * t, Y - a[1] - e[1] * t))
    dentro = np.zeros(X.shape, bool)                                                         # trazado de rayos
    for i in range(n):
        a, b = pts[i], pts[(i + 1) % n]
        cond = ((a[1] > Y) != (b[1] > Y)) & (X < (b[0] - a[0]) * (Y - a[1]) / (b[1] - a[1] + 1e-12) + a[0])
        dentro ^= cond
    return np.where(dentro, -d, d)

def sdf_capsula(X, Y, c0, c1):
    """La envolvente de dos círculos (cx, cy, r): la gota y la llama (el radio varía lineal entre los centros)."""
    t = np.clip((Y - c0[1]) / (c1[1] - c0[1]), 0, 1)
    return np.hypot(X, Y - (c0[1] + (c1[1] - c0[1]) * t)) - (c0[2] + (c1[2] - c0[2]) * t)

def sdf_forma(tipo, X, Y):
    """La SDF de cada forma (RadarSprites.ShapeSdf = PilotSignalSprites.ShapeSdf), sin el borde."""
    if tipo == 'hexagono': return sdf_poligono([(0.9 * math.cos(math.pi / 6 + i * math.pi / 3), 0.9 * math.sin(math.pi / 6 + i * math.pi / 3)) for i in range(6)], X, Y) - 0.05
    if tipo == 'circulo': return np.hypot(X, Y) - 0.92
    if tipo == 'cuadrado':
        qx, qy = np.abs(X) - 0.74 + 0.22, np.abs(Y) - 0.74 + 0.22
        return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - 0.22
    if tipo == 'triangulo': return sdf_poligono([(0.0, 0.98), (0.98, -0.74), (-0.98, -0.74)], X, Y) - 0.08
    if tipo == 'rombo': return sdf_poligono([(0.0, 1.0), (0.84, 0.0), (0.0, -1.0), (-0.84, 0.0)], X, Y) - 0.06
    return sdf_capsula(X, Y, (0.0, -0.34, 0.66), (0.0, 0.96, 0.07))                            # gota

def silueta_hoy(tipo, X, Y, tinta=0.075):
    """La silueta VISIBLE de hoy: la forma más su borde de tinta (sin la sombra dura)."""
    return sdf_forma(tipo, X, Y) <= tinta

def caja(m):
    ys, xs = np.where(m); return (int(xs.max() - xs.min() + 1), int(ys.max() - ys.min() + 1))
