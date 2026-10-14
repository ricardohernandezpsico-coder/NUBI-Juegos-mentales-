# Recursos para la APP que salen de las maquetas en PIL (NO es maqueta: escribe en app/src/main/res).
# DESDE LA TAREA 92 ESTE SCRIPT YA NO GENERA A NUBI: la Nubi de la app (pantallas, ícono adaptativo y monocromo, íconos de Android 7 e ícono de notificación)
# sale del render 3D `tools/isla/isla.py --solo-nubi` con `tools/isla/icono.py` (ver `tools/isla/README.md`). Los `nubi_*.webp` planos (con destellos de cruz) y la Nubi científica se borraron.
# Lo que sigue haciendo:
#   - Planetas de las 4 áreas (drawable-nodpi/area_*.webp: esfera de color con su ícono blanco; Atención = diana, elegida por Ricardo en
#     `docs/previews/navegacion-nubi.png`). La esfera mide AREA_BODY_FRACTION del ancho.
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


def extras():
    spec = importlib.util.spec_from_file_location('nv', os.path.join(HERE, 'navegacion_nubi.py'))
    NV = importlib.util.module_from_spec(spec); spec.loader.exec_module(NV)
    JN = NV.JN
    NV.use_att(NV.att_diana)
    side = 540
    from PIL import ImageDraw
    for key in ['memoria', 'atencion', 'razonamiento', 'lenguaje']:
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
    extras()
    print('listo:', RES)


if __name__ == '__main__':
    main()
