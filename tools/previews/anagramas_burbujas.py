# Lámina (28-sep): cómo se ven las letras de Anagramas con los adornos pedidos por Ricardo. Réplica PIL de lo que
# dibuja AnagramGameController (no es Unity): niveles 1-4 = fichas crema con resplandor de color, respiración y un
# destello que recorre una ficha; niveles 5-7 = burbujas de color aclarado, resplandor que late y brillo grande y suave.
# Uso: python3 tools/previews/anagramas_burbujas.py -> docs/previews/anagramas-burbujas.png
import os, math, random
from PIL import Image, ImageDraw, ImageFont, ImageFilter
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
FB = ROOT + '/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'
W, H = 720, 1100
INK = (42, 23, 64); CREAM = (253, 233, 200)
DECO = [(76, 201, 240), (184, 164, 255), (255, 107, 74), (155, 229, 100), (255, 201, 60)]
def F(s): return ImageFont.truetype(FB, s)
def lerp(a, b, t): return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))

def sky(w, h):
    im = Image.new('RGBA', (w, h), (10, 16, 60, 255)); d = ImageDraw.Draw(im); rnd = random.Random(4)
    for y in range(h):
        c = lerp((22, 30, 92), (6, 8, 30), y / h); d.line([(0, y), (w, y)], fill=c + (255,))
    for _ in range(90):
        x, y, r = rnd.random() * w, rnd.random() * h, rnd.random() * 1.8 + 0.6
        d.ellipse([x - r, y - r, x + r, y + r], fill=(255, 255, 255, int(90 + rnd.random() * 140)))
    return im

def glow(im, cx, cy, r, col, a):
    g = Image.new('RGBA', im.size, (0, 0, 0, 0)); ImageDraw.Draw(g).ellipse([cx - r, cy - r, cx + r, cy + r], fill=col + (a,))
    im.alpha_composite(g.filter(ImageFilter.GaussianBlur(r * 0.45)))

def soft(im, cx, cy, rx, ry, a):
    g = Image.new('RGBA', im.size, (0, 0, 0, 0)); ImageDraw.Draw(g).ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=(255, 255, 255, a))
    im.alpha_composite(g.filter(ImageFilter.GaussianBlur(min(rx, ry) * 0.55)))

def sparkle(d, cx, cy, s):
    d.polygon([(cx, cy - s), (cx + s * .18, cy - s * .18), (cx + s, cy), (cx + s * .18, cy + s * .18), (cx, cy + s), (cx - s * .18, cy + s * .18), (cx - s, cy), (cx - s * .18, cy - s * .18)], fill=(255, 255, 255, 245))

def tiles(im, y0, word):
    d = ImageDraw.Draw(im); n = len(word); s = 92; gap = 22; x0 = (W - (n * s + (n - 1) * gap)) / 2
    for i, ch in enumerate(word):
        cx = x0 + i * (s + gap) + s / 2; cy = y0
        glow(im, cx, cy, s * 0.8, DECO[i % 5], 120)
    d = ImageDraw.Draw(im)
    for i, ch in enumerate(word):
        k = 1 + 0.028 * math.sin(i * 1.7)  # respiración (cada una a su ritmo)
        ss = s * k; cx = x0 + i * (s + gap) + s / 2; cy = y0
        d.rounded_rectangle([cx - ss / 2, cy - ss / 2 + 8, cx + ss / 2, cy + ss / 2 + 8], radius=22, fill=INK)
        d.rounded_rectangle([cx - ss / 2, cy - ss / 2, cx + ss / 2, cy + ss / 2], radius=22, fill=CREAM, outline=INK, width=6)
        d.text((cx, cy), ch, font=F(54), fill=INK, anchor='mm')
    cx = x0 + 2 * (s + gap) + s / 2 + 12; sparkle(d, cx, y0 - s * 0.26, 20)

def bubbles(im, word, area):
    x0, y0, x1, y1 = area; rnd = random.Random(7); pts = []; r = 50
    for ch in word:
        for _ in range(500):
            x = rnd.uniform(x0 + r, x1 - r); y = rnd.uniform(y0 + r, y1 - r)
            if all((x - a) ** 2 + (y - b) ** 2 > (2 * r + 18) ** 2 for a, b in pts): break
        pts.append((x, y))
    for i, (x, y) in enumerate(pts): glow(im, x, y, r * 1.35, DECO[i % 5], 140)
    d = ImageDraw.Draw(im)
    for i, ((x, y), ch) in enumerate(zip(pts, word)):
        face = lerp(DECO[i % 5], CREAM, 0.45)
        d.ellipse([x - r, y - r + 7, x + r, y + r + 7], fill=INK)
        d.ellipse([x - r, y - r, x + r, y + r], fill=face, outline=INK, width=6)
    for i, (x, y) in enumerate(pts): soft(im, x - r * 0.14, y - r * 0.2, r * 0.31, r * 0.22, 190)
    d = ImageDraw.Draw(im)
    for (x, y), ch in zip(pts, word): d.text((x, y), ch, font=F(54), fill=INK, anchor='mm')

im = sky(W, H); d = ImageDraw.Draw(im)
d.text((W / 2, 50), 'Niveles 1-4: fichas quietas', font=F(34), fill=(255, 255, 255), anchor='mm')
d.text((W / 2, 92), 'resplandor de color · respiran · un destello de vez en cuando', font=F(22), fill=(199, 208, 255), anchor='mm')
tiles(im, 220, 'ULNA')
d = ImageDraw.Draw(im)
d.line([(40, 340), (W - 40, 340)], fill=(255, 255, 255, 50), width=2)
d.text((W / 2, 390), 'Niveles 5-7: burbujas', font=F(34), fill=(255, 255, 255), anchor='mm')
d.text((W / 2, 432), 'color propio · resplandor que late · brillo grande y suave', font=F(22), fill=(199, 208, 255), anchor='mm')
bubbles(im, 'SONRISA', (40, 470, W - 40, H - 40))
im.convert('RGB').save(ROOT + '/docs/previews/anagramas-burbujas.png'); print('OK')
