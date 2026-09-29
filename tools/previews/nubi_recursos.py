# Recursos de Nubi para la APP (29-sep, Ricardo aprobó "Nubi suave" y Hoy con Nubi al centro). NO es maqueta: escribe
# en app/src/main/res. Todo sale del mismo dibujo de tools/previews/nubi_suave.py, así el ícono, la bienvenida y Hoy
# son la misma Nubi. Volver a correrlo si cambia el dibujo de Nubi.
#   - Ícono adaptativo: fondo (vector, noche de la app) + Nubi (mipmap-*/ic_launcher_foreground.png) + monocromo
#     (mipmap-*/ic_launcher_monochrome.png: silueta con la cara calada, para el ícono temático de Android 13+).
#   - Íconos para Android 7 (mipmap-*/ic_launcher.webp y ic_launcher_round.webp): el mismo ícono ya recortado.
#   - Ícono de notificación (drawable-*/ic_stat_neurovida.png): la silueta blanca de Nubi.
#   - Nubi para las pantallas (drawable-nodpi/nubi_*.webp): saluda, mira (explica) y celebra. Lienzo cuadrado con
#     Nubi al centro; el cuerpo mide 0,65 del ancho (NUBI_BODY_FRACTION en ui/components/NubiHome.kt).
#   - Nubi científica (drawable-nodpi/nubi_cientifica.webp: bata, lentes y tablilla), la de la ventana de un área en
#     Juegos (Ricardo la eligió el 29-sep, `docs/previews/juegos-nubi.png`).
#   - Planetas de las 6 áreas (drawable-nodpi/area_*.webp: esfera de color con su ícono blanco; Atención = diana,
#     elegida por Ricardo en `docs/previews/navegacion-nubi.png`). La esfera mide AREA_BODY_FRACTION del ancho.
# Uso: python3 tools/previews/nubi_recursos.py
import os, importlib.util
from PIL import Image
HERE = os.path.dirname(__file__)
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
RES = os.path.join(ROOT, 'app', 'src', 'main', 'res')
spec = importlib.util.spec_from_file_location('ns', os.path.join(HERE, 'nubi_suave.py'))
NS = importlib.util.module_from_spec(spec); spec.loader.exec_module(NS)
S, u, N = NS.S, NS.u, NS.N
DENS = [('mdpi', 1.0), ('hdpi', 1.5), ('xhdpi', 2.0), ('xxhdpi', 3.0), ('xxxhdpi', 4.0)]


def save(img, rel, size, fmt='PNG', **kw):
    path = os.path.join(RES, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.resize((size, size), Image.LANCZOS).save(path, fmt, **kw)
    return path


def foreground():
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    NS.nubi(im, S / 2, S / 2 + u(1), u(0.78))
    return im


def sprite(pose):
    """Nubi sola para las pantallas: cuerpo de ~74 unidades en un lienzo de 108 (0,65 del ancho)."""
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    k = u(0.95)
    if pose == 'celebra':
        NS.pose_celebra(im, S / 2, S / 2, k)
    else:
        NS.nubi(im, S / 2, S / 2, k, pose)
    return im


def stat_icon():
    """Silueta blanca de Nubi, recortada a su tamaño con un margen (el sistema la tiñe)."""
    m = NS.icon_mono()
    box = m.getchannel('A').getbbox()
    w, h = box[2] - box[0], box[3] - box[1]
    side = int(max(w, h) * 1.12)
    out = Image.new('RGBA', (side, side), (0, 0, 0, 0))
    out.paste(m.crop(box), ((side - w) // 2, (side - h) // 2))
    return out


def extras():
    spec = importlib.util.spec_from_file_location('nv', os.path.join(HERE, 'navegacion_nubi.py'))
    NV = importlib.util.module_from_spec(spec); spec.loader.exec_module(NV)
    JN = NV.JN
    save(JN.nubi_sprite(JN.scientist, 720), 'drawable-nodpi/nubi_cientifica.webp', 720, 'WEBP', quality=92, method=6)
    NV.use_att(NV.att_diana)
    side = 540
    from PIL import ImageDraw
    for key in ['memoria', 'atencion', 'razonamiento', 'lenguaje', 'calculo', 'velocidad']:
        # Solo la esfera con su ícono (esfera = 0,6 del ancho, AREA_BODY_FRACTION); el resplandor de la maqueta se
        # desbordaba del lienzo y dejaba un cuadrado oscuro: en la app lo dibuja Compose.
        lay = Image.new('RGBA', (side, side), (0, 0, 0, 0)); r = side * 0.3
        JN.NH.orb(lay, side / 2, side / 2, r, key, 0, 0)
        m = Image.new('L', (side, side), 0)
        ImageDraw.Draw(m).ellipse([side / 2 - r - 3, side / 2 - r - 3, side / 2 + r + 3, side / 2 + r + 3], fill=255)
        a = lay.getchannel('A'); from PIL import ImageChops
        lay.putalpha(ImageChops.multiply(a, m))
        save(lay, f'drawable-nodpi/area_{key}.webp', 360, 'WEBP', quality=92, method=6)


def main():
    fg, mono, full = foreground(), NS.icon_mono(), NS.icon_round()
    for name, k in DENS:
        save(fg, f'mipmap-{name}/ic_launcher_foreground.png', int(108 * k))
        save(mono, f'mipmap-{name}/ic_launcher_monochrome.png', int(108 * k))
        side = int(48 * k)
        N.masked(full, side, 'squircle').save(os.path.join(RES, f'mipmap-{name}/ic_launcher.webp'), 'WEBP', lossless=True)
        N.masked(full, side, 'circle').save(os.path.join(RES, f'mipmap-{name}/ic_launcher_round.webp'), 'WEBP', lossless=True)
        save(stat_icon(), f'drawable-{name}/ic_stat_neurovida.png', int(24 * k))
    for pose, file in [('hola', 'nubi_hola'), ('coach', 'nubi_mira'), ('celebra', 'nubi_celebra')]:
        save(sprite(pose), f'drawable-nodpi/{file}.webp', 720, 'WEBP', quality=92, method=6)
    extras()
    print('listo:', RES)


if __name__ == '__main__':
    main()
