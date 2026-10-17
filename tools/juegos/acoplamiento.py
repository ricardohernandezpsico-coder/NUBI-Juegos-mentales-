# -*- coding: utf-8 -*-
"""Acoplamiento — ESTUDIO de la plasticina (Tarea 113, fase A): las placas maestras con las que `acoplamiento_estudio.py` compara los dos caminos. NO se usa en el juego.

La silueta ES la respuesta (módulo y hueco salen del MISMO SDF: unión de cajas redondeadas de radio 0,16 bloque, esquinas cóncavas VIVAS), así que las placas llevan el contorno EXACTO y el bisel solo va en el canto de ARRIBA
(`bevel_weight` en ese lazo): ni redondea las esquinas verticales (la cóncava quedaría con un radio de 0,075 bloque: 2,5 px de error a 80 px por bloque) ni ensancha ni angosta nada.
Todo se ve exactamente desde arriba, sin suelo, con la luz del estudio promediada en CUATRO giros (0°, 90°, 180°, 270°; `empaqueta`/`acoplamiento_estudio.py` la simetriza además con los espejos): ninguna luz con dirección queda horneada.
1 bloque = 26 dp = 1,3 unidades de Blender (la escala de la app); 160 píxeles por bloque en la maestra (el juego hornea 80).

Uso: blender -b -P tools/juegos/acoplamiento.py -- --tilt 0 [--solo mod|hueco] [--samples N] [--out DIR]
  mod_convexa, mod_borde, mod_concava, mod_interior   : el módulo (celeste) con la característica en el origen (esquina convexa, borde, esquina cóncava y una placa lisa de 4,8 bloques para la textura)
  aro_*, fon_*  (convexa, borde, concava, interior): el hueco del puerto en DOS capas, como hoy: el aro blanco (se tiñe de teal, lima o celeste) y el fondo oscuro hundido, con lo mismo
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *
from mathutils import Matrix, Vector
from mathutils.geometry import tessellate_polygon
import bmesh

SOLO = arg('--solo', 'todo')
B = 1.3                                                    # unidades de Blender por bloque
PXB = 160.0                                                # píxeles por bloque de la maestra
PX_U = PXB / B                                             # píxeles por unidad
R_ESQ = 0.16                                               # radio de las esquinas convexas del SDF (bloques)
BORDE = 3.0 / 26.0                                         # el aro del hueco: 0,115 bloque por fuera del contorno
ALTO = 0.16 * B                                            # grosor de la placa
BIS = 0.075 * B                                            # canto blando del borde de arriba (hacia adentro)
MODULO, ARO, FONDO = '#7FD8FF', '#F2F1FA', '#070818'
_R = {}

def sin_suelo(): bpy.data.objects['juego_suelo'].hide_render = True

def luces_giradas(grados):
    Rz = Matrix.Rotation(math.radians(grados), 4, 'Z')
    for o in bpy.data.objects:
        if o.type == 'LIGHT': o.matrix_world = Rz @ o.matrix_world

def promedio_cuatro(nombre, construir, ancho, alto):
    for k in range(4):
        una(f'{nombre}_d{k}', construir, ancho, alto, PX_U); luces_giradas(90.0)

# ------------------------------------------------------------------ el contorno exacto (unión de cajas redondeadas, esquinas cóncavas vivas) y sus desplazamientos
def contorno(vs, delta=0.0, r=R_ESQ, arco=16):
    """El contorno de un polígono rectilíneo [vs] (antihorario, en bloques) desplazado [delta] hacia afuera: las esquinas CONVEXAS son arcos de radio r+delta (el contorno de una caja redondeada); las cóncavas quedan vivas. Devuelve puntos en bloques."""
    n = len(vs); out = []
    for i in range(n):
        p0, p1, p2 = Vector(vs[i - 1]), Vector(vs[i]), Vector(vs[(i + 1) % n])
        e0 = (p1 - p0).normalized(); e1 = (p2 - p1).normalized()
        n0 = Vector((e0.y, -e0.x)); n1 = Vector((e1.y, -e1.x))
        cruz = e0.x * e1.y - e0.y * e1.x
        if cruz > 0:                                        # convexa: arco alrededor del centro de la esquina redondeada original
            c = p1 - (n0 + n1) * r; rho = r + delta
            a0 = math.atan2(n0.y, n0.x); da = (math.atan2(n1.y, n1.x) - a0) % (2 * math.pi)
            for k in range(arco + 1):
                a = a0 + da * k / arco; out.append((c.x + rho * math.cos(a), c.y + rho * math.sin(a)))
        else:                                               # cóncava: la esquina sigue viva, corrida delta por las dos paredes
            v = p1 + (n0 + n1) * delta / (1 + n0.dot(n1)); out.append((v.x, v.y))
    return out

def placa(lazos, h, bis, material, nombre, z0=0.0, padre=None):
    """Una placa de plasticina: la cara de arriba es el primer lazo (antihorario) con los demás como AGUJEROS (horarios); lados verticales; el canto blando solo en el borde de ARRIBA (bevel_weight): las esquinas verticales quedan vivas.
    Los lazos van en bloques; se pasan a unidades de Blender."""
    me = bpy.data.meshes.new(nombre); bm = bmesh.new()
    bw = bm.edges.layers.float.new('bevel_weight_edge')
    capas = []
    for lazo in lazos:
        pts = [(x * B, y * B) for (x, y) in lazo]
        capas.append([[bm.verts.new((x, y, z0 + h * t)) for (x, y) in pts] for t in (0.0, 0.5, 1.0)])
    poligonos = [[Vector((x * B, y * B, 0.0)) for (x, y) in lazo] for lazo in lazos]
    tris = tessellate_polygon(poligonos)
    flat = [v for capa in capas for v in capa[2]]; flat0 = [v for capa in capas for v in capa[0]]
    for t in tris:
        bm.faces.new((flat[t[0]], flat[t[1]], flat[t[2]]))
        bm.faces.new((flat0[t[2]], flat0[t[1]], flat0[t[0]]))
    for li, capa in enumerate(capas):
        n = len(capa[0]); sentido = 1 if li == 0 else -1               # los agujeros miran hacia adentro
        for i in range(2):
            for j in range(n):
                a, b = capa[i][j], capa[i][(j + 1) % n]; c, d = capa[i + 1][(j + 1) % n], capa[i + 1][j]
                f = bm.faces.new((a, b, c, d) if sentido > 0 else (b, a, d, c))
    bm.normal_update()
    for e in bm.edges:                                                     # solo los bordes de ARRIBA que tocan un LADO (no las diagonales internas de la triangulación de la cara)
        if all(abs(v.co.z - (z0 + h)) < 1e-6 for v in e.verts) and any(abs(f.normal.z) < 0.5 for f in e.link_faces): e[bw] = 1.0
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces)); bm.normal_update(); bm.to_mesh(me); bm.free(); me.update()
    o = bpy.data.objects.new(nombre, me); scene.collection.objects.link(o); link(o, 'iconos')
    md = o.modifiers.new('Bevel', 'BEVEL'); md.width = bis; md.segments = 8; md.limit_method = 'WEIGHT'; md.profile = 0.5; md.use_clamp_overlap = False; md.loop_slide = False
    smooth(o)
    wn = o.modifiers.new('Normales', 'WEIGHTED_NORMAL'); wn.keep_sharp = True; wn.weight = 60
    o.data.materials.append(material)
    if padre is not None: o.parent = padre
    return o

# ------------------------------------------------------------------ las placas (cada una con lo que se mide en el ORIGEN)
FORMAS = {
    'convexa': [(-2, -2), (0, -2), (0, 0), (-2, 0)],                       # la esquina convexa en (0, 0)
    'borde': [(-2, -2), (2, -2), (2, 0), (-2, 0)],                        # el borde de arriba a lo largo de x
    'concava': [(-2, -2), (2, -2), (2, 0), (0, 0), (0, 2), (-2, 2)],       # la esquina cóncava (viva) en (0, 0)
    'interior': [(-2.4, -2.4), (2.4, -2.4), (2.4, 2.4), (-2.4, 2.4)],      # una placa lisa de 4,8 bloques: de ahí sale la textura
}

def modulo(forma):
    def construir(r):
        _R['r'] = r; sin_suelo()
        placa([contorno(FORMAS[forma])], ALTO, BIS, mat_kit('m_acop_mod', MODULO), 'modulo_' + forma, padre=r)
    return construir

def hueco(forma, capa):
    """El hueco como DOS capas limpias (como hoy: el borde por debajo y el fondo encima): [capa] = 'aro' → el aro blanco (la placa con el hueco cortado) sobre una losa de relleno del mismo color (que queda debajo del fondo: sin costura), con el fondo invisible a la cámara;
    'fondo' → el fondo oscuro hundido (d ≤ 0) con el aro invisible a la cámara pero que da sombra: la oclusión de las paredes se ve. Las dos capas comparten la luz y la geometría, y sus alfas son los de sus contornos."""
    def construir(r):
        _R['r'] = r; sin_suelo()
        interior = contorno(FORMAS[forma]); exterior = contorno(FORMAS[forma], BORDE)
        fondo = placa([interior], ALTO * 0.35, BIS * 0.5, mat_kit('m_acop_fondo', FONDO), 'hueco_fondo_' + forma, padre=r)
        aro = placa([exterior, list(reversed(interior))], ALTO, BIS, mat_kit('m_acop_aro', ARO), 'hueco_aro_' + forma, padre=r)
        relleno = placa([exterior], ALTO * 0.35, BIS * 0.5, mat_kit('m_acop_aro', ARO), 'hueco_relleno_' + forma, padre=r)
        if capa == 'aro': fondo.visible_camera = False
        else: aro.visible_camera = False; relleno.visible_camera = False
    return construir

def render_todo():
    for forma in ('convexa', 'borde', 'concava', 'interior'):
        ancho = (5.2 if forma == 'interior' else 4.6) * B
        if SOLO in ('todo', 'mod'): promedio_cuatro('mod_' + forma, modulo(forma), ancho, ancho)
        if SOLO in ('todo', 'hueco'):
            promedio_cuatro('aro_' + forma, hueco(forma, 'aro'), ancho, ancho)
            promedio_cuatro('fon_' + forma, hueco(forma, 'fondo'), ancho, ancho)

preparar()
render_todo()
termina()
