# -*- coding: utf-8 -*-
"""Prepara los recursos de la isla para la app a partir de una carpeta de render (capas PNG + isla.json):
   - un recorte común para base y edificios (caja de la isla completa + margen) y un recorte ajustado para Nubi;
   - WebP a 2× (ancho del recorte) para `app/src/main/res/drawable-nodpi/` (en una carpeta de densidad Android los reescala al decodificar y se ven borrosos; ver `ui/components/Island.kt`);
   - isla.json con coordenadas normalizadas (0-1) respecto del recorte común, más la caja de Nubi dentro de él.
Uso: python -I componer.py <carpeta_render> [<carpeta_salida>]   (salida por defecto: <carpeta_render>/app; `isla.json` va a `app/src/main/res/raw/`)
"""
import sys, os, json
from PIL import Image

src = sys.argv[1]; dst = sys.argv[2] if len(sys.argv) > 2 else os.path.join(src, 'app')
os.makedirs(dst, exist_ok=True)
PAD = 16

def bbox(im):
    return im.getchannel('A').point(lambda v: 255 if v > 8 else 0).getbbox()

full = Image.open(os.path.join(src, 'isla-completa.png')).convert('RGBA')
x0, y0, x1, y1 = bbox(full)
x0 = max(0, x0 - PAD); y0 = max(0, y0 - PAD); x1 = min(full.width, x1 + PAD); y1 = min(full.height, y1 + PAD)
box = (x0, y0, x1, y1); W, H = x1 - x0, y1 - y0

layers = {'isla_base': 'capa-isla', 'isla_edificios': 'capa-edificios'}
report = []
def save(im, name):
    p = os.path.join(dst, name + '.webp'); im.save(p, quality=88, method=6)
    report.append((name, im.width, im.height, os.path.getsize(p) // 1024))

for name, f in layers.items():
    im = Image.open(os.path.join(src, f + '.png')).convert('RGBA').crop(box); save(im, name)

nubi_full = Image.open(os.path.join(src, 'capa-nubi.png')).convert('RGBA')
nb = bbox(nubi_full); nb = (max(0, nb[0] - 8), max(0, nb[1] - 8), min(nubi_full.width, nb[2] + 8), min(nubi_full.height, nb[3] + 8))
pose = 'hola'
save(nubi_full.crop(nb), 'nubi_' + pose)
# miradas: capa-nubi-<lugar>.png → nubi_mira_<lugar>.webp, cada una con su propia caja
miradas = {}
for lugar in ('observatorio', 'antena', 'hangar', 'biblioteca'):
    pm = os.path.join(src, f'capa-nubi-{lugar}.png')
    if not os.path.exists(pm): continue
    im = Image.open(pm).convert('RGBA'); b = bbox(im); b = (max(0, b[0]-8), max(0, b[1]-8), min(im.width, b[2]+8), min(im.height, b[3]+8))
    save(im.crop(b), f'nubi_mira_{lugar}'); miradas[lugar] = b

data = json.load(open(os.path.join(src, 'isla.json'), encoding='utf-8'))
def norm(pt): return [round((pt[0] - x0) / W, 4), round((pt[1] - y0) / H, 4)]
def walk(o):
    if isinstance(o, list) and len(o) == 2 and all(isinstance(v, (int, float)) for v in o): return norm(o)
    if isinstance(o, list): return [walk(v) for v in o]
    if isinstance(o, dict): return {k: walk(v) for k, v in o.items()}
    return o
out = walk({k: v for k, v in data.items() if k != 'imagen'})
out['imagen'] = {'recorte_px_2x': [W, H], 'relacion_alto_ancho': round(H / W, 4), 'origen': 'coordenadas 0-1 respecto del recorte común (base y edificios)'}
out['capa_nubi'] = {pose: {'caja': [norm((nb[0], nb[1])), norm((nb[2], nb[3]))], 'px_2x': [nb[2] - nb[0], nb[3] - nb[1]]}}
for lugar, b in miradas.items():
    out['capa_nubi']['mira_' + lugar] = {'caja': [norm((b[0], b[1])), norm((b[2], b[3]))], 'px_2x': [b[2] - b[0], b[3] - b[1]]}
lp = data.get('lugares', {}).get('antena', {}).get('lampara')
if lp:
    out['halos_farolas'] = {'centros': [norm(f) for f in data.get('lamparas', [])], 'radio': round(0.03 * data['imagen']['ancho'] / W, 4), 'opacidad': 0.45, 'color': '#FFBE6E', 'nota': 'un halo chico por farola, misma capa que el de la antena'}
    out['halo_antena'] = {'centro': norm(lp), 'radio': round(0.075 * data['imagen']['ancho'] / W, 4), 'opacidad': 0.6, 'color': '#FFC86A', 'nota': 'degradado radial sol sobre la capa de edificios; de noche puede subir a 0,75'}
out['mirada'] = {'nota': 'al tocar un lugar: fundido de 180 ms a nubi_mira_<lugar>, desplazamiento del 4 % del ancho de la isla hacia el lugar y ladeo de 6° hacia él durante 2 s, luego vuelve a hola; la sombra se mueve con Nubi; con animaciones reducidas solo cambia la imagen'}
nw = nb[2] - nb[0]; sc = data['nubi']['sombra']
out['nubi']['sombra_elipse'] = {'centro': norm((sc[0] + nw*0.04, sc[1])), 'ancho': round(nw*1.4 / W, 4), 'alto': round(nw*0.5 / H, 4), 'opacidad': 0.5, 'desenfoque': round(nw*0.09 / W, 4),
                                'nota': 'elipse de tinta bajo Nubi, en su misma capa; al subir Nubi: 0,85 del tamaño y 0,3 de opacidad; al bajar: 1,0 y 0,46'}
json.dump(out, open(os.path.join(dst, 'isla.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

for r in report: print(f'{r[0]}: {r[1]}x{r[2]} px -> {r[3]} KB')
print(f'total: {sum(r[3] for r in report)} KB; recorte común {W}x{H}; isla.json listo')
