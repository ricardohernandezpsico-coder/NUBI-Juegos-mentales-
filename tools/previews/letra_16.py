"""«Hoy | letra 16» (Tarea 121, parte 3; 17-oct; SOLO IMAGEN, Ricardo la aprueba antes de tocar nada): la app de hoy frente a la misma app con el texto secundario de 14 sp en 16 sp
(dejando 14 solo en los rótulos del gráfico) y el HUD de Unity con los chips de 14 dp en 16 dp. Las dos mitades son capturas REALES:
  - app: `AppActualScreenshotTest` (Roborazzi) con un cambio TEMPORAL de `14.sp` por `16.sp` (el interlineado de esas líneas, +3), deshecho después; «hoy» sale de docs/previews/app-actual/.
  - Unity: `bash tools/verificar-todo.sh --capturas Stroop,Meteoros,Calculo` sin cambios y con un cambio TEMPORAL en `GameHud` (chips y rótulo «seguidos» de 42 a 48 unidades, chip de 64 a 72), deshecho después.
Los recuadros rojos marcan lo que dejaría de caber o de leerse bien; las notas van abajo. Nada de esto está en la app.

  python tools/previews/letra_16.py   ->  docs/previews/letra-16.png
"""
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
HOY = os.path.join(ROOT, "docs", "previews", "app-actual")
L16 = os.path.join(ROOT, "docs", "previews", "letra-16")
OUT = os.path.join(ROOT, "docs", "previews", "letra-16.png")
FREDOKA = os.path.join(ROOT, "app", "src", "main", "res", "font", "fredoka.ttf")
NUNITO = os.path.join(ROOT, "app", "src", "main", "res", "font", "nunito_regular.ttf")

BG = (9, 11, 38)
CREAM = (255, 246, 226)
DIM = (186, 190, 230)
SUN = (255, 201, 60)
RED = (255, 64, 64)


def font(path, size, weight=None):
    f = ImageFont.truetype(path, size)
    if weight is not None:
        try:
            f.set_variation_by_axes([weight])
        except Exception:
            pass
    return f


# Pantallas de la app: (título, archivo «hoy», archivo letra 16, recuadros [(x0, y0, x1, y1, nota)] en píxeles de la captura de 1080 de ancho)
APP = [
    ("Hoy", "hoy.png", "app-hoy.png", []),
    ("Hoy · lámina de un área", "hoy-lamina.png", "app-hoy-lamina.png", [(80, 990, 1030, 1075, 1), (45, 1250, 1035, 1500, 2)]),
    ("Juegos · ficha de un juego", "juegos-ficha.png", "app-juegos-ficha.png", [(80, 630, 1000, 770, 1)]),
    ("Avance", "avance.png", "app-avance.png", []),
    ("Perfil · logros", "perfil.png", "app-perfil.png", [(300, 1470, 780, 1840, 3), (780, 1855, 1025, 2220, 3)]),
    ("Resultado de una partida", "resultado.png", "app-resultado.png", [(385, 1970, 700, 2235, 4)]),
    ("Ajustes", "ajustes.png", "app-ajustes.png", []),
]
GAMES = [("Tinta o Palabra", "stroop"), ("Lluvia de meteoros", "meteoros"), ("Carga exacta", "calculo")]
UNITY_BOXES = {"stroop": [(800, 95, 1015, 180, 5)], "meteoros": [(800, 95, 1015, 180, 5)], "calculo": [(800, 95, 1015, 180, 5)]}

NOTES = {
    1: "Las cinco etapas (Inicio, Aprendiz, Hábil, Experto, Maestro) quedan pegadas: «Experto» y «Maestro» casi se tocan. Hay que partirlas en dos filas o acortarlas.",
    2: "«Hace 3 sem.» y «Hace 2 sem.» pasan a dos líneas y las cuatro cajas de la semana crecen: ya no caben en una línea.",
    3: "Logros: «Explorador» se parte en «Explorad / or» y «En marcha» y «Una semana» pasan a dos líneas; las casillas crecen y la rejilla pierde una fila a la vista.",
    4: "«Tu promedio» pasa a dos líneas y la fila de las tres cajas queda desalineada («Hoy» y «Tu mejor» a una línea).",
    5: "Unity: «seguidos» llega al borde de la píldora (queda pegado y casi cortado): hace falta una píldora unas 35 unidades más ancha. Los chips «Nivel» y «1 de 16» caben.",
}
NOT_BROKEN = "Sin recuadro rojo = cabe sin cambios (Hoy, Avance y Ajustes crecen un poco y siguen sin romperse; el Primer vuelo ya usa 16 sp o más y no cambia)."


def draw_boxes(im, boxes, scale):
    d = ImageDraw.Draw(im)
    f = font(FREDOKA, 22, 700)
    for x0, y0, x1, y1, n in boxes:
        r = (x0 * scale, y0 * scale, x1 * scale, y1 * scale)
        d.rectangle(r, outline=RED, width=4)
        d.ellipse((r[0] - 4, r[1] - 26, r[0] + 26, r[1] + 4), fill=RED)
        d.text((r[0] + 11, r[1] - 22), str(n), font=f, fill=(255, 255, 255), anchor="mt")


def lamina():
    PW = 340                                  # ancho de cada teléfono
    GAP, MARGIN, PER_ROW, COLGAP = 14, 40, 4, 30
    ph = int(PW * 2401 / 1080)
    col_w = PW * 2 + GAP
    width = MARGIN * 2 + PER_ROW * col_w + (PER_ROW - 1) * COLGAP
    notes_f = font(NUNITO, 24)
    title_f = font(FREDOKA, 48, 600)
    sec_f = font(FREDOKA, 32, 600)
    lab_f = font(NUNITO, 22)
    head = font(FREDOKA, 24, 600)
    sheet = Image.new("RGB", (width, 4200), BG)
    d = ImageDraw.Draw(sheet)
    d.text((MARGIN, MARGIN), "Hoy | letra 16 — solo imagen, para aprobar antes de tocar nada", font=title_f, fill=CREAM)
    d.text((MARGIN, MARGIN + 62), "App: texto secundario de 14 sp a 16 sp (14 queda solo en rótulos de gráficos). Unity: chips del HUD de 14 dp a 16 dp. Capturas reales; en rojo, lo que deja de caber.", font=notes_f, fill=DIM)
    y = MARGIN + 120
    d.text((MARGIN, y), "La app", font=sec_f, fill=SUN)
    y += 52
    for start in range(0, len(APP), PER_ROW):
        x = MARGIN
        for title, hoy, l16, boxes in APP[start:start + PER_ROW]:
            a = Image.open(os.path.join(HOY, hoy)).convert("RGB").resize((PW, ph), Image.LANCZOS)
            b = Image.open(os.path.join(L16, l16)).convert("RGB").resize((PW, ph), Image.LANCZOS)
            draw_boxes(b, boxes, PW / 1080)
            sheet.paste(a, (x, y))
            sheet.paste(b, (x + PW + GAP, y))
            d.text((x, y + ph + 8), "hoy", font=head, fill=DIM)
            d.text((x + PW + GAP, y + ph + 8), "letra 16", font=head, fill=SUN)
            d.text((x, y + ph + 42), title, font=lab_f, fill=CREAM)
            x += col_w + COLGAP
        y += ph + 100
    d.text((MARGIN, y), "Los juegos (HUD de Unity, 1080 de ancho): chips de 14 dp | 16 dp", font=sec_f, fill=SUN)
    y += 52
    GW = (width - 2 * MARGIN - 2 * COLGAP) // 3
    gh = int(GW * 560 / 1080)
    gx = MARGIN
    for title, g in GAMES:
        a = Image.open(os.path.join(L16, "unity-%s-14dp.png" % g)).convert("RGB").resize((GW, gh), Image.LANCZOS)
        b = Image.open(os.path.join(L16, "unity-%s-16dp.png" % g)).convert("RGB").resize((GW, gh), Image.LANCZOS)
        draw_boxes(b, UNITY_BOXES[g], GW / 1080)
        sheet.paste(a, (gx, y))
        sheet.paste(b, (gx, y + gh + 40))
        d.text((gx, y + gh + 6), "%s · 14 dp (hoy)" % title, font=head, fill=DIM)
        d.text((gx, y + 2 * gh + 46), "%s · 16 dp" % title, font=head, fill=SUN)
        gx += GW + COLGAP
    y += 2 * gh + 110
    d.text((MARGIN, y), "Dónde algo deja de caber", font=sec_f, fill=SUN)
    y += 52
    for k in sorted(NOTES):
        d.ellipse((MARGIN, y, MARGIN + 32, y + 32), fill=RED)
        d.text((MARGIN + 16, y + 4), str(k), font=font(FREDOKA, 22, 700), fill=(255, 255, 255), anchor="mt")
        d.text((MARGIN + 48, y + 2), NOTES[k], font=notes_f, fill=CREAM)
        y += 44
    d.text((MARGIN, y + 8), NOT_BROKEN, font=notes_f, fill=DIM)
    y += 70
    return sheet.crop((0, 0, width, y))


if __name__ == "__main__":
    lamina().save(OUT, optimize=True)
    print("ok:", OUT)
