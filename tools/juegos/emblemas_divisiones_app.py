# -*- coding: utf-8 -*-
"""Lleva a la app los cinco emblemas de las divisiones del Contrarreloj (Tarea 212): toma las maestras de Blender (`emblema_a1..a5.png`, 440 px, lienzo cuadrado de 2 unidades) que dejó
`emblemas_divisiones.py`, les quita la bruma del «shadow catcher», las reduce con Lanczos y el alfa premultiplicado y las guarda como WebP en `app/src/main/res/drawable-nodpi/division_*.webp`
(`ui/components/DivisionEmblem.kt` las dibuja).
Los cinco de la liga de 10 divisiones (c1..c5: nubes, Marte, asteroides, Júpiter y galaxia, Tarea 228) viven por ahora en `tools/juegos/emblemas_liga/`, FUERA de `res/drawable-nodpi/`: nadie los usa todavía y `NubiAssetsTest` no deja arte sin uso en el APK
(Tarea 232b). La Tarea 229 los devuelve a `res/drawable-nodpi/` (moviéndolos o volviendo a correr este script, que los escribe ahí) cuando Ricardo apruebe los emblemas; si pide cambios, se rehacen en esa carpeta.

Tamaño: 3 px por dp, como el kit (`kit_i_reloj_clay`). El objeto ocupa el 86 % de una ventanilla de 52 dp en la cifra (45 dp: 135 px) y de 28 dp en la escalera (24 dp: 72 px, de la misma imagen).

Uso:  python -I tools/juegos/emblemas_divisiones_app.py [--render DIR] [--px 135]
"""
import json, os, sys
import numpy as np
from PIL import Image

sys.stdout.reconfigure(encoding='utf-8')
AQUI = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
args = sys.argv[1:]


def opt(n, d):
    return args[args.index(n) + 1] if n in args else d


RENDER = os.path.abspath(opt('--render', os.path.join(AQUI, 'out', 'divisiones')))
PX = int(opt('--px', '135'))
DESTINO = os.path.join(REPO, 'app', 'src', 'main', 'res', 'drawable-nodpi')
# a1..a5 = las cinco divisiones de la Tarea 212 (Comparison.Division: PLATFORM, TAKEOFF, ORBIT, MOON, STARS); c1..c5 = las cinco nuevas de la liga de 10 divisiones (Tarea 228)
NOMBRES = {'a1': 'division_plataforma', 'a2': 'division_despegando', 'a3': 'division_orbita', 'a4': 'division_luna', 'a5': 'division_estrellas',
           # los cinco lugares de más de la liga de 10 divisiones (Tarea 228; `docs/liga-de-la-semana.md` §1.1): nubes, Marte, asteroides, Júpiter y otra galaxia
           'c1': 'division_nubes', 'c2': 'division_marte', 'c3': 'division_asteroides', 'c4': 'division_jupiter', 'c5': 'division_galaxia'}


def limpia_bruma(im):
    """La bruma del «shadow catcher»: el suelo invisible deja un velo de alfa 10-20 hasta los bordes. Se resta un piso de 12/255 y el alfa llega a 0 en los últimos 10 px de cada borde (igual que `empaqueta.py`)."""
    a = np.asarray(im.convert('RGBA')).astype(float).copy()
    al = np.clip((a[..., 3] - 12.0) * 255.0 / (255.0 - 12.0), 0, 255)
    h, w = al.shape; yy, xx = np.mgrid[0:h, 0:w]; d = np.minimum.reduce([xx, yy, w - 1 - xx, h - 1 - yy]).astype(float)
    al *= 0.5 - 0.5 * np.cos(np.pi * np.clip(d / 10.0, 0, 1))
    a[..., 3] = al
    return Image.fromarray(a.astype(np.uint8), 'RGBA')


def reduce_premult(im, ancho):
    """Reduce [im] a [ancho] px de ancho con Lanczos y el alfa PREMULTIPLICADO (con el alfa recto, el negro de los píxeles transparentes mancha los bordes)."""
    alto = round(im.height * ancho / im.width)
    a = np.asarray(im.convert('RGBA')).astype(np.float32) / 255.0
    pm = np.concatenate([a[..., :3] * a[..., 3:4], a[..., 3:4]], axis=2)
    ch = [np.asarray(Image.fromarray(pm[..., k], mode='F').resize((ancho, alto), Image.LANCZOS)) for k in range(4)]
    out = np.stack(ch, axis=2); al = np.clip(out[..., 3], 0, 1)
    rgb = np.where(al[..., None] > 1e-4, out[..., :3] / np.maximum(al[..., None], 1e-4), 0.0)
    res = np.concatenate([np.clip(rgb, 0, 1), al[..., None]], axis=2)
    return Image.fromarray((res * 255.0 + 0.5).astype(np.uint8), 'RGBA')


def quita_ruido_alfa(im, minimo=6):
    """Pone en 0 el alfa por debajo de [minimo]/255: la cola de la sombra no se ve y encarece el archivo."""
    a = np.asarray(im).copy(); a[a[..., 3] < minimo] = 0
    return Image.fromarray(a, 'RGBA')


def main():
    os.makedirs(DESTINO, exist_ok=True)
    total = 0
    informe = {}
    for clave, nombre in NOMBRES.items():
        origen = os.path.join(RENDER, 'emblema_%s.png' % clave)
        if not os.path.exists(origen):
            sys.exit('falta %s (corre primero emblemas_divisiones.py con Blender)' % origen)
        im = quita_ruido_alfa(reduce_premult(limpia_bruma(Image.open(origen).convert('RGBA')), PX))
        destino = os.path.join(DESTINO, nombre + '.webp')
        im.save(destino, quality=92, alpha_quality=100, method=6)
        bytes_ = os.path.getsize(destino); total += bytes_
        informe[nombre] = {'px': im.width, 'bytes': bytes_}
        print('%-22s %dx%d  %6d bytes' % (nombre, im.width, im.height, bytes_))
    print('total %d bytes' % total)
    with open(os.path.join(RENDER, 'division_app.json'), 'w', encoding='utf-8') as f:
        json.dump(informe, f, indent=1, ensure_ascii=False)


main()
