# -*- coding: utf-8 -*-
"""¿Verdad o disparate? en plasticina (Tarea 97): la antena de la sala de radio, los dos íconos de los botones (✓ y la onda rota) y las barras del medidor de señal. La placa de la transmisión, los botones VERDAD y DISPARATE y el panel del final salen de las
losas del kit (`comun`): no se renderiza nada para ellos.

Uso: blender -b -P tools/juegos/disparate.py -- [--solo antena|iconos|medidor] [--samples N] [--out DIR]
Salida (en --out, por defecto tools/juegos/out): las «maestras» con fondo transparente; `empaqueta.py disparate` las reduce al tamaño final.

Cada pieza sale en el MISMO lienzo y a la MISMA escala que el sprite procedural al que reemplaza (la antena, ±1,2: de ahí salen `DisparateSprites.AntennaTip`, `AntennaDish` y `AntennaFloor`, donde el código pone las ondas y la luz; los íconos, ±1), así el
controlador no cambia ninguna medida. La plasticina ocupa el cuerpo de hoy más su borde de tinta (la del kit no lleva borde).
  - `antena`: plataforma de acero, base uva, patas y mástil crema con remaches de tinta, plato crema con borde celeste, brazo y luz sol en la punta (la luz que pulsa es un resplandor procedural encima).
  - `icono_visto`, `icono_ola`: la marca ✓ y la onda rota, churros de tinta (se ponen sobre las losas lima y coral).
  - `medidor_encendida`, `medidor_apagada`: UNA barra con los extremos redondos que el código estira en vertical (nine-slice: 5 barras de 50 a 154 u de alto y 34 u de ancho). 1 unidad de Blender = 34 u del lienzo de 1080.
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *

SOLO = arg('--solo', 'todo')
CREMA = '#FFF8EC'; CIELO = '#4CC9F0'; SOL = '#FFC93C'; UVA = '#B8A4FF'; ACERO = '#39408F'; APAGADA = '#38428F'
L = 0.05                                                  # el borde de tinta de hoy

# ------------------------------------------------------------------ la antena (DisparateSprites.RenderAntenna: lienzo ±1,2). Parabólica (corrección del 14-oct): el plato inclinado hacia un lado, un brazo con su receptor y la luz aparte, un mástil con su base.
ANT_LIENZO = 2.4
ANT_PX = 640                                              # la maestra: se reduce a 420 (el tamaño en el juego)
ANT_H = 0.16
LILA = '#8E7CE0'                                          # el mástil y el brazo: tono contra tono con el plato crema
TINTA_SUAVE = '#4A3F8C'                                   # el receptor (la boca del brazo)
EJE = Vector((0.38, 0.30, 0.875)).normalized()            # hacia dónde mira el plato (x a la derecha, y arriba, z hacia el espectador): se ve ~28° inclinado de la vertical y con su interior a la vista
PLATO_R, PLATO_PROF, PLATO_GROSOR = 0.58, 0.20, 0.075    # radio, profundidad (poca: es un plato, no una copa) y grosor
V_IMG, V_Z = (0.03, 0.26), 0.45                           # dónde se ve el fondo del plato (diseño) y a qué altura flota

def mundo(x, yd, z):
    """El punto del mundo que se ve en la imagen en (x, yd) de diseño estando a altura [z] (la altura sube la imagen sen(inclinación) por unidad)."""
    return Vector((x, (yd - sube(z)) / math.cos(math.radians(TILT)), z))

def a_imagen(p):
    """Dónde se ve un punto del mundo en la imagen (diseño), con la altura incluida."""
    t = math.radians(TILT); return (p.x, p.y * math.cos(t) + p.z * math.sin(t))

def varilla(a, b, r, material, nombre='varilla'):
    """Un churro de plasticina entre dos puntos del MUNDO (en 3D, no a una altura fija)."""
    d = b - a
    o = cyl(r=r, d=d.length, verts=24, loc=tuple((a + b) / 2), coll='iconos', name=nombre); o.rotation_euler = d.to_track_quat('Z', 'Y').to_euler(); o.data.materials.append(material)
    for pt in (a, b):
        c = sphere(r=r, seg=20, loc=tuple(pt), coll='iconos', name=nombre + '_punta'); c.data.materials.append(material)

def plato(R, prof, grosor, mat_fuera, mat_dentro, nombre='ant_plato'):
    """Un plato parabólico: un cuenco de poca profundidad ([prof]) con grosor, el interior (lo que mira hacia +Z) en el material [mat_dentro] y un labio crema en el borde. El eje es Z; se inclina con el padre."""
    me = bpy.data.meshes.new(nombre); bm = bmesh.new(); n, m = 14, 72
    def anillo(r, z): return [bm.verts.new((r * math.cos(2 * math.pi * j / m), r * math.sin(2 * math.pi * j / m), z)) for j in range(m)]
    c_in = bm.verts.new((0, 0, 0)); c_out = bm.verts.new((0, 0, -grosor))
    ri = [anillo(R * i / n, prof * (i / n) ** 2) for i in range(1, n + 1)]
    ro = [anillo(R * i / n, prof * (i / n) ** 2 - grosor) for i in range(1, n + 1)]
    for j in range(m):
        k = (j + 1) % m
        f = bm.faces.new((c_in, ri[0][j], ri[0][k])); f.material_index = 1
        bm.faces.new((c_out, ro[0][k], ro[0][j]))
        for i in range(n - 1):
            f = bm.faces.new((ri[i][j], ri[i + 1][j], ri[i + 1][k], ri[i][k])); f.material_index = 1 if i < n - 2 else 0           # el último anillo de adentro queda crema: el labio
            bm.faces.new((ro[i][j], ro[i][k], ro[i + 1][k], ro[i + 1][j]))
        bm.faces.new((ri[n - 1][j], ro[n - 1][j], ro[n - 1][k], ri[n - 1][k]))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces)); bm.normal_update(); bm.to_mesh(me); bm.free(); me.update()
    o = bpy.data.objects.new(nombre, me); scene.collection.objects.link(o); link(o, 'iconos')
    smooth(o); md = bevel(o, 0.012, 4, ang=35); wn = o.modifiers.new('Normales', 'WEIGHTED_NORMAL'); wn.keep_sharp = True
    o.data.materials.append(mat_fuera); o.data.materials.append(mat_dentro)
    return o

def antena(r):
    # plataforma y base (como antes)
    pieza(redondeado(rect(0, -0.86, 0.84, 0.05), 0.08 + L), 0.10, 0.05, nombre='ant_plataforma', padre=r, material=mat_oscuro('m_j_ant_acero', ACERO))
    pieza(redondeado(rect(0, -0.62, 0.33, 0.08), 0.07 + L), 0.14, 0.06, UVA, 'ant_base', z0=0.05, padre=r)
    for sx in (-1, 1): disco(0.035, 0.03, 0.01, material=tinta(), loc=(sx * 0.28, -0.62, 0.19 - 0.012), nombre='ant_remache_base', padre=r)
    mat_l = mat_kit('m_j_ant_lila', LILA)
    # el vértice del plato, el cubo detrás de él y el mástil (corto, lila, del tope de la base al cubo)
    V = mundo(V_IMG[0], V_IMG[1], V_Z); B = V - EJE * (PLATO_GROSOR + 0.01)
    varilla(mundo(0.0, -0.54, 0.19), B, 0.066, mat_l, 'ant_mastil')
    esfera(0.09, material=mat_l, loc=tuple(B), nombre='ant_cubo')
    # el plato, inclinado: crema por fuera, celeste por dentro, con un labio crema redondo
    raiz_p = bpy.data.objects.new('ant_plato_raiz', None); scene.collection.objects.link(raiz_p)
    raiz_p.location = V; raiz_p.rotation_euler = EJE.to_track_quat('Z', 'Y').to_euler()
    crema = mat_kit('m_j_ant_crema', CREMA)
    o = plato(PLATO_R, PLATO_PROF, PLATO_GROSOR, crema, mat_kit('m_j_ant_interior', CIELO)); o.parent = raiz_p
    lab = torus(R=PLATO_R, r=PLATO_GROSOR * 0.55, loc=(0, 0, PLATO_PROF - PLATO_GROSOR * 0.5), coll='iconos', name='ant_labio'); lab.data.materials.append(crema); lab.parent = raiz_p
    # el brazo del receptor (lila), el receptor (tinta suave) y, aparte, la luz de la punta
    F0 = V + EJE * 0.86
    varilla(V + EJE * 0.02, F0, 0.04, mat_l, 'ant_brazo')
    rec = cyl(r=0.075, d=0.10, verts=32, loc=tuple(F0 + EJE * 0.04), coll='iconos', name='ant_receptor'); rec.rotation_euler = EJE.to_track_quat('Z', 'Y').to_euler()
    bevel(rec, 0.02, 6); rec.data.materials.append(mat_kit('m_j_ant_receptor', TINTA_SUAVE))
    luz = F0 + EJE * 0.19
    esfera(0.105, SOL, loc=tuple(luz), nombre='ant_luz')
    esfera(0.03, '#FFFFFF', loc=tuple(luz + Vector((-0.04, -0.03, 0.08))), escala=(1.2, 0.8, 0.6), nombre='ant_luz_brillo')
    print('[juegos] antena: la luz cae en', a_imagen(luz), 'y el plato en', a_imagen(V + EJE * PLATO_PROF * 0.5))

def render_antena():
    una('antena', antena, ANT_LIENZO, ANT_LIENZO, ANT_PX / ANT_LIENZO, centro=(0.0, sube(0.10) / 2))

# ------------------------------------------------------------------ los íconos de los botones (DisparateSprites.CheckIcon y BrokenWaveIcon: lienzo ±1)
ICO_LIENZO = 2.0
ICO_PX = 256                                              # la maestra: se reduce a 128 (el ícono mide 120 u en el juego)
ICO_R = 0.155

def icono_visto(r):
    for (a, b) in (((-0.55, 0.0), (-0.18, -0.4)), ((-0.18, -0.4), (0.6, 0.46))): churro_c(a, b, ICO_R, ICO_R, tinta(), 'ico_visto')

def churro_curva(pts, r, z, material, nombre='trazo'):
    """Un churro de plasticina que sigue una curva (puntos de DISEÑO, y arriba) a la altura [z]: UN tubo continuo con las puntas redondas (sin las costuras de varios cilindros)."""
    k = 1 / math.cos(math.radians(TILT))
    cu = bpy.data.curves.new(nombre, 'CURVE'); cu.dimensions = '3D'; cu.bevel_depth = r; cu.bevel_resolution = 8; cu.resolution_u = 4
    sp = cu.splines.new('POLY'); sp.points.add(len(pts) - 1)
    for p, (x, y) in zip(sp.points, pts): p.co = (x, y * k, z, 1.0)
    o = bpy.data.objects.new(nombre, cu); scene.collection.objects.link(o); link(o, 'iconos'); cu.materials.append(material)
    for (x, y) in (pts[0], pts[-1]):
        c = sphere(r=r, seg=24, loc=(x, y * k, z), coll='iconos', name=nombre + '_punta'); c.data.materials.append(material)

def ola(x0, x1, y0, fase, amp, vueltas=0.8, n=40):
    """Los puntos de un tramo de onda (seno suave) de [x0] a [x1]: [vueltas] ondas completas, con [fase] (en vueltas) al empezar y el centro en [y0]."""
    return [(x0 + (x1 - x0) * i / n, y0 + amp * math.sin(2 * math.pi * (fase + vueltas * i / n))) for i in range(n + 1)]

def icono_ola(r):
    """Una onda de churro de tinta CORTADA en dos tramos desalineados, con un hueco claro entre ellos (corrección del 14-oct: antes eran dos zigzags que se leían como dos manchas)."""
    for pts in (ola(-0.88, -0.27, 0.17, 0.0, 0.22, 0.85), ola(0.27, 0.88, -0.17, 0.0, 0.22, 0.85)):
        churro_curva(pts, 0.105, 0.105, tinta(), 'ico_ola')

def render_iconos():
    una('icono_visto', icono_visto, ICO_LIENZO, ICO_LIENZO, ICO_PX / ICO_LIENZO, centro=(0.0, sube(2 * ICO_R) / 2))
    una('icono_ola', icono_ola, ICO_LIENZO, ICO_LIENZO, ICO_PX / ICO_LIENZO, centro=(0.0, sube(0.21) / 2))

# ------------------------------------------------------------------ las barras del medidor (1 unidad = 34 u: ancho 1, largo 3,5; el lienzo deja 0,4 de sombra a cada lado)
MED_W, MED_H, MED_M = 1.0, 3.5, 0.4
MED_PX_U = 136.0                                           # la maestra: 136 px por unidad (= 4 px por u del lienzo de 1080); se reduce a 2 px por u
MED_RELIEVE = 0.30

def barra(color, oscura):
    def construir(r):
        cont = casco_convexo([(0.0, -(MED_H / 2 - MED_W / 2), MED_W / 2), (0.0, MED_H / 2 - MED_W / 2, MED_W / 2)])
        if oscura: pieza(cont, MED_RELIEVE, 0.15, nombre='medidor', padre=r, material=mat_oscuro('m_j_medidor_apagada', color))
        else: pieza(cont, MED_RELIEVE, 0.15, color, 'medidor', padre=r)
    return construir

def render_medidor():
    for nombre, color, oscura in (('medidor_encendida', SOL, False), ('medidor_apagada', APAGADA, True)):
        una(nombre, barra(color, oscura), MED_W + 2 * MED_M, MED_H + 2 * MED_M, MED_PX_U, centro=(0.0, sube(MED_RELIEVE) / 2))

preparar()
if SOLO in ('todo', 'antena'): render_antena()
if SOLO in ('todo', 'iconos'): render_iconos()
if SOLO in ('todo', 'medidor'): render_medidor()
termina()
