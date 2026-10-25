# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» de la Tarea 164, «Engranajes, primera parte» (24-25-oct-2026; `auditoria-fable/engranajes-visual.md`, partes A, D y E, y `auditoria-fable/engranajes-colores.md`, la paleta 1 «Madera clara» que eligió Ricardo): `docs/previews/tarea-164.png`.
Regla «Plasticina visible» (16-oct): Ricardo la ve ANTES de integrar. Todas las tomas son CAPTURAS REALES de Unity (1080 × 2400, `bash tools/verificar-todo.sh --capturas engranajes`):
«HOY» = el commit anterior (carpeta `unity/test-results/engranajes-164-hoy/`) y «PROPUESTA» = esta tarea (`unity/test-results/engranajes-164-madera/`); los recortes van al 100 % de la captura y los botones de las tomas enteras están en su estado NORMAL (el piloto automático se detiene mientras se sacan).
Las rampas de color son las de `tools/juegos/engranajes_volumen.py` (las reales, no una réplica).
Uso:  python -I tools/previews/tarea_164.py      → docs/previews/tarea-164.png
"""
import os, sys, json, glob
import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, os.path.join(ROOT, 'tools', 'juegos'))
import mide_engranajes_mesa as mm
import engranajes_volumen as vol

HOY = os.path.join(ROOT, 'unity', 'test-results', 'engranajes-164-hoy'); NUEVO = os.path.join(ROOT, 'unity', 'test-results', 'engranajes-164-madera')
ATLAS = os.path.join(ROOT, 'unity', 'NeuroVidaCore', 'Assets', 'Resources', 'Plasticina', 'engranajes')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-164.png')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LIME, CORAL = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100), (255, 140, 107)
WINDOW = (36, 34, 88)                       # el fondo de la ventanilla (EngranajesSprites.WindowTop = #242258)
W = 2400; MG = 40; GAP = 30


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def wrap(d, text, fnt, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=fnt) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines or ['']


def pct(x): return ('%.1f' % x).replace('.', ',')


def toma(folder, name):
    """La captura cuyo nombre (sin el número de orden) es [name]: `03-primera.png` → «primera»."""
    for p in sorted(glob.glob(os.path.join(folder, '*-' + name + '.png'))): return Image.open(p).convert('RGB'), p
    raise FileNotFoundError(name + ' en ' + folder)


def gear_crop(im, win, w=700, h=540):
    """Un recorte al 100 % centrado en el racimo de engranajes de la ventanilla (todo lo claro: madera, celeste y uva)."""
    a = np.asarray(im).astype(int)
    x0, y0, x1, y1 = win
    reg = a[y0:y1, x0:x1]
    face = reg.sum(axis=2) > 400
    ys, xs = np.where(face)
    cx, cy = (int(np.percentile(xs, 2)) + int(np.percentile(xs, 98))) // 2 + x0, (int(np.percentile(ys, 2)) + int(np.percentile(ys, 98))) // 2 + y0
    left = max(0, min(im.width - w, cx - w // 2)); top = max(0, min(im.height - h, cy - h // 2))
    return im.crop((left, top, left + w, top + h))


def cara(nombre):
    """La cara de una pieza del atlas (el promedio del 30 % más claro de sus píxeles opacos) y su contraste (WCAG) contra la ventanilla."""
    m = json.load(open(os.path.join(ATLAS, 'manifest.json'), encoding='utf-8')); e = next(s for s in m['sprites'] if s['n'] == nombre)
    at = np.asarray(Image.open(os.path.join(ATLAS, 'atlas.png')).convert('RGBA')).astype(float)
    top = at.shape[0] - e['y'] - e['h']; px = at[top:top + e['h'], e['x']:e['x'] + e['w']].reshape(-1, 4); px = px[px[:, 3] > 250][:, :3]
    luz = px @ np.array([0.2126, 0.7152, 0.0722]); sel = px[luz >= np.percentile(luz, 70)]
    c = tuple(int(round(v)) for v in sel.mean(axis=0))
    return c, mm.ratio(c, WINDOW)


def swatches(d, x, y, nombres, etiquetas, w=300, h=64, gap=26):
    """Las rampas (de la sombra a la luz) de cada rol, con su nombre."""
    F = font(23, False)
    for k, (n, et) in enumerate(zip(nombres, etiquetas)):
        t = np.linspace(0, 1, w); col = (vol.rampa(vol.RAMPAS[n], t) * 255 + 0.5).astype(np.uint8)
        sw = Image.fromarray(np.repeat(col[None, :, :], h, axis=0), 'RGB')
        im_x = x + k * (w + gap)
        d._image.paste(sw, (im_x, y)); d.rectangle((im_x, y, im_x + w - 1, y + h - 1), outline=(70, 74, 130))
        d.text((im_x, y + h + 6), et, font=F, fill=CREAM)


def main():
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    F_T, F_H, F_P, F_C, F_B = font(60), font(36), font(26, False), font(23, False), font(27)
    mh = mm.mide('HOY', toma(HOY, 't20')[1], False); mn = mm.mide('PROPUESTA', toma(NUEVO, 't20')[1], True)          # el engranaje más bajo de la toma de los 20 s: el motor (lila, hoy; celeste, propuesta)
    mh8 = mm.mide('HOY', toma(HOY, 't08')[1], False); mn8 = mm.mide('PROPUESTA', toma(NUEVO, 't08')[1], True)          # y el de los 8 s: un engranaje del camino (lila, hoy; madera, propuesta)
    caras = [cara('gear_0_0'), cara('gear_0_1'), cara('gear_0_2')]
    intro = ('Primera parte del cambio visual de Engranajes: las partes A, D y E de la spec de Fable (la mesa, el aviso y la composición) y, en la misma pasada, los COLORES de la paleta 1, «Madera clara», que eligió Ricardo (24-oct): el camino en madera clara, el motor celeste y la pieza que mueve la antena o el cohete en uva (el morado de Razonamiento). '
             'Faltan la B (los agujeros del tablero) y la C (el cohete lila). Los dientes, las siluetas, las correas, las posiciones y la mecánica NO cambian. Son capturas REALES de Unity a 1080 × 2400 (una máquina al azar de la misma etapa en cada toma: cambia lo que la rodea) y los botones están en su estado normal.')
    cambios = [
        ('A · La mesa de taller.', 'Una losa crema gruesa del kit a casi todo el ancho; el panel de antes es ahora su ventanilla noche hundida (mismo tamaño y lugar: la máquina no se mueve ni un píxel adentro), con cuatro tornillos de plasticina en el marco. Se quitaron las rayas.'),
        ('D · El aviso.', 'Losa crema con la letra en tinta (20 dp la primera línea, 16 la pista) y la moneda de acierto (lima) o de error (coral) de lo común. Crece si una frase se parte en dos renglones. Las frases no cambian.'),
        ('E · La composición.', 'La mesa empieza cerca de «Cohete número 1» y la escena sube unos 35 dp respecto de antes; los botones son más altos (de 88 a 114 dp) y quedan más a mano; el aviso queda 10 dp bajo ellos.'),
        ('F · Los colores: «Madera clara».', 'Camino en arena (madera clara), motor en el celeste del kit, pieza en uva. Cada rol es de una familia de color distinta (más de 100° de tono entre sí) y el morado queda donde dice Razonamiento. Solo cambia el color: la luz, los rayos, el bisel y el canto son los mismos.'),
    ]
    mias = [
        ('El casco.', 'Sigue con mi tinte lila de una línea sobre la mesa crema hasta que llegue el cohete lila de la parte C (Blender). La turbina dorada y el radar celeste no se tocaron: Fable no los incluyó.'),
        ('Las piezas fijas del cohete.', 'Siguen la receta por rol de la paleta: portón y barra dentada en madera, caja celeste, marco y plataforma en uva. Sus cantos cambian con ellas.'),
        ('El aire de abajo.', 'Con la escena arriba sobraban unos 190 dp de cielo bajo los botones (ahí sale el aviso 3 segundos). Ahora el aire se reparte: botones más altos, la mesa 31 dp más abajo de la cabecera, 44 dp entre la mesa y los botones y 56 dp libres bajo el aviso; con el aviso apagado quedan 150 dp de cielo. Si no gusta, se afloja.'),
        ('El ancho.', 'La mesa mide 385 dp del boceto (de −8 a 377) para que los carteles, que llegan a 369, queden adentro: a la escala máxima deja 10 dp a cada lado.'),
    ]
    medidas = [
        'Cada cara contra la ventanilla (lo que se ve entre un diente y su hueco), medido sobre el atlas: motor celeste %s : 1, camino de madera %s : 1, pieza uva %s : 1 (hoy: 8,0, 6,9 y 10,1 : 1). Los tres pasan de 3 : 1.' % (pct(caras[0][1]), pct(caras[1][1]), pct(caras[2][1])),
        'En el juego, un engranaje contra el hueco que deja entre dientes: camino de madera %s : 1 (hoy, lila: %s : 1) y motor celeste %s : 1 (hoy, azul: %s : 1). Se pedía ≥ 3 : 1.' % (pct(mn8['diente_fondo']), pct(mh8['diente_fondo']), pct(mn['diente_fondo']), pct(mh['diente_fondo'])),
        'Canto bajo un engranaje contra el fondo: el de la madera %s a %s : 1 (hoy, lila: %s a %s : 1); el del motor celeste %s a %s : 1. La spec de la paleta pedía canto/ventanilla ≥ 1,5 : 1 para el camino: se cumple en lo más claro del canto. Lo más oscuro (canto hondo y sombra) sigue en ~1,2 : 1 (hoy 1,1 : 1): el canto y la sombra de la Tarea 115 no se tocaron, solo su color por rol.' % (pct(mn8['canto'][0]), pct(mn8['canto'][1]), pct(mh8['canto'][0]), pct(mh8['canto'][1]), pct(mn['canto'][0]), pct(mn['canto'][1])),
        'Ventanilla contra el cielo: de %s : 1 a %s : 1. La mesa crema contra el cielo: %s : 1.' % (pct(mh['fondo_cielo']), pct(mn['fondo_cielo']), pct(mn['crema_cielo'])),
    ]
    # --- composición de la hoja
    intro_l = wrap(probe, intro, F_P, W - 2 * MG)
    full_h, full_n, full_a = toma(HOY, 'primera')[0], toma(NUEVO, 'primera')[0], toma(NUEVO, 'primera-aviso')[0]
    fw = 470; fh = int(full_h.height * fw / full_h.width)
    colw = W - 2 * MG - 3 * fw - 3 * GAP

    def block(items, width): return [(tag, wrap(probe, body, F_P, width)) for tag, body in items]

    rows_c = block(cambios, colw); rows_m = block(mias, colw)
    med_w = 3 * fw + 2 * GAP; med_l = [wrap(probe, t, F_P, med_w) for t in medidas]            # lo que se midió va bajo las tres tomas (el hueco que dejan)
    med_h = 50 + sum(32 * len(l) + 14 for l in med_l)
    text_h = 50 + sum(36 + 32 * len(b) + 14 for _, b in rows_c) + 40 + 50 + sum(36 + 32 * len(b) + 14 for _, b in rows_m)
    row1_h = max(fh + 50 + med_h, text_h)
    gm_h, gm_n = gear_crop(toma(HOY, 't20')[0], (80, 600, 600, 1500)), gear_crop(toma(NUEVO, 'primera')[0], (90, 500, 700, 1400))
    ch_hoy = toma(HOY, 't20')[0].crop((680, 520, 1080, 1290)); ch_new = toma(NUEVO, 'primera')[0].crop((680, 430, 1080, 1200))
    av_hoy_m, av_new_m = toma(HOY, 'aviso-mal')[0].crop((0, 1990, 1080, 2300)), toma(NUEVO, 'aviso-mal')[0].crop((0, 1975, 1080, 2285))
    av_hoy_b, av_new_b = toma(HOY, 'aviso-bien')[0].crop((0, 1990, 1080, 2300)), toma(NUEVO, 'aviso-bien')[0].crop((0, 1975, 1080, 2285))
    sw_h = 64 + 40; sw_block = 56 + (sw_h + 34) * 2 + 20
    H = 400 + MG + 90 + 34 * len(intro_l) + 30 + 56 + row1_h + 50 + sw_block + 56 + max(gm_h.height, ch_hoy.height) + 70 + 56 + av_hoy_m.height + 70 + 56 + av_hoy_b.height + 70 + 20
    sheet = Image.new('RGB', (W, H), NAVY); d = ImageDraw.Draw(sheet); d._image = sheet; y = MG
    d.text((MG, y), 'Engranajes, primera parte: hoy | propuesta (Tarea 164)', font=F_T, fill=SUN); y += 90
    for ln in intro_l: d.text((MG, y), ln, font=F_P, fill=CREAM); y += 34
    y += 30
    d.text((MG, y), 'COMPOSICIÓN y COLORES · tomas enteras al 44 %, botones en su estado normal', font=F_H, fill=SUN); y += 56
    for k, (im, lab, col) in enumerate(((full_h, 'HOY', DIM), (full_n, 'PROPUESTA', LIME), (full_a, 'PROPUESTA · con el aviso de un error', LIME))):
        x = MG + k * (fw + GAP)
        sheet.paste(im.resize((fw, fh), Image.LANCZOS), (x, y)); d.rectangle((x, y, x + fw - 1, y + fh - 1), outline=(70, 74, 130))
        d.text((x, y + fh + 6), lab, font=F_B, fill=col)
    tx = MG + 3 * (fw + GAP); ty = y
    d.text((tx, ty), 'LO QUE CAMBIÓ', font=F_H, fill=LIME); ty += 50
    for tag, body in rows_c:
        d.text((tx, ty), tag, font=F_B, fill=SUN); ty += 36
        for ln in body: d.text((tx, ty), ln, font=F_P, fill=CREAM); ty += 32
        ty += 14
    ty += 12
    d.text((tx, ty), 'LO QUE DECIDÍ YO (dímelo si no)', font=F_H, fill=CORAL); ty += 50
    for tag, body in rows_m:
        d.text((tx, ty), tag, font=F_B, fill=SUN); ty += 36
        for ln in body: d.text((tx, ty), ln, font=F_P, fill=CREAM); ty += 32
        ty += 14
    my = y + fh + 50
    d.text((MG, my), 'LO QUE SE MIDIÓ', font=F_H, fill=LIME); my += 50
    for lines in med_l:
        for ln in lines: d.text((MG, my), ln, font=F_P, fill=DIM); my += 32
        my += 14
    y += row1_h + 50
    d.text((MG, y), 'LAS RAMPAS · de la sombra a la luz (las reales de engranajes_volumen.py)', font=F_H, fill=SUN); y += 56
    d.text((MG, y), 'HOY', font=F_B, fill=DIM); y += 34
    swatches(d, MG, y, ('azul', 'lila', 'oro'), ('motor · azul', 'camino · lila', 'pieza · oro'))
    y += sw_h + 34
    d.text((MG, y), 'PROPUESTA · Madera clara', font=F_B, fill=LIME); y += 34
    swatches(d, MG, y, ('cielo', 'arena', 'uva'), ('motor · cielo (celeste)', 'camino · arena (madera clara)', 'pieza · uva (el morado de Razonamiento)'))
    y += sw_h + 34 + 20
    d.text((MG, y), 'LA MÁQUINA y EL COHETE · recortes al 100 %', font=F_H, fill=SUN); y += 56
    x = MG
    for im, lab, col in ((gm_h, 'HOY: lila, azul y oro', DIM), (gm_n, 'PROPUESTA: madera, celeste y uva', LIME), (ch_hoy, 'HOY: cohete', DIM), (ch_new, 'PROPUESTA: cohete', LIME)):
        sheet.paste(im, (x, y)); d.rectangle((x, y, x + im.width - 1, y + im.height - 1), outline=(70, 74, 130))
        d.text((x, y + max(gm_h.height, ch_hoy.height) + 8), lab, font=F_C, fill=col)
        x += im.width + GAP
    y += max(gm_h.height, ch_hoy.height) + 70

    def pair(title, a, b, la, lb, y):
        d.text((MG, y), title, font=F_H, fill=SUN); y += 56
        sheet.paste(a, (MG, y)); d.rectangle((MG, y, MG + a.width - 1, y + a.height - 1), outline=(70, 74, 130))
        sheet.paste(b, (MG + a.width + GAP, y)); d.rectangle((MG + a.width + GAP, y, MG + a.width + GAP + b.width - 1, y + b.height - 1), outline=(70, 74, 130))
        h = max(a.height, b.height)
        d.text((MG, y + h + 8), la, font=F_C, fill=DIM); d.text((MG + a.width + GAP, y + h + 8), lb, font=F_C, fill=LIME)
        return y + h + 70
    y = pair('EL AVISO DE UN ERROR · al 100 %', av_hoy_m, av_new_m, 'HOY: losa noche, letra ámbar y dorada', 'PROPUESTA: losa crema, letra en tinta y la moneda de error; crece con el texto', y)
    y = pair('EL AVISO DE UN ACIERTO · al 100 %', av_hoy_b, av_new_b, 'HOY: losa noche, letra menta', 'PROPUESTA: losa crema, letra en tinta y la moneda de acierto', y)
    sheet = sheet.crop((0, 0, W, y)); sheet.save(OUT)
    print('ok', OUT, sheet.size)
    print('caras', caras)
    print(mh); print(mn); print(mh8); print(mn8)


if __name__ == '__main__':
    main()
