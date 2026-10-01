"""Léxico de "Lluvia de meteoros" (juego de Lenguaje; ver docs/diseno-lluvia-de-meteoros.md, sección 6).

Escribe unity/NeuroVidaCore/Assets/Resources/Lexico/meteoros_es.json y docs/lexico-muestra-meteoros.md.
Determinista (semilla fija). Los datos ajenos se descargan a tools/lexico/fuentes/ (fuera de git) y NO van en la app:

  fuentes/spalex.xlsx              SPALEX, prevalencia de cada palabra (Aguasvivas et al., 2018). Licencia: pendiente de
                                   permiso de los autores para uso comercial (se avanza como si lo dieran).
  fuentes/es_ES.dic / es_ES.aff    Hunspell de LibreOffice (solo para FILTRAR al generar: "¿esto es palabra?").

Uso:  python tools/lexico/meteoros.py            (necesita: pip install spylls openpyxl)

Pasos: (1) palabras = SPALEX ∩ lemas del diccionario (4-12 letras, sin lo de excluir.txt), con la prevalencia MÍNIMA de
España y Latinoamérica (así no entran regionalismos); (2) bandas 1-6 por prevalencia, ~200 por banda; (3) inventadas de
tres tipos (obvia, letra cambiada, letras traspuestas), que el diccionario NO reconoce y que no están en SPALEX.
"""
import json
import os
import random
import re
import sys
import unicodedata

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
SRC = os.path.join(HERE, "fuentes")
OUT_JSON = os.path.join(ROOT, "unity", "NeuroVidaCore", "Assets", "Resources", "Lexico", "meteoros_es.json")
OUT_MD = os.path.join(ROOT, "docs", "lexico-muestra-meteoros.md")

ZIPF_MIN = {1: 4.3, 2: 3.7, 3: 2.8, 4: 2.0, 5: 0.0, 6: 0.0}
SEED = 20250930
PER_BAND = 200
PER_TYPE = 500
MIN_LEN, MAX_LEN = 4, 12
# Cortes de prevalencia (% que acepta la palabra, mínimo entre España y Latinoamérica): banda 1 = la conoce casi todo el
# mundo; banda 6 = rara (50-70%). Se ajustaron mirando cuántas palabras quedan en cada una tras filtrar.
CUTS = [(99.0, 101.0), (97.0, 99.0), (94.0, 97.0), (88.0, 94.0), (76.0, 88.0), (50.0, 76.0)]

LETTERS = set("abcdefghijlmnopqrstuvxyzñáéíóúü")  # sin k ni w
VOWELS = set("aeiouáéíóúü")
WEAK = set("iuü")
ACCENTS = set("áéíóú")
STRONG_ACC = set("áéíóú")


def strip_accents(s):
    return "".join(c for c in unicodedata.normalize("NFD", s) if unicodedata.category(c) != "Mn" or c == "̃" and False)


def noacc(s):
    return s.translate(str.maketrans("áéíóúü", "aeiouu"))


# ------------------------------------------------------------------ excluir.txt


def load_exclusions():
    """exact = palabras exactas; stems = raíces prohibidas en cualquier palabra; pseudo_stems (líneas con "~") = raíces
    prohibidas solo DENTRO de las inventadas."""
    exact, stems, pseudo = set(), [], []
    for line in open(os.path.join(HERE, "excluir.txt"), encoding="utf8"):
        line = line.strip().lower()
        if not line or line.startswith("#"):
            continue
        if line.startswith("~"):
            pseudo.append(line[1:])
            continue
        if line.startswith("*"):
            stem = line[1:]
            if len(stem) < 4:
                exact.add(stem)  # raíces de 3 letras o menos: solo la palabra exacta (si no, borrarían media lengua)
            else:
                stems.append(stem)
        else:
            exact.add(line)
    return exact, stems, pseudo


EXACT, STEMS, PSEUDO_STEMS = load_exclusions()


def excluded_word(w):
    wn = noacc(w)
    if w in EXACT or wn in {noacc(e) for e in EXACT}:
        return True
    return any(s in w or noacc(s) in wn for s in STEMS)


def excluded_pseudo(w):
    """Una inventada no puede contener ninguna raíz ni una palabra de 5+ letras de la lista."""
    wn = noacc(w)
    if any(s in w or noacc(s) in wn for s in STEMS) or any(noacc(s) in wn for s in PSEUDO_STEMS):
        return True
    return any(len(e) >= 5 and (e in w or noacc(e) in wn) for e in EXACT)


# ------------------------------------------------------------------ sílabas del español

ONSETS = set(list("bcdfghjlmnñpqrstvxyz") + ["ch", "ll", "rr", "pr", "pl", "br", "bl", "tr", "dr", "cr", "cl", "gr", "gl", "fr", "fl", "qu", "gu", "gü"])
CLUSTER2 = {"pr", "pl", "br", "bl", "tr", "dr", "cr", "cl", "gr", "gl", "fr", "fl"}
CODAS = {"", "n", "s", "r", "l", "d", "z", "m", "ns"}


def is_v(c):
    return c in VOWELS


def tokens(w):
    """Letras como unidades: ch, ll, rr cuentan como una consonante."""
    out, i = [], 0
    while i < len(w):
        if w[i:i + 2] in ("ch", "ll", "rr"):
            out.append(w[i:i + 2])
            i += 2
        else:
            out.append(w[i])
            i += 1
    return out


def syllables(w):
    """Silabeo estándar del español (diptongos, hiatos, grupos consonánticos)."""
    t = tokens(w)
    # núcleos: grupos de vocales (diptongo = fuerte+débil o débil+débil; hiato = fuerte+fuerte o débil acentuada)
    nuclei = []  # (inicio, fin) sobre t
    i = 0
    while i < len(t):
        if len(t[i]) == 1 and is_v(t[i]):
            j = i
            while j + 1 < len(t) and len(t[j + 1]) == 1 and is_v(t[j + 1]):
                a, b = t[j], t[j + 1]
                a_weak, b_weak = a in WEAK, b in WEAK
                a_strong = not a_weak
                b_strong = not b_weak
                if a_strong and b_strong:
                    break  # hiato
                if (a in "íú") or (b in "íú"):
                    break  # hiato por tilde en la débil
                j += 1
            nuclei.append((i, j))
            i = j + 1
        else:
            i += 1
    if not nuclei:
        return [w]
    cuts = [0]
    for k in range(len(nuclei) - 1):
        a_end = nuclei[k][1]
        b_start = nuclei[k + 1][0]
        cons = t[a_end + 1:b_start]
        n = len(cons)
        if n == 0:
            cut = a_end + 1
        elif n == 1:
            cut = a_end + 1
        elif n == 2:
            cut = a_end + 1 if "".join(cons) in CLUSTER2 else a_end + 2
        elif n == 3:
            cut = a_end + 2 if "".join(cons[1:]) in CLUSTER2 else a_end + 3
        else:
            cut = a_end + 3 if "".join(cons[2:]) in CLUSTER2 else a_end + 3
        cuts.append(cut)
    cuts.append(len(t))
    return ["".join(t[cuts[i]:cuts[i + 1]]) for i in range(len(cuts) - 1)]


def syllable_ok(s):
    t = tokens(s)
    i = 0
    while i < len(t) and not (len(t[i]) == 1 and is_v(t[i])):
        i += 1
    onset = "".join(t[:i])
    j = i
    while j < len(t) and len(t[j]) == 1 and is_v(t[j]):
        j += 1
    coda = "".join(t[j:])
    if j == i:
        return False  # sin vocal
    if onset not in ONSETS and onset != "":
        return False
    if coda not in CODAS:
        return False
    nucleus = t[i:j]
    if len(nucleus) > 3:
        return False
    # q solo ante ue/ui; qu y gu ante e/i
    if onset == "qu" and not (nucleus and nucleus[0] in "eiéí"):
        return False
    if onset == "gu" and not (nucleus and nucleus[0] in "eiéí"):
        return False
    if onset in ("c",) and nucleus and nucleus[0] in "eiéí" and False:
        return False
    return True


def pronounceable(w):
    """Una inventada tiene que poder pronunciarse: cada sílaba bien formada y un final como los de palabras reales."""
    if "qu" in w and re.search(r"qu[aoáóú]", w):
        return False
    if re.search(r"[bcdfghjklmnñpqrstvwxyz]{4,}", noacc(w)):
        return False
    if re.search(r"(.)\1\1", w):
        return False
    syl = syllables(w)
    if not all(syllable_ok(s) for s in syl):
        return False
    last = syl[-1]
    return not re.search(r"[bcdfgjkmpqtvxz]$", noacc(last))  # termina en vocal o n, s, r, l, d, z como las reales... (z/d permitidas abajo)


def ends_like_spanish(w):
    return bool(re.search(r"[aeiouáéíóúnsrldzy]$", w))


# ------------------------------------------------------------------ datos


def load_spalex():
    import openpyxl
    wb = openpyxl.load_workbook(os.path.join(SRC, "spalex.xlsx"), read_only=True, data_only=True)
    ws = wb["Spalex"]
    rows = ws.iter_rows(values_only=True)
    head = next(rows)
    ix = {h: i for i, h in enumerate(head)}
    out = {}
    for r in rows:
        w = r[ix["spelling"]]
        if not isinstance(w, str):
            continue
        w = w.strip().lower()
        try:
            es, la = float(r[ix["percent_nts"]]), float(r[ix["percent_ntl"]])
        except (TypeError, ValueError):
            continue
        zipf = r[ix["EsPal_written_Zipf"]]
        out[w] = (min(es, la), float(zipf) if isinstance(zipf, (int, float)) else 0.0)
    return out


def load_lemmas():
    lemmas = set()
    with open(os.path.join(SRC, "es_ES.dic"), encoding="utf8", errors="replace") as f:
        next(f)
        for line in f:
            lem = line.split("/")[0].strip()
            if lem and lem[0].islower():
                lemmas.add(lem)
    return lemmas


def valid_word_shape(w):
    if not (MIN_LEN <= len(w) <= MAX_LEN):
        return False
    if not all(c in LETTERS for c in w):
        return False
    if re.search(r"sh|th|ph|ck|gh|bb|dd|ff|mm|pp|tt|zz|ss", w):
        return False
    if re.search(r"(ing|ier|ors|ory)$", w):
        return False
    if re.search(r"[aeiouáéíóú]{4,}", w):
        return False
    return w.endswith("mente") is False


def load_manual_out():
    """Palabras sacadas a mano tras revisar la lista final: tools/lexico/revision-manual.txt (una por línea)."""
    path = os.path.join(HERE, "revision-manual.txt")
    if not os.path.exists(path):
        return set()
    return {l.strip().lower() for l in open(path, encoding="utf8") if l.strip() and not l.startswith("#")}


def band_of(p):
    for i, (lo, hi) in enumerate(CUTS):
        if lo <= p < hi:
            return i + 1
    return None


# ------------------------------------------------------------------ construcción


def main():
    rnd = random.Random(SEED)
    for need in ("spalex.xlsx", "es_ES.dic", "es_ES.aff"):
        if not os.path.exists(os.path.join(SRC, need)):
            sys.exit(f"Falta tools/lexico/fuentes/{need}: ver el encabezado de este archivo.")
    from spylls.hunspell import Dictionary
    dic = Dictionary.from_files(os.path.join(SRC, "es_ES"))

    spalex = load_spalex()
    lemmas = load_lemmas()
    cand = []
    for w, (prev, zipf) in spalex.items():
        if w not in lemmas or not valid_word_shape(w) or excluded_word(w):
            continue
        b = band_of(prev)
        if b is None:
            continue
        if zipf < ZIPF_MIN[b]:
            continue  # una palabra que "todos conocen" pero casi no se escribe suele ser rara de verdad: se descarta
        if b >= 3 and re.search(r"(ito|ita|itos|itas|illo|illa|ísimo|ísima|ismo)$", w):
            continue  # diminutivos y derivados raros
        cand.append((w, b, prev, zipf))
    by_band = {b: sorted([c for c in cand if c[1] == b]) for b in range(1, 7)}
    import hashlib

    def stable_key(w):
        return hashlib.sha1(f"{SEED}:{w}".encode("utf8")).hexdigest()

    manual = load_manual_out()
    words = []
    for b in range(1, 7):
        # Selección ESTABLE: ordenar por un hash fijo y tomar las primeras. Si se saca una palabra a mano, entra la
        # siguiente y las demás no cambian (así la revisión de las 1.200 se hace una sola vez).
        pool = sorted((c for c in by_band[b] if c[0] not in manual), key=lambda c: stable_key(c[0]))
        pick = pool[:PER_BAND]
        words += [(w, b) for w, _, _, _ in sorted(pick)]
    word_set = {w for w, _ in words}
    # Fuentes de las inventadas: SOLO las palabras de la lista final (pasaron todos los filtros y la revisión a mano).
    banded_all = {w: b for w, b in words}

    # Diccionarios de otros idiomas, solo para descartar inventadas que sean palabra real en inglés, italiano o portugués.
    others = [Dictionary.from_files(os.path.join(SRC, n)) for n in ("en_US", "it_IT", "pt_PT", "pt_BR")
              if os.path.exists(os.path.join(SRC, n + ".dic"))]

    def is_real(w):
        return w in spalex or w in lemmas or dic.lookup(w) or any(d.lookup(w) for d in others)

    # ---- inventadas
    from collections import Counter
    tri = Counter()
    for w, (prev, _z) in spalex.items():
        if prev >= 90 and all(c in LETTERS for c in w):
            ww = "^" + noacc(w) + "$"
            for i in range(len(ww) - 2):
                tri[ww[i:i + 3]] += 1

    def rare_trigrams(p):
        ww = "^" + noacc(p) + "$"
        return sum(1 for i in range(len(ww) - 2) if tri[ww[i:i + 3]] < 4)

    inv = []
    seen = set()

    def accept(p, tipo, de, b):
        if p in seen or is_real(p) or excluded_pseudo(p) or not (MIN_LEN <= len(p) <= MAX_LEN):
            return False
        if tipo != "traspuesta" and (not pronounceable(p) or rare_trigrams(p) > 0):
            return False
        if tipo == "traspuesta" and (rare_trigrams(p) > 1 or not ends_like_spanish(p) or re.search(r"[bcdfghjklmnñpqrstvxyz]{4,}", noacc(p))):
            return False
        seen.add(p)
        inv.append({"p": p, "t": tipo, "de": de, "b": b})
        return True

    sources = sorted(w for w in banded_all if not any(c in ACCENTS for c in w))

    # (a) obvias: sílabas de palabras reales recombinadas
    syl_first, syl_mid, syl_last = [], [], []
    for w in sources:
        s = syllables(w)
        if len(s) >= 2 and not any(c in ACCENTS for c in w):
            syl_first.append(s[0])
            syl_last.append(s[-1])
            syl_mid += s[1:-1]
    tries = 0
    n_obv = 0
    while n_obv < PER_TYPE and tries < 400000:
        tries += 1
        k = rnd.choice((2, 2, 3, 3, 4))
        parts = [rnd.choice(syl_first)] + [rnd.choice(syl_mid) for _ in range(k - 2)] + [rnd.choice(syl_last)]
        p = "".join(parts)
        if accept(p, "obvia", "", 0):
            n_obv += 1

    # (b) una letra cambiada, del mismo tipo (vocal por vocal, consonante por consonante), nunca la primera
    def change_one(w):
        out = []
        for i in range(1, len(w)):
            c = w[i]
            if c in "aeiou":
                alts = [x for x in "aeiou" if x != c]
            elif c in "bcdfglmnprstv":
                alts = [x for x in "bcdfglmnprstv" if x != c]
            else:
                continue
            for a in alts:
                out.append(w[:i] + a + w[i + 1:])
        return out

    per_band = PER_TYPE // 6 + 1
    n_let = 0
    for b in range(1, 7):
        srcs = [w for w in sources if banded_all[w] == b and len(w) >= 5]
        rnd.shuffle(srcs)
        got = 0
        for w in srcs:
            if got >= per_band:
                break
            opts = change_one(w)
            rnd.shuffle(opts)
            for p in opts:
                if accept(p, "letra", w, b):
                    got += 1
                    n_let += 1
                    break

    # (c) dos letras contiguas INTERIORES intercambiadas (nunca la primera ni la última): chocolate -> chocloate
    n_tra = 0
    for b in range(1, 7):
        srcs = [w for w in sources if banded_all[w] == b and len(w) >= 6]
        rnd.shuffle(srcs)
        got = 0
        for w in srcs:
            if got >= per_band:
                break
            idx = [i for i in range(1, len(w) - 2) if w[i] != w[i + 1]]
            rnd.shuffle(idx)
            for i in idx:
                p = w[:i] + w[i + 1] + w[i] + w[i + 2:] if False else w[:i] + w[i + 1] + w[i] + w[i + 2:]
                if accept(p, "traspuesta", w, b):
                    got += 1
                    n_tra += 1
                    break

    # ---- salida
    os.makedirs(os.path.dirname(OUT_JSON), exist_ok=True)
    data = {
        "version": 1,
        "idioma": "es",
        "fuente": "SPALEX (Aguasvivas et al., 2018)",
        "palabras": [{"p": w, "b": b} for w, b in words],
        "inventadas": inv,
    }
    with open(OUT_JSON, "w", encoding="utf8", newline="\n") as f:
        json.dump(data, f, ensure_ascii=False, separators=(",", ":"))
    print(OUT_JSON, os.path.getsize(OUT_JSON) // 1024, "KB")
    print("candidatas por banda (antes de muestrear):", {b: len(by_band[b]) for b in by_band})
    print("palabras por banda:", {b: sum(1 for _, x in words if x == b) for b in range(1, 7)})
    print("inventadas por tipo:", {t: sum(1 for x in inv if x["t"] == t) for t in ("obvia", "letra", "traspuesta")})
    print("traspuestas por banda:", {b: sum(1 for x in inv if x["t"] == "traspuesta" and x["b"] == b) for b in range(1, 7)})
    write_sample(words, inv)


def write_sample(words, inv):
    rnd = random.Random(SEED + 1)
    lines = ["# Muestra del léxico de Lluvia de meteoros (para revisar)", "",
             "Marca las que sacarías (groseras, regionales, raras de verdad o que parezcan reales).", "",
             "Generada con `tools/lexico/meteoros.py` (semilla fija). Las bandas van de 1 (la conoce casi todo el mundo) a 6",
             "(rara). Fuente de las palabras: SPALEX; las inventadas las genera el programa y no están en ninguna lista.", ""]
    lines += ["## 100 palabras (≈17 por banda)", "", "| Banda | Palabra | Banda | Palabra | Banda | Palabra | Banda | Palabra |", "|---|---|---|---|---|---|---|---|"]
    sample = []
    for b in range(1, 7):
        pool = [w for w, x in words if x == b]
        sample += [(w, b) for w in rnd.sample(pool, min(17 if b > 2 else 17, len(pool)))]
    sample = sample[:100] if len(sample) >= 100 else sample
    while len(sample) % 4:
        sample.append(("", ""))
    for i in range(0, len(sample), 4):
        lines.append("| " + " | ".join(f"{b} | {w}" for w, b in sample[i:i + 4]) + " |")
    lines += ["", "## 100 inventadas (≈33 por tipo)", ""]
    names = {"obvia": "Obvias (sílabas recombinadas)", "letra": "Una letra cambiada", "traspuesta": "Letras traspuestas (interiores)"}
    for t in ("obvia", "letra", "traspuesta"):
        pool = [x for x in inv if x["t"] == t]
        pick = rnd.sample(pool, min(33 if t != "traspuesta" else 34, len(pool)))
        lines += [f"### {names[t]}", "", "| Inventada | De la palabra | Banda | Inventada | De la palabra | Banda |", "|---|---|---|---|---|---|"]
        rows = [(x["p"], x["de"] or "—", x["b"] or "—") for x in pick]
        while len(rows) % 2:
            rows.append(("", "", ""))
        for i in range(0, len(rows), 2):
            lines.append("| " + " | ".join(f"{a} | {b} | {c}" for a, b, c in rows[i:i + 2]) + " |")
        lines.append("")
    with open(OUT_MD, "w", encoding="utf8", newline="\n") as f:
        f.write("\n".join(lines))
    print(OUT_MD)


if __name__ == "__main__":
    main()
