# -*- coding: utf-8 -*-
"""Lleva las piezas del kit para Avance y Juegos (Tarea 124, renders de `isla.py --kit --kit-solo avance`) a la app (Tarea 125):
   - `app/src/main/res/drawable-nodpi/kit_<nombre>.webp` (a 3 px por dp, sin que Android los reescale; WebP calidad 92 con alfa) con la bruma del «shadow catcher» recortada, igual que `kit.py`;
   - `app/src/main/res/raw/kit_avance.json`: por pieza la imagen, los márgenes (donde cae la sombra), el cuerpo medido en la imagen y, en el canal y el churro (nine-slice horizontal), los insets; en las medallas, la placa del número.
Los nombres de la app: kit_canal_<crema|cielo>, kit_churro_<sol|lima>, kit_bolita, kit_emb_<juego>, kit_farola_<off|on>, kit_pedestal, kit_cola_<izq|der>, kit_med_<constancia|variedad|volver>[_off], kit_escudo_<liga>, kit_edif_<área> (los edificios de la isla, con `pie_dp`).
Uso:  python -I tools/isla/kit_avance_app.py [carpeta_de_renders]      (por defecto tools/isla/out/kit-avance)
`AvanceKitTest` mide que cada WebP tenga el tamaño que dice el JSON y que no sobre ni falte ninguno."""
import os, sys, json
import numpy as np
from PIL import Image

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SRC = os.path.abspath(sys.argv[1]) if len(sys.argv) > 1 else os.path.join(HERE, 'out', 'kit-avance')
RES = os.path.join(ROOT, 'app', 'src', 'main', 'res'); PX = 3
JUEGOS = ('parejas', 'secuencia', 'bodega', 'correo', 'stroop', 'piloto', 'freno', 'satelites', 'radar', 'calculo', 'engranajes', 'acoplamiento', 'aterrizaje', 'anagramas', 'meteoros', 'disparate', 'intrusa')
FAMILIAS = ('constancia', 'variedad', 'volver')
AREAS = ('memoria', 'atencion', 'razonamiento', 'lenguaje')
LIGAS = ('bronce', 'plata', 'oro', 'platino', 'esmeralda', 'diamante', 'maestro')

# nombre de la app → render de Blender
MAPA = {'canal_crema': 'canal_crema', 'canal_cielo': 'canal_cielo', 'churro_sol': 'churro_sol', 'churro_lima': 'churro_lima', 'bolita': 'bolita_lima',
        'farola_off': 'farola_apagada', 'farola_on': 'farola_encendida', 'pedestal': 'pedestal', 'cola_izq': 'cola_izquierda', 'cola_der': 'cola_derecha'}
for j in JUEGOS: MAPA['emb_' + j] = 'emblema_' + j
for f in FAMILIAS: MAPA['med_' + f] = 'medalla_%s_conseguida' % f; MAPA['med_%s_off' % f] = 'medalla_%s_bloqueada' % f
for l in LIGAS: MAPA['escudo_' + l] = 'escudo_' + l
for a in AREAS: MAPA['edif_' + a] = 'edificio_' + a
TIRAS = ('canal_crema', 'canal_cielo', 'churro_sol', 'churro_lima')           # nine-slice horizontal: la franja central de 4 dp se estira


def carga(n):
    """El PNG con la bruma del «shadow catcher» recortada (como `kit.py`): se resta un piso de 12/255 y el alfa llega a 0 en los últimos 10 px de cada borde."""
    a = np.asarray(Image.open(os.path.join(SRC, 'kit_pz_' + n + '.png')).convert('RGBA')).astype(float).copy()
    al = np.clip((a[..., 3] - 12.0) * 255.0 / (255.0 - 12.0), 0, 255); h, w = al.shape; yy, xx = np.mgrid[0:h, 0:w]
    d = np.minimum.reduce([xx, yy, w - 1 - xx, h - 1 - yy]).astype(float); al *= 0.5 - 0.5 * np.cos(np.pi * np.clip(d / 10.0, 0, 1)); a[..., 3] = al
    return Image.fromarray(a.astype(np.uint8), 'RGBA')


def cuerpo(im):
    """El cuerpo: lo opaco y con color (la sombra de contacto es negra y translúcida)."""
    a = np.asarray(im); m = (a[..., 3] >= 200) & (a[..., :3].max(axis=2) > 40)
    ys, xs = np.where(m); return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def main():
    meta = json.load(open(os.path.join(SRC, 'kit_avance.json'), encoding='utf-8'))['piezas']
    out = {}; total = 0
    for nombre, render in MAPA.items():
        im = carga(render); x0, y0, x1, y1 = cuerpo(im); w, h = im.size
        p = os.path.join(RES, 'drawable-nodpi', 'kit_%s.webp' % nombre); im.save(p, quality=92, alpha_quality=100, method=6); total += os.path.getsize(p)
        e = {'imagen_px': [w, h], 'margenes_dp': {'izq': round(x0 / PX, 2), 'arr': round(y0 / PX, 2), 'der': round((w - x1) / PX, 2), 'abajo': round((h - y1) / PX, 2)},
             'cuerpo_dp': {'ancho': round((x1 - x0) / PX, 2), 'alto': round((y1 - y0) / PX, 2)}}
        if nombre in TIRAS:
            cx = (x0 + x1) // 2; e['insets_px'] = {'izq': cx - 2 * PX, 'der': w - (cx + 2 * PX)}
        if nombre.startswith('edif_'): e['pie_dp'] = meta[render]['pie_dp']                                                       # dónde toca el suelo el edificio (dp desde la esquina de la imagen)
        if nombre.startswith('med_'): e['placa_dp'] = {'cx': w / PX / 2, 'cy': round((48 + 38) * (w / PX) / 96, 2), 'ancho': 52, 'alto': 16}      # la placa lisa del número, en dp desde la esquina de la imagen
        out[nombre] = e
    with open(os.path.join(RES, 'raw', 'kit_avance.json'), 'w', encoding='utf-8') as f:
        json.dump({'px_por_dp': PX, 'nota': 'Piezas del kit para Avance y Juegos (Tarea 125), de los renders de `tools/isla/isla.py --kit --kit-solo avance` por `tools/isla/kit_avance_app.py`. A 3 px por dp; `cuerpo_dp` y `margenes_dp` medidos en la imagen. '
                   'Nine-slice horizontal (`insets_px`) en el canal y el churro; `placa_dp` es el sitio del número de la medalla (va en Compose).', 'piezas': out}, f, ensure_ascii=False, indent=1)
    print('%d piezas, %.0f KB' % (len(out), total / 1024))


if __name__ == '__main__':
    main()
