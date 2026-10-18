# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» del marcador común de dos filas y de la fila de consigna (Tarea 127, `auditoria-fable/pulido-juegos.md` §1.1, §1.2 y §1.7): Tinta o Palabra, Rastro de luz, Correo Estelar y Piloto Estelar con sus capturas REALES de Unity
(`bash tools/verificar-todo.sh --capturas stroop,secuencia,correo,piloto`), y el tutorial de dos juegos con la fila fija de «Práctica (sin puntos)» y «Saltar tutorial».
«Hoy» son las tomas de antes de la tarea (se copian de una corrida anterior: variable NUBI_HOY_CAPTURAS = la carpeta unity/test-results/capturas de entonces) y «propuesta» las de esta.

Uso:
  python -I tools/previews/hud_consigna.py --tomar     copia (a 540 px de ancho) las tomas a docs/previews/hud-consigna/
  python -I tools/previews/hud_consigna.py             arma docs/previews/hud-consigna.png con lo que hay en docs/previews/hud-consigna/
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'docs', 'previews', 'hud-consigna')
OUT = os.path.join(ROOT, 'docs', 'previews', 'hud-consigna.png')
CAP = os.path.join(ROOT, 'unity', 'test-results', 'capturas')
HOY = os.environ.get('NUBI_HOY_CAPTURAS', '')
FONT = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'fredoka.ttf'); NUNITO = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'nunito_regular.ttf')
BG = (9, 11, 38); SUN = (255, 201, 60); DIM = (186, 190, 230); WHITE = (255, 255, 255)
PW = 310
# (id del juego, nombre, sufijo de la toma de juego, sufijo de la toma del tutorial o None)
GAMES = [('stroop', 'Tinta o Palabra', 't20', 'tutorial-3'), ('secuencia', 'Rastro de luz', 't20', 'tutorial-1'),
         ('correo', 'Correo Estelar', 'cinta-dorado', None), ('piloto', 'Piloto Estelar', 'senal-atrapada', None)]


def toma(folder, game, suffix):
    d = os.path.join(folder, game)
    if not folder or not os.path.isdir(d): return None
    for name in sorted(os.listdir(d)):
        if name.endswith('-' + suffix + '.png'): return os.path.join(d, name)
    return None


def chico(im, w=540):
    return im.convert('RGB').resize((w, int(im.height * w / im.width)), Image.LANCZOS)


def tomar():
    os.makedirs(SRC, exist_ok=True)
    faltan = []
    for g, _, play, tut in GAMES:
        for kind, suffix in (('play', play), ('tut', tut)):
            if suffix is None: continue
            for era, folder in (('hoy', HOY), ('prop', CAP)):
                p = toma(folder, g, suffix)
                if p: chico(Image.open(p)).save(os.path.join(SRC, '%s-%s-%s.png' % (era, kind, g)), optimize=True)
                else: faltan.append('%s-%s-%s' % (era, kind, g))
    # la pausa de Tinta: hoy, y la propuesta con su pregunta de «Reiniciar»
    for era, folder, suffix, name in (('hoy', HOY, 'pausa', 'hoy-pausa-stroop'), ('prop', CAP, 'pausa', 'prop-pausa-stroop'), ('prop', CAP, 'pausa-reiniciar', 'prop-pausa-reiniciar-stroop')):
        p = toma(folder, 'stroop', suffix)
        if p: chico(Image.open(p)).save(os.path.join(SRC, name + '.png'), optimize=True)
        else: faltan.append(name)
    # el velo del momento del objeto en Rescate: antes (0,58, la hoja del un solo final) y ahora (0,75)
    antes = os.path.join(ROOT, 'docs', 'previews', 'final-unico', 'prop-momento-radar.png')
    if os.path.exists(antes): Image.open(antes).convert('RGB').save(os.path.join(SRC, 'veil-antes-radar.png'), optimize=True)
    else: faltan.append('veil-antes-radar')
    p = toma(CAP, 'radar', 'momento')
    if p: chico(Image.open(p)).save(os.path.join(SRC, 'veil-ahora-radar.png'), optimize=True)
    else: faltan.append('veil-ahora-radar')
    print('tomadas; faltan:', faltan or 'ninguna')


def fuente(path, size):
    f = ImageFont.truetype(path, size)
    if path == FONT:
        try: f.set_variation_by_name('Bold')
        except Exception: pass
    return f


def abre(name):
    p = os.path.join(SRC, name + '.png')
    return Image.open(p).convert('RGB') if os.path.exists(p) else None


def telefono(im, w):
    if im is None:
        im = Image.new('RGB', (540, 1200), (30, 32, 70)); ImageDraw.Draw(im).text((20, 20), 'falta la toma', fill=WHITE)
    h = int(im.height * w / im.width)
    im = im.resize((w, h), Image.LANCZOS)
    mask = Image.new('L', (w, h), 0); ImageDraw.Draw(mask).rounded_rectangle((0, 0, w - 1, h - 1), 22, fill=255)
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0)); out.paste(im, (0, 0), mask)
    ImageDraw.Draw(out).rounded_rectangle((0, 0, w - 1, h - 1), 22, outline=(60, 64, 120, 255), width=2)
    return out


def main():
    if '--tomar' in sys.argv:
        tomar(); return
    f_t = fuente(FONT, 38); f_h = fuente(FONT, 27); f_s = fuente(NUNITO, 21); f_c = fuente(NUNITO, 19)
    gap, mar = 22, 50
    ph = int(1200 * PW / 540)
    Wt = 2 * mar + 4 * PW + 3 * gap
    block = 64 + ph + 30
    tut = [g for g in GAMES if g[3]]
    Ht = 150 + 2 * block + 60 + 56 + ph + 60 + 56 + ph + 60 + 56 + ph + 70
    sheet = Image.new('RGB', (Wt, Ht), BG)
    d = ImageDraw.Draw(sheet)
    d.text((mar, 28), 'Marcador común y fila de consigna: hoy | propuesta', font=f_t, fill=SUN)
    d.text((mar, 80), 'Fila 1: título a 18 dp y el avance en un chip. Fila 2: la consigna y, a la derecha, su fase. «Nivel» ya no se ve; «×N seguidos» aparece solo desde 2.', font=f_s, fill=DIM)
    d.text((mar, 106), 'Todas las tomas son capturas reales de Unity (verificar-todo.sh --capturas).', font=f_s, fill=DIM)
    y = 150
    for era, titulo, kind in (('hoy', 'HOY', 'play'), ('prop', 'PROPUESTA', 'play')):
        d.text((mar, y), titulo, font=f_h, fill=WHITE if era == 'hoy' else SUN)
        for i, (g, nombre, _, _) in enumerate(GAMES):
            x = mar + i * (PW + gap)
            d.text((x, y + 36), nombre, font=f_c, fill=DIM)
            im = telefono(abre('%s-%s-%s' % (era, kind, g)), PW)
            sheet.paste(im, (x, y + 64), im)
        y += block
    y += 20
    d.text((mar, y), 'Tutorial: los controles siempre arriba, en una fila (los 17 juegos)', font=f_h, fill=SUN)
    y += 56
    x = mar
    for g, nombre, _, tsuf in tut:
        for era in ('hoy', 'prop'):
            d.text((x, y - 24), '%s · %s' % (nombre, 'hoy' if era == 'hoy' else 'propuesta'), font=f_c, fill=DIM)
            im = telefono(abre('%s-tut-%s' % (era, g)), PW)
            sheet.paste(im, (x, y), im)
            x += PW + gap
    y += ph + 60
    d.text((mar, y), 'Pausa: «Reiniciar» pregunta antes de tirar la partida; el subtítulo sube a 16 dp', font=f_h, fill=SUN)
    y += 56
    x = mar
    for name, leg in (('hoy-pausa-stroop', 'hoy'), ('prop-pausa-stroop', 'propuesta'), ('prop-pausa-reiniciar-stroop', 'propuesta: al tocar «Reiniciar»')):
        d.text((x, y - 24), 'Tinta o Palabra · ' + leg, font=f_c, fill=DIM)
        im = telefono(abre(name), PW)
        sheet.paste(im, (x, y), im)
        x += PW + gap
    y += ph + 60
    d.text((mar, y), 'El momento del objeto: el velo sube de 0,58 a 0,75 (el marcador ya se apagaba: texto a 44 %)', font=f_h, fill=SUN)
    y += 56
    x = mar
    for name, leg in (('veil-antes-radar', 'Rescate · velo 0,58'), ('veil-ahora-radar', 'Rescate · velo 0,75')):
        d.text((x, y - 24), leg, font=f_c, fill=DIM)
        im = telefono(abre(name), PW)
        sheet.paste(im, (x, y), im)
        x += PW + gap
    sheet.save(OUT, optimize=True)
    print('ok', OUT, sheet.size)


if __name__ == '__main__':
    main()
