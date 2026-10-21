# -*- coding: utf-8 -*-
"""Hoja «hoy | propuesta» de la Tarea 154 (21-oct-2026; `auditoria-fable/unity-vocabulario.md` y B8 de `auditoria-coherencia-20-oct.md`): lo que dibuja Unity habla como la app.
CAPTURAS REALES de Unity (`bash tools/verificar-todo.sh --capturas todos`, lienzo de teléfono 1080×2400 en `unity/test-results/capturas/`). «HOY» = las láminas de contacto del commit anterior (`git show HEAD:docs/previews/capturas/<juego>.png`, un cuarto de tamaño);
«PROPUESTA» = las tomas nuevas. Las tarjetas «Nuevo» de Bodega, Engranajes y Constelaciones solo salen en la primera partida de por vida (Unity guarda que ya se vieron en las preferencias): se capturan borrando esas marcas (`bod_intros`, `engr_intros2`, `con_intros`) y
sin el piloto automático de esos juegos; esas tomas se leen de la carpeta que diga la variable `NUBI_INTROS` (por defecto `%TEMP%/capt_keep`, con `intro_<juego>.png`).
Uso:  python -I tools/previews/tarea_154.py      → docs/previews/tarea-154.png
"""
import io, os, subprocess, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PREV = os.path.join(ROOT, 'docs', 'previews'); OUT = os.path.join(PREV, 'tarea-154.png')
SHOTS = os.path.join(ROOT, 'unity', 'test-results', 'capturas')
INTROS = os.environ.get('NUBI_INTROS') or os.path.join(os.environ.get('TEMP', ''), 'capt_keep')
FONTS = os.path.join(ROOT, 'app', 'src', 'main', 'res', 'font')
NAVY, SUN, DIM, CREAM, LIME, CORAL = (20, 18, 63), (255, 203, 69), (200, 196, 235), (245, 235, 219), (155, 229, 100), (255, 140, 120)
W, GAP, MARGIN, COLS = 300, 16, 40, 4


def font(px, bold=True): return ImageFont.truetype(os.path.join(FONTS, 'fredoka.ttf' if bold else 'nunito_regular.ttf'), int(px))


def old_tile(game, index):
    """La toma número [index] (de 0) de la lámina de contacto del commit anterior: 5 columnas de 270×600 con 18 de margen, 70 de cabecera y filas de 672."""
    raw = subprocess.run(['git', '-C', ROOT, 'show', 'HEAD:docs/previews/capturas/%s.png' % game], capture_output=True, check=True).stdout
    sheet = Image.open(io.BytesIO(raw)).convert('RGB')
    r, c = divmod(index, 5)
    x, y = 18 + c * 288, 70 + r * 672
    return sheet.crop((x, y, x + 270, y + 600)).resize((W, round(W * 600 / 270)), Image.LANCZOS)


def new_tile(path):
    im = Image.open(path).convert('RGB')
    return im.resize((W, round(W * im.height / im.width)), Image.LANCZOS)


def wrap(d, text, fnt, width):
    lines, cur = [], ''
    for word in text.split(' '):
        t = (cur + ' ' + word).strip()
        if d.textlength(t, font=fnt) <= width: cur = t
        else: lines.append(cur); cur = word
    if cur: lines.append(cur)
    return lines


INTRO = ('Unity dibuja lo mismo que la app dice: caja normal (sin mayúsculas sostenidas), «seguidos» donde era «racha», «¡Un paso más!» donde era «¡Subes de nivel!» o «Nivel 3», «Encargos» en el marcador de Correo y una losa chica «Seguir» donde antes había un «Toca para seguir» en azul que parecía un enlace. '
         'Se borraron los cuatro paneles de fin que nadie dibujaba desde la Tarea 126 (con sus textos) y dos guardas nuevas en las pruebas (`HudRulesTests`) vigilan que no vuelva: nada en mayúsculas sostenidas salvo el estímulo (TINTA, PALABRA, los colores, ALTO, CASA) ni las palabras racha, nivel, Reto, Precisión, sesión.')

TABLA = [
    ('Títulos de las tarjetas «Nuevo»', '«CORREO ESTELAR» · «BODEGA DE CARGA» · «CONSTELACIONES» · «NUEVO»', '«Correo Estelar» · «Bodega de carga» · «Constelaciones» · «Nuevo»'),
    ('Lo que invita a seguir', '«Toca para seguir» / «Toca para empezar» en azul', 'losa chica «Seguir» (sol, letra tinta); toda la tarjeta se toca igual'),
    ('Correo · marcador y resumen', '«Cartas: 13 · atrasadas: 2» · «DÍA 1 DE 4» · «FIN DEL DÍA 1» · «TODOS LOS DÍAS» · «RADIO» · «racha mayor ×9»', '«Encargos: 1 de 2» · «Día 1 de 4» · «Fin del día 1» · «Todos los días» · «Radio» · «×9 seguidas»'),
    ('Rastro de luz', '«AL REVÉS · empieza por la última» · «SOLO LAS ÚLTIMAS 3» · «EN ORDEN» · «¡NUEVO!»', '«Al revés · empieza por la última» · «Solo las últimas 3» · «En orden» · «¡Nuevo!»'),
    ('Constelaciones', '«Racha de memoria» · «Mejor racha: 5» · «¡racha récord!»', '«Seguidas de memoria» · «Más seguidas: 5» · «¡récord de seguidas!»'),
    ('Piloto, Freno, Engranajes, Satélites', '«MISIÓN:» · «NUEVA MISIÓN:» · «SECTOR 2» · «¡HIPERIMPULSO!» · «¡FRENO PERFECTO!» · «MOTOR» · «NUEVO» · «SORPRESA»', '«Misión:» · «Nueva misión:» · «Sector 2» · «¡Hiperimpulso!» · «¡Freno perfecto!» · «Motor» · «Nuevo» · «Sorpresa»'),
    ('Tinta o Palabra', '«Toca lo que DICE la palabra» · aviso «Nivel 3»', '«Toca lo que dice la palabra» (TINTA y PALABRA se quedan: son el estímulo) · «¡Un paso más!»'),
    ('Punta, Disparate, Intrusa, Meteoros', 'aviso «¡Subes de nivel!»', 'aviso «¡Un paso más!»'),
    ('Paneles de fin que nadie dibujaba', 'Acoplamiento «¡TU ESTACIÓN CRECIÓ!» · Aterrizaje «¡MISIÓN CUMPLIDA!» · Rescate «¡RESCATE COMPLETO!» · Satélites «FIN DE LA PARTIDA»; todos con «Racha mayor»', 'se borraron (la app y el «momento del objeto» hacen el final)'),
]


def tile_row(d, sheet, y, label, col, items):
    d.text((MARGIN, y), label, font=font(30), fill=col)
    x = MARGIN; top = y + 44; h = 0
    for im, cap in items:
        sheet.paste(im, (x, top)); d.rectangle((x, top, x + im.width - 1, top + im.height - 1), outline=(70, 74, 130), width=1)
        for i, line in enumerate(wrap(d, cap, font(18, False), W)): d.text((x, top + im.height + 6 + 23 * i), line, font=font(18, False), fill=DIM)
        x += W + GAP; h = max(h, im.height + 6 + 23 * 2)
    return top + h + 22


width = MARGIN * 2 + COLS * W + (COLS - 1) * GAP
probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
intro_lines = wrap(probe, INTRO, font(21, False), width - 2 * MARGIN)
HF, NF = font(20), font(19, False)
LABEL_W = 260
col_w = (width - 2 * MARGIN - LABEL_W - 30) // 2
table_h = 0
rows = []
for what, old, new in TABLA:
    what_lines = wrap(probe, what, font(17), LABEL_W - 16)
    lo, ln = wrap(probe, old, NF, col_w), wrap(probe, new, NF, col_w)
    rows.append((what_lines, lo, ln)); table_h += max(len(what_lines) * 22, len(lo) * 25, len(ln) * 25) + 14
H = 150 + 28 * len(intro_lines) + 40 + 50 + table_h + 5 * 780
sheet = Image.new('RGB', (width, H), NAVY); d = ImageDraw.Draw(sheet)
d.text((MARGIN, 22), 'Unity habla como la app (Tarea 154): hoy | propuesta', font=font(48), fill=SUN)
d.text((MARGIN, 84), 'Capturas reales de Unity (lienzo de teléfono). HOY = las láminas de contacto del commit anterior; PROPUESTA = las tomas nuevas.', font=font(20, False), fill=DIM)
y = 122
for line in intro_lines:
    d.text((MARGIN, y), line, font=font(21, False), fill=CREAM); y += 28
y += 24
d.text((MARGIN, y), 'Los textos', font=font(30), fill=SUN); y += 44
X_OLD, X_NEW = MARGIN + LABEL_W, MARGIN + LABEL_W + col_w + 30
d.text((X_OLD, y), 'HOY', font=HF, fill=CORAL); d.text((X_NEW, y), 'PROPUESTA', font=HF, fill=LIME); y += 28
for what_lines, lo, ln in rows:
    for i, line in enumerate(what_lines): d.text((MARGIN, y + 22 * i), line, font=font(17), fill=SUN)
    for i, line in enumerate(lo): d.text((X_OLD, y + 25 * i), line, font=NF, fill=DIM)
    for i, line in enumerate(ln): d.text((X_NEW, y + 25 * i), line, font=NF, fill=CREAM)
    y += max(len(what_lines) * 22, len(lo) * 25, len(ln) * 25) + 14
y += 18

y = tile_row(d, sheet, y, 'HOY · Correo Estelar', CORAL, [
    (old_tile('correo', 0), 'La tarjeta «NUEVO»: todo en mayúsculas y «Toca para seguir» en azul'),
    (old_tile('correo', 3), 'El marcador dice «Cartas: 0»'),
    (old_tile('correo', 9), 'El resumen: «FIN DEL DÍA 1», «racha mayor ×9»'),
])
y = tile_row(d, sheet, y, 'PROPUESTA · Correo Estelar', LIME, [
    (new_tile(os.path.join(SHOTS, 'correo', '01-nuevo-estacion.png')), 'Caja normal y la losa «Seguir»'),
    (new_tile(os.path.join(SHOTS, 'correo', '04-cinta-dorado.png')), '«Encargos: 0 de 1» (la consigna y el contador, en una sola fila)'),
    (new_tile(os.path.join(SHOTS, 'correo', '10-resumen.png')), '«Fin del día 1» y «×9 seguidas»'),
])
intro_items = []
for g, cap in [('bodega', 'Bodega de carga: «Seguir» (toma a medio fundido)'), ('engranajes', 'Engranajes: «Nuevo» y «Seguir»'), ('parejas', 'Constelaciones: «Seguir»')]:
    p = os.path.join(INTROS, 'intro_%s.png' % g)
    if os.path.exists(p): intro_items.append((new_tile(p), cap))
if intro_items:
    y = tile_row(d, sheet, y, 'PROPUESTA · las otras tres tarjetas «Nuevo» (antes igual que la de Correo)', LIME, intro_items)
y = tile_row(d, sheet, y, 'B8.2 · los cuatro solapes: ya resueltos desde la Tarea 144 (verificado con capturas nuevas)', SUN, [
    (new_tile(os.path.join(SHOTS, 'acoplamiento', '06-anillo-completo.png')), 'Acoplamiento: «Módulo 2 · 8 a bordo» y «×8 seguidos» en filas aparte'),
    (new_tile(os.path.join(SHOTS, 'bodega', '09-t40.png')), 'Bodega: la consigna y el marcador sin pisarse (el chip «×N seguidos» baja bajo la consigna)'),
    (new_tile(os.path.join(SHOTS, 'anagramas', '07-t20.png')), 'Punta: la pregunta pegada a la tarjeta y las casillas desde el inicio'),
    (new_tile(os.path.join(SHOTS, 'freno', '04-primera.png')), 'Freno: cohetes y botones más arriba y grandes (el ALTO ocupa la mitad de arriba)'),
])
sheet = sheet.crop((0, 0, width, y + 10))
sheet.save(OUT)
print('ok', OUT, sheet.size)
