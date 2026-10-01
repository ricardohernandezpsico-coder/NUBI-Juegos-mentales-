"""Muestra de rondas de Cosecha de palabras para revisar a mano (docs/cosecha-muestra.md). La llama rondas.py --muestra."""
import hashlib

TITULO = "Muestra de rondas de Cosecha de palabras (para revisar)"


def write(data, words, out_path):
    rondas = data["rondas"]
    lines = [f"# {TITULO}", "",
             "**Marca palabras que sobren (raras, regionales) o que falten (obvias que no estén).**", "",
             "Cada ronda trae 7 letras (se tocan sin tilde; la ñ cuenta como letra aparte), su palabra estrella (usa las 7) y TODAS",
             "las palabras comunes que se pueden formar. Las comunes son las de uso corriente (SPALEX, 94% o más de aciertos en",
             "España y Latinoamérica). Cuando el juego acepta otras, más raras, no las muestra, pero las suma si alguien las forma.",
             "Generada con `python tools/cosecha/rondas.py --muestra` (semilla fija).", ""]
    for lv in range(1, 11):
        sub = sorted((r for r in rondas if r["nivel"] == lv), key=lambda r: hashlib.sha1(r["id"].encode()).hexdigest())
        if not sub:
            continue
        r = sub[0]
        comunes = [w for w in r["palabras"] if w.get("comun")]
        estrellas = [w["p"] for w in comunes if w.get("estrella")]
        otras = [w["p"] for w in r["palabras"] if w.get("estrella") and not w.get("comun") and not w.get("oculta")]
        letras = " ".join(c.upper() for c in r["letras"])
        lines += [f"## Nivel {lv} · {letras}", "",
                  f"- **Palabra estrella:** {', '.join(estrellas)}" + (f" (también acepta: {', '.join(otras)})" if otras else ""),
                  f"- **Comunes: {len(comunes)}** (de {len(r['palabras'])} palabras válidas)", ""]
        for n in range(7, 2, -1):
            grupo = sorted((w["p"] for w in comunes if len(_norm(w["p"])) == n), key=_norm)
            if grupo:
                lines.append(f"- {n} letras ({len(grupo)}): " + ", ".join(grupo))
        lines.append("")
    with open(out_path, "w", encoding="utf8", newline="\n") as f:
        f.write("\n".join(lines))
    print(out_path)


def _norm(w):
    return w.translate(str.maketrans("áéíóúü", "aeiouu"))
