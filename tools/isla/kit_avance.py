# -*- coding: utf-8 -*-
"""Piezas nuevas del kit para Avance y Juegos (Tarea 124; `auditoria-fable/spec-avance-juegos.md` §3): canal y churro, bolita del cambio, los 17 emblemas de juego, la farola apagada y encendida, el pedestal de lugar,
la cola del globo de Nubi, las 3 medallas (bloqueada y conseguida) y el escudo de liga en 7 colores mate.

NO se corre solo: `isla.py` lo carga con `--kit --kit-solo avance` (comparte sus funciones: `clay`, `box`, `cyl`, `sphere`, `cushion`, `bevel`, `organic`, `mat_kit`, `suelo_kit`, `raiz_kit`, `render`…). Uso:
  blender -b -P isla.py -- --engine CYCLES --samples 112 --kit --kit-solo avance --out out/kit-avance          (`--pieza emblema_,escudo_` renderiza solo las que empiezan así; `--samples 24` para ver rápido)
Deja un PNG por pieza (`kit_pz_<nombre>.png`) y `kit_avance.json` (tamaños y márgenes en dp, 3 px por dp). La luz es la del kit.

Dos cámaras, las mismas del resto del kit:
  · la del kit (ortogonal, 30° de inclinación: se ve la cara y un poco el canto de adelante) para lo que se acuesta sobre la superficie: canal, churro, bolita, emblemas, pedestal, cola y escudo. Todo cuelga de `raiz_kit()` (estirada 1/cos en Y,
    como las losas) para que las caras redondas se vean redondas; lo que debe verse redondo desde cualquier lado (esferas: `pz_rd_*`) cancela ese estirón;
  · la de los íconos de la barra (`cam_iso`, 37° de elevación) para lo que se para: la farola y las tres medallas (moneda de cara a la cámara y la figura parada sobre su losita).
Los símbolos: sin puntas, cruces ni medias lunas (`docs/simbolos-neutros.md`): todo es romo, gordo y redondo. Ningún texto va horneado: el número de una medalla, el de la división de un escudo y las letras van en Compose.
"""
import json

PIEZAS_SOLO = [p for p in arg('--pieza', '').split(',') if p]
PASTEL = {'mem': '#9CC7F5', 'ate': '#F7C58E', 'raz': '#C9B8F0', 'len': '#A9E3C0'}      # el pastel de cada área (Fable §3.4)
CREMA, SOL, LIMA, CIELO, CORAL, LILA, TINTA = KIT_CREMA, KIT_SOL, KIT_LIMA, KIT_CIELO, KIT_CORAL, KIT_LILA, KIT_TINTA
TIERRA, TIERRA_OSC = COL['tan'], COL['tanDk']
CRUDA, CRUDA_OSC = '#CFC8BE', '#B3ABA0'                                                  # plasticina cruda (medalla bloqueada): gris-crema, sin luz
LOSA = '#F1DFBF'                                                                         # la crema de la isla
T = math.radians(KIT_TILT); ST, CT = math.sin(T), math.cos(T)                            # inclinación de la cámara del kit
E = math.atan2(14.5 - 0.7, math.hypot(12.8, 12.8)); SE, CE = math.sin(E), math.cos(E)    # elevación de la cámara de los íconos de la barra
META = {}


def dp(x): return x / KIT_DPU


def mk(color):
    """Plasticina del kit del color dado."""
    return mat_kit('m_kitav_' + color.strip('#'), color)


def mate(color):
    """Plasticina mate (el escudo de liga: ni metal ni brillo)."""
    return clay('m_kitav_mate_' + color.strip('#'), color, bump=0.22, sss=0.2, vary=0.01, rough=0.66, sheen=0.1, dents=0.0, grain=0.0, mid=0.3, ao=0.25, ao_cool=1.0, sat=1.0, piece=0, prints=0.0)


def tubo(pts, r, color, cerrado=False, name='pz_tubo', res=8):
    """Un churro liso (curva con grosor) por los puntos dados (unidades): sin las cuentas que dejan los segmentos sueltos; con las puntas tapadas."""
    cu = bpy.data.curves.new(name, 'CURVE'); cu.dimensions = '3D'; cu.bevel_depth = r; cu.bevel_resolution = res; cu.use_fill_caps = True; cu.resolution_u = 4
    sp = cu.splines.new('POLY'); sp.points.add(len(pts) - 1)
    for i, q in enumerate(pts): sp.points[i].co = (q[0], q[1], q[2], 1.0)
    sp.use_cyclic_u = cerrado
    o = bpy.data.objects.new(name, cu); scene.collection.objects.link(o); link(o, 'iconos'); cu.materials.append(mk(color)); return o


def redondo(o, bev=0.08, seg=5, ang=60):
    bevel(o, bev, seg, ang); organic(o, 0.003, 1.4, 1); return o


def cono(r1, r2, d, **kw):
    bpy.ops.mesh.primitive_cone_add(vertices=48, radius1=r1, radius2=r2, depth=d); o = bpy.context.object; smooth(o); return place(o, **kw)


# ---------------------------------------------------------------------------------------------------- geometría de contornos
def estadio(W, H, n=14):
    """Contorno (dp) de una pastilla de W × H: dos semicírculos y los lados rectos; antihorario."""
    r = H / 2; cx = W / 2 - r; pts = []
    for k in range(n + 1): a = -math.pi / 2 + math.pi * k / n; pts.append((cx + r * math.cos(a), r * math.sin(a)))
    for k in range(n + 1): a = math.pi / 2 + math.pi * k / n; pts.append((-cx + r * math.cos(a), r * math.sin(a)))
    return pts


def redondea(pts, k=0.3, m=8):
    """Esquinas redondas de un polígono (curva cuadrática en cada vértice)."""
    out = []; n = len(pts)
    for i in range(n):
        p0, p1, p2 = pts[i - 1], pts[i], pts[(i + 1) % n]
        a = (p1[0] + (p0[0] - p1[0]) * k, p1[1] + (p0[1] - p1[1]) * k); b_ = (p1[0] + (p2[0] - p1[0]) * k, p1[1] + (p2[1] - p1[1]) * k)
        for j in range(m + 1):
            t = j / m; u = 1 - t; out.append((u * u * a[0] + 2 * u * t * p1[0] + t * t * b_[0], u * u * a[1] + 2 * u * t * p1[1] + t * t * b_[1]))
    return out


def losa_perfil(pts, h_dp, bev_dp, name='pz_losa', z_dp=0.0, rings=2, barrel=0.0):
    """Una losa de plasticina con el contorno dado (dp, antihorario o no): canto blando, cara casi lisa. Se queda en `iconos`; el llamador la pinta."""
    P = [(dp(x), dp(y)) for x, y in pts]
    if sum(P[i][0] * P[(i + 1) % len(P)][1] - P[(i + 1) % len(P)][0] * P[i][1] for i in range(len(P))) < 0: P.reverse()
    h = dp(h_dp); me = bpy.data.meshes.new(name); bm = bmesh.new(); n = len(P); layers = []
    for i in range(rings + 1):
        t = i / rings; sc = 1 + barrel * math.sin(math.pi * t); layers.append([bm.verts.new((x * sc, y * sc, h * t)) for (x, y) in P])
    bm.faces.new(list(reversed(layers[0]))); bm.faces.new(layers[-1])
    for i in range(rings):
        for j in range(n): bm.faces.new((layers[i][j], layers[i][(j + 1) % n], layers[i + 1][(j + 1) % n], layers[i + 1][j]))
    bm.normal_update(); bm.to_mesh(me); bm.free(); me.update()
    o = bpy.data.objects.new(name, me); scene.collection.objects.link(o); place(o, loc=(0, 0, dp(z_dp)), coll='iconos', name=name)
    bevel(o, dp(bev_dp), 8, ang=40); smooth(o); organic(o, 0.002, 1.4, 1); return o


# ---------------------------------------------------------------------------------------------------- cámaras y marco de cada pieza
def cam_tilt(w_dp, h_dp, alto_u):
    """La cámara del kit (ortogonal, inclinada KIT_TILT°) con el cuerpo centrado: lo que sube `alto_u` unidades se corre hacia arriba alto_u·sen(inclinación) en la imagen."""
    cam = cam_kit(0.0, alto_u * math.tan(T) / 2, w_dp, h_dp); cam.ortho_scale = max(w_dp, h_dp) / KIT_DPU; return cam


def cam_pie(w_dp, h_dp, zc):
    """La cámara de los íconos de la barra (37° de elevación, de frente) apuntando a la altura zc (unidades)."""
    cam = cam_iso(target=(0, 0, zc), ortho=max(w_dp, h_dp) / KIT_DPU, name='cam_pz')
    scene.render.resolution_x = int(round(w_dp * KIT_PX)); scene.render.resolution_y = int(round(h_dp * KIT_PX)); return cam


EDIF_ORTHO = 3.4; EDIF_ZC = 1.05                                                          # unidades que cubre la imagen y altura a la que mira la cámara


def cam_edif(w_dp, h_dp):
    """La cámara de la isla (de frente y a la derecha, ~37° de elevación) pero ortogonal, mirando a un edificio solo; los cuatro con la misma escala (la antena es la más alta)."""
    cam = bpy.data.cameras.new('cam_edif'); cam.type = 'ORTHO'; cam.ortho_scale = EDIF_ORTHO
    d = (Vector((12.8, -12.8, 14.5)) - Vector((0, 0, 0.7))).normalized(); tgt = Vector((0, 0, EDIF_ZC))
    co = bpy.data.objects.new('cam_edif', cam); scene.collection.objects.link(co); co.location = tuple(tgt + d * 30)
    co.rotation_euler = (tgt - co.location).to_track_quat('-Z', 'Y').to_euler(); scene.camera = co
    scene.render.resolution_x = int(round(w_dp * KIT_PX)); scene.render.resolution_y = int(round(h_dp * KIT_PX)); return co


def pie_en_imagen(co, w_dp, h_dp):
    """Dónde cae el pie del edificio (el origen) en la imagen, en dp desde la esquina de arriba a la izquierda."""
    from bpy_extras.object_utils import world_to_camera_view as w2c
    bpy.context.view_layer.update(); c = w2c(scene, co, Vector((0, 0, 0)))
    return [round(c.x * w_dp, 2), round((1 - c.y) * h_dp, 2)]


def iso_p(u, v, prof=0.0):
    """Punto del mundo que la cámara de pie ve en (u derecha, v arriba) dp del centro, `prof` dp hacia la cámara."""
    return (dp(u), dp(v) * SE - dp(prof) * CE, dp(v) * CE + dp(prof) * SE)


def suelo_pie(u, v, z):
    """Punto sobre el piso (altura z dp) que la cámara de pie ve en (u, v)."""
    return (dp(u), dp((v - z * CE) / SE), dp(z))


def de_cara(o, u, v, prof=0.0):
    """Pone un objeto plano (su cara mira a +Z) de cara a la cámara de pie, en (u, v) dp."""
    o.location = iso_p(u, v, prof); o.rotation_euler = (math.pi / 2 - E, 0, 0); return o


def tiene(nombre):
    return not PIEZAS_SOLO or any(nombre.startswith(p) for p in PIEZAS_SOLO)


def una(nombre, w_dp, h_dp, construye, camara='kit', alto_u=0.3, zc=0.6, visibles=('iconos', 'luces'), **meta):
    """Arma una pieza, la fotografía y la limpia. camara='kit': todo lo que cuelga queda bajo `raiz_kit()` (cara redonda = redonda); 'pie': cámara de los íconos de la barra."""
    if not tiene(nombre): return
    antes = set(bpy.data.objects)
    cam_tilt(w_dp, h_dp, alto_u) if camara == 'kit' else (cam_edif(w_dp, h_dp) if camara == 'edif' else cam_pie(w_dp, h_dp, zc))
    if camara == 'edif': meta['pie_dp'] = pie_en_imagen(scene.camera, w_dp, h_dp)
    suelo_kit(); nuevos0 = set(bpy.data.objects)
    construye()
    if camara == 'kit':
        raiz = raiz_kit()
        for o in [o for o in bpy.data.objects if o not in nuevos0 and o is not raiz]:
            if o.parent is None and o.type in ('MESH', 'CURVE', 'EMPTY'): o.parent = raiz
            if 'rd_' in o.name: o.scale.y *= CT                      # las esferas se ven redondas desde cualquier lado: cancelan el estirón de la raíz
    times['kit_pz_' + nombre] = render('kit_pz_' + nombre, set(visibles))
    for o in [o for o in bpy.data.objects if o not in antes]: bpy.data.objects.remove(o, do_unlink=True)
    META[nombre] = dict(lienzo_dp=[w_dp, h_dp], **meta)


# ---------------------------------------------------------------------------------------------------- 3.1 canal · 3.2 churro · 3.3 bolita
def canal(crema):
    """El canal (la zanja de la barra de avance), nine-slice horizontal: un piso hundido y un labio de churro alrededor; el labio le tira su sombra interior. Sobre losa crema o sobre el cielo."""
    W, H = 60.0, 12.0; r = 1.5
    labio, piso = (KIT_CREMA, '#E6D6B4') if crema else ('#34337F', '#1C1B52')
    losa_perfil(estadio(W - 2.0, H - 2.0), 1.2, 0.5, 'pz_canal_piso').data.materials.append(mk(piso))
    tubo([(dp(x), dp(y), dp(r)) for (x, y) in estadio(W - 2 * r, H - 2 * r, 24)], dp(r), labio, cerrado=True, name='pz_canal_labio', res=10)


def churro_p(color):
    """El churro (el relleno de la barra), nine-slice horizontal: un cilindro de plasticina con las puntas redondas; tapa de 6 dp a cada lado."""
    W, H = 60.0, 8.0; r = dp(H) / 2; m = mk(color); a = dp(W - H) / 2
    tubo([(-a, 0, r), (a, 0, r)], r, color, name='pz_churro', res=12)
    for sx in (-1, 1):
        t = sphere(r=r, seg=40, loc=(sx * a, 0, r), coll='iconos', name='pz_churro_tapa'); mat(t, m)


def bolita():
    r = dp(10.0) / 2
    o = sphere(r=r, seg=48, loc=(0, 0, r), coll='iconos', name='pz_rd_bolita'); organic(o, 0.002, 1.4, 1); mat(o, mk(LIMA))


# ---------------------------------------------------------------------------------------------------- la llama de la racha (Tarea 148, P4)
def llama(encendida):
    """La llama de la racha: una GOTA de plasticina con la punta hacia arriba (la gota es símbolo aprobado, `docs/simbolos-neutros.md`; nada de llama con varias puntas que parezca una estrella). 24 dp.
    Encendida: sol, con un corazón claro; apagada: crema lisa, sin corazón. Sustituye al ícono `Whatshot` de Material en Hoy, Avance, el Perfil y el resumen de la ruta."""
    cuerpo = SOL if encendida else CREMA
    esf(0.46, (0, -0.2, 0.46), cuerpo, 'pz_rd_llama', seg=56)                       # la misma gota de `em_stroop` a 0,62 de su tamaño: 19 dp de ancho y ~26 de alto en el lienzo de 32 dp
    p = cono(0.39, 0.045, 0.78, loc=(0, 0.27, 0.3), rot=(-90, 0, 0), coll='iconos', name='pz_llama_punta'); organic(p, 0.003, 1.2, 1); mat(p, mk(cuerpo))
    if encendida:                                                                    # el corazón: otra gota más chica y más clara, echada sobre la grande (la llama encendida tiene luz por dentro)
        esf(0.24, (0, -0.27, 0.84), '#FFF0B4', 'pz_brillo', sz=0.55)
        c = cono(0.2, 0.025, 0.42, loc=(0, -0.05, 0.8), rot=(-90, 0, 0), coll='iconos', name='pz_brillo_punta'); c.scale.z = 0.55; mat(c, mk('#FFF0B4'))


# ---------------------------------------------------------------------------------------------------- 3.4 los 17 emblemas (ficha de 64 dp, objeto al ~70 %)
def ficha(area):
    """La ficha cuadrada redondeada del pastel del área (64 dp, radio 14, 6 dp de alto); devuelve su altura en unidades."""
    h = dp(6.0); d = (64.0 - 6.0 * ST)                                              # lo que ocupa la CARA en la imagen (el canto de adelante suma 6·sen)
    o = cushion(dp(64.0), dp(d), h, r_plan=dp(14.0), bev=min(h * 0.72, dp(14.0) * 0.6), seg=8, name='pz_ficha', coll='iconos', barrel=0.012, rings=2); organic(o, 0.003, 1.4, 1)
    mat(o, mk(PASTEL[area])); return h


def emblema_en_ficha(fn, area, esc):
    """La ficha y, encima, el objeto del juego (achicado por `esc` desde el centro de la ficha, apoyado en ella)."""
    z = ficha(area); antes = set(bpy.data.objects); fn(z)
    if esc != 1.0:
        emb = bpy.data.objects.new('pz_emblema', None); scene.collection.objects.link(emb); emb.scale = (esc, esc, esc); emb.location = (0, 0, z * (1 - esc))
        for o in [o for o in bpy.data.objects if o not in antes and o is not emb and o.parent is None]: o.parent = emb


def esf(r, loc, color, name='pz_rd_esfera', sz=1.0, seg=40):
    o = sphere(r=r, seg=seg, loc=loc, coll='iconos', name=name); o.scale = (1, 1, sz); organic(o, 0.003, 1.2, 1); mat(o, mk(color)); return o


def hilo(p0, p1, r, color):
    return churro(p0, p1, r, mk(color), coll='iconos', name='pz_hilo')


def em_parejas(z):          # dos luceros redondos unidos por un hilo
    a, b_ = (-0.62, -0.42), (0.62, 0.42)
    hilo((a[0], a[1], z + 0.1), (b_[0], b_[1], z + 0.1), 0.07, TINTA)
    esf(0.5, (a[0], a[1], z + 0.5), CREMA, 'pz_rd_lucero'); esf(0.5, (b_[0], b_[1], z + 0.5), SOL, 'pz_rd_lucero')


def em_secuencia(z):        # una luz dorada redonda con su estela corta
    for (x, y, r, c) in ((-0.98, -0.86, 0.16, '#FFE9A8'), (-0.66, -0.56, 0.26, '#FFDD7A'), (-0.24, -0.2, 0.4, '#FFD152')): esf(r, (x, y, z + r), c, 'pz_rd_estela')
    esf(0.62, (0.36, 0.3, z + 0.62), SOL, 'pz_rd_luz'); esf(0.17, (0.16, 0.52, z + 1.16), '#FFF3C4', 'pz_brillo', sz=0.5)


def em_bodega(z):           # caja de carga con cinchas y escotilla
    caja = box((1.9, 1.9, 0.8), loc=(0, 0, z + 0.4), coll='iconos', name='pz_caja'); redondo(caja, 0.16, 5); mat(caja, mk(TIERRA))
    for x in (-0.55, 0.55):
        t = box((0.26, 1.96, 0.86), loc=(x, 0, z + 0.43), coll='iconos', name='pz_cincha'); redondo(t, 0.07, 4); mat(t, mk(TIERRA_OSC))
    esc = cyl(r=0.34, d=0.18, verts=40, loc=(0, 0, z + 0.88), coll='iconos', name='pz_escotilla'); redondo(esc, 0.06, 4); mat(esc, mk(SOL))
    esf(0.1, (0, 0, z + 1.0), TINTA, 'pz_asa')


def em_correo(z):           # sobre crema con su solapa y un sello redondo
    s = box((2.1, 1.45, 0.3), loc=(0, -0.1, z + 0.15), coll='iconos', name='pz_sobre'); redondo(s, 0.12, 5); mat(s, mk(CREMA))
    f = losa_perfil(redondea([(-21.0, 0.0), (21.0, 0.0), (0.0, -15.0)], 0.3, 8), 2.6, 1.0, 'pz_solapa', z_dp=(z + 0.3) * KIT_DPU); f.location = (0, 0.5, f.location.z); mat(f, mk('#E9DCC2'))
    esf(0.24, (0, -0.24, z + 0.5), CORAL, 'pz_sello', sz=0.6)


def em_stroop(z):           # gota de tinta azul
    esf(0.74, (0, -0.3, z + 0.74), CIELO, 'pz_rd_gota', seg=56)
    p = cono(0.62, 0.07, 1.2, loc=(0, 0.44, z + 0.5), rot=(-90, 0, 0), coll='iconos', name='pz_gota_punta'); organic(p, 0.003, 1.2, 1); mat(p, mk(CIELO))
    esf(0.17, (-0.3, -0.52, z + 1.4), '#E6F8FF', 'pz_brillo', sz=0.45)


def em_piloto(z):           # cohete redondo (el de la barra, chico)
    cu = cyl(r=0.5, d=1.5, verts=48, loc=(0, -0.15, z + 0.5), rot=(90, 0, 0), coll='iconos', name='pz_cohete'); redondo(cu, 0.1, 5); mat(cu, mk(LILA))
    na = cono(0.5, 0.1, 0.8, loc=(0, 0.98, z + 0.5), rot=(-90, 0, 0), coll='iconos', name='pz_nariz'); organic(na, 0.003, 1.2, 1); mat(na, mk(CREMA))
    esf(0.23, (0, 0.2, z + 1.0), COL['window'], 'pz_ventana', sz=0.55)
    for sx in (-1, 1):
        a = box((0.5, 0.7, 0.14), loc=(sx * 0.62, -0.72, z + 0.2), rot=(0, 0, -sx * 20), coll='iconos', name='pz_aleta'); redondo(a, 0.06, 4); mat(a, mk(SOL))
    esf(0.27, (0, -1.18, z + 0.3), '#FFB84A', 'pz_llama', sz=0.7)


def em_freno(z):            # señal de ALTO octogonal con barra
    o = cyl(r=1.1, d=0.2, verts=8, loc=(0, 0, z + 0.1), rot=(0, 0, 22.5), coll='iconos', name='pz_alto_borde'); redondo(o, 0.1, 5, 20); mat(o, mk(CREMA))
    i = cyl(r=0.93, d=0.2, verts=8, loc=(0, 0, z + 0.28), rot=(0, 0, 22.5), coll='iconos', name='pz_alto'); redondo(i, 0.1, 5, 20); mat(i, mk(CORAL))
    b_ = box((1.2, 0.3, 0.14), loc=(0, 0, z + 0.45), coll='iconos', name='pz_alto_barra'); redondo(b_, 0.06, 4); mat(b_, mk(CREMA))


def em_satelites(z):        # satélite con dos paneles
    rot = 28
    cu = box((0.9, 0.9, 0.6), loc=(0, 0, z + 0.3), rot=(0, 0, rot), coll='iconos', name='pz_sat'); redondo(cu, 0.16, 5); mat(cu, mk(CIELO))
    for sx in (-1, 1):
        a = Vector((math.cos(math.radians(rot)) * sx, math.sin(math.radians(rot)) * sx, 0))
        br = box((0.5, 0.12, 0.1), loc=tuple(a * 0.7 + Vector((0, 0, z + 0.3))), rot=(0, 0, rot), coll='iconos', name='pz_brazo'); mat(br, mk(TINTA))
        pa = box((0.9, 0.62, 0.12), loc=tuple(a * 1.38 + Vector((0, 0, z + 0.3))), rot=(0, 0, rot), coll='iconos', name='pz_panel'); redondo(pa, 0.06, 4); mat(pa, mk(SOL if sx > 0 else CREMA))
    pl = cyl(r=0.22, d=0.18, verts=32, loc=(0, 0, z + 0.69), coll='iconos', name='pz_antena'); redondo(pl, 0.05, 4); mat(pl, mk(CORAL))


def em_radar(z):            # cápsula con ventana (Rescate relámpago)
    rot = 35
    cu = cyl(r=0.5, d=1.5, verts=48, loc=(0, 0, z + 0.5), rot=(90, 0, rot), coll='iconos', name='pz_capsula'); smooth(cu); mat(cu, mk(CIELO))
    d = Vector((-math.sin(math.radians(rot)), math.cos(math.radians(rot)), 0))
    for sgn in (-1, 1): esf(0.5, tuple(d * sgn * 0.75 + Vector((0, 0, z + 0.5))), CIELO, 'pz_rd_capsula_tapa', seg=48)
    v = cyl(r=0.27, d=0.14, verts=40, loc=(0, 0, z + 1.0), coll='iconos', name='pz_cap_ventana'); redondo(v, 0.04, 4); mat(v, mk(COL['window']))
    a = torus(R=0.29, r=0.07, loc=(0, 0, z + 1.03), coll='iconos', name='pz_cap_aro'); mat(a, mk(SOL))


def em_calculo(z):          # reactor cilíndrico con indicador
    c = cyl(r=0.98, d=0.55, verts=56, loc=(0, 0, z + 0.28), coll='iconos', name='pz_reactor'); redondo(c, 0.12, 5); mat(c, mk(CIELO))
    t = torus(R=0.62, r=0.12, loc=(0, 0, z + 0.62), coll='iconos', name='pz_reactor_aro'); mat(t, mk(SOL))
    n = cyl(r=0.34, d=0.2, verts=40, loc=(0, 0, z + 0.62), coll='iconos', name='pz_reactor_nucleo'); redondo(n, 0.05, 4); mat(n, mk(CREMA))
    esf(0.13, (0.66, -0.14, z + 0.88), CORAL, 'pz_indicador')


def em_engranajes(z):       # un engranaje de dientes romos
    R = 0.78; c = cyl(r=R, d=0.34, verts=56, loc=(0, 0, z + 0.17), coll='iconos', name='pz_engranaje'); redondo(c, 0.1, 5); mat(c, mk(SOL))
    for k in range(8):
        a = k * math.tau / 8
        d = box((0.42, 0.34, 0.34), loc=(R * 1.02 * math.cos(a), R * 1.02 * math.sin(a), z + 0.17), rot=(0, 0, math.degrees(a) + 90), coll='iconos', name='pz_diente'); redondo(d, 0.12, 5); mat(d, mk(SOL))
    h = cyl(r=0.3, d=0.4, verts=40, loc=(0, 0, z + 0.3), coll='iconos', name='pz_eje'); redondo(h, 0.06, 4); mat(h, mk(CIELO))


def em_acoplamiento(z):     # el módulo en L del juego (la almohada celeste), inclinado y entrando en su hueco de cordón verde agua
    def ele(e): return [(-18 - e, -21 - e), (18 + e, -21 - e), (18 + e, -8 + e), (-5 + e, -8 + e), (-5 + e, 21 + e), (-18 - e, 21 + e)]     # la L (dp), centrada; e = cuánto se agranda
    zd = z * KIT_DPU
    hueco = losa_perfil(redondea(ele(2.0), 0.26, 7), 1.0, 0.4, 'pz_hueco', z_dp=zd); mat(hueco, mk('#0B0D26'))
    cordon = [(dp(x), dp(y), dp(zd + 1.7)) for (x, y) in redondea(ele(3.6), 0.26, 7)]
    tubo(cordon, dp(1.8), '#4FD6B0', cerrado=True, name='pz_hueco_cordon', res=8)
    mod = losa_perfil(redondea(ele(-1.5), 0.3, 8), 7.0, 2.8, 'pz_modulo', z_dp=0); mat(mod, mk(CIELO))
    mod.location = (dp(7.0), dp(8.0), dp(zd + 8.0)); mod.rotation_euler = (math.radians(-6), math.radians(8), math.radians(12))


def em_aterrizaje(z):       # la nave posándose sobre la regla: tres patas gordas con sus pies, el domo y una barra con sus marcas
    r = box((2.3, 0.34, 0.12), loc=(0, -1.12, z + 0.06), coll='iconos', name='pz_regla'); redondo(r, 0.05, 4); mat(r, mk(CREMA))
    for k in range(-3, 4):
        t = box((0.07, 0.24 if k % 3 == 0 else 0.14, 0.05), loc=(k * 0.33, -1.12, z + 0.14), coll='iconos', name='pz_marca'); mat(t, mk(TINTA))
    cy = 0.28
    for a in (210, 330, 90):
        rad = math.radians(a); p0 = (math.cos(rad) * 0.3, cy + math.sin(rad) * 0.3, z + 0.6); p1 = (math.cos(rad) * 1.02, cy + math.sin(rad) * 1.02, z + 0.12)
        churro(p0, p1, 0.1, mk(TINTA), coll='iconos', name='pz_pata'); esf(0.21, (p1[0], p1[1], z + 0.13), SOL, 'pz_pie', sz=0.55)
    esf(0.66, (0, cy, z + 0.66), CREMA, 'pz_rd_nave', sz=0.85, seg=48); esf(0.28, (0, cy - 0.12, z + 1.14), CIELO, 'pz_nave_ventana', sz=0.55)


def em_anagramas(z):        # ficha SIN letra con tres puntos de relieve
    f = box((1.7, 1.7, 0.3), loc=(0, 0, z + 0.15), coll='iconos', name='pz_ficha_letra'); redondo(f, 0.3, 6); mat(f, mk(CREMA))
    for x in (-0.46, 0, 0.46): esf(0.17, (x, 0, z + 0.38), '#2F7E5C', 'pz_punto', sz=0.7)


def em_meteoros(z):         # meteorito redondo con estela
    for (x, y, r, c) in ((0.62, 0.64, 0.2, '#FFE9A8'), (1.0, 1.02, 0.14, '#FFDD7A'), (1.27, 1.3, 0.09, '#FFD152')): esf(r, (x, y, z + r), c, 'pz_rd_estela')
    m = sphere(r=0.78, seg=48, loc=(-0.3, -0.3, z + 0.78), coll='iconos', name='pz_rd_meteorito'); organic(m, 0.05, 0.9, 2); mat(m, mk('#D2CDEC'))
    for (x, y, r) in ((-0.5, -0.05, 0.2), (-0.05, -0.55, 0.17), (-0.7, -0.6, 0.12), (-0.28, 0.2, 0.1)): esf(r, (x, y, z + 1.4 - r * 0.4), '#9D95CC', 'pz_crater', sz=0.45)


def em_disparate(z):        # libro abierto
    lo = box((1.22, 1.64, 0.1), loc=(0, 0, z + 0.05), coll='iconos', name='pz_tapa'); redondo(lo, 0.06, 4); mat(lo, mk('#2F7E5C'))
    for sx, rot in ((-1, 5), (1, -5)):
        p = box((0.98, 1.45, 0.2), loc=(sx * 0.52, 0, z + 0.15), rot=(0, 0, rot), coll='iconos', name='pz_pagina'); redondo(p, 0.08, 5); mat(p, mk(CREMA))
        for j in (-0.4, 0.0, 0.4):
            l = box((0.62, 0.07, 0.04), loc=(sx * 0.52, j, z + 0.27), rot=(0, 0, rot), coll='iconos', name='pz_renglon'); mat(l, mk('#B5AEDC'))


def em_intrusa(z):          # el hexágono-figura del juego
    h = cyl(r=1.0, d=0.34, verts=6, loc=(0, 0, z + 0.17), rot=(0, 0, 30), coll='iconos', name='pz_hexagono'); redondo(h, 0.14, 5, 20); mat(h, mk(CIELO))
    i = cyl(r=0.55, d=0.2, verts=6, loc=(0, 0, z + 0.4), rot=(0, 0, 30), coll='iconos', name='pz_hexagono_centro'); redondo(i, 0.08, 4, 20); mat(i, mk(CREMA))


ESCALA = {'acoplamiento': 0.98, 'piloto': 0.84, 'satelites': 0.8, 'aterrizaje': 0.88, 'meteoros': 0.9, 'secuencia': 0.92, 'radar': 0.92, 'stroop': 0.95, 'parejas': 0.95, 'correo': 0.95}   # lo que se pasa del 70 % se achica


EMBLEMAS = (   # (id del juego, área, dibujo)
    ('parejas', 'mem', em_parejas), ('secuencia', 'mem', em_secuencia), ('bodega', 'mem', em_bodega), ('correo', 'mem', em_correo),
    ('stroop', 'ate', em_stroop), ('piloto', 'ate', em_piloto), ('freno', 'ate', em_freno), ('satelites', 'ate', em_satelites), ('radar', 'ate', em_radar),
    ('calculo', 'raz', em_calculo), ('engranajes', 'raz', em_engranajes), ('acoplamiento', 'raz', em_acoplamiento), ('aterrizaje', 'raz', em_aterrizaje),
    ('anagramas', 'len', em_anagramas), ('meteoros', 'len', em_meteoros), ('disparate', 'len', em_disparate), ('intrusa', 'len', em_intrusa),
)


# ---------------------------------------------------------------------------------------------------- 3.5 farola (la misma de la isla y del ícono de Avance de la barra)
def objetos_nuevos(fn):
    antes = set(bpy.data.objects); fn()
    return [o for o in bpy.data.objects if o not in antes]


def farola(encendida):
    objs = [o for o in objetos_nuevos(lambda: icono_avance_farol3(encendida)) if o.type in ('MESH', 'LIGHT')]
    if encendida:   # un núcleo brillante dentro del vidrio: a 22-28 dp se tiene que notar que está prendida (el resplandor lo da el compuesto)
        n = sphere(r=0.14, seg=24, loc=(0, 0, 0.33), coll='iconos', name='ic_nucleo')
        mat(n, clay('m_kitav_nucleo', '#FFE9A0', bump=0, sss=0, vary=0, emit='#FFC24A', emit_strength=8.0, sat=1.0, piece=0, chalk=0, sheen=0, prints=0)); objs.append(n)
    else:           # apagada: la bombilla y la vela del mismo gris-crema, sin amarillo
        for o in objs:
            if o.name.startswith('ic_luz') or o.name.startswith('ic_vela'): o.data.materials.clear(); mat(o, mk('#CDC6B6'))
    normaliza(objs, 1.5, 0.0)


# ---------------------------------------------------------------------------------------------------- los edificios de la isla, uno por área (Tarea 125: la grilla de Juegos y la ventana de un área; `area_<área>.webp` son los planetas planos de antes)
EDIFICIOS = {'memoria': ('Observatorio', observatorio, 90), 'atencion': ('Antena', antena, 0), 'razonamiento': ('Hangar', hangar, 0), 'lenguaje': ('Biblioteca', biblioteca, 0)}


def edificio(area):
    """El edificio de la isla de esa área, solo, con la luz y la sombra de contacto del kit (los mismos modelos de `build_island`, con el «hecho a mano» de la isla)."""
    nombre, fn, rz = EDIFICIOS[area]
    group(nombre, collect(fn), (0, 0), rz); handmade('edificios')


# ---------------------------------------------------------------------------------------------------- 3.6 pedestal del lugar
def pedestal():
    """150 × 56 dp: la losa crema de la isla sobre su base de tierra, con el canto de adelante a la vista; sostiene al edificio (con la farola encima si es «Sugerida»)."""
    d = 56.0 - 19.0 * ST
    tierra = cushion(dp(140.0), dp(d - 1.0), dp(12.0), r_plan=dp(21.5), bev=dp(4.0), seg=8, name='pz_pedestal_tierra', coll='iconos', barrel=0.015, rings=2); organic(tierra, 0.004, 1.4, 1)
    mat(tierra, clay('m_kitav_tierra', TIERRA, bump=0.6, sss=0.12, vary=0.05, rough=0.7, dents=0.5, dent_scale=2.6, grain=0.1, mid=0.5, ao=0.3, ao_cool=1.0, sat=1.0, piece=0, prints=0.1))
    losa = cushion(dp(150.0), dp(d), dp(7.0), r_plan=dp(22.5), bev=dp(3.6), seg=8, name='pz_pedestal_losa', coll='iconos', barrel=0.012, rings=2); organic(losa, 0.003, 1.4, 1)
    losa.location = (0, 0, dp(12.0)); mat(losa, clay('m_kitav_losa', LOSA, bump=0.45, sss=0.3, vary=0.03, rough=0.6, dents=0.7, dent_scale=2.0, grain=0.0, mid=0.3, ao=0.28, ao_cool=1.0, sat=1.0, piece=0, prints=0.1))


# ---------------------------------------------------------------------------------------------------- 3.7 cola del globo de Nubi (16 × 12 dp; una por lado)
def cola(izquierda):
    """La cola del globo: un triángulo muy redondo de la plasticina crema de la losa grande; el lado pegado al globo arriba y la punta hacia abajo (a la izquierda o a la derecha)."""
    s = 1 if izquierda else -1
    d = 12.0 - 5.0 * ST                                                          # lo que ocupa la cara; el canto de adelante suma el resto
    pts = redondea([(-8.0 * s, d / 2), (8.0 * s, d / 2), (-8.0 * s, -d / 2)], 0.32, 10)
    o = losa_perfil(pts, 5.0, 1.8, 'pz_cola'); mat(o, mk(CREMA))


# ---------------------------------------------------------------------------------------------------- 3.8 medallas (96 dp, la figura parada sobre su disco, con la cámara de los íconos)
FAMILIAS = {'constancia': CORAL, 'variedad': LILA, 'volver': CIELO}


def a_crudo(objs):
    """Plasticina cruda (medalla bloqueada): todo gris-crema, sin luz ni color."""
    for o in objs:
        if o.type == 'LIGHT': bpy.data.objects.remove(o, do_unlink=True); continue
        if o.type != 'MESH': continue
        o.data.materials.clear(); mat(o, mk(CRUDA_OSC if ('pata' in o.name or 'montante' in o.name) else CRUDA))


def medalla(familia, lograda):
    """Un disco de plasticina del color de la familia (la moneda, con su aro crema), la figura parada en el centro y una placa lisa abajo para el número (en Compose). Bloqueada: plasticina cruda, gris-crema, sin luz."""
    def c(col): return col if lograda else CRUDA
    antes = set(bpy.data.objects)
    h = dp(7.0); R = dp(31.0); centro = suelo_pie(0, -6, 3.5)                    # el centro del disco, a media altura
    disco = cyl(r=R, d=h, verts=72, loc=centro, coll='iconos', name='pz_moneda'); bevel(disco, h * 0.62, 6); organic(disco, 0.002, 1.4, 1); mat(disco, mk(c(FAMILIAS[familia])))
    aro = torus(R=R * 0.84, r=dp(2.1), loc=(centro[0], centro[1], h + dp(0.5)), coll='iconos', name='pz_moneda_aro'); mat(aro, mk(c(CREMA)))
    placa = losa_perfil(estadio(52.0, 16.0 / SE), 4.0, 1.8, 'pz_placa'); mat(placa, mk(c(CREMA)))            # acostada: con la inclinación de la cámara se ve de 52 × 16 dp
    placa.location = (0, dp((-37.0 - 4.0 * CE) / SE), 0)
    base = Vector((0, centro[1], h))                                            # el centro de la cara del disco
    if familia == 'constancia':
        figura = [o for o in objetos_nuevos(lambda: icono_avance_farol3(lograda)) if o.type in ('MESH', 'LIGHT')]
        normaliza(figura, dp(44.0), base.z)
        for o in figura: o.location = (o.location.x + base.x, o.location.y + base.y, o.location.z)
    elif familia == 'variedad':
        mi = bpy.data.objects.new('pz_mini_isla', None); scene.collection.objects.link(mi); mi.rotation_euler = (0, 0, math.radians(-45)); mi.scale = (0.7, 0.7, 0.7); mi.location = tuple(base)
        for r in mini_isla(): r.parent = mi
    else:
        nave_pz(base, lograda)
    if not lograda: a_crudo([o for o in bpy.data.objects if o not in antes])
    for o in [o for o in bpy.data.objects if o.type == 'LIGHT' and o.name.startswith('ant_')]: bpy.data.objects.remove(o, do_unlink=True)


def mini_isla():
    """Los cuatro edificios de la isla (los mismos de `build_island`) para la medalla de Variedad; devuelve sus raíces."""
    raices = []
    for nombre, fn, loc, rz in (('Observatorio', observatorio, (-P, -P), 90), ('Antena', antena, (-1.7, 0.1), 0), ('Biblioteca', biblioteca, (P, P), 0), ('Hangar', hangar, (P, -P), 0)):
        raices.append(group(nombre, collect(fn), loc, rz))
    return raices


def nave_pz(base, lograda):
    """La nave de Aterrizaje posándose: cuerpo redondo, ventana, cuatro patas con sus pies; cuelga un poco sobre el disco."""
    def c(col): return col if lograda else CRUDA
    x0, y0, z0 = base.x, base.y, base.z + dp(4.0)
    n = sphere(r=dp(11.0), seg=48, loc=(x0, y0, z0 + dp(18.0)), scale=(1, 1, 0.85), coll='iconos', name='pz_nave'); organic(n, 0.003, 1.2, 1); mat(n, mk(c(CREMA)))
    v = sphere(r=dp(4.4), seg=28, loc=(x0, y0 - dp(8.6), z0 + dp(20.5)), scale=(1, 0.5, 1), coll='iconos', name='pz_nave_ventana'); mat(v, mk(c(CIELO)))
    for a in (45, 135, 225, 315):
        r = math.radians(a); p0 = (x0 + dp(7.0) * math.cos(r), y0 + dp(7.0) * math.sin(r), z0 + dp(13.0)); p1 = (x0 + dp(16.0) * math.cos(r), y0 + dp(16.0) * math.sin(r), z0 + dp(2.4))
        churro(p0, p1, dp(1.7), mk(c(TINTA)), coll='iconos', name='pz_pata')
        pie = sphere(r=dp(3.6), seg=24, loc=p1, scale=(1, 1, 0.55), coll='iconos', name='pz_pie'); mat(pie, mk(c(SOL)))


# ---------------------------------------------------------------------------------------------------- 3.9 escudo de liga (120 dp), 7 colores mate
ESCUDO = {'bronce': ('#F3B27A', '#CD7F32', '#8A4B14'), 'plata': ('#F4F6FA', '#B7BFCC', '#6E7787'), 'oro': ('#FFF0A8', '#F5B301', '#B37400'), 'platino': ('#E6FBFF', '#5FD3E6', '#1B8CA3'),
          'esmeralda': ('#B9FFD9', '#17C97A', '#07683E'), 'diamante': ('#DCEBFF', '#4C8DFF', '#1B3FB3'), 'maestro': ('#F2D4FF', '#B04DFF', '#5B1B99')}


def contorno_escudo(w, h, inset, n=14):
    """El contorno de `shieldPath` (LeagueShield.kt): arriba curvo, lados rectos hasta la mitad y la punta redonda abajo. En dp, centrado, +Y arriba."""
    l, r, t, b = inset, w - inset, inset, h - inset; cx = w / 2
    def bez(p0, p1, p2, p3, k):
        u = 1 - k; return (u**3 * p0[0] + 3 * u * u * k * p1[0] + 3 * u * k * k * p2[0] + k**3 * p3[0], u**3 * p0[1] + 3 * u * u * k * p1[1] + 3 * u * k * k * p2[1] + k**3 * p3[1])
    H = b - t; R = r - cx
    segs = [
        ('b', (cx, t), (cx + R * 0.35, t + H * 0.05), (r - R * 0.25, t + H * 0.06), (r, t + H * 0.12)),
        ('l', (r, t + H * 0.12)),
        ('b', (r, t + H * 0.50), (r, t + H * 0.76), (cx + R * 0.55, t + H * 0.90), (cx, b)),
        ('b', (cx, b), (cx - R * 0.55, t + H * 0.90), (l, t + H * 0.76), (l, t + H * 0.50)),
        ('l', (l, t + H * 0.50)),
        ('b', (l, t + H * 0.12), (l + R * 0.25, t + H * 0.06), (cx - R * 0.35, t + H * 0.05), (cx, t)),
    ]
    pts = []
    for s in segs:
        if s[0] == 'l': pts.append(s[1])
        else:
            for k in range(n): pts.append(bez(s[1], s[2], s[3], s[4], k / n))
    return [(x - cx, -(y - h / 2)) for (x, y) in pts]


def escudo(nombre):
    claro, base, oscuro = ESCUDO[nombre]
    W = 108.0; Hh = 121.0 - 13.0 * ST
    borde = losa_perfil(contorno_escudo(W, Hh, 0.0), 7.0, 3.0, 'pz_escudo_borde'); mat(borde, mate(oscuro))
    cara = losa_perfil(contorno_escudo(W, Hh, W * 0.075), 4.5, 2.4, 'pz_escudo_cara', z_dp=5.0); mat(cara, mate(base))
    centro = losa_perfil(contorno_escudo(W, Hh, W * 0.2), 3.0, 1.8, 'pz_escudo_centro', z_dp=8.5); mat(centro, mate(claro))


# ---------------------------------------------------------------------------------------------------- el programa
def render_kit_avance():
    scene.view_settings.exposure = float(arg('--kit-exp', '-0.42'))
    una('canal_crema', 72.0, 24.0, lambda: canal(True), nine_slice='horizontal', cuerpo_dp=[60, 12], cuerpo_minimo_dp=24, radio_dp=6)
    una('canal_cielo', 72.0, 24.0, lambda: canal(False), nine_slice='horizontal', cuerpo_dp=[60, 12], cuerpo_minimo_dp=24, radio_dp=6)
    una('churro_sol', 72.0, 20.0, lambda: churro_p(SOL), nine_slice='horizontal', cuerpo_dp=[60, 8], tapa_dp=6, radio_dp=4)
    una('churro_lima', 72.0, 20.0, lambda: churro_p(LIMA), nine_slice='horizontal', cuerpo_dp=[60, 8], tapa_dp=6, radio_dp=4)
    una('bolita_lima', 20.0, 20.0, bolita, cuerpo_dp=[10, 10])
    una('llama_encendida', 32.0, 32.0, lambda: llama(True), alto_u=0.5, cuerpo_dp=[19, 26])
    una('llama_apagada', 32.0, 32.0, lambda: llama(False), alto_u=0.5, cuerpo_dp=[19, 26])
    for (gid, area, fn) in EMBLEMAS:
        def hacer(fn=fn, area=area, esc=ESCALA.get(gid, 1.0)): emblema_en_ficha(fn, area, esc)
        una('emblema_' + gid, 72.0, 72.0, hacer, alto_u=0.9, juego=gid, area=area, cuerpo_dp=[64, 64], usos_dp=[64, 56, 48, 40, 30])
    una('farola_apagada', 36.0, 36.0, lambda: farola(False), camara='pie', zc=0.69, usos_dp=[28, 22])
    una('farola_encendida', 36.0, 36.0, lambda: farola(True), camara='pie', zc=0.69, usos_dp=[28, 22])
    una('pedestal', 162.0, 70.0, pedestal, alto_u=0.75, cuerpo_dp=[150, 56])
    for area in EDIFICIOS:
        una('edificio_' + area, 144.0, 144.0, lambda a=area: edificio(a), camara='edif', visibles=('iconos', 'luces', 'edificios'), area=area)
    una('cola_izquierda', 28.0, 24.0, lambda: cola(True), alto_u=0.25, cuerpo_dp=[16, 12], nota='la punta hacia abajo a la izquierda')
    una('cola_derecha', 28.0, 24.0, lambda: cola(False), alto_u=0.25, cuerpo_dp=[16, 12], nota='la punta hacia abajo a la derecha')
    for fam in FAMILIAS:
        for lograda in (False, True):
            una('medalla_%s_%s' % (fam, 'conseguida' if lograda else 'bloqueada'), 96.0, 96.0, lambda f=fam, l=lograda: medalla(f, l), camara='pie', zc=0.0,
                visibles=('iconos', 'luces', 'edificios'), usos_dp=[56, 96], placa_numero_dp=[52, 16])
    for nombre in ESCUDO:
        una('escudo_' + nombre, 120.0, 134.0, lambda n=nombre: escudo(n), alto_u=0.65, cuerpo_dp=[108, 121], usos_dp=[40, 96, 120])
    ruta = os.path.join(OUT, 'kit_avance.json'); previo = {}
    if os.path.exists(ruta):
        try: previo = json.load(open(ruta, encoding='utf-8')).get('piezas', {})
        except Exception: previo = {}
    previo.update(META)
    with open(ruta, 'w', encoding='utf-8') as f: json.dump({'px_por_dp': KIT_PX, 'piezas': previo}, f, ensure_ascii=False, indent=1)
