# -*- coding: utf-8 -*-
"""Lámina «hoy | propuesta» del kit en lo que falta (Tarea 129, paso 1; `auditoria-fable/spec-kit-juice-ajustes.md` §1): Carga exacta, En la punta de la lengua, Bodega de carga y La estación de correo (con «NUEVO» y «Encargos de hoy») y, en el paso 2, Engranajes, Piloto y los extras (Satélites y Rescate)
con sus capturas REALES de Unity (`bash tools/verificar-todo.sh --capturas calculo,anagramas,bodega,correo`). Los paneles y botones ya eran losas del kit, pero en tono noche sobre un cielo navy; ahora la información va en crema (letra en tinta),
los botones secundarios en lila y lo calibrado sobre oscuro (los planetas de los buzones, la carta o la torre, el objeto) queda en su ventanilla noche.
«Hoy» son las tomas de antes de la tarea (variable NUBI_HOY_CAPTURAS = una carpeta PLANA con `<juego>-<toma>.png`) y «propuesta» las de esta (unity/test-results/capturas/<juego>/NN-<toma>.png).

Uso:
  python -I tools/previews/kit_lo_que_falta.py --tomar     copia (a 540 px de ancho) las tomas a docs/previews/kit-lo-que-falta/
  python -I tools/previews/kit_lo_que_falta.py             arma docs/previews/kit-lo-que-falta.png con lo que hay en esa carpeta
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'docs', 'previews', 'kit-lo-que-falta')
OUT = os.path.join(ROOT, 'docs', 'previews', 'kit-lo-que-falta.png')
CAP = os.path.join(ROOT, 'unity', 'test-results', 'capturas')
HOY = os.environ.get('NUBI_HOY_CAPTURAS', '')
FONT = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'fredoka.ttf'); NUNITO = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'nunito_regular.ttf')
BG = (9, 11, 38); SUN = (255, 201, 60); DIM = (186, 190, 230); WHITE = (255, 255, 255)
PW = 310
# (carpeta de las tomas, nombre, sufijo de la toma, leyenda de lo que cambia)
PRIMERA = [('calculo', 'Carga exacta', 'primera', 'operaciones y botones en lila con tinta; «Empezar de nuevo» pide un segundo toque'),
           ('anagramas', 'En la punta de la lengua', 'primera', 'la definición en una lámina crema; botones lila; «Ayudas» a 16 dp'),
           ('bodega', 'Bodega de carga', 'primera', 'la tarjeta del pedido en crema; el objeto en su disco oscuro'),
           ('correo', 'La estación de correo', 'cinta-dorado', 'los buzones, la caja fuerte y el faro siguen en noche (sus planetas se calibraron sobre oscuro)'),
           ('correo', 'Correo · «NUEVO»', 'nuevo-estacion', 'lámina crema; el arte en su ventanilla noche'),
           ('correo', 'Correo · «Encargos de hoy»', 'hoja', 'filas crema con el ícono en ventanilla; «TODOS LOS DÍAS» en sol')]
PASO2 = [('engranajes', 'Engranajes · primera pantalla', 'primera', 'carteles y rótulo MOTOR en losas chicas crema; la cuenta de cambios en crema (número a 24 dp)'),
         ('engranajes', 'Engranajes · a los 40 s', 't40', 'el cartel cumplido o fallido sigue oscuro (verde o granate)'),
         ('piloto', 'Piloto · «¡Tu misión!»', 'inicio-suave', 'el aviso en crema con la señal en su ventanilla noche'),
         ('piloto', 'Piloto · «¡HIPERIMPULSO!»', 'hiperimpulso', 'los hitos, en sol con tinta'),
         ('satelites', 'Satélites · sorpresa', 'aviso-nube', 'el aviso en lámina crema; el seguimiento sigue plano'),
         ('radar', 'Rescate · «Lluvia de cápsulas»', 'lluvia', 'la franja de avisos en crema; las casillas siguen noche')]
TAMBIEN = [('calculo', 'Carga exacta · a los 40 s', 't40', 'el reactor, las celdas y los botones'),
           ('anagramas', 'Punta · a los 40 s', 't40', 'la tarjeta crema con el texto que va apareciendo'),
           ('bodega', 'Bodega · a los 40 s', 't40', 'la tarjeta y el carro de reparto a 16 dp'),
           ('correo', 'Correo · «¡Llega un saco!»', 'saco', 'el aviso en crema con la tinta'),
           ('correo', 'Correo · RADIO', 'radio', 'el aviso de la radio en crema'),
           ('correo', 'Correo · fin del día', 'resumen', 'filas crema; el chip «TODOS LOS DÍAS» en sol')]


def toma(folder, game, suffix, flat=False):
    if not folder: return None
    if flat:
        p = os.path.join(folder, '%s-%s.png' % (game, suffix))
        return p if os.path.exists(p) else None
    d = os.path.join(folder, game)
    if not os.path.isdir(d): return None
    for name in sorted(os.listdir(d)):
        if name.endswith('-' + suffix + '.png'): return os.path.join(d, name)
    return None


def chico(im, w=540):
    return im.convert('RGB').resize((w, int(im.height * w / im.width)), Image.LANCZOS)


def clave(game, suffix, era): return '%s-%s-%s' % (era, game, suffix)


def tomar():
    os.makedirs(SRC, exist_ok=True)
    faltan = []
    for g, _, suffix, _ in PRIMERA + TAMBIEN + PASO2:
        for era, folder, flat in (('hoy', HOY, True), ('prop', CAP, False)):
            p = toma(folder, g, suffix, flat)
            if p: chico(Image.open(p)).save(os.path.join(SRC, clave(g, suffix, era) + '.png'), optimize=True)
            else: faltan.append(clave(g, suffix, era))
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
    ph = int(2400 * PW / 1080)
    Wt = 2 * mar + 6 * PW + 5 * gap
    block = 64 + ph + 110
    sheet = Image.new('RGB', (Wt, 300 + 6 * block), BG)
    d = ImageDraw.Draw(sheet)
    d.text((mar, 28), 'El kit en lo que falta: hoy | propuesta', font=f_t, fill=SUN)
    yy = 80
    for texto in ('Los paneles y botones ya eran losas del kit, pero en tono noche sobre un cielo del mismo azul: la sombra y el canto no se veían. Ahora la información va en crema con la letra en tinta, los botones secundarios en lila, '
                  'y lo que se calibró sobre oscuro (planetas, cartas, objetos) queda en su ventanilla noche. Nada de la mecánica cambia.',
                  'Todas las tomas son capturas reales de Unity (verificar-todo.sh --capturas), en la pantalla de 20:9. Texto informativo a 16 dp o más.'):
        for linea in envuelve(d, texto, f_s, Wt - 2 * mar):
            d.text((mar, yy), linea, font=f_s, fill=DIM); yy += 26
    y = yy + 24

    def fila(juegos, titulo, y):
        d.text((mar, y), titulo, font=f_h, fill=SUN)
        y += 44
        for era, nombre_era in (('hoy', 'HOY'), ('prop', 'PROPUESTA')):
            d.text((mar, y), nombre_era, font=f_h, fill=WHITE if era == 'hoy' else SUN)
            for i, (g, nombre, suffix, leyenda) in enumerate(juegos):
                x = mar + i * (PW + gap)
                d.text((x, y + 36), nombre, font=f_c, fill=DIM)
                im = telefono(abre(clave(g, suffix, era)), PW)
                sheet.paste(im, (x, y + 64), im)
                if era == 'prop':
                    for j, linea in enumerate(envuelve(d, leyenda, f_c, PW)):
                        d.text((x, y + 64 + im.height + 8 + j * 24), linea, font=f_c, fill=SUN)
            y += block
        return y

    y = fila(PRIMERA, 'Carga, Punta, Bodega y Correo: primera pantalla jugable, «NUEVO» y «Encargos de hoy»', y)
    y += 40
    y = fila(TAMBIEN, 'También cambian', y)
    y += 40
    y = fila(PASO2, 'Paso 2: Engranajes, Piloto y los extras (Satélites y Rescate)', y)
    sheet = sheet.crop((0, 0, Wt, y - 40))
    sheet.save(OUT, optimize=True)
    print('ok', OUT, sheet.size)


if __name__ == '__main__':
    main()
