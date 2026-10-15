# -*- coding: utf-8 -*-
"""Las dos capas de la roca de Lluvia de meteoros que NO son plasticina (Tarea 105): la BRASA del borde que va por delante (luz: naranja-coral al rojo vivo, también fuera del contorno) y las GRIETAS de la roca de polvo (la marca de «esa no existía»).
Se pintan con numpy sobre el mismo lienzo que el render de Blender (`meteoros.py`: 2,5 × 1,9 unidades, hoy a 120 px por unidad = 300 × 228 px) y la misma silueta (el contorno de `RockSdf`), así que caen donde la roca. Son del tamaño FINAL: `empaqueta.py` no las reduce.
Uso: python -I tools/juegos/meteoros_capas.py        (deja `tools/juegos/out/meteoros/brasa_0..3.png` y `grietas_0..3.png`)
"""
import math, os, sys
import numpy as np
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.join(AQUI, 'out', 'meteoros')
os.makedirs(SALIDA, exist_ok=True)
FASES = ((0.3, 1.9, 4.1, 2.2), (2.2, 0.4, 1.3, 5.1), (4.6, 3.1, 0.7, 3.3), (1.1, 5.2, 2.6, 0.9))
LIENZO_W, LIENZO_H, PX_U = 2.5, 1.9, 120
DESPLAZA_Y = -0.09          # la silueta de la pieza (de la cara de arriba al pie) cae entre la cara de arriba (−0,065) y la huella en el suelo (−0,125): se pinta a medio camino
EMBER = np.array([0.55, -0.83]); EMBER /= np.linalg.norm(EMBER)           # hacia dónde cae el meteoro: abajo a la derecha (MeteorSprites.EmberX/EmberY)
CORAL = np.array([0xFF, 0x6B, 0x4A], float); SOL = np.array([0xFF, 0xC9, 0x3C], float); NARANJA = np.array([0xFF, 0x8A, 0x3D], float); TINTA = np.array([0x1A, 0x12, 0x40], float)
GRIETAS = (((-0.62, 0.36), (-0.46, 0.12)), ((-0.46, 0.12), (-0.58, -0.10)), ((-0.58, -0.10), (-0.42, -0.34)), ((0.50, 0.40), (0.64, 0.14)), ((0.64, 0.14), (0.50, -0.08)), ((0.50, -0.08), (0.66, -0.34)))      # MeteorSprites.Cracks

def contorno(s, n=720):
    ph = FASES[s]; pts = []
    for i in range(n):
        a = 2 * math.pi * i / n
        w = 1 + 0.07 * math.sin(3 * a + ph[0]) + 0.045 * math.sin(5 * a + ph[1]) + 0.03 * math.sin(9 * a + ph[2]) + 0.015 * math.sin(14 * a + ph[3])
        r = w / math.sqrt(math.cos(a) ** 2 / 0.81 + math.sin(a) ** 2 / 0.3844)
        pts.append((r * math.cos(a), r * math.sin(a)))
    return np.array(pts)

def malla():
    W, H = int(round(LIENZO_W * PX_U)), int(round(LIENZO_H * PX_U))
    xs = ((np.arange(W) + 0.5) / PX_U) - LIENZO_W / 2
    ys = (LIENZO_H / 2 - (np.arange(H) + 0.5) / PX_U) - DESPLAZA_Y
    X, Y = np.meshgrid(xs, ys)
    return X, Y, W, H

def ruido(X, Y, k):
    """Ruido de valores suave y barato (para que la brasa no sea pareja)."""
    def h(ix, iy): v = np.sin(ix * 127.1 + iy * 311.7) * 43758.5453; return v - np.floor(v)
    x, y = X * k, Y * k; ix, iy = np.floor(x), np.floor(y); fx, fy = x - ix, y - iy
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a, b, c, d = h(ix, iy), h(ix + 1, iy), h(ix, iy + 1), h(ix + 1, iy + 1)
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy

def distancia_firmada(P, X, Y):
    """La distancia a la silueta (negativa adentro) y la normal hacia afuera del punto más cercano."""
    n = len(P); tang = np.roll(P, -1, axis=0) - np.roll(P, 1, axis=0); tang /= np.linalg.norm(tang, axis=1, keepdims=True)
    norm = np.stack([tang[:, 1], -tang[:, 0]], axis=1)                       # el contorno va antihorario: la normal hacia afuera es (ty, −tx)
    pts = np.stack([X.ravel(), Y.ravel()], axis=1).astype(np.float32); Pf = P.astype(np.float32)
    d_min = np.empty(len(pts), np.float32); idx = np.empty(len(pts), np.int32)
    for i in range(0, len(pts), 4096):
        d2 = ((pts[i:i + 4096, None, :] - Pf[None, :, :]) ** 2).sum(-1); j = d2.argmin(1); idx[i:i + 4096] = j; d_min[i:i + 4096] = np.sqrt(d2[np.arange(len(j)), j])
    # adentro o afuera: la normal del punto más cercano
    v = pts - Pf[idx]; dentro = (v * norm[idx].astype(np.float32)).sum(1) < 0
    return (np.where(dentro, -d_min, d_min)).reshape(X.shape), norm[idx].reshape(X.shape + (2,))

def brasa(s):
    X, Y, W, H = malla(); P = contorno(s)
    d, nrm = distancia_firmada(P, X, Y)
    facing = np.clip(nrm[..., 0] * EMBER[0] + nrm[..., 1] * EMBER[1], 0, 1)
    facing = np.clip(facing * 1.25 - 0.05, 0, 1); facing = facing * facing * (3 - 2 * facing)
    grano = ruido(X * 2, Y * 2, 1.0) * 0.6 + ruido(X * 2, Y * 2, 2.3) * 0.3 + ruido(X * 2, Y * 2, 5.0) * 0.1
    rgba = np.zeros((H, W, 4), float)
    # adentro: el borde al rojo vivo (de coral a sol según lo caliente)
    rim = np.clip(1 - (-d) / 0.20, 0, 1); calor = facing * rim * (0.75 + 0.5 * grano)
    mezcla = np.clip(calor * 1.3 - 0.25, 0, 1)[..., None]; hot = CORAL * (1 - mezcla) + SOL * mezcla
    a_in = np.clip(calor * 1.25, 0, 1) * (d < 0)
    # afuera: el resplandor, apagado antes del borde del lienzo
    borde = np.minimum.reduce([np.arange(W)[None, :] / PX_U + 0 * Y, (W - 1 - np.arange(W))[None, :] / PX_U + 0 * Y, np.arange(H)[:, None] / PX_U + 0 * X, (H - 1 - np.arange(H))[:, None] / PX_U + 0 * X])
    fade = np.clip(borde / 0.14, 0, 1)
    brillo = np.exp(-np.maximum(d, 0) * 9.0) * facing * 0.6 * fade * (d >= 0)
    glow = NARANJA * 0.6 + CORAL * 0.4
    a = a_in + brillo * (1 - a_in)
    col = (hot * a_in[..., None] + glow * (brillo * (1 - a_in))[..., None]) / np.maximum(a, 1e-4)[..., None]
    rgba[..., :3] = col; rgba[..., 3] = np.clip(a, 0, 1) * 255
    return Image.fromarray(np.clip(rgba, 0, 255).astype(np.uint8), 'RGBA')

def grietas(s):
    X, Y, W, H = malla(); P = contorno(s)
    d, _ = distancia_firmada(P, X, Y)
    dist = np.full(X.shape, 9.0)
    for (a, b) in GRIETAS:
        ax, ay = a; bx, by = b; dx, dy = bx - ax, by - ay; t = np.clip(((X - ax) * dx + (Y - ay) * dy) / (dx * dx + dy * dy), 0, 1)
        dist = np.minimum(dist, np.hypot(X - ax - dx * t, Y - ay - dy * t))
    cob = np.clip(0.5 - (dist - 0.022) / 0.012, 0, 1) * (d < -0.02)                 # el churro de la grieta, solo sobre la roca
    rgba = np.zeros((H, W, 4), float); rgba[..., :3] = TINTA; rgba[..., 3] = cob * 255
    return Image.fromarray(np.clip(rgba, 0, 255).astype(np.uint8), 'RGBA')

for s in range(4):
    brasa(s).save(os.path.join(SALIDA, f'brasa_{s}.png')); grietas(s).save(os.path.join(SALIDA, f'grietas_{s}.png')); print('capas de la forma', s)
