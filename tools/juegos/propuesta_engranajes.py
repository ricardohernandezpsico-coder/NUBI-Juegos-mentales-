# -*- coding: utf-8 -*-
"""Propuesta de volumen para ENGRANAJES (Tarea 114, parte 2): el MISMO cuadro real del juego («hoy») con el cohete y la máquina rehechos como plasticina con volumen. Entradas (las saca un gancho TEMPORAL de las capturas, `tools/juegos/propuesta/PropuestaHook.cs`):
  NN-<toma>.png           el cuadro normal (hoy);
  NN-<toma>-fondo.png     el mismo cuadro SIN el cohete ni la máquina (queda la sala y el cielo);
  NN-<toma>-gII-K.png / -W.png   el grupo II (un engranaje, una correa con sus poleas, el motor, un cartel, una parte del cohete…) SOLO, sobre un telón negro (K) y uno blanco (W): alfa = 1 − (W − K), color premultiplicado = K → la capa EXACTA de cada objeto, sin sombra horneada;
  <toma>-grupos.txt       «II<TAB>nombre» de cada grupo, de abajo hacia arriba.
Receta (la de Ricardo y Opus): cada objeto recibe un CANTO (su misma silueta, más oscura, extruida hacia ABAJO en la pantalla: no gira), una SOMBRA DE CONTACTO, una cara MATE con un bisel marcado por la distancia al borde (sin dirección: gira sin delatarse), colores cálidos y saturados
(motor azul, camino LILA de verdad, pieza dorada) por un MAPA DE COLOR (la luz del original recorre una rampa de plasticina) y la luz fija arriba a la izquierda; las letras y el borde de los carteles salen en relieve y las monedas ✓/✗ son las del kit; el cohete gana cuerpo, ventanillas con aro y remaches.
El fondo (la sala, el cielo) no se toca; abajo, «Arrancar» y las llaves son losas del kit con más canto.
Uso: python -I tools/juegos/propuesta_engranajes.py <carpeta de tomas> <toma>   → tools/juegos/out/propuesta/eng_<toma>_hoy.png y eng_<toma>_propuesta.png"""
import os, sys, math, glob, json
import numpy as np
from PIL import Image, ImageDraw
AQUI = os.path.dirname(os.path.abspath(__file__)); REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
sys.path.insert(0, AQUI)
sys.stdout.reconfigure(encoding='utf-8')
from propuesta_util import *

OUT = os.path.join(AQUI, 'out', 'propuesta')
ROI = (560, 1720)                                          # las filas de la máquina y el cohete (la interfaz de abajo no es máquina)
PAD = 54                                                   # margen alrededor de cada objeto para su canto y su sombra

# ------------------------------------------------------------------ mapas de color (cada rampa: de la sombra profunda a la luz de plasticina)
RAMPAS = {
    'lila':  [(0.00, (40, 26, 96)),  (0.30, (98, 66, 184)),  (0.62, (156, 118, 236)), (0.85, (194, 162, 252)), (1.00, (232, 214, 255))],
    'azul':  [(0.00, (14, 42, 106)), (0.30, (28, 94, 192)),  (0.62, (62, 162, 242)),  (0.85, (112, 202, 252)), (1.00, (198, 238, 255))],
    'oro':   [(0.00, (108, 60, 8)),  (0.30, (192, 114, 12)), (0.62, (246, 172, 34)),  (0.85, (255, 206, 72)),  (1.00, (255, 242, 160))],
    'coral': [(0.00, (96, 22, 40)),  (0.30, (180, 50, 68)),  (0.62, (240, 102, 98)),  (0.85, (255, 142, 126)), (1.00, (255, 206, 186))],
    'perla': [(0.00, (96, 70, 132)), (0.30, (172, 142, 204)), (0.62, (226, 204, 238)), (0.85, (248, 236, 250)), (1.00, (255, 252, 255))],
    'noche': [(0.00, (8, 10, 34)),   (0.40, (28, 34, 88)),   (0.80, (50, 58, 132)),   (1.00, (84, 96, 176))],
}
ROLES = list(RAMPAS)

def hsv(rgb): return rgb_a_hsv(rgb)

def pesos_auto(rgb, a, sigma=2.0):
    """Para cada rol una máscara suave (suma 1) según el color de cada píxel de la capa."""
    h = hsv(rgb); H, S, V = h[..., 0] * 360.0, h[..., 1], h[..., 2]
    m = {}
    m['coral'] = ((H < 22) | (H > 338)) & (S > 0.35) & (V > 0.35)
    m['oro'] = (H >= 24) & (H < 70) & (S > 0.28) & (V > 0.40)
    m['azul'] = (H >= 180) & (H < 236) & (S > 0.20) & (V > 0.35)
    ya = m['coral'] | m['oro'] | m['azul']
    m['noche'] = (V < 0.42) & ~ya
    m['lila'] = (H >= 236) & (H < 312) & (S > 0.05) & (V >= 0.42) & ~ya
    m['perla'] = ~(m['coral'] | m['oro'] | m['azul'] | m['noche'] | m['lila'])
    w = {k: blur(v.astype(np.float64), sigma) for k, v in m.items()}
    tot = sum(w.values()) + 1e-9
    return {k: v / tot for k, v in w.items()}

def rol_global(rgb, a):
    """El rol dominante de toda la capa (para un engranaje: un solo color, sin manchas)."""
    w = pesos_auto(rgb, a, 1.0); sel = a > 0.9
    roles = [k for k in ROLES if k != 'noche']                        # el cubo oscuro no define el color de un engranaje
    return max(roles, key=lambda k: w[k][sel].sum() if sel.any() else 0.0)

# ------------------------------------------------------------------ la cara, el canto y la sombra de un objeto
def cara_por_rampas(L, pesos, bisel_dp=2.2, contraste=1.0, local=0.8, semilla=1, grano_amp=0.02, pct=(4.0, 99.5), gamma=1.0, extra_t=None, fuente=1.0, sesgo=0.0):
    """La CARA de la capa [L] (premultiplicada): la luz del original (su luminancia) recorre la rampa de cada rol [pesos]; el bisel (por distancia al borde, sin dirección) mueve esa luz con la luz FIJA de la pantalla; [local] realza lo de adentro (rayos, cubo)."""
    a = L[..., 3]; rgb = recto(L); Y = lum(rgb); sel = a > 0.9
    yl, yh = np.percentile(Y[sel], pct) if sel.any() else (0.0, 1.0)
    t = np.clip((Y - yl) / max(yh - yl, 1e-6), 0, 1) ** gamma
    if fuente != 1.0: med = np.median(t[sel]) if sel.any() else 0.5; t = med + fuente * (t - med)          # [fuente] < 1: la luz horneada del original (un brillo diagonal) pesa menos
    t = np.clip(t + local * (t - blur(t, 4.0)) + sesgo, 0, 1)
    s, d = infla(a, bisel_dp * DP)
    t = np.clip(t + (s - 1.0) * 0.85 * contraste + (grano(a.shape, semilla, grano_amp, 0.9, 0.03) - 1.0) + (0.0 if extra_t is None else extra_t), 0, 1)
    col = np.zeros(a.shape + (3,))
    for k, w in pesos.items(): col += w[..., None] * rampa(RAMPAS[k], t)
    return np.concatenate([col * a[..., None], a[..., None]], axis=2)

def capas_de(a, rol_color, canto_dp, sombra=0.5):
    """(sombra, canto) de la silueta [a]: el canto extruido hacia ABAJO del color más profundo del rol, y la sombra de contacto."""
    base = rampa(RAMPAS[rol_color], 0.30) if isinstance(rol_color, str) else np.asarray(rol_color, float) / 255.0
    return canto_y_sombra(a, canto_dp * DP, base, sombra_alfa=sombra)

def objeto(L, rol=None, pesos=None, bisel_dp=2.2, canto_dp=4.5, contraste=1.0, local=0.8, semilla=1, sombra=0.5, canto_rol=None, **kw):
    """Todo de un objeto: (sombra, canto, cara) sobre la caja de [L] con el margen [PAD]. [rol] un solo color, o [pesos] por píxel."""
    a = L[..., 3]; rgb = recto(L)
    if pesos is None: pesos = {rol or rol_global(rgb, a): np.ones(a.shape)}
    cara = cara_por_rampas(L, pesos, bisel_dp, contraste, local, semilla, **kw)
    rc = canto_rol or max(pesos, key=lambda k: pesos[k][a > 0.9].sum() if (a > 0.9).any() else 0.0)
    s, c = capas_de(a, rc, canto_dp, sombra)
    return s, c, cara

# ------------------------------------------------------------------ una capa suelta de la toma
def carga(p): return np.asarray(Image.open(p).convert('RGB')).astype(np.float32) / 255.0

class Toma:
    def __init__(self, d, nombre):
        self.d = d; self.nombre = nombre
        pat = lambda suf: sorted(f for f in glob.glob(os.path.join(d, '*-' + nombre + suf + '.png')) if os.path.basename(f)[:2].isdigit() and os.path.basename(f)[2] == '-')
        self.F = carga(pat('')[0]); self.fondo = carga(pat('-fondo')[0])
        self.grupos = [l.split('\t') for l in open(os.path.join(d, nombre + '-grupos.txt'), encoding='utf-8').read().splitlines()]
    def capa(self, idx):
        """(L recortada con margen, (x0, y0)) o None: la capa EXACTA del grupo [idx] dentro de la franja de la máquina."""
        K = carga(glob.glob(os.path.join(self.d, '*-%s-g%s-K.png' % (self.nombre, idx)))[0]); W = carga(glob.glob(os.path.join(self.d, '*-%s-g%s-W.png' % (self.nombre, idx)))[0])
        K = K[ROI[0]:ROI[1]]; W = W[ROI[0]:ROI[1]]
        L = capa_KW(K, W)
        b = caja_de(L[..., 3], 0, 0.02)
        if b is None: return None
        x0, y0, x1, y1 = b
        crop = L[y0:y1, x0:x1].astype(np.float64)
        return np.pad(crop, ((PAD, PAD), (PAD, PAD), (0, 0))), (x0 - PAD, y0 + ROI[0] - PAD)

# ------------------------------------------------------------------ cada clase de objeto
def objeto_cohete(nombre, L):
    a = L[..., 3]; rgb = recto(L)
    if nombre == 'Hull':
        h = hsv(rgb); H, S, V = h[..., 0] * 360.0, h[..., 1], h[..., 2]
        coral = blur((((H < 22) | (H > 338)) & (S > 0.35) & (V > 0.3)).astype(np.float64), 1.5)
        noche = blur(((V < 0.40) & (S < 0.7)).astype(np.float64), 1.5)
        perla = np.clip(1 - coral - noche, 0, 1)
        # un cuerpo REDONDO: la luz de la pantalla (arriba a la izquierda) aclara el lado izquierdo y oscurece el derecho, como un cilindro
        ys, xs = np.where(perla > 0.5); x0, x1 = xs.min(), xs.max()
        u = np.clip((np.arange(a.shape[1]) - x0) / max(1.0, x1 - x0), 0, 1)[None, :] * np.ones(a.shape)
        cil = 0.12 * (1 - u) - 0.28 * u ** 1.5
        return objeto(L, pesos={'coral': coral, 'noche': noche, 'perla': perla}, bisel_dp=7.0, canto_dp=6.0, contraste=1.25, local=0.1, semilla=40, canto_rol=(124, 92, 156), extra_t=cil * (a > 0.5), fuente=0.15, sesgo=0.08)
    if nombre == 'Window':
        w = pesos_auto(rgb, a, 1.0)
        oro = w['oro']; resto = 1 - oro                                  # encendida: oro; apagada: noche
        return objeto(L, pesos={'oro': oro, 'noche': resto}, bisel_dp=4.0, canto_dp=1.6, contraste=1.3, local=0.0, semilla=41)
    return objeto_color(L, bisel_dp=2.4, canto_dp=3.0, contraste=1.0, semilla=42)

def objeto_color(L, bisel_dp=2.4, canto_dp=3.0, contraste=1.0, semilla=1, sat=1.12):
    """Un objeto que CONSERVA los colores del original (las partes del cohete: compuerta, carga, antena, turbina) y gana el bisel de plasticina, el canto y la sombra."""
    a = L[..., 3]; rgb = recto(L)
    s, d = infla(a, bisel_dp * DP)
    g = grano(a.shape, semilla, 0.02, 0.9, 0.03)
    h = hsv(rgb); h[..., 1] = np.clip(h[..., 1] * sat, 0, 1); col = hsv_a_rgb(h)
    col = np.clip(col * (1.0 + (s[..., None] - 1.0) * 0.55 * contraste) * g[..., None], 0, 1)
    cara = np.concatenate([col * a[..., None], a[..., None]], axis=2)
    base = np.median(rgb[a > 0.9], axis=0) if (a > 0.9).any() else np.array([0.5, 0.4, 0.6])
    sh, ca = canto_y_sombra(a, canto_dp * DP, np.clip(base * 0.55, 0, 1), sombra_alfa=0.45)
    return sh, ca, cara

def domo(r_px, rol='perla', semilla=1, canto_dp=0.7, bisel_extra=1.0):
    """Un remache o una bola de plasticina de radio [r_px]: (sombra, canto, cara) sobre un lienzo de (2 r + 2 PAD) px; el centro es el centro del lienzo."""
    n = int(2 * r_px + 2 * 24); yy, xx = np.mgrid[0:n, 0:n]; c = (n - 1) / 2.0
    a = np.clip(r_px + 0.5 - np.hypot(xx - c, yy - c), 0, 1)
    s, d = infla(a, r_px * bisel_extra); t = np.clip(0.80 + (s - 1.0) * 1.0, 0, 1)
    col = rampa(RAMPAS[rol], t); cara = np.concatenate([col * a[..., None], a[..., None]], axis=2)
    sh, ca = canto_y_sombra(a, canto_dp * DP, rampa(RAMPAS[rol], 0.40), sombra_alfa=0.42)
    return sh, ca, cara, n

def aro_de(a, ancho_px, fuera=0.0):
    """El anillo de [ancho_px] px alrededor de la silueta [a] (por fuera)."""
    m = a > 0.5; d = edt(~m)
    return ((d > fuera) & (d <= fuera + ancho_px)).astype(np.float64)

def objeto_placa(L):
    """Un cartel o el rótulo MOTOR: una losa gruesa (canto y sombra); el borde y las letras se levantan aparte; el color es el del original."""
    a = L[..., 3]; rgb = recto(L); h = hsv(rgb); V = h[..., 2]
    d = distancia_adentro(a)
    cuerpo = (a > 0.5) & (V < 0.45)
    claro = (a > 0.5) & ~cuerpo
    S = h[..., 1]; H = h[..., 0] * 360.0
    moneda = (a > 0.9) & (V > 0.72) & (S > 0.55) & (d >= 5.0) & (((H < 22) | (H > 338)) | ((H > 120) & (H < 180)))        # la moneda ✗ (coral) o ✓ (menta) del cartel, si hay
    coin = None; coral = False
    cs = [c for c in componentes(moneda) if c[0].size > 120]
    if cs:
        ys, xs = max(cs, key=lambda c: c[0].size); cx, cy = xs.mean(), ys.mean(); rr = max(xs.max() - xs.min(), ys.max() - ys.min()) / 2.0 + 2.0
        coin = (cx, cy, rr); coral = bool(np.median(H[ys, xs]) < 60 or np.median(H[ys, xs]) > 300)
        yy, xx = np.mgrid[0:a.shape[0], 0:a.shape[1]]; en_moneda = np.hypot(xx - cx, yy - cy) <= rr + 3.0
    else:
        en_moneda = np.zeros(a.shape, bool)
    borde = claro & (d < 10.0) & ~en_moneda
    glifo = claro & (d >= 10.0) & ~en_moneda
    cuerpo = cuerpo | en_moneda
    color_cuerpo = np.median(rgb[cuerpo], axis=0) if cuerpo.any() else np.array([0.11, 0.14, 0.36])
    base = np.where(cuerpo[..., None], rgb, color_cuerpo[None, None, :])
    s_sl, d_sl = infla(a, 3.4 * DP)
    cara = np.clip(base * (1.02 + 0.5 * (s_sl[..., None] - 1.0)), 0, 1)
    cara = np.concatenate([cara * a[..., None], a[..., None]], axis=2)
    sombra, canto = canto_y_sombra(a, 5.0 * DP, color_cuerpo * 0.72 + 0.02, sombra_alfa=0.52)
    return sombra, canto, cara, borde, glifo, rgb, a, coin, coral

def alza(rgb, mascara, bisel_dp, canto_dp, factor_canto=0.42):
    """Una parte levantada sobre la cara (el borde dorado, una letra): (sombra, canto, cara) con el color del original, un bisel y un canto bajo que la separa."""
    m = np.clip(blur(mascara.astype(np.float64), 0.7), 0, 1)
    s, d = infla(m, bisel_dp * DP)
    col = np.clip(rgb * (1.0 + 0.55 * (s[..., None] - 1.0) + 0.04), 0, 1)
    cara = np.concatenate([col * m[..., None], m[..., None]], axis=2)
    k = max(1, int(round(canto_dp * DP))); canto = np.zeros(cara.shape)
    base_c = np.clip(rgb * factor_canto, 0, 1)
    for i in range(k, 0, -1):
        mi = shift(m, i); c = np.concatenate([base_c * mi[..., None], mi[..., None]], axis=2); canto = c + canto * (1 - c[..., 3:4])
    sh = blur(shift(m, k + 1), 1.2 * DP * 0.5) * 0.45
    sombra = np.concatenate([np.array([0.01, 0.01, 0.06])[None, None, :] * sh[..., None], sh[..., None]], axis=2)
    return sombra, canto, cara

def pega_rgba(img, rgba, x0, y0): pega(img, rgba, int(x0), int(y0))

# ------------------------------------------------------------------ la propuesta
def propuesta(d, nombre):
    T = Toma(d, nombre)
    img = T.fondo.astype(np.float64).copy()
    capas = []
    for idx, nm in T.grupos:
        r = T.capa(idx)
        if r is None: continue
        L, (x0, y0) = r; capas.append((nm, (x0, y0), L))
    def pinta(obj, xy):
        s, c, f = obj[:3]; x0, y0 = xy
        pega_rgba(img, s, x0, y0); pega_rgba(img, c, x0, y0); pega_rgba(img, f, x0, y0)
    def pinta_por_pasos(lista):
        """Objetos a la MISMA altura (los engranajes que encajan): primero todas las sombras, luego todos los cantos y al final todas las caras."""
        for _, xy, o in lista: pega_rgba(img, o[0], *xy)
        for _, xy, o in lista: pega_rgba(img, o[1], *xy)
        for _, xy, o in lista: pega_rgba(img, o[2], *xy)
    # ---------------- el cohete
    for nm, xy, L in capas:
        if not nm.startswith('R.'): continue
        n = nm[2:]
        if n == 'WindowGlow': pega_rgba(img, L, *xy); continue
        if n == 'Window':                                                                   # cada luz con su aro de plasticina (una ventanilla)
            aro = aro_de(L[..., 3], 1.7 * DP)
            perla = np.broadcast_to(np.array([0.90, 0.86, 0.95]), aro.shape + (3,))
            s2, c2, f2 = alza(perla, aro, 0.9, 0.9, 0.55)
            pega_rgba(img, s2, *xy); pega_rgba(img, c2, *xy); pega_rgba(img, f2, *xy)
        pinta(objeto_cohete(n, L), xy)
        if n == 'Hull':                                                                     # remaches: bolitas de plasticina por el borde izquierdo del casco
            ys, xs = np.where(L[..., 3] > 0.9); gx0 = xy[0] + xs.min(); gy0 = xy[1] + ys.min(); gy1 = xy[1] + ys.max()
            r = 2.6 * DP; sh, ca, ca2, nn = domo(r, 'perla', 1)
            for k, yy in enumerate(range(int(gy0 + 130), int(gy1 - 70), 51)):
                for (px, py) in ((gx0 + 20, yy),):
                    pega_rgba(img, sh, px - nn // 2, py - nn // 2); pega_rgba(img, ca, px - nn // 2, py - nn // 2); pega_rgba(img, ca2, px - nn // 2, py - nn // 2)
    # ---------------- la máquina: ejes, barras y engranajes a la misma altura
    base = []
    for nm, xy, L in capas:
        if nm.startswith('M.Line'): base.append((nm, xy, objeto(L, rol='lila', bisel_dp=1.4, canto_dp=1.6, contraste=1.0, local=0.2, semilla=50, grano_amp=0.01)))
    for nm, xy, L in capas:
        if nm.startswith('M.Rack'): base.append((nm, xy, objeto(L, rol='lila', bisel_dp=1.8, canto_dp=3.0, contraste=1.1, local=0.5, semilla=51)))
    for nm, xy, L in capas:
        if nm.startswith('M.Gear'): base.append((nm, xy, objeto(L, bisel_dp=2.4, canto_dp=4.5, contraste=1.1, local=0.9, semilla=60 + int(nm[6:]), sesgo=0.05)))
    pinta_por_pasos(base)
    # ---------------- las correas (con sus poleas): encima de los engranajes
    for nm, xy, L in capas:
        if nm.startswith('M.Belt'): pinta(objeto(L, pesos=pesos_auto(recto(L), L[..., 3], 1.5), bisel_dp=1.8, canto_dp=3.0, contraste=1.1, local=0.6, semilla=70), xy)
    # ---------------- lo de arriba: el halo del motor, su flecha, el rótulo y los carteles
    for nm, xy, L in capas:
        if nm == 'M.MotorPulse': pega_rgba(img, L, *xy)
    for nm, xy, L in capas:
        if nm == 'M.MotorArrow': pinta(objeto(L, rol='azul', bisel_dp=1.6, canto_dp=2.0, contraste=1.1, local=0.2, semilla=80, grano_amp=0.01), xy)
    for nm, xy, L in capas:
        if nm == 'M.MotorLabel' or nm.startswith('M.Cartel'):
            sombra, canto, cara, borde, glifo, rgb, a, coin, coral = objeto_placa(L)
            x0, y0 = xy
            pega_rgba(img, sombra, x0, y0); pega_rgba(img, canto, x0, y0); pega_rgba(img, cara, x0, y0)
            for (m, b, cn) in ((borde, 1.3, 1.0), (glifo, 1.0, 1.3)):
                s2, c2, f2 = alza(rgb, m, b, cn)
                pega_rgba(img, s2, x0, y0); pega_rgba(img, c2, x0, y0); pega_rgba(img, f2, x0, y0)
            if coin is not None:                                                            # la moneda del kit (✗ coral / ✓ menta), con su sombra
                cx, cy, rr = coin; dpx = int(round(2 * rr + 4)); sp = sprite_kit('marca_no' if coral else 'marca_ok')
                im = Image.fromarray((np.clip(sp, 0, 1) * 255 + 0.5).astype(np.uint8), 'RGBA').resize((dpx, dpx), Image.LANCZOS)
                m = np.asarray(im).astype(np.float64) / 255.0; m = np.pad(m, ((20, 20), (20, 20), (0, 0)))
                sh, ca = canto_y_sombra(m[..., 3], 1.4 * DP, np.array([0.30, 0.08, 0.10]) if coral else np.array([0.08, 0.28, 0.18]), sombra_alfa=0.5)
                px, py = int(round(x0 + cx - dpx / 2.0)) - 20, int(round(y0 + cy - dpx / 2.0)) - 20
                pega_rgba(img, sh, px, py); pega_rgba(img, ca, px, py); pega_rgba(img, m, px, py)
    for nm, xy, L in capas:
        if nm == 'M.Pulse' or nm == 'M.BlockCoin': pega_rgba(img, L, *xy)
    abajo(img, T.F)
    return img, T

def abajo(img, F):
    """Abajo, la cuenta de cambios y «Arrancar» como losas del kit con más canto: se limpia la franja y se arman otra vez con los mismos rectángulos, textos e íconos del cuadro real (sus letras salen en relieve)."""
    cielo_entre(img, 1690, 2112, 16, 1064, (0, 14), (1066, 1080))
    P = 44
    def losa_con_canto(nombre, bx0, by0, bw, bh, canto_dp, color_canto, tinte=None):
        losa, (ml, mt) = losa_kit(nombre, bw, bh); h, w = losa.shape[:2]
        if tinte is not None:                                                                # la misma losa, con su luz y su bisel, pero en otra rampa de color (la cuenta de cambios es de la noche, no morada)
            al = losa[..., 3]; Y = lum(np.where(al[..., None] > 1e-4, losa[..., :3] / np.maximum(al[..., None], 1e-4), 0.0))
            t = np.clip((Y - 0.05) / 0.32, 0, 1); losa = np.concatenate([rampa(tinte, t) * al[..., None], al[..., None]], axis=2)
        cuerpo_alfa = losa[mt:mt + bh, ml:ml + bw, 3]
        losa_con_canto.dentro = edt(cuerpo_alfa > 0.97) > 6.0                                # lo que está de verdad sobre la losa (no las esquinas del rectángulo)
        L = np.pad(losa, ((P, P), (P, P), (0, 0))); a = L[..., 3]
        sh, ca = canto_y_sombra(a, canto_dp * DP, color_canto, sombra_alfa=0.45)
        X0 = bx0 - ml - P; Y0 = by0 - mt - P
        pega_rgba(img, sh, X0, Y0); pega_rgba(img, ca, X0, Y0); pega_rgba(img, L, X0, Y0)
        return X0 + P + ml, Y0 + P + mt                                                     # la esquina del cuerpo (donde estaba en el cuadro)
    def relieve_de(mascara_local, color, bx0, by0, bisel, canto):
        n = P
        mpad = np.pad(mascara_local.astype(np.float64), ((n, n), (n, n)))
        rgb = np.broadcast_to(np.asarray(color, float), mpad.shape + (3,))
        s2, c2, f2 = alza(rgb, mpad, bisel, canto, 0.5)
        pega_rgba(img, s2, bx0 - n, by0 - n); pega_rgba(img, c2, bx0 - n, by0 - n); pega_rgba(img, f2, bx0 - n, by0 - n)
    # -------- «Arrancar»
    ax, ay, aw, ah = 391, 1728, 647, 303
    losa_con_canto('losa_md_cielo', ax, ay, aw, ah, 5.0, np.array([26, 92, 126]) / 255.0)
    sub = lum(F[ay:ay + ah, ax:ax + aw]); tinta = (sub < 0.30) & losa_con_canto.dentro        # el triángulo de «play» y las letras (tinta) del cuadro real, solo sobre la losa
    relieve_de(tinta, np.array([46, 34, 104]) / 255.0, ax, ay, 1.0, 1.1)
    # -------- la cuenta de cambios
    cx, cy, cw, chh = 42, 1727, 312, 305
    losa_con_canto('losa_md_noche', cx, cy, cw, chh, 3.5, np.array([6, 8, 30]) / 255.0, tinte=[(0.0, (5, 7, 26)), (0.5, (26, 34, 80)), (1.0, (66, 80, 156))])
    sub = F[cy:cy + chh, cx:cx + cw]; clara = (lum(sub) > 0.36) & losa_con_canto.dentro
    comps = componentes(clara)
    texto_m = np.zeros(clara.shape, bool); llaves = []
    for ys, xs in comps:
        if ys.size < 12: continue
        if ys.mean() < 0.40 * chh: texto_m[ys, xs] = True
        else: llaves.append((ys, xs))
    rgb_txt = np.median(sub[texto_m], axis=0) if texto_m.any() else np.array([0.7, 0.7, 0.9])
    relieve_de(texto_m, np.clip(rgb_txt * 1.05, 0, 1), cx, cy, 0.9, 0.9)
    for ys, xs in llaves:
        col = np.median(sub[ys, xs], axis=0); usada = col[0] > 0.6 and col[2] < 0.5
        y0, y1, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max(); mx, my = (x0 + x1) / 2.0, (y0 + y1) / 2.0; r = max(x1 - x0, y1 - y0) / 2.0 + 13
        yy, xx = np.mgrid[0:chh, 0:cw]; disco = np.clip(r + 0.5 - np.hypot(xx - mx, yy - my), 0, 1)
        relieve_de(disco > 0.5, np.array([96, 82, 70]) / 255.0 if usada else np.array([44, 52, 124]) / 255.0, cx, cy, 2.0, 1.5)
        m = np.zeros(clara.shape, bool); m[ys, xs] = True
        relieve_de(m, np.clip(col * (1.25 if usada else 1.45), 0, 1), cx, cy, 0.9, 0.9)

if __name__ == '__main__':
    d, nombre = sys.argv[1], sys.argv[2]
    os.makedirs(OUT, exist_ok=True)
    img, T = propuesta(d, nombre)
    Image.fromarray((np.clip(img, 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(OUT, 'eng_%s_propuesta.png' % nombre))
    Image.fromarray((np.clip(T.F, 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(OUT, 'eng_%s_hoy.png' % nombre))
    print('listo')
