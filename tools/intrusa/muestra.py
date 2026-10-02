"""Muestra del banco de grupos de "La estrella intrusa" para revisar a mano (docs/intrusa-muestra.md). La llama grupos.py --muestra."""
import random

TIPOS = {1: "amplia", 2: "vecina", 3: "uso", 4: "material / lugar / parte", 5: "trampa de asociación", 6: "regla + trampa"}
NIVELES = {1: "niveles 1-2", 2: "niveles 3-4", 3: "niveles 5-6", 4: "niveles 7-8", 5: "niveles 9-10", 6: "niveles 11-12"}
SEMILLA = 7


def elegir(grupos_tipo, n, semilla):
    """n grupos de reglas distintas (primero una por regla, al azar, y si faltan se repiten reglas)."""
    rng = random.Random(semilla)
    por_regla = {}
    for g in grupos_tipo:
        por_regla.setdefault(g["k"], []).append(g)
    claves = sorted(por_regla)
    rng.shuffle(claves)
    elegidos = []
    ronda = 0
    while len(elegidos) < n and ronda < 10:
        for k in claves:
            lista = por_regla[k]
            if ronda < len(lista) and len(elegidos) < n:
                elegidos.append(sorted(lista, key=lambda g: g["i"])[(ronda * 7 + len(k)) % len(lista)])
        ronda += 1
    return elegidos[:n]


def escribir(grupos, ruta):
    lineas = ["# Muestra del banco de grupos de La estrella intrusa (para revisar)", "",
              "**Marca los grupos dudosos (más de una intrusa posible, regla rara) o palabras que sacarías.**", "",
              "Cada grupo trae 4 palabras que comparten una regla y una intrusa, el nombre de la constelación y 3 opciones para",
              "«¿Qué las une?» (la correcta va con ✓; las otras dos son plausibles pero falsas). En las trampas se dice con qué palabra del",
              "grupo «va» la intrusa. Generada con `python tools/intrusa/grupos.py --muestra` (semilla fija).", ""]
    for t in range(1, 7):
        lineas += [f"## Tipo {t} · {TIPOS[t]} ({NIVELES[t]})", ""]
        for n, g in enumerate(elegir(grupos[t], 10, SEMILLA + t), 1):
            ops = " · ".join(("✓ " if i == g["c"] else "") + o for i, o in enumerate(g["o"]))
            linea = f"{n}. {', '.join(g['p'])} → **{g['x']}** · regla: {g['n']} · opciones: {ops}"
            if g["r"]:
                linea += f" · {g['x']} {g['r']}"
            lineas.append(linea)
            lineas.append(f"   - {g['e']}")
        lineas.append("")
    with open(ruta, "w", encoding="utf8", newline="\n") as f:
        f.write("\n".join(lineas))
    print(ruta)
