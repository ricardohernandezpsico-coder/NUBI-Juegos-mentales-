# -*- coding: utf-8 -*-
"""Lleva los renders de Blender (y las losas del kit de la app) a Unity: un ATLAS por grupo, su manifiesto y los `.meta`, y mide cuánto pesa.

Uso: python -I tools/juegos/empaqueta.py <radar|comun|nubi|todo> [--unity DIR] [--render DIR]
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
    nombres = ['losa_md_noche', 'losa_md_noche_p', 'losa_md_acierto', 'losa_md_error', 'losa_md_apagada', 'losa_md_crema_control']
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

GRUPO_FN = {'radar': grupo_radar, 'comun': grupo_comun, 'nubi': grupo_nubi}

# ------------------------------------------------------------------ el atlas
def empaca(sprites, ancho_max=2048, pad=2):
    """Empaque por estantes: ordena por alto y llena filas; prueba anchos de 512 a [ancho_max] y se queda con el de menos área."""
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
        if mejor is None or W * H < mejor[0] * mejor[1]: mejor = (W, H, pos)
    if mejor is None: raise SystemExit('un sprite no cabe en el ancho máximo del atlas')
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
    nombres = ['radar', 'comun', 'nubi'] if (not GRUPOS or 'todo' in GRUPOS) else GRUPOS
    total = {'png_bytes': 0, 'deflate_rgba32': 0}
    for n in nombres:
        p = construye(n)
        for k in total: total[k] += p[k]
    if len(nombres) > 1: print(f'TOTAL: PNG {total["png_bytes"] / 1024:.0f} KB; en el APK sin compresión de textura ≈ {total["deflate_rgba32"] / 1024:.0f} KB')

main()
