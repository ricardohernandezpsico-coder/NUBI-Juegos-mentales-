# -*- coding: utf-8 -*-
"""El VOLUMEN de Engranajes (Tarea 115; la propuesta aprobada por Ricardo en `docs/previews/plasticina/propuesta-volumen.png`): lo que se le hace al arte del atlas `engranajes` DESPUÉS del render de Blender y del promedio de cuatro luces, en numpy (se llama desde `empaqueta.py engranajes`).
Receta (la de la hoja): color por ROL con un MAPA DE COLOR (la luz del render recorre una rampa de plasticina: en la Tarea 115 motor azul, camino lila y pieza dorada; desde la Tarea 164, paleta «Madera clara»: motor celeste, camino de arena y pieza uva), un bisel marcado y mate, y todo lo que se vea de frente con la luz fija arriba a la izquierda.
  · Lo que GIRA (engranajes, radar, turbina) lleva un bisel ISÓTROPO (sin dirección: oscurece el borde por parejo): una luz horneada que gire con la pieza la delataría. Su CANTO y su sombra NO van aquí: son capas que no giran (`PlasticCanto` en Unity).
  · Lo que NO gira (polea, portón, marco, plataforma, caja, barra, casco) conserva sus colores, con más vida y un bisel con la luz de la pantalla; el casco gana cuerpo de cilindro y remaches de bolita.
  · Las flechas y los triángulos se tiñen con `Image.color`: van en gris claro con sombreado de tubo (bordes más oscuros) y un brillo arriba a la izquierda.
  · Nuevo: las placas de los carteles y del rótulo MOTOR (nine-slice con su canto, su sombra y su borde levantado, una por estado), un domo (las luces del cohete: una bolita que se tiñe) y una moneda para las llaves.
Los CONTORNOS no se tocan (los dientes son la mecánica): todo se aplica DENTRO de la silueta que ya traen.
Uso: python -I tools/juegos/engranajes_volumen.py   → la hoja de inspección `tools/juegos/out/engranajes_volumen.png` (antes | después de lo que sale del render)."""
import os, sys, math
import numpy as np
from PIL import Image
AQUI = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, AQUI)
from propuesta_util import blur, shift, over, edt, distancia_adentro, infla, grano, rgb_a_hsv, hsv_a_rgb, lum, rampa, DP, canto_y_sombra

# ------------------------------------------------------------------ los mapas de color (de la sombra profunda a la luz de plasticina)
RAMPAS = {
    'lila':  [(0.00, (40, 26, 96)),  (0.30, (98, 66, 184)),  (0.62, (156, 118, 236)), (0.85, (194, 162, 252)), (1.00, (232, 214, 255))],
    'azul':  [(0.00, (14, 42, 106)), (0.30, (28, 94, 192)),  (0.62, (62, 162, 242)),  (0.85, (112, 202, 252)), (1.00, (198, 238, 255))],
    'oro':   [(0.00, (108, 60, 8)),  (0.30, (192, 114, 12)), (0.62, (246, 172, 34)),  (0.85, (255, 206, 72)),  (1.00, (255, 242, 160))],
    'coral': [(0.00, (96, 22, 40)),  (0.30, (180, 50, 68)),  (0.62, (240, 102, 98)),  (0.85, (255, 142, 126)), (1.00, (255, 206, 186))],
    'perla': [(0.00, (96, 70, 132)), (0.30, (172, 142, 204)), (0.62, (226, 204, 238)), (0.85, (248, 236, 250)), (1.00, (255, 252, 255))],
    'noche': [(0.00, (8, 10, 34)),   (0.40, (28, 34, 88)),   (0.80, (50, 58, 132)),   (1.00, (84, 96, 176))],
}
# Tarea 164, paleta 1 «Madera clara» (Ricardo, 24-oct; `auditoria-fable/engranajes-colores.md`): el camino en madera clara (arena), el motor en el celeste del kit y la pieza que mueve la antena o el cohete en el morado de Razonamiento (uva). Las rampas de arriba (lila, azul, oro) siguen siendo las del radar y la turbina.
RAMPAS.update({
    'arena': [(0.00, (92, 56, 28)),  (0.30, (150, 98, 52)),  (0.62, (212, 158, 96)),  (0.85, (238, 200, 146)), (1.00, (255, 238, 208))],
    'cielo': [(0.00, (14, 80, 120)), (0.30, (22, 130, 178)), (0.62, (76, 201, 240)),  (0.85, (150, 226, 250)), (1.00, (224, 248, 255))],
    'uva':   [(0.00, (70, 44, 140)), (0.30, (104, 72, 190)), (0.62, (138, 111, 216)), (0.85, (182, 160, 240)), (1.00, (226, 214, 255))],
})
ROL_PALETA = {0: 'cielo', 1: 'arena', 2: 'uva'}                       # la paleta del engranaje (EngranajesSprites.Palette): motor, camino y ramas, piezas del cohete
PPD = 3.5                                                             # píxeles por dp de estos sprites

def a_float(im):
    """RGBA uint8 → (straight rgb 0..1 (h, w, 3), alfa (h, w))."""
    a = np.asarray(im.convert('RGBA')).astype(np.float64) / 255.0; return a[..., :3], a[..., 3]

def a_imagen(rgb, a):
    return Image.fromarray((np.clip(np.concatenate([rgb, a[..., None]], axis=2), 0, 1) * 255.0 + 0.5).astype(np.uint8), 'RGBA')

def luz_normalizada(rgb, a, pct=(4.0, 99.5)):
    """La luminancia del render llevada a 0..1 (lo oscuro del relieve → 0; lo claro de la cara → 1) con los percentiles de lo que está adentro de la silueta."""
    Y = lum(rgb); sel = a > 0.9
    yl, yh = np.percentile(Y[sel], pct) if sel.any() else (0.0, 1.0)
    return np.clip((Y - yl) / max(yh - yl, 1e-6), 0, 1)

def almohada(a, ancho_dp, fuerza):
    """El bisel ISÓTROPO (sin dirección): el borde se oscurece por parejo hasta [ancho_dp] adentro y la cara sube un punto; devuelve el cambio de luz a sumar (negativo en el borde)."""
    d = blur(distancia_adentro(a), 1.0); e = np.clip(d / (ancho_dp * PPD), 0, 1)                    # la distancia suavizada: sin escalones de píxel en las diagonales de los dientes
    return fuerza * (-(1 - e) ** 2 + 0.35 * e)

def mapa_color(t, rol):
    return rampa(RAMPAS[rol], np.clip(t, 0, 1))

# ------------------------------------------------------------------ lo que gira: engranajes, radar y turbina (bisel isótropo; el degradé radial de color se queda: muestra el sentido del giro)
def gear_volumen(im, size, pal, tip_dp, presionado=False):
    rgb, a = a_float(im); t0 = luz_normalizada(rgb, a); H, W = a.shape
    yy, xx = np.mgrid[0:H, 0:W]; x = (xx + 0.5 - W / 2.0) / PPD; y = (H / 2.0 - yy - 0.5) / PPD
    fx, fy = -tip_dp * 0.35, tip_dp * 0.35
    rr = np.clip(np.hypot(x - fx, y - fy) / (tip_dp * 1.85), 0, 1)                     # 0 junto al foco claro (arriba a la izquierda), 1 en la punta opuesta
    t = t0 * 0.80 + 0.22 * (1 - rr) - 0.02 + almohada(a, 2.2, 0.14) + 0.10
    if presionado: t = t * 0.74 - 0.07                                                  # el motor presionado: más oscuro y más plano
    g = grano(a.shape, 11 + size * 3 + pal, 0.012, 0.9, 0.0) - 1.0
    col = mapa_color(t + g, ROL_PALETA[pal])
    return a_imagen(col, a)

def radar_volumen(im):
    rgb, a = a_float(im); h = rgb_a_hsv(rgb); t0 = luz_normalizada(rgb, a)
    t = t0 * 0.9 + almohada(a, 1.6, 0.30) + 0.04
    col = mapa_color(t, 'perla') * 0.96 + np.array([0.02, 0.10, 0.16]) * (1 - t0)[..., None] * 0.4               # celeste pálido: el radar sigue siendo celeste
    azul = mapa_color(t, 'azul'); mezcla = 0.55
    col = col * (1 - mezcla) + azul * mezcla
    coral = ((h[..., 0] * 360 < 25) | (h[..., 0] * 360 > 335)) & (h[..., 1] > 0.35)                                 # el punto coral de la punta (ASIMÉTRICO: muestra el giro) se conserva
    cm = blur(coral.astype(np.float64), 1.0)[..., None]
    col = col * (1 - cm) + np.clip(rgb * 1.08, 0, 1) * cm
    return a_imagen(col, a)

def turbina_volumen(im):
    rgb, a = a_float(im); t0 = luz_normalizada(rgb, a)
    oscuro = np.clip((0.30 - lum(rgb)) / 0.25, 0, 1)                                    # el cubo (tinta) se queda oscuro
    t = t0 * 0.85 + almohada(a, 1.5, 0.30) - 0.04                                       # el oro de la turbina: el de siempre (rico), no el pálido de la luz
    col = mapa_color(t, 'oro'); col = col * (1 - oscuro[..., None]) + rgb * oscuro[..., None]
    return a_imagen(col, a)

# ------------------------------------------------------------------ lo que NO gira: conserva el color, con vida y un bisel con la luz de la pantalla
def fija_volumen(im, bisel_dp=2.2, contraste=1.0, sat=1.14, aclara=0.0, semilla=3):
    rgb, a = a_float(im)
    s, d = infla(a, bisel_dp * PPD)
    g = grano(a.shape, semilla, 0.012, 0.9, 0.02)
    h = rgb_a_hsv(rgb); h[..., 1] = np.clip(h[..., 1] * sat, 0, 1); col = hsv_a_rgb(h)
    col = np.clip((col + aclara) * (1.0 + (s[..., None] - 1.0) * 0.55 * contraste) * g[..., None], 0, 1)
    return a_imagen(col, a)

def fija_rol(im, rol, bisel_dp=2.2, base=0.10, semilla=3, fuerza=0.16):
    """Lo que NO gira, con la MISMA receta de color de los engranajes (Tarea 116): la luz del render recorre la rampa de plasticina de su rol (Tarea 164: portón y barra en arena, caja en cielo, marco y plataforma en uva; antes lila, azul y dorada), con el bisel y el grano de los engranajes. Antes (`fija_volumen`) conservaba el color gris lavanda del render y se leía más pálido y sucio que los engranajes y que los objetos de Bodega o Correo."""
    rgb, a = a_float(im); t0 = luz_normalizada(rgb, a)
    t = t0 * 0.80 + 0.16 + almohada(a, bisel_dp, fuerza) + base
    g = grano(a.shape, semilla, 0.012, 0.9, 0.0) - 1.0
    return a_imagen(mapa_color(t + g, rol), a)

def polea_volumen(im):
    """La polea (disco oscuro, disco violeta, eje): los tres tonos conservan su lugar; el bisel es isótropo y la luz fija la pone el canto de Unity (no gira: es una polea, pero así queda igual de plana a la vista)."""
    rgb, a = a_float(im)
    s, d = infla(a, 2.6 * PPD); g = grano(a.shape, 5, 0.012, 0.9, 0.02)
    h = rgb_a_hsv(rgb); h[..., 1] = np.clip(h[..., 1] * 1.25 + 0.06, 0, 1); h[..., 2] = np.clip(h[..., 2] * 1.20 + 0.03, 0, 1)
    col = np.clip(hsv_a_rgb(h) * (1.0 + (s[..., None] - 1.0) * 0.60) * g[..., None], 0, 1)
    return a_imagen(col, a)

def blanca_tubo(im, bisel_dp=1.5, base=0.92):
    """Las flechas y los triángulos: gris claro con sombreado de TUBO (los bordes bajan, el lomo sube, un brillo arriba a la izquierda): se tiñen con `Image.color`, así que va como multiplicador entre ~0,58 y 1."""
    rgb, a = a_float(im)
    s, d = infla(a, bisel_dp * PPD)
    luz = np.clip(base + (s - 1.0) * 0.55, 0.56, 1.0)
    col = np.repeat(luz[..., None], 3, axis=2)
    return a_imagen(col, a)

# ------------------------------------------------------------------ el casco: cuerpo de cilindro, bisel grande, remaches de bolita
def domo_rgb(r_px, rol='perla', claro=0.80):
    """Una bolita de plasticina de radio [r_px] como lienzo cuadrado: (rgb, alfa)."""
    n = int(2 * r_px + 4); yy, xx = np.mgrid[0:n, 0:n]; c = (n - 1) / 2.0
    a = np.clip(r_px + 0.5 - np.hypot(xx - c, yy - c), 0, 1)
    s, d = infla(a, r_px); t = np.clip(claro + (s - 1.0) * 1.0, 0, 1)
    return rampa(RAMPAS[rol], t), a

def pega_domo(rgb, a, cx, cy, r_px, rol='perla'):
    """Pega una bolita (con su sombrita de contacto abajo) en [rgb], [a] (straight) centrada en (cx, cy) px."""
    drgb, da = domo_rgb(r_px, rol); n = da.shape[0]; x0 = int(round(cx - n / 2.0)); y0 = int(round(cy - n / 2.0))
    H, W = a.shape
    # sombrita: la bolita corrida 1,3 px abajo, oscura y suave
    sh = blur(shift(da, int(round(1.3 * PPD / 1.5))), 1.1) * 0.45
    ys0, ys1, xs0, xs1 = max(0, y0), min(H, y0 + n), max(0, x0), min(W, x0 + n)
    if ys1 <= ys0 or xs1 <= xs0: return
    sub = (slice(ys0, ys1), slice(xs0, xs1)); loc = (slice(ys0 - y0, ys1 - y0), slice(xs0 - x0, xs1 - x0))
    a_old = a[sub]; rgb_old = rgb[sub]
    # la sombra solo donde ya hay casco (alfa > 0.5)
    base_dark = np.array([0.30, 0.22, 0.42])
    ms = (sh[loc] * (a_old > 0.5))[..., None]; rgb_old = rgb_old * (1 - ms) + base_dark * ms
    m = da[loc][..., None] * (a_old > 0.5)[..., None]
    rgb[sub] = rgb_old * (1 - m) + drgb[loc] * m

def casco_volumen(im):
    rgb, a = a_float(im); h = rgb_a_hsv(rgb); H, S, V = h[..., 0] * 360.0, h[..., 1], h[..., 2]
    coral = blur((((H < 22) | (H > 338)) & (S > 0.35) & (V > 0.3)).astype(np.float64), 1.5)
    noche = blur(((V < 0.40) & (S < 0.7)).astype(np.float64), 1.5)
    perla = np.clip(1 - coral - noche, 0, 1)
    sel = a > 0.9; ys, xs = np.where((perla > 0.5) & sel); x0, x1 = xs.min(), xs.max()
    u = np.clip((np.arange(a.shape[1]) - x0) / max(1.0, x1 - x0), 0, 1)[None, :] * np.ones(a.shape)
    cil = (0.12 * (1 - u) - 0.28 * u ** 1.5) * perla                                    # un cuerpo REDONDO: aclara el lado izquierdo y oscurece el derecho (solo el casco: las aletas quedan rojas)
    t0 = luz_normalizada(rgb, a); med = np.median(t0[sel]); t0 = med + 0.15 * (t0 - med)
    s, d = infla(a, 7.0 * 3.0, suave=1.8)                                               # el casco está a 3 px por dp; la distancia en la curva de la nariz sale escalonada: se suaviza más
    t = np.clip(t0 + (s - 1.0) * 0.85 * 1.25 + cil + 0.08 + (grano(a.shape, 40, 0.012, 0.9, 0.012) - 1.0), 0, 1)
    col = coral[..., None] * rampa(RAMPAS['coral'], t) + noche[..., None] * rampa(RAMPAS['noche'], t) + perla[..., None] * rampa(RAMPAS['perla'], t)
    # los remaches: bolitas por el borde izquierdo del cuerpo (fuera de lo que tapan las piezas y los carteles, que van encima)
    gy0, gy1 = ys.min(), ys.max(); gx0 = x0
    for yy in range(int(gy0 + 130), int(gy1 - 70), 51): pega_domo(col, a, gx0 + 20, yy, 2.6 * 3.0, 'perla')
    return a_imagen(col, a)

# ------------------------------------------------------------------ nuevos: el domo (se tiñe), la moneda de las llaves y las placas (nine-slice) de los carteles y del rótulo MOTOR
def domo_sprite(px=48):
    """El domo (las luces del cohete): una bolita gris claro con su brillo arriba a la izquierda; se tiñe con `Image.color` (oro encendida, noche apagada)."""
    n = px; yy, xx = np.mgrid[0:n, 0:n]; c = (n - 1) / 2.0; r = n / 2.0 - 1.0
    a = np.clip(r + 0.5 - np.hypot(xx - c, yy - c), 0, 1)
    s, d = infla(a, r); luz = np.clip(0.74 + (s - 1.0) * 0.95, 0.42, 1.0)
    brillo = np.clip(1.0 - np.hypot(xx - (c - r * 0.34), yy - (c - r * 0.38)) / (r * 0.42), 0, 1) ** 1.6 * 0.30
    col = np.clip(np.repeat(luz[..., None], 3, axis=2) + brillo[..., None], 0, 1)
    return a_imagen(col, a)

def moneda_sprite(px=96):
    """La moneda de una llave: un disco gris claro con un aro levantado y el centro plano; se tiñe."""
    n = px; yy, xx = np.mgrid[0:n, 0:n]; c = (n - 1) / 2.0; r = n / 2.0 - 1.0
    a = np.clip(r + 0.5 - np.hypot(xx - c, yy - c), 0, 1)
    s, d = infla(a, r * 0.34); luz = np.clip(0.80 + (s - 1.0) * 0.80, 0.46, 1.0)
    col = np.repeat(luz[..., None], 3, axis=2)
    return a_imagen(col, a)

def losa_placa(ancho_px, alto_px, radio_px, rim_px, color_cuerpo, color_rim, canto_dp=5.0, margen=(10, 8, 10, 26), semilla=1):
    """Una placa gruesa (cartel o rótulo): cuerpo con bisel y degradé suave, borde levantado de [rim_px] px, canto de [canto_dp] dp hacia ABAJO y sombra de contacto, todo dentro del lienzo (cuerpo + márgenes (izq, arriba, der, abajo) en px).
    Devuelve (imagen RGBA, margen): el cuerpo es el rectángulo central de [ancho_px] × [alto_px]; el borde levantado llega al límite del cuerpo."""
    ml, mt, mr, mb = margen; W = ancho_px + ml + mr; H = alto_px + mt + mb
    yy, xx = np.mgrid[0:H, 0:W]
    # el alfa del cuerpo: un rectángulo redondeado exacto (distancia con signo)
    cx, cy = ml + ancho_px / 2.0, mt + alto_px / 2.0; hx, hy = ancho_px / 2.0, alto_px / 2.0
    qx = np.abs(xx + 0.5 - cx) - hx + radio_px; qy = np.abs(yy + 0.5 - cy) - hy + radio_px
    sd = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - radio_px
    a_cuerpo = np.clip(0.5 - sd, 0, 1)
    d = distancia_adentro(a_cuerpo)
    c_cuerpo = np.asarray(color_cuerpo, float) / 255.0; c_rim = np.asarray(color_rim, float) / 255.0
    rim = np.clip(1 - (d - rim_px + 0.5), 0, 1) * (a_cuerpo > 0.5)                       # 1 en el aro de [rim_px] px junto al borde
    s_sl, _ = infla(a_cuerpo, 3.4 * PPD)
    s_rim, _ = infla(np.clip(rim, 0, 1), 1.3 * PPD)
    base = np.where(rim[..., None] > 0.5, c_rim[None, None, :], c_cuerpo[None, None, :])
    luz = np.where(rim > 0.5, 1.0 + (s_rim - 1.0) * 0.55, 1.0 + (s_sl - 1.0) * 0.50 + 0.02)
    cara = np.clip(base * luz[..., None], 0, 1)
    cara_rgba = np.concatenate([cara * a_cuerpo[..., None], a_cuerpo[..., None]], axis=2)
    sombra, canto = canto_y_sombra(a_cuerpo, canto_dp * DP, c_cuerpo * 0.62 + 0.02, sombra_alfa=0.52)
    out = over(over(sombra, canto), cara_rgba)
    al = out[..., 3:4]; rgb = np.where(al > 1e-4, out[..., :3] / np.maximum(al, 1e-4), 0.0)
    return a_imagen(rgb, al[..., 0]), (ml, mt, mr, mb)

# colores de los carteles (los de `EngranajesGameController`): cuerpo y borde por estado
CARTEL = {'neutra': ((28, 35, 80), (255, 201, 74)), 'ok': ((15, 59, 51), (159, 245, 214)), 'mala': ((74, 30, 34), (255, 140, 107))}
