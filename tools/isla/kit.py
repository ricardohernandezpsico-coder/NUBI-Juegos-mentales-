# -*- coding: utf-8 -*-
"""El kit de plasticina (Tareas 89 y 90): toma los PNG que renderiza `isla.py --kit` y deja lo que entra a la app:
   - `kit_losa_<ch|md|gr>_<color>[_p].webp`: losas para NINE-SLICE (esquinas fijas, bordes estirados en un eje, centro estirado) en tres radios (ch = 14 dp: chips y casillas; md = 22: botones y tarjetas; gr = 30: láminas y paneles), normal y presionada, todas con el mismo lienzo y el cuerpo en el mismo sitio;
   - `kit_cerrar[_p].webp` (disco crema con la X de dos churros tinta), `kit_disco_<crema|cielo>[_p].webp` (discos lisos de 48 dp: perfil, volver, opciones; el ícono o la inicial va encima);
   - `kit_i_<volver|visto|izq|der|compartir|candado|engranaje|globo|altavoz|vibracion|reloj|sin_reloj|escudo|info|campana|persona|arriba|abajo>.webp` y `kit_jugar.webp`: íconos de churro tinta (sin estados: al presionar el disco bajan 2 dp);
   - `kit_av_<cohete|farol|isla|engranaje|planeta|lucero|gota>.webp`: los avatares (Tarea 91), lienzo de 48 × 48 dp con su sombra de contacto. YA NO ENTRAN A LA APP (Tarea 93: los avatares se quitaron): solo se procesan con `--con-avatares`, por si algún día vuelven;
   - `kit.json`: la densidad del render, el lienzo, los márgenes (donde cae la sombra), el cuerpo y los insets del nine-slice de cada pieza.
   Mide que el CENTRO que se estira sea parejo (si no, el nine-slice deformaría las huellas): imprime la desviación a lo ancho y a lo alto de cada losa.
Uso: python -I kit.py <carpeta_render> [<carpeta_salida>] [--con-avatares]     (la salida por defecto es <carpeta_render>/app; `kit.json` va a `app/src/main/res/raw/` y los WebP a `res/drawable-nodpi/`)"""
import sys, os, json
import numpy as np
sys.stdout.reconfigure(encoding='utf-8')
from PIL import Image

CON_AVATARES = '--con-avatares' in sys.argv
args = [a for a in sys.argv[1:] if not a.startswith('--')]
src = args[0]
dst = args[1] if len(args) > 1 else os.path.join(src, 'app')
os.makedirs(dst, exist_ok=True)
PX = 3                                   # px por dp del render (KIT_PX en isla.py)
MARGEN = (8, 6, 12, 14)                  # dp: margen (izq, arr, der, abajo) de las losas, igual que KIT_MARGEN de isla.py
CENTRO = (24, 8)                         # dp: el centro que se estira (ancho, alto): KIT_CW y KIT_CH
# clase → (radio dp, prefijo del archivo, colores): igual que KIT_LOSAS de isla.py
LOSAS = {
    'ch': (14, 'kit_losa_ch_', ('crema', 'sol', 'lima')),
    'md': (22, 'kit_losa_md_', ('sol', 'crema', 'lima', 'cielo', 'coral', 'lila')),
    'gr': (30, 'kit_losa_gr_', ('crema',)),
}
COLORES = {'sol': '#FFCB45', 'crema': '#F5EBDB', 'lima': '#9BE564', 'cielo': '#4CC9F0', 'coral': '#FF6B4A', 'lila': '#B8A4FF'}
DISCO = {'margen': (6, 4, 10, 12), 'cuerpo': (48, 48)}
DISCOS = ('kit_cerrar', 'kit_disco_crema', 'kit_disco_cielo')
ICONOS = ('volver', 'visto', 'izq', 'der', 'compartir', 'candado', 'engranaje',
          'globo', 'altavoz', 'vibracion', 'reloj', 'sin_reloj', 'escudo', 'info', 'campana', 'persona', 'arriba', 'abajo')      # los últimos once son de la Tarea 91 (reemplazan los de Material de los paneles)
AVATARES = ('cohete', 'farol', 'isla', 'engranaje', 'planeta', 'lucero', 'gota')                                        # Tarea 91 (quitados de la app en la 93): objetos de plasticina de 48 dp para los avatares de Opciones
ICONO = {'margen': (4, 3, 6, 7), 'cuerpo': (24, 24)}        # el lienzo de cada ícono (dp); el cuerpo real se mide en la imagen
JUGAR = {'margen': (4, 3, 6, 7), 'cuerpo': (20, 22)}


def load(n):
    """El PNG con la bruma del «shadow catcher» recortada: el suelo invisible deja un velo de alfa 10-20 hasta los bordes y sobre crema se vería como un rectángulo gris. Se resta un piso de 12/255 y se hace llegar a 0 en los últimos 10 px de cada borde."""
    im = Image.open(os.path.join(src, n + '.png')).convert('RGBA'); a = np.asarray(im).astype(float).copy()
    al = np.clip((a[..., 3] - 12.0) * 255.0 / (255.0 - 12.0), 0, 255)
    h, w = al.shape; yy, xx = np.mgrid[0:h, 0:w]; d = np.minimum.reduce([xx, yy, w - 1 - xx, h - 1 - yy]).astype(float)
    al *= 0.5 - 0.5 * np.cos(np.pi * np.clip(d / 10.0, 0, 1))        # rampa suave hasta 0 en el borde
    a[..., 3] = al
    return Image.fromarray(a.astype(np.uint8), 'RGBA')


def body_bbox(im, ink=False):
    """El cuerpo: lo opaco y con color (la sombra de contacto es negra y translúcida). Para íconos de tinta (casi negros) solo se mira lo muy opaco."""
    a = np.asarray(im)
    m = (a[..., 3] >= 235) if ink else ((a[..., 3] >= 200) & (a[..., :3].max(axis=2) > 70))
    ys, xs = np.where(m)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


def normalize(ims, spec):
    """Corre todas las imágenes de una pieza para que el cuerpo de la PRIMERA caiga exacto en su margen izquierdo y superior; las demás se corren lo mismo (la huella en el suelo no cambia entre estados)."""
    ml, mt, mr, mb = spec['margen']; bw, bh = spec['cuerpo']
    W = (ml + bw + mr) * PX; H = (mt + bh + mb) * PX
    x0, y0, x1, y1 = body_bbox(ims[0])
    dx, dy = ml * PX - x0, mt * PX - y0
    out = []
    for im in ims:
        canvas = Image.new('RGBA', (W, H), (0, 0, 0, 0)); canvas.paste(im, (dx, dy))        # paste sin máscara copia los cuatro canales tal cual (también con corrimiento negativo)
        out.append(canvas)
    return out, (x1 - x0, y1 - y0)


def measured(im, ink=False):
    """Los márgenes REALES (dp) del cuerpo dentro del lienzo y su tamaño, medidos en la imagen: los de diseño solo sirven para alinear; el JSON dice lo que se midió."""
    w, h = im.size; x0, y0, x1, y1 = body_bbox(im, ink)
    return {'izq': round(float(x0) / PX, 2), 'arr': round(float(y0) / PX, 2), 'der': round(float(w - x1) / PX, 2), 'abajo': round(float(h - y1) / PX, 2)}, {'ancho': round(float(x1 - x0) / PX, 2), 'alto': round(float(y1 - y0) / PX, 2)}


def baja(ims):
    """Cuánto baja (dp) la cara de la pieza presionada respecto de la normal."""
    return round((body_bbox(ims[1])[1] - body_bbox(ims[0])[1]) / PX, 2)


def save(im, name):
    p = os.path.join(dst, name + '.webp'); im.save(p, quality=92, alpha_quality=100, method=6); return os.path.getsize(p)


total = 0
kit = {'px_por_dp': PX, 'nota': 'Kit de plasticina (Tareas 89 y 90), renderizado en Blender con `tools/isla/isla.py --kit`; `tools/isla/kit.py` lo normaliza. Los WebP viven en drawable-nodpi (a 3 px por dp, sin que Android los reescale).', 'colores': COLORES}
reporte = []

# ---- las losas (nine-slice), una clase por radio
kit['losas'] = {}
for clase, (R, pref, colores) in LOSAS.items():
    spec = {'margen': MARGEN, 'cuerpo': (2 * R + CENTRO[0], 2 * R + CENTRO[1])}
    first = None
    for color in colores:
        ims, size = normalize([load(pref + color), load(pref + color + '_p')], spec)
        for im, suf in zip(ims, ('', '_p')): total += save(im, pref + color + suf)
        a = np.asarray(ims[0]).astype(float); W, H = ims[0].size
        if first is None:
            marg, cuerpo = measured(ims[0])
            insets = {'izq': round((marg['izq'] + R) * PX), 'arr': round((marg['arr'] + R) * PX), 'der': round((R + marg['der']) * PX), 'abajo': round((R + marg['abajo']) * PX)}
            first = (W, H, marg, cuerpo, insets, baja(ims))
        _, _, _, _, ins, _ = first
        band = a[ins['arr']:H - ins['abajo'], ins['izq']:W - ins['der'], :3]          # el centro: lo que se estira
        reporte.append((clase, color, size, band.std(axis=1).mean(), band.std(axis=0).mean()))
    W, H, marg, cuerpo, insets, b = first
    kit['losas'][clase] = {
        'imagen_px': [W, H], 'margenes_dp': marg, 'cuerpo_dp': cuerpo, 'radio_dp': R,
        'insets_px': insets, 'centro_px': [W - insets['izq'] - insets['der'], H - insets['arr'] - insets['abajo']],
        'presionada': {'texto_baja_dp': 2.0, 'borde_baja_dp': b},
        'colores': list(colores)
    }
kit['losas']['nota'] = 'la losa presionada es más baja (la mitad de alto), más oscura y de sombra más chica; el texto y el contenido bajan `texto_baja_dp` al apoyar el dedo'

# ---- los discos (el cuerpo de 48 dp con sus márgenes; el ícono o la inicial va encima, en Compose)
kit['discos'] = {}
for nombre in DISCOS:
    ims, size = normalize([load(nombre), load(nombre + '_p')], DISCO)
    for im, suf in zip(ims, ('', '_p')): total += save(im, nombre + suf)
    marg, cuerpo = measured(ims[0])
    kit['discos'][nombre.replace('kit_', '')] = {'imagen_px': list(ims[0].size), 'margenes_dp': marg, 'cuerpo_dp': cuerpo, 'presionada': {'texto_baja_dp': 2.0, 'borde_baja_dp': baja(ims)}}

# ---- los íconos de churro tinta (el cuerpo real se mide con lo muy opaco; se dibujan centrados en su caja)
kit['piezas'] = {}
for nombre in ('jugar',) + ICONOS:
    arch = 'kit_jugar' if nombre == 'jugar' else 'kit_i_' + nombre
    if nombre == 'jugar':
        (im,), _ = normalize([load(arch)], JUGAR)
    else:
        im = load(arch)
    total += save(im, arch)
    marg, cuerpo = measured(im, ink=True)
    kit['piezas'][nombre] = {'imagen_px': list(im.size), 'margenes_dp': marg, 'cuerpo_dp': cuerpo}

# ---- los avatares (Tarea 91; quitados de la app en la Tarea 93): solo con --con-avatares. Se dibujan enteros en una caja de 48 × 48 dp (sin márgenes: la sombra ya cae dentro del lienzo)
kit['avatares'] = {}
for nombre in (AVATARES if CON_AVATARES else ()):
    im = load('kit_av_' + nombre)
    total += save(im, 'kit_av_' + nombre)
    kit['avatares'][nombre] = {'imagen_px': list(im.size), 'margenes_dp': {'izq': 0.0, 'arr': 0.0, 'der': 0.0, 'abajo': 0.0}, 'cuerpo_dp': {'ancho': round(im.size[0] / PX, 2), 'alto': round(im.size[1] / PX, 2)}}

if not kit['avatares']: del kit['avatares']
json.dump(kit, open(os.path.join(dst, 'kit.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
for clase, d in kit['losas'].items():
    if clase == 'nota': continue
    print('losa %s: imagen %dx%d px; centro que se estira %dx%d px; insets %s' % (clase, d['imagen_px'][0], d['imagen_px'][1], d['centro_px'][0], d['centro_px'][1], d['insets_px']))
for clase, color, sz, rs, cs in reporte:
    print('  %s/%-6s cuerpo %dx%d px (%.1fx%.1f dp); desviación del centro: a lo ancho %.2f, a lo alto %.2f (de 255)' % (clase, color, sz[0], sz[1], sz[0] / PX, sz[1] / PX, rs, cs))
for n, d in kit['discos'].items(): print('disco %s: cuerpo %s, baja %s dp' % (n, d['cuerpo_dp'], d['presionada']['borde_baja_dp']))
for n, d in kit['piezas'].items(): print('pieza %s: cuerpo %s, margenes %s' % (n, d['cuerpo_dp'], d['margenes_dp']))
for n, d in kit.get('avatares', {}).items(): print('avatar %s: %s px' % (n, d['imagen_px']))
print('peso total de los WebP: %.1f KB (%d archivos)' % (total / 1024, len(os.listdir(dst)) - 1))
