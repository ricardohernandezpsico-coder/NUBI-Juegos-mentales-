# -*- coding: utf-8 -*-
"""
La isla de Nubi en 3D (versión vigente: ronda 26b, 11-oct-2026): pocas piezas, gordas y redondeadas; Nubi con lóbulos simples y cara
lisa; torre de celosía sin plato; luz más clara; edificios con silueta de juguete. Blender 4.5, solo herramientas de Blender.
Uso: blender -b -P isla.py -- [--engine EEVEE|CYCLES] [--samples N] [--width 1080] [--out DIR] [--layers] [--json] [--solo-nubi --mirada X] [--perfil] [--tablero --lamina A|B [--sin-luceros | --solo-luceros] [--b-aprobado]] [--nubi hola|descansa]
Ver tools/isla/README.md y tools/isla/render.sh.
"""
import bpy, bmesh, math, os, sys, time, random
from mathutils import Vector

args = sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
def arg(n, d): return args[args.index(n)+1] if n in args else d
ENGINE = arg('--engine','EEVEE').upper(); SAMPLES = int(arg('--samples','48')); WIDTH = int(arg('--width','1080'))
OUT = os.path.abspath(arg('--out', os.path.join(os.path.dirname(os.path.abspath(__file__)),'out'))); LAYERS = '--layers' in args; POSE = arg('--nubi','hola')
JSON = '--json' in args; NORENDER = '--no-render' in args; ICONOS = '--iconos' in args; REPISA = '--repisa' in args; ALTERNATIVAS = '--alternativas' in args
LAMINA = arg('--lamina','A').upper(); MIRADA = arg('--mirada','frente'); SOLO_NUBI = '--solo-nubi' in args; FONDO = '--fondo' in args; SOLO_EDIFICIOS = '--solo-edificios' in args; PERFIL = '--perfil' in args; TABLERO = '--tablero' in args; KIT = '--kit' in args; BARRA = '--barra' in args
os.makedirs(OUT, exist_ok=True); random.seed(5)

def hx(h): h=h.lstrip('#'); return tuple(int(h[i:i+2],16)/255 for i in (0,2,4))
def lin(c): return tuple((v/12.92) if v<=0.04045 else ((v+0.055)/1.055)**2.4 for v in c)
COL = {'cream':'#F6EAD2','creamDk':'#E8D6B2','tan':'#DDB782','tanDk':'#C09560','ink':'#2B2451','sun':'#FFD36B','sunLt':'#FFEDB8',
 'leaf':'#93CC82','leafL':'#CDF2B6','trunk':'#B08A5A','rock':'#D2CDEC','water':'#A9D8FF','stone':'#F2E6CC','dot':'#FFD86A',
 'mem':'#8FC0F5','memDk':'#3E6FBF','ate':'#F5B672','ateDk':'#B5631F','raz':'#BDA7F2','razDk':'#6A55B8','len':'#8FD9B0','lenDk':'#2F7E5C',
 'nubi':'#CBB8F4','nubiBlue':'#9FCBF7','rose':'#F6AFC4','glint':'#FFFFFF','window':'#FFE8A8','smoke':'#EFE7D6'}

scene = bpy.context.scene
for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)
for n in ('isla','edificios','nubi','luces','iconos'):
    c = bpy.data.collections.new(n); scene.collection.children.link(c)
C = {c.name: c for c in scene.collection.children}
def link(obj, coll):
    for c in obj.users_collection: c.objects.unlink(obj)
    C[coll].objects.link(obj)

# ------------------------------------------------------------------ plasticina
_m = {}
def clay(name, color, rough=0.5, bump=0.5, sss=0.25, emit=None, vary=0.045, coat=0.0, sheen=0.08, spots=None, emit_strength=2.5, spec=0.3, dents=0.0, dent_scale=4.5, grain=None, mid=0.35, dent_fine=0.45, spot_noise=0.0, spot_mix=1.0, spot_soft=0.72, fade=None, ao=0.0, ao_dist=0.35, veta=0.0, ao_cool=0.85, sat=1.06, piece=0.04, prints=0.25, chalk=0.0):
    # ronda 26: plasticina real = colores tizosos (sat 0,95, chalk mezcla un 8 % con gris cálido), brillo de cera (rugosidad 0,5, sheen 0,22), jaspeado ±vary; huellas de pulgar como blobs suaves, huellas dactilares (prints) en 1-2 zonas, casi nada de grano fino
    key=(name,color,rough,bump,sss,emit,vary,coat,sheen,spots,emit_strength,spec,dents,dent_scale,grain,mid,dent_fine,spot_noise,spot_mix,spot_soft,fade,ao,ao_dist,veta,ao_cool,sat,piece,prints,chalk)
    if key in _m: return _m[key]
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree; N = nt.nodes; L = nt.links
    b = N.get('Principled BSDF'); base = lin(hx(color))
    b.inputs['Base Color'].default_value = (*base,1); b.inputs['Roughness'].default_value = rough
    if 'Specular IOR Level' in b.inputs: b.inputs['Specular IOR Level'].default_value = spec
    if sheen and 'Sheen Weight' in b.inputs: b.inputs['Sheen Weight'].default_value = sheen
    if coat and 'Coat Weight' in b.inputs: b.inputs['Coat Weight'].default_value = coat; b.inputs['Coat Roughness'].default_value = 0.05
    if sss and 'Subsurface Weight' in b.inputs:
        b.inputs['Subsurface Weight'].default_value = sss; b.inputs['Subsurface Radius'].default_value = (0.5,0.3,0.18)
        if 'Subsurface Scale' in b.inputs: b.inputs['Subsurface Scale'].default_value = 0.12
    if emit: b.inputs['Emission Color'].default_value = (*lin(hx(emit)),1); b.inputs['Emission Strength'].default_value = emit_strength
    color_out = None
    if vary:   # jaspeado leve: vetas alargadas de tono y valor, como masa mal mezclada (coordenadas de objeto estiradas)
        tcj = N.new('ShaderNodeTexCoord'); mpj = N.new('ShaderNodeMapping'); mpj.inputs['Scale'].default_value = (1.0, 2.4, 1.3); L.new(tcj.outputs['Object'], mpj.inputs['Vector'])
        nz = N.new('ShaderNodeTexNoise'); nz.inputs['Scale'].default_value = 2.8; nz.inputs['Detail'].default_value = 2.5; nz.inputs['Roughness'].default_value = 0.55; L.new(mpj.outputs['Vector'], nz.inputs['Vector'])
        mr = N.new('ShaderNodeMapRange'); mr.inputs['From Min'].default_value = 0.35; mr.inputs['From Max'].default_value = 0.65
        mr.inputs['To Min'].default_value = 1-vary; mr.inputs['To Max'].default_value = 1+vary
        mh2 = N.new('ShaderNodeMapRange'); mh2.inputs['From Min'].default_value = 0.35; mh2.inputs['From Max'].default_value = 0.65; mh2.inputs['To Min'].default_value = 0.5 - vary*0.25; mh2.inputs['To Max'].default_value = 0.5 + vary*0.25
        hs = N.new('ShaderNodeHueSaturation'); hs.inputs['Color'].default_value = (*base,1)
        L.new(nz.outputs['Fac'], mr.inputs['Value']); L.new(mr.outputs['Result'], hs.inputs['Value']); L.new(nz.outputs['Fac'], mh2.inputs['Value']); L.new(mh2.outputs['Result'], hs.inputs['Hue']); color_out = hs.outputs['Color']
    if spots:  # manchas pintadas al ras: esferas de influencia fijas en coordenadas locales (color, radio, [(x,y,z,r),...])
        tc = N.new('ShaderNodeTexCoord'); acc = None
        irr = None
        if spot_noise:  # borde irregular: la distancia a cada centro se perturba con ruido (parches, no círculos)
            nn = N.new('ShaderNodeTexNoise'); nn.inputs['Scale'].default_value = 5.0; nn.inputs['Detail'].default_value = 3; nn.inputs['Roughness'].default_value = 0.6; L.new(tc.outputs['Object'], nn.inputs['Vector'])
            irr = N.new('ShaderNodeMath'); irr.operation = 'MULTIPLY_ADD'; irr.inputs[1].default_value = spot_noise; irr.inputs[2].default_value = -spot_noise*0.5; L.new(nn.outputs['Fac'], irr.inputs[0])
        for (cx,cy,cz,cr) in spots[1]:
            sub = N.new('ShaderNodeVectorMath'); sub.operation = 'SUBTRACT'; sub.inputs[1].default_value = (cx,cy,cz); L.new(tc.outputs['Object'], sub.inputs[0])
            ln = N.new('ShaderNodeVectorMath'); ln.operation = 'LENGTH'; L.new(sub.outputs['Vector'], ln.inputs[0])
            dist = ln.outputs['Value']
            if irr is not None:
                pd = N.new('ShaderNodeMath'); pd.operation = 'ADD'; L.new(dist, pd.inputs[0]); L.new(irr.outputs[0], pd.inputs[1]); dist = pd.outputs[0]
            lt = N.new('ShaderNodeMapRange'); lt.interpolation_type = 'SMOOTHSTEP'; lt.inputs['From Min'].default_value = cr*spot_soft; lt.inputs['From Max'].default_value = cr
            lt.inputs['To Min'].default_value = 1.0; lt.inputs['To Max'].default_value = 0.0; L.new(dist, lt.inputs['Value'])
            if acc is None: acc = lt
            else:
                mx_ = N.new('ShaderNodeMath'); mx_.operation = 'MAXIMUM'; L.new(acc.outputs[0], mx_.inputs[0]); L.new(lt.outputs[0], mx_.inputs[1]); acc = mx_
        mx = N.new('ShaderNodeMixRGB'); mx.inputs['Color2'].default_value = (*lin(hx(spots[0])),1)
        if color_out: L.new(color_out, mx.inputs['Color1'])
        else: mx.inputs['Color1'].default_value = (*base,1)
        if spot_mix != 1.0:
            sm = N.new('ShaderNodeMath'); sm.operation = 'MULTIPLY'; sm.inputs[1].default_value = spot_mix; L.new(acc.outputs[0], sm.inputs[0]); acc = sm
        L.new(acc.outputs[0], mx.inputs['Fac']); color_out = mx.outputs['Color']
    if fade:  # degradado radial corto en el borde (coordenadas locales XY): el color se funde con fade[2] entre los radios fade[0] y fade[1]
        tcf = N.new('ShaderNodeTexCoord'); sxf = N.new('ShaderNodeSeparateXYZ'); L.new(tcf.outputs['Object'], sxf.inputs['Vector'])
        cz = N.new('ShaderNodeCombineXYZ'); L.new(sxf.outputs['X'], cz.inputs['X']); L.new(sxf.outputs['Y'], cz.inputs['Y']); cz.inputs['Z'].default_value = 0.0
        lnf = N.new('ShaderNodeVectorMath'); lnf.operation = 'LENGTH'; L.new(cz.outputs['Vector'], lnf.inputs[0])
        mrf = N.new('ShaderNodeMapRange'); mrf.interpolation_type = 'SMOOTHSTEP'; mrf.inputs['From Min'].default_value = fade[0]; mrf.inputs['From Max'].default_value = fade[1]
        L.new(lnf.outputs['Value'], mrf.inputs['Value'])
        mxf = N.new('ShaderNodeMixRGB'); mxf.inputs['Color2'].default_value = (*lin(hx(fade[2])),1)
        if color_out: L.new(color_out, mxf.inputs['Color1'])
        else: mxf.inputs['Color1'].default_value = (*base,1)
        L.new(mrf.outputs['Result'], mxf.inputs['Fac']); color_out = mxf.outputs['Color']
    if ao:  # oclusión ambiental: en uniones, aleros y contactos el color se oscurece y se enfría un poco
        aon = N.new('ShaderNodeAmbientOcclusion'); aon.inputs['Distance'].default_value = ao_dist; aon.samples = 4; aon.inside = False; aon.only_local = False
        dkm = N.new('ShaderNodeMapRange'); dkm.inputs['From Min'].default_value = 0.0; dkm.inputs['From Max'].default_value = 1.0; dkm.inputs['To Min'].default_value = 1.0 - ao; dkm.inputs['To Max'].default_value = 1.0
        L.new(aon.outputs['AO'], dkm.inputs['Value'])
        sh = N.new('ShaderNodeMixRGB'); sh.blend_type = 'MULTIPLY'; sh.inputs['Fac'].default_value = 1.0
        if color_out: L.new(color_out, sh.inputs['Color1'])
        else: sh.inputs['Color1'].default_value = (*base,1)
        cc = N.new('ShaderNodeCombineColor'); L.new(dkm.outputs['Result'], cc.inputs[0]); L.new(dkm.outputs['Result'], cc.inputs[1])
        bl = N.new('ShaderNodeMath'); bl.operation = 'MULTIPLY_ADD'; bl.inputs[1].default_value = ao_cool; bl.inputs[2].default_value = 1.0 - ao_cool; L.new(dkm.outputs['Result'], bl.inputs[0]); L.new(bl.outputs[0], cc.inputs[2])   # la sombra tira a frío
        L.new(cc.outputs[0], sh.inputs['Color2']); color_out = sh.outputs['Color']
    if piece or sat != 1.0:  # hecho a mano: cada pieza con su tono y valor (±piece) y pastel algo más saturado
        oi = N.new('ShaderNodeObjectInfo')
        mv = N.new('ShaderNodeMapRange'); mv.inputs['To Min'].default_value = 1 - piece; mv.inputs['To Max'].default_value = 1 + piece; L.new(oi.outputs['Random'], mv.inputs['Value'])
        mh = N.new('ShaderNodeMath'); mh.operation = 'MULTIPLY_ADD'; mh.inputs[1].default_value = piece * 0.35; mh.inputs[2].default_value = 0.5 - piece * 0.175; L.new(oi.outputs['Random'], mh.inputs[0])
        hp = N.new('ShaderNodeHueSaturation'); hp.inputs['Saturation'].default_value = sat; L.new(mv.outputs['Result'], hp.inputs['Value']); L.new(mh.outputs[0], hp.inputs['Hue'])
        if color_out: L.new(color_out, hp.inputs['Color'])
        else: hp.inputs['Color'].default_value = (*base,1)
        color_out = hp.outputs['Color']
    if chalk:   # tizoso: un poco de gris cálido mezclado en el color (plasticina, no plástico)
        ch = N.new('ShaderNodeMixRGB'); ch.inputs['Fac'].default_value = chalk; ch.inputs['Color2'].default_value = (*lin(hx('#D9D0C2')),1)
        if color_out: L.new(color_out, ch.inputs['Color1'])
        else: ch.inputs['Color1'].default_value = (*base,1)
        color_out = ch.outputs['Color']
    if color_out: L.new(color_out, b.inputs['Base Color'])
    if bump:
        tco = N.new('ShaderNodeTexCoord')
        n1 = N.new('ShaderNodeTexNoise'); n1.inputs['Scale'].default_value = 60; n1.inputs['Detail'].default_value = 3; n1.inputs['Roughness'].default_value = 0.6   # grano fino: casi nada (antes delataba «ruido parejo»)
        n2 = N.new('ShaderNodeTexNoise'); n2.inputs['Scale'].default_value = 5; n2.inputs['Detail'].default_value = 1.5
        m1 = N.new('ShaderNodeMath'); m1.operation='MULTIPLY'; m1.inputs[1].default_value = grain if grain is not None else 0.05
        m2 = N.new('ShaderNodeMath'); m2.operation='MULTIPLY'; m2.inputs[1].default_value = mid
        ad = N.new('ShaderNodeMath'); ad.operation='ADD'; ad.use_clamp = True
        L.new(n1.outputs['Fac'], m1.inputs[0]); L.new(n2.outputs['Fac'], m2.inputs[0]); L.new(m1.outputs[0], ad.inputs[0]); L.new(m2.outputs[0], ad.inputs[1])
        height = ad
        if dents:  # huellas de pulgar: pocas, anchas y suaves, como blobs de ruido umbralizado (sin las aristas de celda de Voronoi, que se leían como facetas)
            for sc, wgt, lo_, hi_ in ((dent_scale*0.55, 1.0, 0.54, 0.68), (dent_scale*1.4, dent_fine, 0.56, 0.68)):
                nb = N.new('ShaderNodeTexNoise'); nb.inputs['Scale'].default_value = sc; nb.inputs['Detail'].default_value = 1.2; nb.inputs['Roughness'].default_value = 0.4; L.new(tco.outputs['Object'], nb.inputs['Vector'])
                st = N.new('ShaderNodeMapRange'); st.interpolation_type = 'SMOOTHSTEP'; st.inputs['From Min'].default_value = lo_; st.inputs['From Max'].default_value = hi_; L.new(nb.outputs['Fac'], st.inputs['Value'])
                md = N.new('ShaderNodeMath'); md.operation='MULTIPLY'; md.inputs[1].default_value = dents*wgt*0.5; L.new(st.outputs['Result'], md.inputs[0])
                ad2 = N.new('ShaderNodeMath'); ad2.operation='SUBTRACT'; L.new(height.outputs[0], ad2.inputs[0]); L.new(md.outputs[0], ad2.inputs[1]); height = ad2
        if prints:  # huellas dactilares: crestas finas concéntricas, solo en una o dos zonas de la pieza (máscara de ruido), centro distinto por pieza
            oi2 = N.new('ShaderNodeObjectInfo'); cz2 = N.new('ShaderNodeCombineXYZ'); mu2 = N.new('ShaderNodeMath'); mu2.operation = 'MULTIPLY'; mu2.inputs[1].default_value = 7.3; L.new(oi2.outputs['Random'], mu2.inputs[0])
            L.new(mu2.outputs[0], cz2.inputs['X']); L.new(oi2.outputs['Random'], cz2.inputs['Y']); cz2.inputs['Z'].default_value = 0.37
            va = N.new('ShaderNodeVectorMath'); va.operation = 'ADD'; L.new(tco.outputs['Object'], va.inputs[0]); L.new(cz2.outputs['Vector'], va.inputs[1])
            wv2 = N.new('ShaderNodeTexWave'); wv2.wave_type = 'RINGS'; wv2.rings_direction = 'SPHERICAL'; wv2.inputs['Scale'].default_value = 55; wv2.inputs['Distortion'].default_value = 1.6; wv2.inputs['Detail'].default_value = 0.5; L.new(va.outputs['Vector'], wv2.inputs['Vector'])
            nm2 = N.new('ShaderNodeTexNoise'); nm2.inputs['Scale'].default_value = 1.8; nm2.inputs['Detail'].default_value = 1.0; L.new(va.outputs['Vector'], nm2.inputs['Vector'])
            mk2 = N.new('ShaderNodeMapRange'); mk2.interpolation_type = 'SMOOTHSTEP'; mk2.inputs['From Min'].default_value = 0.57; mk2.inputs['From Max'].default_value = 0.67; L.new(nm2.outputs['Fac'], mk2.inputs['Value'])
            pr = N.new('ShaderNodeMath'); pr.operation = 'MULTIPLY'; L.new(wv2.outputs['Fac'], pr.inputs[0]); L.new(mk2.outputs['Result'], pr.inputs[1])
            pr2 = N.new('ShaderNodeMath'); pr2.operation = 'MULTIPLY'; pr2.inputs[1].default_value = prints*0.35; L.new(pr.outputs[0], pr2.inputs[0])
            ad3 = N.new('ShaderNodeMath'); ad3.operation='ADD'; L.new(height.outputs[0], ad3.inputs[0]); L.new(pr2.outputs[0], ad3.inputs[1]); height = ad3
        if veta:  # veta de madera suave: bandas onduladas a lo largo del eje local Z de la pieza
            tcv = N.new('ShaderNodeTexCoord'); wv = N.new('ShaderNodeTexWave'); wv.wave_type = 'BANDS'; wv.bands_direction = 'X'; wv.inputs['Scale'].default_value = 10; wv.inputs['Distortion'].default_value = 1.4; wv.inputs['Detail'].default_value = 1.2   # vetas a lo LARGO de la pieza (varían en X, corren por Z)
            L.new(tcv.outputs['Object'], wv.inputs['Vector']); mv = N.new('ShaderNodeMath'); mv.operation = 'MULTIPLY'; mv.inputs[1].default_value = veta; L.new(wv.outputs['Fac'], mv.inputs[0])
            adv = N.new('ShaderNodeMath'); adv.operation = 'ADD'; L.new(height.outputs[0], adv.inputs[0]); L.new(mv.outputs[0], adv.inputs[1]); height = adv
        bp = N.new('ShaderNodeBump'); bp.inputs['Strength'].default_value = bump; bp.inputs['Distance'].default_value = 0.03 + 0.025*dents
        L.new(height.outputs[0], bp.inputs['Height']); L.new(bp.outputs['Normal'], b.inputs['Normal'])
    _m[key] = m; return m
def nubi_material(spots, base='#C4B8E8', relief=0.009, soft=0.8, wobble=0.2, occl=0.1):   # ronda 26b (cierre): gotas de plasticina blandas: contorno de baja frecuencia (2-4 ondulaciones), canto redondo, sin costura, sombra de contacto muy suave   # ronda 26: borde limpio (soft 0,9) apenas ondulado, costura alrededor   # plasticina esculpida; manchas orgánicas al ras
    """Plasticina de dos colores amasada: por cada mancha (x,y,z,r,color) una máscara esférica suave con borde ondulado
       que cambia el color, empuja la superficie 'relief' (0,5 % del ancho de Nubi) con borde redondeado y oscurece un poco
       el borde inferior (oclusión). Coordenadas de objeto del cuerpo."""
    m = clay('m_nubi_base', base, bump=0.7, sss=0.3, sheen=0.08, vary=0.035, rough=0.55, spec=0.2, dents=1.0, dent_scale=2.2, grain=0.04, mid=0.5, ao=0.3, ao_dist=0.25, ao_cool=1.0, sat=1.0, piece=0, prints=0.25)   # ronda 26b: relieve de mano de vuelta (huellas anchas y ondulación), sin facetas   # sombra de contacto bajo discos, mejillas y boca   # plasticina mate sin pelusa: casi sin grano fino, huellas medias
    m = m.copy(); m.name = 'm_nubi'; nt = m.node_tree; N = nt.nodes; L = nt.links; b = N.get('Principled BSDF'); out = N.get('Material Output')
    # color de entrada actual del Principled (variación + huellas ya conectadas)
    color_in = b.inputs['Base Color'].links[0].from_socket if b.inputs['Base Color'].links else None
    tc = N.new('ShaderNodeTexCoord'); sx = N.new('ShaderNodeSeparateXYZ'); L.new(tc.outputs['Object'], sx.inputs['Vector'])
    nz = N.new('ShaderNodeTexNoise'); nz.inputs['Scale'].default_value = 1.3; nz.inputs['Detail'].default_value = 0.0; nz.inputs['Roughness'].default_value = 0.3; L.new(tc.outputs['Object'], nz.inputs['Vector'])   # ondulación de baja frecuencia: nunca dientes ni flecos
    wob = N.new('ShaderNodeMath'); wob.operation = 'MULTIPLY_ADD'; wob.inputs[1].default_value = wobble; wob.inputs[2].default_value = -wobble*0.5; L.new(nz.outputs['Fac'], wob.inputs[0])
    height = None; color = color_in; dark = None; seam = None
    for (cx,cy,cz,cr,col) in spots:
        sub = N.new('ShaderNodeVectorMath'); sub.operation = 'SUBTRACT'; sub.inputs[1].default_value = (cx,cy,cz); L.new(tc.outputs['Object'], sub.inputs[0])
        ln = N.new('ShaderNodeVectorMath'); ln.operation = 'LENGTH'; L.new(sub.outputs['Vector'], ln.inputs[0])
        pd = N.new('ShaderNodeMath'); pd.operation = 'ADD'; L.new(ln.outputs['Value'], pd.inputs[0]); L.new(wob.outputs[0], pd.inputs[1])   # distancia perturbada: borde ondulado
        mk = N.new('ShaderNodeMapRange'); mk.interpolation_type = 'SMOOTHSTEP'; mk.inputs['From Min'].default_value = cr*soft; mk.inputs['From Max'].default_value = cr
        mk.inputs['To Min'].default_value = 1.0; mk.inputs['To Max'].default_value = 0.0; L.new(pd.outputs[0], mk.inputs['Value'])
        mx = N.new('ShaderNodeMixRGB'); mx.inputs['Color2'].default_value = (*lin(hx(col)),1)
        if color is not None: L.new(color, mx.inputs['Color1'])
        else: mx.inputs['Color1'].default_value = (*lin(hx(base)),1)
        L.new(mk.outputs['Result'], mx.inputs['Fac']); color = mx.outputs['Color']
        if height is None: height = mk
        else:
            ad = N.new('ShaderNodeMath'); ad.operation = 'MAXIMUM'; L.new(height.outputs[0] if hasattr(height,'outputs') else height, ad.inputs[0]); L.new(mk.outputs['Result'], ad.inputs[1]); height = ad
        # oclusión: anillo del borde (entre 0,86 r y 1,06 r) solo en la mitad inferior de la mancha (z local menor que el centro)
        ring = N.new('ShaderNodeMapRange'); ring.interpolation_type = 'SMOOTHSTEP'; ring.inputs['From Min'].default_value = cr*0.86; ring.inputs['From Max'].default_value = cr*0.98; L.new(pd.outputs[0], ring.inputs['Value'])
        ring2 = N.new('ShaderNodeMapRange'); ring2.interpolation_type = 'SMOOTHSTEP'; ring2.inputs['From Min'].default_value = cr*0.98; ring2.inputs['From Max'].default_value = cr*1.10; ring2.inputs['To Min'].default_value = 1.0; ring2.inputs['To Max'].default_value = 0.0; L.new(pd.outputs[0], ring2.inputs['Value'])
        band = N.new('ShaderNodeMath'); band.operation = 'MULTIPLY'; L.new(ring.outputs['Result'], band.inputs[0]); L.new(ring2.outputs['Result'], band.inputs[1])
        below = N.new('ShaderNodeMapRange'); below.inputs['From Min'].default_value = cz; below.inputs['From Max'].default_value = cz - cr*0.6; below.inputs['To Min'].default_value = 0.0; below.inputs['To Max'].default_value = 1.0; L.new(sx.outputs['Z'], below.inputs['Value'])
        oc = N.new('ShaderNodeMath'); oc.operation = 'MULTIPLY'; L.new(band.outputs[0], oc.inputs[0]); L.new(below.outputs['Result'], oc.inputs[1])
        oc2 = N.new('ShaderNodeMath'); oc2.operation = 'MULTIPLY_ADD'; oc2.inputs[1].default_value = 0.0; L.new(band.outputs[0], oc2.inputs[0]); L.new(oc.outputs[0], oc2.inputs[2]); oc = oc2   # costura: todo el contorno se oscurece un poco, abajo más
        if seam is None: seam = band
        else:
            sm_ = N.new('ShaderNodeMath'); sm_.operation = 'MAXIMUM'; L.new(seam.outputs[0], sm_.inputs[0]); L.new(band.outputs[0], sm_.inputs[1]); seam = sm_
        if dark is None: dark = oc
        else:
            dd = N.new('ShaderNodeMath'); dd.operation = 'MAXIMUM'; L.new(dark.outputs[0], dd.inputs[0]); L.new(oc.outputs[0], dd.inputs[1]); dark = dd
    # oscurecer el color según la oclusión
    dk = N.new('ShaderNodeMath'); dk.operation = 'MULTIPLY_ADD'; dk.inputs[1].default_value = -occl; dk.inputs[2].default_value = 1.0; L.new(dark.outputs[0], dk.inputs[0])
    dmx = N.new('ShaderNodeMixRGB'); dmx.blend_type = 'MULTIPLY'; dmx.inputs['Fac'].default_value = 1.0
    L.new(color, dmx.inputs['Color1']); dkc = N.new('ShaderNodeCombineColor'); L.new(dk.outputs[0], dkc.inputs[0]); L.new(dk.outputs[0], dkc.inputs[1]); L.new(dk.outputs[0], dkc.inputs[2]); L.new(dkc.outputs[0], dmx.inputs['Color2'])
    L.new(dmx.outputs['Color'], b.inputs['Base Color'])
    # relieve mínimo con borde redondeado: desplazamiento real + el relieve de plasticina existente
    hs = N.new('ShaderNodeMath'); hs.operation = 'MULTIPLY'; hs.inputs[1].default_value = relief; L.new(height.outputs[0], hs.inputs[0])
    sg = N.new('ShaderNodeMath'); sg.operation = 'MULTIPLY'; sg.inputs[1].default_value = 0.0; L.new(seam.outputs[0], sg.inputs[0])   # costura: surco blando alrededor de cada mancha (la marca de haberla apretado)
    hs2 = N.new('ShaderNodeMath'); hs2.operation = 'SUBTRACT'; L.new(hs.outputs[0], hs2.inputs[0]); L.new(sg.outputs[0], hs2.inputs[1])
    disp = N.new('ShaderNodeDisplacement'); disp.inputs['Midlevel'].default_value = 0.0; disp.inputs['Scale'].default_value = 1.0; L.new(hs2.outputs[0], disp.inputs['Height'])
    L.new(disp.outputs['Displacement'], out.inputs['Displacement'])
    try: m.displacement_method = 'BOTH'
    except Exception:
        try: m.cycles.displacement_method = 'BOTH'
        except Exception: pass
    return m

def stripes(name, a, b_, scale=9, along='X'):
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree; N = nt.nodes; L = nt.links; b = N.get('Principled BSDF')
    wv = N.new('ShaderNodeTexWave'); wv.inputs['Scale'].default_value = scale; wv.wave_type = 'BANDS'; wv.bands_direction = along
    tcs = N.new('ShaderNodeTexCoord'); L.new(tcs.outputs['Object'], wv.inputs['Vector'])
    st = N.new('ShaderNodeMath'); st.operation = 'GREATER_THAN'; st.inputs[1].default_value = 0.5
    mx = N.new('ShaderNodeMixRGB'); mx.inputs['Color1'].default_value = (*lin(hx(a)),1); mx.inputs['Color2'].default_value = (*lin(hx(b_)),1)
    L.new(wv.outputs['Fac'], st.inputs[0]); L.new(st.outputs[0], mx.inputs['Fac']); L.new(mx.outputs['Color'], b.inputs['Base Color'])
    b.inputs['Roughness'].default_value = 0.6
    if 'Subsurface Weight' in b.inputs: b.inputs['Subsurface Weight'].default_value = 0.2
    return m
def water():
    m = bpy.data.materials.new('agua'); m.use_nodes = True; b = m.node_tree.nodes.get('Principled BSDF')
    b.inputs['Base Color'].default_value = (*lin(hx(COL['water'])),1); b.inputs['Roughness'].default_value = 0.05
    if 'Coat Weight' in b.inputs: b.inputs['Coat Weight'].default_value = 1.0
    return m

# ------------------------------------------------------------------ geometría
def smooth(o):
    for p in o.data.polygons: p.use_smooth = True
def bevel(o, w=0.06, seg=5, ang=60):
    md = o.modifiers.new('Bevel','BEVEL'); md.width = w; md.segments = seg; md.limit_method = 'ANGLE'; md.angle_limit = math.radians(ang); return md
def organic(o, strength=0.04, scale=0.9, levels=2):
    sd = o.modifiers.new('Sub','SUBSURF'); sd.levels = levels; sd.render_levels = levels
    tex = bpy.data.textures.new('bultos','CLOUDS'); tex.noise_scale = scale; tex.noise_depth = 1
    md = o.modifiers.new('Bultos','DISPLACE'); md.texture = tex; md.strength = strength; md.mid_level = 0.5; md.texture_coords = 'GLOBAL'
    smooth(o); return md
def mat(o, m): o.data.materials.append(m)
def place(o, loc=(0,0,0), rot=(0,0,0), scale=(1,1,1), coll='isla', name=None):
    o.location = loc; o.rotation_euler = tuple(math.radians(a) for a in rot); o.scale = scale
    if name: o.name = name
    link(o, coll); return o
def box(size=(1,1,1), **kw):
    bpy.ops.mesh.primitive_cube_add(size=1); o = bpy.context.object; o.scale = size; bpy.ops.object.transform_apply(scale=True); return place(o, **kw)
def cyl(r=0.5, d=1, verts=48, **kw):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=d); o = bpy.context.object; smooth(o); return place(o, **kw)
def sphere(r=0.5, seg=40, **kw):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, segments=seg, ring_count=max(8,seg//2)); o = bpy.context.object; smooth(o); return place(o, **kw)
def torus(R=0.5, r=0.1, **kw):
    bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=r, major_segments=48, minor_segments=16); o = bpy.context.object; smooth(o); return place(o, **kw)
def wedge(w, dd, h, **kw):
    me = bpy.data.meshes.new('wedge'); bm = bmesh.new()
    v = [bm.verts.new(p) for p in [(-w/2,-dd/2,0),(w/2,-dd/2,0),(w/2,dd/2,0),(-w/2,dd/2,0),(0,-dd/2,h),(0,dd/2,h)]]
    for f in [(0,1,2,3),(0,4,5,3),(1,2,5,4),(0,1,4),(3,2,5)]: bm.faces.new([v[i] for i in f])
    bm.normal_update(); bm.to_mesh(me); bm.free(); me.update()
    o = bpy.data.objects.new('wedge', me); scene.collection.objects.link(o); return place(o, **kw)
def blob(r=0.3, strength=0.1, **kw):
    o = sphere(r=r, seg=24, **kw); organic(o, strength=strength*r, scale=0.6, levels=1); return o
def churro(p0, p1, r, m, coll='edificios', name='churro', caps=True):
    """Rollito de plasticina entre dos puntos: cilindro con puntas redondeadas (casquetes)."""
    a, b_ = Vector(p0), Vector(p1); d = b_ - a
    o = cyl(r=r, d=d.length, verts=20, loc=tuple((a+b_)/2), coll=coll, name=name); o.rotation_euler = d.to_track_quat('Z','Y').to_euler(); mat(o, m)
    if caps:
        for pt in (a, b_):
            c = sphere(r=r, seg=16, loc=tuple(pt), coll=coll, name=name+'_punta'); mat(c, m)
    return o

HAND = random.Random(23)
def handmade(coll_name, skip=('obs_cortador',), bev=0.14, noise=0.015, rot=1.2, scl=0.02):
    """Hecho a mano: biseles gordos (bev × dimensión menor, ≥4 segmentos), ruido de baja frecuencia (noise × tamaño), giro leve y escala irregular por pieza."""
    for o in list(C[coll_name].objects):
        if o.type != 'MESH' or o.name.split('.')[0] in skip or o.hide_render: continue
        dims = [d for d in o.dimensions if d > 1e-4]
        if not dims: continue
        mn, mx = min(dims), max(dims)
        bv = [md for md in o.modifiers if md.type == 'BEVEL']
        if bv: bv[0].width = max(bv[0].width, bev*mn); bv[0].segments = max(bv[0].segments, 4)
        else:
            md = o.modifiers.new('Bevel','BEVEL'); md.width = bev*mn; md.segments = 4; md.limit_method = 'ANGLE'; md.angle_limit = math.radians(60)
            o.modifiers.move(len(o.modifiers)-1, 0)
        if not any(md.type == 'DISPLACE' for md in o.modifiers) and mx > 0.08:
            tex = bpy.data.textures.new('mano','CLOUDS'); tex.noise_scale = mx*1.2; tex.noise_depth = 0
            md = o.modifiers.new('Mano','DISPLACE'); md.texture = tex; md.strength = noise*mx; md.mid_level = 0.5; md.texture_coords = 'GLOBAL'
        o.rotation_euler = (o.rotation_euler.x + math.radians(HAND.uniform(-rot*0.6, rot*0.6)), o.rotation_euler.y + math.radians(HAND.uniform(-rot*0.6, rot*0.6)), o.rotation_euler.z + math.radians(HAND.uniform(-rot, rot)))
        f = HAND.uniform(1-scl, 1+scl); o.scale = (o.scale.x*f, o.scale.y*HAND.uniform(1-scl, 1+scl), o.scale.z*HAND.uniform(1-scl, 1+scl))

def lamp(x, y, coll='isla', h=0.4):
    post = cyl(r=0.065, d=h, verts=20, loc=(x,y,h/2), coll=coll, name='farola'); bevel(post,0.025,3); organic(post,0.006,0.4,1); mat(post, clay('m_tanDk', COL['tanDk'], bump=0.5, sss=0.1))
    cap = sphere(r=0.085, seg=20, loc=(x,y,h), scale=(1,1,0.5), coll=coll, name='farola_tapa'); mat(cap, clay('m_tanDk', COL['tanDk'], bump=0.4, sss=0.1))
    bulb = sphere(r=0.095, loc=(x,y,h+0.095), coll=coll, name='farola_luz'); mat(bulb, clay('m_bulb', COL['sunLt'], bump=0, sss=0, vary=0, emit='#FFB84A', emit_strength=1.6, chalk=0))
    Lt = bpy.data.lights.new('farola','POINT'); Lt.energy = 1.7; Lt.color = lin(hx('#FFB36A')); Lt.shadow_soft_size = 0.35   # ronda 26b: luz más cálida y anaranjada (~2700 K) y menos alcance; la amarilla #FFCF80 teñía la tapa de amarillo-verdoso
    lo = bpy.data.objects.new('farola_L', Lt); lo.location = (x,y,h+0.12); scene.collection.objects.link(lo); link(lo,'luces')
def window(x,y,z,w=0.24,h=0.24,rot=0, coll='edificios'):
    mf = clay('m_marco', '#FAEDD2', bump=0.3, sss=0.22, ao=0.25, ao_cool=1.0); r = 0.028; ca, sa = math.cos(math.radians(rot)), math.sin(math.radians(rot))
    def W_(dx, dz): return (x + dx*ca, y + dx*sa, z + dz)   # marco: cuatro churros con puntas redondas, en el plano de la pared girada
    hw, hh = w/2 + r*0.6, h/2 + r*0.6
    churro(W_(-hw, -hh), W_(hw, -hh), r, mf, coll, 'ventana_marco'); churro(W_(-hw, hh), W_(hw, hh), r, mf, coll, 'ventana_marco')
    churro(W_(-hw, -hh), W_(-hw, hh), r, mf, coll, 'ventana_marco', caps=False); churro(W_(hw, -hh), W_(hw, hh), r, mf, coll, 'ventana_marco', caps=False)
    wn = box((w,0.07,h), loc=(x,y,z), rot=(0,0,rot), coll=coll, name='ventana'); mat(wn, clay('m_window', COL['window'], bump=0, sss=0, vary=0, emit=COL['window'], emit_strength=1.2))
def handle(x,y,z, coll='edificios'):
    k = sphere(r=0.04, seg=16, loc=(x,y,z), coll=coll, name='manilla'); mat(k, clay('m_sun', COL['sun'], bump=0.2, sss=0, vary=0))


def cushion(w, d, h, r_plan, bev, seg, wave=0.0, name='cojin', coll='isla', barrel=0.0, rings=1, seed=7, loc=(0,0,0)):
    """Losa de esquinas muy redondas en planta (radio r_plan), canto redondo (bisel bev, seg segmentos), ondeo leve del contorno
       (wave = ±fracción del semiancho, baja frecuencia) y, si barrel > 0, perfil abombado como un pan (anillos intermedios más anchos)."""
    rr = random.Random(seed); f1, f2 = rr.uniform(0, math.tau), rr.uniform(0, math.tau)
    prof = []   # contorno en planta: cuatro lados y cuatro cuartos de círculo
    hw, hd = w/2 - r_plan, d/2 - r_plan; K = 14
    for (cx, cy, a0) in ((hw, hd, 0.0), (-hw, hd, 90.0), (-hw, -hd, 180.0), (hw, -hd, 270.0)):
        for k in range(K):
            a = math.radians(a0 + 90.0*k/K); prof.append((cx + r_plan*math.cos(a), cy + r_plan*math.sin(a)))
        nxt = {0.0: (-hw, hd), 90.0: (-hw, -hd), 180.0: (hw, -hd), 270.0: (hw, hd)}[a0]; a = math.radians(a0 + 90.0)
        ex, ey = cx + r_plan*math.cos(a), cy + r_plan*math.sin(a); fx, fy = nxt[0] + r_plan*math.cos(a), nxt[1] + r_plan*math.sin(a)
        for k in range(1, 8): t = k/8; prof.append((ex + (fx-ex)*t, ey + (fy-ey)*t))
    if wave:
        out = []
        for (x, y) in prof:
            a = math.atan2(y, x); f = 1 + wave*math.sin(3*a + f1) + wave*0.6*math.sin(5*a + f2); out.append((x*f, y*f))
        prof = out
    me = bpy.data.meshes.new(name); bm = bmesh.new(); n = len(prof); R = max(1, rings); layers = []
    for i in range(R + 1):
        t = i/R; sc = 1 + barrel*math.sin(math.pi*t); layers.append([bm.verts.new((x*sc, y*sc, h*t)) for (x, y) in prof])
    bm.faces.new(list(reversed(layers[0]))); bm.faces.new(layers[-1])
    for i in range(R):
        for j in range(n): bm.faces.new((layers[i][j], layers[i][(j+1)%n], layers[i+1][(j+1)%n], layers[i+1][j]))
    bm.normal_update(); bm.to_mesh(me); bm.free(); me.update()
    o = bpy.data.objects.new(name, me); scene.collection.objects.link(o); place(o, loc=loc, coll=coll, name=name)
    md = bevel(o, bev, seg, ang=40); md.limit_method = 'ANGLE'; smooth(o); return o

# ------------------------------------------------------------------ la isla: pocas piezas, gordas
HALF = 2.3
P = 1.32   # radio de los edificios (en diagonal)
def build_island():
    # ronda 26: la plataforma es un cojín: capa crema gruesa de canto muy redondo que sobresale un 3 % sobre una base abombada como un pan, esquinas en planta casi circulares (radio 13 % del lado), contorno con ondeo leve (±1,5 %)
    base = cushion(HALF*2*0.97, HALF*2*0.97, 1.0, r_plan=0.6, bev=0.16, seg=8, wave=0.012, name='base_tierra', coll='isla', barrel=0.02, rings=6, seed=11, loc=(0,0,-1.72)); organic(base, 0.012, 1.0, 1)   # ronda 26b: bizcocho firme: lados casi verticales apenas abombados, canto definido, sin panza
    mat(base, clay('m_tan', COL['tan'], bump=0.6, sss=0.15, vary=0.04, dents=0.35, dent_scale=5.5, grain=0.28, mid=0.25, prints=0.0, piece=0, sat=1.0))   # esponja porosa fina: poros chicos y miga, sin huellas grandes
    top = cushion(HALF*2, HALF*2, 0.8, r_plan=0.62, bev=0.34, seg=10, wave=0.015, name='losa', coll='isla', seed=7, loc=(0,0,-0.8)); organic(top, 0.025, 1.2, 1)
    mat(top, clay('m_losa', '#F1DFBF', bump=0.35, sss=0.3, vary=0.012, dents=0.35, dent_scale=1.6, grain=0.0, mid=0.2, dent_fine=0.2, ao=0.22, ao_dist=0.45, ao_cool=1.0, piece=0, sat=1.0, prints=0.12))   # ronda 26b: crema limpio y parejo, huellas solo en relieve y tenues, sin tinte por pieza ni sombra fría
    R = 0.8  # rombo de la ruta
    route = [(-R,-R),(-R,R),(R,R),(R,-R),(-R,-R)]
    for i in range(len(route)-1):
        (x1,y1),(x2,y2) = route[i], route[i+1]; n = max(3, int(math.hypot(x2-x1,y2-y1)/0.22))
        for j in range(n):
            t=j/n; x=x1+(x2-x1)*t+HAND.uniform(-0.02,0.02); y=y1+(y2-y1)*t+HAND.uniform(-0.02,0.02); rr = 0.052*HAND.uniform(0.85,1.15)
            d = sphere(r=rr, seg=16, loc=(x,y,0.008), scale=(1,1,0.45), name='punto'); d.rotation_euler = (0,0,HAND.uniform(0,3)); mat(d, clay('m_dot', '#F0CB55', bump=0.3, sss=0.2, vary=0.05, rough=0.55))   # ronda 26b: amarillo sol un poco apagado y claro (el mostaza se leía como hoyos)
    for (x,y) in [(-R,0),(0,R),(R,0),(0,-R)]: lamp(x,y)

# ------------------------------------------------------------------ edificios: silueta de juguete (se arman en el origen y se agrupan)
K = 1.0
ROOTS = {}
def group(name, objs, loc, rotz=0):
    root = bpy.data.objects.new(name, None); scene.collection.objects.link(root); link(root,'edificios'); bpy.context.view_layer.update()
    for o in objs: o.parent = root
    root.location = (loc[0], loc[1], 0.0); root.rotation_euler = (0,0,math.radians(rotz)); ROOTS[name] = root; return root
def collect(fn):
    before = set(bpy.data.objects); fn(); return [o for o in bpy.data.objects if o not in before and o.type != 'LIGHT']

def observatorio():
    x=y=0
    PARED = clay('m_pared_obs', '#FBEBCB', bump=0.6, sss=0.22, rough=0.62, vary=0.03, dents=0.6, dent_scale=7, ao=0.28, ao_cool=1.0)
    AZUL = clay('m_mem_obs', '#5A94E0', bump=0.45, sss=0.2, rough=0.62, vary=0.04, dents=0.3, dent_scale=6, ao=0.45)
    AZULDK = clay('m_memDk_obs', '#3E6FBF', bump=0.4, sss=0.15, ao=0.4)
    base = cyl(r=0.6*K, d=0.6*K, loc=(x,y,0.3*K), coll='edificios', name='obs_base'); bevel(base,0.06,3); organic(base,0.012,0.7,2); mat(base, PARED)
    zoc = cyl(r=0.62*K, d=0.07*K, loc=(x,y,0.035*K), coll='edificios', name='obs_zocalo'); bevel(zoc,0.02,2); mat(zoc, clay('m_creamDk_obs', '#EDD8B2', bump=0.5, sss=0.25, ao=0.3, ao_cool=1.0))
    dome = sphere(r=0.62*K, seg=64, loc=(x,y,0.6*K), scale=(1,1,0.92), coll='edificios', name='obs_cupula'); mat(dome, AZUL)
    cutter = box((0.3*K, 1.6*K, 0.9*K), loc=(x, y-0.45*K, 1.05*K), coll='edificios', name='obs_cortador'); cutter.hide_render = True; cutter.hide_viewport = True
    bo = dome.modifiers.new('Rendija','BOOLEAN'); bo.operation = 'DIFFERENCE'; bo.object = cutter; bo.solver = 'EXACT'
    inner = sphere(r=0.56*K, seg=48, loc=(x,y,0.6*K), scale=(1,1,0.92), coll='edificios', name='obs_interior'); mat(inner, clay('m_obsint', '#2E2866', bump=0.2, sss=0.0, vary=0))
    # borde de la rendija: dos listones azules oscuros a cada lado del tajo
    for sx in (-1, 1):
        lip = box((0.045*K, 0.78*K, 0.045*K), loc=(x+sx*0.17*K, y-0.42*K, 1.08*K), coll='edificios', name='obs_borde'); bevel(lip,0.012,2)
        lip.rotation_euler = (math.radians(-28), 0, 0); mat(lip, AZULDK)
    # costilla de la cúpula (de atrás hacia adelante, por el centro) y aro de la base
    rib = torus(R=0.62*K, r=0.035*K, loc=(x,y,0.6*K), rot=(90,0,90), coll='edificios', name='obs_costilla'); rib.scale = (1,0.92,1); mat(rib, AZULDK)
    band = torus(R=0.61*K, r=0.06*K, loc=(x,y,0.62*K), coll='edificios', name='obs_aro'); mat(band, AZULDK)
    # telescopio: tubo crema con dos anillos azules y tapa celeste
    tdir = Vector((0,-0.75,0.8)).normalized(); tc = Vector((x, y-0.22*K, 0.98*K)); tl = 0.62*K
    tele = cyl(r=0.12*K, d=tl, verts=24, loc=tuple(tc + tdir*tl*0.5), coll='edificios', name='obs_telescopio'); bevel(tele,0.025,3)
    tele.rotation_euler = tdir.to_track_quat('Z','Y').to_euler(); mat(tele, clay('m_cream_tele', '#FAEDD2', bump=0.4, sss=0.3, ao=0.25, ao_cool=1.0))
    for f in (0.55, 0.82):
        rg = torus(R=0.12*K, r=0.024*K, loc=tuple(tc + tdir*tl*f), coll='edificios', name='obs_anillo'); rg.rotation_euler = tele.rotation_euler; mat(rg, AZULDK)
    lens = cyl(r=0.13*K, d=0.07*K, verts=24, loc=tuple(tc + tdir*(tl+0.02*K)), coll='edificios', name='obs_lente'); lens.rotation_euler = tele.rotation_euler; mat(lens, clay('m_lente', '#9CC7F5', bump=0.2, sss=0.2, vary=0, coat=0.6, rough=0.2))
    # puerta en arco con marco y escalón
    frame_a = cyl(r=0.2*K, d=0.1, verts=32, loc=(x,y-0.58*K,0.3*K), rot=(90,0,0), coll='edificios', name='obs_marco_arco'); mat(frame_a, AZULDK)
    frame_b = box((0.4*K,0.1,0.3*K), loc=(x,y-0.58*K,0.15*K), coll='edificios', name='obs_marco'); mat(frame_b, AZULDK)
    arch = cyl(r=0.15*K, d=0.12, verts=32, loc=(x,y-0.6*K,0.3*K), rot=(90,0,0), coll='edificios', name='obs_arco'); mat(arch, clay('m_puerta_obs', '#2E2866', bump=0.2, sss=0.0, vary=0))
    door = box((0.3*K,0.12,0.3*K), loc=(x,y-0.6*K,0.15*K), coll='edificios', name='obs_puerta'); mat(door, clay('m_puerta_obs', '#2E2866', bump=0.2, sss=0.0, vary=0)); handle(x+0.09*K,y-0.665*K,0.17*K)
    step = box((0.46*K,0.16*K,0.05*K), loc=(x,y-0.66*K,0.025*K), coll='edificios', name='obs_escalon'); bevel(step,0.015,2); mat(step, clay('m_creamDk_obs', '#EDD8B2', bump=0.5, sss=0.25, ao=0.3, ao_cool=1.0))
    window(x-0.45*K,y-0.4*K,0.33*K,rot=-48, w=0.2*K, h=0.2*K); window(x+0.45*K,y-0.4*K,0.33*K,rot=48, w=0.2*K, h=0.2*K)

def antena():
    x=y=0
    H = 1.15*K; r0 = 0.3*K; r1 = 0.1*K; z0 = 0.0   # sin caja: las patas se apoyan en la losa, con pies chicos
    legm = clay('m_ate_madera', '#E9A355', bump=0.5, sss=0.2, vary=0.06, veta=0.1, ao=0.4)
    barm = clay('m_ateDk_madera', '#D18A38', bump=0.4, sss=0.15, vary=0.05, veta=0.08, ao=0.4)
    for sx,sy in ((-1,-1),(1,-1),(1,1),(-1,1)):
        a = Vector((sx*r0, sy*r0, z0)); b_ = Vector((sx*r1, sy*r1, z0+H)); d = b_-a
        leg = cyl(r=0.06*K, d=d.length, verts=12, loc=tuple((a+b_)/2), coll='edificios', name='ant_pata'); leg.rotation_euler = d.to_track_quat('Z','Y').to_euler(); bevel(leg,0.015,2); mat(leg, legm)
        foot = cyl(r=0.1*K, d=0.07*K, verts=20, loc=(sx*r0, sy*r0, 0.035*K), coll='edificios', name='ant_pie'); bevel(foot,0.02,2); mat(foot, legm)
    corners = lambda f: [Vector((sx*(r0+(r1-r0)*f), sy*(r0+(r1-r0)*f), z0+H*f)) for sx,sy in ((-1,-1),(1,-1),(1,1),(-1,1))]
    def bar(a, b_, r=0.04*K):
        d = b_-a; o = cyl(r=r, d=d.length, verts=10, loc=tuple((a+b_)/2), coll='edificios', name='ant_barra'); o.rotation_euler = d.to_track_quat('Z','Y').to_euler(); mat(o, barm); return o
    levels = (0.0, 0.33, 0.66, 1.0)
    for f in levels[1:]:
        c = corners(f)
        for k in range(4):
            bar(c[k], c[(k+1)%4], r=0.045*K)
            kn = sphere(r=0.06*K, seg=16, loc=tuple(c[k]), coll='edificios', name='ant_nudo'); mat(kn, barm)   # unión marcada
    for f0,f1 in zip(levels[:-1], levels[1:]):
        c0, c1 = corners(f0), corners(f1)
        for k in range(4):
            bar(c0[k], c1[(k+1)%4], r=0.03*K); bar(c0[(k+1)%4], c1[k], r=0.03*K)
    plat = box((0.3*K,0.3*K,0.12*K), loc=(x,y,z0+H+0.04*K), coll='edificios', name='ant_plataforma'); bevel(plat,0.03,3); organic(plat,0.01,0.5,1); mat(plat, legm)   # bloque de remate, como la referencia
    bulb = sphere(r=0.21*K, loc=(x,y,z0+H+0.26*K), coll='edificios', name='ant_luz'); mat(bulb, clay('m_bulb_ant', '#FFF0C8', bump=0, sss=0, vary=0, emit='#FFE08A', emit_strength=1.8))   # ámbar, sin quemarse a blanco
    Lt = bpy.data.lights.new('ant_L','POINT'); Lt.energy = 48; Lt.color = lin(hx('#FFB84A')); Lt.shadow_soft_size = 0.4
    lo = bpy.data.objects.new('ant_L', Lt); lo.location = (x,y,z0+H+0.26*K); scene.collection.objects.link(lo); link(lo,'luces')

def gear(gx,gy,gz,r,teeth,rot=(90,0,0),coll='edificios'):
    mc = clay('m_gear', '#F3E4C8', bump=0.45, sss=0.22, rough=0.62, dents=0.4, dent_scale=8, ao=0.35, ao_cool=1.0)
    g = cyl(r=r, d=0.1, verts=48, loc=(gx,gy,gz), rot=rot, coll=coll, name='engranaje'); bevel(g,0.025,4); mat(g, mc)
    hub = cyl(r=r*0.28, d=0.13, verts=24, loc=(gx,gy,gz), rot=rot, coll=coll, name='eje'); bevel(hub,0.015,3); mat(hub, clay('m_razDk', COL['razDk'], bump=0.2))
    for i_ in range(teeth):
        a = i_*math.tau/teeth; rr = r*1.08
        if rot == (90,0,0):
            t = box((r*0.36, 0.1, r*0.34), loc=(gx + rr*math.cos(a), gy, gz + rr*math.sin(a)), coll=coll, name='diente'); t.rotation_euler = (0, -a, 0)
        else:
            t = box((0.1, r*0.36, r*0.34), loc=(gx, gy + rr*math.cos(a), gz + rr*math.sin(a)), coll=coll, name='diente'); t.rotation_euler = (a, 0, 0)
        bevel(t, 0.022, 4); mat(t, mc)

def hangar():
    x=y=0
    LAV = clay('m_raz_h', '#9B82DE', bump=0.7, sss=0.2, rough=0.62, vary=0.06, dents=0.7, dent_scale=7, ao=0.45)
    LAVLT = clay('m_razLt_h', '#B59FEA', bump=0.5, sss=0.25, vary=0.04, dents=0.5, dent_scale=7, ao=0.4)
    LAVDK = clay('m_razDk_h', '#5C47A8', bump=0.4, sss=0.15, vary=0.04, ao=0.4)
    CREMA = clay('m_cream_h', '#FBEBCB', bump=0.5, sss=0.3, ao=0.28, ao_cool=1.0)
    body = box((1.0*K,1.0*K,0.95*K), loc=(x,y,0.475*K), coll='edificios', name='han_cuerpo'); bevel(body,0.09,5); organic(body,0.018,0.9,2); mat(body, LAV)   # cantos redondos sin perder el cubo
    zoc = box((1.05*K,1.05*K,0.09*K), loc=(x,y,0.045*K), coll='edificios', name='han_zocalo'); bevel(zoc,0.02,2); mat(zoc, LAVDK)
    roof = box((1.2*K,1.2*K,0.17*K), loc=(x,y,0.985*K), coll='edificios', name='han_techo'); bevel(roof,0.07,6); organic(roof,0.008,1.0,2); mat(roof, LAVLT)   # techo gordo con canto redondo y desnivel leve (el casquete abombado parecía un segundo techo)
    # portón de lamas con marco (dos jambas y dintel) en la cara +X
    door = box((0.07,0.5*K,0.55*K), loc=(x+0.5*K,y+0.12*K,0.275*K), coll='edificios', name='han_porton'); bevel(door,0.03,2); mat(door, LAVDK)
    for i_ in range(5):   # lamas: churros horizontales con puntas redondas
        churro((x+0.53*K, y+0.12*K-0.22*K, 0.07*K+i_*0.1*K), (x+0.53*K, y+0.12*K+0.22*K, 0.07*K+i_*0.1*K), 0.018*K, LAV, 'edificios', 'han_lama')
    for sy in (-1, 1):
        jamb = box((0.09,0.06*K,0.6*K), loc=(x+0.5*K, y+0.12*K+sy*0.28*K, 0.3*K), coll='edificios', name='han_jamba'); bevel(jamb,0.015,2); mat(jamb, LAVLT)
    lintel = box((0.09,0.62*K,0.06*K), loc=(x+0.5*K, y+0.12*K, 0.585*K), coll='edificios', name='han_dintel'); bevel(lintel,0.015,2); mat(lintel, LAVLT)
    # placa oscura con el engranaje chico, arriba del portón
    plate = box((0.06,0.3*K,0.3*K), loc=(x+0.5*K, y-0.26*K, 0.72*K), coll='edificios', name='han_placa'); bevel(plate,0.015,2); mat(plate, LAVDK)
    gear(x+0.53*K, y-0.26*K, 0.72*K, 0.09*K, 7, rot=(0,90,0))
    gear(x-0.05*K, y-0.56*K, 0.42*K, 0.25*K, 9, rot=(90,0,0))   # más bajo y algo menor: fuera de la sombra del alero
    chim = box((0.18*K,0.18*K,0.35*K), loc=(x-0.3*K,y+0.3*K,1.15*K), coll='edificios', name='han_chimenea'); bevel(chim,0.03,2); mat(chim, LAVDK)

def biblioteca():
    x=y=0
    CREMA = clay('m_cream_b', '#FBEBCB', bump=0.7, sss=0.22, rough=0.62, vary=0.03, dents=0.7, dent_scale=7, ao=0.28, ao_cool=1.0)
    VERDE = clay('m_len_b', '#4E9E66', bump=0.6, sss=0.2, rough=0.62, vary=0.06, dents=0.6, dent_scale=7, ao=0.45)
    VERDELT = clay('m_lenLt_b', '#7DC28C', bump=0.6, sss=0.25, vary=0.05, dents=0.5, dent_scale=7, ao=0.4)
    VERDEDK = clay('m_lenDk_b', '#2F7E5C', bump=0.4, sss=0.15, vary=0.04, ao=0.4)
    body = box((1.05*K,0.9*K,0.72*K), loc=(x,y,0.36*K), coll='edificios', name='bib_cuerpo'); bevel(body,0.05,3); organic(body,0.015,0.8,2); mat(body, CREMA)
    zoc = box((1.09*K,0.94*K,0.08*K), loc=(x,y,0.04*K), coll='edificios', name='bib_zocalo'); bevel(zoc,0.02,2); mat(zoc, clay('m_creamDk_b', '#EDD8B2', bump=0.5, sss=0.25, ao=0.3, ao_cool=1.0))
    upper = box((1.07*K,0.92*K,0.3*K), loc=(x,y,0.6*K), coll='edificios', name='bib_alto'); bevel(upper,0.04,3); organic(upper,0.012,0.8,1); mat(upper, VERDE)
    # techo a dos aguas con tejas en relieve y alero más ancho
    # techo a dos aguas: dos losas gordas y lisas (canto muy redondo), con tres surcos apretados con el dedo por agua (relieve en el material)
    mt = clay('m_techo_b', '#6DB882', bump=0.5, sss=0.22, rough=0.62, vary=0.04, dents=0.4, dent_scale=5, ao=0.4)
    nt = mt.node_tree; N = nt.nodes; Lk = nt.links
    wv = N.new('ShaderNodeTexWave'); wv.wave_type = 'BANDS'; wv.bands_direction = 'X'; wv.wave_profile = 'SIN'; wv.inputs['Scale'].default_value = 3.6; wv.inputs['Distortion'].default_value = 0.3
    tco = N.new('ShaderNodeTexCoord'); Lk.new(tco.outputs['Object'], wv.inputs['Vector'])
    bpn = [n_ for n_ in N if n_.type == 'BUMP'][0]; prev = bpn.inputs['Height'].links[0].from_socket
    mw = N.new('ShaderNodeMath'); mw.operation = 'MULTIPLY'; mw.inputs[1].default_value = 0.35; Lk.new(wv.outputs['Fac'], mw.inputs[0])
    ad = N.new('ShaderNodeMath'); ad.operation = 'ADD'; Lk.new(prev, ad.inputs[0]); Lk.new(mw.outputs[0], ad.inputs[1]); Lk.new(ad.outputs[0], bpn.inputs['Height'])
    ang = math.degrees(math.atan2(0.5*K, 0.65*K)); L_ = math.hypot(0.5*K, 0.65*K)
    for sgn in (-1, 1):
        slab = box((L_*1.06, 1.1*K, 0.13*K), loc=(sgn*0.325*K, y, 1.0*K + 0.05*K), rot=(0, sgn*ang, 0), coll='edificios', name='bib_techo')
        bevel(slab, 0.055*K, 6); organic(slab, 0.006, 1.2, 2); mat(slab, mt)
    eave = box((1.34*K, 1.12*K, 0.06*K), loc=(x,y,0.74*K), coll='edificios', name='bib_alero'); bevel(eave,0.02,2); mat(eave, VERDEDK)   # canto del alero
    ridge = cyl(r=0.05*K, d=1.14*K, verts=20, loc=(x,y,1.27*K), rot=(90,0,0), coll='edificios', name='bib_cumbrera'); bevel(ridge, 0.02, 3); mat(ridge, VERDEDK)
    # toldo a rayas, bien visible, sobre la puerta
    awn = cyl(r=0.24*K, d=0.7*K, verts=32, loc=(x,y-0.45*K,0.5*K), rot=(0,90,0), coll='edificios', name='bib_toldo'); awn.scale = (0.5,1,1)
    mat(awn, stripes('m_toldo', '#2F7E5C', '#F6EAD2', scale=7, along='Z'))
    for k_ in range(5):
        sc_ = sphere(r=0.075*K, seg=16, loc=(x-0.28*K+k_*0.14*K, y-0.45*K-0.2*K, 0.5*K-0.19*K), scale=(1,0.5,0.7), coll='edificios', name='bib_toldo_onda'); mat(sc_, stripes('m_toldo', '#2F7E5C', '#F6EAD2', scale=7, along='Z'))
    arch = cyl(r=0.16*K, d=0.08, verts=32, loc=(x,y-0.46*K,0.26*K), rot=(90,0,0), coll='edificios', name='bib_arco'); mat(arch, VERDEDK)
    door = box((0.32*K,0.08,0.26*K), loc=(x,y-0.46*K,0.13*K), coll='edificios', name='bib_puerta'); mat(door, VERDEDK); handle(x+0.1*K,y-0.51*K,0.15*K)
    window(x-0.36*K,y-0.46*K,0.3*K, w=0.2*K, h=0.2*K); window(x+0.36*K,y-0.46*K,0.3*K, w=0.2*K, h=0.2*K)
    window(x+0.53*K,y+0.1*K,0.3*K, w=0.2*K, h=0.2*K, rot=90)
    # letrero Ñ grande con marco
    sign_f = box((0.64*K,0.07,0.42*K), loc=(x,y-0.53*K,0.95*K), coll='edificios', name='bib_letrero_marco'); bevel(sign_f,0.03,2); mat(sign_f, VERDEDK)
    sign = box((0.56*K,0.08,0.34*K), loc=(x,y-0.545*K,0.95*K), coll='edificios', name='bib_letrero'); bevel(sign,0.03,2); mat(sign, clay('m_sun_b', COL['sun'], bump=0.3, sss=0.2, ao=0.3))
    cu = bpy.data.curves.new('enie', type='FONT'); cu.body = 'Ñ'; cu.extrude = 0.03; cu.size = 0.3*K; cu.align_x = 'CENTER'; cu.align_y = 'CENTER'
    tx = bpy.data.objects.new('bib_enie', cu); scene.collection.objects.link(tx); place(tx, loc=(x,y-0.59*K,0.94*K), rot=(90,0,0), coll='edificios'); mat(tx, clay('m_ink', COL['ink'], bump=0, sss=0, vary=0))
    # buzón verde al costado derecho, con ranura
    mbx = box((0.14*K,0.12*K,0.16*K), loc=(x+0.66*K,y-0.2*K,0.3*K), coll='edificios', name='bib_buzon'); bevel(mbx,0.03,3); mat(mbx, VERDEDK)
    slot = box((0.02,0.07*K,0.015*K), loc=(x+0.735*K,y-0.2*K,0.34*K), coll='edificios', name='bib_ranura'); mat(slot, clay('m_cream_h', '#FBEBCB', bump=0.5, sss=0.3, ao=0.28, ao_cool=1.0))
    mbp = cyl(r=0.025*K, d=0.22*K, verts=12, loc=(x+0.66*K,y-0.2*K,0.11*K), coll='edificios', name='bib_buzon_pie'); mat(mbp, clay('m_tanDk', COL['tanDk'], bump=0.3))

# ------------------------------------------------------------------ Nubi: silueta de la referencia (base achatada + lóbulos), cara al centro
NUBI_SPOTS = ((-0.62,-0.22,0.52,0.26),(0.12,-0.1,0.95,0.22),(0.78,-0.25,0.56,0.30),(-1.12,-0.3,-0.05,0.2),(1.18,-0.2,-0.2,0.22),(-0.5,-0.48,-0.42,0.16))
def nubi(x=0,y=0,z=3.0,pose='hola',pose_mirada='frente'):
    # cuerpo: elipsoide achatado + lóbulos como esferas de tamaño exacto, unidos por remallado de vóxeles y suavizado
    pieces = []
    J = random.Random(23); jit = lambda a: J.uniform(-a, a)   # asimetría hecha a mano, con semilla fija (igual en las cinco capas)
    # ronda 26b (Ricardo: «el ejemplo tiene una estructura sin tanto movimiento»): silueta calma como frijol o almohada: base plana de canto redondo, una cúpula ancha y encima 2-3 ondulaciones suaves (escotaduras de ~6-8 % del alto), costados fundidos, ancho/alto ~1,5
    b0 = sphere(r=1.0, seg=48, loc=(0,0.02,-0.18), scale=(1.13,0.66,0.42), coll='nubi', name='nubi_base'); pieces.append(b0)     # base ancha, redonda de lado, fundida con la cúpula (sin repisa)
    b1 = sphere(r=1.0, seg=48, loc=(0,-0.05,0.05), scale=(0.9,0.5,0.55), coll='nubi', name='nubi_frente'); pieces.append(b1)   # frente (se aplana después con un corte local)
    b2 = sphere(r=1.0, seg=48, loc=(-0.04,0.0,0.26), scale=(1.1,0.56,0.64), coll='nubi', name='nubi_cupula'); pieces.append(b2)   # cúpula ancha: la forma grande, tranquila
    for (dx,dy,dz,r) in [(-0.1,0.05,0.62,0.42),    # tres ondulaciones suaves sobre la cúpula: sobresalen 0,05-0,12 (6-8 % del alto)
                         (-0.62,0.0,0.5,0.42),
                         (0.5,0.0,0.55,0.4),
                         (-0.98,0.05,-0.08,0.27),  # costados: bultos chicos fundidos en la curva de la base
                         (0.98,0.05,-0.1,0.27)]:
        pieces.append(sphere(r=r*(1+jit(0.03)), seg=40, loc=(dx+jit(0.02),dy,dz+jit(0.02)), coll='nubi', name='nubi_lobulo'))
    for o in bpy.context.selected_objects: o.select_set(False)
    for o in pieces: o.select_set(True)
    bpy.context.view_layer.objects.active = pieces[0]; bpy.ops.object.transform_apply(scale=True); bpy.ops.object.join(); body = bpy.context.object; body.name = 'nubi'
    # cara aplanada SOLO localmente: lo que sobresale al frente de y = CARA_Y se comprime contra ese plano (en los vértices, antes del remallado);
    # el remallado y el suavizado redondean el borde. Atrás y a los costados, la nube queda redonda.
    CARA_Y = -0.44
    for v in body.data.vertices:
        if v.co.y < CARA_Y: v.co.y = CARA_Y + (v.co.y - CARA_Y) * 0.12
    body.scale = (1.0, 0.9, 1.0); bpy.ops.object.transform_apply(scale=True)   # profundidad ~88 % de la original: de lado, una nube redonda
    rm = body.modifiers.new('Union','REMESH'); rm.mode = 'VOXEL'; rm.voxel_size = 0.016; rm.use_smooth_shade = True
    sm = body.modifiers.new('Suave','SMOOTH'); sm.factor = 1.0; sm.iterations = 30
    organic(body, strength=0.05, scale=0.4, levels=2)   # amasado a mano: grumos chicos EN la superficie; la forma grande queda tranquila
    SPOTS = [((-0.62,0.0,0.5,0.42), (-0.75,-0.45,0.55), 0.33, '#93CBE6'),   # dos tonos de celeste, trozos apretados con el dedo: contorno irregular, grosor mínimo
             ((-0.1,0.05,0.62,0.42), (0.35,-0.5,0.8), 0.24, '#6FB4D8'),
             ((0.5,0.0,0.55,0.4), (0.55,-0.7,0.4), 0.3, '#93CBE6'),
             ((-0.98,0.05,-0.08,0.27), (-0.95,-0.3,0.1), 0.24, '#6FB4D8'),
             ((0.98,0.05,-0.1,0.27), (0.6,-0.7,-0.1), 0.17, '#93CBE6')]
    # la primera y la cuarta miran hacia afuera: doblan el borde de la silueta, como la de la izquierda en la referencia
    spots = []
    for ((cx,cy,cz,cr), nd, rs, col) in SPOTS:
        P = Vector((cx,cy,cz)) + Vector(nd).normalized()*cr; spots.append((round(P.x,3), round(P.y,3), round(P.z,3), rs, col))
    body.data.materials.clear(); mat(body, nubi_material(spots)); link(body,'nubi')
    parts = [body]
    MIRADAS = {'frente': ((0.0,0.0), (0,0,0)), 'observatorio': ((-0.04,-0.022), (6,-6,-18)), 'antena': ((-0.04,0.012), (0,-4,-12)),
               'biblioteca': ((0.04,-0.022), (6,6,18)), 'hangar': ((-0.01,-0.038), (8,0,-3))}   # (desplazamiento de pupila x,z, dentro del disco: el aro nunca desaparece), (cabeceo, ladeo, giro) en grados
    (PX, PZ), (PITCH, ROLL, YAW) = MIRADAS.get(pose_mirada, MIRADAS['frente'])
    print('[isla] nubi: malla armada', round(time.time()-t_all,1)); sys.stdout.flush()
    from mathutils.bvhtree import BVHTree
    _dg = bpy.context.evaluated_depsgraph_get(); _ev = body.evaluated_get(_dg); _me = _ev.to_mesh()   # una sola copia de la malla evaluada de Nubi para todos los rayos de la cara
    _bm = bmesh.new(); _bm.from_mesh(_me); _bvh = BVHTree.FromBMesh(_bm); _ev.to_mesh_clear(); _bm.free()
    print('[isla] nubi: bvh listo', round(time.time()-t_all,1)); sys.stdout.flush()
    def surf_y(px, pz, default=-0.3):
        loc, nrm, idx, dist = _bvh.ray_cast(Vector((px, -5.0, pz)), Vector((0, 1, 0)))
        return loc.y if loc is not None else default
    EZ, ER, EX = 0.2 + jit(0.012), 0.24, 0.42 + jit(0.012)   # ojos al ~64 % desde la cima (con el cuerpo redondo de la ronda 23 la cara había caído al 71 %); leve asimetría
    def eye_y(sx):   # el disco va por delante del punto más saliente del cuerpo bajo todo su borde, así ningún lóbulo lo corta
        ys = [surf_y(sx + dx, EZ + dz) for dx, dz in ((0,0),(ER,0),(-ER,0),(0,ER),(0,-ER),(0.7*ER,0.7*ER),(-0.7*ER,0.7*ER),(0.7*ER,-0.7*ER),(-0.7*ER,-0.7*ER))]
        return min(ys) + 0.012   # el disco se asienta: su cara trasera queda 0,037 dentro del punto más saliente bajo su borde
    EYS = {-EX: eye_y(-EX), EX: eye_y(EX)}; EY = min(EYS.values())
    if pose == 'descansa':
        for sx in (-EX, EX):
            cv = bpy.data.curves.new('parpado','CURVE'); cv.dimensions='3D'; cv.bevel_depth=0.022; sp=cv.splines.new('BEZIER'); sp.bezier_points.add(2)
            for p_,(px,py,pz) in zip(sp.bezier_points,[(sx-0.13,EY-0.08,EZ+0.06),(sx,EY-0.12,EZ-0.02),(sx+0.13,EY-0.08,EZ+0.06)]): p_.co=(px,py,pz); p_.handle_left_type=p_.handle_right_type='AUTO'
            e = bpy.data.objects.new('nubi_ojo_c', cv); scene.collection.objects.link(e); link(e,'nubi'); e.data.materials.append(clay('m_ink', COL['ink'], bump=0, sss=0, vary=0)); parts.append(e)
    else:
        for sx in (-EX, EX):
            EY = EYS[sx]
            disc = cyl(r=ER, d=0.05, verts=40, loc=(sx,EY,EZ+jit(0.01)), rot=(90,0,0), coll='nubi', name='nubi_ojo_disco'); bevel(disc,0.014,4); mat(disc, clay('m_ojo_disco', '#F2E6D0', bump=0.15, sss=0.2, vary=0, rough=0.55, spec=0.18, emit='#F6ECD8', emit_strength=0.12, sat=1.0, piece=0)); parts.append(disc)   # lentejuela crema-blanco, con un poco de luz propia para que no se vea gris   # disco de plasticina con grosor (~2 % del ancho) y canto redondo
            iris = sphere(r=ER*0.85, seg=32, loc=(sx+PX,EY-0.025+0.03,EZ-0.004+PZ), scale=(1,0.42,1), coll='nubi', name='nubi_ojo'); mat(iris, clay('m_ojo', '#4B2A44', bump=0, sss=0.05, vary=0, rough=0.45, coat=0.3, spec=0.2, sheen=0, sat=1.0, piece=0, chalk=0, prints=0)); parts.append(iris)   # ronda 26: ciruela casi parejo, mate con barniz suave; aro de ~15 % del radio   # bolita aplastada con barniz, sobre el disco   # iris café-burdeo casi plano, al 75 % del disco, centrado
            g = sphere(r=0.05, seg=16, loc=(sx+PX-0.07,EY-0.06,EZ+PZ+0.072), coll='nubi', name='nubi_brillo'); mat(g, clay('m_glint', COL['glint'], bump=0, sss=0, vary=0, rough=0.3, spec=0.1)); parts.append(g)
            g2 = sphere(r=0.017, seg=12, loc=(sx+PX+0.062,EY-0.06,EZ+PZ-0.065), coll='nubi', name='nubi_brillo2'); mat(g2, clay('m_glint', COL['glint'], bump=0, sss=0, vary=0, rough=0.3, spec=0.1)); parts.append(g2)
    for sx in (-0.72, 0.72):  # mejillas: óvalos al ras, a la altura de la sonrisa y por fuera de los ojos
        ch = sphere(r=0.13, seg=24, loc=(sx*0.9 + jit(0.015), surf_y(sx*0.9, -0.07) - 0.006, -0.07 + jit(0.012)), scale=(1.25,0.17,0.8), coll='nubi', name='nubi_mejilla'); mat(ch, clay('m_mejilla', '#F2A58C', bump=0.2, sss=0.4, vary=0)); parts.append(ch)
    # boca: un churrito de plasticina oscura (tubo curvo, radio ~1,1 % del ancho, puntas redondas), apenas hundido en la cara
    MB = clay('m_boca', '#4A2A3A', bump=0.2, sss=0.12, vary=0, rough=0.55, sat=1.0, piece=0, chalk=0, prints=0); RB = 0.045; mx_ = jit(0.01)
    # ronda 26: arco liso de curvatura pareja «‿», sin punto medio: nueve puntos sobre un arco de círculo; ancho 0,8 diámetros de ojo, hondura 30 % del ancho
    W_ = 0.56*2*ER; D_ = 0.3*W_; Rc = (W_*W_/4 + D_*D_)/(2*D_); th = math.acos(1 - D_/Rc); ZM = -0.03; zc = ZM - D_/2 + Rc
    boca = [(mx_ + Rc*math.sin(-th + 2*th*i/8), zc - Rc*math.cos(-th + 2*th*i/8)) for i in range(9)]
    cv = bpy.data.curves.new('sonrisa','CURVE'); cv.dimensions='3D'; cv.bevel_depth=RB; cv.bevel_resolution=6; sp=cv.splines.new('BEZIER'); sp.bezier_points.add(8)
    for p_,(px,pz) in zip(sp.bezier_points,boca): p_.co=(px, surf_y(px, pz) + RB*0.25, pz); p_.handle_left_type=p_.handle_right_type='AUTO'   # churro hundido: 1,25 radios dentro, 0,75 fuera
    so = bpy.data.objects.new('nubi_sonrisa', cv); scene.collection.objects.link(so); link(so,'nubi'); so.data.materials.append(MB); parts.append(so)
    for (px,pz) in (boca[0], boca[-1]):
        tip = sphere(r=RB*0.98, seg=16, loc=(px, surf_y(px, pz) + RB*0.25, pz), coll='nubi', name='nubi_sonrisa_punta'); mat(tip, MB); parts.append(tip)
    print('[isla] nubi: cara lista', round(time.time()-t_all,1)); sys.stdout.flush()
    root = bpy.data.objects.new('nubi_raiz', None); scene.collection.objects.link(root); link(root,'nubi')
    giro = bpy.data.objects.new('nubi_giro', None); scene.collection.objects.link(giro); link(giro,'nubi'); bpy.context.view_layer.update()
    giro.parent = root; giro.rotation_euler = (math.radians(PITCH), math.radians(ROLL + 3.0), math.radians(YAW))   # giro en los ejes propios de Nubi
    for o in parts: o.parent = giro
    root.location = (x,y,z); root.rotation_euler = (0,0,math.radians(45)); root.scale = (1.0,1.0,1.0); ROOTS['Nubi'] = root   # ronda 26b: Nubi un 18 % más grande respecto de la isla, como la referencia

# ------------------------------------------------------------------ luz clara, cámara, compositor
def studio():
    def area(name, loc, energy, size, color):
        L = bpy.data.lights.new(name,'AREA'); L.energy = energy; L.size = size; L.color = lin(hx(color))
        o = bpy.data.objects.new(name, L); o.location = loc; scene.collection.objects.link(o); link(o,'luces')
        o.rotation_euler = (Vector((0,0,0.4)) - Vector(loc)).to_track_quat('-Z','Y').to_euler(); return o
    # luz clave: SOL desde arriba a la izquierda y algo al frente (en pantalla), elevación ~45°, disco de 6° (sombra blanda); las sombras caen abajo a la derecha
    S = bpy.data.lights.new('sol','SUN'); S.energy = 4.2; S.color = lin(hx('#FFF1DC')); S.angle = math.radians(6)
    so = bpy.data.objects.new('sol', S); scene.collection.objects.link(so); link(so,'luces')
    so.rotation_euler = Vector((1.0, 0.18, -1.0)).to_track_quat('-Z','Y').to_euler()   # viaja hacia +X (en pantalla, de arriba-izquierda hacia abajo-derecha), elevación ~45°
    area('relleno', (8,-3,4.5), 300, 10, '#D6DEFF')
    area('rebote', (2,2,-1.5), 220, 7, '#FFE2C2')
    area('contraluz', (-2,7,6), 380, 6, '#FFE6C8')
    def area_nubi(name, loc, energy, size, color, target):
        o = area(name, loc, energy, size, color)
        o.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat('-Z','Y').to_euler()
        try: o.light_linking.receiver_collection = C['nubi']   # solo Nubi recibe esta luz (Blender 4.x)
        except Exception as e: print('[isla] sin enlace de luces:', e)
        return o
    NUBI_AT = (0.5, 0.5, 2.75)
    area_nubi('nubi_relleno', (6.0, -6.5, 5.5), 420, 5, '#FFF3E6', NUBI_AT)    # relleno frontal, cálido y parejo
    area_nubi('nubi_rebote', (0.8, 0.0, 0.3), 80, 4, '#FFEBDA', NUBI_AT)       # rebote cálido desde la losa (ronda 26: a la mitad y menos rosado: la panza más oscura y cálida) crema
    w = scene.world or bpy.data.worlds.new('World'); scene.world = w; w.use_nodes = True
    bg = w.node_tree.nodes.get('Background'); bg.inputs['Color'].default_value = (*lin(hx('#3A3590')),1); bg.inputs['Strength'].default_value = 0.42
def camera():
    cam = bpy.data.cameras.new('cam'); cam.type = 'PERSP'; cam.lens = 90; cam.sensor_width = 36
    co = bpy.data.objects.new('cam', cam); co.location = (12.8,-12.8,14.5); scene.collection.objects.link(co)
    co.rotation_euler = (Vector((0,0,0.7)) - co.location).to_track_quat('-Z','Y').to_euler(); scene.camera = co
    cam.dof.use_dof = True; cam.dof.focus_distance = (Vector((0,0,0.7)) - co.location).length; cam.dof.aperture_fstop = 5.0
def camera_perfil():
    root = ROOTS['Nubi']; bpy.context.view_layer.update()
    c = Vector(root.location); right = root.matrix_world.to_3x3() @ Vector((1,0,0)); right.normalize()
    cam = bpy.data.cameras.new('cam_perfil'); cam.type = 'PERSP'; cam.lens = 90; cam.sensor_width = 36
    co = bpy.data.objects.new('cam_perfil', cam); co.location = tuple(c + right*7.5 + Vector((0,0,0.6))); scene.collection.objects.link(co)
    co.rotation_euler = (c - co.location).to_track_quat('-Z','Y').to_euler(); scene.camera = co

def compositor():
    scene.use_nodes = True; nt = scene.node_tree; N = nt.nodes; N.clear()
    rl = N.new('CompositorNodeRLayers'); gl = N.new('CompositorNodeGlare'); comp = N.new('CompositorNodeComposite')
    for t in ('BLOOM','FOG_GLOW'):
        try: gl.glare_type = t; break
        except Exception: continue
    for a,v in (('threshold',1.9),('size',6),('mix',-0.8),('quality','MEDIUM')):
        try: setattr(gl,a,v)
        except Exception: pass
    cb = N.new('CompositorNodeColorBalance'); cb.correction_method = 'LIFT_GAMMA_GAIN'; cb.lift = (1.0, 1.0, 1.0); cb.gamma = (1.0, 1.0, 1.0); cb.gain = (1.0, 1.0, 1.0)   # ronda 26b: neutro (el toque cálido sumaba amarillo a la tapa)
    bc = N.new('CompositorNodeBrightContrast'); bc.inputs['Contrast'].default_value = 0.0   # ronda 26b: sin bajar el contraste (la 26 salía neblinosa)
    nt.links.new(rl.outputs['Image'], gl.inputs['Image']); nt.links.new(gl.outputs['Image'], cb.inputs['Image']); nt.links.new(cb.outputs['Image'], bc.inputs['Image']); nt.links.new(bc.outputs['Image'], comp.inputs['Image'])
    if 'Alpha' in rl.outputs and 'Alpha' in comp.inputs: nt.links.new(rl.outputs['Alpha'], comp.inputs['Alpha'])
def setup_render():
    r = scene.render; r.resolution_x = WIDTH; r.resolution_y = int(WIDTH*0.95); r.resolution_percentage = 100
    r.film_transparent = True; r.image_settings.file_format = 'PNG'; r.image_settings.color_mode = 'RGBA'; r.image_settings.compression = 50
    scene.view_settings.view_transform = 'Standard'; scene.view_settings.exposure = 0.0
    if ENGINE == 'CYCLES':
        r.engine = 'CYCLES'; scene.cycles.device = 'CPU'; scene.cycles.samples = SAMPLES; scene.cycles.use_denoising = True
        try: scene.cycles.denoiser = 'OPENIMAGEDENOISE'
        except Exception: pass
        scene.cycles.use_adaptive_sampling = True; scene.cycles.max_bounces = 8; scene.cycles.diffuse_bounces = 3   # ronda 26b: menos rebotes de color (verde de la Biblioteca sobre la tapa)
    else:
        for eid in ('BLENDER_EEVEE_NEXT','BLENDER_EEVEE'):
            try: r.engine = eid; break
            except Exception: continue
        ee = scene.eevee; ee.taa_render_samples = SAMPLES
        for a,v in (('use_gtao',True),('use_shadows',True),('use_raytracing',True),('shadow_ray_count',2),('shadow_step_count',4)):
            if hasattr(ee,a): setattr(ee,a,v)
def render(name, visible, holdout=()):
    for cn,c in C.items(): c.hide_render = cn not in visible
    for cn in holdout:   # presentes pero invisibles: solo proyectan sombra y rebotes (para que cada capa de la app lleve las sombras de las otras)
        for o in C[cn].objects:
            if o.type == 'MESH': o.is_holdout = True
    scene.render.filepath = os.path.join(OUT, name+'.png'); t0=time.time(); bpy.ops.render.render(write_still=True); dt=time.time()-t0
    for cn in holdout:
        for o in C[cn].objects:
            if o.type == 'MESH': o.is_holdout = False
    print(f'[isla] {name}: {dt:.1f} s'); return dt

def export_json():
    import json
    from bpy_extras.object_utils import world_to_camera_view as w2c
    bpy.context.view_layer.update()
    W, H = scene.render.resolution_x, scene.render.resolution_y
    def px(v):
        c = w2c(scene, scene.camera, Vector(v)); return [round(c.x*W, 1), round((1-c.y)*H, 1)]
    alt = {'Observatorio': 1.2*K, 'Antena': 1.6*K, 'Hangar': 1.15*K, 'Biblioteca': 1.3*K}   # alto aproximado de cada edificio, para el área tocable
    R = 0.8
    data = {
      'imagen': {'ancho': W, 'alto': H, 'origen': 'arriba-izquierda, píxeles del render completo sin recortar'},
      'losa': {'esquinas': [px((-HALF,-HALF,0)), px((-HALF,HALF,0)), px((HALF,HALF,0)), px((HALF,-HALF,0))], 'centro': px((0,0,0))},
      'lugares': {},
      'paradas': {'rombo': [px((-R,-R,0)), px((-R,R,0)), px((R,R,0)), px((R,-R,0))]},
      'lamparas': [px((-R,0,0.5)), px((0,R,0.5)), px((R,0,0.5)), px((0,-R,0.5))],
      'nubi': {},
    }
    for name, r in ROOTS.items():
        x, y, z = r.location
        if name == 'Nubi':
            data['nubi'] = {'centro': px((x,y,z)), 'sombra': px((x,y,0)), 'base': px((x,y,z-0.75))}
        else:
            data['lugares'][name.lower()] = {'pie': px((x,y,0)), 'centro': px((x,y,alt[name]*0.5)), 'techo': px((x,y,alt[name])), 'area': {'Observatorio':'memoria','Antena':'atencion','Hangar':'razonamiento','Biblioteca':'lenguaje'}[name]}
            if name == 'Antena': data['lugares']['antena']['lampara'] = px((x,y,1.41*K))
    with open(os.path.join(OUT,'isla.json'),'w',encoding='utf-8') as f: json.dump(data, f, ensure_ascii=False, indent=1)
    print('[isla] isla.json escrito')

# ------------------------------------------------------------------ íconos de la barra: objetos chicos con el mismo material y la misma luz
def icono_hoy(on):
    base = box((0.8,0.8,0.3), loc=(0,0,-0.37), coll='iconos', name='ic_base'); bevel(base,0.08,3); organic(base,0.02,0.6,1); mat(base, clay('m_tan', COL['tan'], bump=0.8, sss=0.1, vary=0.08, dents=1.0, dent_scale=2.8, grain=0.1, mid=0.5))
    top = box((0.9,0.9,0.22), loc=(0,0,-0.11), coll='iconos', name='ic_losa'); bevel(top,0.1,5); organic(top,0.015,0.5,2); mat(top, clay('m_losa', '#F1DFBF', bump=0.55, sss=0.3, vary=0.03, dents=1.0, dent_scale=2.0, grain=0.0, mid=0.3, dent_fine=0.2))
    ob = cyl(r=0.2, d=0.2, loc=(-0.18,-0.18,0.1), coll='iconos', name='ic_obs'); bevel(ob,0.03,2); mat(ob, clay('m_pared', COL['cream'], bump=0.8, sss=0.3, dents=0.6, dent_scale=7))
    dm = sphere(r=0.21, loc=(-0.18,-0.18,0.2), scale=(1,1,0.9), coll='iconos', name='ic_cupula'); mat(dm, clay('m_mem', '#7FB0EE', bump=0.7, sss=0.3, dents=0.5, dent_scale=7))
    legm = clay('m_ate', COL['ate'], bump=0.5, sss=0.2); H = 0.62
    for sx,sy in ((-1,-1),(1,-1),(1,1),(-1,1)):
        a = Vector((0.22+sx*0.11, 0.22+sy*0.11, 0.0)); b_ = Vector((0.22+sx*0.04, 0.22+sy*0.04, H)); d = b_-a
        leg = cyl(r=0.022, d=d.length, verts=10, loc=tuple((a+b_)/2), coll='iconos', name='ic_pata'); leg.rotation_euler = d.to_track_quat('Z','Y').to_euler(); mat(leg, legm)
    bulb = sphere(r=0.095, loc=(0.22,0.22,H+0.1), coll='iconos', name='ic_luz'); mat(bulb, clay('m_bulb_on' if on else 'm_bulb_off', COL['sunLt'] if on else '#E8D6B2', bump=0, sss=0.2, vary=0, emit=('#FFB84A' if on else None), emit_strength=3.2))
    if on:
        Lt = bpy.data.lights.new('ic_L','POINT'); Lt.energy = 6; Lt.color = lin(hx(COL['sun'])); Lt.shadow_soft_size = 0.15
        lo = bpy.data.objects.new('ic_L', Lt); lo.location = (0.22,0.22,H+0.1); scene.collection.objects.link(lo); link(lo,'iconos')

def icono_juegos(on):
    body = sphere(r=0.33, seg=40, loc=(0,0,0.22), scale=(1,1,1.25), coll='iconos', name='ic_cohete'); organic(body,0.012,0.5,1); mat(body, clay('m_raz', COL['raz'], bump=0.8, sss=0.25, dents=0.6, dent_scale=7))
    nose = sphere(r=0.2, seg=32, loc=(0,0,0.58), scale=(1,1,0.8), coll='iconos', name='ic_nariz'); mat(nose, clay('m_pared', COL['cream'], bump=0.8, sss=0.3, dents=0.6, dent_scale=7))
    ring = torus(R=0.3, r=0.035, loc=(0,0,-0.12), coll='iconos', name='ic_aro'); mat(ring, clay('m_razDk', COL['razDk'], bump=0.3))
    fw = Vector((1,-1,0)).normalized()   # hacia la cámara
    win = cyl(r=0.12, d=0.06, verts=32, loc=tuple(Vector((0,0,0.26)) + fw*0.3), coll='iconos', name='ic_ventana'); win.rotation_euler = fw.to_track_quat('Z','Y').to_euler(); bevel(win,0.015,2)
    mat(win, clay('m_ic_win_on' if on else 'm_ic_win', COL['window'] if on else COL['cream'], bump=0.2, sss=0.2, vary=0, emit=(COL['window'] if on else None), emit_strength=1.6))
    for k in range(3):
        a = math.radians(45 + 90 + k*120)
        fin = box((0.3,0.06,0.3), loc=(0.3*math.cos(a), 0.3*math.sin(a), -0.08), rot=(0,0,math.degrees(a)), coll='iconos', name='ic_aleta'); bevel(fin,0.04,3); mat(fin, clay('m_sun', COL['sun'], bump=0.4, sss=0.2))
    if on:
        fl = sphere(r=0.15, seg=24, loc=(0,0,-0.3), scale=(1,1,1.3), coll='iconos', name='ic_llama'); organic(fl,0.02,0.3,1); mat(fl, clay('m_llama', '#FFB84A', bump=0.2, sss=0.3, vary=0, emit='#FF9A3C', emit_strength=2.2))

def icono_avance(on):
    base = box((0.8,0.8,0.14), loc=(0,0,-0.37), coll='iconos', name='ic_base'); bevel(base,0.06,3); mat(base, clay('m_losa', '#F1DFBF', bump=0.55, sss=0.3, vary=0.03, dents=1.0, dent_scale=2.0, grain=0.0, mid=0.3, dent_fine=0.2))
    hill = sphere(r=0.46, seg=40, loc=(0,0,-0.3), scale=(1.15,1.15,1.25), coll='iconos', name='ic_cumbre'); organic(hill,0.02,0.5,1); mat(hill, clay('m_lenRoof', '#63B884', bump=0.5, sss=0.25, dents=0.5, dent_scale=6))
    pole = cyl(r=0.024, d=0.46, verts=12, loc=(0,0,0.46), coll='iconos', name='ic_asta'); mat(pole, clay('m_ink', COL['ink'], bump=0, sss=0, vary=0))
    flag = box((0.3,0.03,0.18), loc=(0.16,0,0.6), coll='iconos', name='ic_banderin'); bevel(flag,0.015,2); organic(flag,0.01,0.3,1); mat(flag, clay('m_sun_on' if on else 'm_sun', COL['sun'], bump=0.4, sss=0.2, emit=(COL['sun'] if on else None), emit_strength=0.9))
    if on:
        bulb = sphere(r=0.075, loc=(0,0,0.76), coll='iconos', name='ic_luz'); mat(bulb, clay('m_bulb_on', COL['sunLt'], bump=0, sss=0.2, vary=0, emit='#FFB84A', emit_strength=3.2))
        Lt = bpy.data.lights.new('ic_L','POINT'); Lt.energy = 7; Lt.color = lin(hx(COL['sun'])); Lt.shadow_soft_size = 0.15
        lo = bpy.data.objects.new('ic_L', Lt); lo.location = (0,0,0.78); scene.collection.objects.link(lo); link(lo,'iconos')

def glass():
    # «vidrio» de plasticina translúcida: no refleja la noche; deja ver lo de adentro con un velo celeste
    m = bpy.data.materials.new('vidrio'); m.use_nodes = True; b = m.node_tree.nodes.get('Principled BSDF')
    b.inputs['Base Color'].default_value = (*lin(hx('#DDEFFA')),1); b.inputs['Roughness'].default_value = 0.35; b.inputs['Alpha'].default_value = 0.42
    if 'Specular IOR Level' in b.inputs: b.inputs['Specular IOR Level'].default_value = 0.4
    try: m.surface_render_method = 'BLENDED'
    except Exception: pass
    return m

def gores(name, a, b_, n=8):
    # gajos verticales (como un globo de verdad): franjas según el ángulo alrededor del eje Z, en coordenadas de objeto
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree; N = nt.nodes; L = nt.links; b = N.get('Principled BSDF')
    tc = N.new('ShaderNodeTexCoord'); sx = N.new('ShaderNodeSeparateXYZ'); L.new(tc.outputs['Object'], sx.inputs['Vector'])
    at = N.new('ShaderNodeMath'); at.operation = 'ARCTAN2'; L.new(sx.outputs['Y'], at.inputs[0]); L.new(sx.outputs['X'], at.inputs[1])
    mu = N.new('ShaderNodeMath'); mu.operation = 'MULTIPLY'; mu.inputs[1].default_value = n/2; L.new(at.outputs[0], mu.inputs[0])
    sn = N.new('ShaderNodeMath'); sn.operation = 'SINE'; L.new(mu.outputs[0], sn.inputs[0])
    gt = N.new('ShaderNodeMath'); gt.operation = 'GREATER_THAN'; gt.inputs[1].default_value = 0.0; L.new(sn.outputs[0], gt.inputs[0])
    mx = N.new('ShaderNodeMixRGB'); mx.inputs['Color1'].default_value = (*lin(hx(a)),1); mx.inputs['Color2'].default_value = (*lin(hx(b_)),1)
    L.new(gt.outputs[0], mx.inputs['Fac']); L.new(mx.outputs['Color'], b.inputs['Base Color'])
    b.inputs['Roughness'].default_value = 0.6
    if 'Subsurface Weight' in b.inputs: b.inputs['Subsurface Weight'].default_value = 0.25
    return m

def lucero(loc, r, on, name='ic_lucero'):
    o = sphere(r=r, seg=20, loc=loc, coll='iconos', name=name)
    mat(o, clay('m_lucero_on' if on else 'm_lucero', COL['sunLt'] if on else COL['sun'], bump=0.2, sss=0.3, vary=0, emit=('#FFB84A' if on else None), emit_strength=2.6)); return o

def icono_avance_brote(on):
    pot = cyl(r=0.3, d=0.42, verts=40, loc=(0,0,-0.2), coll='iconos', name='ic_maceta'); pot.scale = (1,1,1); bevel(pot,0.04,3); organic(pot,0.012,0.5,1)
    mat(pot, clay('m_maceta', '#E0A77A', bump=0.7, sss=0.25, dents=0.6, dent_scale=7))
    rim = torus(R=0.3, r=0.05, loc=(0,0,0.0), coll='iconos', name='ic_borde'); mat(rim, clay('m_maceta', '#E0A77A', bump=0.7, sss=0.25, dents=0.6, dent_scale=7))
    soil = cyl(r=0.25, d=0.06, verts=32, loc=(0,0,0.02), coll='iconos', name='ic_tierra'); mat(soil, clay('m_tierra', '#8A6A4E', bump=0.8, sss=0.1))
    stem = cyl(r=0.035, d=0.5, verts=12, loc=(0,0,0.27), coll='iconos', name='ic_tallo'); bevel(stem,0.01,2); mat(stem, clay('m_tallo', '#5EA86E', bump=0.4, sss=0.3))
    leafm = clay('m_hoja', '#8FD9B0', bump=0.5, sss=0.4)
    for k,(dx,dz,rz) in enumerate([(-0.16,0.25,30),(0.17,0.36,-30)]):
        lf = sphere(r=0.13, seg=24, loc=(dx,0,dz), scale=(1.4,0.5,1), coll='iconos', name='ic_hoja'); lf.rotation_euler = (0,0,math.radians(rz)); organic(lf,0.01,0.3,1); mat(lf, leafm)
    if on:  # elegido: hoja nueva arriba y flor redonda encendida
        lf = sphere(r=0.11, seg=24, loc=(-0.1,0,0.5), scale=(1.3,0.5,1), coll='iconos', name='ic_hoja_nueva'); lf.rotation_euler = (0,0,math.radians(40)); mat(lf, leafm)
        lucero((0.02,0,0.6), 0.1, True, 'ic_flor')
        for k in range(5):
            a = k*math.tau/5; pt = sphere(r=0.06, seg=16, loc=(0.02+0.12*math.cos(a), -0.02, 0.6+0.12*math.sin(a)), coll='iconos', name='ic_petalo'); mat(pt, clay('m_rose', COL['rose'], bump=0.3, sss=0.4))
    else:
        bud = sphere(r=0.07, seg=16, loc=(0,0,0.54), scale=(1,1,1.3), coll='iconos', name='ic_brote'); mat(bud, clay('m_tallo', '#5EA86E', bump=0.4, sss=0.3))

def icono_avance_globo(on):
    bal = sphere(r=0.33, seg=48, loc=(0,0,0.3), scale=(1,1,1.15), coll='iconos', name='ic_globo'); organic(bal,0.01,0.5,1)
    bal.data.materials.append(gores('m_globo', COL['rose'], COL['cream'], n=8))
    if on:  # el globo brilla un poco al elegirse
        b = bal.data.materials[0].node_tree.nodes.get('Principled BSDF'); b.inputs['Emission Color'].default_value = (*lin(hx('#FFE0B0')),1); b.inputs['Emission Strength'].default_value = 0.35
    neck = cyl(r=0.09, d=0.08, verts=24, loc=(0,0,-0.1), coll='iconos', name='ic_cuello'); mat(neck, clay('m_razDk', COL['razDk'], bump=0.3))
    bask = box((0.22,0.22,0.16), loc=(0,0,-0.4), coll='iconos', name='ic_canasta'); bevel(bask,0.03,3); organic(bask,0.01,0.3,1); mat(bask, clay('m_canasta', COL['tanDk'], bump=0.8, sss=0.2, dents=0.5, dent_scale=8))
    for sx,sy in ((-1,-1),(1,-1),(1,1),(-1,1)):
        a = Vector((sx*0.1, sy*0.1, -0.32)); b_ = Vector((sx*0.07, sy*0.07, -0.12)); d = b_-a
        rope = cyl(r=0.012, d=d.length, verts=8, loc=tuple((a+b_)/2), coll='iconos', name='ic_cuerda'); rope.rotation_euler = d.to_track_quat('Z','Y').to_euler(); mat(rope, clay('m_ink', COL['ink'], bump=0, sss=0, vary=0))
    if on:
        fl = sphere(r=0.07, seg=20, loc=(0,0,-0.22), scale=(1,1,1.3), coll='iconos', name='ic_llama'); mat(fl, clay('m_llama', '#FFB84A', bump=0.2, sss=0.3, vary=0, emit='#FF9A3C', emit_strength=3.0))
        Lt = bpy.data.lights.new('ic_L','POINT'); Lt.energy = 4; Lt.color = lin(hx(COL['sun'])); Lt.shadow_soft_size = 0.1
        lo = bpy.data.objects.new('ic_L', Lt); lo.location = (0,0,-0.2); scene.collection.objects.link(lo); link(lo,'iconos')

def icono_avance_frasco(on):
    jar = cyl(r=0.27, d=0.62, verts=48, loc=(0,0,-0.08), coll='iconos', name='ic_frasco'); bevel(jar,0.06,4); mat(jar, glass())
    lid = cyl(r=0.2, d=0.1, verts=32, loc=(0,0,0.28), coll='iconos', name='ic_tapa'); bevel(lid,0.03,3); mat(lid, clay('m_tapa', COL['tanDk'], bump=0.8, sss=0.2, dents=0.5, dent_scale=8))
    knob = sphere(r=0.06, seg=16, loc=(0,0,0.36), coll='iconos', name='ic_perilla'); mat(knob, clay('m_tapa', COL['tanDk'], bump=0.8, sss=0.2, dents=0.5, dent_scale=8))
    rnd = random.Random(7)
    pts = [(-0.1,-0.06,-0.28),(0.1,0.04,-0.3),(0.0,-0.1,-0.12),(-0.12,0.08,-0.05),(0.12,-0.04,0.05),(0.0,0.06,0.14)]
    for k,(x,y,z) in enumerate(pts[: (6 if on else 4)]):
        lucero((x,y,z), 0.065, on)
    if on:
        Lt = bpy.data.lights.new('ic_L','POINT'); Lt.energy = 3; Lt.color = lin(hx(COL['sun'])); Lt.shadow_soft_size = 0.2
        lo = bpy.data.objects.new('ic_L', Lt); lo.location = (0,0,-0.1); scene.collection.objects.link(lo); link(lo,'iconos')

def icono_avance_farol(on):
    base = cyl(r=0.2, d=0.08, verts=32, loc=(0,0,-0.4), coll='iconos', name='ic_pie'); bevel(base,0.02,2); mat(base, clay('m_tanDk', COL['tanDk'], bump=0.5, sss=0.1))
    post = cyl(r=0.05, d=0.3, verts=16, loc=(0,0,-0.22), coll='iconos', name='ic_poste'); mat(post, clay('m_tanDk', COL['tanDk'], bump=0.5, sss=0.1))
    cage = cyl(r=0.2, d=0.4, verts=40, loc=(0,0,0.12), coll='iconos', name='ic_vidrio'); mat(cage, glass())
    for k in range(4):
        a = k*math.tau/4 + math.radians(45)
        bar = cyl(r=0.022, d=0.42, verts=10, loc=(0.2*math.cos(a), 0.2*math.sin(a), 0.12), coll='iconos', name='ic_varilla'); mat(bar, clay('m_tanDk', COL['tanDk'], bump=0.4, sss=0.1))
    cap = sphere(r=0.24, seg=32, loc=(0,0,0.33), scale=(1,1,0.45), coll='iconos', name='ic_sombrero'); mat(cap, clay('m_tanDk', COL['tanDk'], bump=0.6, sss=0.1, dents=0.5, dent_scale=8))
    ring = torus(R=0.06, r=0.015, loc=(0,0,0.46), rot=(90,0,0), coll='iconos', name='ic_argolla'); mat(ring, clay('m_tanDk', COL['tanDk'], bump=0.3))
    lucero((0,0,0.1), 0.1, on, 'ic_luz')
    if on:
        Lt = bpy.data.lights.new('ic_L','POINT'); Lt.energy = 5; Lt.color = lin(hx(COL['sun'])); Lt.shadow_soft_size = 0.15
        lo = bpy.data.objects.new('ic_L', Lt); lo.location = (0,0,0.1); scene.collection.objects.link(lo); link(lo,'iconos')

def fondo_lucero():
    o = sphere(r=0.3, seg=48, loc=(0,0,0), coll='iconos', name='fo_lucero'); organic(o,0.012,0.3,1); mat(o, clay('m_lucero_f', '#FBEBCB', bump=0.5, sss=0.35, vary=0.03, dents=0.5, dent_scale=8, grain=0.1, ao=0.2, ao_cool=1.0))   # bolita de plasticina crema clara, con huella fina
def fondo_planeta():
    o = sphere(r=0.42, seg=48, loc=(0,0,0), coll='iconos', name='fo_planeta'); organic(o,0.015,0.5,1); mat(o, clay('m_planeta', '#F3C9A8', bump=0.6, sss=0.3, dents=0.5, dent_scale=6))
    r = torus(R=0.72, r=0.07, loc=(0,0,0), coll='iconos', name='fo_anillo'); r.scale = (1,1,0.35); r.rotation_euler = (math.radians(18), math.radians(-10), 0); mat(r, clay('m_anillo', '#C9B8F0', bump=0.4, sss=0.3))
def fondo_nubecita():
    for (dx,dz,rr) in ((-0.3,0,0.24),(0.05,0.1,0.3),(0.38,-0.02,0.22)):
        o = sphere(r=rr, seg=32, loc=(dx,0,dz), coll='iconos', name='fo_nube'); organic(o,0.01,0.3,1); mat(o, clay('m_nubecita', '#9A90C4', bump=0.4, sss=0.4, dents=0.3, dent_scale=6))

def render_fondo():
    camera_icono()
    for name, fn, res in (('fondo_lucero', fondo_lucero, 260), ('fondo_planeta', fondo_planeta, 420), ('fondo_nubecita', fondo_nubecita, 300)):
        scene.render.resolution_x = res; scene.render.resolution_y = res
        before = set(bpy.data.objects); fn()
        times[name] = render(name, {'iconos','luces'})
        for o in [o for o in bpy.data.objects if o not in before]: bpy.data.objects.remove(o, do_unlink=True)

# ------------------------------------------------------------------ fondo como TABLERO físico: lámina de plasticina azul noche con luceros apoyados
TAB_W, TAB_H = 6.0, 10.0   # proporción de teléfono 3:5 (unidades de escena)
TEXTO = (0.04, 0.08, 0.70, 0.22); BARRA_Z = (0.0, 0.88, 1.0, 1.0)   # zonas (x0,y0,x1,y1 en fracción, y hacia abajo) sin luceros: saludo y barra
# Tarea 83: los luceros son una capa APARTE (la lámina y los luceros se apilan en la app). Tarea 87: la capa va FIJA detrás de las tres pestañas (Hoy, Juegos y Avance), ya sin fundido por pestaña.
#   --sin-luceros   renderiza solo la lámina (azul marino con su nebulosa y su textura) → fondo_lamina.png, opaca.
#   --solo-luceros  renderiza solo los luceros con su sombra de contacto → fondo_luceros.png, transparente: la lámina es «shadow catcher» de Cycles (invisible, pero recibe la sombra y el rebote).
#   --b-aprobado    no quita los luceros de la zona fija: reproduce el B aprobado de la ronda 26b con sus primeros 58 luceros (la secuencia sigue hasta N_LUCEROS).
# Tarea 87 (Ricardo encontró Hoy «muy vacío»): ~80 luceros visibles en vez de 17. Zona libre de luceros SOLO bajo los textos grandes que caen siempre en el mismo lugar: el saludo de Hoy y los títulos de Juegos y Avance, que están en la banda de arriba.
# Bajo botones, piedritas y tarjetas no se excluye nada (son opacas y los tapan); los textos claros que van directo sobre el fondo llevan un halo navy (`OnBoardText`). Los luceros de esa banda se QUITAN sin tocar la secuencia de colocación (semilla 5):
# los primeros 58 quedan exactamente donde estaban en el B aprobado y la secuencia sigue hasta N_LUCEROS.
ZONAS_HOY = (
  (0.0, 0.03, 1.0, 0.28),    # encabezado a todo el ancho: saludo de Hoy (a 360 dp en dos líneas), fecha con la meta, farolitos y racha (el texto llega a y 22,5 %), «Tu avance» (y 5-8 %) y «¿Qué jugamos hoy?» con su sugerencia (y 10-20 %); margen para barras de estado más altas. Se mide en las pruebas
)
N_LUCEROS = 95   # 95 colocados: ~80 visibles (los demás caen en la banda de arriba o en la de la barra)
SIN_LUCEROS = '--sin-luceros' in args; SOLO_LUCEROS = '--solo-luceros' in args; B_APROBADO = '--b-aprobado' in args
def tablero():
    lam = box((TAB_W, TAB_H, 0.3), loc=(0,0,-0.15), coll='iconos', name='tab_lamina'); bevel(lam, 0.06, 3)
    m = clay('m_lamina', '#1C1A50' if LAMINA == 'A' else '#23255A', bump=0.7, sss=0.1, vary=0.05, rough=0.72, spec=0.22, dents=1.0, dent_scale=1.6, grain=0.2, mid=0.4, sat=1.0, piece=0, chalk=0, prints=0.15)   # A: lila actual; B: azul noche como la referencia y el fondo de la app
    nt = m.node_tree; N = nt.nodes; L = nt.links; b = N.get('Principled BSDF')   # nebulosa suave pintada en la lámina: dos manchas grandes lila y azul
    tc = N.new('ShaderNodeTexCoord'); nz = N.new('ShaderNodeTexNoise'); nz.inputs['Scale'].default_value = 0.22; nz.inputs['Detail'].default_value = 2; L.new(tc.outputs['Object'], nz.inputs['Vector'])
    c0, c1 = ('#1F1B55', '#5A4FA8') if LAMINA == 'A' else ('#1E2152', '#2E3A8C')   # B: azul marino sin violeta (medido en la referencia, ~#23255A)
    ramp = N.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].position = 0.40; ramp.color_ramp.elements[0].color = (*lin(hx(c0)),1); ramp.color_ramp.elements[1].position = 0.64; ramp.color_ramp.elements[1].color = (*lin(hx(c1)),1)
    L.new(nz.outputs['Fac'], ramp.inputs['Fac'])
    prev = b.inputs['Base Color'].links[0].from_socket
    mix = N.new('ShaderNodeMixRGB'); mix.blend_type = 'MIX'; mix.inputs['Fac'].default_value = 0.5; L.new(prev, mix.inputs['Color1']); L.new(ramp.outputs['Color'], mix.inputs['Color2']); L.new(mix.outputs['Color'], b.inputs['Base Color'])
    mat(lam, m)
    if SOLO_LUCEROS: lam.is_shadow_catcher = True   # tarea 83: la lámina no se ve, solo recibe la sombra y el rebote de cada lucero
    # luceros: bolitas de plasticina crema medio hundidas, 3 tamaños, sin amontonarse, fuera del saludo y de la barra (incluido su borde)
    rnd = random.Random(5); puestos = []; TAM = ((0.042, 0.55), (0.06, 0.32), (0.085, 0.13)); n = 0   # ronda 26: puntitos, no perlas
    # bolitas chicas, como las de la referencia (1-2 % del ancho del teléfono)
    mlu = clay('m_lucero_t', '#F3DFBF', bump=0.5, sss=0.3, vary=0.03, dents=0.5, dent_scale=12, grain=0.1, ao=0.2, ao_cool=1.0, sat=1.0, rough=0.7, spec=0.2)
    def zona(fx, fy, z): return z[0] <= fx <= z[2] and z[1] <= fy <= z[3]
    while n < N_LUCEROS and not SIN_LUCEROS:
        u = rnd.random(); r = (TAM[0][0] if u < 0.55 else TAM[1][0] if u < 0.87 else TAM[2][0]) * rnd.uniform(0.9, 1.1)
        fx, fy = rnd.random(), rnd.random(); x, y = (fx - 0.5) * TAB_W, (0.5 - fy) * TAB_H
        fr = r / TAB_W
        if any(zona(fx+dx, fy+dy*TAB_W/TAB_H, TEXTO) or zona(fx+dx, fy+dy*TAB_W/TAB_H, BARRA_Z) for dx in (-fr,0,fr) for dy in (-fr,0,fr)): continue
        if any((x-px)**2 + (y-py)**2 < (r*4.5)**2 for px, py in puestos): continue
        puestos.append((x, y)); n += 1
        if not B_APROBADO and any(zona(fx+dx, fy+dy*TAB_W/TAB_H, z) for z in ZONAS_HOY for dx in (-fr,0,fr) for dy in (-fr,0,fr)): continue   # tarea 83: cuenta para la secuencia pero no se crea
        o = sphere(r=r, seg=32, loc=(x, y, r*0.62), coll='iconos', name='tab_lucero'); organic(o, 0.012, 0.3, 1); mat(o, mlu)   # ronda 25: menos hundido, para que su sombra se vea

def render_tablero():
    tablero()
    cam = bpy.data.cameras.new('cam_tab'); cam.type = 'ORTHO'; cam.ortho_scale = TAB_H
    co = bpy.data.objects.new('cam_tab', cam); co.location = (0,0,12); co.rotation_euler = (0,0,0); scene.collection.objects.link(co); scene.camera = co
    for o in C['luces'].objects:
        if o.type == 'LIGHT' and o.data.type == 'SUN': o.data.energy = float(arg('--tabsol','2.0')); o.rotation_euler = Vector((1.0, -0.6, -1.0)).to_track_quat('-Z','Y').to_euler()   # en el tablero (visto desde arriba) la sombra cae abajo a la derecha de cada lucero   # ronda 25: más sol que en la 24 (1,1) para que cada lucero deje sombra; la luz central baja a cambio
    for nm in ('rebote','nubi_relleno','nubi_rebote'):   # el rebote (área bajo la losa, apuntando arriba) atraviesa la lámina y deja una diagonal nítida en la esquina; las luces de Nubi no pintan nada aquí
        if nm in bpy.data.objects: bpy.data.objects.remove(bpy.data.objects[nm], do_unlink=True)
    L1 = bpy.data.lights.new('tab_centro','AREA'); L1.energy = float(arg('--tabcentro','260')); L1.size = 4.5; L1.color = lin(hx('#EEF0FF'))   # caída de luz y viñeta naturales: una luz sobre el centro de la lámina
    lo = bpy.data.objects.new('tab_centro', L1); lo.location = (0.6, 1.5, 7.5); scene.collection.objects.link(lo); link(lo,'luces'); lo.rotation_euler = (0,0,0)
    scene.render.resolution_x = 720; scene.render.resolution_y = 1200; scene.render.film_transparent = SOLO_LUCEROS
    for nm in arg('--sin','').split(','):
        if nm and nm in bpy.data.objects: bpy.data.objects.remove(bpy.data.objects[nm], do_unlink=True); print('[isla] sin', nm)
    scene.render.resolution_percentage = int(arg('--pct','100'))
    if arg('--exec',''): exec(arg('--exec',''))   # pruebas: código arbitrario justo antes de renderizar
    times['tablero'] = render('fondo_lamina' if SIN_LUCEROS else 'fondo_luceros' if SOLO_LUCEROS else 'fondo_tablero', {'iconos','luces'})


# ------------------------------------------------------------------ íconos de la barra, ronda 25: siluetas que se reconozcan a 32 dp
def normaliza(objs, target_h, z0):
    # escala uniforme para que el ícono mida target_h de alto, apoyado en z0 y centrado en x,y (luces incluidas)
    bpy.context.view_layer.update(); cs = []
    for o in objs:
        if o.type == 'MESH' and not o.name.startswith('ic_llama'): cs += [o.matrix_world @ Vector(c) for c in o.bound_box]   # la llamita no cuenta: el cohete mide igual encendido y apagado
    if not cs: return
    zmin = min(c.z for c in cs); zmax = max(c.z for c in cs); cx = (min(c.x for c in cs) + max(c.x for c in cs))/2; cy = (min(c.y for c in cs) + max(c.y for c in cs))/2
    sf = target_h / max(1e-6, zmax - zmin)
    for o in objs:
        o.scale = (o.scale.x*sf, o.scale.y*sf, o.scale.z*sf); o.location = ((o.location.x-cx)*sf, (o.location.y-cy)*sf, (o.location.z-zmin)*sf + z0)
        if o.type == 'LIGHT': o.scale = (1,1,1); o.data.shadow_soft_size *= sf
    return sf

def luz_icono(loc, energy, color=None):
    Lt = bpy.data.lights.new('ic_L','POINT'); Lt.energy = energy; Lt.color = lin(hx(color or COL['sun'])); Lt.shadow_soft_size = 0.12
    lo = bpy.data.objects.new('ic_L', Lt); lo.location = loc; scene.collection.objects.link(lo); link(lo,'iconos'); return lo

def icono_hoy3(on):
    # isla en miniatura: tierra redonda, losa crema, cúpula azul adelante y torrecita con su lucero atrás; cabe entera
    tierra = box((0.78,0.78,0.26), loc=(0,0,0.13), coll='iconos', name='ic_tierra'); bevel(tierra,0.11,5); organic(tierra,0.02,0.6,1); mat(tierra, clay('m_tan', COL['tan'], bump=0.8, sss=0.1, vary=0.08, dents=1.0, dent_scale=2.8, grain=0.1, mid=0.5))
    losa = box((0.9,0.9,0.2), loc=(0,0,0.34), coll='iconos', name='ic_losa'); bevel(losa,0.09,5); organic(losa,0.015,0.5,2); mat(losa, clay('m_losa', '#F1DFBF', bump=0.55, sss=0.3, vary=0.03, dents=1.0, dent_scale=2.0, grain=0.0, mid=0.3, dent_fine=0.2))
    ob = cyl(r=0.21, d=0.2, loc=(-0.2,-0.16,0.54), coll='iconos', name='ic_obs'); bevel(ob,0.03,2); mat(ob, clay('m_pared', COL['cream'], bump=0.8, sss=0.3, dents=0.6, dent_scale=7))
    dm = sphere(r=0.23, loc=(-0.2,-0.16,0.64), scale=(1,1,0.9), coll='iconos', name='ic_cupula'); mat(dm, clay('m_mem', '#7FB0EE', bump=0.7, sss=0.3, dents=0.5, dent_scale=7))
    legm = clay('m_ate', COL['ate'], bump=0.5, sss=0.2); H = 0.5; tx, ty, tz = 0.22, 0.17, 0.44
    for sx,sy in ((-1,-1),(1,-1),(1,1),(-1,1)):
        a = Vector((tx+sx*0.1, ty+sy*0.1, tz)); b_ = Vector((tx+sx*0.035, ty+sy*0.035, tz+H)); d = b_-a
        leg = cyl(r=0.03, d=d.length, verts=10, loc=tuple((a+b_)/2), coll='iconos', name='ic_pata'); leg.rotation_euler = d.to_track_quat('Z','Y').to_euler(); mat(leg, legm)
    bulb = sphere(r=0.1, loc=(tx,ty,tz+H+0.08), coll='iconos', name='ic_luz'); mat(bulb, clay('m_bulb_on' if on else 'm_bulb_off', COL['sunLt'] if on else '#E8D6B2', bump=0, sss=0.2, vary=0, emit=('#FFB84A' if on else None), emit_strength=3.2))
    if on: luz_icono((tx,ty,tz+H+0.08), 6)

def icono_juegos3(on):
    # cohete vertical: cuerpo 2,2 veces más alto que ancho, nariz redondeada, ventanilla al frente, tres aletas gordas, tobera; elegido: ventanilla y llamita encendidas
    R = 0.2
    body = cyl(r=R, d=0.62, verts=40, loc=(0,0,0.45), coll='iconos', name='ic_cuerpo'); bevel(body,0.07,6); organic(body,0.01,0.6,1); mat(body, clay('m_raz', COL['raz'], bump=0.8, sss=0.25, dents=0.6, dent_scale=7))
    nose = sphere(r=R*1.02, seg=40, loc=(0,0,0.76), scale=(1,1,0.9), coll='iconos', name='ic_nariz'); organic(nose,0.01,0.5,1); mat(nose, clay('m_pared', COL['cream'], bump=0.8, sss=0.3, dents=0.6, dent_scale=7))
    win = cyl(r=0.1, d=0.05, verts=32, loc=(0,-R+0.005,0.52), coll='iconos', name='ic_ventana'); win.rotation_euler = (math.radians(90),0,0); bevel(win,0.012,2)
    mat(win, clay('m_ic_win_on' if on else 'm_ic_win', COL['window'] if on else '#DCEFF8', bump=0.2, sss=0.2, vary=0, emit=(COL['window'] if on else None), emit_strength=1.8))
    ring = torus(R=0.11, r=0.028, loc=(0,-R-0.01,0.52), rot=(90,0,0), coll='iconos', name='ic_aro'); mat(ring, clay('m_razDk', COL['razDk'], bump=0.3))
    for a_deg in (210, 330, 90):
        a = math.radians(a_deg)
        fin = box((0.2,0.08,0.3), loc=((R+0.07)*math.cos(a), (R+0.07)*math.sin(a), 0.22), rot=(0,0,a_deg), coll='iconos', name='ic_aleta'); bevel(fin,0.035,4); organic(fin,0.008,0.5,1); mat(fin, clay('m_sun', COL['sun'], bump=0.4, sss=0.2))
    noz = cyl(r=0.11, d=0.08, verts=24, loc=(0,0,0.11), coll='iconos', name='ic_tobera'); bevel(noz,0.02,2); mat(noz, clay('m_razDk', COL['razDk'], bump=0.3))
    if on:
        fl = sphere(r=0.1, seg=24, loc=(0,0,0.0), scale=(1,1,1.4), coll='iconos', name='ic_llama'); organic(fl,0.02,0.3,1); mat(fl, clay('m_llama', '#FFB84A', bump=0.2, sss=0.3, vary=0, emit='#FF9A3C', emit_strength=2.4))
        luz_icono((0,0,0.0), 4, '#FFB060'); luz_icono((0,-R-0.1,0.52), 2.5, COL['window'])

def icono_avance_farol3(on):
    # farol completo: base, pie, cuerpo de vidrio con cuatro montantes de churro, vela con su luz, techito con argolla
    W = clay('m_tanDk', COL['tanDk'], bump=0.5, sss=0.1)
    base = cyl(r=0.21, d=0.07, verts=36, loc=(0,0,0.035), coll='iconos', name='ic_pie'); bevel(base,0.025,3); mat(base, W)
    foot = cyl(r=0.12, d=0.07, verts=24, loc=(0,0,0.1), coll='iconos', name='ic_pie2'); mat(foot, W)
    vid = cyl(r=0.17, d=0.46, verts=40, loc=(0,0,0.37), coll='iconos', name='ic_vidrio'); mat(vid, glass())
    for k in range(4):
        a = k*math.tau/4 + math.radians(45)
        bar = cyl(r=0.028, d=0.48, verts=12, loc=(0.17*math.cos(a), 0.17*math.sin(a), 0.37), coll='iconos', name='ic_montante'); organic(bar,0.004,2.0,1); mat(bar, W)
    top = cyl(r=0.21, d=0.05, verts=36, loc=(0,0,0.625), coll='iconos', name='ic_tapa'); bevel(top,0.02,3); mat(top, W)
    cap = sphere(r=0.25, seg=32, loc=(0,0,0.64), scale=(1,1,0.5), coll='iconos', name='ic_sombrero'); organic(cap,0.01,0.5,1); mat(cap, clay('m_tanDk_d', COL['tanDk'], bump=0.6, sss=0.1, dents=0.5, dent_scale=8))
    knob = sphere(r=0.045, seg=20, loc=(0,0,0.78), coll='iconos', name='ic_boton'); mat(knob, W)
    ring = torus(R=0.055, r=0.014, loc=(0,0,0.865), rot=(90,0,0), coll='iconos', name='ic_argolla'); mat(ring, W)
    vela = cyl(r=0.05, d=0.14, verts=20, loc=(0,0,0.21), coll='iconos', name='ic_vela'); mat(vela, clay('m_vela', '#FBEBCB', bump=0.3, sss=0.3, vary=0.02))
    lucero((0,0,0.33), 0.085, on, 'ic_luz')
    if on: luz_icono((0,0,0.34), 7)

# ------------------------------------------------------------------ barra: bandeja de plasticina crema con 3 casillas que sobresalen, en 2 estados
ICONO_H = 0.44; CUBO = 0.44; CUBO_BEV = 0.135; SEP = 1.35; CUBO_GIRO = 28   # ronda 26: cubos blandos en isométrica (misma cámara que la isla), ícono de 0,32 encima
def cam_iso(target=(0,0,0.3), dist=8.0, ortho=1.6, name='cam_bar'):
    cam = bpy.data.cameras.new(name); cam.type = 'ORTHO'; cam.ortho_scale = ortho
    e = math.atan2(14.5-0.7, math.hypot(12.8,12.8)); d = Vector((0, -math.cos(e), math.sin(e)))   # misma elevación que la cámara de la isla (~37°), pero de frente: la bandeja queda horizontal en pantalla; los cubos van girados
    co = bpy.data.objects.new(name, cam); scene.collection.objects.link(co); co.location = tuple(Vector(target) + d*dist)
    co.rotation_euler = (Vector(target) - co.location).to_track_quat('-Z','Y').to_euler(); scene.camera = co; return cam
def casilla(on, icon_fn, at=(0,0,0)):
    # cubo blando de canto muy redondo con su ícono encima; elegido: caramelo, hundido, resplandor cálido en la base y la luz del ícono encendida
    m = clay('m_cubo_on' if on else 'm_cubo', '#D7A15E' if on else '#F3E6CC', bump=0.45, sss=0.25, vary=0.03, rough=0.5, dents=0.5, dent_scale=4.0, grain=0.05, mid=0.3, ao=0.25, ao_cool=1.0, sat=1.0, emit=('#E79A3E' if on else None), emit_strength=0.2)
    h = CUBO - 0.07 if on else CUBO; x, y, z0 = at
    c = box((CUBO,CUBO,h), loc=(x,y,z0+h/2), rot=(0,0,CUBO_GIRO), coll='iconos', name='cas_cuerpo'); bevel(c, CUBO_BEV, 8); organic(c, 0.005, 0.8, 2); mat(c, m)   # girado: se ven la tapa, el frente y un costado, como en la referencia
    if on:
        g = cyl(r=CUBO*0.8, d=0.012, verts=40, loc=(x,y,z0+0.006), coll='iconos', name='cas_glow'); mat(g, clay('m_glow', '#FFC068', bump=0, sss=0, vary=0, emit='#FFB04A', emit_strength=4.0, sat=1.0, piece=0, chalk=0, sheen=0, prints=0))
        Lw = bpy.data.lights.new('cas_L','POINT'); Lw.energy = 2.5; Lw.color = lin(hx('#FFB55A')); Lw.shadow_soft_size = 0.3
        lo = bpy.data.objects.new('cas_L', Lw); lo.location = (x, y-0.1, z0+0.05); scene.collection.objects.link(lo); link(lo,'iconos')
    before = set(bpy.data.objects); icon_fn(on)
    objs = [o for o in bpy.data.objects if o not in before and o.type in ('MESH','LIGHT')]
    normaliza(objs, ICONO_H, z0 + h); ga = math.radians(CUBO_GIRO); ca, sa = math.cos(ga), math.sin(ga)
    for o in objs:   # el ícono gira con el cubo
        ox, oy = o.location.x, o.location.y; o.location = (x + ox*ca - oy*sa, y + ox*sa + oy*ca, o.location.z); o.rotation_euler = (o.rotation_euler.x, o.rotation_euler.y, o.rotation_euler.z + ga)
def bandeja(w=4.4, d=0.9, h=0.16):
    t = cushion(w, d, h, r_plan=0.3, bev=0.07, seg=6, name='bandeja', coll='iconos'); organic(t, 0.004, 1.3, 1)
    mat(t, clay('m_bandeja', '#F1DFBF', bump=0.5, sss=0.25, vary=0.03, rough=0.5, dents=0.6, dent_scale=2.0, grain=0.0, mid=0.3, ao=0.3, ao_cool=1.0, sat=1.0, piece=0)); return t

def render_barra():
    scene.render.resolution_percentage = int(arg('--pct','100'))
    fns = (('hoy', icono_hoy3), ('juegos', icono_juegos3), ('avance', icono_avance_farol3))
    # barra completa por estado (bandeja + tres cubos con sus íconos) al 2× del teléfono: 786 px de ancho
    cam = cam_iso(target=(0,0,0.25), ortho=5.0); scene.render.resolution_x = 786; scene.render.resolution_y = 420
    for elegido, _ in fns:
        before = set(bpy.data.objects); bandeja()
        for (k, fn), dx in zip(fns, (-SEP, 0, SEP)): casilla(k == elegido, fn, at=(dx, 0, 0.16))
        times['barra_' + elegido] = render('barra_' + elegido, {'iconos','luces'})
        for o in [o for o in bpy.data.objects if o not in before]: bpy.data.objects.remove(o, do_unlink=True)
    # recursos sueltos para la app: cada casilla en dos estados y la bandeja sola, misma cámara
    cam.ortho_scale = 1.3; scene.render.resolution_x = 400; scene.render.resolution_y = 400
    for k, fn in fns:
        for on in (False, True):
            before = set(bpy.data.objects); casilla(on, fn)
            times['cas_' + k + ('_on' if on else '')] = render('cas_' + k + ('_on' if on else ''), {'iconos','luces'})
            for o in [o for o in bpy.data.objects if o not in before]: bpy.data.objects.remove(o, do_unlink=True)
    cam.ortho_scale = 5.0; scene.render.resolution_x = 786; scene.render.resolution_y = 420
    before = set(bpy.data.objects); bandeja(); times['bandeja'] = render('bandeja', {'iconos','luces'})
    for o in [o for o in bpy.data.objects if o not in before]: bpy.data.objects.remove(o, do_unlink=True)


# ------------------------------------------------------------------ kit de plasticina (Tareas 89 y 90): losas de nine-slice en tres radios, discos redondos, íconos de churro tinta y el triángulo de «Jugar», con dos estados las losas y los discos
# 1 unidad de Blender = 20 dp; se renderiza a 3 px por dp (KIT_PX). Cámara ortogonal, casi desde arriba (KIT_TILT grados de inclinación: se ve un poco el canto de adelante); el sol de la isla (arriba a la izquierda) deja la sombra de contacto abajo a la derecha.
# `--kit-solo losas|discos|iconos` renderiza solo una parte (todo por defecto).
KIT_DPU = 20.0; KIT_PX = 3.0; KIT_TILT = float(arg('--kit-tilt','30')); KIT_SOLO = arg('--kit-solo','todo')
KIT_CW = 24.0; KIT_CH = 8.0                                      # ancho y alto del centro que se estira (dp)
KIT_MARGEN = (8.0, 6.0, 12.0, 14.0)                              # margen de la imagen alrededor del cuerpo: izquierda, arriba, derecha, abajo (dp): ahí cae la sombra
KIT_H = float(arg('--kit-h','9')); KIT_H_P = KIT_H*0.5           # alto de la losa elevada y presionada (dp)
KIT_TINTA = '#140E30'                                            # la tinta del diseño es #211C45: se renderiza más oscura porque con la luz de la isla la plasticina oscura sale ~1,5× más clara
KIT_SOL = '#FFCB45'; KIT_CREMA = '#F5EBDB'; KIT_LIMA = '#9BE564'; KIT_CIELO = '#4CC9F0'; KIT_CORAL = '#FF6B4A'; KIT_LILA = '#B8A4FF'
# radio de las esquinas (dp) → archivo y colores: chica = chips y casillas (14), media = botones y tarjetas (22), grande = láminas y paneles (30)
KIT_LOSAS = (
    ('ch', 14.0, (('crema', KIT_CREMA), ('sol', KIT_SOL), ('lima', KIT_LIMA))),
    ('md', 22.0, (('sol', KIT_SOL), ('crema', KIT_CREMA), ('lima', KIT_LIMA), ('cielo', KIT_CIELO), ('coral', KIT_CORAL), ('lila', KIT_LILA))),
    ('gr', 30.0, (('crema', KIT_CREMA),)),
)

def cam_kit(cx, cy, w_dp, h_dp, name='cam_kit'):
    cam = bpy.data.cameras.new(name); cam.type = 'ORTHO'; cam.ortho_scale = w_dp / KIT_DPU      # la escala ortogonal es el ancho de la imagen en unidades
    t = math.radians(KIT_TILT); d = Vector((0, -math.sin(t), math.cos(t)))
    co = bpy.data.objects.new(name, cam); scene.collection.objects.link(co); tgt = Vector((cx, cy, 0.0)); co.location = tuple(tgt + d*30)
    co.rotation_euler = (tgt - co.location).to_track_quat('-Z','Y').to_euler(); scene.camera = co
    scene.render.resolution_x = int(round(w_dp*KIT_PX)); scene.render.resolution_y = int(round(h_dp*KIT_PX)); return cam

def cam_pieza(margen, bw, bh):
    """La cámara de una pieza de cuerpo bw × bh dp con esos márgenes (izq, arr, der, abajo): apunta al centro del lienzo; `kit.py` alinea después el cuerpo con exactitud."""
    ml, mt, mr, mb = margen; w_dp = ml + bw + mr; h_dp = mt + bh + mb; t = math.radians(KIT_TILT)
    return cam_kit(-((ml + bw/2) - w_dp/2)/KIT_DPU, ((mt + bh/2) - h_dp/2)/(KIT_DPU*math.cos(t)), w_dp, h_dp)

def suelo_kit():
    g = box((24, 24, 0.2), loc=(0, 0, -0.1), coll='iconos', name='kit_suelo'); g.is_shadow_catcher = True; return g

def raiz_kit():
    """Un padre estirado en Y por 1/cos(inclinación): lo que cuelga de él se ve del tamaño pedido en la imagen (el disco redondo, las esquinas redondas) y cada pieza conserva su giro."""
    r = bpy.data.objects.new('kit_raiz', None); scene.collection.objects.link(r); r.scale = (1, 1/math.cos(math.radians(KIT_TILT)), 1); return r

def mat_kit(nombre, color):
    return clay(nombre, color, bump=0.28, sss=0.28, vary=0.012, rough=0.5, dents=0.0, grain=0.0, mid=0.3, ao=0.28, ao_cool=1.0, sat=1.0, piece=0, prints=0.0)

def mat_tinta():
    return clay('m_kit_tinta', KIT_TINTA, bump=0.3, sss=0.15, vary=0.01, rough=0.55, dents=0.0, grain=0.0, ao=0.2, sat=1.0, piece=0, prints=0.0)

def oscurece(color, k=0.93):
    return '#' + ''.join('%02X' % int(c*k) for c in (int(color.strip('#')[i:i+2], 16) for i in (0, 2, 4)))

def losa_kit(color, h_dp, radio, name='kit_losa'):
    """Losa de plasticina de esquinas redondas (radio dp) y canto blando, de (2R + centro) de ancho por (2R + centro) de fondo. Cara casi lisa: el centro se ESTIRA en la app (nine-slice), así que no lleva huellas."""
    t = math.radians(KIT_TILT); body_w = 2*radio + KIT_CW; body_h = 2*radio + KIT_CH
    h = h_dp / KIT_DPU
    d_img = (body_h - h_dp*math.sin(t)) / KIT_DPU                           # lo que ocupa la CARA en la imagen (el canto de adelante suma h·sen)
    o = cushion(body_w/KIT_DPU, d_img, h, r_plan=radio/KIT_DPU, bev=min(h*0.72, radio/KIT_DPU*0.6), seg=8, name=name, coll='iconos', barrel=0.012, rings=2); organic(o, 0.003, 1.4, 1)
    o.scale = (1, 1/math.cos(t), 1)                                          # estirada en Y por 1/cos: vista inclinada, la cara y sus esquinas redondas quedan como pedidas
    mat(o, mat_kit('m_kit_' + color.strip('#') + ('p' if h_dp < KIT_H else ''), color))
    return o

def render_losas():
    ml, mt, mr, mb = KIT_MARGEN
    for sufijo, radio, colores in KIT_LOSAS:
        body_w = 2*radio + KIT_CW; body_h = 2*radio + KIT_CH
        cam_pieza(KIT_MARGEN, body_w, body_h)
        for label, color in colores:
            for presionada in (False, True):
                before = set(bpy.data.objects); suelo_kit()
                losa_kit(oscurece(color) if presionada else color, KIT_H_P if presionada else KIT_H, radio, 'kit_losa_' + label)
                nombre = 'kit_losa_' + sufijo + '_' + label + ('_p' if presionada else '')
                times[nombre] = render(nombre, {'iconos','luces'})
                for o in [o for o in bpy.data.objects if o not in before]: bpy.data.objects.remove(o, do_unlink=True)

def disco_kit(color, presionado, con_x):
    """Disco de plasticina de 48 dp con, si [con_x], una X de dos churros tinta de puntas redondas. Presionado: más bajo, la X más hundida y un punto más oscuro."""
    h = (KIT_H_P if presionado else KIT_H) / KIT_DPU; R = 24.0 / KIT_DPU
    d = cyl(r=R, d=h, verts=64, loc=(0, 0, h/2), coll='iconos', name='kit_disco'); bevel(d, h*0.72, 8); organic(d, 0.003, 1.4, 1)
    mat(d, mat_kit('m_kit_disco_' + color.strip('#') + ('p' if presionado else ''), oscurece(color, 0.93) if presionado else color))
    d.parent = raiz_kit() if False else None
    objs = [d]
    if con_x:
        a = 5.4 / KIT_DPU; r = 2.1 / KIT_DPU; z = h + r*(0.55 if not presionado else 0.2)       # la X asoma de la cara; al presionar se hunde
        for (p0, p1) in (((-a, -a, z), (a, a, z)), ((-a, a, z), (a, -a, z))): objs.append(churro(p0, p1, r, mat_tinta(), coll='iconos', name='kit_x'))
    raiz = raiz_kit()
    for o in objs: o.parent = raiz

def render_discos():
    cam_pieza((6.0, 4.0, 10.0, 12.0), 48.0, 48.0)
    for nombre, color, con_x in (('kit_cerrar', KIT_CREMA, True), ('kit_disco_crema', KIT_CREMA, False), ('kit_disco_cielo', KIT_CIELO, False)):
        for presionado in (False, True):
            before = set(bpy.data.objects); suelo_kit(); disco_kit(color, presionado, con_x)
            n = nombre + ('_p' if presionado else ''); times[n] = render(n, {'iconos', 'luces'})
            for o in [o for o in bpy.data.objects if o not in before]: bpy.data.objects.remove(o, do_unlink=True)

# ---- íconos de churro tinta: se dibujan en el plano del suelo (x a la derecha, y hacia arriba en pantalla), en dp, y descansan sobre él
KIT_ICONO_R = 1.9                                                # radio del churro (dp)
def churros(trazos, radio=KIT_ICONO_R):
    m = mat_tinta(); objs = []
    for (x0, y0, x1, y1) in trazos:
        a = Vector((x0/KIT_DPU, y0/KIT_DPU, radio/KIT_DPU)); b_ = Vector((x1/KIT_DPU, y1/KIT_DPU, radio/KIT_DPU))
        o = churro(tuple(a), tuple(b_), radio/KIT_DPU, m, coll='iconos', name='kit_ch'); objs.append(o)
    return [o for o in bpy.data.objects if o.name.startswith('kit_ch')]

def icono_kit(nombre):
    objs = []
    if nombre == 'volver':                                       # ←
        objs = churros([(7, 0, -7, 0), (-7, 0, -0.5, 6.5), (-7, 0, -0.5, -6.5)])
    elif nombre == 'visto':                                      # ✓
        objs = churros([(-7.5, -0.5, -2.8, -5.4), (-2.8, -5.4, 7.5, 6.0)])
    elif nombre == 'izq':                                        # ‹
        objs = churros([(3.5, 7.5, -3.5, 0), (-3.5, 0, 3.5, -7.5)])
    elif nombre == 'der':                                        # ›
        objs = churros([(-3.5, 7.5, 3.5, 0), (3.5, 0, -3.5, -7.5)])
    elif nombre == 'compartir':                                  # tres bolitas unidas por dos churros
        objs = churros([(-5.5, 0, 5.5, 6.0), (-5.5, 0, 5.5, -6.0)], radio=1.4)
        m = mat_tinta()
        for (x, y) in ((-5.5, 0), (5.5, 6.0), (5.5, -6.0)):
            sp = sphere(r=3.3/KIT_DPU, seg=24, loc=(x/KIT_DPU, y/KIT_DPU, 3.3/KIT_DPU), coll='iconos', name='kit_ch_bola'); mat(sp, m); objs.append(sp)
    elif nombre == 'candado':                                    # el arco arriba y el cuerpo abajo
        m = mat_tinta()
        body = box((15/KIT_DPU, 11/KIT_DPU, 6/KIT_DPU), loc=(0, -3.5/KIT_DPU, 3/KIT_DPU), coll='iconos', name='kit_ch_cuerpo'); bevel(body, 0.12, 5); mat(body, m); objs.append(body)
        arco = torus(R=4.6/KIT_DPU, r=1.6/KIT_DPU, loc=(0, 4.2/KIT_DPU, 1.6/KIT_DPU), coll='iconos', name='kit_ch_arco'); mat(arco, m); objs.append(arco)
    elif nombre == 'engranaje':                                  # un aro grueso con ocho dientes
        m = mat_tinta()
        aro = torus(R=6.0/KIT_DPU, r=2.2/KIT_DPU, loc=(0, 0, 2.2/KIT_DPU), coll='iconos', name='kit_ch_aro'); mat(aro, m); objs.append(aro)
        for k in range(8):
            ang = k*math.tau/8; rr = 9.0/KIT_DPU
            d = box((3.6/KIT_DPU, 4.4/KIT_DPU, 4.4/KIT_DPU), loc=(rr*math.cos(ang), rr*math.sin(ang), 2.2/KIT_DPU), rot=(0, 0, math.degrees(ang) + 90), coll='iconos', name='kit_ch_diente'); bevel(d, 0.04, 4); mat(d, m); objs.append(d)
    raiz = raiz_kit()
    for o in objs: o.parent = raiz

def render_iconos_kit():
    cam_pieza((4.0, 3.0, 6.0, 7.0), 24.0, 24.0)
    for nombre in ('volver', 'visto', 'izq', 'der', 'compartir', 'candado', 'engranaje'):
        before = set(bpy.data.objects); suelo_kit(); icono_kit(nombre)
        n = 'kit_i_' + nombre; times[n] = render(n, {'iconos', 'luces'})
        for o in [o for o in bpy.data.objects if o not in before]: bpy.data.objects.remove(o, do_unlink=True)

def triangulo_jugar_kit():
    """El triángulo de «Jugar» como un churrito de plasticina tinta: un prisma triangular de puntas muy redondas, apuntando a la derecha."""
    h = 7.0 / KIT_DPU; R = 12.0 / KIT_DPU
    tri = cyl(r=R, d=h, verts=3, loc=(0, 0, h/2), rot=(0, 0, -90), coll='iconos', name='kit_triangulo'); bevel(tri, h*0.95, 8, ang=20); sd = tri.modifiers.new('Sub', 'SUBSURF'); sd.levels = 2; sd.render_levels = 2; smooth(tri)
    mat(tri, mat_tinta()); tri.parent = raiz_kit()

def render_kit():
    scene.view_settings.exposure = float(arg('--kit-exp','-0.42'))   # la plasticina clara se quemaba (crema y lima a 255) con el sol desde arriba
    if KIT_SOLO in ('todo', 'losas'): render_losas()
    if KIT_SOLO in ('todo', 'discos'): render_discos()
    if KIT_SOLO in ('todo', 'iconos'):
        render_iconos_kit()
        cam_pieza((4.0, 3.0, 6.0, 7.0), 20.0, 22.0)
        before = set(bpy.data.objects); suelo_kit(); triangulo_jugar_kit()
        times['kit_jugar'] = render('kit_jugar', {'iconos', 'luces'})
        for o in [o for o in bpy.data.objects if o not in before]: bpy.data.objects.remove(o, do_unlink=True)

def camera_icono():
    cam = bpy.data.cameras.new('cam_ic'); cam.type = 'PERSP'; cam.lens = 90; cam.sensor_width = 36
    d = (Vector((12.8,-12.8,14.5)) - Vector((0,0,0.7))).normalized()   # misma dirección que la cámara de la isla
    co = bpy.data.objects.new('cam_ic', cam); co.location = tuple(Vector((0,0,0.15)) + d*4.2); scene.collection.objects.link(co)
    co.rotation_euler = (Vector((0,0,0.15)) - co.location).to_track_quat('-Z','Y').to_euler(); scene.camera = co

def render_iconos():
    camera_icono(); scene.render.resolution_x = 320; scene.render.resolution_y = 320
    lista = (('tab_avance_brote', icono_avance_brote), ('tab_avance_globo', icono_avance_globo), ('tab_avance_frasco', icono_avance_frasco), ('tab_avance_farol', icono_avance_farol)) if ALTERNATIVAS else (('tab_hoy', icono_hoy), ('tab_juegos', icono_juegos), ('tab_avance', icono_avance_farol))   # Avance = farol, elegido por Ricardo
    for name, fn in lista:
        for on in (False, True):
            before = set(bpy.data.objects); fn(on)
            times[name + ('_on' if on else '')] = render(name + ('_on' if on else ''), {'iconos','luces'})
            for o in [o for o in bpy.data.objects if o not in before]: bpy.data.objects.remove(o, do_unlink=True)

def render_repisa():
    # tira de huellas: plano de plasticina crema visto desde arriba, 256 px que luego se espeja a 512 (sin costura)
    pl = box((3.0, 1.03, 0.1), loc=(0,0,0), coll='iconos', name='repisa_plano'); mat(pl, clay('m_losa', '#F1DFBF', bump=0.55, sss=0.3, vary=0.03, dents=1.0, dent_scale=2.0, grain=0.0, mid=0.3, dent_fine=0.2))
    cam = bpy.data.cameras.new('cam_rep'); cam.type = 'ORTHO'; cam.ortho_scale = 3.0
    co = bpy.data.objects.new('cam_rep', cam); co.location = (0,0,5); co.rotation_euler = (0,0,0); scene.collection.objects.link(co); scene.camera = co
    scene.render.resolution_x = 256; scene.render.resolution_y = 88
    times['repisa_huellas'] = render('repisa_huellas', {'iconos','luces'})

t_all = time.time()
times = {}
if ICONOS or REPISA or ALTERNATIVAS or FONDO or TABLERO or BARRA or KIT:
    studio(); compositor(); setup_render(); scene.render.film_transparent = True
    if ICONOS or ALTERNATIVAS: render_iconos()
    if FONDO: render_fondo()
    if TABLERO: render_tablero()
    if BARRA: render_barra()
    if KIT: render_kit()
    if REPISA: render_repisa()
    with open(os.path.join(OUT,'tiempos.txt'),'w',encoding='utf-8') as f:
        for k,v in times.items(): f.write(f'{k}: {v:.1f} s\n')
    print('[isla] listo'); raise SystemExit
build_island()
group('Observatorio', collect(observatorio), (-P,-P), 90)
group('Antena', collect(antena), (-1.7,0.1), 0)
group('Biblioteca', collect(biblioteca), (P,P), 0)
group('Hangar', collect(hangar), (P,-P), 0)
handmade('edificios'); handmade('isla', skip=('losa','base_tierra','punto'))
nubi(0.5,0.5,2.75,POSE,MIRADA)
studio(); camera(); compositor(); setup_render()
if JSON: export_json()
def export_marcas():
    import json
    from bpy_extras.object_utils import world_to_camera_view as w2c
    bpy.context.view_layer.update(); W, H = scene.render.resolution_x, scene.render.resolution_y
    def py(v): c = w2c(scene, scene.camera, Vector(v)); return round((1-c.y)*H, 1)
    ojos = [o for o in bpy.data.objects if o.name.startswith('nubi_ojo') and 'disco' not in o.name and 'aro' not in o.name]
    son = [o for o in bpy.data.objects if o.name == 'nubi_sonrisa']
    body = bpy.data.objects.get('nubi'); dg = bpy.context.evaluated_depsgraph_get(); ev = body.evaluated_get(dg)
    vs = ev.data.vertices; ys = [py(ev.matrix_world @ Vector(vs[i].co)) for i in range(0, len(vs), 7)]
    d = {'ojos': py(sum((o.matrix_world.translation for o in ojos), Vector()) / max(1,len(ojos))) if ojos else None,
         'sonrisa': py(son[0].matrix_world @ son[0].data.splines[0].bezier_points[1].co) if son else None, 'cima': min(ys), 'base': max(ys), 'alto': H}
    json.dump(d, open(os.path.join(OUT, 'nubi-marcas.json'), 'w'), indent=1); print('[isla] marcas', d)

if PERFIL:
    camera_perfil(); scene.render.resolution_x = 900; scene.render.resolution_y = 700
    times = {'perfil': render('capa-nubi-perfil', {'nubi','luces'})}
elif SOLO_NUBI:
    print('[isla] escena lista', round(time.time()-t_all,1)); sys.stdout.flush()
    if MIRADA == 'frente': export_marcas()
    print('[isla] marcas listas', round(time.time()-t_all,1)); sys.stdout.flush()
    times = {} if NORENDER else {'nubi_'+MIRADA: render('capa-nubi-'+MIRADA, {'nubi','luces'})}
elif SOLO_EDIFICIOS:
    times = {'edificios': render('capa-edificios', {'isla','edificios','luces'}, holdout=('isla',))}
else:
    times = {} if NORENDER else {'completa': render('isla-completa', {'isla','edificios','nubi','luces'})}
if LAYERS and not NORENDER and not SOLO_NUBI and not SOLO_EDIFICIOS and not PERFIL:
    times['base'] = render('capa-isla', {'isla','edificios','luces'}, holdout=('edificios',)); times['edificios'] = render('capa-edificios', {'isla','edificios','luces'}, holdout=('isla',)); times['nubi'] = render('capa-nubi', {'nubi','luces'})
with open(os.path.join(OUT,'tiempos.txt'),'w',encoding='utf-8') as f:
    f.write(f'motor={ENGINE} muestras={SAMPLES} ancho={WIDTH} pose={POSE}\n')
    for k,v in times.items(): f.write(f'{k}: {v:.1f} s\n')
    f.write(f'total: {time.time()-t_all:.1f} s\n')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'isla.blend')); print('[isla] listo')
