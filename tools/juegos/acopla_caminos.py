# -*- coding: utf-8 -*-
"""Acoplamiento — los DOS caminos del estudio (Tarea 113, fase A), con las placas maestras de `acoplamiento.py` (Blender).
  CAMINO 1 (sub-baldosas): cada bloque se parte en 4 cuartos; cada cuarto elige una de las baldosas (convexa, borde, cóncava, interior) según sus tres vecinos (derecha, arriba y la diagonal). Las baldosas salen de los renders
    (luz promediada en 4 giros y simetrizada con sus espejos: sin luz con dirección), 40 × 40 px a 80 px por bloque; las del aro del hueco llevan un margen de 12 px por fuera (el aro de 0,115 bloque).
  CAMINO 2 (SDF + perfil): el contorno sale EXACTO del SDF del juego (alfa analítico) y el color de una tabla 1-D (perfil del canto, medido en un borde recto del render) indexada por la distancia con signo: no depende de la dirección.
Todo devuelve sprites de 384 × 384 (80 px por bloque), premultiplicados, fila 0 abajo. El HUECO son DOS capas, como hoy: el aro (la silueta de d ≤ 0,115, debajo) y el fondo (d ≤ 0, encima)."""
import os, math
import numpy as np
from PIL import Image
from acopla_geo import *

PXM = 160                                        # px por bloque de la maestra
MARGEN = 12                                      # margen de las baldosas del aro (px a 80 por bloque)
Q = PX // 2                                      # un cuarto de bloque: 40 px

def luma(a):
    al = np.maximum(a[..., 3], 1e-6); return (0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]) / al

def baja2(a):
    h, w = a.shape[:2]; return a.reshape(h // 2, 2, w // 2, 2, 4).mean(axis=(1, 3))

def carga_maestra(render, nombre, luces=(0, 1, 2, 3)):
    """El promedio de los giros de luz [luces] (los cuatro por omisión; alfa premultiplicado) de una placa maestra → (alto, ancho, 4) float, fila 0 abajo, 160 px por bloque."""
    ims = [np.asarray(Image.open(os.path.join(render, f'{nombre}_d{k}.png')).convert('RGBA')).astype(np.float64) / 255.0 for k in luces]
    pm = [np.concatenate([x[..., :3] * x[..., 3:4], x[..., 3:4]], axis=2) for x in ims]
    return (sum(pm) / len(pm))[::-1]

def fondo_negro(a, lref):
    """El fondo hundido se renderiza con el kit a ≈ (36, 34, 40) (la plasticina oscura se aclara con la luz de rebote y el SSS); lo que importa es su sombreado RELATIVO (la oclusión de las paredes): se pasa al negro de hoy (#070818) conservando esa razón."""
    al = np.maximum(a[..., 3:4], 1e-6); L = luma(a)[..., None]
    rgb = HOLEC[None, None, :] * np.clip(L / lref, 0.0, 2.0)
    return np.concatenate([rgb * a[..., 3:4], a[..., 3:4]], axis=2)

def sim_transpuesta(t): return (t + t.transpose(1, 0, 2)) / 2.0
def sim_horizontal(t): return (t + t[:, ::-1]) / 2.0

def recorta(a, x0, x1, y0, y1):
    """La región [x0, x1) × [y0, y1) (bloques, con el origen en el centro de la placa; a 80 px por bloque) de un arreglo (fila 0 abajo)."""
    h, w = a.shape[:2]; c0 = int(round(w / 2 + x0 * PX)); c1 = int(round(w / 2 + x1 * PX)); r0 = int(round(h / 2 + y0 * PX)); r1 = int(round(h / 2 + y1 * PX))
    return a[r0:r1, c0:c1]

class Maestras:
    """Las baldosas y el perfil de un grupo de placas (módulo 'mod', aro del hueco 'aro' o fondo del hueco 'fon')."""
    def __init__(self, render, prefijo, margen, luces=(0, 1, 2, 3), simetriza=True):
        self.render_dir = render; self.prefijo = prefijo; self.margen = margen; self.luces = luces
        self.hr = {f: self._carga160(f'{prefijo}_{f}', prefijo, render, luces) for f in ('convexa', 'borde', 'concava', 'interior')}     # a 160 px por bloque
        self.m = {f: baja2(v) for f, v in self.hr.items()}
        mg = margen
        st = sim_transpuesta if simetriza else (lambda t: t); sh = sim_horizontal if simetriza else (lambda t: t)
        # el marco local de un cuarto: u a la derecha, v hacia arriba, el centro del bloque en (0, 0); el cuarto es [0, Q)²; los márgenes van en +u, +v
        self.t_convexa = st(recorta(self.m['convexa'], -0.5, 0.0 + mg / PX, -0.5, 0.0 + mg / PX))                  # la esquina en (0, 0) de la placa = (0,5; 0,5) del cuarto
        self.t_borde = sh(recorta(self.m['borde'], 0.0, 0.5, -0.5, 0.0 + mg / PX))                                    # el borde de arriba (v = 0,5), a lo largo de u
        self.t_concava = st(recorta(self.m['concava'], -0.5, 0.0 + mg / PX, -0.5, 0.0 + mg / PX))                   # el vértice cóncavo en la esquina (0,5; 0,5)
        self.t_interior = recorta(self.m['interior'], 0.0, 0.5, 0.0, 0.5)
        self.textura = recorta(self.m['interior'], -2.0, 2.0, -2.0, 2.0)

    @staticmethod
    def _carga160(nombre, prefijo, render, luces):
        a = carga_maestra(render, nombre, luces)
        if prefijo == 'fon':
            ref = carga_maestra(render, 'fon_interior', luces); h, w = ref.shape[:2]
            lref = float(np.median(luma(ref)[h // 2 - 60:h // 2 + 60, w // 2 - 60:w // 2 + 60]))
            a = fondo_negro(a, lref)
        return a

    def perfil(self, lo, hi):
        """El perfil del canto recto, medido en la placa 'borde' a 160 px por bloque: (y, RGBA premultiplicado) promediado a lo largo del borde (x de −1 a 1); d = y (d > 0 = afuera)."""
        a = self.hr['borde']; h, w = a.shape[:2]
        cols = a[:, int(w / 2 - 1.0 * PXM):int(w / 2 + 1.0 * PXM)].mean(axis=1)
        ys = (np.arange(h) + 0.5 - h / 2.0) / PXM; sel = (ys >= lo) & (ys <= hi)
        return ys[sel], cols[sel]

def paso_tipo(R, U, D):
    if not R and not U: return 'convexa'
    if R and not U: return 'borde_arriba'
    if U and not R: return 'borde_derecha'
    return 'concava' if not D else 'interior'

def compone_c1(cells, mas, size=SIZE):
    """El camino 1: el sprite de la pieza armado con las baldosas (premultiplicado, 384 × 384, fila 0 abajo)."""
    canvas = np.zeros((size, size, 4)); s = set(cells); ccx, ccy, w, h = piece_box(cells); mg = mas.margen
    orden = {'interior': 0, 'borde_arriba': 1, 'borde_derecha': 1, 'convexa': 2, 'concava': 3}
    trabajos = []
    for (x, y) in cells:
        cx = int(round(size / 2 + (x - ccx) * PX)); cy = int(round(size / 2 + (y - ccy) * PX))
        for sx in (-1, 1):
            for sy in (-1, 1):
                tipo = paso_tipo((x + sx, y) in s, (x, y + sy) in s, (x + sx, y + sy) in s)
                trabajos.append((orden[tipo], tipo, cx, cy, sx, sy))
    for _, tipo, cx, cy, sx, sy in sorted(trabajos):
        t = {'interior': mas.t_interior, 'borde_arriba': mas.t_borde, 'borde_derecha': mas.t_borde.transpose(1, 0, 2), 'convexa': mas.t_convexa, 'concava': mas.t_concava}[tipo].copy()
        if tipo == 'concava' and mg:                                                       # el cuarto cóncavo solo aporta su cuarto y la esquinita de más allá del vértice (los bordes ya pusieron sus franjas)
            q = t.copy(); t = np.zeros_like(t); t[Q:, Q:] = q[Q:, Q:]; t[:Q, :Q] = q[:Q, :Q]
        hv, hu = t.shape[:2]
        if sx < 0: t = t[:, ::-1]
        if sy < 0: t = t[::-1]
        x0 = cx if sx > 0 else cx - hu; y0 = cy if sy > 0 else cy - hv
        ys0, xs0 = max(0, y0), max(0, x0); ys1, xs1 = min(size, y0 + hv), min(size, x0 + hu)
        if ys1 <= ys0 or xs1 <= xs0: continue
        ts = t[ys0 - y0:ys1 - y0, xs0 - x0:xs1 - x0]; dst = canvas[ys0:ys1, xs0:xs1]
        canvas[ys0:ys1, xs0:xs1] = ts + dst * (1 - ts[..., 3:4])
    return canvas

class PerfilSDF:
    """El camino 2: una tabla 1-D (RGB recto) del canto recto; el alfa es analítico."""
    def __init__(self, ys, cols):
        al = np.maximum(cols[:, 3], 1e-6); self.d = ys; self.rgb = cols[:, :3] / al[:, None]; self.a = cols[:, 3]

    def rgb_en(self, d):
        d = np.asarray(d); out = np.empty(d.shape + (3,)); ok = self.a > 0.5
        for k in range(3): out[..., k] = np.interp(d, self.d[ok], self.rgb[ok][:, k])
        return out

    def con_relieve(self, k):
        """La misma tabla con el relieve multiplicado por [k] (alrededor del color del interior): el camino 2 deja subir o bajar el relieve sin volver a Blender."""
        o = PerfilSDF.__new__(PerfilSDF); o.d = self.d; o.a = self.a; interior = self.rgb[self.d < -0.5].mean(axis=0)
        o.rgb = np.clip(interior + k * (self.rgb - interior), 0.0, 1.0); return o

def sdf_robusto(cells, gx, gy):
    """El SDF del juego MÁS una caja de 2 × 2 por cada 2 × 2 de bloques: con solo cajas y puentes de 1 × 2, el punto donde se juntan 4 bloques tiene d = 0 (la unión de los puentes lo deja justo en su borde) y se dibuja un alfiler translúcido."""
    d = piece_sdf(cells, gx, gy); s = set(cells)
    for (x, y) in cells:
        if (x + 1, y) in s and (x, y + 1) in s and (x + 1, y + 1) in s: d = np.minimum(d, round_box(gx, gy, x + 0.5, y + 0.5, 1.0, 1.0, R))
    return d

def compone_c2(cells, perfil, size=SIZE, nivel=0.0, aa_px=1.5, robusto=True):
    """El camino 2 para el MÓDULO: alfa analítico del SDF en [nivel] y color de la tabla por la distancia con signo (la luz no tiene dirección)."""
    gx, gy = grid(cells, size); d = (sdf_robusto if robusto else piece_sdf)(cells, gx, gy); aa = SIDE / size * aa_px
    alfa = cover(d - nivel, aa); rgb = perfil.rgb_en(d)
    return np.concatenate([rgb * alfa[..., None], alfa[..., None]], axis=2)

def compone_c2_hueco(cells, perfil_aro, perfil_fondo, size=SIZE, aa_px=1.5):
    """El camino 2 para el HUECO: (aro, fondo): el aro es la silueta d ≤ 0,115 y el fondo d ≤ 0; el color de cada uno, de la tabla del borde recto de su capa."""
    gx, gy = grid(cells, size); d = sdf_robusto(cells, gx, gy); aa = SIDE / size * aa_px
    a1 = cover(d - INK, aa); a2 = cover(d, aa)
    aro = np.concatenate([perfil_aro.rgb_en(d) * a1[..., None], a1[..., None]], axis=2)
    fon = np.concatenate([perfil_fondo.rgb_en(d) * a2[..., None], a2[..., None]], axis=2)
    return aro, fon

def compone_c2_baja(cells, perfil, factor=2, size=SIZE, aa_px=1.5):
    """El camino 2 con el SDF evaluado a 1/[factor] de la resolución y subido por interpolación bilineal (la distancia es suave salvo en los vértices cóncavos): el alfa y el color salen de la distancia subida. Cuesta 1/factor² del SDF."""
    n = size // factor; gx, gy = grid(cells, n); d = np.pad(sdf_robusto(cells, gx, gy), 1, mode='edge')
    ys, xs = np.mgrid[0:size, 0:size]; X = (xs + 0.5) / factor - 0.5 + 1; Y = (ys + 0.5) / factor - 0.5 + 1
    du = bilinear(d[..., None], X, Y)[..., 0]; aa = SIDE / size * aa_px
    alfa = cover(du, aa); rgb = perfil.rgb_en(du)
    return np.concatenate([rgb * alfa[..., None], alfa[..., None]], axis=2)
