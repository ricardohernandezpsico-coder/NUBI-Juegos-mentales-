# -*- coding: utf-8 -*-
"""Acoplamiento v3 (Tarea 117, SOLO PROPUESTA): la pantalla de juego y la pantalla final armadas sobre las capturas REALES (1080 × 2400 = 360 × 800 dp), con lo de `acopla_v3.py` (pieza, puerto) y lo de Blender (`acoplamiento_v3.py`: íconos, chevrones, luces).
`plan_hoy` es una copia de `DockingPlan` (DockingLayout.cs) y `plan_v3` el plan propuesto: SIN la estación (el aro, el cilindro y los casilleros), con la fila de luces de avance junto al HUD y la pieza y el puerto más grandes."""
import os, sys, math
import numpy as np
from PIL import Image
AQUI = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, AQUI)
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
import acopla_v3 as A
from acopla_geo import normalize, pick_pieces
from propuesta_util import (DP, blur, shift, over, pega, grano, rampa, canto_y_sombra, losa_kit, sprite_kit, texto, rect_redondeado, dibuja, cielo_entre, lum, FUENTE)
from PIL import ImageFont

CAPT = os.path.join(REPO, 'unity', 'test-results', 'capturas')
ASSETS = os.path.join(AQUI, 'out', 'acoplamiento_v3')
FONTS = os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Fonts')
W_PX, H_PX = 1080, 2400
INK = np.array([0x1A, 0x12, 0x40]) / 255.0; LAV = np.array([226, 222, 252]) / 255.0; GRIS_LAV = np.array([176, 170, 222]) / 255.0

def fuente_dp(dp, nombre='Fredoka-Bold.ttf'): return ImageFont.truetype(os.path.join(FONTS, nombre), int(round(dp * DP)))

def carga(path): return np.asarray(Image.open(path).convert('RGB')).astype(np.float64) / 255.0

def sprite(nombre, ancho_px):
    """Un PNG de Blender (RGBA recto) → RGBA premultiplicado reducido a [ancho_px] de ancho (LANCZOS sobre el premultiplicado, sin halos)."""
    im = Image.open(os.path.join(ASSETS, nombre + '.png')).convert('RGBA'); a = np.asarray(im).astype(np.float64) / 255.0
    pm = np.concatenate([a[..., :3] * a[..., 3:4], a[..., 3:4]], axis=2)
    h = int(round(im.height * ancho_px / im.width)); out = np.zeros((h, int(ancho_px), 4))
    for k in range(4): out[..., k] = np.asarray(Image.fromarray(pm[..., k].astype(np.float32), 'F').resize((int(ancho_px), h), Image.LANCZOS)).astype(np.float64)
    return np.clip(out, 0, 1)

def pon_centro(img, rgba, cx, cy): h, w = rgba.shape[:2]; pega(img, rgba, int(round(cx - w / 2.0)), int(round(cy - h / 2.0)))

# ------------------------------------------------------------------ los planes (dp lógicos de un campo de 360 de ancho; y hacia abajo)
HUD_BOTTOM = 190.0 / 3.0

def plan_hoy(alto, guiado=False):
    """Una copia de `DockingPlan` (DockingLayout.cs): lo que hay hoy."""
    eff = alto - (96.0 if guiado else 0.0); strip_bottom = 278.0
    K = max(0.6, min(1.0, (eff - strip_bottom - 6.0) / (741.0 - strip_bottom))); extra = max(0.0, eff - 780.0)
    down = lambda ref, w: strip_bottom + (ref - strip_bottom) * K + extra * w
    B = 26.0 * K
    return dict(K=K, bloque=B, modulo_cy=down(360, 0.30), modulo_r=math.sqrt(32.0) * 0.5 * B, barra_cy=down(444, 0.35), puerto_cy=down(534, 0.50), puerto_w=168 * K, puerto_h=146 * K,
                boton_cy=down(660, 0.85), boton_w=158 * K, boton_h=76 * K, pista_cy=down(734, 0.90), franja=(246.0, 278.0), estacion=True)

BLOQUE_V3 = 38.0
def plan_v3(alto, guiado=False):
    """El plan propuesto: sin estación; la fila de luces (y 100) y la franja de avisos (y 118-150) suben; todo lo de abajo se mide desde la franja y se achica parejo (mínimo 0,6) como hoy; lo que sobra se reparte hacia abajo."""
    eff = alto - (96.0 if guiado else 0.0); strip_bottom = 150.0; needed = 737.0 - strip_bottom
    K = max(0.6, min(1.0, (eff - strip_bottom - 6.0) / needed)); extra = max(0.0, eff - 780.0)
    B = BLOQUE_V3 * K; r = math.sqrt(32.0) * 0.5 * B
    mod_cy = strip_bottom + 8 * K + r + extra * 0.20
    barra_cy = mod_cy + r + 12 * K + extra * 0.08
    port_h = 5.62 * B; port_w = 6.46 * B
    puerto_cy = barra_cy + 14 * K + port_h / 2 + extra * 0.20
    boton_h = 76 * K; boton_w = 158 * K
    boton_cy = puerto_cy + port_h / 2 + 14 * K + boton_h / 2 + extra * 0.30
    pista_cy = boton_cy + boton_h / 2 + 20 * K + extra * 0.20
    return dict(K=K, bloque=B, modulo_cy=mod_cy, modulo_r=r, barra_cy=barra_cy, puerto_cy=puerto_cy, puerto_w=port_w, puerto_h=port_h, boton_cy=boton_cy, boton_w=boton_w, boton_h=boton_h, pista_cy=pista_cy,
                franja=(118.0, strip_bottom), luces_cy=100.0, estacion=False)

# ------------------------------------------------------------------ el fondo: la captura sin lo que cambia
def estrellas(img, caja, n, semilla):
    """Unas pocas estrellitas (puntos suaves) en [caja] = (x0, y0, x1, y1): lo que se llevó la limpieza del cielo."""
    rng = np.random.RandomState(semilla); x0, y0, x1, y1 = caja
    for _ in range(n):
        x = rng.randint(x0, x1); y = rng.randint(y0, y1); r = rng.choice([1.0, 1.0, 1.6, 2.2]); a = rng.uniform(0.25, 0.85)
        ys, xs = np.mgrid[y - 4:y + 5, x - 4:x + 5]; m = np.clip(1.0 - np.hypot(xs - x, ys - y) / (r + 0.6), 0, 1) * a
        reg = img[y - 4:y + 5, x - 4:x + 5]
        if reg.shape[:2] == (9, 9): img[y - 4:y + 5, x - 4:x + 5] = reg * (1 - m[..., None]) + np.array([0.86, 0.9, 1.0])[None, None, :] * m[..., None]

def vertical(img, y0, y1, x0, x1, ya, yb):
    """Rellena [y0:y1, x0:x1] interpolando en VERTICAL entre la fila [ya] (arriba) y la [yb] (abajo): para una franja con algo brillante a lo ancho (una barra) que el relleno por filas no quita."""
    t = ((np.arange(y0, y1) - y0 + 0.5) / (y1 - y0))[:, None, None]
    a = img[ya - 2:ya + 1, x0:x1].mean(axis=0)[None, :, :]; b = img[yb:yb + 3, x0:x1].mean(axis=0)[None, :, :]
    img[y0:y1, x0:x1] = a * (1 - t) + b * t

def fondo_juego():
    """La captura REAL de «respuesta» sin la estación, el módulo, la barra de tiempo, el puerto, los botones ni la pista: el cielo y el planeta de la captura y el marcador de arriba (que no cambia)."""
    img = carga(os.path.join(CAPT, 'acoplamiento', '02-respuesta.png'))
    cielo_entre(img, 420, 790, 20, 1060, (0, 16), (1064, 1080))                              # la estación (y los casilleros punteados)
    vertical(img, 1318, 1384, 74, 1045, 1306, 1396)                                           # el rótulo y la barra «Tiempo» de hoy (el relleno por filas se llevaría su brillo)
    cielo_entre(img, 790, 1890, 140, 1040, (132, 150), (1050, 1068))                         # el módulo, la barra y el puerto (el planeta queda a la izquierda)
    cielo_entre(img, 1890, 2330, 0, 1080, (0, 12), (1068, 1080))                              # los botones y la pista
    estrellas(img, (150, 430, 1000, 780), 9, 3); estrellas(img, (150, 800, 1000, 1850), 12, 5); estrellas(img, (20, 1900, 1060, 2300), 12, 8)
    return img

def fondo_final():
    """La captura REAL de la pantalla final sin la estación de anillos (las medidas de abajo no cambian)."""
    img = carga(os.path.join(CAPT, 'acoplamiento', '11-final.png'))
    cielo_entre(img, 262, 750, 0, 1080, (24, 80), (1000, 1056))                              # (en la captura el cielo cambia de tono justo en y = 750: la limpieza llega hasta ahí)
    estrellas(img, (40, 280, 1040, 740), 8, 11)
    return img

# ------------------------------------------------------------------ piezas de la interfaz v3
def barra_tiempo(img, cx, cy, ancho, frac=0.86):
    """La barra de «Tiempo»: una pastilla oscura con el relleno ámbar de plasticina (tubo), el rótulo a la izquierda (14 dp)."""
    h = 12 * DP; w = ancho * DP; wi, hi = int(round(w)), int(round(h))
    pista = rect_redondeado(wi, hi, hi / 2.0); pad = 14
    # pista: una cavidad (oscura con el borde de arriba en sombra)
    cav = np.concatenate([np.array([10, 10, 34])[None, None, :] / 255.0 * np.pad(pista, pad)[..., None], np.pad(pista, pad)[..., None]], axis=2)
    pon_centro(img, cav, cx, cy)
    wf = max(hi, int(round(wi * frac))); rel = np.pad(rect_redondeado(wf, hi - 4, (hi - 4) / 2.0), pad)
    s, c, cara = A_cuerpo((255, 196, 64), rel, canto_dp=1.0, bisel_dp=2.2, semilla=5, grano_amp=0.010)
    capa = over(over(s, c), cara)
    pega(img, capa, int(round(cx - wi / 2.0 - pad + 2)), int(round(cy - (hi - 4) / 2.0 - pad)))

def A_cuerpo(color, alfa, canto_dp, bisel_dp, semilla, grano_amp=0.03):
    from propuesta_util import objeto_plasticina
    return objeto_plasticina(np.asarray(color) / 255.0, alfa, canto_dp=canto_dp, bisel_dp=bisel_dp, semilla=semilla, grano_amp=grano_amp)

def boton_kit(img, tono, texto_boton, icono, cx, cy, ancho_dp, alto_dp, presionada=False):
    """Un botón del kit TAL CUAL (losa lima o uva; cuerpo de [ancho_dp] × [alto_dp]) con el ícono de churro de tinta a la izquierda y el texto liso de tinta (22 dp): la receta de VERDAD / DISPARATE y de «Arrancar»."""
    wb, hb = int(round(ancho_dp * DP)), int(round(alto_dp * DP))
    losa, (ml, mt) = losa_kit('losa_md_' + tono + ('_p' if presionada else ''), wb, hb)
    h, w = losa.shape[:2]; x0 = int(round(cx - wb / 2.0)) - ml; y0 = int(round(cy - hb / 2.0)) - mt
    capa = np.zeros((h, w, 4)); baja = int(round(2 * DP)) if presionada else 0                         # el kit baja el texto 2 dp al presionar (la losa presionada ya viene aplastada)
    ico = sprite(icono, 54 * DP); ih = ico.shape[0]
    pega(capa, ico, int(ml + 2 * DP), int(mt + hb / 2.0 - ih / 2.0) + baja)
    t = texto(w, h, texto_boton, fuente_dp(22), INK, int(ml + 62 * DP), int(mt + hb / 2.0) + baja, 'lm'); capa = over(capa, t)
    pega(img, over(losa, capa), x0, y0)

def panel_puerto(img, cxp, cyp, ancho_dp, alto_dp):
    """El panel «Puerto»: la losa «noche» del kit (la de las casillas de Rescate) con el rótulo de 15 dp. Centro en PÍXELES."""
    wb, hb = int(round(ancho_dp * DP)), int(round(alto_dp * DP))
    losa, (ml, mt) = losa_kit('losa_md_noche', wb, hb)
    pega(img, losa, int(round(cxp - wb / 2.0)) - ml, int(round(cyp - hb / 2.0)) - mt)
    t = texto(W_PX, H_PX, 'Puerto', fuente_dp(15), LAV, int(cxp), int(cyp - hb / 2.0 + 17 * DP), 'mm'); pega(img, t, 0, 0)

def chevrones(img, cx, cy, ancho_dp, estado='reposo', cuantos_ok=4):
    nombre = {'reposo': 'chev_verde_agua', 'ok': 'chev_lima', 'mostrar': 'chev_celeste'}[estado]
    paso = ancho_dp / 4.2 * 1.0
    for i in range(4):
        nm = nombre if (estado != 'ok' or i < cuantos_ok) else 'chev_verde_agua'
        pon_centro(img, sprite(nm, 24 * DP), cx + (i - 1.5) * 26 * DP, cy)

def fila_luces(img, cx, cy, encendidas, completas=False, destello=None, paso_dp=30.0, medalla=0, lima=0):
    """La fila de avance: 8 luces redondas de plasticina (apagada / oro / lima) de 22 dp cada 30 dp; con [completas] todas lima. [destello] = índice de la luz que acaba de encenderse (un halo suave)."""
    for i in range(8):
        x = cx + (i - 3.5) * paso_dp * DP
        on = i < encendidas
        nom = 'luz_lima' if (completas or i < lima) else ('luz_oro' if on else 'luz_apagada')
        if destello is not None and i == destello:
            gl = np.zeros((int(60 * DP),) * 2 + (4,)); yy, xx = np.mgrid[0:gl.shape[0], 0:gl.shape[1]]; r = np.hypot(xx - gl.shape[1] / 2, yy - gl.shape[0] / 2) / (30 * DP)
            al = np.clip(1 - r, 0, 1) ** 2 * 0.55; col = np.array([1.0, 0.82, 0.34]); gl[..., :3] = col[None, None, :] * al[..., None]; gl[..., 3] = al
            pon_centro(img, gl, x, cy)
        pon_centro(img, sprite(nom, 30 * DP), x, cy)

def toast(img, plan, cadena, tono='sol'):
    """Un aviso en la franja: la losa chica del kit (sol) con el texto liso de tinta (17 dp)."""
    wb, hb = int(len(cadena) * 10.5 * DP * 0.62 + 44 * DP), int(30 * DP); losa, (ml, mt) = losa_kit('losa_ch_' + tono, wb, hb)
    fy = (plan['franja'][0] + plan['franja'][1]) / 2.0 * DP; pega(img, losa, int(540 - wb / 2) - ml, int(fy - hb / 2) - mt)
    t = texto(W_PX, H_PX, cadena, fuente_dp(17), INK, 540, int(fy), 'mm'); pega(img, t, 0, 0)

def medalla_anillos(img, plan, n):
    """La medalla de los anillos completos: una luz lima más grande con el número, a la derecha de la fila."""
    cy = plan['luces_cy'] * DP; mx = 540 + 4.7 * 30 * DP
    pon_centro(img, sprite('luz_lima', 40 * DP), mx, cy)
    t = texto(W_PX, H_PX, str(n), fuente_dp(16), INK, int(mx), int(cy - 1.5 * DP), 'mm'); pega(img, t, 0, 0)

def textos(img, plan, mostrar_pista=True):
    if mostrar_pista:
        t = texto(W_PX, H_PX, 'Gira la pieza en tu mente y compárala con el hueco', fuente_dp(14, 'Fredoka-SemiBold.ttf'), GRIS_LAV, 540, int(plan['pista_cy'] * DP), 'mm'); pega(img, t, 0, 0)

def pieza_en(img, P, plan, angulo=225.0, flip=True, cy=None, escala=1.0, canto_dp=5.0, sombra=True):
    cy = plan['modulo_cy'] if cy is None else cy
    bp = plan['bloque'] * DP * escala
    pz, al, d = A.pieza(P, bp=bp, angulo=angulo, flip=flip, canto_dp=canto_dp, sombra=sombra)
    pon_centro(img, pz, 540, cy * DP); return pz

def puerto_en(img, P, plan, estado='reposo', asentada=False, angulo=0.0, flip=False, marca_ok=False):
    cxp, cyp = 540.0, plan['puerto_cy'] * DP
    panel_puerto(img, cxp, cyp, plan['puerto_w'], plan['puerto_h'])
    bp = plan['bloque'] * DP; hy = cyp - 3 * DP
    if asentada: capa, ab = A.pieza_asentada(P, bp=bp, angulo=angulo, flip=flip, estado=estado)
    else: capa, ab, d = A.puerto(P, bp=bp, angulo=angulo, flip=flip, estado=estado)
    pon_centro(img, capa, cxp, hy)
    chevrones(img, cxp, cyp + (plan['puerto_h'] / 2 - 15) * DP, plan['puerto_w'], estado)
    if marca_ok:
        tam = int(26 * DP); mk = sprite_kit('marca_ok')
        mk2 = np.asarray(Image.fromarray((np.clip(mk, 0, 1) * 255).astype(np.uint8), 'RGBA').resize((tam, tam), Image.LANCZOS)).astype(np.float64) / 255.0
        pon_centro(img, mk2, cxp + (plan['puerto_w'] / 2 - 22) * DP, cyp - (plan['puerto_h'] / 2 - 22) * DP)

def botones(img, plan):
    cy = plan['boton_cy'] * DP; gap = 12 * plan['K']
    boton_kit(img, 'lima', 'Encaja', 'ico_encaja', 540 - (gap + plan['boton_w']) * 0.5 * DP, cy, plan['boton_w'], plan['boton_h'])
    boton_kit(img, 'lila', 'Espejo', 'ico_espejo', 540 + (gap + plan['boton_w']) * 0.5 * DP, cy, plan['boton_w'], plan['boton_h'])

def pantalla_v3(P_pieza, P_hueco, angulo=225.0, flip=True, estado='reposo', asentada=False, luces=0, completas=False, destello=None, pieza_visible=True, pieza_cy=None, pieza_escala=1.0, pieza_canto=5.0, hueco_angulo=0.0, marca_ok=False, altura=800, base=None, etiqueta=True):
    """La pantalla de juego v3 (1080 × 2400, RGB 0..1) sobre el fondo de la captura real."""
    plan = plan_v3(altura); img = (fondo_juego() if base is None else base.copy())
    fila_luces(img, 540, plan['luces_cy'] * DP, luces, completas, destello)
    barra_tiempo(img, 540 + 18 * DP, plan['barra_cy'] * DP, 200 * plan['K'] if plan['K'] > 0 else 200)
    if etiqueta: t = texto(W_PX, H_PX, 'Tiempo', fuente_dp(14, 'Fredoka-SemiBold.ttf'), LAV, int(540 - 118 * DP), int(plan['barra_cy'] * DP), 'rm'); pega(img, t, 0, 0)
    puerto_en(img, P_hueco, plan, estado, asentada, hueco_angulo, False, marca_ok)
    botones(img, plan)
    if pieza_visible: pieza_en(img, P_pieza, plan, angulo, flip, pieza_cy, pieza_escala, pieza_canto)
    textos(img, plan, not asentada)
    return img

def a_u8(a): return Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8))

# ------------------------------------------------------------------ la pantalla final: «tu estación» como escultura de plasticina
RAMPA_NUCLEO = [(0.00, (60, 46, 140)), (0.35, (112, 92, 206)), (0.62, (170, 146, 236)), (0.86, (214, 196, 252)), (1.00, (248, 240, 255))]
def nucleo(w_dp, h_dp, ry_dp):
    """El núcleo de la escultura: un cilindro de plasticina lavanda (la luz de la lámpara arriba a la izquierda: claro a la izquierda, sombra a la derecha) con su tapa elíptica arriba. RGBA premultiplicado (fila 0 ARRIBA); la tapa llega hasta la fila ry·DP."""
    w, h, ry = int(round(w_dp * DP)), int(round(h_dp * DP)), int(round(ry_dp * DP)); H = h + 2 * ry; pad = 6
    ys, xs = np.mgrid[0:H, 0:w]; cx = (w - 1) / 2.0; nx = np.clip((xs - cx) / (w / 2.0), -1, 1)
    top = np.clip(1 - (((xs - cx) / (w / 2.0)) ** 2 + ((ys - ry) / float(ry)) ** 2) ** 0.5, 0, 1)                     # tapa de arriba
    cuerpo = ((ys >= ry) & (ys <= H - ry)).astype(float)
    bot = np.clip(1 - (((xs - cx) / (w / 2.0)) ** 2 + ((ys - (H - ry)) / float(ry)) ** 2) ** 0.5, 0, 1)
    ab = np.clip(np.maximum.reduce([cuerpo, (top > 0).astype(float), (bot > 0).astype(float)]), 0, 1)
    nz = np.sqrt(np.clip(1 - nx ** 2, 0, 1)); n = np.stack([nx, np.zeros_like(nx), nz], axis=-1)
    dif, spec = A.lampara(n, 0.25, 14.0); t = 0.60 + 0.36 * (dif - 1.0) + spec * 0.4
    g = grano(ab.shape, 41, 0.010, 0.9, 0.02); rgb = rampa(RAMPA_NUCLEO, np.clip(t, 0, 1) * (0.97 + 0.03 * g))
    en_tapa = ((((xs - cx) / (w / 2.0)) ** 2 + ((ys - ry) / float(ry)) ** 2) <= 1.0) & (ys < ry + 2)
    rgb[en_tapa] = rampa(RAMPA_NUCLEO, 0.84)[None, :][0] if False else rampa(RAMPA_NUCLEO, np.full(en_tapa.sum(), 0.86))
    return np.concatenate([rgb * ab[..., None], ab[..., None]], axis=2)

def pista_anillo(rx_dp, ry_dp, grosor_dp, color, mitad):
    """La pista elíptica de un anillo como un tubo de plasticina; [mitad] = 'atras' (la mitad de arriba de la elipse) o 'adelante' (la de abajo). RGBA premultiplicado centrado."""
    rx, ry, g = rx_dp * DP, ry_dp * DP, grosor_dp * DP; w, h = int(2 * rx + 2 * g + 16), int(2 * ry + 2 * g + 16)
    im = dibuja(w, h, lambda d, ss: d.ellipse(((w / 2.0 - rx - g / 2) * ss, (h / 2.0 - ry - g / 2) * ss, (w / 2.0 + rx + g / 2) * ss, (h / 2.0 + ry + g / 2) * ss), outline=255, width=int(g * ss)))
    yy = np.arange(h)[:, None] * np.ones((1, w)); sel = (yy < h / 2.0) if mitad == 'atras' else (yy >= h / 2.0)
    s, c, cara = A_cuerpo(color, im * sel, canto_dp=0.9, bisel_dp=1.3, semilla=17, grano_amp=0.010)
    return over(over(s, c), cara)

def escultura(img, acopladas=23, semilla=4, centro_x=540, ring_y_dp=(219, 179, 139), rx_dp=104.0, ry_dp=20.0, bp_dp=10.0):
    """«Tu estación»: [acopladas] piezas (las de verdad, 5 formas en giros al azar) en anillos de 8 alrededor de un núcleo, con la lámpara fija. Los anillos de abajo están completos (pista lima) y el de arriba en curso (pista lavanda)."""
    rng = np.random.RandomState(semilla); piezas = pick_pieces(); nombres = list(piezas)
    n_anillos = int(math.ceil(acopladas / 8.0)); cuentas = [min(8, acopladas - 8 * i) for i in range(n_anillos)]
    bp = bp_dp * DP; esc = bp / A.BP
    sprites = []
    for i in range(acopladas):
        P = piezas[nombres[rng.randint(len(nombres))]]
        for _ in range(rng.randint(4)): P = normalize([(-y, x) for x, y in P])
        pz, al, d = A.pieza(P, bp=bp, angulo=0.0, flip=False, canto_dp=1.4, sombra=True, ancho_dp=15.0 * esc, semilla=3 + i)
        sprites.append(pz)
    ring_y = list(ring_y_dp)[:n_anillos]                                           # el 0 es el de ABAJO
    ring_y = ring_y[:n_anillos]
    top_y = (ring_y[-1] - ry_dp - 10) * DP; bot_y = (ring_y[0] + ry_dp + 12) * DP
    core = nucleo(46, (bot_y - top_y) / DP - 2 * 9, 9)
    # el orden de dibujo: atrás (pistas y piezas), el núcleo, adelante (pistas y piezas)
    cols_pista = [(166, 227, 107) if i < n_anillos - 1 or cuentas[-1] == 8 else (140, 120, 236) for i in range(n_anillos)]
    cx = centro_x
    for i in reversed(range(n_anillos)):
        pon_centro(img, pista_anillo(rx_dp, ry_dp, 3.4, cols_pista[i], 'atras'), cx, ring_y[i] * DP + 0)
    def colocar(frente):
        items = []
        for i in range(n_anillos):
            for k in range(cuentas[i]):
                a = math.pi * 0.5 + k * 2 * math.pi / 8.0; dep = math.sin(a)
                if (dep >= 0) == frente: items.append((ring_y[i] + ry_dp * dep, i, k, a, dep))
        for (yy, i, k, a, dep) in sorted(items):
            spr = sprites[sum(cuentas[:i]) + k]; f = 1.0 if dep >= 0 else 0.88 + 0.06 * (dep + 1) * 0.0
            if f != 1.0:
                h0, w0 = spr.shape[:2]; im = Image.fromarray((np.clip(spr, 0, 1) * 255).astype(np.uint8), 'RGBA').resize((int(w0 * f), int(h0 * f)), Image.LANCZOS); spr = np.asarray(im).astype(np.float64) / 255.0
            if dep < 0:
                spr = spr.copy(); spr[..., :3] = spr[..., :3] * (0.80 + 0.12 * (dep + 1)) + np.array([0.02, 0.03, 0.10])[None, None, :] * spr[..., 3:4] * 0.5
            pon_centro(img, spr, cx + rx_dp * math.cos(a) * DP, (ring_y[i] + ry_dp * dep) * DP)
    colocar(False)
    pon_centro(img, core, cx, (top_y + bot_y) / 2.0)
    for i in reversed(range(n_anillos)):
        pon_centro(img, pista_anillo(rx_dp, ry_dp, 3.4, cols_pista[i], 'adelante'), cx, ring_y[i] * DP)
    colocar(True)
    return img

def pantalla_final_v3(acopladas=23):
    img = fondo_final(); escultura(img, acopladas); return img
