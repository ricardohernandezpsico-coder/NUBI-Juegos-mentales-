# -*- coding: utf-8 -*-
"""Lámina de la Tarea 172, «Portadas que se entienden» (`docs/previews/tarea-172.png`), con CAPTURAS REALES (no maquetas).
Ricardo (25-oct), tras probar la Tarea 171: «algunas imágenes se ven cortadas y no se aprecia con claridad los objetos que el usuario usará o con los que interactuará en el juego».
  1. Las 17 fichas «hoy | propuesta», DENTRO de la ficha real (con la X, el emblema y «Sobre este juego» encima, que es como las ve Ricardo): a la izquierda la ficha de la Tarea 171 (ventanilla de 150 dp, recortes a escala 1),
     a la derecha la nueva (ventanilla ADAPTATIVA: 200 dp con letra hasta 1,3 y 180 dp con letra mayor; objetos encuadrados y, si hace falta, alejados con el mismo cielo del juego). Debajo de cada pareja, los objetos que deben verse,
     con la marca de «se ve entero» o de lo que falta en «hoy» y en «propuesta» (la mide `tools/previews/portadas.py` sobre la zona visible de las dos ventanillas, con la X, el emblema, «Sobre este juego» con letra 1,0 y 1,3 y un margen a los
     lados) y cuánto miden en la pantalla (dp). Un objeto entero pero de menos de ~24 dp se marca como chico.
  2. La ficha completa plegada de Piloto (con Reto) y Correo (sin Reto) con letra 1,0, 1,3 y 2,0, «hoy | propuesta», y lo que cuesta la ventanilla nueva.
Entradas: `app/src/test/screenshots/tarea-172/*.png` (`./gradlew.bat :app:recordRoborazziDebug --tests "*Tarea172CapturasTest*"`; `hoy-*.png` son las de la Tarea 171, guardadas antes de cambiar las portadas), `app/src/test/screenshots/tarea-171/`
(Piloto y Correo de la Tarea 171) y `tools/previews/out/informe.json` (lo escribe `portadas.py`).
Uso:  python -I tools/previews/tarea_172.py
"""
import json, os, re, sys, textwrap
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import portadas as P

sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
S172 = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-172')
S171 = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-171')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
INFORME = os.path.join(HERE, 'out', 'informe.json')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-172.png')
NAVY, SUN, DIM, CREAM, OK, BAD, AMBER = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (110, 220, 130), (255, 120, 110), (255, 200, 80)
GAP = 36
NOMBRES = {
    'parejas': 'Constelaciones', 'secuencia': 'Rastro de luz', 'bodega': 'Bodega de carga', 'correo': 'Correo Estelar', 'stroop': 'Tinta o Palabra', 'piloto': 'Piloto Estelar',
    'freno': 'Freno de Emergencia', 'satelites': 'Satélites', 'radar': 'Rescate relámpago', 'calculo': 'Carga exacta', 'engranajes': 'Engranajes', 'acoplamiento': 'Acoplamiento',
    'aterrizaje': 'Aterrizaje Lunar', 'anagramas': 'En la punta de la lengua', 'meteoros': 'Lluvia de meteoros', 'disparate': '¿Verdad o disparate?', 'intrusa': 'La estrella intrusa',
}
# Los recortes de la Tarea 171 (ventanilla de 150 dp, 1080 × 480 px): captura, fila donde empezaba el recorte y zoom; para medir qué se veía entero ENTONCES con los mismos recuadros de objetos.
HOY = {
    'parejas': ('09-t40.png', 1330, 1.0), 'secuencia': ('05-t08.png', 1060, 1.0), 'bodega': ('05-t08.png', 810, 1.0), 'correo': ('09-faro.png', 620, 1.0), 'stroop': ('06-t20.png', 500, 1.0), 'piloto': ('02-ruta-balizas.png', 1680, 1.4),
    'freno': ('06-t20.png', 1420, 1.0), 'satelites': ('02-seguimiento.png', 940, 1.0), 'radar': ('02-destello.png', 560, 1.0), 'calculo': ('05-t08.png', 600, 1.0), 'engranajes': ('11-t40.png', 730, 1.0),
    'acoplamiento': ('05-luz-encendida.png', 1210, 1.0), 'aterrizaje': ('02-acierto.png', 1400, 1.0), 'anagramas': ('05-fichas.png', 1190, 1.0), 'meteoros': ('05-t08.png', 1100, 1.0), 'disparate': ('05-t08.png', 250, 1.0),
    'intrusa': ('05-t08.png', 700, 1.0),
}


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def load(path): return Image.open(path).convert('RGB') if os.path.exists(path) else None


def card_top(im):
    """La fila donde empieza la tarjeta crema (la primera de arriba con el crema de la tarjeta en el centro)."""
    for y in range(40, im.height // 2):
        r, g, b = im.getpixel((im.width // 2, y))
        if r > 235 and g > 225 and b > 200: return y
    return 0


def fit_w(im, w): return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)


def mark(d, x, y, ok, size=30, color=None):
    """La marca: un disco verde con su visto, o rojo con su cruz (forma, no solo color)."""
    c = color or (OK if ok else BAD)
    d.ellipse((x, y, x + size, y + size), fill=c)
    if ok: d.line([(x + 7, y + size * 0.52), (x + size * 0.42, y + size * 0.74), (x + size * 0.78, y + size * 0.28)], fill=NAVY, width=4)
    else:
        d.line([(x + 8, y + 8), (x + size - 8, y + size - 8)], fill=NAVY, width=4); d.line([(x + size - 8, y + 8), (x + 8, y + size - 8)], fill=NAVY, width=4)


def hoy_status(game):
    """Qué se veía entero en la ficha de la Tarea 171: cada recuadro de objeto sobre el recorte de 1080 × 480 de entonces, con la zona visible de la ventanilla de 150 dp."""
    f, y0, zoom = HOY[game]
    w, h = round(1080 / zoom), round(480 / zoom); x0 = (1080 - w) // 2
    H150 = 480
    zx = (round((345 - 8 - 48) * P.PX), round(8 * P.PX), 1080, round(56 * P.PX))
    seg = (P.MARGEN_X, round(H150 * 0.03), 1080 - P.MARGEN_X, round((150 - 28) * P.PX) - 6)
    out = []
    for nombre, b in P.PORTADAS[game]['objetos']:
        t = ((b[0] - x0) * zoom, (b[1] - y0) * zoom, (b[2] - x0) * zoom, (b[3] - y0) * zoom)
        problemas = []
        if t[0] < 0 or t[2] > 1080 or t[1] < 0 or t[3] > H150: problemas.append('cortado por el recorte')
        else:
            if t[0] < seg[0] or t[2] > seg[2] or t[1] < seg[1]: problemas.append('casi en el borde')
            if t[3] > seg[3]: problemas.append('lo tapan el emblema o el botón')
            if P.hits(t, zx): problemas.append('lo tapa la X')
        out.append(problemas)
    return out


def parse_medidas():
    """medidas.txt (lo escribe `Tarea172CapturasTest`): por captura, el alto de la tarjeta plegada, el de la ventanilla y lo que «Sobre este juego» entra por dentro."""
    mp = os.path.join(S172, 'medidas.txt'); out = {}
    if os.path.exists(mp):
        for m in open(mp, encoding='utf-8').read().splitlines():
            r = re.match(r'(\w+)-(\d_\d): la tarjeta mide (\d+) dp de (\d+) .*?la ventanilla mide (\d+) dp; «Sobre este juego» mide (\d+) × (\d+) dp y entra (\d+) dp', m)
            if r and r.group(1) == 'piloto': out[r.group(2)] = tuple(int(v) for v in r.groups()[2:])
    return out


def build():
    informe = json.load(open(INFORME, encoding='utf-8'))
    por = {p['juego']: p for p in informe['portadas']}
    orden = ['parejas', 'secuencia', 'bodega', 'correo', 'stroop', 'piloto', 'freno', 'satelites', 'radar', 'calculo', 'engranajes', 'acoplamiento', 'aterrizaje', 'anagramas', 'meteoros', 'disparate', 'intrusa']
    pw = 540                                                     # ancho de cada ficha en la lámina
    crop_h = 940                                                 # alto de lo que se muestra de arriba de la ficha (px de la captura de 1080)
    ph = round(crop_h * pw / 1080)
    per_row = 2
    cell_h = ph + 44 + 4 * 76 + 34                               # ficha + nombre + hasta 4 objetos (de hasta 2 líneas) + aire
    rows = (len(orden) + per_row - 1) // per_row
    W = GAP + per_row * (2 * pw + 30 + GAP * 2)
    w2 = 370
    hh = round(2203 * w2 / 1080)
    H = 330 + rows * cell_h + 340 + 2 * (hh + 90) + 80
    sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet)
    d.text((GAP, 24), 'Portadas que se entienden (Tarea 172)', font=font(64), fill=SUN)
    y = 108
    for line in ('Ricardo, tras probar la Tarea 171: «algunas imágenes se ven cortadas y no se aprecia con claridad los objetos que el usuario usará o con los que interactuará en el juego».',
                 'Capturas reales de la ficha. «hoy» = Tarea 171 (ventanilla de 150 dp, recortes a escala 1); «propuesta» = ventanilla adaptativa (200 dp con letra hasta 1,3; 180 dp con letra mayor) con los objetos encuadrados ENTEROS y a buen tamaño.',
                 'Lo que se ve de verdad: la ventanilla menos la X (arriba a la derecha), el emblema y «Sobre este juego» (abajo, con letra 1,0 y 1,3) y un margen a los lados (un teléfono angosto recorta). Si hace falta alejar, los costados llevan el mismo cielo del juego;',
                 'si entre dos objetos hay cielo vacío (¿Verdad o disparate?, Meteoros, Carga exacta, Tinta, La estrella intrusa, Piloto) se quita esa franja. Un objeto entero pero de menos de ~24 dp se marca «chico»: ninguno lo es. Las fichas «propuesta» son con letra 1,0.'):
        d.text((GAP, y), line, font=font(26, False), fill=DIM); y += 36
    y0 = y + 30
    for i, g in enumerate(orden):
        col, row = i % per_row, i // per_row
        x0 = GAP + col * (2 * pw + 30 + GAP * 2); y = y0 + row * cell_h
        for k, (prefix, etiqueta) in enumerate((('hoy-', 'hoy'), ('propuesta-', 'propuesta'))):
            im = load(os.path.join(S172, f'{prefix}{g}.png'))
            if im:
                t = card_top(im); c = im.crop((0, max(0, t - 16), 1080, max(0, t - 16) + crop_h)); p = fit_w(c, pw)
                sheet.paste(p, (x0 + k * (pw + 30), y + 44))
            d.text((x0 + k * (pw + 30), y + 4), f'{NOMBRES[g]} · {etiqueta}', font=font(30), fill=CREAM if k else DIM)
        yy = y + 44 + ph + 12
        rep = por[g]; hoy = hoy_status(g)
        d.text((x0, yy - 4), 'hoy', font=font(20), fill=DIM); d.text((x0 + 46, yy - 4), 'propuesta', font=font(20), fill=DIM)
        yy += 22
        for o, hp in zip(rep['objetos'], hoy):
            mark(d, x0, yy + 2, not hp); mark(d, x0 + 46, yy + 2, o['entero'], color=AMBER if o['entero'] and o['chico'] else None)
            size = f"{o['ancho_dp']} × {o['alto_dp']} dp" + (' · CHICO' if o['chico'] else '')
            txt = o['nombre'] + (f"  ·  hoy: {', '.join(hp)}  ·  ahora {size}" if hp else f"  ·  {size}") if o['entero'] else o['nombre'] + '  ·  ' + ', '.join(o['problemas'])
            for j, line in enumerate(textwrap.wrap(txt, 60)[:2]):
                d.text((x0 + 100, yy + j * 28), line, font=font(24, False), fill=BAD if not o['entero'] else AMBER if o['chico'] else CREAM)
            yy += 38 if len(txt) <= 60 else 66
    y = y0 + rows * cell_h + 20
    d.text((GAP, y), 'La ficha completa, hoy | propuesta (letra 1,0, 1,3 y 2,0, a 393 × 873 dp menos las barras del sistema)', font=font(46), fill=SUN)
    m = parse_medidas()
    if m:
        a, b, c = m.get('1_0'), m.get('1_3'), m.get('2_0')
        txt = ('La ventanilla adaptativa: ' + ' · '.join(f"letra {k.replace('_', ',')}: {v[2]} dp" for k, v in sorted(m.items())) + '. La ficha plegada mide '
               + ' · '.join(f"{k.replace('_', ',')}: {v[0]} dp (antes {v[0] - (v[2] - 150)})" for k, v in sorted(m.items())) + f' de los {a[1]} dp que tiene la ventana: '
               + ('cabe sin deslizar con las tres letras' if all(v[0] <= v[1] for v in m.values()) else 'con alguna letra no cabe y se desliza') + '. '
               + f"«Sobre este juego» mide {a[3]} × {a[4]} dp con letra 1,0" + (f", {b[3]} × {b[4]} con 1,3" if b else '') + (f" y {c[3]} × {c[4]} con 2,0 (dos líneas)" if c else '') + f"; por dentro de la ventanilla entra {a[5]} dp con 1,0" + (f", {b[5]} con 1,3" if b else '') + (f" y {c[5]} con 2,0" if c else '') + '.')
        for j, line in enumerate(textwrap.wrap(txt, 150)): d.text((GAP, y + 62 + j * 34), line, font=font(26, False), fill=DIM)
    y += 62 + 34 * 4 + 40
    x = GAP
    for gid, label in (('piloto', 'Piloto Estelar · con Reto'), ('correo', 'Correo Estelar · sin Reto')):
        for sc in ('1_0', '1_3', '2_0'):
            for src, cap in ((S171, 'hoy'), (S172, 'propuesta')):
                im = load(os.path.join(src, f'{gid}-{sc}.png'))
                if im:
                    p = fit_w(im, w2); sheet.paste(p, (x, y)); d.text((x, y + p.height + 8), f'{label.split(" · ")[0].split()[0]} · letra {sc.replace("_", ",")}', font=font(22), fill=CREAM if cap == 'propuesta' else DIM); d.text((x, y + p.height + 36), cap, font=font(24), fill=SUN if cap == 'propuesta' else DIM)
                x += w2 + 12
            x += 12
        x = GAP; y += hh + 90
    sheet = sheet.crop((0, 0, W, min(H, y)))
    sheet.save(OUT, optimize=True); print('lámina:', OUT, sheet.size, os.path.getsize(OUT) // 1024, 'KB')


if __name__ == '__main__':
    build()
