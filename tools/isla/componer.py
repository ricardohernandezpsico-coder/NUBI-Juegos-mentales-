# -*- coding: utf-8 -*-
"""Prepara los recursos de la isla para la app a partir de una carpeta de render (capas PNG + isla.json):
   - un recorte común para base y edificios (caja de la isla completa + margen) y un recorte ajustado para Nubi;
   - WebP a 2× (ancho del recorte) para `app/src/main/res/drawable-nodpi/` (en una carpeta de densidad Android los reescala al decodificar y se ven borrosos; ver `ui/components/Island.kt`);
   - isla.json con coordenadas normalizadas (0-1) respecto del recorte común, más la caja de Nubi dentro de él;
   - (Tarea 134) las diez piezas de la isla por medallas, si están (`capa-deco-<id>.png` de `isla.py --deco`): `isla_deco_<id>.webp` con el MISMO recorte común, la bruma del «shadow catcher» recortada, el halo del farolito (vo3) horneado,
     y `decoraciones` en isla.json (ancla = donde toca la isla, capa = suelo | aire).
Uso: python -I componer.py <carpeta_render> [<carpeta_salida>]   (salida por defecto: <carpeta_render>/app; `isla.json` va a `app/src/main/res/raw/`)
"""
import sys, os, json, math, itertools
import numpy as np
from PIL import Image
sys.stdout.reconfigure(encoding='utf-8')

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

# --- Tarea 134: las piezas de la isla por medallas. Orden de dibujo en la app: las de suelo después de isla_base y antes de isla_edificios; las de aire después de isla_edificios.
DECO_IDS = ('c1', 'c2', 'c3', 'c4', 'v1', 'v2', 'v3', 'vo1', 'vo2', 'vo3')
BRUMA = 12.0     # el suelo invisible («shadow catcher») deja un velo de alfa 10-20 en todo el rectángulo renderizado: se resta ese piso (como kit.py) para que no se vea un cuadro gris
HALO_R = 0.05    # radio del halo del farolito (vo3), en fracción del ancho del render: ~29 dp en la isla de 390 dp
raw = json.load(open(os.path.join(src, 'isla.json'), encoding='utf-8'))

SOMBRA = (62, 42, 92)   # la sombra de contacto del «shadow catcher» sale negra y densa (alfa ~0,8 bajo un arbusto, 0,17 de la luz de la losa); las sombras de la base son violetas y blandas (~0,8 de la luz): se tiñe y se baja a K_SOMBRA
K_SOMBRA = 0.6
RAMPA = 40       # px del recorte: el alfa baja a 0 en los últimos RAMPA px del rectángulo que se renderizó (`borde` de isla.json): la sombra blanda de las luces grandes se corta ahí y dejaría una costura

def limpia(im, borde=None):
    a = np.asarray(im).astype(float).copy(); a[..., 3] = np.clip((a[..., 3] - BRUMA) * 255.0 / (255.0 - BRUMA), 0, 255)
    sh = (a[..., 3] < 252) & (a[..., :3].max(axis=2) < 48)               # píxeles de pura sombra (casi negros y translúcidos); el borde de la pieza, que es de color, no entra
    a[..., 3] = np.where(sh, a[..., 3] * K_SOMBRA, a[..., 3]); a[..., :3] = np.where(sh[..., None], np.array(SOMBRA, float), a[..., :3])
    if borde:
        h, w = a.shape[:2]; yy, xx = np.mgrid[0:h, 0:w]; (bx0, by0), (bx1, by1) = borde
        d = np.minimum.reduce([xx - bx0, bx1 - xx, yy - by0, by1 - yy]).astype(float)
        a[..., 3] *= 0.5 - 0.5 * np.cos(np.pi * np.clip(d / RAMPA, 0, 1))
    a[..., :3] = np.where(a[..., 3:4] > 0, a[..., :3], 0)          # sin color suelto donde no hay nada: el WebP pesa menos
    return Image.fromarray(a.astype(np.uint8), 'RGBA')

def hornea_halo(im, centro, radio, color=(255, 200, 106), pico=0.7):
    """Degradado radial detrás de la pieza (como `halo_antena` y las farolas encendidas): sol suave que cae a 0 en `radio` px."""
    a = np.asarray(im).astype(float) / 255.0; h, w = a.shape[:2]; yy, xx = np.mgrid[0:h, 0:w]
    d = np.clip(np.hypot(xx - centro[0], yy - centro[1]) / radio, 0, 1); H = pico * (1 - d * d * (3 - 2 * d)) ** 1.6
    A = a[..., 3]; out_a = A + H * (1 - A)
    rgb = (a[..., :3] * A[..., None] + (np.array(color) / 255.0)[None, None, :] * (H * (1 - A))[..., None]) / np.maximum(out_a[..., None], 1e-6)
    return Image.fromarray((np.dstack([np.clip(rgb, 0, 1), np.clip(out_a, 0, 1)]) * 255).astype(np.uint8), 'RGBA')

deco_kb = {}
for did in DECO_IDS:
    f = os.path.join(src, 'capa-deco-' + did + '.png')
    if not os.path.exists(f): continue
    bd = raw.get('deco', {}).get(did, {}).get('borde'); bd = [[bd[0][0] - x0, bd[0][1] - y0], [bd[1][0] - x0, bd[1][1] - y0]] if bd else None
    im = limpia(Image.open(f).convert('RGBA').crop(box), bd)
    if did == 'vo3' and raw.get('deco', {}).get('vo3', {}).get('halo'):
        hx_, hy_ = raw['deco']['vo3']['halo']; im = hornea_halo(im, (hx_ - x0, hy_ - y0), HALO_R * raw['imagen']['ancho'])
    save(im, 'isla_deco_' + did)
    if report[-1][3] > 30:                    # una capa casi vacía pesa 8-25 KB; si alguna pasa de 30, se baja a calidad 80 solo esa
        p = os.path.join(dst, 'isla_deco_' + did + '.webp'); im.save(p, quality=80, method=6); report[-1] = (report[-1][0], report[-1][1], report[-1][2], os.path.getsize(p) // 1024)
    deco_kb[did] = report[-1][3]

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
if raw.get('deco'):   # Tarea 134: dónde toca la isla cada pieza (la app la escala desde ahí al aparecer) y en qué capa se dibuja
    out['decoraciones'] = {i: {'ancla': out['deco'][i]['ancla'], 'capa': out['deco'][i]['capa']} for i in DECO_IDS if i in out.get('deco', {}) and i in deco_kb}
    if 'vo3' in out['decoraciones'] and 'halo' in out['deco']['vo3']: out['decoraciones']['vo3']['halo'] = {'centro': out['deco']['vo3']['halo'], 'radio': round(HALO_R * data['imagen']['ancho'] / W, 4), 'nota': 'horneado en isla_deco_vo3 (sol, alfa 0,7 al centro)'}
out.pop('deco', None)
lp = data.get('lugares', {}).get('antena', {}).get('lampara')
if lp:
    out['halos_farolas'] = {'centros': [norm(f) for f in data.get('lamparas', [])], 'radio': round(0.03 * data['imagen']['ancho'] / W, 4), 'opacidad': 0.45, 'color': '#FFBE6E', 'nota': 'un halo chico por farola, misma capa que el de la antena'}
    out['halo_antena'] = {'centro': norm(lp), 'radio': round(0.075 * data['imagen']['ancho'] / W, 4), 'opacidad': 0.6, 'color': '#FFC86A', 'nota': 'degradado radial sol sobre la capa de edificios; de noche puede subir a 0,75'}
# Tarea 88: una farola por lugar (la asignación de menor distancia total entre el pie de cada edificio y el tope de cada farola) y cómo se ve una farola ENCENDIDA (parada hecha hoy).
if lp:
    pies = {k: out['lugares'][k]['pie'] for k in ('observatorio', 'antena', 'biblioteca', 'hangar')}
    lamps = out['lamparas']
    best = min(itertools.permutations(range(len(lamps)), len(pies)), key=lambda perm: sum(math.dist(pies[k], lamps[i]) for k, i in zip(pies, perm)))
    out['lampara_de'] = dict(zip(pies, best))
    out['farolas_encendidas'] = {'disco_radio': 0.0115, 'disco_color': '#FFC93C', 'halo_radio': round(2 * out['halos_farolas']['radio'], 4), 'halo_opacidad': 0.8, 'halo_color': '#FFC86A', 'pie_dy': 0.057,
                                 'resplandor_losa': {'radio': 0.05, 'opacidad': 0.35, 'color': '#FFC86A'},
                                 'nota': 'farola de una parada HECHA hoy: disco sol en el tope, halo de 2x el de las apagadas (alfa 0,8) y un leve resplandor en la losa a su pie (`pie_dy` = alto del poste, normalizado al alto del recorte); las apagadas quedan con `halos_farolas`'}
out['mirada'] = {'nota': 'al tocar un lugar: fundido de 180 ms a nubi_mira_<lugar>, desplazamiento del 4 % del ancho de la isla hacia el lugar y ladeo de 6° hacia él durante 2 s, luego vuelve a hola; la sombra se mueve con Nubi; con animaciones reducidas solo cambia la imagen'}
nw = nb[2] - nb[0]; sc = data['nubi']['sombra']
out['nubi']['sombra_elipse'] = {'centro': norm((sc[0] + nw*0.04, sc[1])), 'ancho': round(nw*1.4 / W, 4), 'alto': round(nw*0.5 / H, 4), 'opacidad': 0.5, 'desenfoque': round(nw*0.09 / W, 4),
                                'nota': 'elipse de tinta bajo Nubi, en su misma capa; al subir Nubi: 0,85 del tamaño y 0,3 de opacidad; al bajar: 1,0 y 0,46'}
json.dump(out, open(os.path.join(dst, 'isla.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

for r in report: print(f'{r[0]}: {r[1]}x{r[2]} px -> {r[3]} KB')
print(f'total: {sum(r[3] for r in report)} KB; recorte común {W}x{H}; isla.json listo')
if deco_kb: print(f'piezas de la isla: {len(deco_kb)} capas, {sum(deco_kb.values())} KB' + ('  (¡pasa de 200 KB!)' if sum(deco_kb.values()) > 200 else '  (≤ 200 KB)') + ' -> ' + ', '.join(f'{k} {v}' for k, v in deco_kb.items()))
