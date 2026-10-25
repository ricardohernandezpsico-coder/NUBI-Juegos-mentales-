# -*- coding: utf-8 -*-
"""Medidas de «Engranajes, primera parte» (Tarea 164, 24-oct-2026): la mesa de taller (losa crema con la ventanilla noche) contra el panel de antes, sobre CAPTURAS REALES de Unity (1080 × 2400, `bash tools/verificar-todo.sh --capturas engranajes`).
Mide, por captura: (1) el fondo de la ventanilla (o del panel de antes) contra el cielo; (2) el contraste (WCAG) de lo que hay debajo de un engranaje (el canto lila, el canto hondo y la sombra de contacto) contra ese fondo, que es lo que pide la spec de Fable (≥ 1,5 : 1; hoy ≈ 1,1 : 1);
(3) el diente contra el hueco entre dientes (el fondo): que no baje de 3 : 1 con la paleta «Madera clara» (Tarea 164, parte de los colores); (4) la mesa crema contra el cielo. Sirve con cualquier paleta de engranajes: la cara se busca por luz, no por tono.
Uso:  python -I tools/juegos/mide_engranajes_mesa.py <carpeta de «hoy»> <carpeta de la propuesta> [toma]      (toma = «t20» por omisión, con o sin su número de orden; el panel de antes y la ventanilla se buscan solos)
"""
import sys
import numpy as np
from PIL import Image

sys.stdout.reconfigure(encoding='utf-8')


def lin(c):
    c = np.asarray(c, float) / 255.0
    return np.where(c <= 0.03928, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def lum(rgb):
    r, g, b = lin(rgb[0]), lin(rgb[1]), lin(rgb[2])
    return float(0.2126 * r + 0.7152 * g + 0.0722 * b)


def ratio(a, b):
    la, lb = lum(a), lum(b)
    return (max(la, lb) + 0.05) / (min(la, lb) + 0.05)


def bbox_ventana(im, mesa):
    """La caja (x0, y0, x1, y1) de la ventanilla (con la mesa) o del panel de antes (sin ella), en píxeles de la captura."""
    h, w = im.shape[:2]
    if mesa:
        # la ventanilla: el rectángulo azul violeta oscuro rodeado de crema; se busca desde el centro de la mesa
        crema = (abs(im[..., 0] - 255) < 14) & (abs(im[..., 1] - 236) < 18) & (abs(im[..., 2] - 214) < 26)
        ys, xs = np.where(crema[200:1400])
        return (int(xs.min()), int(ys.min()) + 200, int(xs.max()), int(ys.max()) + 200)
    return (72, 592, 710, 1510)


def mide(nombre, path, mesa):
    im = np.asarray(Image.open(path).convert('RGB')).astype(int)
    out = {'nombre': nombre}
    if mesa:
        # la mesa crema: sus filas de arriba abajo (desde que la mesa baja un poco de la cabecera, «aire repartido», su lugar depende del teléfono); la ventanilla empieza 86 px bajo su borde
        crema = (abs(im[..., 0] - 255) < 14) & (abs(im[..., 1] - 236) < 18) & (abs(im[..., 2] - 214) < 26)
        filas = np.where(crema[:, :].sum(axis=1) > 300)[0]
        top = int(filas[filas > 200][0]); bottom = top
        for yy in range(top, im.shape[0]):
            if crema[yy].sum() <= 300: break
            bottom = yy
        win_box = (90, top + 86, 700, top + 986)
        sky_pt = (300, top - 40)                                      # el cielo justo sobre la mesa (el del panel de antes se toma también entre la cabecera y el panel)
    else:
        win_box = (80, 600, 700, 1500)
        sky_pt = (300, 400)
    x0, y0, x1, y1 = win_box
    reg = im[y0:y1, x0:x1]
    flat = reg.reshape(-1, 3)
    dark = flat[flat.sum(axis=1) < 330]
    vals, counts = np.unique(dark, axis=0, return_counts=True)
    bg = tuple(int(v) for v in vals[counts.argmax()])
    sky = tuple(int(v) for v in im[sky_pt[1], sky_pt[0]])
    out['fondo'] = bg
    out['cielo'] = sky
    out['fondo_cielo'] = ratio(bg, sky)
    if mesa:
        cream = tuple(int(v) for v in im[top + 36, 300])
        out['crema'] = cream
        out['crema_cielo'] = ratio(cream, sky)
    cartel = (reg[..., 0] > 235) & (reg[..., 1] > 215) & (reg[..., 2] > 185)           # los rótulos crema (MOTOR, carteles) no son engranajes
    face = (reg.sum(axis=2) > 420) & ~cartel                                            # la cara de un engranaje, de cualquier paleta (lila, arena, celeste, uva)
    best = None
    for x in range(40, reg.shape[1] - 40, 4):
        ys = np.where(face[:, x])[0]
        if ys.size < 40:
            continue
        yb = ys.max()
        if yb > reg.shape[0] - 90:
            continue
        if best is None or yb > best[1]:
            best = (x, yb)
    if best is None:
        return out
    x, yb = best
    perfil = [tuple(int(v) for v in reg[yb + d, x]) for d in range(0, 64, 4)]
    out['diente'] = perfil[0]
    out['diente_fondo'] = ratio(perfil[0], bg)
    canto = perfil[1:5]                                  # +4 a +16 px: el canto (la pared del engranaje)
    out['canto'] = (min(ratio(c, bg) for c in canto), max(ratio(c, bg) for c in canto))
    out['canto_color'] = canto[len(canto) // 2]
    hondo = perfil[5:16]                                 # +20 a +60 px: el canto hondo y la sombra de contacto
    oscuro = min(hondo, key=lum)
    out['sombra'] = oscuro
    out['sombra_fondo'] = ratio(oscuro, bg)
    return out


def muestra(o):
    print('==', o['nombre'])
    print('  fondo %s contra el cielo %s: %.2f : 1' % (o['fondo'], o['cielo'], o['fondo_cielo']))
    if 'crema' in o:
        print('  la mesa crema %s contra el cielo: %.1f : 1' % (o['crema'], o['crema_cielo']))
    if 'diente' in o:
        print('  diente %s contra el hueco (el fondo): %.1f : 1 (pedido: ≥ 3 : 1)' % (o['diente'], o['diente_fondo']))
        print('  canto (+4 a +16 px bajo el engranaje) %s contra el fondo: de %.2f a %.2f : 1 (la spec de la paleta «Madera clara» pide ≥ 1,5 : 1)' % ((o['canto_color'],) + o['canto']))
        print('  lo más oscuro del canto hondo y la sombra %s contra el fondo: %.2f : 1 (pedido por la spec: ≥ 1,5 : 1)' % (o['sombra'], o['sombra_fondo']))


def buscar(carpeta, toma):
    """La captura de [toma] en [carpeta]: el nombre exacto («05-t20») o sin el número de orden («t20»: la numeración cambia cuando se suma una toma)."""
    import glob, os
    exacta = os.path.join(carpeta, toma + '.png')
    if os.path.exists(exacta): return exacta
    hallada = sorted(glob.glob(os.path.join(carpeta, '*-' + toma + '.png')))
    if not hallada: raise FileNotFoundError(toma + ' en ' + carpeta)
    return hallada[0]


if __name__ == '__main__':
    hoy, nuevo = sys.argv[1], sys.argv[2]
    toma = sys.argv[3] if len(sys.argv) > 3 else 't20'
    muestra(mide('HOY (panel de antes)', buscar(hoy, toma), False))
    muestra(mide('PROPUESTA (ventanilla de la mesa)', buscar(nuevo, toma), True))
