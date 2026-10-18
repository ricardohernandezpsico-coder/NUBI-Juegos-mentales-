# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» del pulgar, los tamaños y la letra 16 (Tarea 128, `auditoria-fable/pulido-juegos.md` §1.8, §1.9 y la tabla de §3): Constelaciones, Rastro de luz, Estrella intrusa, Bodega de carga y Carga exacta con lo que se toca MÁS ABAJO;
Lluvia de meteoros, Piloto Estelar, Satélites y Freno con lo que se ve más GRANDE; y el tutorial de Acoplamiento en la pantalla más baja (1080 × 1920) con sus botones de 48 dp o más. Todas son capturas REALES de Unity
(`bash tools/verificar-todo.sh --capturas parejas,secuencia,intrusa,bodega,calculo,meteoros,piloto,satelites,freno,acoplamiento`).
«Hoy» son las tomas de antes de la tarea (variable NUBI_HOY_CAPTURAS = la carpeta de capturas de entonces, con la misma estructura que unity/test-results/capturas) y «propuesta» las de esta.

Uso:
  python -I tools/previews/pulgar_tamanos.py --tomar     copia (a 540 px de ancho) las tomas a docs/previews/pulgar-tamanos/
  python -I tools/previews/pulgar_tamanos.py             arma docs/previews/pulgar-tamanos.png con lo que hay en docs/previews/pulgar-tamanos/
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'docs', 'previews', 'pulgar-tamanos')
OUT = os.path.join(ROOT, 'docs', 'previews', 'pulgar-tamanos.png')
CAP = os.path.join(ROOT, 'unity', 'test-results', 'capturas')
HOY = os.environ.get('NUBI_HOY_CAPTURAS', '')
FONT = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'fredoka.ttf'); NUNITO = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'nunito_regular.ttf')
BG = (9, 11, 38); SUN = (255, 201, 60); DIM = (186, 190, 230); WHITE = (255, 255, 255)
PW = 310
# (id del juego, nombre, sufijo de la toma, leyenda de lo que cambia)
PULGAR = [('parejas', 'Constelaciones', 'primera', 'el cielo baja ~16 % de la altura; mismas luces, tablero completo'),
          ('secuencia', 'Rastro de luz', 't08', 'el anillo baja ~10 % de la altura'),
          ('intrusa', 'Estrella intrusa', 'primera', 'el campo baja; placas de palabra a 18 dp'),
          ('bodega', 'Bodega de carga', 'primera', 'el carro de reparto, justo bajo el anillo'),
          ('calculo', 'Carga exacta', 'primera', 'reactor, celdas y botones, en la mitad de abajo')]
TAMANOS = [('meteoros', 'Lluvia de meteoros', 'primera', 'nacen al 20 % de la altura (de ~17 %), con el mismo tiempo de caída'),
           ('piloto', 'Piloto Estelar', 'ruta-balizas', 'señales de ~44 a ~56 dp'),
           ('satelites', 'Satélites', 'seguimiento', 'satélites de ~40 a ~48 dp'),
           ('freno', 'Freno', 'tutorial-3', 'el ALTO de ~133 a 160 dp (ningún tiempo cambia)')]
# el tutorial de Acoplamiento en la pantalla más baja: 1080 × 1920 (sufijo «-16x9»)
ACOP = ('acoplamiento', 'Acoplamiento · práctica', 'tutorial-3-16x9', 'en 640 dp: botones de 48 dp o más, nada por debajo')


def toma(folder, game, suffix):
    d = os.path.join(folder, game) if folder else ''
    if not folder or not os.path.isdir(d): return None
    for name in sorted(os.listdir(d)):
        if name.endswith('-' + suffix + '.png'): return os.path.join(d, name)
    return None


def chico(im, w=540):
    return im.convert('RGB').resize((w, int(im.height * w / im.width)), Image.LANCZOS)


def tomar():
    os.makedirs(SRC, exist_ok=True)
    faltan = []
    for g, _, suffix, _ in PULGAR + TAMANOS + [ACOP]:
        for era, folder in (('hoy', HOY), ('prop', CAP)):
            p = toma(folder, g, suffix)
            if p: chico(Image.open(p)).save(os.path.join(SRC, '%s-%s.png' % (era, g)), optimize=True)
            else: faltan.append('%s-%s' % (era, g))
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


def envuelve(d, texto, font, ancho):
    lineas, actual = [], ''
    for palabra in texto.split():
        prueba = (actual + ' ' + palabra).strip()
        if d.textlength(prueba, font=font) <= ancho: actual = prueba
        else: lineas.append(actual); actual = palabra
    if actual: lineas.append(actual)
    return lineas


def main():
    if '--tomar' in sys.argv:
        tomar(); return
    f_t = fuente(FONT, 38); f_h = fuente(FONT, 27); f_s = fuente(NUNITO, 21); f_c = fuente(NUNITO, 19)
    gap, mar = 22, 50
    ph = int(2400 * PW / 1080)                   # la toma de 20:9; la de Acoplamiento (9:16) queda más baja
    Wt = 2 * mar + 5 * PW + 4 * gap
    block = 64 + ph + 104
    Ht = 260 + 4 * block + 100
    sheet = Image.new('RGB', (Wt, Ht), BG)
    d = ImageDraw.Draw(sheet)
    d.text((mar, 28), 'Pulgar, tamaños y letra 16: hoy | propuesta', font=f_t, fill=SUN)
    yy = 80
    for texto in ('Lo que se toca baja hacia el pulgar y lo que se mira crece; ni la mecánica, ni el ritmo, ni las cantidades cambian. Ningún botón tocable queda por debajo de 48 dp y nada informativo por debajo de 16 dp.',
                  'Todas las tomas son capturas reales de Unity (verificar-todo.sh --capturas), en la pantalla de 20:9 salvo el tutorial de Acoplamiento, que va en la más baja (1080 × 1920). La línea punteada de Meteoros marca el 20 % de la altura.'):
        for linea in envuelve(d, texto, f_s, Wt - 2 * mar):
            d.text((mar, yy), linea, font=f_s, fill=DIM); yy += 26
    y = yy + 24

    def fila(juegos, titulo, y):
        d.text((mar, y), titulo, font=f_h, fill=SUN)
        y += 44
        for era, nombre_era in (('hoy', 'HOY'), ('prop', 'PROPUESTA')):
            d.text((mar, y), nombre_era, font=f_h, fill=WHITE if era == 'hoy' else SUN)
            for i, (g, nombre, _, leyenda) in enumerate(juegos):
                x = mar + i * (PW + gap)
                d.text((x, y + 36), nombre, font=f_c, fill=DIM)
                im = telefono(abre('%s-%s' % (era, g)), PW)
                sheet.paste(im, (x, y + 64), im)
                if g == 'meteoros':                  # el 20 % de la altura: dónde nacen las rocas
                    ly = y + 64 + int(im.height * 0.20)
                    for lx in range(x, x + PW, 14): d.line([(lx, ly), (lx + 7, ly)], fill=SUN, width=2)
                    d.text((x + PW - 164, ly + 4), '20 % de la altura', font=f_c, fill=SUN)
                if era == 'prop':
                    for j, linea in enumerate(envuelve(d, leyenda, f_c, PW)):
                        d.text((x, y + 64 + im.height + 8 + j * 24), linea, font=f_c, fill=SUN)
            y += block
        return y

    y = fila(PULGAR, 'El pulgar: lo que se toca queda en la mitad de abajo', y)
    y += 40
    y = fila(TAMANOS + [ACOP], 'Los tamaños: lo que se mira crece (y el tutorial de Acoplamiento en la pantalla más baja)', y)
    sheet = sheet.crop((0, 0, Wt, y - 40))
    sheet.save(OUT, optimize=True)
    print('ok', OUT, sheet.size)


if __name__ == '__main__':
    main()
