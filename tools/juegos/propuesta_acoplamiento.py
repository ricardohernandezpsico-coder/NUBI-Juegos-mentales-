# -*- coding: utf-8 -*-
"""Propuesta de volumen para ACOPLAMIENTO (Tarea 114, parte 2; camino 2 del estudio): la pantalla REAL de «respuesta» (1080 × 2400) con el módulo y el hueco del puerto, el panel, los chevrones y los botones rehechos como plasticina con volumen. El fondo es el de la captura.
  - MÓDULO: la misma silueta exacta (SDF), cara mate (sin la franja de brillo vidrioso), bisel ×3, variación a mano, celeste más cálido, CANTO en espacio de pantalla (4,5 dp hacia abajo: no gira ni se espeja) y sombra de contacto.
  - HUECO: una cavidad hundida: el aro de estado (teal / lima / celeste) como un churro de plasticina, la pared de adentro visible arriba (4,5 dp, la silueta corrida hacia abajo) y el fondo negro; el contorno de la abertura es el del SDF.
  - PANEL del puerto, CHEVRONES y «Encaja» / «Espejo» (con las losas del kit) como plasticina con canto y sombra.
V2 (Tarea 115, parte 2; Ricardo aprobó la idea y la quiere ver antes de integrarla): el módulo como un juguete de plasticina hecho de bloques.
  - Una BOLITA de plasticina (un domo de ~0,33 del bloque) en el centro de cada cuadrito: su sombreado es ISÓTROPO (brillo centrado y borde parejo; ninguna luz que venga de un lado: una luz horneada que gire con la pieza delataría el espejo) y su sombrita de contacto va en la PANTALLA, hacia abajo, como el canto.
  - El módulo, más inflado («almohada»): el sombreado sale de la distancia al borde (isótropo: se oscurece hacia el borde y sube hacia el centro), un canto redondeado amplio, una leve variación de color hecha a mano, mate, celeste cálido; el canto de 5 dp hacia abajo y la sombra se quedan. Sin juntas ni «+»: en un 2 × 2 las 4 bolitas quedan como 4 puntos sueltos.
  - El hueco del puerto queda como en la v1 (cavidad hundida), liso, sin bolitas.
Uso: python -I tools/juegos/propuesta_acoplamiento.py   → tools/juegos/out/propuesta/aco_hoy.png, aco_propuesta.png (v1) y aco_v2.png"""
import os, sys, math, json
import numpy as np
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.stdout.reconfigure(encoding='utf-8')
from acopla_geo import *
from acopla_caminos import *
from propuesta_util import *

AQUI = os.path.dirname(os.path.abspath(__file__)); REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
CAPT = os.path.join(REPO, 'unity', 'test-results', 'capturas', 'acoplamiento')
OUT = os.path.join(AQUI, 'out', 'propuesta'); RENDER = os.path.join(AQUI, 'out', 'acoplamiento')
BP = 78.0                                                 # px por bloque (26 dp × 3)
MOD_C = (540, 1098); HUECO_C = (540, 1650); PANEL_C = (540, 1632); PANEL_WH = (504, 438)
TEAL_P = np.array([92, 200, 194]) / 255.0; LIME_P = np.array([166, 227, 107]) / 255.0; CYAN_P = np.array([127, 216, 255]) / 255.0

def carga(path): return np.asarray(Image.open(path).convert('RGB')).astype(np.float64) / 255.0

def alfa_pieza(cells, nivel, bp=BP, aa_px=1.0):
    """El alfa EXACTO (analítico, rampa de [aa_px] px) de la pieza a [bp] px por bloque en el cuadro de 4,8 bloques; fila 0 ARRIBA. Con el SDF arreglado (caja de 2 × 2)."""
    ns = int(round(SIDE * bp)); gx, gy = grid(cells, ns); d = sdf_robusto(cells, gx, gy)
    return cover(d - nivel, aa_px / bp)[::-1], (sdf_robusto, ns)

def limpia(base, fondo01, hoy_c):
    """La pantalla sin el módulo, el hueco, el panel, los chevrones ni los botones: el módulo sale de la captura de «llegada» (todavía no había llegado); lo demás se rellena con el cielo de la misma fila."""
    img = base.copy()
    img[868:1332, 300:780] = fondo01[868:1332, 300:780]                                   # donde estaba el módulo (con su sombra)
    cielo_entre(img, 1393, 1900, 236, 844, (206, 232), (850, 876))
    cielo_entre(img, 1895, 2175, 0, 1080, (0, 16), (1064, 1080))
    return img

# ------------------------------------------------------------------ las piezas de plasticina de la interfaz
def icono_encaja(px):
    """«Encaja»: tres bloques (una L) que bajan a su soporte (una U), crema con borde de tinta."""
    s = px / 36.0
    def dib(d, ss):
        def blq(x, y, w, h, r=2.0, grow=0.0):
            d.rounded_rectangle(((18 + x - grow) * s * ss, (18 + y - grow) * s * ss, (18 + x + w + grow) * s * ss, (18 + y + h + grow) * s * ss), radius=(r + grow) * s * ss, fill=255)
        for (x, y) in ((-9, -14), (-1, -14), (-1, -6)): blq(x, y, 8, 8, grow=1.5)
        for (a, b, c, e) in ((-13, 6, -13, 13), (-13, 13, 13, 13), (13, 13, 13, 6)): d.line(((18 + a) * s * ss, (18 + b) * s * ss, (18 + c) * s * ss, (18 + e) * s * ss), fill=255, width=int(3 * s * ss))
    tinta = dibuja(int(px), int(px), dib)
    def dib2(d, ss):
        for (x, y) in ((-9, -14), (-1, -14), (-1, -6)): d.rounded_rectangle(((18 + x) * s * ss, (18 + y) * s * ss, (18 + x + 8) * s * ss, (18 + y + 8) * s * ss), radius=2 * s * ss, fill=255)
    crema = dibuja(int(px), int(px), dib2)
    return tinta, crema

def icono_espejo(px):
    s = px / 36.0
    def dib(d, ss, grow=0.0):
        for (x, y) in ((-15, -10), (-15, -4), (-9, 2), (9, -10), (9, -4), (3, 2)):
            d.rounded_rectangle(((18 + x - grow) * s * ss, (18 + y - grow) * s * ss, (18 + x + 6 + grow) * s * ss, (18 + y + 6 + grow) * s * ss), radius=(2 + grow) * s * ss, fill=255)
    tinta = dibuja(int(px), int(px), lambda d, ss: (dib(d, ss, 1.5), [d.line((18 * s * ss, (18 + y0) * s * ss, 18 * s * ss, (18 + min(14, y0 + 3)) * s * ss), fill=255, width=int(3 * s * ss)) for y0 in range(-14, 14, 6)]))
    crema = dibuja(int(px), int(px), lambda d, ss: dib(d, ss, 0.0))
    return tinta, crema

def boton(nombre_kit, texto_boton, icono, cx, cy, ancho=474, alto=228):
    """Un botón del kit (cuerpo [ancho] × [alto] px) con su ícono (a la izquierda) y su texto de tinta; devuelve (RGBA premultiplicado, x0, y0)."""
    losa, (ml, mt) = losa_kit(nombre_kit, ancho, alto)
    h, w = losa.shape[:2]; x0 = cx - ancho // 2 - ml; y0 = cy - alto // 2 - mt
    tinta_i, crema_i = icono(108)
    INK = np.array([0x1A, 0x12, 0x40]) / 255.0; CREMA = np.array([0xE9, 0xE6, 0xFF]) / 255.0
    capa = np.zeros((h, w, 4))
    ix = ml + (34 - 18) * 3 + 0; iy = mt + alto // 2 - 54                                  # el ícono: centro a 34 dp del borde izquierdo
    def pon(a, col, ox, oy):
        nonlocal capa
        ah, aw = a.shape; sub = np.zeros((h, w)); sub[oy:oy + ah, ox:ox + aw] = a[:h - oy, :w - ox]
        c = np.concatenate([np.asarray(col)[None, None, :] * sub[..., None], sub[..., None]], axis=2); capa = over(capa, c)
    pon(tinta_i, INK, ix, iy); pon(crema_i, CREMA, ix, iy)
    tx = ml + 58 * 3 - 0; ty = mt + alto // 2
    t = texto(w, h, texto_boton, fuente(22), INK, tx, ty, 'lm'); capa = over(capa, t)
    return over(losa, capa), x0, y0

def chevron(px=54):
    """Una luz del puerto: una «V» (12 × 6 dp, trazo de 3 dp) como un churro de plasticina: la silueta con tubo."""
    s = px / 18.0
    def dib(d, ss):
        d.line(((9 - 6) * s * ss, (9 - 3) * s * ss, 9 * s * ss, (9 + 3) * s * ss), fill=255, width=int(3 * s * ss)); d.line((9 * s * ss, (9 + 3) * s * ss, (9 + 6) * s * ss, (9 - 3) * s * ss), fill=255, width=int(3 * s * ss))
        for (cx, cy) in ((3, 6), (9, 12), (15, 6)): d.ellipse(((cx - 1.5) * s * ss, (cy - 1.5) * s * ss, (cx + 1.5) * s * ss, (cy + 1.5) * s * ss), fill=255)
    return np.pad(dibuja(px, px, dib), 12)

def pieza_en_pantalla(face_sprite, cells, angulo, flip, bp=BP):
    """El sprite de la cara (premultiplicado, fila 0 ABAJO) visto en pantalla (rotado, espejado) → RGBA premultiplicado con fila 0 ARRIBA."""
    return resample(face_sprite, bp / PX, angulo, flip)[::-1]

def canvas_de(a): return a.shape[:2]

def sobre_pantalla(img, rgba, cx, cy):
    h, w = rgba.shape[:2]; pega(img, rgba, cx - w // 2, cy - h // 2)

def compone_modulo(cara, P, angulo=225.0, flip=True, canto_dp=5.0, bp=BP):
    """El módulo como plasticina: la cara (sprite del camino 2) vista en pantalla con grano a mano, un celeste más cálido y la luz fija mate (sin franja de brillo), el CANTO hacia abajo en la pantalla y la sombra de contacto. Devuelve (RGBA premultiplicado, alfa de la cara)."""
    pan = pieza_en_pantalla(cara.copy(), P, angulo, flip, bp); alfa = pan[..., 3].copy()
    lut_int = np.array([134, 213, 242]) / 255.0; objetivo = np.array([124, 212, 246]) / 255.0                 # el interior del perfil y el celeste cálido que se quiere
    warm = (objetivo / lut_int)[None, None, :]
    pan[..., :3] = np.minimum(pan[..., :3] * warm * grano(alfa.shape, 7, 0.010, 0.9, 0.022)[..., None], pan[..., 3:4])
    cara_m = aplica_luz(pan, arriba=0.16, abajo=0.15)
    base_c = np.array([54, 128, 186]) / 255.0
    s, c = canto_y_sombra(alfa, canto_dp * DP, base_c, sombra_alfa=0.52)
    return over(over(s, c), cara_m), alfa

def centros_en_pantalla(P, angulo=225.0, flip=True, bp=BP):
    """El centro (columna, fila; fila 0 ARRIBA) de cada cuadrito de la pieza [P] en la imagen de `pieza_en_pantalla`: el mismo giro y espejo con que se muestrea el sprite, aplicados hacia adelante."""
    scale = bp / PX; n = int(math.ceil(384 * scale * 1.45)); n += n % 2
    th = math.radians(angulo); c, s_ = math.cos(th), math.sin(th); ccx, ccy, cw, ch = piece_box(P); out = []
    for (cx, cy) in sorted(P):
        u = (cx - ccx) * PX; v = (cy - ccy) * PX
        xi = -u if flip else u; yi = v
        X = scale * (c * xi - s_ * yi); Y = scale * (s_ * xi + c * yi)
        out.append((round((n - 1) / 2.0 + X, 4), round((n - 1) / 2.0 - Y, 4)))                                  # redondeados a 1e-4: un centro justo en x,5 no debe saltar de píxel por 1e-13 según el espejo
    return out

def bolita(r_px, color, semilla=1, pad=10):
    """Una bolita de plasticina de radio [r_px] (RGBA premultiplicado, lienzo cuadrado de 2 r + 2 pad): sombreado ISÓTROPO, solo función de la distancia al centro: brillo centrado, borde parejo y un anillo de oclusión en el filo. Sin ninguna luz que venga de un lado."""
    n = int(2 * r_px + 2 * pad); yy, xx = np.mgrid[0:n, 0:n]; c = (n - 1) / 2.0; rho = np.hypot(xx - c, yy - c) / r_px
    a = np.clip(r_px + 0.5 - np.hypot(xx - c, yy - c), 0, 1)
    m = 1.12 - 0.50 * np.clip(rho, 0, 1) ** 2 - 0.18 * np.clip((rho - 0.78) / 0.22, 0, 1) ** 2                   # centro 1,12 → filo ~0,44
    v = grano((n, n), semilla, 0.012, 0.9, 0.0)
    rgb = np.clip(np.asarray(color)[None, None, :] * (m * v)[..., None], 0, 1)
    return np.concatenate([rgb * a[..., None], a[..., None]], axis=2), a

def almohada(alfa, ancho_dp=9.0, minimo=0.52):
    """El sombreado ISÓTROPO de un módulo inflado: sube de [minimo] en el borde a 1 en el centro con un perfil de cuarto de círculo (el canto redondeado), solo de la distancia al borde de la silueta."""
    d = blur(distancia_adentro(alfa), 1.2); e = np.clip(d / (ancho_dp * DP), 0, 1)
    return minimo + (1.0 - minimo) * np.sqrt(np.clip(1 - (1 - e) ** 2, 0, 1))

def compone_modulo_v2(cara, P, angulo=225.0, flip=True, canto_dp=5.0, bp=BP, bolitas=True, ancho_almohada_dp=9.0):
    """El módulo v2 (ver la cabecera): la silueta EXACTA de la cara (la del sprite del camino 2: no cambia), el sombreado de almohada (isótropo), las bolitas, el canto hacia abajo en la pantalla y las sombras de contacto. Devuelve (RGBA premultiplicado, alfa de la cara, centros de las bolitas)."""
    scale = bp / PX
    pan = resample(cara.copy(), scale, angulo, flip)[::-1]; alfa = pan[..., 3].copy()
    objetivo = np.array([124, 212, 246]) / 255.0                                                              # el celeste cálido
    pill = almohada(alfa, ancho_almohada_dp)
    gr = grano(alfa.shape, 7, 0.012, 0.9, 0.030)                                                              # la variación a mano
    tono = gr[..., None]
    rgb = np.clip(objetivo[None, None, :] * (pill * 1.04)[..., None] * tono, 0, 1)
    cara_m = np.concatenate([rgb * alfa[..., None], alfa[..., None]], axis=2)
    cara_m = aplica_luz(cara_m, arriba=0.16, abajo=0.15)
    centros = centros_en_pantalla(P, angulo, flip, bp)
    # las bolitas y su sombrita de contacto (en la pantalla, hacia abajo): solo sobre la cara
    if bolitas:
        r = 0.33 * bp
        sombra = np.zeros_like(alfa); cuerpo = np.zeros(alfa.shape + (4,))
        for k, (cx, cy) in enumerate(centros):
            b, ab = bolita(r, objetivo * 1.03, semilla=int(round(cx)) * 7 + int(round(cy)) * 13); h = b.shape[0]; x0 = int(round(cx - h / 2.0)); y0 = int(round(cy - h / 2.0))
            disco = np.zeros_like(alfa); ys0, xs0 = max(0, y0), max(0, x0); ys1, xs1 = min(alfa.shape[0], y0 + h), min(alfa.shape[1], x0 + h)
            disco[ys0:ys1, xs0:xs1] = ab[ys0 - y0:ys1 - y0, xs0 - x0:xs1 - x0]
            sombra = np.maximum(sombra, blur(shift(disco, int(round(2.4 * DP))), 2.2) * 0.42)
            yy, xx = np.mgrid[0:alfa.shape[0], 0:alfa.shape[1]]; rho = np.hypot(xx - cx, yy - cy) / r
            anillo = np.clip((1.42 - rho) / 0.42, 0, 1) * (rho >= 0.9) * 0.26                                      # un anillo de oclusión ISÓTROPO alrededor de la base: la bolita está asentada en la cara
            sombra = np.maximum(sombra, anillo)
            pega(cuerpo, b, x0, y0)
        sh = np.concatenate([np.array([0.03, 0.10, 0.26])[None, None, :] * (sombra * alfa)[..., None], (sombra * alfa)[..., None]], axis=2)
        cara_m = over(cara_m, sh); cara_m = over(cara_m, cuerpo)
    base_c = np.array([54, 128, 186]) / 255.0
    s_, c_ = canto_y_sombra(alfa, canto_dp * DP, base_c, sombra_alfa=0.52)
    return over(over(s_, c_), cara_m), alfa, centros

def modulo_suelto(version, bp=BP, fondo=(8, 8, 28), cara=None, P=None, angulo=225.0, flip=True):
    """El módulo de la v1 (1) o la v2 (2) a [bp] px por bloque sobre un fondo liso de noche: para los recortes y las medidas a otro tamaño de bloque (47 u) y con otras piezas (por omisión, la L de la captura)."""
    if P is None:
        L4 = pick_pieces()['L4']; P = normalize([(-x, -y) for x, y in L4])
    cara = cara if cara is not None else perfil_cara(P)
    capa = (compone_modulo_v2(cara, P, angulo, flip, bp=bp)[0]) if version == 2 else compone_modulo(cara, P, angulo, flip, bp=bp)[0]
    img = np.zeros(capa.shape[:2] + (3,)) + (np.array(fondo) / 255.0)[None, None, :]
    return img * (1 - capa[..., 3:4]) + capa[..., :3]

def perfil_cara(P, relieve=3.0):
    """La cara (sprite 384² del camino 2, fila 0 abajo) de la pieza [P] con el relieve pedido."""
    mod = Maestras(RENDER, 'mod', 0); pm = PerfilSDF(*mod.perfil(-1.0, 0.3))
    return compone_c2(P, pm.con_relieve(relieve), aa_px=1.0)

def construye(estado='idle', flip=True, angulo=225.0, relieve=3.0, canto_dp=4.5, quitar_vidrio=True, v2=False):
    real = carga(os.path.join(CAPT, '02-respuesta.png')); llegada = carga(os.path.join(CAPT, '01-llegada.png'))
    L4 = pick_pieces()['L4']; P = normalize([(-x, -y) for x, y in L4])                  # la pieza de la captura: la L girada 180° (el hueco se ve como un «7»)
    # -------- el estudio: la cara con el perfil del camino 2
    mod = Maestras(RENDER, 'mod', 0); pm = PerfilSDF(*mod.perfil(-1.0, 0.3))
    cara = compone_c2(P, pm.con_relieve(relieve), aa_px=1.0)                              # (sprite 384² de la cara, fila 0 abajo)
    # -------- el cielo sin nada
    img = limpia(real, llegada, None)
    # -------- el panel del puerto: una losa gruesa con canto
    pw, ph = PANEL_WH; r = 22 * 3
    ap = np.pad(rect_redondeado(pw, ph, r), 40)
    base_panel = np.array([50, 44, 132]) / 255.0
    s, c, cara_p = objeto_plasticina(base_panel, ap, canto_dp=5.0, bisel_dp=2.6, semilla=11, grano_amp=0.012)
    cara_p = aplica_luz(cara_p, arriba=0.14, abajo=0.12)
    capa = over(over(s, c), cara_p)
    sobre_pantalla(img, capa, PANEL_C[0], PANEL_C[1])
    # el rótulo «Puerto» y los chevrones
    txt = texto(1080, 2400, 'Puerto', fuente(14), np.array([226, 222, 252]) / 255.0, 540, PANEL_C[1] - ph // 2 + 48, 'mm'); pega(img, txt, 0, 0)
    ch = chevron(); col = np.array([92, 200, 194]) / 255.0 if estado == 'idle' else (LIME_P if estado == 'ok' else CYAN_P)
    for i in range(4):
        x = [354, 478, 602, 726][i]; y = PANEL_C[1] + ph // 2 - 33 - 6
        sh, cn, fc = objeto_plasticina(col * 0.9, ch, canto_dp=1.4, bisel_dp=1.2, semilla=20 + i, contraste=0.9)
        sobre_pantalla(img, over(over(sh, cn), fc), x, y)
    # -------- el hueco: el aro de estado y la cavidad
    S, _ = alfa_pieza(P, 0.0)                                                              # la abertura (d ≤ 0), a 78 px por bloque
    ns = S.shape[0]; borde = alfa_pieza(P, INK)[0]; aro = np.clip(borde - S, 0, 1)
    colaro = {'idle': TEAL_P, 'ok': LIME_P, 'show': CYAN_P}[estado]
    pad = 40; Sp = np.pad(S, pad); Ap = np.pad(aro, pad); Bp = np.pad(borde, pad)
    # el aro: un churro (canto de 1,5 dp y su sombra) alrededor de la abertura
    sa, ca, fa = objeto_plasticina(colaro, Ap, canto_dp=1.5, bisel_dp=1.5, semilla=31, contraste=1.0)
    capa_aro = over(over(sa, ca), aplica_luz(fa, arriba=0.12, abajo=0.10))
    # la cavidad: pared de adentro visible arriba (la silueta corrida hacia abajo) y el fondo
    H = int(round(canto_dp * DP)); piso = Sp * shift(Sp, H)                                 # lo que se ve del fondo
    pared = np.clip(Sp - piso, 0, 1)
    sombreado, dist = infla(Sp, 5.0)
    suelo_c = np.array([7, 8, 24]) / 255.0; pared_c = np.array([74, 62, 168]) / 255.0 * 0.85
    gradpared = np.clip(0.55 + 0.45 * np.clip(shift(np.where(Sp > 0.5, 1.0, 0.0), 0) * 0 + 1, 0, 1), 0, 1)
    cav = np.zeros(Sp.shape + (4,))
    cav = over(cav, np.concatenate([pared_c[None, None, :] * pared[..., None], pared[..., None]], axis=2))
    cav = over(cav, np.concatenate([suelo_c[None, None, :] * piso[..., None], piso[..., None]], axis=2))
    # una sombra suave de la pared sobre el fondo (oclusión): el piso se oscurece cerca de la pared de arriba
    capa_hueco = over(cav, np.zeros_like(cav)) if False else cav
    capa_total = over(capa_aro, capa_hueco)
    sobre_pantalla(img, capa_total, HUECO_C[0], HUECO_C[1])
    # -------- los botones del kit
    b1, x1, y1 = boton('losa_md_lima', 'Encaja', icono_encaja, 285, 2031); pega(img, b1, x1, y1)
    b2, x2, y2 = boton('losa_md_lila', 'Espejo', icono_espejo, 795, 2031); pega(img, b2, x2, y2)
    # -------- el módulo
    capa_mod = compone_modulo_v2(cara, P, angulo, flip)[0] if v2 else compone_modulo(cara, P, angulo, flip)[0]
    sobre_pantalla(img, capa_mod, MOD_C[0], MOD_C[1])
    return img, real

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    img, real = construye()
    Image.fromarray((np.clip(construye(v2=True)[0], 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(OUT, 'aco_v2.png'))
    Image.fromarray((np.clip(img, 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(OUT, 'aco_propuesta.png'))
    Image.fromarray((np.clip(real, 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(OUT, 'aco_hoy.png'))
    print('listo', img.shape)
