# -*- coding: utf-8 -*-
"""Módulo común de Blender para pasar los juegos de Unity a plasticina (Tarea 94).

NO es un script que se corra solo: lo importan los scripts de cada juego (`radar.py`, `comun.py`...), que se lanzan con
    blender -b -P tools/juegos/<juego>.py -- [opciones]
y hacen `from plasticina import *` (el script pone su carpeta en `sys.path`).

UNA sola plasticina: el material `clay()`, la luz del estudio (`studio()`), el compositor y los ajustes de render NO se copian de `tools/isla/isla.py`:
se TOMAN de ese archivo (se lee su árbol de sintaxis y se ejecutan solo esas definiciones), así que si la isla o el kit cambian de luz o de material, los juegos
cambian con ellos y todo el arte de la app sale del mismo material y con la misma luz. Lo único propio de los juegos está acá:
la cámara ortogonal inclinada (la del kit, con la compensación de 1/cos para que las formas planas salgan tal como se dibujaron), el suelo que solo recibe
sombra, un constructor de piezas a partir de un contorno (`pieza`) y el render de una imagen con fondo transparente.

Convenciones:
- Cada pieza se modela en SU unidad de diseño (la que usa el código de Unity: por ejemplo, las unidades de `RadarSprites`), 1 unidad de Blender = 1 unidad de diseño, x a la derecha, y hacia arriba en pantalla,
  z hacia el espectador. La vista es casi cenital (`TILT` grados de inclinación, 30 como el kit): se ve un poco el canto de adelante.
- `raiz()` es el padre estirado en Y por 1/cos(TILT): lo que cuelga de él se ve del tamaño pedido en la imagen.
- La luz viene de arriba a la izquierda: las sombras caen abajo a la derecha (como en toda la app).
"""
import ast, bpy, bmesh, contextlib, math, os, random, sys, time
from mathutils import Vector

args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
def arg(n, d): return args[args.index(n) + 1] if n in args else d

ENGINE = 'CYCLES'
SAMPLES = int(arg('--samples', '112'))
WIDTH = 1080
TILT = float(arg('--tilt', '30'))               # grados de inclinación de la cámara respecto de la vista desde arriba
EXPOSICION = float(arg('--exp', '-0.42'))       # la del kit: la plasticina clara se quemaba con el sol desde arriba
ISLA = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'isla', 'isla.py'))
OUT = os.path.abspath(arg('--out', os.path.join(os.path.dirname(os.path.abspath(__file__)), 'out')))
os.makedirs(OUT, exist_ok=True)
random.seed(5)

scene = bpy.context.scene
for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)
for n in ('isla', 'edificios', 'nubi', 'luces', 'iconos'):
    c = bpy.data.collections.new(n); scene.collection.children.link(c)
C = {c.name: c for c in scene.collection.children}

# ------------------------------------------------------------------ lo que se toma de la isla (por su árbol de sintaxis)
_DEFS = ('hx', 'lin', 'clay', 'link', 'studio', 'compositor', 'setup_render', 'smooth', 'bevel', 'organic', 'mat', 'place', 'box', 'sphere', 'cyl', 'torus', 'churro', 'cushion',
         'mat_kit', 'mat_tinta', 'oscurece', 'losa_kit', 'cam_kit', 'cam_pieza', 'suelo_kit')
_ASSIGNS = ('_m', 'HAND', 'KIT_DPU', 'KIT_PX', 'KIT_TILT', 'KIT_SOLO', 'KIT_CW', 'KIT_CH', 'KIT_MARGEN', 'KIT_H', 'KIT_H_P', 'KIT_TINTA', 'KIT_SOL', 'KIT_CREMA', 'KIT_LIMA', 'KIT_CIELO', 'KIT_CORAL', 'KIT_LILA', 'KIT_LOSAS')

def _cargar_de_isla():
    ns = dict(bpy=bpy, bmesh=bmesh, math=math, os=os, sys=sys, time=time, random=random, Vector=Vector, scene=scene, C=C, args=args, arg=arg,
              ENGINE=ENGINE, SAMPLES=SAMPLES, WIDTH=WIDTH)
    tree = ast.parse(open(ISLA, encoding='utf-8').read(), ISLA)
    cuerpo = []
    for node in tree.body:
        if isinstance(node, ast.FunctionDef) and node.name in _DEFS: cuerpo.append(node)
        elif isinstance(node, ast.Assign) and any(isinstance(t, ast.Name) and t.id in _ASSIGNS for t in node.targets): cuerpo.append(node)
    faltan = [n for n in _DEFS if not any(isinstance(x, ast.FunctionDef) and x.name == n for x in cuerpo)]
    if faltan: raise SystemExit('[juegos] isla.py ya no define: ' + ', '.join(faltan))
    exec(compile(ast.Module(body=cuerpo, type_ignores=[]), ISLA, 'exec'), ns)
    return ns

_ISLA = _cargar_de_isla()
globals().update({k: v for k, v in _ISLA.items() if not k.startswith('__') and k not in ('bpy', 'bmesh', 'math', 'os', 'sys', 'time', 'random', 'Vector', 'scene', 'C', 'args', 'arg', 'ENGINE', 'SAMPLES', 'WIDTH')})

ISLA_NS = _ISLA
MAT_KIT = dict(bump=0.28, sss=0.28, vary=0.012, rough=0.5, dents=0.0, grain=0.0, mid=0.3, ao=0.28, ao_cool=1.0, sat=1.0, piece=0, prints=0.0)     # los parámetros de `mat_kit` de isla.py

@contextlib.contextmanager
def material_kit(**params):
    """Mientras dura el bloque, las losas del kit (`losa_kit`) usan el material del kit con estos cambios (por ejemplo `spec=0.05, sheen=0.0` para que un color oscuro no salga lechoso)."""
    orig = ISLA_NS['mat_kit']
    ISLA_NS['mat_kit'] = lambda nombre, color: clay(nombre + '_j' + '_'.join(f'{k}{v}' for k, v in sorted(params.items())), color, **{**MAT_KIT, **params})
    try: yield
    finally: ISLA_NS['mat_kit'] = orig

# ------------------------------------------------------------------ escena
def preparar():
    """Luz del estudio de la isla, compositor y ajustes de render (Cycles, fondo transparente), igual que `isla.py --kit`."""
    studio(); compositor(); setup_render(); scene.render.film_transparent = True
    scene.view_settings.exposure = EXPOSICION
    return scene

def raiz():
    """Un padre estirado en Y por 1/cos(inclinación): lo que cuelga de él se ve del tamaño pedido en la imagen (el disco redondo, las esquinas redondas)."""
    r = bpy.data.objects.new('juego_raiz', None); scene.collection.objects.link(r); r.scale = (1, 1 / math.cos(math.radians(TILT)), 1); return r

def suelo():
    """El suelo que solo recibe sombra (shadow catcher): la sombra de contacto cae abajo a la derecha y el resto queda transparente."""
    bpy.ops.mesh.primitive_cube_add(size=1); g = bpy.context.object; g.scale = (60, 60, 0.2); bpy.ops.object.transform_apply(scale=True)
    g.location = (0, 0, -0.1); g.name = 'juego_suelo'; link(g, 'iconos'); g.is_shadow_catcher = True; return g

def camara(ancho, alto, px_por_u, centro=(0.0, 0.0)):
    """Cámara ortogonal inclinada. [ancho]/[alto] en unidades de diseño; [centro] es el punto del lienzo (x, y de diseño) que cae en el centro de la imagen."""
    t = math.radians(TILT); d = Vector((0, -math.sin(t), math.cos(t)))
    cam = bpy.data.cameras.new('cam_juego'); cam.type = 'ORTHO'; cam.ortho_scale = ancho
    co = bpy.data.objects.new('cam_juego', cam); scene.collection.objects.link(co)
    tgt = Vector((centro[0], centro[1] / math.cos(t), 0.0)); co.location = tuple(tgt + d * 30)
    co.rotation_euler = (tgt - co.location).to_track_quat('-Z', 'Y').to_euler(); scene.camera = co
    scene.render.resolution_x = int(round(ancho * px_por_u)); scene.render.resolution_y = int(round(alto * px_por_u))
    return co

def sube(h):
    """Cuánto sube en la imagen (unidades de diseño) algo que está a altura [h]: el centro de un objeto de alto h aparece h/2·sen(TILT) más arriba que su huella."""
    return h * math.sin(math.radians(TILT))

# ------------------------------------------------------------------ geometría a partir de contornos
def circulo(r, n=96, cx=0.0, cy=0.0):
    return [(cx + r * math.cos(2 * math.pi * i / n), cy + r * math.sin(2 * math.pi * i / n)) for i in range(n)]

def redondeado(pts, r, arco=10):
    """El contorno de un polígono CONVEXO [pts] (antihorario) engordado [r] con esquinas de círculo: la misma figura que `Polygon(x, y, pts) - r` de ClayRaster."""
    n = len(pts); out = []
    for i in range(n):
        p0, p1, p2 = Vector(pts[i - 1]), Vector(pts[i]), Vector(pts[(i + 1) % n])
        e0 = (p1 - p0).normalized(); e1 = (p2 - p1).normalized()
        n0 = Vector((e0.y, -e0.x)); n1 = Vector((e1.y, -e1.x))      # normales hacia afuera (antihorario)
        a0 = math.atan2(n0.y, n0.x); a1 = math.atan2(n1.y, n1.x)
        da = (a1 - a0) % (2 * math.pi)
        for k in range(arco + 1):
            a = a0 + da * k / arco; out.append((p1.x + r * math.cos(a), p1.y + r * math.sin(a)))
    return out

def casco_convexo(circulos, n=64):
    """El contorno de la envolvente convexa de varios círculos [(cx, cy, r), ...] (la gota de Rescate: dos círculos con tangentes)."""
    pts = []
    for (cx, cy, r) in circulos:
        for i in range(n): a = 2 * math.pi * i / n; pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    pts.sort()
    def cruz(o, a, b): return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    inf = []; sup = []
    for p in pts:
        while len(inf) >= 2 and cruz(inf[-2], inf[-1], p) <= 1e-12: inf.pop()
        inf.append(p)
    for p in reversed(pts):
        while len(sup) >= 2 and cruz(sup[-2], sup[-1], p) <= 1e-12: sup.pop()
        sup.append(p)
    return inf[:-1] + sup[:-1]

def pieza(contorno, h, bis, color=None, nombre='pieza', z0=0.0, seg=8, panza=0.0, bultos=0.0, material=None, padre=None, coll='iconos'):
    """Una pieza de plasticina: el [contorno] (lista de (x, y) antihoraria, unidades de diseño) levantado [h], con el canto redondeado [bis] y, si [panza] > 0, el costado abombado.
    El material es el del kit (`mat_kit`) del [color] (o el [material] ya hecho). Cuelga de [padre] (la raíz estirada).
    [bultos] > 0 le suma ruido al contorno (subdivisión + desplazamiento): NO en caras grandes y planas (la subdivisión del polígono deja costuras radiales visibles); sí en piezas irregulares como la roca."""
    me = bpy.data.meshes.new(nombre); bm = bmesh.new(); n = len(contorno); R = 2
    capas = []
    for i in range(R + 1):
        t = i / R; k = 1 + panza * math.sin(math.pi * t)
        capas.append([bm.verts.new((x * k, y * k, z0 + h * t)) for (x, y) in contorno])
    bm.faces.new(list(reversed(capas[0]))); bm.faces.new(capas[-1])
    for i in range(R):
        for j in range(n): bm.faces.new((capas[i][j], capas[i][(j + 1) % n], capas[i + 1][(j + 1) % n], capas[i + 1][j]))
    bm.normal_update(); bm.to_mesh(me); bm.free(); me.update()
    o = bpy.data.objects.new(nombre, me); scene.collection.objects.link(o); link(o, coll)
    md = bevel(o, bis, seg, ang=40); md.limit_method = 'ANGLE'; smooth(o)
    wn = o.modifiers.new('Normales', 'WEIGHTED_NORMAL'); wn.keep_sharp = True; wn.weight = 60      # la cara grande queda plana y el canto suave (sin las costuras del polígono)
    if bultos: organic(o, bultos, 1.4, 1)
    o.data.materials.append(material if material is not None else mat_kit('m_j_' + nombre + '_' + str(color).strip('#'), color))
    if padre is not None: o.parent = padre
    return o

def disco(r, h, bis, color=None, loc=(0, 0, 0), nombre='disco', material=None, padre=None, verts=64):
    """Un disco de plasticina redondo (cilindro bajo con el canto blando)."""
    o = cyl(r=r, d=h, verts=verts, loc=(loc[0], loc[1], loc[2] + h / 2), coll='iconos', name=nombre)
    bevel(o, bis, 8); organic(o, 0.002, 1.4, 1)
    o.data.materials.append(material if material is not None else mat_kit('m_j_' + nombre + '_' + str(color).strip('#'), color))
    if padre is not None: o.parent = padre
    return o

def esfera(r, color=None, loc=(0, 0, 0), escala=(1, 1, 1), nombre='esfera', material=None, padre=None, seg=48):
    o = sphere(r=r, seg=seg, loc=loc, coll='iconos', name=nombre); o.scale = escala
    o.data.materials.append(material if material is not None else mat_kit('m_j_' + nombre + '_' + str(color).strip('#'), color))
    if padre is not None: o.parent = padre
    return o

def aro_plano(R, r, color=None, loc=(0, 0, 0), nombre='aro', material=None, padre=None, sx=1.0):
    """Un aro (churro curvado) acostado: radio mayor [R], grosor [r]."""
    o = torus(R=R, r=r, loc=loc, coll='iconos', name=nombre); o.scale = (sx, 1, 1)
    o.data.materials.append(material if material is not None else mat_kit('m_j_' + nombre + '_' + str(color).strip('#'), color))
    if padre is not None: o.parent = padre
    return o

def churro_c(p0, p1, r, z, material, nombre='churro'):
    """Un churro de plasticina entre dos puntos de DISEÑO (y arriba) a la altura [z]: las posiciones se corrigen por 1/cos(inclinación) y la forma queda redonda."""
    t = math.radians(TILT); k = 1 / math.cos(t)
    a = Vector((p0[0], p0[1] * k, z)); b = Vector((p1[0], p1[1] * k, z)); d = b - a
    o = cyl(r=r, d=d.length, verts=24, loc=tuple((a + b) / 2), coll='iconos', name=nombre); o.rotation_euler = d.to_track_quat('Z', 'Y').to_euler(); o.data.materials.append(material)
    for pt in (a, b):
        c = sphere(r=r, seg=20, loc=tuple(pt), coll='iconos', name=nombre + '_punta'); c.data.materials.append(material)

def mat_oscuro(nombre, color):
    """El material del kit para un color OSCURO: con el brillo normal el sol sobre la cara plana le suma un gris de ~45/255 y la plasticina sale lechosa; con poco especular y sin «sheen» queda a ±8/255 del color pedido (igual que las casillas «noche»)."""
    return clay(nombre, color, **{**MAT_KIT, 'spec': 0.05, 'sheen': 0.0})

def tinta(): return mat_tinta()

# ------------------------------------------------------------------ render
times = {}
def render(nombre, visible=('iconos', 'luces')):
    """Renderiza la escena actual a OUT/<nombre>.png (RGBA, fondo transparente) y devuelve los segundos."""
    for cn, c in C.items(): c.hide_render = cn not in visible
    scene.render.filepath = os.path.join(OUT, nombre + '.png'); t0 = time.time(); bpy.ops.render.render(write_still=True); dt = time.time() - t0
    times[nombre] = dt; print(f'[juegos] {nombre}: {dt:.1f} s'); sys.stdout.flush(); return dt

def limpia(antes):
    """Borra lo que se creó desde que se tomó la foto [antes] = set(bpy.data.objects)."""
    for o in [o for o in bpy.data.objects if o not in antes]: bpy.data.objects.remove(o, do_unlink=True)

def una(nombre, construir, ancho, alto, px_por_u, centro=(0.0, 0.0), visible=('iconos', 'luces')):
    """Arma una pieza con [construir](raiz) en un lienzo de [ancho] × [alto] unidades a [px_por_u] píxeles por unidad, la renderiza y limpia la escena."""
    antes = set(bpy.data.objects); camara(ancho, alto, px_por_u, centro); suelo(); r = raiz(); construir(r)
    render(nombre, visible); limpia(antes)

def termina():
    with open(os.path.join(OUT, 'tiempos.txt'), 'w', encoding='utf-8') as f:
        for k, v in times.items(): f.write(f'{k}: {v:.1f} s\n')
    print('[juegos] listo')
