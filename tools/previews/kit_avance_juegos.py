# -*- coding: utf-8 -*-
"""Hoja de las piezas nuevas del kit para Avance y Juegos (Tarea 124): todas a tamaño real (3 px por dp) sobre el fondo A, y los 17 emblemas juntos para compararlos y ver cómo se leen a 30 dp.
Lee los renders de Blender (`tools/isla/out/kit-avance/`, los deja `tools/isla/isla.py --kit --kit-solo avance`), copia los que se guardan a `docs/previews/kit-avance-juegos/` (WebP con transparencia, con su `kit_avance.json`: tamaños,
márgenes y cuerpo de cada pieza medidos en la imagen) y arma `docs/previews/kit-avance-juegos.png`.
Maqueta PIL, NO la app: los textos de ejemplo (número de la medalla, división del escudo, texto del globo) van en Compose; acá se dibujan solo para ver el sitio que dejan.
Uso:  python -I tools/previews/kit_avance_juegos.py [carpeta_de_renders]
"""
import os, sys, json
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SRC = os.path.abspath(sys.argv[1]) if len(sys.argv) > 1 else os.path.join(ROOT, 'tools', 'isla', 'out', 'kit-avance')
DST = os.path.join(ROOT, 'docs', 'previews', 'kit-avance-juegos'); OUT = os.path.join(ROOT, 'docs', 'previews', 'kit-avance-juegos.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font'); RAW = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'raw'); RES = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'drawable-nodpi')
PX = 3
WD = 560                                                                 # ancho de la hoja en dp
INK, SUN, DIM, CREAM = (33, 28, 69), (255, 203, 69), (200, 196, 235), (245, 235, 219)


def font(dp_size, bold=True):
    return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(round(dp_size * PX)))


def pieza(n):
    """El render con la bruma del «shadow catcher» recortada, igual que `tools/isla/kit.py` (el suelo invisible deja un velo de alfa 10-20 que sobre crema se vería como un rectángulo gris): se resta un piso de 12/255 y el alfa llega a 0 en los últimos 10 px."""
    a = np.asarray(Image.open(os.path.join(SRC, 'kit_pz_' + n + '.png')).convert('RGBA')).astype(float).copy()
    al = np.clip((a[..., 3] - 12.0) * 255.0 / (255.0 - 12.0), 0, 255); h, w = al.shape; yy, xx = np.mgrid[0:h, 0:w]
    d = np.minimum.reduce([xx, yy, w - 1 - xx, h - 1 - yy]).astype(float); al *= 0.5 - 0.5 * np.cos(np.pi * np.clip(d / 10.0, 0, 1)); a[..., 3] = al
    return Image.fromarray(a.astype(np.uint8), 'RGBA')


def existe(n): return os.path.exists(os.path.join(SRC, 'kit_pz_' + n + '.png'))


def escala(im, k):
    return im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)


def cuerpo_px(im):
    """El cuerpo opaco de la imagen (sin la sombra, que es translúcida): (izq, arr, der, abajo) en px."""
    a = im.getchannel('A').point(lambda v: 255 if v >= 200 else 0)
    return a.getbbox()


# ------------------------------------------------------------------------------------------------ metadatos y copias
def guarda():
    os.makedirs(DST, exist_ok=True)
    meta = json.load(open(os.path.join(SRC, 'kit_avance.json'), encoding='utf-8'))
    piezas = {}
    for n, m in meta['piezas'].items():
        if not existe(n): continue
        im = pieza(n); b = cuerpo_px(im)
        if b is None: continue
        iz, ar, de, ab = b; w, h = im.size
        piezas[n] = dict(m, imagen_px=[w, h], margenes_dp=dict(izq=round(iz / PX, 2), arr=round(ar / PX, 2), der=round((w - de) / PX, 2), abajo=round((h - ab) / PX, 2)),
                         cuerpo_medido_dp=dict(ancho=round((de - iz) / PX, 2), alto=round((ab - ar) / PX, 2)))
        im.save(os.path.join(DST, 'kit_pz_' + n + '.webp'), 'WEBP', quality=92, alpha_quality=100, method=6)
    with open(os.path.join(DST, 'kit_avance.json'), 'w', encoding='utf-8') as f:
        json.dump({'px_por_dp': PX, 'nota': 'Piezas del kit para Avance y Juegos (Tarea 124), renders de Blender (`tools/isla/isla.py --kit --kit-solo avance`) a 3 px por dp. `cuerpo_medido_dp` y `margenes_dp` salen de la imagen (lo opaco; la sombra es translúcida). '
                   'Nine-slice horizontal en canal y churro. El texto (número de la medalla y del escudo, texto del globo) va en Compose.', 'piezas': piezas}, f, ensure_ascii=False, indent=1)
    return piezas


# ------------------------------------------------------------------------------------------------ nine-slice de las losas del kit y de las piezas horizontales
def losa_kit(nombre_tam, color, body_w_dp, body_h_dp):
    """Una losa del kit (`kit.json`) estirada a un cuerpo de body_w × body_h dp, con su sombra."""
    kit = json.load(open(os.path.join(RAW, 'kit.json'), encoding='utf-8'))['losas'][nombre_tam]
    im = Image.open(os.path.join(RES, 'kit_losa_%s_%s.webp' % (nombre_tam, color))).convert('RGBA')
    ins = kit['insets_px']; mg = kit['margenes_dp']
    W = int(round((body_w_dp + mg['izq'] + mg['der']) * PX)); H = int(round((body_h_dp + mg['arr'] + mg['abajo']) * PX))
    l, t, r, b = ins['izq'], ins['arr'], ins['der'], ins['abajo']; iw, ih = im.size
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    xs = [(0, l, 0, l), (l, iw - r, l, W - r), (iw - r, iw, W - r, W)]; ys = [(0, t, 0, t), (t, ih - b, t, H - b), (ih - b, ih, H - b, H)]
    for (sx0, sx1, dx0, dx1) in xs:
        for (sy0, sy1, dy0, dy1) in ys:
            if sx1 <= sx0 or sy1 <= sy0 or dx1 <= dx0 or dy1 <= dy0: continue
            out.alpha_composite(im.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return out, (mg['izq'] * PX, mg['arr'] * PX)


def horizontal(im, ancho_cuerpo_dp):
    """Nine-slice horizontal de una pieza (canal, churro): el cuerpo se estira a ancho_cuerpo_dp por una franja de 4 dp del centro; las tapas quedan como están."""
    b = cuerpo_px(im); bw = (b[2] - b[0]) / PX; extra = int(round((ancho_cuerpo_dp - bw) * PX))
    cx = (b[0] + b[2]) // 2; fx = 2 * PX
    izq = im.crop((0, 0, cx - fx, im.height)); med = im.crop((cx - fx, 0, cx + fx, im.height)); der = im.crop((cx + fx, 0, im.width, im.height))
    out = Image.new('RGBA', (im.width + extra, im.height), (0, 0, 0, 0)); out.alpha_composite(izq, (0, 0))
    out.alpha_composite(med.resize((med.width + extra, med.height), Image.LANCZOS), (izq.width, 0)); out.alpha_composite(der, (izq.width + med.width + extra, 0))
    return out, b


def edificio_sobre_pedestal(area, ancho_dp, meta, farola=False):
    """El edificio de la isla de [area] sobre el pedestal, como lo arma `BuildingOnPedestal` de la app (el pie del edificio en el centro de la cara de arriba del pedestal; 21,7 dp bajo el borde de arriba de su cuerpo)."""
    ped = pieza('pedestal'); pb = cuerpo_px(ped); k = ancho_dp / ((pb[2] - pb[0]) / PX)
    ed = pieza('edificio_' + area); pie = meta['piezas']['edificio_' + area]['pie_dp']
    W = int((ped.width + 60) * k) + 400; H = int((ped.height + 72 * PX) * k) + 400; canvas = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    ox, oy = 200, 200 + int(72 * PX * k)                                   # dónde cae la esquina de la imagen del pedestal
    canvas.alpha_composite(escala(ped, k), (ox, oy))
    fx = ox + (pb[0] + pb[2]) / 2 * k; fy = oy + pb[1] * k + 21.7 * PX * k
    e = escala(ed, k); canvas.alpha_composite(e, (int(fx - pie[0] * PX * k), int(fy - pie[1] * PX * k)))
    if farola:
        fa = pieza('farola_encendida'); fb = cuerpo_px(fa); fa = escala(fa, 1.0)
        canvas.alpha_composite(fa, (int(ox + (pb[0] + (pb[2] - pb[0]) * 0.8) * k - (fb[0] + fb[2]) / 2), int(fy - fb[3] + 4 * PX)))
    return canvas.crop(canvas.getbbox())


# ------------------------------------------------------------------------------------------------ la hoja
def fondo(w, h):
    base = Image.open(os.path.join(ROOT, 'docs', 'previews', 'fondo-espacio', 'A-fondo_lamina.webp')).convert('RGBA')
    lu = Image.open(os.path.join(ROOT, 'docs', 'previews', 'fondo-espacio', 'A-fondo_luceros.webp')).convert('RGBA')
    k = max(w / base.width, h / base.height); sz = (int(base.width * k) + 1, int(base.height * k) + 1)
    base = base.resize(sz, Image.LANCZOS); lu = lu.resize(sz, Image.LANCZOS)
    x0 = (sz[0] - w) // 2; y0 = 0
    out = base.crop((x0, y0, x0 + w, y0 + h)); out.alpha_composite(lu.crop((x0, y0, x0 + w, y0 + h))); return out


def hoja():
    piezas = guarda()
    lienzo = Image.new('RGBA', (WD * PX, 6400), (0, 0, 0, 0)); d = ImageDraw.Draw(lienzo); M = 20 * PX; y = M
    def parrafo(texto, tam=11, color=DIM, x=None, ancho=None):
        nonlocal y
        f = font(tam, False); x = M if x is None else x; ancho = (WD * PX - M - x) if ancho is None else ancho; linea = ''
        for pal in texto.split(' '):
            if d.textlength((linea + ' ' + pal).strip(), font=f) > ancho and linea: d.text((x, y), linea, font=f, fill=color); y += int((tam + 4) * PX); linea = pal
            else: linea = (linea + ' ' + pal).strip()
        if linea: d.text((x, y), linea, font=f, fill=color); y += int((tam + 4) * PX)
    def titulo(texto, nota=None):
        nonlocal y
        d.text((M, y), texto, font=font(15), fill=SUN); y += int(21 * PX)
        if nota: parrafo(nota)
        else: y += int(3 * PX)
    def pon(im, x, yy): lienzo.alpha_composite(im, (int(x), int(yy)))
    def etiqueta(texto, cx, yy, tam=10):
        d.text((cx, yy), texto, font=font(tam, False), fill=DIM, anchor='ma')

    d.text((M, y), 'Piezas nuevas del kit · Avance y Juegos', font=font(21), fill=(255, 255, 255)); y += int(30 * PX)
    parrafo('A tamaño real (1 dp = 3 px) sobre el fondo A · Tarea 124 · render de Blender con la luz y el material del kit · el texto va siempre en Compose'); y += int(8 * PX)

    # 1. barra de avance: canal, churro y bolita
    if all(existe(n) for n in ('canal_crema', 'canal_cielo', 'churro_sol', 'churro_lima', 'bolita_lima')):
        titulo('Barra de avance', 'canal (zanja) y churro (relleno) estiran en horizontal; la bolita es el cambio. Arriba, sobre una losa crema; abajo, sobre el cielo.')
        panel, (ox, oy) = losa_kit('gr', 'crema', 330, 92); pon(panel, M, y)
        cn, cb = horizontal(pieza('canal_crema'), 280); px0 = M + ox + 25 * PX - cb[0]; py0 = y + oy + 22 * PX - cb[1]; pon(cn, px0, py0)
        ch, hb = horizontal(pieza('churro_sol'), 280 * 0.62 - 4); pon(ch, px0 + cb[0] + 1.5 * PX - hb[0], py0 + cb[1] + (6 - 4) * PX - 1.0 * PX - hb[1])
        d.text((M + ox + 25 * PX, y + oy + 46 * PX), '4 de 7 días', font=font(15), fill=INK)
        bo = pieza('bolita_lima'); bb = cuerpo_px(bo); pon(bo, M + ox + 25 * PX + 280 * 0.62 * PX - bb[0] + 4 * PX, y + oy + 22 * PX + 6 * PX - (bb[1] + bb[3]) // 2)
        y += int(112 * PX)
        cn, cb = horizontal(pieza('canal_cielo'), 280); px0 = M + 25 * PX - cb[0]; py0 = y - cb[1]; pon(cn, px0, py0)
        ch, hb = horizontal(pieza('churro_lima'), 280 * 0.35 - 4); pon(ch, px0 + cb[0] + 1.5 * PX - hb[0], py0 + cb[1] + (6 - 4) * PX - 1.0 * PX - hb[1])
        d.text((M + 25 * PX + 300 * PX, y - 2 * PX), 'En el cielo', font=font(11, False), fill=DIM); y += int(34 * PX)

    # 2. emblemas
    nombres = [n for n in ('parejas', 'secuencia', 'bodega', 'correo', 'stroop', 'piloto', 'freno', 'satelites', 'radar', 'calculo', 'engranajes', 'acoplamiento', 'aterrizaje', 'anagramas', 'meteoros', 'disparate', 'intrusa') if existe('emblema_' + n)]
    if nombres:
        titulo('Emblemas de juego · 17', 'ficha de 64 dp en el pastel de su área (Memoria, Atención, Razonamiento, Lenguaje); el objeto, al ~70 %. Sin estrellas con puntas, cruces ni medias lunas.')
        paso = 72 * PX; x0 = M + (WD * PX - 2 * M - 6 * paso) // 2
        for i, n in enumerate(nombres):
            c, r = i % 6, i // 6; pon(pieza('emblema_' + n), x0 + c * paso, y + r * (paso + 14 * PX)); etiqueta(n, x0 + c * paso + 36 * PX, y + r * (paso + 14 * PX) + 70 * PX, 9)
        y += 3 * (paso + 14 * PX) + int(8 * PX)
        titulo('Los 17 juntos, para comparar', 'a 48 dp (la ficha del juego), a 40 dp (las piedritas de Hoy) y a 30 dp (la lámina del lugar): tienen que distinguirse a 30.')
        for tam in (48, 40, 30):
            d.text((M, y), '%d dp' % tam, font=font(11, False), fill=SUN); y += int(15 * PX)
            cel = int(tam * 72 / 64 * PX); gap = 4 * PX; por_fila = 9; x0 = M
            for i, n in enumerate(nombres):
                c, r = i % por_fila, i // por_fila; pon(escala(pieza('emblema_' + n), cel / (72 * PX)), x0 + c * (cel + gap), y + r * (cel + gap))
            y += 2 * (cel + gap) + int(6 * PX)
        y += int(6 * PX)

    # 3. farola
    if existe('farola_apagada') and existe('farola_encendida'):
        titulo('Farola', 'la misma de la isla y del ícono de Avance. 28 dp en «Tu semana» y 22 dp en Hoy; «hoy» es un aro lima de 2,5 dp que se dibuja en Compose.')
        ap, en = pieza('farola_apagada'), pieza('farola_encendida'); x = M
        for tam in (28, 22):
            k = tam / 28.0
            for im in (ap, en): pon(escala(im, k), x, y); x += int(40 * PX)
        # una semana: 4 encendidas y 3 apagadas, el lunes con el aro de hoy
        lx = x + 20 * PX; bb = cuerpo_px(en); bcx, bcy = (bb[0] + bb[2]) / 2, (bb[1] + bb[3]) / 2
        for i in range(7):
            im = en if i < 4 else ap; pon(im, lx + i * int(32 * PX), y - 4 * PX)
            if i == 3:
                cx, cy = lx + i * int(32 * PX) + bcx, y - 4 * PX + bcy
                d.ellipse([cx - 17 * PX, cy - 17 * PX, cx + 17 * PX, cy + 17 * PX], outline=(155, 229, 100), width=int(2.5 * PX))
        y += int(46 * PX)

    # 4. pedestal
    if existe('pedestal'):
        titulo('Pedestal de lugar', '150 × 56 dp: la losa crema de la isla con su base de tierra. Sostiene al edificio; con la farola encima si el lugar es «Sugerida».')
        pe = pieza('pedestal'); pon(pe, M, y); pon(pe, M + 190 * PX, y)
        if existe('farola_encendida'):
            fa = pieza('farola_encendida'); fb = cuerpo_px(fa); k = 1.6; fa = escala(fa, k)
            pon(fa, M + 190 * PX + 243 - (fb[0] + fb[2]) * k / 2, y + 40 * PX - fb[3] * k)
        d.text((M + 8 * PX, y + 72 * PX), 'el pedestal solo', font=font(11, False), fill=DIM); d.text((M + 198 * PX, y + 72 * PX), 'con la farola, si el lugar es «Sugerida»', font=font(11, False), fill=DIM)
        y += int(92 * PX)

    # 4b. los edificios de la isla sobre el pedestal (la grilla de Juegos)
    if all(existe('edificio_' + a) for a in ('memoria', 'atencion', 'razonamiento', 'lenguaje')) and existe('pedestal'):
        meta = json.load(open(os.path.join(SRC, 'kit_avance.json'), encoding='utf-8'))
        titulo('Edificios de la isla · uno por área', 'observatorio = Memoria, antena = Atención, hangar = Razonamiento, biblioteca = Lenguaje: los mismos modelos de la isla, con la cámara de la isla, parados sobre el pedestal (104 dp; el sugerido, 140 dp y con la farola encendida).')
        x = M
        for i, a in enumerate(('memoria', 'atencion', 'razonamiento', 'lenguaje')):
            im = edificio_sobre_pedestal(a, 104 if i != 2 else 104, meta); pon(im, x, y); x += im.width + 8 * PX
        h1 = max(im.height for im in [edificio_sobre_pedestal(a, 104, meta) for a in ('memoria',)])
        y += int(h1 + 10 * PX)
        d.text((M, y), 'la «Sugerida»', font=font(11, False), fill=DIM); y += int(14 * PX)
        sug = edificio_sobre_pedestal('razonamiento', 140, meta, farola=True); pon(sug, M, y); y += sug.height + int(10 * PX)

    # 5. globo de Nubi
    if existe('cola_izquierda') and existe('cola_derecha'):
        titulo('Globo de Nubi', 'losa crema nine-slice (la grande del kit) + cola aparte de 16 × 12 dp, a la izquierda o a la derecha. El texto va en Compose.')
        for lado, nombre, texto in (('izquierda', 'cola_izquierda', 'Hoy descansaste: tu racha sigue.'), ('derecha', 'cola_derecha', 'Un día de descanso por semana no la corta.')):
            gl, (ox, oy) = losa_kit('md', 'crema', 300, 52); x = M + (0 if lado == 'izquierda' else 20 * PX)
            pon(gl, x, y); d.text((x + ox + 16 * PX, y + oy + 15 * PX), texto, font=font(12, False), fill=INK)
            co = pieza(nombre); cb = cuerpo_px(co); cx = x + ox + (30 * PX if lado == 'izquierda' else 300 * PX - 46 * PX)
            pon(co, cx - cb[0], y + oy + 52 * PX - 6 * PX - cb[1]); y += int(80 * PX)
        y += int(4 * PX)

    # 6. medallas
    fam = ('constancia', 'variedad', 'volver'); ok = all(existe('medalla_%s_%s' % (f, e)) for f in fam for e in ('conseguida', 'bloqueada'))
    if ok:
        titulo('Medallas · 3 familias', 'conseguida (color y luz) y bloqueada (plasticina cruda, gris-crema, sin luz). Constancia = farola encendida; Variedad = los cuatro edificios; Volver = la nave posándose. El número va sobre la placa lisa.')
        paso = 100 * PX; x0 = M + 8 * PX
        for r, est in enumerate(('conseguida', 'bloqueada')):
            for c, f in enumerate(fam):
                im = pieza('medalla_%s_%s' % (f, est)); pon(im, x0 + c * paso, y + r * paso)
                if est == 'conseguida': d.text((x0 + c * paso + 48 * PX, y + r * paso + 86 * PX), str((3, 5, 7)[c]), font=font(13), fill=INK, anchor='mm')
                else: d.text((x0 + c * paso + 48 * PX, y + r * paso + 86 * PX), '·', font=font(13), fill=(150, 144, 130), anchor='mm')
        for r, est in enumerate(('conseguida', 'bloqueada')):
            for c, f in enumerate(fam): pon(escala(pieza('medalla_%s_%s' % (f, est)), 56 / 96.0), x0 + 3 * paso + 12 * PX + c * 62 * PX, y + r * 62 * PX)
        d.text((x0 + 3 * paso + 12 * PX, y + 128 * PX), 'a 56 dp (la grilla)', font=font(11, False), fill=DIM)
        y += 2 * paso + int(8 * PX)

    # 7. escudos
    ligas = [n for n in ('bronce', 'plata', 'oro', 'platino', 'esmeralda', 'diamante', 'maestro') if existe('escudo_' + n)]
    if ligas:
        titulo('Escudos de liga · 7 colores', 'plasticina mate, no metal; sin estrella ni cruz. La división («2») va en Compose sobre la placa clara; a 40 dp es el de la fila de la liga.')
        paso = 126 * PX; x0 = M + 4 * PX
        for i, n in enumerate(ligas):
            c, r = i % 4, i // 4; im = pieza('escudo_' + n); pon(im, x0 + c * paso, y + r * 140 * PX)
            d.text((x0 + c * paso + 60 * PX, y + r * 140 * PX + 56 * PX), '2', font=font(26), fill=INK, anchor='mm')
            etiqueta(n, x0 + c * paso + 60 * PX, y + r * 140 * PX + 127 * PX, 10)
        y += 2 * 140 * PX + int(4 * PX)
        for i, n in enumerate(ligas): pon(escala(pieza('escudo_' + n), 40 / 120.0), M + 8 * PX + i * 46 * PX, y)
        d.text((M + 8 * PX + 7 * 46 * PX + 6 * PX, y + 14 * PX), 'a 40 dp', font=font(11, False), fill=DIM)
        y += int(56 * PX)

    H = y + M
    fondo_img = fondo(WD * PX, H); fondo_img.alpha_composite(lienzo.crop((0, 0, WD * PX, H)))
    fondo_img.convert('RGB').save(OUT, optimize=True); print('hoja', OUT, fondo_img.size, 'piezas copiadas:', len(piezas))


if __name__ == '__main__':
    hoja()
