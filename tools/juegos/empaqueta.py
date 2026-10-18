# -*- coding: utf-8 -*-
"""Lleva los renders de Blender (y las losas del kit de la app) a Unity: un ATLAS por grupo, su manifiesto y los `.meta`, y mide cuánto pesa.

Uso: python -I tools/juegos/empaqueta.py <radar|piloto|freno|calculo|disparate|satelites|rastro|stroop|punta|meteoros|parejas|bodega|correo|aterrizaje|engranajes|comun|nubi|todo> [--unity DIR] [--render DIR]
  - lee los PNG que dejó Blender en tools/juegos/out/<grupo>/ (y `medidas.json` si el script de Blender lo escribió);
  - `comun` toma además las losas del kit de la app (`app/src/main/res/drawable-nodpi/kit_losa_*.webp` + `res/raw/kit.json`: los insets del nine-slice) y la Nubi 3D (`nubi_3d.webp`) con su sombra;
  - escribe en unity/NeuroVidaCore/Assets/Resources/Plasticina/<grupo>/: `atlas.png`, `manifest.json` y sus `.meta` (GUID fijo por ruta: el mismo en cualquier PC), más los `.meta` de las carpetas.

Un atlas por grupo (un juego, o lo común): una textura, un lote de dibujo y UN solo archivo que pesa. `PlasticArt` (Unity) corta cada sprite del atlas con las medidas del manifiesto. Los bordes del nine-slice (losas) y los pivotes van en el manifiesto, no en
los `.meta`: los `.meta` solo dicen cómo importar la textura (sin compresión, sin mipmaps, sin reescalar). `PlasticinaImporter` (Editor) vuelve a imponer esos ajustes en cada importación, por si un `.meta` se rompe.
Formato del manifiesto: ver `PlasticArt.cs` (`PlasticManifest`).
"""
import hashlib, json, math, os, sys, zlib
import numpy as np
from PIL import Image, ImageFilter
sys.stdout.reconfigure(encoding='utf-8')

AQUI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, AQUI)                                   # `python -I` no pone la carpeta del script: `engranajes_volumen.py` y `propuesta_util.py` viven al lado
REPO = os.path.normpath(os.path.join(AQUI, '..', '..'))
RES_APP = os.path.join(REPO, 'app', 'src', 'main', 'res')
args = sys.argv[1:]
def opt(n, d):
    return args[args.index(n) + 1] if n in args else d
UNITY = os.path.abspath(opt('--unity', os.path.join(REPO, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina')))
RENDER = os.path.abspath(opt('--render', os.path.join(AQUI, 'out')))
GRUPOS = [a for a in args if not a.startswith('--') and a not in (UNITY, RENDER)]
PX_DP = 3                                   # píxeles por dp del kit

# ------------------------------------------------------------------ imágenes
def limpia_bruma(im):
    """La bruma del «shadow catcher»: el suelo invisible deja un velo de alfa 10-20 hasta los bordes. Se resta un piso de 12/255 y el alfa llega a 0 en los últimos 10 px de cada borde (igual que `tools/isla/kit.py`)."""
    a = np.asarray(im.convert('RGBA')).astype(float).copy()
    al = np.clip((a[..., 3] - 12.0) * 255.0 / (255.0 - 12.0), 0, 255)
    h, w = al.shape; yy, xx = np.mgrid[0:h, 0:w]; d = np.minimum.reduce([xx, yy, w - 1 - xx, h - 1 - yy]).astype(float)
    al *= 0.5 - 0.5 * np.cos(np.pi * np.clip(d / 10.0, 0, 1))
    a[..., 3] = al
    return Image.fromarray(a.astype(np.uint8), 'RGBA')

def cuerpo_bbox(im, umbral=235):
    a = np.asarray(im)[..., 3]; ys, xs = np.where(a >= umbral)
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1

def reduce_premult(im, ancho):
    """Reduce [im] a [ancho] px de ancho con Lanczos y el alfa PREMULTIPLICADO (con el alfa recto, el negro de los píxeles transparentes mancha los bordes)."""
    if ancho is None or ancho == im.width: return im
    alto = round(im.height * ancho / im.width)
    a = np.asarray(im.convert('RGBA')).astype(np.float32) / 255.0
    pm = np.concatenate([a[..., :3] * a[..., 3:4], a[..., 3:4]], axis=2)
    ch = [np.asarray(Image.fromarray(pm[..., k], mode='F').resize((ancho, alto), Image.LANCZOS)) for k in range(4)]
    out = np.stack(ch, axis=2); al = np.clip(out[..., 3], 0, 1)
    rgb = np.where(al[..., None] > 1e-4, out[..., :3] / np.maximum(al[..., None], 1e-4), 0.0)
    res = np.concatenate([np.clip(rgb, 0, 1), al[..., None]], axis=2)
    return Image.fromarray((res * 255.0 + 0.5).astype(np.uint8), 'RGBA')

def quita_ruido_alfa(im, minimo=6, hueco=None):
    """Pone en 0 el alfa por debajo de [minimo]/255 (la cola del suelo de sombra: no se ve y encarece el PNG) y, si [hueco], todo lo que cae dentro de ese radio (fracción del medio lienzo): el vidrio del radar va debajo y es oscuro."""
    a = np.asarray(im).copy(); h, w = a.shape[:2]
    m = a[..., 3] < minimo
    if hueco is not None:
        yy, xx = np.mgrid[0:h, 0:w]; r = np.hypot(xx - w / 2 + 0.5, yy - h / 2 + 0.5) / (w / 2)
        m |= r < hueco
    a[m] = 0
    return Image.fromarray(a, 'RGBA')

def carga_render(grupo, nombre, ancho=None, hueco=None):
    """El render de Blender (la «maestra», a mayor resolución) sin bruma, con el hueco limpio si se pide y reducido a [ancho] px: el tamaño final que pesa en el APK."""
    im = Image.open(os.path.join(RENDER, grupo, nombre + '.png')).convert('RGBA')
    if hueco is not None:
        im = quita_ruido_alfa(im, 0, hueco)
    im = limpia_bruma(im)
    im = reduce_premult(im, ancho)
    return quita_ruido_alfa(im, 6)

def alinea_losa(ims, margen_dp, ancho_px, alto_px, ref):
    """Corre las imágenes de una losa para que el cuerpo de [ref] caiga en su margen izquierdo y superior (como `kit.py`) y las deja en el lienzo de [ancho_px] × [alto_px]."""
    ml, mt = margen_dp; x0, y0, _, _ = cuerpo_bbox(ref)
    dx, dy = round(ml * PX_DP) - x0, round(mt * PX_DP) - y0
    out = []
    for im in ims:
        lienzo = Image.new('RGBA', (ancho_px, alto_px), (0, 0, 0, 0)); lienzo.paste(im, (dx, dy)); out.append(lienzo)
    return out

# ------------------------------------------------------------------ los grupos: qué sprites lleva cada atlas
def sprite(nombre, im, **extra):
    d = dict(nombre=nombre, im=im, ppu=100.0, pivote=(0.5, 0.5), borde=(0, 0, 0, 0), margen=(0, 0, 0, 0), baja=0.0); d.update(extra); return d

# el ancho final (px) de cada imagen: lo que se ve en el teléfono (1 unidad del lienzo de 1080 = 1 px) con un 5-10 % de sobra, para que el APK no pese de más
ANCHOS_RADAR = {'capsula': 224, 'icono': 112, 'roca': 160, 'radar_aro': 896, 'nave': 720, 'asiento_lleno': 60}

def grupo_radar():
    S = []
    tipos = ('hexagono', 'gota', 'circulo', 'cuadrado', 'triangulo', 'rombo')
    for t in tipos: S.append(sprite('capsula_' + t, carga_render('radar', 'capsula_' + t, ANCHOS_RADAR['capsula'])))
    for t in tipos: S.append(sprite('icono_' + t, carga_render('radar', 'icono_' + t, ANCHOS_RADAR['icono'])))
    S.append(sprite('roca', carga_render('radar', 'roca', ANCHOS_RADAR['roca'])))
    S.append(sprite('radar_aro', carga_render('radar', 'radar_aro', ANCHOS_RADAR['radar_aro'], hueco=0.78)))     # el vidrio (oscuro) va debajo: dentro del aro no hace falta nada
    S.append(sprite('nave', carga_render('radar', 'nave', ANCHOS_RADAR['nave'])))
    S.append(sprite('asiento_lleno', carga_render('radar', 'asiento_lleno', ANCHOS_RADAR['asiento_lleno'])))
    med = []
    p = os.path.join(RENDER, 'radar', 'medidas.json')
    if os.path.exists(p):
        for k, v in json.load(open(p, encoding='utf-8')).items(): med.append((k, float(v)))
    return S, med

ANCHOS_PILOTO = {'senal': 192, 'chica': 96, 'nave': 192}

def grupo_piloto():
    """Las 15 señales (192 px: el juego y el aviso de misión nueva; 96 px: la tarjeta de misión, que se ve a 28 dp) y la nave. La versión chica sale de la MISMA maestra."""
    S = []
    for f in ('hexagono', 'gota', 'circulo', 'cuadrado', 'triangulo'):
        for d in ('punto', 'anillo', 'franja'):
            S.append(sprite(f'senal_{f}_{d}', carga_render('piloto', f'senal_{f}_{d}', ANCHOS_PILOTO['senal'])))
            S.append(sprite(f'chica_{f}_{d}', carga_render('piloto', f'senal_{f}_{d}', ANCHOS_PILOTO['chica'])))
            S.append(sprite(f'senal_p_{f}_{d}', carga_render('piloto', f'senal_p_{f}_{d}', ANCHOS_PILOTO['senal'])))         # presionada (se ve solo cuando la persona toca la señal)
    S.append(sprite('nave', carga_render('piloto', 'nave', ANCHOS_PILOTO['nave'])))
    return S, []

ANCHOS_FRENO = {'boton': 256, 'cohete': 256, 'plataforma': 320, 'alto': 400}

def grupo_freno():
    """Freno de Emergencia: los dos botones (210 u en el juego), los tres cohetes (230 u), la plataforma (300 u) y la señal de ALTO (400-420 u). Cada uno en el mismo lienzo cuadrado que el sprite procedural al que reemplaza."""
    S = []
    for n in ('boton_apagado', 'boton_encendido', 'boton_apagado_p', 'boton_encendido_p'): S.append(sprite(n, carga_render('freno', n, ANCHOS_FRENO['boton'])))       # (los _p: el botón presionado)
    for n in ('cohete_coral', 'cohete_cielo', 'cohete_lima'): S.append(sprite(n, carga_render('freno', n, ANCHOS_FRENO['cohete'])))
    S.append(sprite('plataforma', carga_render('freno', 'plataforma', ANCHOS_FRENO['plataforma'])))
    S.append(sprite('alto', carga_render('freno', 'alto', ANCHOS_FRENO['alto'])))
    return S, []

ANCHOS_CALCULO = {'reactor': 552}

def grupo_calculo():
    """Carga exacta: el aro del reactor y su núcleo, en el MISMO lienzo cuadrado de ±92 dp (552 px a 3 px/dp = 1 px por unidad del lienzo de 1080). Del aro se limpia el hueco (lo cubre el núcleo): el suelo de sombra deja ahí un velo."""
    S = []
    S.append(sprite('reactor_aro', carga_render('calculo', 'reactor_aro', ANCHOS_CALCULO['reactor'], hueco=0.70)))
    S.append(sprite('reactor_nucleo', carga_render('calculo', 'reactor_nucleo', ANCHOS_CALCULO['reactor'])))
    return S, []

ANCHOS_DISPARATE = {'antena': 420, 'icono': 128}
MEDIDOR_U = 34.0                 # 1 unidad de Blender del medidor = 34 u del lienzo de 1080 (el ancho de la barra)
MEDIDOR_MARGEN = 0.4             # lo que sobresale la sombra (en unidades de Blender); el radio de los extremos es 0,5

def grupo_disparate():
    """¿Verdad o disparate?: la antena (420 u en el juego), los dos íconos (120 u) y las barras del medidor, UNA por estado, como nine-slice VERTICAL a 1 px por unidad del lienzo (los extremos redondos no se estiran)."""
    S = []; med = []
    S.append(sprite('antena', carga_render('disparate', 'antena', ANCHOS_DISPARATE['antena'])))
    for n in ('icono_visto', 'icono_ola'): S.append(sprite(n, carga_render('disparate', n, ANCHOS_DISPARATE['icono'])))
    ancho = round((1.0 + 2 * MEDIDOR_MARGEN) * MEDIDOR_U); tope = round((MEDIDOR_MARGEN + 0.5) * MEDIDOR_U); margen = round(MEDIDOR_MARGEN * MEDIDOR_U)
    for n in ('medidor_encendida', 'medidor_apagada'):
        S.append(sprite(n, carga_render('disparate', n, ancho), borde=(0, tope, 0, tope), margen=(margen, margen, margen, margen)))
    med.append(('medidor_margen_u', MEDIDOR_MARGEN * MEDIDOR_U))
    return S, med

ANCHOS_SATELITES = {'satelite': 192, 'planeta': 512, 'sobre': 128, 'nube': 448}

def grupo_satelites():
    """Satélites: el satélite (64 dp = 191 u en el juego; UNO solo, todos son iguales), el planeta (334 u, y ×1,75 = 585 u en la pantalla final), el sobre (110 u) y la nube de polvo (600 u). Cada uno en el mismo lienzo cuadrado que el sprite procedural al que reemplaza."""
    S = []
    for n in ('satelite', 'satelite_p', 'planeta', 'sobre', 'nube'): S.append(sprite(n, carga_render('satelites', n, ANCHOS_SATELITES[n.replace('_p', '')])))       # (satelite_p: el satélite presionado, al responder)
    return S, []

ANCHOS_RASTRO = {'lucero': 192}

def grupo_rastro():
    """Rastro de luz: los 9 luceros (192 px: el sprite mide 179 u en el juego), del color de `RastroBoard.Colors`, y los 9 PRESIONADOS (`lucero_p_i`: la esfera aplastada, un punto más oscura y con la huella de un dedo; se muestran mientras el dedo está apoyado). Cada uno sale de su maestra de 384 px."""
    S = []
    for i in range(9): S.append(sprite(f'lucero_{i}', carga_render('rastro', f'lucero_{i}', ANCHOS_RASTRO['lucero'])))
    for i in range(9): S.append(sprite(f'lucero_p_{i}', carga_render('rastro', f'lucero_p_{i}', ANCHOS_RASTRO['lucero'])))
    return S, []

ANCHOS_STROOP = {'icono': 128}
LOSAS_STROOP = {'md': ('md_boton_rojo', 'md_boton_rojo_p', 'md_boton_azul', 'md_boton_azul_p', 'md_boton_amarillo', 'md_boton_amarillo_p', 'md_boton_blanco', 'md_boton_blanco_p', 'md_cinta_tinta', 'md_cinta_palabra'),
                'gr': ('gr_tarjeta_fondo', 'gr_tarjeta_tinta', 'gr_tarjeta_palabra'), 'ch': ('ch_chip_tinta', 'ch_chip_palabra')}

def grupo_stroop():
    """Tinta o Palabra: las losas con el color EXACTO de cada tinta, de la tarjeta, de la cinta y de las orillas (mismo formato, nine-slice y márgenes que las del kit de la misma clase: ch, md o gr) y los íconos de la gota y el libro (128 px, en gris claro: el código los tiñe)."""
    kit = json.load(open(os.path.join(RES_APP, 'raw', 'kit.json'), encoding='utf-8'))
    S = []
    for cls, nombres in LOSAS_STROOP.items():
        d = kit['losas'][cls]; W, H = d['imagen_px']; m = d['margenes_dp']; ins = d['insets_px']
        borde = (ins['izq'], ins['abajo'], ins['der'], ins['arr']); margen = tuple(round(m[k] * PX_DP) for k in ('izq', 'arr', 'der', 'abajo'))
        ims = [carga_render('stroop', 'losa_' + n) for n in nombres]
        ims = alinea_losa(ims, (m['izq'], m['arr']), W, H, ims[0])
        for n, im in zip(nombres, ims): S.append(sprite('losa_' + n, im, borde=borde, margen=margen, baja=d['presionada']['texto_baja_dp'] * PX_DP))
    for n in ('gota', 'libro'): S.append(sprite(n, carga_render('stroop', n, ANCHOS_STROOP['icono'])))
    return S, []

ANCHOS_PUNTA = {'ficha': 192, 'casilla': 128, 'perla': 64}

def grupo_punta():
    """En la punta de la lengua: la ficha-letra (gris claro: el código la tiñe; el cuerpo mide 0,8 del lado), la casilla (el cuerpo mide 0,83) y la perla del lucero del cielo (gris claro; 0,94)."""
    S = []
    for n in ('ficha', 'ficha_p', 'casilla', 'perla'): S.append(sprite(n, carga_render('punta', n, ANCHOS_PUNTA[n.replace('_p', '')])))       # (ficha_p: la ficha presionada)
    return S, []

ANCHOS_METEOROS = {'roca': 300, 'estrella': 96}
BORDE_ROCA = (112, 90, 112, 90)                  # el nine-slice de la roca (izquierda, abajo, derecha, arriba, en px finales): el centro de 0,64 × 0,4 unidades (120 px por unidad) es lo que se estira
PPU_ROCA = 100.0 * 120.0 / 180.0                 # el dibujo mide 120 px por unidad de diseño y en el juego 1 unidad = 180 u: la imagen se muestra 1,5 veces más grande

def grupo_meteoros():
    """Lluvia de meteoros: la roca de plasticina (4 siluetas, nine-slice, a 120 px por unidad: 300 × 228) y sus 4 presionadas, la brasa y las grietas (capas de numpy, del mismo lienzo y tamaño) y las 2 estrellas de la constelación (96 px)."""
    S = []; med = []
    cuerpos = [carga_render('meteoros', f'roca_{i}', ANCHOS_METEOROS['roca']) for i in range(4)]
    for i, im in enumerate(cuerpos): S.append(sprite(f'roca_{i}', im, ppu=PPU_ROCA, borde=BORDE_ROCA))
    for i in range(4): S.append(sprite(f'roca_p_{i}', carga_render('meteoros', f'roca_p_{i}', ANCHOS_METEOROS['roca']), ppu=PPU_ROCA, borde=BORDE_ROCA))
    for capa in ('brasa', 'grietas'):
        for i in range(4):
            im = Image.open(os.path.join(RENDER, 'meteoros', f'{capa}_{i}.png')).convert('RGBA'); assert im.size == cuerpos[0].size, (capa, im.size, cuerpos[0].size)
            S.append(sprite(f'{capa}_{i}', im, ppu=PPU_ROCA, borde=BORDE_ROCA))
    for n in ('estrella_encendida', 'estrella_apagada'): S.append(sprite(n, carga_render('meteoros', n, ANCHOS_METEOROS['estrella'])))
    # cuánto sobresale la imagen alrededor del CUERPO (la sombra y el resplandor): el mínimo de las 4 formas, en unidades del lienzo del juego (1 px del atlas = 1,5 u)
    ml = mr = mt = mb = 99999
    for im in cuerpos:
        x0, y0, x1, y1 = cuerpo_bbox(im); ml = min(ml, x0); mr = min(mr, im.width - x1); mt = min(mt, y0); mb = min(mb, im.height - y1)
    k = 100.0 / PPU_ROCA
    # el cuerpo no está centrado en el lienzo (la cúpula sube y la sombra cae abajo): arriba sobra más que abajo. El juego usa cada lado y corre la imagen para que el CUERPO quede centrado en la roca
    med += [('roca_margen_x_u', (ml + mr) / 2 * k), ('roca_margen_arr_u', mt * k), ('roca_margen_abj_u', mb * k)]
    return S, med

ANCHOS_PAREJAS = {'objeto': 224, 'luz': 252}
TINTA_PAREJAS = (0x1A, 0x12, 0x40)
CONTORNO_U = 1.3                                 # el borde de tinta del boceto: 1,3 unidades hacia afuera de la silueta (la caja del objeto mide 56 unidades)
GEMELOS_PAREJAS = (0, 1, 3, 5)
LUZ_PX_U = 504.0 / (84.0 / 38.0 * 0.94)         # los píxeles por unidad de Blender de la luz maestra (constelaciones.py)
LUZ_R_PX = 0.94 * LUZ_PX_U                       # el radio de la esfera (38 unidades del boceto) en la maestra

def con_contorno(im, grosor_px):
    """El borde de tinta del boceto: la silueta del CUERPO (alfa ≥ 235/255: sin la sombra de contacto) engordada [grosor_px], por detrás del render (la sombra queda como está)."""
    a = np.asarray(im.convert('RGBA')).astype(np.float32); H, W = a.shape[:2]
    cuerpo = (a[..., 3] >= 235).astype(np.float32); r = int(math.ceil(grosor_px))
    pad = np.pad(cuerpo, r); dil = np.zeros_like(cuerpo)
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dx * dx + dy * dy <= grosor_px * grosor_px: dil = np.maximum(dil, pad[r + dy:r + dy + H, r + dx:r + dx + W])
    dil = np.asarray(Image.fromarray((dil * 255).astype(np.uint8), 'L').filter(ImageFilter.GaussianBlur(0.9))).astype(np.float32)
    tinta = np.zeros((H, W, 4), np.float32); tinta[..., 0], tinta[..., 1], tinta[..., 2] = TINTA_PAREJAS; tinta[..., 3] = dil
    fondo = Image.fromarray(tinta.astype(np.uint8), 'RGBA'); fondo.alpha_composite(im.convert('RGBA')); return fondo

def con_nucleo(im):
    """El brillo suave alrededor del grano del núcleo de la luz dormida (luz, no plasticina: el boceto lo pintaba como un resplandor de 22 unidades de ancho). Busca el grano (lo más claro de la esfera)."""
    a = np.asarray(im.convert('RGBA')).astype(np.float32); H, W = a.shape[:2]
    yy, xx = np.mgrid[0:H, 0:W]
    claro = (a[..., :3].mean(-1) > 175) & (a[..., 3] > 250) & (np.hypot(xx + 0.5 - W / 2.0, yy + 0.5 - H / 2.0) < 0.45 * LUZ_R_PX)        # (el borde de la esfera también brilla: se busca solo hacia el centro)
    ys, xs = np.nonzero(claro); assert len(ys) > 50, 'no se encontró el grano del núcleo'
    cy, cx = ys.mean(), xs.mean()
    gr = np.hypot(xx + 0.5 - cx, yy + 0.5 - cy) / (LUZ_R_PX * 11.0 / 38.0)        # el radio del resplandor del boceto: 11 unidades
    al = 0.85 * np.where(gr < 0.35, 1.0 - gr / 0.35 * 0.55, 0.45 * (1.0 - (gr - 0.35) / 0.65)); al = np.clip(al, 0, 1) * (gr < 1.0) * (~claro) * (a[..., 3] > 250)
    glow = np.array([170.0, 190.0, 255.0]); out = a.copy()
    out[..., :3] = a[..., :3] * (1 - al[..., None]) + glow * al[..., None]
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), 'RGBA')

def carga_parejas(nombre, ancho, contorno=False, nucleo=False):
    """Un render de Constelaciones: sin bruma, con el contorno de tinta (los objetos) o el brillo del núcleo (la luz dormida) puestos sobre la maestra, y reducido a [ancho] px."""
    im = limpia_bruma(Image.open(os.path.join(RENDER, 'parejas', nombre + '.png')).convert('RGBA'))
    if contorno: im = con_contorno(im, CONTORNO_U * im.width / 56.0)
    if nucleo: im = con_nucleo(im)
    return quita_ruido_alfa(reduce_premult(im, ancho), 6)

def grupo_parejas():
    """Constelaciones: los 12 objetos y los 4 gemelos (224 px = la caja de 56 unidades, a 4 px por unidad, con el borde de tinta del boceto) y la luz: dormida, dormida presionada y abierta (252 px = la caja de 84 unidades, a 3 px por unidad)."""
    S = []; med = []
    for k in range(12):
        for v in ((0, 1) if k in GEMELOS_PAREJAS else (0,)):
            n = f'obj_{k}_{v}'; S.append(sprite(n, carga_parejas(n, ANCHOS_PAREJAS['objeto'], contorno=True), ppu=4.0))
    for n in ('dormante', 'dormante_p'): S.append(sprite(n, carga_parejas(n, ANCHOS_PAREJAS['luz'], nucleo=True), ppu=3.0))
    S.append(sprite('abierta', carga_parejas('abierta', ANCHOS_PAREJAS['luz']), ppu=3.0))
    return S, med

PX_DP_BODEGA = 160.0 / 20.0                      # la maestra de Bodega: 8 px por dp (la final, 4)

def promedia_cuatro(nombre, grupo='bodega'):
    """Las cuatro maestras de una pieza que GIRA por código (`<nombre>_d0..3`: la luz del estudio girada 0°, 90°, 180° y 270°) promediadas con alfa premultiplicado: sin luz ni sombra direccional."""
    ims = [np.asarray(Image.open(os.path.join(RENDER, grupo, f'{nombre}_d{k}.png')).convert('RGBA')).astype(np.float32) / 255.0 for k in range(4)]
    pm = [np.concatenate([x[..., :3] * x[..., 3:4], x[..., 3:4]], axis=2) for x in ims]
    m = sum(pm) / 4.0; al = m[..., 3:4]
    rgb = np.where(al > 1e-4, m[..., :3] / np.maximum(al, 1e-4), 0.0)
    return Image.fromarray((np.clip(np.concatenate([rgb, al], axis=2), 0, 1) * 255.0 + 0.5).astype(np.uint8), 'RGBA')

def carga_bodega(nombre, ancho, bruma=False, promedio=False, extra=None):
    """Un render de Bodega: las piezas SIN suelo (flotan o van dentro de otra) no llevan bruma que limpiar; las de suelo (marco, caja, objetos) sí. [extra] (maestra → maestra) pone la luz o la sombra que no es plasticina."""
    im = promedia_cuatro(nombre) if promedio else Image.open(os.path.join(RENDER, 'bodega', nombre + '.png')).convert('RGBA')
    if bruma: im = limpia_bruma(im)
    if extra is not None: im = extra(im)
    return quita_ruido_alfa(reduce_premult(im, ancho), 6)

def oscurece_borde(im, radio_dp, color, alfa_borde):
    """El interior de una escotilla se oscurece hacia el borde (el boceto: degradé radial): una capa de sombra, no plasticina."""
    a = np.asarray(im).astype(np.float32); H, W = a.shape[:2]; yy, xx = np.mgrid[0:H, 0:W]
    t = np.clip(np.hypot(xx + 0.5 - W / 2.0, yy + 0.5 - H / 2.0) / (radio_dp * PX_DP_BODEGA), 0, 1)
    al = (t ** 2.0) * alfa_borde
    a[..., :3] = a[..., :3] * (1 - al[..., None]) + np.array(color, np.float32) * al[..., None]
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), 'RGBA')

def con_reflejo(im):
    """El reflejo del vidrio de la esclusa (boceto: un disco blanco al 18 % de 8 dp de radio a (−8, +8) dp): luz, no plasticina."""
    a = np.asarray(im).astype(np.float32); H, W = a.shape[:2]; yy, xx = np.mgrid[0:H, 0:W]
    d = np.hypot(xx + 0.5 - (W / 2.0 - 8 * PX_DP_BODEGA), yy + 0.5 - (H / 2.0 - 8 * PX_DP_BODEGA)) / (8 * PX_DP_BODEGA)
    al = 0.18 * np.clip(1.0 - np.maximum(d - 0.85, 0) / 0.15, 0, 1) * (a[..., 3] > 250)
    a[..., :3] = a[..., :3] * (1 - al[..., None]) + 255.0 * al[..., None]
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), 'RGBA')

def grupo_bodega():
    """Bodega de carga: los 12 objetos (192 px), las piezas de la escotilla (4 px por dp: marco, aro, tres interiores, la hoja de la puerta, la hoja presionada y el disco de la puerta de la tarjeta de entrada), el robot, la caja, la esclusa y los tres círculos del carro."""
    S = []; med = []
    for k in range(12): S.append(sprite(f'obj_{k}', carga_bodega(f'obj_{k}', 192, bruma=True)))
    S.append(sprite('marco', carga_bodega('marco', 276, bruma=True), ppu=4.0))
    S.append(sprite('aro', carga_bodega('aro', 276), ppu=4.0))
    realce = {0: (11, 14, 36, 0.75), 1: (11, 14, 36, 0.8), 2: None}
    for k in range(3):
        ex = (lambda im, c=realce[k]: oscurece_borde(im, 28.0, c[:3], c[3])) if realce[k] else None
        S.append(sprite(f'interior_{k}', carga_bodega(f'interior_{k}', 224, extra=ex), ppu=4.0))
    for n in ('puerta', 'puerta_p', 'puerta_disco'): S.append(sprite(n, carga_bodega(n, 228, promedio=True), ppu=4.0))
    S.append(sprite('robot', carga_bodega('robot', 236), ppu=4.0))
    S.append(sprite('visor', carga_bodega('visor', 128), ppu=4.0))
    S.append(sprite('ojo', carga_bodega('ojo', 40), ppu=4.0))
    S.append(sprite('tallo', carga_bodega('tallo', 24), ppu=4.0))
    S.append(sprite('bola', carga_bodega('bola', 40), ppu=4.0))
    S.append(sprite('caja', carga_bodega('caja', 152, bruma=True), ppu=4.0))
    S.append(sprite('esclusa', carga_bodega('esclusa', 280, extra=con_reflejo), ppu=4.0))
    for k in range(3): S.append(sprite(f'carro_{k}', carga_bodega(f'carro_{k}', 160), ppu=4.0))
    return S, med

ANCHOS_CORREO = {'carta': 384, 'planeta': 256, 'dial': 128, 'puerta': 264, 'manija': 128, 'torre': 176, 'lampara': 112, 'nave': 400, 'saco': 160, 'raya': 128, 'reloj_base': 256, 'reloj_tapa': 200}

def carga_correo(nombre, ancho, bruma=False, promedio=False):
    """Un render de Correo: las piezas SIN suelo (flotan: planetas, lámparas, nave, saco, dial, manija, raya) no llevan bruma que limpiar; las de suelo (cartas, puerta, torre, reloj) sí. [promedio]: las cuatro luces del dial, que gira por código."""
    im = promedia_cuatro(nombre, 'correo') if promedio else Image.open(os.path.join(RENDER, 'correo', nombre + '.png')).convert('RGBA')
    if bruma: im = limpia_bruma(im)
    return quita_ruido_alfa(reduce_premult(im, ancho), 6)

def grupo_correo():
    """La estación de correo: las 12 cartas (3 px por dp), los 4 planetas de los buzones, el dial (promedio de cuatro luces: gira), la puerta y la manija de la caja fuerte, la torre y las dos lámparas del faro, la nave, el saco, la marca de raya y el reloj (base, tapa y tapa presionada); 4 px por dp salvo las cartas.
    Los buzones, la caja fuerte, el faro y las tarjetas son losas del atlas «comun»; el marco punteado, la barra del día, el propulsor y los resplandores siguen procedurales."""
    S = []; med = []
    for p in range(4):
        for c in range(3): S.append(sprite(f'carta_{p}_{c}', carga_correo(f'carta_{p}_{c}', ANCHOS_CORREO['carta'], bruma=True), ppu=3.0))
    for p in range(4): S.append(sprite(f'planeta_{p}', carga_correo(f'planeta_{p}', ANCHOS_CORREO['planeta']), ppu=4.0))
    S.append(sprite('dial', carga_correo('dial', ANCHOS_CORREO['dial'], promedio=True), ppu=4.0))
    S.append(sprite('caja_puerta', carga_correo('caja_puerta', ANCHOS_CORREO['puerta'], bruma=True), ppu=4.0))
    S.append(sprite('manija', carga_correo('manija', ANCHOS_CORREO['manija']), ppu=4.0))
    S.append(sprite('torre', carga_correo('torre', ANCHOS_CORREO['torre'], bruma=True), ppu=4.0))
    for k in range(2): S.append(sprite(f'lampara_{k}', carga_correo(f'lampara_{k}', ANCHOS_CORREO['lampara']), ppu=4.0))
    S.append(sprite('nave', carga_correo('nave', ANCHOS_CORREO['nave']), ppu=4.0))
    S.append(sprite('saco', carga_correo('saco', ANCHOS_CORREO['saco']), ppu=4.0))
    S.append(sprite('marca_raya', carga_correo('marca_raya', ANCHOS_CORREO['raya']), ppu=4.0))
    S.append(sprite('reloj_base', carga_correo('reloj_base', ANCHOS_CORREO['reloj_base'], bruma=True), ppu=4.0))
    S.append(sprite('reloj_tapa', carga_correo('reloj_tapa', ANCHOS_CORREO['reloj_tapa'], bruma=True), ppu=4.0))
    S.append(sprite('reloj_tapa_p', carga_correo('reloj_tapa_p', ANCHOS_CORREO['reloj_tapa'], bruma=True), ppu=4.0))
    return S, med

ANCHOS_ATERRIZAJE = {'nave': 288, 'gallardete': 144, 'tierra': 384, 'satelite': 128, 'domo': 112}

def simetriza(im):
    """La imagen promediada con su espejo horizontal (con el alfa premultiplicado): la nave de Aterrizaje es simétrica respecto de su eje vertical y SU CENTRO EN X ES LA RESPUESTA, así que ni la luz del render ni el ruido pueden cargarla a un lado.
    Como la forma es simétrica, es lo mismo que renderizarla con la luz de la izquierda y con la de la derecha y promediar."""
    a = np.asarray(im.convert('RGBA')).astype(np.float32) / 255.0
    pm = np.concatenate([a[..., :3] * a[..., 3:4], a[..., 3:4]], axis=2); m = (pm + pm[:, ::-1]) / 2.0; al = m[..., 3:4]
    rgb = np.where(al > 1e-4, m[..., :3] / np.maximum(al, 1e-4), 0.0)
    return Image.fromarray((np.clip(np.concatenate([rgb, al], axis=2), 0, 1) * 255.0 + 0.5).astype(np.uint8), 'RGBA')

def carga_aterrizaje(nombre, ancho, bruma=False, simetrica=False):
    """Un render de Aterrizaje: las piezas SIN suelo (flotan) no llevan bruma que limpiar; [simetrica] promedia la pieza con su espejo (la nave)."""
    im = Image.open(os.path.join(RENDER, 'aterrizaje', nombre + '.png')).convert('RGBA')
    if bruma: im = limpia_bruma(im)
    if simetrica: im = simetriza(im)
    return quita_ruido_alfa(reduce_premult(im, ancho), 6)

def grupo_aterrizaje():
    """Aterrizaje Lunar: la nave (SIMÉTRICA: su centro en x es la respuesta), el gallardete, la Tierra, el satélite, las dos cúpulas del marcador (4 px por dp; la Tierra, 3) y la regla (una losa crema sin marcas, nine-slice a lo ancho, a 3 px por dp con los márgenes
    de las losas del kit). Las marcas de la regla, sus números y el asta NO están: son procedurales y exactas. La luna y las cordilleras tampoco."""
    S = []; med = []
    S.append(sprite('nave', carga_aterrizaje('nave', ANCHOS_ATERRIZAJE['nave'], simetrica=True), ppu=4.0))
    S.append(sprite('gallardete', carga_aterrizaje('gallardete', ANCHOS_ATERRIZAJE['gallardete']), ppu=4.0))
    S.append(sprite('tierra', carga_aterrizaje('tierra', ANCHOS_ATERRIZAJE['tierra']), ppu=3.0))
    S.append(sprite('satelite', carga_aterrizaje('satelite', ANCHOS_ATERRIZAJE['satelite']), ppu=4.0))
    for n in ('domo_on', 'domo_off'): S.append(sprite(n, carga_aterrizaje(n, ANCHOS_ATERRIZAJE['domo']), ppu=4.0))
    # la regla: una losa del kit de radio 7,5 dp (cuerpo de 39,7 × 20 dp) con los márgenes del kit; los bordes del nine-slice son margen + radio
    radio = 7.5; kit = json.load(open(os.path.join(RES_APP, 'raw', 'kit.json'), encoding='utf-8')); m = kit['losas']['md']['margenes_dp']
    cuerpo_w = 2 * radio + (kit['losas']['md']['cuerpo_dp']['ancho'] - 44.0); cuerpo_h = 2 * radio + (kit['losas']['md']['cuerpo_dp']['alto'] - 44.0)
    W = round((cuerpo_w + m['izq'] + m['der']) * PX_DP); H = round((cuerpo_h + m['arr'] + m['abajo']) * PX_DP)
    im = carga_render('aterrizaje', 'regla')
    im = alinea_losa([im], (m['izq'], m['arr']), W, H, im)[0]
    borde = (round((m['izq'] + radio) * PX_DP), round((m['abajo'] + radio) * PX_DP), round((m['der'] + radio) * PX_DP), round((m['arr'] + radio) * PX_DP))        # izquierda, abajo, derecha, arriba (el orden de Unity)
    margen = (round(m['izq'] * PX_DP), round(m['arr'] * PX_DP), round(m['der'] * PX_DP), round(m['abajo'] * PX_DP))                                                       # izquierda, arriba, derecha, abajo
    S.append(sprite('regla', im, borde=borde, margen=margen))
    med += [('regla_cuerpo_ancho_dp', float(cuerpo_w)), ('regla_cuerpo_alto_dp', float(cuerpo_h))]
    return S, med

PX_ENG = 3.5                                                         # píxeles por dp de los engranajes y las piezas de la sala (el procedural: 3,5)
TIP_ENG = {0: 33.0, 1: 23.0}
PAL_ENG = {0: ('#CFEFFF', '#5FA3E0'), 1: ('#F1EDFF', '#A49BE0'), 2: ('#FFF1C2', '#E2B24A')}

def hexv(h): return np.array([int(h.lstrip('#')[i:i + 2], 16) for i in (0, 2, 4)], np.float32)

def promedia_cuatro_eng(nombre):
    """Las cuatro maestras de una pieza que GIRA por código (`<nombre>_d0..3`: la luz del estudio girada 0°, 90°, 180° y 270°, vista exactamente desde arriba y sin suelo) promediadas con alfa premultiplicado: ni la luz ni la sombra giran con la pieza."""
    ims = [np.asarray(Image.open(os.path.join(RENDER, 'engranajes', f'{nombre}_d{k}.png')).convert('RGBA')).astype(np.float32) / 255.0 for k in range(4)]
    pm = [np.concatenate([x[..., :3] * x[..., 3:4], x[..., 3:4]], axis=2) for x in ims]
    m = sum(pm) / 4.0; al = m[..., 3:4]
    rgb = np.where(al > 1e-4, m[..., :3] / np.maximum(al, 1e-4), 0.0)
    return Image.fromarray((np.clip(np.concatenate([rgb, al], axis=2), 0, 1) * 255.0 + 0.5).astype(np.uint8), 'RGBA')

def degrade_engranaje(im, size, pal, ppd):
    """El degradé radial del boceto (de un foco chico arriba a la izquierda, en el color claro de la paleta, hasta la punta, en el oscuro), como COLOR y no como luz: gira con el engranaje (es lo que muestra el sentido del giro, junto con los rayos). Se aplica como una razón por canal sobre
    el color medio con que se renderizó (el relieve y su luz se conservan)."""
    a = np.asarray(im).astype(np.float32); H, W = a.shape[:2]; tip = TIP_ENG[size]
    yy, xx = np.mgrid[0:H, 0:W]; x = (xx + 0.5 - W / 2.0) / ppd; y = (H / 2.0 - yy - 0.5) / ppd                       # dp, y hacia ARRIBA, como el sprite de siempre
    fx, fy = -tip * 0.35, tip * 0.35
    t = np.clip(np.hypot(x - fx, y - fy) / (tip + 0.35 * tip * 1.4142), 0.0, 1.0)
    c0, c1 = hexv(PAL_ENG[pal][0]), hexv(PAL_ENG[pal][1]); medio = c0 + (c1 - c0) * 0.45
    objetivo = c0[None, None, :] + (c1 - c0)[None, None, :] * t[..., None]
    razon = np.clip(objetivo / medio[None, None, :], 0.72, 1.30)
    a[..., :3] = np.clip(a[..., :3] * razon, 0, 255)
    return Image.fromarray(a.astype(np.uint8), 'RGBA')

def carga_eng(nombre, ancho, bruma=False, promedio=False):
    """Un render de Engranajes: lo que gira se promedia en cuatro luces; lo fijo, no. Ninguna pieza lleva suelo (no hay sombra de contacto que limpiar), salvo [bruma]."""
    im = promedia_cuatro_eng(nombre) if promedio else Image.open(os.path.join(RENDER, 'engranajes', nombre + '.png')).convert('RGBA')
    if bruma: im = limpia_bruma(im)
    return quita_ruido_alfa(reduce_premult(im, ancho), 6)

def grupo_engranajes():
    """Engranajes «Taller de reparación»: los 6 engranajes (grande y chico en las tres paletas) y el estado presionado del motor, el radar y la turbina (que giran: cuatro luces promediadas y vistos desde arriba), la polea (y su presionada), el portón, el marco, la plataforma y la caja de la
    carga, la barra dentada, el casco, las flechas del motor (grande y chica, a favor y en contra del reloj) y del cartel, y los triángulos (blancos: se tildan). 3,5 px por dp salvo el casco y las flechas grandes (3), la flechita (4) y los triángulos (2).
    Desde la Tarea 115 todo pasa por `engranajes_volumen.py` (el VOLUMEN aprobado el 16-oct: color por rol con un mapa de color, bisel isótropo en lo que gira, casco de cilindro con remaches, flechas de tubo) y suma el domo de las luces y el aro de las luces. Desde la Tarea 116 los carteles y el rótulo MOTOR son losas chicas del kit (`losa_ch_noche/acierto/error`; el rótulo dorado es la `losa_ch_sol` de `comun`) y «Arrancar» y las llaves tienen ícono de plasticina (`ico_play`, `llave_libre`, `llave_usada`)."""
    import engranajes_volumen as vol
    S = []; med = []
    for size, box in ((0, 72.0), (1, 52.0)):
        ancho = round(box * PX_ENG)
        for pal in (0, 1, 2):
            im = degrade_engranaje(carga_eng(f'gear_{size}_{pal}', ancho, promedio=True), size, pal, PX_ENG)
            S.append(sprite(f'gear_{size}_{pal}', vol.gear_volumen(im, size, pal, TIP_ENG[size]), ppu=PX_ENG))
        im = degrade_engranaje(carga_eng(f'gear_{size}_0_p', ancho, promedio=True), size, 0, PX_ENG)
        S.append(sprite(f'gear_{size}_0_p', vol.gear_volumen(im, size, 0, TIP_ENG[size], presionado=True), ppu=PX_ENG))
    S.append(sprite('radar', vol.radar_volumen(carga_eng('radar', round(48 * PX_ENG), promedio=True)), ppu=PX_ENG))
    S.append(sprite('turbina', vol.turbina_volumen(carga_eng('turbina', round(40 * PX_ENG), promedio=True)), ppu=PX_ENG))
    for n, w in (('polea', 40), ('polea_p', 40)): S.append(sprite(n, vol.polea_volumen(carga_eng(n, round(w * PX_ENG))), ppu=PX_ENG))
    for n, w, rol, bisel, base in (('porton', 44, 'lila', 2.2, 0.10), ('marco_carga', 48, 'lila', 1.6, 0.0), ('plataforma', 40, 'oro', 2.2, 0.10), ('caja_carga', 32, 'azul', 2.2, 0.06), ('barra', 20, 'lila', 2.2, -0.10)):       # Tarea 116: con la receta de color de los engranajes (`fija_rol`)
        S.append(sprite(n, vol.fija_rol(carga_eng(n, round(w * PX_ENG)), rol, bisel, base, semilla=len(n)), ppu=PX_ENG))
    S.append(sprite('casco', vol.casco_volumen(carga_eng('casco', 348)), ppu=3.0))
    for n, box in (('grande', 2 * (33 + 8 + 14)), ('chico', 2 * (23 + 8 + 14))):
        for suf in ('cw', 'ccw'): S.append(sprite(f'flecha_{n}_{suf}', vol.blanca_tubo(carga_eng(f'flecha_{n}_{suf}', round(box * 3)), 1.6 * 3.0 / PX_ENG), ppu=3.0))
    for suf in ('cw', 'ccw'): S.append(sprite(f'miniflecha_{suf}', vol.blanca_tubo(carga_eng(f'miniflecha_{suf}', 88), 1.1 * 4.0 / PX_ENG), ppu=4.0))
    for n in ('tri_arriba', 'tri_abajo', 'tri_play'): S.append(sprite(n, vol.blanca_tubo(carga_eng(n, 112), 1.6 * 2.0 / PX_ENG), ppu=2.0))
    S.append(sprite('domo', vol.domo_sprite(48), ppu=100.0 * 3.0 / 3.0)); S.append(sprite('moneda', vol.moneda_sprite(96), ppu=100.0))
    # Tarea 116: los carteles y el rótulo MOTOR son losas CHICAS (ch) del kit en el color de Rescate, y «Arrancar» y las llaves llevan íconos de plasticina (`engranajes_botones.py`)
    kit = json.load(open(os.path.join(RES_APP, 'raw', 'kit.json'), encoding='utf-8'))
    d = kit['losas']['ch']; W, H = d['imagen_px']; m = d['margenes_dp']; ins = d['insets_px']
    borde = (ins['izq'], ins['abajo'], ins['der'], ins['arr']); margen = tuple(round(m[k] * PX_DP) for k in ('izq', 'arr', 'der', 'abajo'))
    nombres = ('losa_ch_noche', 'losa_ch_acierto', 'losa_ch_error')
    ims = alinea_losa([carga_render('engranajes_botones', n) for n in nombres], (m['izq'], m['arr']), W, H, Image.open(os.path.join(RES_APP, 'drawable-nodpi', 'kit_losa_ch_crema.webp')).convert('RGBA'))
    for n, im in zip(nombres, ims): S.append(sprite(n, im, borde=borde, margen=margen, baja=d['presionada']['texto_baja_dp'] * PX_DP))
    for n in ('ico_play', 'llave_libre', 'llave_usada'): S.append(sprite(n, carga_render('engranajes_botones', n, 128)))
    return S, med

def grupo_acoplamiento():
    """Acoplamiento v3 (Tarea 118): lo que sale de Blender (`acoplamiento_v3.py`): los íconos de «Encaja» y «Espejo» (churro de tinta, 128 px), los 4 chevrones del puerto (verde agua, lima, celeste, apagado; 96 px de ancho) y las 3 luces de la fila de avance (apagada, oro, lima; 96 px).
    La pieza, el hueco y la escultura NO van en el atlas: se hornean por módulo en `DockingArt` (la lámpara se calcula en la orientación que se ve); las losas del panel «Puerto», de «Encaja», de «Espejo» y de los avisos son las del kit (`comun`)."""
    S = []
    for n in ('ico_encaja', 'ico_espejo'): S.append(sprite(n, carga_render('acoplamiento_v3', n, 128)))
    for n in ('chev_verde_agua', 'chev_lima', 'chev_celeste', 'chev_apagado'): S.append(sprite(n, carga_render('acoplamiento_v3', n, 96)))
    for n in ('luz_apagada', 'luz_oro', 'luz_lima'): S.append(sprite(n, carga_render('acoplamiento_v3', n, 96)))
    return S, []

def grupo_comun():
    S = []; med = []
    kit = json.load(open(os.path.join(RES_APP, 'raw', 'kit.json'), encoding='utf-8'))
    # 1) las losas del kit de la app (WebP → PNG en el atlas), con su nine-slice: bordes en el orden de Unity (izquierda, abajo, derecha, arriba)
    for cls, d in kit['losas'].items():
        if cls == 'nota': continue
        ins = d['insets_px']; m = d['margenes_dp']
        borde = (ins['izq'], ins['abajo'], ins['der'], ins['arr']); margen = tuple(round(m[k] * PX_DP) for k in ('izq', 'arr', 'der', 'abajo'))
        for color in d['colores']:
            for suf in ('', '_p'):
                f = os.path.join(RES_APP, 'drawable-nodpi', f'kit_losa_{cls}_{color}{suf}.webp')
                S.append(sprite(f'losa_{cls}_{color}{suf}', Image.open(f).convert('RGBA'), borde=borde, margen=margen, baja=d['presionada']['texto_baja_dp'] * PX_DP))
    # 2) las losas «noche» (md): se alinean al lienzo y a los márgenes de las md de la app
    md = kit['losas']['md']; W, H = md['imagen_px']; m = md['margenes_dp']; ins = md['insets_px']
    borde = (ins['izq'], ins['abajo'], ins['der'], ins['arr']); margen = tuple(round(m[k] * PX_DP) for k in ('izq', 'arr', 'der', 'abajo'))
    nombres = ['losa_md_noche', 'losa_md_noche_p', 'losa_md_acierto', 'losa_md_error', 'losa_md_apagada', 'losa_md_apagada_p', 'losa_md_crema_control']
    ims = [carga_render('comun', n) for n in nombres]
    ims = alinea_losa(ims, (m['izq'], m['arr']), W, H, ims[0])
    for n, im in zip(nombres, ims):
        if n == 'losa_md_crema_control': continue
        S.append(sprite(n, im, borde=borde, margen=margen, baja=md['presionada']['texto_baja_dp'] * PX_DP))
    # control: la losa crema re-renderizada con ESTE pipeline contra la del kit de la app (que el pipeline dé el mismo kit)
    ref = np.asarray(Image.open(os.path.join(RES_APP, 'drawable-nodpi', 'kit_losa_md_crema.webp')).convert('RGBA')).astype(float)
    nueva = np.asarray(ims[-1]).astype(float)
    dif = np.abs(ref[..., :3] - nueva[..., :3]) * (ref[..., 3:4] > 200) / 255.0
    med.append(('control_losa_crema_dif_media', float(dif.sum() / max(1.0, (ref[..., 3] > 200).sum() * 3))))
    # 3) las marcas y el anillo
    for n, ancho in (('marca_ok', None), ('marca_no', None), ('anillo_cuenta', 576)): S.append(sprite(n, carga_render('comun', n, ancho)))
    for k in ('px_por_dp',): med.append((k, float(PX_DP)))
    return S, med

EMBLEMAS = ('parejas', 'secuencia', 'bodega', 'correo', 'stroop', 'piloto', 'freno', 'satelites', 'radar', 'calculo', 'engranajes', 'acoplamiento', 'aterrizaje', 'anagramas', 'meteoros', 'disparate', 'intrusa')

def grupo_emblemas():
    """Los 17 emblemas de juego (la ficha del pastel de su área con el objeto del juego; `tools/isla/kit_avance.py`, la misma pieza de la app) para la cortina del final (`FinishCurtain`, Tarea 126). Se renderizan a 6 px por dp
    (`isla.py --kit --kit-solo avance --kit-px 6 --pieza emblema_`, en `tools/isla/out/kit-avance-emb6/`): 432 px de lienzo, para que la ficha se vea nítida a ~500 unidades del lienzo de 1080."""
    base = os.path.join(REPO, 'tools', 'isla', 'out', 'kit-avance-emb6')
    S = []
    for j in EMBLEMAS:
        im = Image.open(os.path.join(base, f'kit_pz_emblema_{j}.png')).convert('RGBA')
        S.append(sprite('emb_' + j, quita_ruido_alfa(limpia_bruma(im), 6)))
    return S, [('px_por_dp', 6.0)]

def grupo_nubi():
    """La Nubi 3D de la app (`nubi_3d.webp`, 720×476) con la sombra elíptica de `NubiClay` (NubiHome.kt) horneada: 0,8 del ancho, 0,17 de alto sobre eso, degradado radial 0,55 → 0,45 (a 0,62) → 0."""
    nubi = Image.open(os.path.join(RES_APP, 'drawable-nodpi', 'nubi_3d.webp')).convert('RGBA'); w, h = nubi.size
    sombra_h = round(w * 0.1); H = h + sombra_h
    yy, xx = np.mgrid[0:H, 0:w].astype(float)
    cx, cy = w / 2.0, h + w * 0.1 * 0.42; rx = w * 0.4; ry = rx * 0.2
    d = np.sqrt(((xx + 0.5 - cx) / rx) ** 2 + ((yy + 0.5 - cy) / ry) ** 2)
    alfa = np.where(d < 0.62, 0.55 + (0.45 - 0.55) * (d / 0.62), np.where(d < 1.0, 0.45 * (1.0 - (d - 0.62) / 0.38), 0.0))
    sombra = np.zeros((H, w, 4)); sombra[..., 0] = 0x0B; sombra[..., 1] = 0x09; sombra[..., 2] = 0x30; sombra[..., 3] = alfa * 255.0
    lienzo = Image.fromarray(sombra.astype(np.uint8), 'RGBA'); lienzo.alpha_composite(nubi, (0, 0))
    return [sprite('nubi_3d', lienzo)], [('ancho_cuerpo_px', float(w)), ('alto_cuerpo_px', float(h))]

GRUPO_FN = {'radar': grupo_radar, 'piloto': grupo_piloto, 'freno': grupo_freno, 'calculo': grupo_calculo, 'disparate': grupo_disparate, 'satelites': grupo_satelites, 'rastro': grupo_rastro, 'stroop': grupo_stroop, 'punta': grupo_punta, 'meteoros': grupo_meteoros, 'parejas': grupo_parejas, 'bodega': grupo_bodega, 'correo': grupo_correo, 'aterrizaje': grupo_aterrizaje, 'engranajes': grupo_engranajes, 'acoplamiento': grupo_acoplamiento, 'comun': grupo_comun, 'emblemas': grupo_emblemas, 'nubi': grupo_nubi}

# ------------------------------------------------------------------ el atlas
def empaca(sprites, ancho_max=2048, pad=2):
    """Empaque por estantes: ordena por alto y llena filas; prueba anchos de 512 a [ancho_max] y se queda con el de menos área (con un alto de a lo más 4096 px)."""
    mejor = None
    for W in range(512, ancho_max + 1, 128):
        x = y = fila = 0; pos = {}; ok = True
        for s in sorted(sprites, key=lambda s: -s['im'].height):
            w, h = s['im'].size
            if w + 2 * pad > W: ok = False; break
            if x + w + 2 * pad > W: x = 0; y += fila; fila = 0
            pos[s['nombre']] = (x + pad, y + pad); x += w + 2 * pad; fila = max(fila, h + 2 * pad)
        if not ok: continue
        H = y + fila
        if H > 4096: continue                                  # Unity achica a 4096 de alto (tamaño máximo de textura del importador) y los recortes del manifiesto dejan de caber: se prefiere un atlas más ancho
        if mejor is None or W * H < mejor[0] * mejor[1]: mejor = (W, H, pos)
    if mejor is None: raise SystemExit('un sprite no cabe en el ancho máximo del atlas (o el atlas no cabe en 4096 de alto)')
    return mejor

def guid(ruta_rel):
    return hashlib.md5(('plasticina:' + ruta_rel.replace('\\', '/')).encode('utf-8')).hexdigest()

META_CARPETA = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""
META_TEXTO = """fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""
META_TEXTURA = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 4096
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 4096
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Android
    maxTextureSize: 4096
    resizeAlgorithm: 0
    textureFormat: 4
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 1
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""

def escribe_meta(ruta, plantilla):
    """Escribe el `.meta` solo si no existe: Unity lo reescribe completo al importar y esa es la versión que va al repo (con el mismo GUID, que sale de la ruta)."""
    if os.path.exists(ruta + '.meta'): return
    rel =os.path.relpath(ruta, os.path.join(UNITY, '..')).replace('\\', '/')
    with open(ruta + '.meta', 'w', encoding='utf-8', newline='\n') as f: f.write(plantilla.format(guid=guid(rel)))

def construye(nombre):
    sprites, medidas = GRUPO_FN[nombre]()
    W, H, pos = empaca(sprites)
    atlas = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    entradas = []
    for s in sprites:
        x, y = pos[s['nombre']]; atlas.paste(s['im'], (x, y))
        w, h = s['im'].size
        entradas.append({'n': s['nombre'], 'x': x, 'y': H - y - h, 'w': w, 'h': h, 'ppu': s['ppu'], 'px': s['pivote'][0], 'py': s['pivote'][1],
                         'borde': list(s['borde']), 'margen': list(s['margen']), 'baja': s['baja']})
    carpeta = os.path.join(UNITY, nombre); os.makedirs(carpeta, exist_ok=True)
    ruta_png = os.path.join(carpeta, 'atlas.png'); atlas.save(ruta_png, optimize=True)
    crudo = np.asarray(atlas).tobytes()
    pesos = {'png_bytes': os.path.getsize(ruta_png), 'crudo_rgba32': len(crudo), 'deflate_rgba32': len(zlib.compress(crudo, 6)), 'astc6x6': math.ceil(W / 6) * math.ceil(H / 6) * 16}
    manifiesto = {'version': 1, 'grupo': nombre, 'ancho': W, 'alto': H, 'sprites': entradas, 'medidas': [{'n': k, 'v': v} for k, v in medidas],
                  'peso': {k: int(v) for k, v in pesos.items()}}
    ruta_json = os.path.join(carpeta, 'manifest.json')
    with open(ruta_json, 'w', encoding='utf-8', newline='\n') as f: json.dump(manifiesto, f, ensure_ascii=False, indent=1)
    # los .meta: de la carpeta Plasticina, de la del grupo, de la textura y del manifiesto
    escribe_meta(UNITY, META_CARPETA); escribe_meta(carpeta, META_CARPETA); escribe_meta(ruta_png, META_TEXTURA); escribe_meta(ruta_json, META_TEXTO)
    print(f'[{nombre}] atlas {W}×{H} px, {len(entradas)} sprites')
    print(f'    PNG en disco {pesos["png_bytes"] / 1024:.0f} KB · crudo RGBA32 {pesos["crudo_rgba32"] / 1048576:.2f} MB · sin compresión dentro del APK (deflate) ≈ {pesos["deflate_rgba32"] / 1024:.0f} KB · con ASTC 6×6 ≈ {pesos["astc6x6"] / 1024:.0f} KB')
    for k, v in medidas: print(f'    medida {k} = {v:.4g}')
    return pesos

def main():
    nombres = ['radar', 'piloto', 'freno', 'calculo', 'disparate', 'satelites', 'rastro', 'stroop', 'punta', 'meteoros', 'parejas', 'bodega', 'correo', 'aterrizaje', 'engranajes', 'acoplamiento', 'comun', 'nubi', 'emblemas'] if (not GRUPOS or 'todo' in GRUPOS) else GRUPOS
    total = {'png_bytes': 0, 'deflate_rgba32': 0}
    for n in nombres:
        p = construye(n)
        for k in total: total[k] += p[k]
    if len(nombres) > 1: print(f'TOTAL: PNG {total["png_bytes"] / 1024:.0f} KB; en el APK sin compresión de textura ≈ {total["deflate_rgba32"] / 1024:.0f} KB')

main()
