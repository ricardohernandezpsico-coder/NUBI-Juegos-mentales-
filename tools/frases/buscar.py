"""Busca frases de "¿Verdad o disparate?" por id (las que la gente marcó como "no está clara") o por texto.

  python tools/frases/buscar.py a1b2c3d4 0f9e8d7c      # ids separados por espacio o coma
  python tools/frases/buscar.py --texto delfines
"""
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
JSON_PATH = os.path.join(ROOT, "unity", "NeuroVidaCore", "Assets", "Resources", "Frases", "disparate_es.json")
NOMBRES = {1: "corta", 2: "con complemento", 3: "negación", 4: "con pausa", 5: "cuantificadores", 6: "comparación"}


def main():
    sys.stdout.reconfigure(encoding="utf8")
    args = [a for a in sys.argv[1:] if a != "--texto"]
    modo_texto = "--texto" in sys.argv
    with open(JSON_PATH, encoding="utf8") as fh:
        frases = json.load(fh)["frases"]
    claves = [x for a in args for x in a.replace(",", " ").split()]
    for f in frases:
        hit = any(k.lower() in f["f"].lower() for k in claves) if modo_texto else f["i"] in claves
        if hit:
            print(f"{f['i']}  tipo {f['t']} ({NOMBRES[f['t']]})  {'VERDAD' if f['v'] else 'DISPARATE ' + f['c']}  «{f['f']}»" + (f"  → {f['r']}" if f["r"] else ""))


if __name__ == "__main__":
    main()
