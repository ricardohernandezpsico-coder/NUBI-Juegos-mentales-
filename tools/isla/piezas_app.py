# -*- coding: utf-8 -*-
"""Lleva las piezas de la isla por medallas a la app (Tarea 136, A1.3 de `auditoria-fable/spec-adopcion-A1-A7.md`).

Qué hace, en este orden:
  1. copia las diez capas `isla_deco_<id>.webp` (las que aprobó Ricardo en `docs/previews/isla-piezas/`, mismo recorte que `isla_base` e `isla_edificios`) a `app/src/main/res/drawable-nodpi/`;
  2. suma la sección `decoraciones` (ancla y capa de cada pieza) a `app/src/main/res/raw/isla.json` SIN tocar nada más del archivo (la base y los edificios de la app no se re-renderizan);
  3. arma las diez imágenes chicas `kit_pz_deco_<id>.webp` (264 × 168 px = 88 × 56 dp a 3×) que usa la celebración de la medalla («Y en tu isla aparece: …»), recortadas de los primeros planos de Blender
     (`tools/isla/out/<carpeta>/lamina-deco-<id>.png`, los mismos de la lámina): la pieza nueva en su contexto (las flores con los arbustos, los frutos con el árbol, la nave con el muelle), con su sombra de contacto teñida.
Uso:  python -I tools/isla/piezas_app.py [carpeta_de_renders]      (por defecto tools/isla/out/t134; solo el paso 3 la necesita)
Es un paso de ARTE, a mano: ninguna verificación lo corre; `NubiAssetsTest` y `IslaPiezasTest` vigilan lo que deja.
"""
import os, sys, json, shutil
import numpy as np
from PIL import Image

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SRC = os.path.abspath(sys.argv[1]) if len(sys.argv) > 1 else os.path.join(HERE, 'out', 't134')
DOCS = os.path.join(ROOT, 'docs', 'previews', 'isla-piezas')
RES = os.path.join(ROOT, 'app', 'src', 'main', 'res')
NODPI = os.path.join(RES, 'drawable-nodpi')
IDS = ('c1', 'c2', 'c3', 'c4', 'v1', 'v2', 'v3', 'vo1', 'vo2', 'vo3')
TW, TH, PAD = 264, 168, 8                        # la imagen chica: 88 × 56 dp a 3×


def limpia(im):
    """Igual que `componer.py` y `tools/previews/isla_piezas.py`: sin la bruma del «shadow catcher» y con la sombra de contacto teñida y suave."""
    a = np.asarray(im.convert('RGBA')).astype(float).copy(); a[..., 3] = np.clip((a[..., 3] - 12.0) * 255.0 / 243.0, 0, 255)
    sh = (a[..., 3] < 252) & (a[..., :3].max(axis=2) < 48); a[..., 3] = np.where(sh, a[..., 3] * 0.6, a[..., 3]); a[..., :3] = np.where(sh[..., None], np.array((62, 42, 92), float), a[..., :3])
    return Image.fromarray(a.astype(np.uint8), 'RGBA')


# 1. las capas
for i in IDS:
    shutil.copyfile(os.path.join(DOCS, f'isla_deco_{i}.webp'), os.path.join(NODPI, f'isla_deco_{i}.webp'))
print('capas copiadas:', len(IDS), '·', sum(os.path.getsize(os.path.join(NODPI, f'isla_deco_{i}.webp')) for i in IDS) // 1024, 'KB')

# 2. isla.json: solo se suma `decoraciones`
p = os.path.join(RES, 'raw', 'isla.json')
data = json.load(open(p, encoding='utf-8'))
deco = json.load(open(os.path.join(DOCS, 'isla-deco.json'), encoding='utf-8'))['decoraciones']
assert set(deco) == set(IDS)
data['decoraciones'] = deco
open(p, 'w', encoding='utf-8', newline='\n').write(json.dumps(data, indent=1, ensure_ascii=False))
print('isla.json: decoraciones =', len(deco))

# 3. las imágenes chicas (sin la sombra de contacto)
for i in IDS:
    im = limpia(Image.open(os.path.join(SRC, f'lamina-deco-{i}.png')))
    arr = np.asarray(im).copy(); arr[(arr[..., :3] == np.array((62, 42, 92), np.uint8)).all(axis=2), 3] = 0; im = Image.fromarray(arr, 'RGBA')      # la ficha va sobre el velo oscuro de la celebración: sin la sombra de contacto (cortada se vería como un recuadro violeta)
    a = np.asarray(im.getchannel('A')) >= 200; ys, xs = np.where(a)                      # el recorte va por lo SÓLIDO de la pieza (la sombra de contacto, larga y tenue, queda cortada donde sobra: la pieza se ve más grande)
    m = 6; im = im.crop((max(0, xs.min() - m), max(0, ys.min() - m), min(im.width, xs.max() + 1 + m), min(im.height, ys.max() + 1 + m)))
    k = min((TW - 2 * PAD) / im.width, (TH - 2 * PAD) / im.height)
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
    c = Image.new('RGBA', (TW, TH), (0, 0, 0, 0)); c.alpha_composite(im, ((TW - im.width) // 2, (TH - im.height) // 2))
    f = os.path.join(NODPI, f'kit_pz_deco_{i}.webp'); c.save(f, 'WEBP', quality=88, alpha_quality=100, method=6)
    print(f'kit_pz_deco_{i}: {im.width}x{im.height} dentro de {TW}x{TH} · {os.path.getsize(f) / 1024:.1f} KB')
