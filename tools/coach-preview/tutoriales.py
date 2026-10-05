#!/usr/bin/env python
"""Láminas de revisión del tutorial con Nubi (5-oct): una imagen por paso de cada juego, en 20:9 (el teléfono de Ricardo).

NO son capturas del juego: el smoke de Unity corre sin gráficos. Son láminas armadas con los RECTÁNGULOS REALES que dejó registrados
NubiCoach en ese arranque (unity/test-results/coach-audit/<juego>-<alto>.json): el hueco, las zonas protegidas, cada texto del
juego que había a la vista, el dedo que insiste, Nubi y el globo (con su texto en Fredoka Bold a 54, la misma letra del juego).
Lo que se ve: qué queda iluminado y qué en la sombra, dónde cayó Nubi y que no pisa nada. El arte de los juegos no aparece.

Uso:  python tools/coach-preview/tutoriales.py [alto]     (alto = 2400 por defecto; 2160 = 18:9, 1920 = 16:9)
Salida: docs/previews/tutoriales/<juego>-paso<N>.png  (y -18x9 / -16x9 si se pide otro alto)
"""
import json
import os
import sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
AUDIT = os.path.join(ROOT, "unity", "test-results", "coach-audit")
OUT = os.path.join(ROOT, "docs", "previews", "tutoriales")
FONT = os.path.join(ROOT, "unity", "NeuroVidaCore", "Assets", "Resources", "Fonts", "Fredoka-Bold.ttf")

SCALE = 0.5                                   # 1080 x 2400 -> 540 x 1200
BG = (13, 16, 40)
GAME_TEXT = (150, 160, 200)
SKY = (110, 200, 255)
SUN = (255, 201, 60)
OK = (90, 220, 150)
BAD = (255, 90, 90)
VEIL = (8, 10, 28, 184)
BUBBLE = (255, 246, 229)
INK = (40, 30, 70)

NAMES = {
    "anagramas": "En la punta de la lengua", "aterrizaje": "Aterrizaje lunar", "calculo": "Carga exacta",
    "freno": "Freno de emergencia", "meteoros": "Lluvia de meteoros", "secuencia": "Rastro de luz", "stroop": "Dos orillas",
}


def rect(r, w, h):
    """Rect de Unity (x, y de abajo-izquierda, centrado en el área) a píxeles de imagen (y hacia abajo)."""
    x0 = (r["x"] + w / 2) * SCALE
    x1 = (r["x"] + r["width"] + w / 2) * SCALE
    y1 = (h / 2 - r["y"]) * SCALE
    y0 = (h / 2 - r["y"] - r["height"]) * SCALE
    return [x0, y0, x1, y1]


def wrap(text, font, width):
    lines, cur = [], ""
    for word in text.split(" "):
        trial = (cur + " " + word).strip()
        if font.getlength(trial) <= width or not cur:
            cur = trial
        else:
            lines.append(cur)
            cur = word
    if cur:
        lines.append(cur)
    return lines


def draw_step(game, step, w, h):
    W, H = int(w * SCALE), int(h * SCALE)
    img = Image.new("RGBA", (W, H), BG + (255,))
    d = ImageDraw.Draw(img, "RGBA")
    f_small = ImageFont.truetype(FONT, 11)
    # textos del juego a la vista (lo que ningún globo debe tapar)
    for r in step["GameText"]:
        b = rect(r, w, h)
        d.rectangle(b, fill=GAME_TEXT + (70,), outline=GAME_TEXT + (200,))
    # zonas iluminadas (hueco + protegidas): se ven claras, sin la sombra
    lit = []
    if step["HasHole"]:
        lit.append((rect(step["Hole"], w, h), SKY, step["Circle"]))
    for r in step["Keep"]:
        if r["width"] > 0:
            lit.append((rect(r, w, h), SUN, False))
    veil = Image.new("RGBA", (W, H), VEIL if step["Kind"] != "Notice" else (0, 0, 0, 0))
    vd = ImageDraw.Draw(veil)
    for b, _, circle in lit:
        (vd.ellipse if circle else vd.rounded_rectangle)(b, fill=(0, 0, 0, 0)) if circle else vd.rounded_rectangle(b, radius=12, fill=(0, 0, 0, 0))
    img = Image.alpha_composite(img, veil)
    d = ImageDraw.Draw(img, "RGBA")
    for b, color, circle in lit:
        (d.ellipse if circle else d.rounded_rectangle)(b, outline=color + (255,), width=3) if circle else d.rounded_rectangle(b, radius=12, outline=color + (255,), width=3)
    if step["Kind"] == "Touch" and step["Finger"]["width"] > 0:
        d.rectangle(rect(step["Finger"], w, h), outline=(255, 255, 255, 110))
    # Nubi y el globo
    nb = rect(step["Nubi"], w, h)
    d.ellipse(nb, fill=(120, 150, 255, 255), outline=(255, 255, 255, 255), width=2)
    d.text(((nb[0] + nb[2]) / 2, (nb[1] + nb[3]) / 2), "Nubi", fill=(255, 255, 255), font=ImageFont.truetype(FONT, 22), anchor="mm")
    bb = rect(step["Bubble"], w, h)
    d.rounded_rectangle(bb, radius=18, fill=BUBBLE + (255,), outline=(120, 100, 80, 255), width=2)
    font = ImageFont.truetype(FONT, int(round(step["FontSize"] * SCALE)))
    lines = wrap(step["Text"], font, (step["Bubble"]["width"] - 52) * SCALE)
    lh = font.size * 1.15
    y = (bb[1] + bb[3]) / 2 - lh * len(lines) / 2
    for ln in lines:
        d.text(((bb[0] + bb[2]) / 2, y), ln, fill=INK, font=font, anchor="ma")
        y += lh
    # pie: tipo de paso, líneas y si está limpio
    ok = step["Clean"] and step["ActualLines"] <= 3
    d.rectangle([0, H - 26, W, H], fill=(0, 0, 0, 200))
    d.text((6, H - 20), f"{NAMES.get(game, game)} · paso {step['Index'] + 1} · {step['Kind']} · {step['Lines']} líneas · "
           + ("sin tapar nada" if ok else "TAPA ALGO"), fill=OK if ok else BAD, font=f_small)
    return img.convert("RGB")


def main():
    height = int(sys.argv[1]) if len(sys.argv) > 1 else 2400
    suffix = "" if height == 2400 else {2160: "-18x9", 1920: "-16x9"}.get(height, f"-{height}")
    os.makedirs(OUT, exist_ok=True)
    n = 0
    for name in sorted(os.listdir(AUDIT)):
        if not name.endswith(f"-{height}.json"):
            continue
        data = json.load(open(os.path.join(AUDIT, name), encoding="utf-8"))
        game = data["Game"]
        for i, step in enumerate(data["Steps"]):
            img = draw_step(game, step, 1080, height)
            img.save(os.path.join(OUT, f"{game}-paso{i + 1}{suffix}.png"))
            n += 1
    print(f"{n} láminas en {OUT}")


if __name__ == "__main__":
    main()
