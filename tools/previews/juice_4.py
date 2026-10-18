# -*- coding: utf-8 -*-
"""Lámina «antes · durante · después» del «juice» de Carga exacta, Tinta o Palabra, En la punta de la lengua y Rastro de luz (Tarea 130, `auditoria-fable/spec-kit-juice-ajustes.md` §2). Todas las tomas son del juego REAL
de Unity (`NUBI_SHOTS_JUICE=1`: `ScreenshotRunner.JuiceShots` hace la jugada por reflexión y saca una toma antes, ~0,12 s durante y después). Únicas salvedades, dichas en la propia lámina:
  · Rastro de luz: la persona tendría que tocar los luceros con el dedo; el guion arma los tramos 1 y 2 del hilo y el 3 a 0,10 s de la toma «durante» (el hilo es el que dibuja el juego).
  · «Durante» de Tinta: el aro blanco que se ve sobre el botón es la chispa que ya existía (acierto).

Uso:
  python -I tools/previews/juice_4.py --tomar     copia (a 540 px de ancho) las tomas de NUBI_JUICE_CAPTURAS (por defecto unity/test-results/capturas-juice) a docs/previews/juice-4/
  python -I tools/previews/juice_4.py             arma docs/previews/juice-4.png con lo que hay en docs/previews/juice-4/
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'docs', 'previews', 'juice-4')
OUT = os.path.join(ROOT, 'docs', 'previews', 'juice-4.png')
CAP = os.environ.get('NUBI_JUICE_CAPTURAS') or os.path.join(ROOT, 'unity', 'test-results', 'capturas-juice')
FONT = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'fredoka.ttf'); NUNITO = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font', 'nunito_regular.ttf')
BG = (9, 11, 38); SUN = (255, 201, 60); DIM = (186, 190, 230); WHITE = (255, 255, 255)
PW = 330
GAMES = [('calculo', 'Carga exacta', 'el aro de 12 luces se llena con la mejor celda que no se pasa, en cascada; la celda que recibe late con rebote; al dar justo, el núcleo late'),
         ('stroop', 'Tinta o Palabra', 'la moneda de acierto entra en el botón tocado (0,30 s); al errar, la de error en el tocado y la de acierto en el correcto. Nunca se tiñe la losa'),
         ('anagramas', 'En la punta de la lengua', 'con «¡La tengo!» la tarjeta se hace a un lado (0,94, sube 10 dp) y las fichas nacen desde su borde; al completar, la palabra late una vez'),
         ('secuencia', 'Rastro de luz', 'el hilo toma cuerpo (11 del color del modo + 7 sol) y cada tramo se tiende en 0,20 s desde el lucero anterior')]
STEPS = [('1-antes', 'ANTES'), ('2-durante', 'DURANTE'), ('3-despues', 'DESPUÉS')]
NOTAS = {('calculo', '3-despues'): 'tras la fusión', ('stroop', '3-despues'): 'al errar: error y acierto', ('secuencia', '2-durante'): 'estado armado por el guion', ('secuencia', '3-despues'): 'estado armado por el guion'}


def toma(game, step):
    d = os.path.join(CAP, game)
    if not os.path.isdir(d): return None
    for name in sorted(os.listdir(d)):
        if name.endswith('-' + step + '.png'): return os.path.join(d, name)
    return None


def chico(im, w=540):
    return im.convert('RGB').resize((w, int(im.height * w / im.width)), Image.LANCZOS)


def tomar():
    os.makedirs(SRC, exist_ok=True)
    faltan = []
    for g, _, _ in GAMES:
        for step, _ in STEPS:
            p = toma(g, step)
            if p: chico(Image.open(p)).save(os.path.join(SRC, '%s-%s.png' % (g, step)), optimize=True)
            else: faltan.append('%s-%s' % (g, step))
    print('tomadas; faltan:', faltan or 'ninguna')


def fuente(path, size):
    f = ImageFont.truetype(path, size)
    if path == FONT:
        try: f.set_variation_by_name('Bold')
        except Exception: pass
    return f


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
    f_t = fuente(FONT, 38); f_h = fuente(FONT, 25); f_s = fuente(NUNITO, 21); f_c = fuente(NUNITO, 18)
    gap, mar, lab = 22, 50, 0
    ph = int(2400 * PW / 1080)
    Wt = 2 * mar + len(GAMES) * PW + (len(GAMES) - 1) * gap
    Ht = 270 + 3 * (ph + 70) + 80
    sheet = Image.new('RGB', (Wt, Ht), BG)
    d = ImageDraw.Draw(sheet)
    d.text((mar, 28), 'Juice en cuatro juegos: antes · durante · después', font=f_t, fill=SUN)
    y = 82
    for texto in ('Todas las tomas son del juego REAL de Unity: un guion hace la jugada y saca la toma antes, ~0,12 s durante y después (verificar-todo no cambia: `NUBI_SHOTS_JUICE=1`). Ningún sonido nuevo; ninguna medida ni tiempo de ensayo cambia; con «quitar animaciones» cada cosa va a su estado final de una vez.',
                  'Salvedad de Rastro de luz: el guion arma los tramos del hilo (tocar los luceros de verdad pide un dedo); el hilo mismo lo dibuja el juego.'):
        for linea in envuelve(d, texto, f_s, Wt - 2 * mar):
            d.text((mar, y), linea, font=f_s, fill=DIM); y += 26
    y += 18
    # encabezados de columna: el juego y qué cambia
    for i, (g, nombre, leyenda) in enumerate(GAMES):
        x = mar + i * (PW + gap)
        d.text((x, y), nombre, font=f_h, fill=WHITE)
        for j, linea in enumerate(envuelve(d, leyenda, f_c, PW)[:4]):
            d.text((x, y + 32 + j * 22), linea, font=f_c, fill=DIM)
    y += 150
    for step, titulo in STEPS:
        d.text((mar, y), titulo, font=f_h, fill=SUN)
        y += 34
        for i, (g, nombre, _) in enumerate(GAMES):
            x = mar + i * (PW + gap)
            p = os.path.join(SRC, '%s-%s.png' % (g, step))
            im = telefono(Image.open(p).convert('RGB') if os.path.exists(p) else None, PW)
            sheet.paste(im, (x, y), im)
            nota = NOTAS.get((g, step))
            if nota: d.text((x + 8, y + ph - 28), nota, font=f_c, fill=SUN)
        y += ph + 24
    sheet = sheet.crop((0, 0, Wt, y))
    sheet.save(OUT, optimize=True)
    print('ok', OUT, sheet.size)


if __name__ == '__main__':
    main()
