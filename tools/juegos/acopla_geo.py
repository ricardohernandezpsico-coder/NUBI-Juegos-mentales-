# -*- coding: utf-8 -*-
"""Acoplamiento — geometría del estudio (Tarea 113, fase A): las piezas reales del juego, su SDF (el MISMO de `DockingSprites.PieceSdf`), el arte de HOY (réplica en numpy de `RenderPieceLayers`) y la luz fija de pantalla.
Convenciones: los sprites son arreglos (alto, ancho, 4) en float 0..1 con alfa PREMULTIPLICADO y la fila 0 ABAJO (como las texturas de Unity); 80 px por bloque (384 px / 4,8 bloques)."""
import math
import numpy as np
from PIL import Image

PX = 80                                  # px por bloque en el sprite que hornea el juego
SIDE = 4.8                               # lado del cuadro de la pieza, en bloques
SIZE = 384                               # PiecePx
R = 0.16                                 # radio de las esquinas convexas
INK = 3.0 / 26.0                         # el aro / borde de tinta: 0,115 bloque
HOLE_INSET = 0.6 / 26.0
MODULE = np.array([0x7F, 0xD8, 0xFF]) / 255.0
INKC = np.array([0x1A, 0x12, 0x40]) / 255.0
HOLEC = np.array([0x07, 0x08, 0x18]) / 255.0
TEAL = np.array([120, 240, 220]) / 255.0; TEAL_A = 0.65
LIME = np.array([166, 227, 107]) / 255.0
CYAN = np.array([127, 216, 255]) / 255.0

# ------------------------------------------------------------------ las piezas (poliominós quirales de 4 a 7 bloques que caben en 4 × 4, como `RandomChiralShape`)
def normalize(cells):
    mx = min(x for x, y in cells); my = min(y for x, y in cells)
    return tuple(sorted((x - mx, y - my) for x, y in cells))

def rot90(cells): return normalize([(-y, x) for x, y in cells])
def mirror(cells): return normalize([(-x, y) for x, y in cells])

def orbit(cells):
    out = []; c = normalize(cells)
    for _ in range(4): out.append(c); c = rot90(c)
    return out

def is_chiral(cells): return normalize(mirror(cells)) not in orbit(cells)
def fits(cells): return max(x for x, y in cells) < 4 and max(y for x, y in cells) < 4

def connected(cells):
    s = set(cells); todo = [next(iter(s))]; seen = {todo[0]}
    while todo:
        x, y = todo.pop()
        for d in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + d[0], y + d[1])
            if n in s and n not in seen: seen.add(n); todo.append(n)
    return len(seen) == len(s)

def enumerate_pieces():
    """Todas las siluetas quirales (a menos de un giro: una pieza y su espejo cuentan por separado) de 4 a 7 bloques que caben en 4 × 4."""
    fixed = {1: {((0, 0),)}}
    for n in range(2, 8):
        nxt = set()
        for c in fixed[n - 1]:
            s = set(c)
            for (x, y) in c:
                for d in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    q = (x + d[0], y + d[1])
                    if q not in s: nxt.add(normalize(list(s | {q})))
        fixed[n] = nxt
    res = {}
    for n in range(4, 8):
        classes = set()
        for c in fixed[n]:
            if fits(c) and is_chiral(c): classes.add(min(orbit(c)))
        res[n] = sorted(classes)
    return res

def tile_cases(cells):
    """Cuántos cuartos de bloque de la pieza caen en cada caso de los 3 vecinos (derecha, arriba, diagonal): convexa, borde, cóncava, interior y el «pellizco» (solo la diagonal)."""
    s = set(cells); cnt = {'convexa': 0, 'borde': 0, 'concava': 0, 'interior': 0, 'pellizco': 0}
    for (x, y) in cells:
        for sx in (-1, 1):
            for sy in (-1, 1):
                r = (x + sx, y) in s; u = (x, y + sy) in s; d = (x + sx, y + sy) in s
                if not r and not u: cnt['pellizco' if d else 'convexa'] += 1
                elif r != u: cnt['borde'] += 1
                elif not d: cnt['concava'] += 1
                else: cnt['interior'] += 1
    return cnt

def has_notch(cells):
    """Una concavidad de UN bloque de ancho: una casilla vacía con piezas a los dos lados opuestos (izquierda-derecha o arriba-abajo) o rodeada por tres."""
    s = set(cells); w = max(x for x, y in cells) + 1; h = max(y for x, y in cells) + 1; out = []
    for x in range(-1, w + 1):
        for y in range(-1, h + 1):
            if (x, y) in s: continue
            l, r, u, d = (x - 1, y) in s, (x + 1, y) in s, (x, y + 1) in s, (x, y - 1) in s
            if (l and r) or (u and d) or (l + r + u + d) >= 3: out.append((x, y))
    return out

def has_2x2(cells):
    s = set(cells)
    return any({(x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)} <= s for (x, y) in cells)

def pick_pieces():
    """Cinco piezas reales: la L del tutorial (4), una F (5, con concavidades de un bloque), una P (5, con un 2 × 2), una de 6 con un hueco de un bloque y una de 7 con otro."""
    L4 = normalize([(0, 0), (0, 1), (0, 2), (1, 0)])                                # la pieza del tutorial (docs: «una L de 4 bloques»)
    F5 = normalize([(1, 0), (2, 0), (0, 1), (1, 1), (1, 2)])
    P5 = normalize([(0, 0), (1, 0), (0, 1), (1, 1), (0, 2)])
    pool = enumerate_pieces()
    C6 = next(c for c in pool[6] if has_notch(c) and fits(c) and max(max(x for x, y in c), max(y for x, y in c)) == 3 and len(has_notch(c)) == 1)
    cand = [c for c in pool[7] if has_notch(c) and max(max(x for x, y in c), max(y for x, y in c)) == 3]
    H7 = next((c for c in cand if has_2x2(c)), cand[0])
    for c in (L4, F5, P5, C6, H7): assert is_chiral(c) and fits(c) and connected(c), c
    return {'L4': L4, 'F5': F5, 'P5': P5, 'C6': C6, 'H7': H7}

# ------------------------------------------------------------------ el SDF del juego (`DockingSprites.PieceSdf`)
def round_box(x, y, cx, cy, hx, hy, r):
    qx = np.abs(x - cx) - hx + r; qy = np.abs(y - cy) - hy + r
    ox = np.maximum(qx, 0.0); oy = np.maximum(qy, 0.0)
    return np.sqrt(ox * ox + oy * oy) + np.minimum(np.maximum(qx, qy), 0.0) - r

def piece_sdf(cells, gx, gy):
    s = set(cells); d = np.full(gx.shape, 1e5)
    for (cx, cy) in cells:
        d = np.minimum(d, round_box(gx, gy, cx, cy, 0.5, 0.5, R))
        if (cx + 1, cy) in s: d = np.minimum(d, round_box(gx, gy, cx + 0.5, cy, 1.0, 0.5, R))
        if (cx, cy + 1) in s: d = np.minimum(d, round_box(gx, gy, cx, cy + 0.5, 0.5, 1.0, R))
    return d

def piece_box(cells):
    w = max(x for x, y in cells) + 1; h = max(y for x, y in cells) + 1
    return (w - 1) * 0.5, (h - 1) * 0.5, w, h

def grid(cells, size=SIZE, side=SIDE, supers=1):
    """Las coordenadas de bloque (gx, gy) del centro de cada píxel de un cuadro de [side] bloques centrado en la pieza (fila 0 = abajo); [supers] > 1 devuelve la rejilla de sub-muestras."""
    ccx, ccy, w, h = piece_box(cells); n = size * supers
    t = ((np.arange(n) + 0.5) / n - 0.5) * side
    gx = ccx + t[None, :] + 0 * t[:, None]; gy = ccy + t[:, None] + 0 * t[None, :]
    return gx, gy

def cover(d, aa): return np.clip(0.5 - d / aa, 0.0, 1.0)

def sdf_image(cells, size=SIZE, side=SIDE):
    gx, gy = grid(cells, size, side); return piece_sdf(cells, gx, gy)

def exact_coverage(cells, level, size, side=SIDE, ss=4):
    """La cobertura EXACTA (promedio de ss × ss sub-muestras del indicador d ≤ level) de la pieza en un cuadro de [size] px (fila 0 = abajo)."""
    gx, gy = grid(cells, size, side, ss); ind = (piece_sdf(cells, gx, gy) <= level).astype(np.float64)
    return ind.reshape(size, ss, size, ss).mean(axis=(1, 3))

# ------------------------------------------------------------------ el arte de HOY (réplica de `DockingSprites.RenderPieceLayers`)
RIVETS = [(-0.27, -0.27), (0.27, -0.27), (-0.27, 0.27), (0.27, 0.27)]

def over(dst, color, alpha):
    """Compone [color] (3,) con alfa [alpha] (h, w) sobre [dst] (h, w, 4) premultiplicado."""
    a = alpha[..., None]; out = dst.copy()
    out[..., :3] = color[None, None, :] * a + dst[..., :3] * (1 - a); out[..., 3:4] = a + dst[..., 3:4] * (1 - a); return out

def today_layers(cells, size=SIZE):
    """(cuerpo, borde, hueco) de hoy, premultiplicados, fila 0 = abajo."""
    gx, gy = grid(cells, size); d = piece_sdf(cells, gx, gy); aa = SIDE / size * 1.5
    body = np.zeros((size, size, 4)); edge = np.zeros((size, size, 4)); hole = np.zeros((size, size, 4))
    edge = over(edge, np.ones(3), cover(d - INK, aa)); hole = over(hole, np.ones(3), cover(d + HOLE_INSET, aa))
    body = over(body, INKC, cover(d - INK, aa)); body = over(body, MODULE, cover(d + 0.02, aa))
    s = set(cells); ix = np.floor(gx + 0.5).astype(int); iy = np.floor(gy + 0.5).astype(int)
    inside_cell = np.zeros(gx.shape, bool)
    for (cx, cy) in cells: inside_cell |= (ix == cx) & (iy == cy)
    lx = gx - ix; ly = gy - iy; inside = cover(d + 0.02, aa)
    joint = np.full(gx.shape, 1e5)
    for (cx, cy) in cells:
        m = (ix == cx) & (iy == cy)
        j = np.full(gx.shape, 1e5)
        row = np.abs(ly) <= 0.5; col = np.abs(lx) <= 0.5
        if (cx + 1, cy) in s: j = np.where(row, np.minimum(j, np.abs(lx - 0.5)), j)
        if (cx - 1, cy) in s: j = np.where(row, np.minimum(j, np.abs(lx + 0.5)), j)
        if (cx, cy + 1) in s: j = np.where(col, np.minimum(j, np.abs(ly - 0.5)), j)
        if (cx, cy - 1) in s: j = np.where(col, np.minimum(j, np.abs(ly + 0.5)), j)
        joint = np.where(m, j, joint)
    body = over(body, INKC, 0.55 * cover(joint - 0.02, aa) * inside * inside_cell)
    for (rx, ry) in RIVETS:
        dr = np.sqrt((lx - rx) ** 2 + (ly - ry) ** 2) - 0.06
        body = over(body, INKC, 0.35 * cover(dr, aa) * inside * inside_cell)
    return body, edge, hole

# ------------------------------------------------------------------ la pantalla: el módulo con su luz FIJA, su brillo y su sombra dura (como `DockingGameController`)
SHADOW = np.array([8, 6, 30]) / 255.0

def to_img(a):
    """float premultiplicado (fila 0 abajo) → imagen PIL RGBA recta (fila 0 arriba)."""
    a = a[::-1]; al = a[..., 3:4]; rgb = np.where(al > 1e-5, a[..., :3] / np.maximum(al, 1e-5), 0.0)
    return Image.fromarray((np.clip(np.concatenate([rgb, al], axis=2), 0, 1) * 255 + 0.5).astype(np.uint8), 'RGBA')

def from_img(im):
    a = np.asarray(im.convert('RGBA')).astype(np.float64) / 255.0; al = a[..., 3:4]
    return np.concatenate([a[..., :3] * al, al], axis=2)[::-1]

def bilinear(img, px, py):
    """Muestrea [img] (h, w, c; fila 0 = abajo) con bilineal en las coordenadas de píxel (px, py) (centro del píxel i = i); fuera = transparente."""
    h, w = img.shape[:2]
    x0 = np.floor(px).astype(int); y0 = np.floor(py).astype(int); tx = (px - x0)[..., None]; ty = (py - y0)[..., None]
    def g(xx, yy):
        ok = ((xx >= 0) & (xx < w) & (yy >= 0) & (yy < h))[..., None]
        return np.where(ok, img[np.clip(yy, 0, h - 1), np.clip(xx, 0, w - 1)], 0.0)
    return (g(x0, y0) * (1 - tx) + g(x0 + 1, y0) * tx) * (1 - ty) + (g(x0, y0 + 1) * (1 - tx) + g(x0 + 1, y0 + 1) * tx) * ty

def resample(sprite, scale, angle_deg=0.0, flip=False, n=None):
    """El sprite premultiplicado visto en pantalla: [scale] veces el tamaño, girado [angle_deg] (antihorario en pantalla) y espejado (escala x −1) si [flip]; muestreo bilineal SIN prefiltro, como la GPU. Cuadro de [n] px (por omisión 1,45 veces el del sprite)."""
    h, w = sprite.shape[:2]; n = n or int(math.ceil(w * scale * 1.45)); n += n % 2
    ys, xs = np.mgrid[0:n, 0:n]; X = xs - (n - 1) / 2.0; Y = ys - (n - 1) / 2.0            # píxeles de pantalla, Y hacia arriba
    th = math.radians(angle_deg); c, s_ = math.cos(th), math.sin(th)
    xi = (X * c + Y * s_) / scale; yi = (-X * s_ + Y * c) / scale                           # el punto del sprite que cae en cada píxel (giro inverso)
    if flip: xi = -xi
    return np.clip(bilinear(sprite, xi + (w - 1) / 2.0, yi + (h - 1) / 2.0), 0.0, 1.0)

def fixed_light(shape_hw, cells_wh, block_px):
    """La luz de arriba (un velo blanco 35 % que se apaga hasta el 55 %, y abajo un velo oscuro 15 %) y el brillo (elipse blanca al 30 %), FIJOS en la pantalla, centrados en el módulo (fila 0 = abajo)."""
    h, w = shape_hw; cw, ch = cells_wh
    rad = math.sqrt(cw * cw + ch * ch) * block_px * 0.5
    ys, xs = np.mgrid[0:h, 0:w]; cx = (w - 1) / 2.0; cy = (h - 1) / 2.0
    X = xs - cx; Y = ys - cy                                               # Y hacia ARRIBA (fila 0 abajo)
    # el gradiente: un cuadrado de lado 2·rad centrado
    t = np.clip(0.5 - Y / (2 * rad), 0.0, 1.0)                              # 0 arriba, 1 abajo
    inside = (np.abs(X) <= rad) & (np.abs(Y) <= rad)
    white_a = np.where(t < 0.55, 0.35 * (1 - t / 0.55), 0.0) * inside
    dark_a = np.where(t >= 0.55, 0.18 * ((t - 0.55) / 0.45), 0.0) * inside
    # el brillo: elipse de 1,1·rad × 0,48·rad en (−0,35·rad, +0,5·rad), girada 0,45 rad
    sx, sy = -0.35 * rad, 0.5 * rad; rho = 0.45
    dx = X - sx; dy = Y - sy; ux = dx * math.cos(rho) + dy * math.sin(rho); uy = -dx * math.sin(rho) + dy * math.cos(rho)
    ell = np.clip(0.5 - ((np.sqrt((ux / (0.55 * rad * 0.9)) ** 2 + (uy / (0.24 * rad * 0.9)) ** 2) - 1.0) * 0.24 * rad * 0.9), 0.0, 1.0)
    return white_a, dark_a, 0.3 * ell

def module_on_screen(body, cells, block_px, angle_deg=0.0, flip=False, pad=1.2):
    """El módulo como se ve en pantalla (premultiplicado, fila 0 = abajo): la sombra dura, el cuerpo girado/espejado y la luz fija y el brillo recortados por su silueta. Devuelve (imagen, alfa del cuerpo)."""
    scale = block_px / PX
    b = resample(body, scale, angle_deg, flip); h, w = b.shape[:2]
    ccx, ccy, cw, ch = piece_box(cells)
    sh = np.zeros_like(b); off = int(round(6.0 / 26.0 * block_px))                       # la sombra cae 6 dp (a la escala de 26 dp por bloque) hacia abajo
    a = b[..., 3]; sh[..., :3] = SHADOW[None, None, :] * 0.55 * a[..., None]; sh[..., 3] = 0.55 * a
    sh = np.roll(sh, -off, axis=0)
    out = np.zeros_like(b); out = sh.copy()
    # el cuerpo encima de la sombra (over)
    out = b + out * (1 - b[..., 3:4])
    white_a, dark_a, spot_a = fixed_light((h, w), (cw, ch), block_px)
    mask = (a > 0.5).astype(np.float64)
    for color, alpha in ((np.ones(3), white_a), (SHADOW, dark_a), (np.ones(3), spot_a)):
        al = (alpha * mask)[..., None]
        out[..., :3] = color[None, None, :] * al + out[..., :3] * (1 - al); out[..., 3:4] = al + out[..., 3:4] * (1 - al)
    return out, a
