# -*- coding: utf-8 -*-
"""Las piezas comunes de los juegos en plasticina (fase 0 de la Tarea 94), lo que NO sale ya hecho del kit de la app:
- las losas «noche» de las casillas de respuesta (normal, presionada, acierto, error y apagada): losas md (radio 22 dp) oscuras, para que los símbolos de color de los juegos resalten;
- una losa md crema de CONTROL (se compara con `kit_losa_md_crema` de la app para comprobar que este pipeline da el mismo kit);
- las marcas de respuesta ✓ (disco lima) y ✗ (disco coral) con el glifo de churro tinta;
- el anillo de la cuenta regresiva y, desde la Tarea 148 (P9), los numerales 3, 2 y 1 de plasticina que van sobre él (antes un `Text` plano con sombra dura).
Las losas de botones y paneles (sol, crema, cielo, lila, lima, coral; ch, md, gr) NO se vuelven a renderizar: `empaqueta.py` las toma de los WebP del kit de la app (`app/src/main/res/drawable-nodpi/kit_losa_*.webp`).

Uso: blender -b -P tools/juegos/comun.py -- [--solo losas|marcas|anillo|numerales] [--samples N] [--out DIR]   (salida por defecto: tools/juegos/out/comun)
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *

SOLO = arg('--solo', 'todo')
# los colores de diseño de las casillas de Rescate (ButtonLive, ButtonHit, ButtonWrong, ButtonDim)
DISENO = {'noche': '#2A2566', 'acierto': '#3D6A2C', 'error': '#6B2E3A', 'apagada': '#1C1A48'}

def render_losas():
    losa('losa_md_crema_control', KIT_CREMA)
    # Un color oscuro con el brillo normal del kit sale lechoso (el sol sobre la cara plana le suma un gris de ~45/255): con poco especular y sin «sheen» el centro queda a ±8/255 del color de diseño
    with material_kit(spec=0.05, sheen=0.0):
        for clave, dis in DISENO.items():
            losa('losa_md_' + clave, dis)
            if clave in ('noche', 'apagada'): losa('losa_md_' + clave + '_p', dis, presionada=True)          # (la apagada: las herramientas de Carga exacta, que se tocan)

# ------------------------------------------------------------------ marcas ✓ y ✗ (AnswerMarkSprite: lienzo ±1,1, disco de radio 0,8, glifo de churro tinta)
def marca(color, glifo):
    H = 0.16
    def construir(r):
        disco(0.8, H, 0.07, color, loc=(0, 0, 0), nombre='marca_disco', padre=r, verts=72)
        z = H + 0.07
        for seg in glifo: churro_c(seg[0], seg[1], 0.105, z, tinta(), 'marca_glifo')
    return construir

VISTO = (((-0.36, 0.02), (-0.10, -0.26)), ((-0.10, -0.26), (0.38, 0.30)))
ASPA = (((-0.30, -0.30), (0.30, 0.30)), ((-0.30, 0.30), (0.30, -0.30)))

def render_marcas():
    una('marca_ok', marca(KIT_LIMA, VISTO), 2.2, 2.2, 128 / 2.2, centro=(0.0, sube(0.16) / 2))
    una('marca_no', marca(KIT_CORAL, ASPA), 2.2, 2.2, 128 / 2.2, centro=(0.0, sube(0.16) / 2))

# ------------------------------------------------------------------ el anillo de la cuenta regresiva (RingSprite: radios 0,39 a 0,48 del lado; se tiñe con Image.color y se llena en radial)
def anillo_cuenta(r):
    aro_plano(0.435, 0.047, '#F3F0FF', loc=(0, 0, 0.047), nombre='cuenta_anillo', padre=r)

def render_anillo():
    una('anillo_cuenta', anillo_cuenta, 1.0, 1.0, 640.0, centro=(0.0, sube(0.094) / 2))

# ------------------------------------------------------------------ los numerales de la cuenta regresiva (Tarea 148, P9): 3, 2 y 1 de plasticina, claros (el código los tiñe con el color de cada paso, como al anillo)
# Churros gordos de plasticina (como el glifo de las marcas ✓ y ✗): un trazo continuo por cada numeral, con las puntas y las esquinas redondas. Se probó la tipografía Fredoka extruida de Blender: sus contornos se
# superponen (es una fuente variable) y el 3 y el 1 salían rajados; el trazo a mano no tiene ese problema y queda del mismo grosor en los tres.
def arco(cx, cy, rx, ry, a0, a1, n=30):
    return [(cx + rx * math.cos(math.radians(a0 + (a1 - a0) * i / n)), cy + ry * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]

NUMERALES = {
    '3': arco(0.0, 0.150, 0.150, 0.150, 150, -90) + arco(0.0, -0.150, 0.172, 0.150, 90, -150)[1:],
    '2': arco(0.0, 0.140, 0.170, 0.160, 160, -38) + [(-0.175, -0.300), (0.195, -0.300)],
    '1': [(-0.170, 0.205), (0.050, 0.300), (0.050, -0.300)],
}
GROSOR_NUMERAL = float(arg('--num-r', '0.078'))                           # radio del churro (el numeral mide 0,6 + 2 radios de alto en un lienzo de 1)

def numeral(txt):
    def construir(r):
        pts = NUMERALES[txt]; rad = GROSOR_NUMERAL; k = 1 / math.cos(math.radians(TILT))                # la raíz de `una` no se usa: el churro quedaría estirado en y (como `churro_c`)
        xs = [p[0] for p in pts]; cx = (min(xs) + max(xs)) / 2; pts = [(x - cx, y) for x, y in pts]
        m = mat_kit('m_j_cuenta_numeral', '#F3F0FF')
        cu = bpy.data.curves.new('cuenta_' + txt, 'CURVE'); cu.dimensions = '3D'; cu.bevel_depth = rad; cu.bevel_resolution = 8; cu.use_fill_caps = True; cu.resolution_u = 4
        sp = cu.splines.new('POLY'); sp.points.add(len(pts) - 1)
        for i, (x, y) in enumerate(pts): sp.points[i].co = (x, y * k, rad, 1.0)
        o = bpy.data.objects.new('cuenta_' + txt, cu); scene.collection.objects.link(o); link(o, 'iconos'); cu.materials.append(m)
        esquinas = [pts[0], pts[-1]] + ([pts[len(pts) // 2]] if txt == '3' else []) + ([pts[1]] if txt == '1' else []) + ([pts[-2]] if txt == '2' else [])
        for (x, y) in esquinas:
            s = sphere(r=rad, seg=28, loc=(x, y * k, rad), coll='iconos', name='cuenta_punta'); s.data.materials.append(m)
    return construir

def render_numerales():
    for d in ('3', '2', '1'): una('cuenta_' + d, numeral(d), 1.0, 1.0, 640.0, centro=(0.0, sube(2 * GROSOR_NUMERAL) / 2))

preparar()
if SOLO in ('todo', 'losas'): render_losas()
if SOLO in ('todo', 'marcas'): render_marcas()
if SOLO in ('todo', 'anillo'): render_anillo()
if SOLO in ('todo', 'numerales'): render_numerales()
termina()
