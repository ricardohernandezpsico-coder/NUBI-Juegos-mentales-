# -*- coding: utf-8 -*-
"""Medidas de las propuestas de volumen (Tarea 114, parte 2). Criterio: la precisión (silueta y medidas) manda; el volumen se suma, no la cambia.
ACOPLAMIENTO (camino 2):
  · IoU de la silueta de la CARA contra la silueta EXACTA (el SDF analítico a 4 × 4 sub-muestras, girada y espejada como en pantalla) y el IoU de lo que se ve con el canto;
  · fuga del espejo CON EL CANTO: el módulo compuesto de la pieza P espejada en pantalla contra el de la pieza espejada (celdas reflejadas) sin espejar; el compositor solo ve la silueta de pantalla (luz, canto y grano fijos y sin dirección), así que deben salir iguales;
  · contraste de la pieza contra el fondo (razón de contraste WCAG): cara y canto, hoy y propuesta.
ENGRANAJES:
  · legibilidad del hueco entre dientes con el canto detrás: por cada engranaje, el anillo entre la raíz y la punta de los dientes; razón de contraste entre el diente y el hueco, hoy y propuesta, en la mitad de arriba y en la de abajo (donde cae el canto), y la fracción de hueco que se sigue leyendo (razón ≥ 1,5).
Uso: python -I tools/juegos/propuesta_medidas.py aco | aco2 | eng <carpeta de tomas> <toma> | engr <carpeta de tomas del juego integrado> <toma>"""
import os, sys, math, json
import numpy as np
from PIL import Image
AQUI = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, AQUI)
sys.stdout.reconfigure(encoding='utf-8')
from propuesta_util import *

def luminancia_rel(rgb):
    c = np.where(rgb <= 0.03928, rgb / 12.92, ((rgb + 0.055) / 1.055) ** 2.4)
    return 0.2126 * c[..., 0] + 0.7152 * c[..., 1] + 0.0722 * c[..., 2]

def razon(l1, l2):
    a, b = max(l1, l2), min(l1, l2); return (a + 0.05) / (b + 0.05)

# ------------------------------------------------------------------ ACOPLAMIENTO
def acoplamiento():
    import propuesta_acoplamiento as pa
    from acopla_geo import normalize, mirror, pick_pieces, piece_sdf, piece_box, SIDE, PX
    L4 = pick_pieces()['L4']; P = normalize([(-x, -y) for x, y in L4]); Pm = mirror(P)
    cara = pa.perfil_cara(P); cara_m = pa.perfil_cara(Pm)
    A, alfa = pa.compone_modulo(cara, P, 225.0, True)
    B, alfa_b = pa.compone_modulo(cara_m, Pm, 225.0, False)
    # --- fuga del espejo con el canto: la pieza espejada en pantalla contra la pieza con las celdas reflejadas
    m = (A[..., 3] > 0.01) | (B[..., 3] > 0.01)
    dif = np.abs(A - B)[m]
    print('FUGA DEL ESPEJO (con canto y sombra): la pieza espejada en pantalla vs la de celdas reflejadas sin espejar')
    print('  diferencia máxima %.1e / 255, media %.1e / 255 (sobre %d píxeles; el alfa de la cara difiere en %.1e)' % (dif.max() * 255, dif.mean() * 255, m.sum(), np.abs(alfa - alfa_b).max()))
    # --- IoU de la silueta de la cara contra la exacta (SDF analítico, a 4 × 4 sub-muestras) con el mismo giro y espejo
    n = alfa.shape[0]; ss = 4; scale = pa.BP / PX; th = math.radians(225.0); c, s_ = math.cos(th), math.sin(th)
    ys, xs = np.mgrid[0:n * ss, 0:n * ss]; X = (xs + 0.5) / ss - n / 2.0; Y = (ys + 0.5) / ss - n / 2.0             # píxeles de pantalla, Y hacia ARRIBA (fila 0 abajo)
    xi = (X * c + Y * s_) / scale; yi = (-X * s_ + Y * c) / scale; xi = -xi                                         # flip
    ccx, ccy, cw, ch = piece_box(P)
    gx = ccx + xi / PX; gy = ccy + yi / PX
    d = piece_sdf(P, gx, gy)
    print('IoU DE LA SILUETA DE LA CARA contra la exacta (d ≤ nivel), fila de abajo hacia arriba como la pantalla:')
    cara_px = (alfa[::-1] > 0.5)
    mejor = None
    for nivel in (-0.04, -0.02, 0.0, 0.02, 0.04):
        ex = (d <= nivel).astype(np.float64).reshape(n, ss, n, ss).mean(axis=(1, 3)) > 0.5
        iou = (ex & cara_px).sum() / max(1, (ex | cara_px).sum()); print('  nivel %+.2f bloque: IoU %.4f' % (nivel, iou)); mejor = max(mejor or 0, iou)
    # --- contraste contra el fondo
    real = np.asarray(Image.open(os.path.join(AQUI, 'out', 'propuesta', 'aco_hoy.png')).convert('RGB')).astype(np.float64) / 255.0
    prop = np.asarray(Image.open(os.path.join(AQUI, 'out', 'propuesta', 'aco_propuesta.png')).convert('RGB')).astype(np.float64) / 255.0
    def media(img, m): return img[m].mean(axis=0)
    # el fondo de alrededor de la pieza (el cielo que quedó libre), el cuerpo de hoy (el interior celeste) y la cara y el canto de la propuesta
    sub = (slice(860, 1340), slice(300, 780)); fondo = np.median(prop[sub].reshape(-1, 3)[(prop[sub].reshape(-1, 3).sum(axis=1) < 0.35)], axis=0)
    azul_hoy = (real[sub][..., 2] > 0.85) & (real[sub][..., 1] > 0.7) & (real[sub][..., 0] < 0.65)
    azul_cara = (prop[sub][..., 2] > 0.85) & (prop[sub][..., 1] > 0.75) & (prop[sub][..., 0] < 0.60)
    canto = (prop[sub][..., 2] > 0.35) & (prop[sub][..., 2] < 0.65) & (prop[sub][..., 0] < 0.30) & (prop[sub][..., 1] > 0.20)
    cl = lambda im, m: luminancia_rel(media(im[sub], m))
    lf = luminancia_rel(fondo)
    print('CONTRASTE (razón WCAG contra el fondo, fondo ≈ L %.3f):' % lf)
    print('  cuerpo de hoy (interior celeste): %.1f : 1' % razon(cl(real, azul_hoy), lf))
    print('  cara de la propuesta:             %.1f : 1' % razon(cl(prop, azul_cara), lf))
    if canto.sum() > 50: print('  canto de la propuesta:            %.1f : 1  (separa la pieza del fondo por abajo; contra la cara %.1f : 1)' % (razon(cl(prop, canto), lf), razon(cl(prop, canto), cl(prop, azul_cara))))

# ------------------------------------------------------------------ ACOPLAMIENTO v2 (las bolitas y la almohada)
def acoplamiento_v2():
    import propuesta_acoplamiento as pa
    from acopla_geo import normalize, mirror, pick_pieces
    L4 = pick_pieces()['L4']; P = normalize([(-x, -y) for x, y in L4]); Pm = mirror(P)
    cara = pa.perfil_cara(P); cara_m = pa.perfil_cara(Pm)
    A, alfa, cA = pa.compone_modulo_v2(cara, P, 225.0, True)
    B, alfa_b, cB = pa.compone_modulo_v2(cara_m, Pm, 225.0, False)
    A1, alfa1 = pa.compone_modulo(cara, P, 225.0, True)
    print('V2 · SILUETA: la cara de la v2 es la MISMA silueta que la de la v1 (diferencia máxima del alfa %.1e): las bolitas y la almohada van adentro' % np.abs(alfa - alfa1).max())
    m = (A[..., 3] > 0.01) | (B[..., 3] > 0.01); dif = np.abs(A - B)[m]
    cs_a = sorted((round(x, 3), round(y, 3)) for x, y in cA); cs_b = sorted((round(x, 3), round(y, 3)) for x, y in cB)
    print('V2 · FUGA DEL ESPEJO con bolitas, canto y sombras: la pieza espejada en pantalla vs la de celdas reflejadas sin espejar')
    print('  diferencia máxima %.1e / 255, media %.1e / 255 (%d píxeles); los centros de las bolitas coinciden: %s' % (dif.max() * 255, dif.mean() * 255, m.sum(), max(abs(a[0] - b[0]) + abs(a[1] - b[1]) for a, b in zip(cs_a, cs_b)) < 1e-6))
    # --- contraste de la bolita contra la cara, a 78 y a 47 u por bloque (y cuánto mide la bolita)
    print('V2 · CONTRASTE de la bolita contra la cara (razón de luminancia WCAG) y tamaño')
    for bp in (78, 47):
        capa, alfa_p, cs = pa.compone_modulo_v2(cara, P, 225.0, True, bp=bp)
        al = capa[..., 3]; rgb = np.where(al[..., None] > 1e-4, capa[..., :3] / np.maximum(al[..., None], 1e-4), 0.0)
        r = 0.33 * bp; yy, xx = np.mgrid[0:al.shape[0], 0:al.shape[1]]
        rho_min = np.full(al.shape, 9.0); centro = np.zeros(al.shape, bool); anillo = np.zeros(al.shape, bool)
        for (cx, cy) in cs:
            rho = np.hypot(xx - cx, yy - cy) / r; rho_min = np.minimum(rho_min, rho)
            centro |= rho < 0.25; anillo |= (rho > 0.78) & (rho < 0.95)
        cara_ref = (alfa_p > 0.99) & (rho_min > 1.7) & (distancia_adentro(alfa_p) > 0.10 * bp)
        lc, la, lf = (luminancia_rel(rgb[mk].mean(axis=0)) for mk in (centro, anillo, cara_ref))
        print('  bloque de %d u: bolita de %.0f u de diámetro (%.1f dp); centro/cara %.2f : 1, filo/cara %.2f : 1 (la mayor %.2f : 1; pedido ≥ 1,3 : 1 o relieve claro)' % (bp, 2 * r, 2 * r / 3.0, razon(lc, lf), razon(la, lf), max(razon(lc, lf), razon(la, lf))))

# ------------------------------------------------------------------ ENGRANAJES
def engranajes(d, toma, real=False):
    """[real] = el cuadro REAL del juego integrado (con canto) contra el mismo cuadro SIN canto (`NN-<toma>-sincanto.png`, que saca el gancho temporal): lo que hace el canto y nada más. Si no, la hoja de la propuesta (hoy vs composición)."""
    import propuesta_engranajes as pe
    if real:
        T = pe.Toma(d, toma); P = T.F.astype(np.float64)
        F = pe.carga(pe.Toma.__init__.__globals__['glob'].glob(os.path.join(d, '*-' + toma + '-sincanto.png'))[0]).astype(np.float64)
    else:
        img, T = pe.propuesta(d, toma)
        F = T.F.astype(np.float64); P = img
    print('LEGIBILIDAD DEL HUECO ENTRE DIENTES (anillo raíz–punta de cada engranaje; razón de contraste diente/hueco, hoy → propuesta)')
    filas = []; cache = {idx: T.capa(idx) for idx, nm in T.grupos}
    for idx, nm in T.grupos:
        if not nm.startswith('M.Gear'): continue
        r = cache[idx]
        if r is None: continue
        L, (x0, y0) = r; a = L[..., 3]
        ys, xs = np.where(a > 0.5)
        if ys.size < 1500: continue
        cy, cx = ys.mean(), xs.mean()
        th = np.linspace(0, 2 * math.pi, 720, endpoint=False); rmax = np.zeros_like(th)
        for i, t in enumerate(th):
            rr = np.arange(8, 120, 0.5); yy = (cy - rr * np.sin(t)).round().astype(int); xx = (cx + rr * np.cos(t)).round().astype(int)
            ok = (yy >= 0) & (yy < a.shape[0]) & (xx >= 0) & (xx < a.shape[1]); v = np.zeros(rr.shape, bool); v[ok] = a[yy[ok], xx[ok]] > 0.5
            rmax[i] = rr[np.where(v)[0].max()] if v.any() else 0
        tip, root = np.percentile(rmax, 95), np.percentile(rmax, 5)
        if tip - root < 6: continue
        Y, X = np.mgrid[0:a.shape[0], 0:a.shape[1]]; rad = np.hypot(X - cx, Y - cy)
        anillo = (rad > root + 2) & (rad < tip - 2)
        diente = anillo & (a > 0.9); hueco = anillo & (a < 0.1)
        # solo los huecos que no están tapados por otro engranaje o por una correa: el hueco del exacto está vacío en la capa de ESTE engranaje, pero otro puede entrar; se excluye lo que otro objeto cubre
        otros = np.zeros(a.shape, bool)
        for idx2, nm2 in T.grupos:
            if nm2 == nm or not nm2.startswith('M.') or nm2 in ('M.Soft', 'M.Canto'): continue
            r2 = cache[idx2]
            if r2 is None: continue
            L2, (xx0, yy0) = r2; ox, oy = xx0 - x0, yy0 - y0
            hh, ww = L2.shape[:2]; ya, yb = max(0, oy), min(a.shape[0], oy + hh); xa, xb = max(0, ox), min(a.shape[1], ox + ww)
            if ya < yb and xa < xb: otros[ya:yb, xa:xb] |= L2[ya - oy:yb - oy, xa - ox:xb - ox, 3] > 0.05
        hueco &= ~otros; diente &= ~otros
        if hueco.sum() < 80 or diente.sum() < 80: continue
        def regiones(img):
            sub = img[y0:y0 + a.shape[0], x0:x0 + a.shape[1]]
            res = {}
            for nombre, mitad in (('arriba', Y < cy), ('abajo', Y >= cy)):
                dm, hm = diente & mitad, hueco & mitad
                if dm.sum() < 20 or hm.sum() < 20: res[nombre] = None; continue
                ld = luminancia_rel(np.clip(sub[dm].mean(axis=0), 0, 1)); lh = luminancia_rel(np.clip(sub[hm].mean(axis=0), 0, 1))
                lum_h = luminancia_rel(np.clip(sub[hm], 0, 1)); lum_d = ld
                legible = np.mean([(razon(ld, v) >= 1.5) for v in lum_h])
                res[nombre] = (razon(ld, lh), legible)
            return res
        h, p = regiones(F), regiones(P)
        filas.append((nm, h, p, tip, root))
        txt = '  %-9s raíz %.0f px, punta %.0f px | ' % (nm[2:], root, tip)
        for k in ('arriba', 'abajo'):
            if h[k] and p[k]: txt += '%s %.1f→%.1f : 1 (hueco legible %.0f %%→%.0f %%)  ' % (k, h[k][0], p[k][0], h[k][1] * 100, p[k][1] * 100)
        print(txt)
    if filas:
        for k in ('arriba', 'abajo'):
            hs = [f[1][k][0] for f in filas if f[1][k] and f[2][k]]; ps = [f[2][k][0] for f in filas if f[1][k] and f[2][k]]
            lg = [f[2][k][1] for f in filas if f[1][k] and f[2][k]]
            if hs: print('  mitad de %-6s: mediana hoy %.1f : 1 → propuesta %.1f : 1; hueco legible mínimo %.0f %%' % (k, np.median(hs), np.median(ps), min(lg) * 100))

if __name__ == '__main__':
    if sys.argv[1] == 'aco': acoplamiento()
    elif sys.argv[1] == 'aco2': acoplamiento_v2()
    elif sys.argv[1] == 'engr': engranajes(sys.argv[2], sys.argv[3], real=True)
    else: engranajes(sys.argv[2], sys.argv[3])
