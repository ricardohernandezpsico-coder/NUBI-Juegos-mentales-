"""Lámina de revisión para Ricardo: 40 definiciones del banco (8 por nivel, de categorías distintas) → docs/previews/punta-muestra.md.

Uso:  python tools/punta/muestra.py     (después de banco.py). Determinista (semilla fija).
"""
import collections
import json
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import banco as B  # noqa: E402

OUT_MD = os.path.join(B.ROOT, "docs", "previews", "punta-muestra.md")
POR_NIVEL = 8
SEED = 20261003
NOMBRES = {1: "las más comunes y cortas", 2: "comunes, de 4-6 letras", 3: "comunes, de 6-8 letras", 4: "menos comunes", 5: "las menos comunes y más largas, hasta 10 letras"}


def elegir(palabras, n):
    """n palabras del nivel, de categorías distintas mientras se pueda, y mezclando los largos."""
    rnd = random.Random(SEED + n)
    pool = [w for w in palabras if w["n"] == n]
    rnd.shuffle(pool)
    usadas, elegidas = collections.Counter(), []
    while len(elegidas) < POR_NIVEL and pool:
        pool.sort(key=lambda w: usadas[w["c"]])  # estable: respeta el orden barajado entre empates
        w = pool.pop(0)
        usadas[w["c"]] += 1
        elegidas.append(w)
    return sorted(elegidas, key=lambda w: (len(w["p"]), w["p"]))


def main():
    with open(B.OUT_JSON, encoding="utf8") as f:
        palabras = json.load(f)["palabras"]
    por_nivel = collections.Counter(w["n"] for w in palabras)
    L = []
    L.append("# En la punta de la lengua — muestra de definiciones para revisar (3-oct)\n")
    L.append("Son %d de las %d definiciones del banco, 8 por cada nivel, de categorías distintas. **Las escribí yo para Nubi; no copian ni "
             "parafrasean ningún diccionario.** Revisa sobre todo:\n" % (POR_NIVEL * len(B.NIVELES), len(palabras)))
    L.append("- ¿Hay UNA sola respuesta razonable con la definición sola? (si dudas entre dos palabras, anótala);")
    L.append("- ¿El tono es cercano y concreto, sin tecnicismos ni regionalismos?")
    L.append("- ¿Alguna toca un tema que incomode (enfermedad, muerte, violencia, política, religión)?\n")
    L.append("Qué va en cada nivel: el nivel sale de qué tan poco común es la palabra y de cuántas letras tiene (el 1 son las más conocidas y cortas; el 5, "
             "las menos comunes y largas, hasta 10 letras). Las fichas van sin tilde y la palabra final se muestra bien escrita.\n")
    L.append("Banco completo: `tools/punta/datos/*.txt` (editable a mano; después `python tools/punta/banco.py`). Palabras por nivel: " +
             ", ".join("%d → %d" % (n, por_nivel[n]) for n in B.NIVELES) + ".\n")
    for n in B.NIVELES:
        L.append("## Nivel %d — %s\n" % (n, NOMBRES[n]))
        L.append("| Palabra | Letras | Definición | Tema |")
        L.append("|---|---|---|---|")
        for w in elegir(palabras, n):
            L.append("| **%s** | %d | %s | %s |" % (w["p"], len(w["p"]), w["d"], w["c"]))
        L.append("")
    os.makedirs(os.path.dirname(OUT_MD), exist_ok=True)
    with open(OUT_MD, "w", encoding="utf8", newline="\n") as f:
        f.write("\n".join(L))
    print("escrito", OUT_MD)


if __name__ == "__main__":
    main()
