# -*- coding: utf-8 -*-
"""Lámina de la Tarea 186, «En qué se basa Nubi, ampliada» (`docs/previews/tarea-186.png`), con CAPTURAS REALES; no son maquetas.
  1. «Más estudios» de un juego (Constelaciones), plegado y desplegado, con letra 1,0 y 1,3.
  2. «Cómo está hecha Nubi» al principio y al final, con letra 1,0 y 1,3.
  3. «Lo que no decimos» ampliada (Simons, Melby-Lervåg, Sala y Gates, y el aviso de salud), con letra 1,0 y 1,3.
  4. Las medidas.
  5. Las líneas que se corrigieron (antes → después) y las cuatro nuevas.
  6. Los textos de las revisiones 5a (racha cortada) y 5b (Rastro de luz) con su propuesta: solo informe, sin cambios en la app.
Entradas (fuera de git): `app/src/test/screenshots/tarea-186/propuesta/*.png` (`Tarea186CapturasTest`, solo al grabar); en git: `tools/previews/tarea_186_medidas.txt` y `docs/bibliografia/t186-lineas-y-revisiones.md`.
Uso:  python -I tools/previews/tarea_186.py
"""
import os, re, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PROP = os.path.join(ROOT, 'app', 'src', 'test', 'screenshots', 'tarea-186', 'propuesta')
MEDIDAS = os.path.join(HERE, 'tarea_186_medidas.txt')
REVISION = os.path.join(ROOT, 'docs', 'bibliografia', 't186-lineas-y-revisiones.md')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
OUT = os.path.join(ROOT, 'docs', 'previews', 'tarea-186.png')
NAVY, SUN, DIM, CREAM, LILAC = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (184, 164, 255)
GAP = 36
COL_W = 1080
W = 2 * COL_W + 3 * GAP


def font(px, kind='fredoka'):
    return ImageFont.truetype(os.path.join(FONTS, {'rubik': 'rubik_bold.ttf', 'fredoka': 'fredoka.ttf', 'nunito': 'nunito_regular.ttf'}[kind]), int(px))


def clean(s):
    return re.sub(r'`', '', re.sub(r'\*\*', '', s)).strip()


def wrap(d, text, f, width):
    lines = []
    for para in text.split('\n'):
        cur = ''
        for word in para.split(' '):
            trial = (cur + ' ' + word).strip()
            if d.textlength(trial, font=f) <= width or not cur:
                cur = trial
            else:
                lines.append(cur); cur = word
        lines.append(cur)
    return lines


def text_height(d, text, f, width, step):
    return len(wrap(d, text, f, width)) * step


def draw_text(d, x, y, text, f, fill, width, step):
    for line in wrap(d, text, f, width):
        d.text((x, y), line, font=f, fill=fill); y += step
    return y


def load(name):
    p = os.path.join(PROP, name)
    return Image.open(p).convert('RGB') if os.path.exists(p) else None


def parse_revision():
    t = open(REVISION, encoding='utf-8').read().replace('\r\n', '\n')
    cut = t.index('### Líneas nuevas')
    changes = re.findall(r'- \*\*(.+?)\*\*\n  - antes: (.+)\n  - después: (.+)', t[:cut])
    nuevas_txt = t[cut:t.index('### Decisiones')]
    nuevas = re.findall(r'- \*\*(.+?)\*\* \(`[^`]+`\): (.+)', nuevas_txt)
    s2 = t[t.index('## 2. Revisión 5a'):t.index('## 3. Revisión 5b')]
    s3 = t[t.index('## 3. Revisión 5b'):]

    def blocks(s):
        resultado = re.search(r'\*\*Resultado:\*\* (.+)', s).group(1)
        prop = s[s.index('**Propuesta de textos**'):]
        bullets = re.findall(r'^- (.+)$', prop, re.M)
        return clean(resultado), [clean(b) for b in bullets]
    return changes, nuevas, blocks(s2), blocks(s3)


def main():
    changes, nuevas, (res_a, prop_a), (res_b, prop_b) = parse_revision()
    measures = [l.strip() for l in open(MEDIDAS, encoding='utf-8').read().splitlines() if l.strip()]
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    f_title, f_h, f_cap, f_txt, f_small = font(54, 'rubik'), font(44, 'rubik'), font(34), font(30, 'nunito'), font(28, 'nunito')

    def pair(left, right, labels):
        a, b = load(left), load(right)
        return [(a, labels[0]), (b, labels[1])]

    sections = [
        ('1. «Más estudios» en cada juego: plegado y desplegado', 'Bajo la losa de Constelaciones. Lila, de 48 dp, plegado por defecto; al tocarlo se despliegan en el mismo lugar sus estudios, cada uno con su cita, su línea y «Ver el estudio ›».',
         [pair('mas-plegado-1_0.png', 'mas-desplegado-1_0.png', ('Letra 1,0 · plegado', 'Letra 1,0 · desplegado')), pair('mas-plegado-1_3.png', 'mas-desplegado-1_3.png', ('Letra 1,3 · plegado', 'Letra 1,3 · desplegado'))]),
        ('2. «Cómo está hecha Nubi»: al principio y al final', 'Entre los juegos y «Las palabras»: diez grupos, cada uno con su frase y sus fuentes. «La dificultad se acomoda a ti» absorbe lo que era «Cómo se ajusta la dificultad».',
         [pair('mecanismos-inicio-1_0.png', 'mecanismos-final-1_0.png', ('Letra 1,0 · el principio', 'Letra 1,0 · el final')), pair('mecanismos-inicio-1_3.png', 'mecanismos-final-1_3.png', ('Letra 1,3 · el principio', 'Letra 1,3 · el final'))]),
        ('3. «Lo que no decimos», ampliada', 'Simons 2016 y tres revisiones más (Melby-Lervåg 2016, Sala 2018 y Gates 2020, cada una con su línea); el aviso de salud sigue al final.',
         [pair('no-decimos-1_0.png', 'no-decimos-1_3.png', ('Letra 1,0', 'Letra 1,3'))]),
    ]

    # ---- primero se mide todo para saber el alto
    y_total = 40 + 70 + 70
    plan = []
    for title, note, rows in sections:
        y_total += 70 + text_height(probe, note, f_txt, W - 2 * GAP, 40) + 20
        for row in rows:
            h = max((im.height for im, _ in row if im is not None), default=0)
            y_total += h + 60 + 40
    # medidas
    meas_h = sum(text_height(probe, '• ' + m, f_txt, W - 2 * GAP, 40) + 8 for m in measures)
    y_total += 70 + meas_h + 40
    # antes → después, en dos columnas
    col = (W - 3 * GAP) // 2

    def entry_h(c, a, d):
        return text_height(probe, c, f_cap, col, 38) + text_height(probe, 'antes: ' + a, f_small, col, 34) + text_height(probe, 'después: ' + d, f_small, col, 34) + 24
    cheights = [entry_h(*c) for c in changes]
    half = sum(cheights) / 2
    acc, split = 0, 0
    for i, h in enumerate(cheights):
        acc += h
        if acc >= half: split = i + 1; break
    left_h, right_h = sum(cheights[:split]), sum(cheights[split:])
    y_total += 70 + text_height(probe, 'Las otras líneas quedaron como venían de las tablas de origen.', f_txt, W - 2 * GAP, 40) + max(left_h, right_h) + 40
    new_h = sum(text_height(probe, '%s: %s' % n, f_small, W - 2 * GAP, 34) + 10 for n in nuevas)
    y_total += 70 + new_h + 40
    # 5a y 5b
    rev_h = 0
    for res, props in ((res_a, prop_a), (res_b, prop_b)):
        rev_h += 70 + text_height(probe, res, f_small, W - 2 * GAP, 36) + 20 + sum(text_height(probe, '• ' + p, f_txt, W - 2 * GAP, 40) + 10 for p in props) + 40
    y_total += rev_h + 60

    sheet = Image.new('RGB', (W, y_total), NAVY)
    d = ImageDraw.Draw(sheet)
    y = 40
    d.text((GAP, y), 'Tarea 186 · «En qué se basa Nubi», ampliada', font=f_title, fill=SUN); y += 80
    d.text((GAP, y), 'Capturas reales (Roborazzi, 393 dp de ancho). Las 69 líneas nuevas son borradores; solo informe en 5a y 5b.', font=f_txt, fill=DIM); y += 60
    for title, note, rows in sections:
        d.text((GAP, y), title, font=f_h, fill=SUN); y += 70
        y = draw_text(d, GAP, y, note, f_txt, DIM, W - 2 * GAP, 40) + 20
        for row in rows:
            h = max((im.height for im, _ in row if im is not None), default=0)
            for i, (im, label) in enumerate(row):
                x = GAP + i * (COL_W + GAP)
                if im is not None: sheet.paste(im, (x, y))
                d.text((x, y + h + 12), label, font=f_cap, fill=LILAC)
            y += h + 60 + 40
    d.text((GAP, y), '4. Las medidas', font=f_h, fill=SUN); y += 70
    for m in measures:
        y = draw_text(d, GAP, y, '• ' + m, f_txt, CREAM, W - 2 * GAP, 40) + 8
    y += 40
    d.text((GAP, y), '5. Las líneas que se corrigieron, antes y después: %d de 69' % len(changes), font=f_h, fill=SUN); y += 70
    y = draw_text(d, GAP, y, 'Las otras líneas quedaron como venían de las tablas de origen.', f_txt, DIM, W - 2 * GAP, 40)
    top = y
    for ci, (items, x) in enumerate(((changes[:split], GAP), (changes[split:], 2 * GAP + col))):
        yy = top
        for c, a, dd in items:
            yy = draw_text(d, x, yy, c, f_cap, SUN, col, 38)
            yy = draw_text(d, x, yy, 'antes: ' + a, f_small, DIM, col, 34)
            yy = draw_text(d, x, yy, 'después: ' + dd, f_small, CREAM, col, 34) + 24
    y = top + max(left_h, right_h) + 40
    d.text((GAP, y), 'Líneas nuevas (los cuatro estudios que ya estaban y no tenían línea)', font=f_h, fill=SUN); y += 70
    for n in nuevas:
        y = draw_text(d, GAP, y, '%s: %s' % n, f_small, CREAM, W - 2 * GAP, 34) + 10
    y += 40
    for title, (res, props) in (('6a. Revisión 5a · cuando una racha se corta (solo informe y propuesta)', (res_a, prop_a)), ('6b. Revisión 5b · cómo presenta Rastro de luz su resultado (solo informe y propuesta)', (res_b, prop_b))):
        d.text((GAP, y), title, font=f_h, fill=SUN); y += 70
        y = draw_text(d, GAP, y, 'Resultado: ' + res, f_small, DIM, W - 2 * GAP, 36) + 20
        for p in props:
            y = draw_text(d, GAP, y, '• ' + p, f_txt, CREAM, W - 2 * GAP, 40) + 10
        y += 40
    sheet = sheet.crop((0, 0, W, min(sheet.height, y + 20)))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet.save(OUT, optimize=True)
    print('ok', OUT, sheet.size)


main()
