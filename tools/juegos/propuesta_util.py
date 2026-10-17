# -*- coding: utf-8 -*-
"""Propuesta de VOLUMEN (Tarea 114, parte 2): las herramientas con que se arman las pantallas «hoy | propuesta». Todo en numpy sobre imágenes float 0..1 con alfa PREMULTIPLICADO, fila 0 ARRIBA (como una captura).
Receta (la de Ricardo y Opus): los OBJETOS ganan un CANTO visible (la misma silueta, más oscura, corrida hacia ABAJO en el espacio de la pantalla: no gira, no delata un espejo), una SOMBRA DE CONTACTO, una cara MATE con un bisel marcado (el relieve sale de la distancia al borde:
no tiene dirección, así que gira o se espeja sin delatarse) y colores cálidos y saturados; la luz de pantalla es FIJA (arriba a la izquierda). El fondo no se toca."""
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

DP = 3.0                                                    # píxeles por dp en las capturas de 1080 de ancho

# ------------------------------------------------------------------ operaciones básicas
def blur(a, sigma):
    if sigma <= 0: return a
    r = int(math.ceil(sigma * 3)); k = np.exp(-0.5 * (np.arange(-r, r + 1) / sigma) ** 2); k /= k.sum()
    def pasa(x, eje):
        pad = [(0, 0)] * x.ndim; pad[eje] = (r, r); xp = np.pad(x, pad, mode='edge'); out = np.zeros_like(x)
        for i, w in enumerate(k): out += w * np.take(xp, np.arange(i, i + x.shape[eje]), axis=eje)
        return out
    return pasa(pasa(a, 0), 1)

def shift(a, dy, dx=0):
    """Corre [a] [dy] filas hacia abajo y [dx] columnas a la derecha (lo que sale se pierde; lo nuevo es 0)."""
    out = np.zeros_like(a); h, w = a.shape[:2]
    ys0, ys1 = max(0, dy), min(h, h + dy); xs0, xs1 = max(0, dx), min(w, w + dx)
    if ys1 > ys0 and xs1 > xs0: out[ys0:ys1, xs0:xs1] = a[ys0 - dy:ys1 - dy, xs0 - dx:xs1 - dx]
    return out

def over(dst, src):
    """Compone [src] (premultiplicado, h × w × 4) sobre [dst] (premultiplicado)."""
    return src + dst * (1 - src[..., 3:4])

def pega(dst, src, x0, y0):
    """Compone [src] (premultiplicado, 4 canales) sobre [dst] (RGB o RGBA) en (x0, y0), recortando lo que se sale."""
    h, w = src.shape[:2]; ys0, xs0 = max(0, y0), max(0, x0); ys1, xs1 = min(dst.shape[0], y0 + h), min(dst.shape[1], x0 + w)
    if ys1 <= ys0 or xs1 <= xs0: return
    s = src[ys0 - y0:ys1 - y0, xs0 - x0:xs1 - x0]; d = dst[ys0:ys1, xs0:xs1]
    dst[ys0:ys1, xs0:xs1] = s[..., :3] + d * (1 - s[..., 3:4]) if dst.shape[2] == 3 else s + d * (1 - s[..., 3:4])

def edt(mask):
    """La distancia euclídea EXACTA de cada píxel verdadero de [mask] al píxel falso más cercano (0 donde es falso), con el algoritmo separable de Felzenszwalb."""
    inf = 1e12; h, w = mask.shape; f = np.where(mask, inf, 0.0)
    # primero por columnas
    def pasa(g):
        n = g.shape[0]; d = np.empty(n); v = np.zeros(n, int); z = np.empty(n + 1); k = 0; v[0] = 0; z[0] = -inf; z[1] = inf
        for q in range(1, n):
            while True:
                p = v[k]; s = ((g[q] + q * q) - (g[p] + p * p)) / (2.0 * q - 2.0 * p)
                if s <= z[k]: k -= 1
                else: break
            k += 1; v[k] = q; z[k] = s; z[k + 1] = inf
        k = 0
        for q in range(n):
            while z[k + 1] < q: k += 1
            d[q] = (q - v[k]) ** 2 + g[v[k]]
        return d
    for j in range(w): f[:, j] = pasa(f[:, j])
    for i in range(h): f[i, :] = pasa(f[i, :])
    return np.sqrt(np.minimum(f, inf))

def distancia_adentro(alfa, umbral=0.5):
    """La distancia (px) al borde de la silueta de [alfa] (0..1), con sub-píxel: edt de la máscara más la parte fraccionaria del alfa en el borde."""
    m = alfa >= umbral
    d = edt(m)
    return np.where(m, d - 0.5 + np.clip(alfa, 0, 1) * 0.0, 0.0)

# ------------------------------------------------------------------ el relieve sin dirección y la luz fija de pantalla
LUZ = np.array([-0.55, 0.62, 0.56]); LUZ = LUZ / np.linalg.norm(LUZ)      # (x a la derecha, y hacia ARRIBA, z hacia el ojo): la luz viene de arriba a la izquierda

def infla(alfa, ancho_px, altura=1.0, luz=LUZ):
    """«Infla» una silueta: una altura que sube de 0 en el borde a [altura] a [ancho_px] píxeles adentro (un cuarto de círculo: canto redondo) y de ahí es plana. Devuelve (sombreado, d): el sombreado (≈ 1 en la cara plana, más claro mirando a la luz y más oscuro del otro lado) con la luz FIJA
    de pantalla, y la distancia al borde. El relieve sale solo de la distancia: no tiene dirección propia, así que la pieza gira o se espeja sin delatarse; la luz fija es de la pantalla."""
    d = distancia_adentro(alfa); t = np.clip(d / max(ancho_px, 1e-6), 0.0, 1.0)
    h = np.sqrt(np.clip(1 - (1 - t) ** 2, 0, 1)) * altura * ancho_px
    gy, gx = np.gradient(blur(h, 0.8))                                          # y hacia ABAJO (fila 0 arriba)
    nx, ny, nz = -gx, gy, np.ones_like(h)                                       # la normal (con y hacia arriba: −dh/dy_abajo = +gy... se invierte el signo)
    n = np.sqrt(nx * nx + ny * ny + nz * nz); nx, ny, nz = nx / n, ny / n, nz / n
    plano = luz[2]                                                              # una cara plana (nx = ny = 0) recibe luz[2]
    s = (nx * luz[0] + ny * luz[1] + nz * luz[2]) / plano
    return np.clip(s, 0.35, 1.45), d

def grano(shape, semilla, amp=0.03, sigma=0.9, baja=0.04, sigma_baja=14.0):
    """Una variación a mano, ISÓTROPA: un grano fino (σ ≈ 1 px) y una mancha suave de baja frecuencia (σ ≈ 14 px), con media 1; no tiene dirección."""
    r = np.random.RandomState(semilla)
    g = blur(r.randn(*shape), sigma); g = g / (g.std() + 1e-9) * amp
    b = blur(r.randn(*shape), sigma_baja); b = b / (b.std() + 1e-9) * baja
    return 1.0 + g + b

def rgb_a_hsv(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]; mx = np.max(rgb, axis=-1); mn = np.min(rgb, axis=-1); d = mx - mn
    h = np.zeros_like(mx)
    m = d > 1e-9
    rc = np.where(m & (mx == r), ((g - b) / np.where(m, d, 1)) % 6, 0); gc = np.where(m & (mx == g) & (mx != r), (b - r) / np.where(m, d, 1) + 2, 0); bc = np.where(m & (mx == b) & (mx != r) & (mx != g), (r - g) / np.where(m, d, 1) + 4, 0)
    h = (rc + gc + bc) / 6.0
    s = np.where(mx > 1e-9, d / np.maximum(mx, 1e-9), 0); return np.stack([h % 1.0, s, mx], axis=-1)

def hsv_a_rgb(hsv):
    h, s, v = hsv[..., 0] * 6.0, hsv[..., 1], hsv[..., 2]; i = np.floor(h).astype(int) % 6; f = h - np.floor(h)
    p = v * (1 - s); q = v * (1 - s * f); t = v * (1 - s * (1 - f))
    r = np.choose(i, [v, q, p, p, t, v]); g = np.choose(i, [t, v, v, q, p, p]); b = np.choose(i, [p, p, t, v, v, q]); return np.stack([r, g, b], axis=-1)

def cuerpo(color_rgb, alfa, ancho_bisel_px, semilla, amp_grano=0.03, ancho_variacion=0.04, contraste=1.0):
    """La CARA de una pieza de plasticina mate: el color plano, el sombreado del bisel por distancia con la luz fija, una variación a mano. Devuelve un RGBA premultiplicado."""
    s, d = infla(alfa, ancho_bisel_px)
    s = 1.0 + (s - 1.0) * contraste
    v = grano(alfa.shape, semilla, amp_grano, 0.9, ancho_variacion)
    rgb = np.clip(np.asarray(color_rgb)[None, None, :] * (s * v)[..., None], 0, 1)
    return np.concatenate([rgb * alfa[..., None], alfa[..., None]], axis=2)

def canto_y_sombra(alfa, canto_px, color_canto, sombra_px=None, sombra_sigma=None, sombra_alfa=0.5, color_sombra=(0.02, 0.02, 0.10)):
    """Las capas de DEBAJO de la cara (premultiplicadas): la sombra de contacto (la silueta corrida [sombra_px] hacia abajo y desenfocada) y el CANTO (la silueta extruida [canto_px] píxeles hacia abajo, de [color_canto], con un degradé: más claro arriba y más oscuro abajo)."""
    h, w = alfa.shape; sombra_px = sombra_px if sombra_px is not None else canto_px + 3.0 * DP; sombra_sigma = sombra_sigma if sombra_sigma is not None else 1.6 * DP
    sh = blur(shift(alfa, int(round(sombra_px))), sombra_sigma) * sombra_alfa
    capa_sombra = np.concatenate([np.asarray(color_sombra)[None, None, :] * sh[..., None], sh[..., None]], axis=2)
    canto = np.zeros((h, w, 4)); k = int(round(canto_px))
    for i in range(k, 0, -1):                                                   # de abajo hacia arriba: lo de arriba tapa lo de abajo
        a = shift(alfa, i); f = 0.70 - 0.22 * (i / max(1, k))                  # más oscuro abajo
        c = np.concatenate([np.asarray(color_canto)[None, None, :] * f * a[..., None], a[..., None]], axis=2)
        canto = over(canto, c) if False else c + canto * (1 - c[..., 3:4])
    return capa_sombra, canto

def objeto_plasticina(color_rgb, alfa, canto_dp=4.5, bisel_dp=2.2, semilla=1, color_canto=None, contraste=1.0, sombra=True, grano_amp=0.03):
    """Todo junto: (sombra, canto, cara) de un objeto; el canto toma el color de la cara más oscuro y más saturado."""
    color = np.asarray(color_rgb, float)
    if color_canto is None:
        hsv = rgb_a_hsv(color[None, None, :]); hsv[..., 1] = np.clip(hsv[..., 1] * 1.15 + 0.05, 0, 1); hsv[..., 2] = hsv[..., 2] * 0.62; color_canto = hsv_a_rgb(hsv)[0, 0]
    cara = cuerpo(color, alfa, bisel_dp * DP, semilla, amp_grano=grano_amp, contraste=contraste)
    s, c = canto_y_sombra(alfa, canto_dp * DP, color_canto, sombra_alfa=0.5 if sombra else 0.0)
    return s, c, cara

def luz_fija(alfa, caja=None, arriba=0.16, abajo=0.16):
    """La luz de arriba de la pantalla, recortada por la silueta [alfa] (premultiplicada): un velo blanco arriba que se apaga a mitad de la pieza y uno oscuro abajo (MATE: sin la franja de brillo del módulo de hoy). [caja] = (y0, y1) filas donde va el degradé (por omisión, la caja de la silueta)."""
    h, w = alfa.shape; ys = np.arange(h)[:, None] * np.ones((1, w))
    if caja is None:
        filas = np.where(alfa.max(axis=1) > 0.5)[0]; caja = (filas.min(), filas.max()) if filas.size else (0, h)
    t = np.clip((ys - caja[0]) / max(1, caja[1] - caja[0]), 0, 1)
    blanco = np.where(t < 0.55, arriba * (1 - t / 0.55), 0.0) * alfa; oscuro = np.where(t >= 0.55, abajo * ((t - 0.55) / 0.45), 0.0) * alfa
    return blanco, oscuro

def aplica_luz(cara, caja=None, arriba=0.16, abajo=0.16, azul_oscuro=(0.03, 0.02, 0.12)):
    blanco, oscuro = luz_fija(cara[..., 3], caja, arriba, abajo)
    out = cara.copy()
    out[..., :3] = out[..., :3] * (1 - blanco[..., None]) + blanco[..., None]                    # velo blanco (premultiplicado: el alfa no cambia)
    out[..., :3] = out[..., :3] * (1 - oscuro[..., None]) + np.asarray(azul_oscuro)[None, None, :] * oscuro[..., None]
    out[..., :3] = np.minimum(out[..., :3], out[..., 3:4])
    return out

# ------------------------------------------------------------------ dibujo con PIL (a sobremuestreo) → alfa
def rect_redondeado(w, h, r, ss=4):
    """El alfa (h × w, 0..1) de un rectángulo redondeado de radio [r] px, dibujado a [ss] veces y reducido."""
    im = Image.new('L', (w * ss, h * ss), 0); ImageDraw.Draw(im).rounded_rectangle((0, 0, w * ss - 1, h * ss - 1), radius=r * ss, fill=255)
    return np.asarray(im.resize((w, h), Image.LANCZOS)).astype(np.float64) / 255.0

def dibuja(w, h, fn, ss=4):
    """El alfa de lo que dibuja [fn](ImageDraw, ss) en un lienzo de w × h px (a [ss] veces el tamaño y reducido)."""
    im = Image.new('L', (w * ss, h * ss), 0); fn(ImageDraw.Draw(im), ss); return np.asarray(im.resize((w, h), Image.LANCZOS)).astype(np.float64) / 255.0

def texto(w, h, s, fuente, color, x, y, ancla='mm'):
    """Un texto (RGBA premultiplicado) en un lienzo de w × h px."""
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0)); ImageDraw.Draw(im).text((x, y), s, font=fuente, fill=tuple(int(round(c * 255)) for c in color) + (255,), anchor=ancla)
    a = np.asarray(im).astype(np.float64) / 255.0; return np.concatenate([a[..., :3] * a[..., 3:4], a[..., 3:4]], axis=2)

# ------------------------------------------------------------------ capas sueltas (K sobre negro, W sobre blanco), rampas de color y trabajo en una caja
def lum(rgb): return rgb[..., 0] * 0.299 + rgb[..., 1] * 0.587 + rgb[..., 2] * 0.114

def capa_KW(K, W):
    """La capa (premultiplicada, h × w × 4) de un objeto dibujado sobre un telón NEGRO (K) y uno BLANCO (W): alfa = 1 − (W − K), color premultiplicado = K."""
    a = np.clip(1.0 - (W - K).mean(axis=2), 0.0, 1.0)
    return np.concatenate([np.minimum(K, a[..., None]), a[..., None]], axis=2)

def recto(L):
    """El color sin premultiplicar (h × w × 3) de una capa premultiplicada."""
    a = L[..., 3:4]; return np.where(a > 1e-4, L[..., :3] / np.maximum(a, 1e-4), 0.0)

def caja_de(a, margen=0, umbral=0.02):
    ys, xs = np.where(a > umbral)
    if ys.size == 0: return None
    h, w = a.shape; return (max(0, xs.min() - margen), max(0, ys.min() - margen), min(w, xs.max() + 1 + margen), min(h, ys.max() + 1 + margen))

def rampa(paradas, t):
    """Un mapa de color: [paradas] = [(t0, (r, g, b) 0..255), …] con t creciente; [t] cualquier forma → (…, 3) en 0..1, con interpolación lineal."""
    ts = np.array([p[0] for p in paradas], float); cs = np.array([p[1] for p in paradas], float) / 255.0
    t = np.clip(np.asarray(t, float), ts[0], ts[-1]); out = np.zeros(t.shape + (3,))
    for c in range(3): out[..., c] = np.interp(t, ts, cs[:, c])
    return out

# ------------------------------------------------------------------ el kit de la app (las losas), la tipografía y el cielo
import os, json
_AQUI = os.path.dirname(os.path.abspath(__file__)); _REPO = os.path.normpath(os.path.join(_AQUI, '..', '..'))
KIT = os.path.join(_REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'comun')
FUENTE = os.path.join(_REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Fonts', 'Fredoka-Bold.ttf')

def fuente(dp): return ImageFont.truetype(FUENTE, int(round(dp * DP)))

def losa_kit(nombre, ancho, alto):
    """Una losa del kit de la app (nine-slice) con el CUERPO de [ancho] × [alto] px; devuelve (RGBA premultiplicado, (margen izquierdo, margen de arriba))."""
    man = json.load(open(os.path.join(KIT, 'manifest.json'), encoding='utf-8')); at = Image.open(os.path.join(KIT, 'atlas.png')).convert('RGBA')
    e = next(s for s in man['sprites'] if s['n'] == nombre)
    im = at.crop((e['x'], at.height - e['y'] - e['h'], e['x'] + e['w'], at.height - e['y']))
    bl, bb, br, bt = e['borde']; ml, mt, mr, mb = e['margen']; w0, h0 = im.size
    W = ancho + ml + mr; H = alto + mt + mb
    xs = [0, bl, w0 - br, w0]; ys = [0, bt, h0 - bb, h0]; xd = [0, bl, W - br, W]; yd = [0, bt, H - bb, H]
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    for i in range(3):
        for j in range(3):
            parte = im.crop((xs[i], ys[j], xs[i + 1], ys[j + 1])); tam = (max(1, xd[i + 1] - xd[i]), max(1, yd[j + 1] - yd[j]))
            out.alpha_composite(parte.resize(tam, Image.LANCZOS) if parte.size != tam else parte, (xd[i], yd[j]))
    a = np.asarray(out).astype(np.float64) / 255.0
    return np.concatenate([a[..., :3] * a[..., 3:4], a[..., 3:4]], axis=2), (ml, mt)

def sprite_kit(nombre):
    """Un sprite suelto del kit (RGBA premultiplicado, fila 0 ARRIBA): las monedas «marca_ok» y «marca_no»."""
    man = json.load(open(os.path.join(KIT, 'manifest.json'), encoding='utf-8')); at = Image.open(os.path.join(KIT, 'atlas.png')).convert('RGBA')
    e = next(s for s in man['sprites'] if s['n'] == nombre)
    a = np.asarray(at.crop((e['x'], at.height - e['y'] - e['h'], e['x'] + e['w'], at.height - e['y']))).astype(np.float64) / 255.0
    return np.concatenate([a[..., :3] * a[..., 3:4], a[..., 3:4]], axis=2)

def cielo_entre(img, y0, y1, x0, x1, izq, der):
    """Rellena [y0:y1, x0:x1] con el cielo: por cada fila, una interpolación entre la mediana de las columnas [izq] (a la izquierda de la zona) y la de las columnas [der] (a la derecha): conserva el degradé horizontal del cielo y no deja bordes."""
    a = np.median(img[y0:y1, izq[0]:izq[1]], axis=1); b = np.median(img[y0:y1, der[0]:der[1]], axis=1)
    t = ((np.arange(x0, x1) - x0 + 0.5) / (x1 - x0))[None, :, None]
    img[y0:y1, x0:x1] = a[:, None, :] * (1 - t) + b[:, None, :] * t

def componentes(mascara):
    """Las componentes conexas (8 vecinos) de una máscara booleana: lista de (ys, xs). Sin scipy: relleno por inundación sobre los píxeles verdaderos."""
    h, w = mascara.shape; vis = np.zeros_like(mascara, bool); out = []
    for y0, x0 in zip(*np.where(mascara)):
        if vis[y0, x0]: continue
        pila = [(y0, x0)]; vis[y0, x0] = True; ys = []; xs = []
        while pila:
            y, x = pila.pop(); ys.append(y); xs.append(x)
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    yy, xx = y + dy, x + dx
                    if 0 <= yy < h and 0 <= xx < w and mascara[yy, xx] and not vis[yy, xx]: vis[yy, xx] = True; pila.append((yy, xx))
        out.append((np.array(ys), np.array(xs)))
    return out
