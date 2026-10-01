"""Rondas de "Cosecha de palabras" (juego de Lenguaje; ver docs/diseno-cosecha-de-palabras.md, secciones 5 y 6).

Escribe unity/NeuroVidaCore/Assets/Resources/Lexico/cosecha_es.json y (con --muestra) docs/cosecha-muestra.md.
Determinista (semilla fija). Reutiliza las fuentes de tools/lexico/ (fuera de git, NO van en la app):

  fuentes/word_info.csv           SPALEX (Aguasvivas et al., 2018), CC BY 4.0: prevalencia de cada palabra.
  fuentes/es_ES.dic / es_ES.aff   Hunspell de LibreOffice, leído con spylls: se usa para GENERAR las formas (plurales,
                                  femeninos, conjugaciones) de cada lema y para comprobar que existen.

Uso:  python tools/cosecha/rondas.py [--muestra]       (necesita: pip install spylls)

Pasos:
 1. Lemas: SPALEX con prevalencia >= 80% (mínimo entre España y Latinoamérica). Sus formas: plural y femenino de los
    sustantivos/adjetivos; de los verbos, infinitivo, presente, pretérito, imperfecto, gerundio y participio (sin
    vosotros, subjuntivo imperfecto/futuro ni pronombres pegados). Solo 3-7 letras, sin k ni w.
 2. Cada palabra se juega SIN tilde (á -> a; la ñ es su propia letra) y se muestra con su tilde.
 3. Una ronda = 7 letras que forman al menos una palabra "estrella" común (7 letras, banda <= 3) y >= 12 palabras comunes
    (banda 1-3: prevalencia >= 94%). Las de `tools/lexico/excluir.txt` se aceptan pero van `oculta` (nunca se muestran).
 4. ~600 rondas en 10 niveles de dificultad (vocales, frecuencia de las letras, cuántas comunes, rareza de la estrella).
"""
import argparse
import hashlib
import json
import os
import random
import re
import sys
from collections import Counter, defaultdict

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
LEX = os.path.join(ROOT, "tools", "lexico")
sys.path.insert(0, LEX)
import meteoros as lex  # noqa: E402  (reutiliza SPALEX, las exclusiones y noacc)

OUT_JSON = os.path.join(ROOT, "unity", "NeuroVidaCore", "Assets", "Resources", "Lexico", "cosecha_es.json")
OUT_MD = os.path.join(ROOT, "docs", "cosecha-muestra.md")

SEED = 20261001
MIN_PREV = 80.0           # prevalencia mínima del lema (%) para que sus formas sean válidas
COMMON_PREV = 94.0        # banda 1-3 = prevalencia >= 94
MIN_LEN, MAX_LEN = 3, 7
MIN_COMMON = 12
MAX_COMMON = 55           # más de 55 comunes serían demasiadas para 60 s
N_ROUNDS = 600
LEVELS = 10
LETTERS = set("abcdefghijlmnñopqrstuvxyz")   # sin k ni w
VOWELS = set("aeiou")

# Frecuencia de las letras en español (%, tabla habitual); la ñ y las raras, abajo.
FREQ = dict(e=13.7, a=12.5, o=8.7, s=7.9, r=6.9, n=6.7, i=6.2, d=5.9, l=5.0, c=4.7, t=4.6, u=3.9, m=3.1, p=2.5,
            b=1.4, h=1.2, q=0.9, y=0.9, v=0.9, g=1.0, f=0.7, z=0.5, j=0.4, x=0.2, ñ=0.3)


def band_of(prev):
    for i, (lo, hi) in enumerate(lex.CUTS):
        if lo <= prev < hi:
            return i + 1
    return 6


def norm(w):
    return lex.noacc(w)


# ------------------------------------------------------------------ formas de cada lema

# Lo que NO queremos de un verbo: vosotros, subjuntivo imperfecto y futuro, y pronombres pegados.
VERB_BLOCK = re.compile(
    r"(áis|éis|ís|ais|eis|asteis|isteis|ad|ed|id|"
    r"ase|ases|asen|ásemos|aseis|iese|ieses|iesen|iésemos|ieseis|yese|yeses|yesen|"
    r"ara|aras|aran|áramos|arais|iera|ieras|ieran|iéramos|ierais|yera|yeras|yeran|yéramos|"
    r"are|ares|aren|áremos|areis|iere|ieres|ieren|iéremos|iereis|yere|yeres|yeren)$")
CLITICS = ("melo", "mela", "telo", "tela", "selo", "sela", "nosla", "noslo", "me", "te", "se", "nos", "os", "lo", "la",
           "le", "los", "las", "les")


# Un verbo poco usado (zipf < 3,4: "fallar" sí, "chapotear" no) no aporta formas conjugadas a las COMUNES: sirven si alguien las
# forma, pero no se muestran. Lo mismo el presente de subjuntivo ("cante", "coma"), que casi no se piensa solo.
ZIPF_VERB = 3.8
ZIPF_PRETERITO_YO = 4.6     # "usé", "leí" sí; "cacé", "cavé", "pesé" no (primera persona del pretérito: casi nadie la piensa suelta)
ZIPF_PRETERITO_EL = 3.9     # "pagó", "tocó" sí; "soló" no

# Fuera del todo (ni se aceptan): nombres propios que el diccionario admite en minúscula y formas arcaicas o de vosotros.
NO_VALIDAS = {"juan", "luis", "rita", "ritas", "japón", "buda", "budas", "cuba", "cubas", "mega", "sía", "sías", "sían", "síais",
              "síamos", "semos", "séis", "veos", "qués", "pedro", "pedros", "teresa", "maría", "marías", "martín", "judas",
              "aries", "tauro", "tauros", "virgo", "saturno", "córdoba", "caribe", "caribes", "romeo", "romea", "romeas",
              "manuela", "rebeca", "peora", "peoras", "nadies", "mases", "doses", "docas", "doñas", "acnés", "sapas", "durada",
              "durado", "duradas", "julios", "armé", "alé", "tenme", "tenlo", "tenlos", "ponle", "ponlo", "leedlos", "teneos",
              "volveos", "situaos", "cubrios", "ritos", "soladas", "solamos", "solando", "solaba", "solaban", "soló", "solé",
              "muebla", "mueblas", "cenada", "cenadas", "granes", "veres", "vendís", "rones"}
# Válidas (se aceptan en silencio) pero que no se muestran: vulgares o que hacen ruido.
EXTRA_OCULTAS = {"ano", "anos", "culo", "culos", "puta", "putas", "puto", "putos", "putear", "porno", "pene", "penes", "coito",
                 "coitos", "cagado", "cagar", "mierda", "polla", "pollas", "tetas", "teta", "vagina", "zorra", "zorras",
                 "sida", "sidas", "sidos", "coger", "coge", "coges", "cogen", "cogió", "cogí", "cogemos", "cogido", "cogidos",
                 "cogida", "cogidas", "ramera", "follar", "folla", "follo", "joder", "jodido", "coño", "cabrón", "cabrona",
                 "cabrones", "marica", "maricón", "mamón", "mamada", "mamar", "pendejo", "pendeja", "culear", "cagada", "mear",
                 "pedo"}
# Válidas, pero demasiado raras para pedirlas ("ceso", "doto"): se aceptan y no se muestran.
EXTRA_NO_COMUN = {"ceso", "ceno", "doto", "dota", "dotan", "dotas", "cito", "pesé", "atraje"}
SUBJ_AR = ("e", "es", "en", "emos")        # presente de subjuntivo de los verbos en -ar
SUBJ_ER_IR = ("a", "as", "an", "amos")     # y de los verbos en -er / -ir


def is_verb_forms(forms):
    return any(f.endswith(("ando", "iendo", "yendo")) for f in forms)


def gen_forms(aff, stem, flags):
    out = {stem}
    for fl in flags:
        for s in aff.SFX.get(fl, []):
            if s.cond_regexp.search(stem):
                base = stem[:len(stem) - len(s.strip)] if s.strip and s.strip != "0" else stem
                out.add(base + s.add)
    return out


def valid_shape(w):
    n = norm(w)
    return (MIN_LEN <= len(n) <= MAX_LEN and all(c in LETTERS for c in n) and any(c in VOWELS or c == "y" for c in n)
            and re.fullmatch(r"[a-záéíóúüñ]+", w) is not None)


def noun_form_ok(lemma, f):
    """Plural o femenino/masculino del lema (comparando sin tildes: lápiz -> lápices, canción -> canciones, joven -> jóvenes)."""
    L, F = norm(lemma), norm(f)
    if F == L:
        return True
    plurals = {L + "s", L + "es"}
    if L.endswith("z"):
        plurals.add(L[:-1] + "ces")
    if F in plurals:
        return True
    fem = {L + "a", L + "as"}
    if L.endswith("o"):
        fem |= {L[:-1] + "a", L[:-1] + "as"}
    if L.endswith("e"):
        fem |= {L[:-1] + "a", L[:-1] + "as"}
    if L.endswith("on") or L.endswith("or") or L.endswith("an"):
        fem |= {L + "a", L + "as"}
    return F in fem


def forms_of(aff, dic, lemma):
    """Formas comunes de un lema: {forma: tipo}. Une todas las entradas homónimas del diccionario."""
    out = {}
    for w in dic.dic.homonyms(lemma):
        g = gen_forms(aff, w.stem, w.flags)
        if is_verb_forms(g) and lemma.endswith(("ar", "er", "ir", "ír")):
            subj = SUBJ_AR if lemma.endswith("ar") else SUBJ_ER_IR
            for f in g:
                if f == lemma:
                    out[f] = "lema"
                    continue
                if f.startswith(lemma) and len(f) > len(lemma):
                    continue                     # futuro y condicional regulares, y pronombres pegados
                if VERB_BLOCK.search(f) or re.search("[aeiíúu]os$", f):
                    continue                     # vosotros, y "teneos", "volveos"
                if f.endswith(CLITICS):
                    cl = next(c for c in CLITICS if f.endswith(c))
                    if f[:-len(cl)] in g or f[:-len(cl)] in ("ten", "pon", "ven", "sal", "haz", "di", "ve", "da", "de", "ve")                             or re.search("[áéíóú]", f):
                        continue                 # dale, dímelo, tenlo, ponle: orden + pronombre
                out.setdefault(f, "subjuntivo" if f.endswith(subj) and lemma not in ("ir", "ser") else "verbo")
        else:
            for f in g:
                if noun_form_ok(lemma, f):
                    out.setdefault(f, "nombre")
    return out


# ------------------------------------------------------------------ diccionario válido


def build_dictionary():
    from spylls.hunspell import Dictionary
    dic = Dictionary.from_files(os.path.join(lex.SRC, "es_ES"))
    aff = dic.aff
    spalex = lex.load_spalex()                   # palabra -> (prevalencia mínima, zipf)
    lemmas = {w: v for w, v in spalex.items() if v[0] >= MIN_PREV}

    words = {}          # clave sin tilde -> {"p": con tilde, "prev": prevalencia, "zipf": zipf}

    def add(form, prev, zipf, kind="lema"):
        if not valid_shape(form) or not dic.lookup(form) or form in NO_VALIDAS:
            return
        own = spalex.get(form)
        if own is not None:                     # la forma misma está en SPALEX: manda lo que se midió para ELLA
            if own[0] < MIN_PREV:
                return
            prev, zipf = own
        elif (kind == "subjuntivo" or (kind == "verbo" and (
                zipf < ZIPF_VERB
                or (form[-1] in "éí" and zipf < ZIPF_PRETERITO_YO)
                or (form[-1] == "ó" and zipf < ZIPF_PRETERITO_EL)))):
            prev = min(prev, COMMON_PREV - 0.1)  # válida, pero no común
        if form in EXTRA_NO_COMUN:
            prev = min(prev, COMMON_PREV - 0.1)
        k = norm(form)
        cur = words.get(k)
        if cur is None or prev > cur["prev"] or (prev == cur["prev"] and form < cur["p"]):
            words[k] = {"p": form, "prev": prev, "zipf": zipf}

    for lemma, (prev, zipf) in sorted(lemmas.items()):
        if lemma.lower() != lemma:
            continue
        add(lemma, prev, zipf)
        for f, kind in forms_of(aff, dic, lemma).items():
            if f != lemma:
                add(f, prev, zipf, kind)
    return words


# ------------------------------------------------------------------ rondas


def ms(w):
    return Counter(w)


def can_form(wc, lc):
    return all(lc[c] >= n for c, n in wc.items())


def score_all(cands, words):
    """Dificultad 0 (fácil) a 1 (difícil) de cada combinación, mezclando por PERCENTILES: pocas comunes (lo que más pesa),
    letras raras, pocas vocales y estrella menos común. (Los percentiles reparten parejo: ningún factor domina por escala.)"""
    def pct(values, reverse=False):
        order = sorted(range(len(values)), key=lambda i: values[i], reverse=reverse)
        out = [0.0] * len(values)
        i = 0
        while i < len(order):                       # empates = promedio de sus posiciones
            j = i
            while j + 1 < len(order) and values[order[j + 1]] == values[order[i]]:
                j += 1
            for t in range(i, j + 1):
                out[order[t]] = (i + j) / 2 / max(1, len(order) - 1)
            i = j + 1
        return out

    com = pct([c["n_common"] for c in cands], reverse=True)                      # menos comunes = más difícil
    fr = pct([sum(FREQ[x] for x in c["set"]) / 7 for c in cands], reverse=True)  # letras más raras = más difícil
    vw = pct([abs(sum(1 for x in c["set"] if x in VOWELS) - 3.4) for c in cands])   # lejos de 3-4 vocales = más difícil
    st = pct([words[c["star"]]["prev"] for c in cands], reverse=True)
    for i, c in enumerate(cands):
        c["diff"] = 0.42 * com[i] + 0.22 * fr[i] + 0.16 * vw[i] + 0.20 * st[i]


def make_rounds(words):
    rnd = random.Random(SEED)
    keys = list(words)
    wcount = {k: ms(k) for k in keys}
    by_len7 = [k for k in keys if len(k) == 7]
    hidden = {k for k in keys if lex.excluded_word(words[k]["p"]) or lex.excluded_word(k) or k in EXTRA_OCULTAS}

    # una candidata por cada conjunto de 7 letras con una estrella común
    stars_by_set = defaultdict(list)
    for k in by_len7:
        if k not in hidden and words[k]["prev"] >= COMMON_PREV:
            stars_by_set["".join(sorted(k))].append(k)

    by_sorted = defaultdict(list)                  # letras ordenadas -> palabras con exactamente esas letras
    for k in keys:
        by_sorted["".join(sorted(k))].append(k)

    def sub_multisets(sset):
        """Todas las sub-combinaciones (3-7 letras) de las 7 letras, sin repetir."""
        out = set()
        for mask in range(1 << len(sset)):
            if bin(mask).count("1") >= MIN_LEN:
                out.add("".join(sset[i] for i in range(len(sset)) if mask >> i & 1))
        return out

    cands = []
    for sset, stars in sorted(stars_by_set.items()):
        allw = [k for sub in sub_multisets(sset) for k in by_sorted.get(sub, [])]
        n_comm = sum(1 for k in allw if k not in hidden and words[k]["prev"] >= COMMON_PREV)
        if n_comm < MIN_COMMON or n_comm > MAX_COMMON:
            continue
        star = max(stars, key=lambda k: (words[k]["prev"], k))
        cands.append({"set": sset, "stars": stars, "star": star, "words": sorted(allw), "n_common": n_comm})
    score_all(cands, words)
    return cands, hidden


def family(star):
    return star[:5]


def pick_rounds(cands, words, previous=None):
    """Elige N_ROUNDS rondas repartidas en LEVELS niveles, variadas: nada de la misma combinación reordenada ni de dos
    rondas que compartan 6 o 7 letras, ni dos con la estrella de la misma familia."""
    cands = sorted(cands, key=lambda c: c["diff"])
    n = len(cands)
    per = N_ROUNDS // LEVELS
    chosen, level_of = [], {}
    used_fam, picked_sets = set(), []
    # Estabilidad: una ronda ya elegida antes (previous: letras ordenadas -> nivel) se queda en su nivel mientras siga cumpliendo
    # los mínimos; solo se reemplazan las que dejaron de cumplirlos. Así corregir una palabra no revuelve las 600.
    kept_per_level = Counter()
    for c in cands:
        lv = (previous or {}).get(c["set"])
        if lv and kept_per_level[lv] < per:
            chosen.append(c)
            level_of[c["set"]] = lv
            used_fam.add(family(c["star"]))
            picked_sets.append(ms(c["set"]))
            kept_per_level[lv] += 1

    def too_close(c):
        lc = ms(c["set"])
        for other in picked_sets:
            if sum((lc & other).values()) >= 6:
                return True
        return False

    for lv in range(LEVELS):
        lo, hi = lv * n // LEVELS, (lv + 1) * n // LEVELS
        got = kept_per_level[lv + 1]
        # primero la franja propia del nivel; si faltan rondas (por la variedad), se amplía hacia los lados
        for widen in (0, n // LEVELS // 3, n // LEVELS // 2, n // LEVELS):
            band = cands[max(0, lo - widen):min(n, hi + widen)]
            band = sorted(band, key=lambda c: hashlib.sha1(f"{SEED}:{c['set']}".encode()).hexdigest())
            for c in band:
                if got >= per:
                    break
                if c["set"] in level_of or family(c["star"]) in used_fam or too_close(c):
                    continue
                chosen.append(c)
                level_of[c["set"]] = lv + 1
                used_fam.add(family(c["star"]))
                picked_sets.append(ms(c["set"]))
                got += 1
            if got >= per:
                break
    return chosen, level_of


def shuffled_letters(sset, valid_keys, rnd):
    letters = list(sset)
    for _ in range(200):
        rnd.shuffle(letters)
        s = "".join(letters)
        if s not in valid_keys and not any(s[i:i + 4] in valid_keys for i in range(0, 4)):
            return s
    return "".join(letters)


def build_json(chosen, level_of, words, hidden, letters_of=None):
    valid_keys = set(words)
    rondas = []
    for c in sorted(chosen, key=lambda c: (level_of[c["set"]], c["diff"], c["set"])):
        lc = ms(c["set"])
        plist = []
        for k in sorted(c["words"], key=lambda k: (-words[k]["prev"], k)):
            w = words[k]
            b = band_of(w["prev"])
            e = {"p": w["p"], "b": b}
            hid = k in hidden
            if hid:
                e["oculta"] = True
            elif b <= 3:
                e["comun"] = True
            if len(k) == 7:
                e["estrella"] = True
            plist.append(e)
        rid = hashlib.sha1(c["set"].encode()).hexdigest()[:8]
        letras = (letters_of or {}).get(c["set"]) or shuffled_letters(
            c["set"], valid_keys, random.Random(hashlib.sha1(f"{SEED}:letras:{c['set']}".encode()).hexdigest()))
        rondas.append({"id": rid, "nivel": level_of[c["set"]], "letras": letras,
                       "palabras": plist})
    return {
        "version": 1,
        "idioma": "es",
        "fuente": "SPALEX (Aguasvivas et al., 2018), https://figshare.com/projects/SPALEX/29722; formas con el diccionario Hunspell es_ES (LibreOffice)",
        "licencia": "SPALEX: CC BY 4.0. Hunspell es_ES: ver docs/cosecha-de-palabras (créditos); en la app solo van las listas por ronda",
        "rondas": rondas,
    }


def summary(data):
    rs = data["rondas"]
    print("rondas:", len(rs))
    for lv in range(1, LEVELS + 1):
        sub = [r for r in rs if r["nivel"] == lv]
        if not sub:
            continue
        com = [sum(1 for w in r["palabras"] if w.get("comun")) for r in sub]
        tot = [len(r["palabras"]) for r in sub]
        vow = [sum(1 for c in r["letras"] if c in VOWELS) for r in sub]
        print(f"  nivel {lv:2d}: {len(sub):3d} rondas · comunes {sum(com)/len(sub):5.1f} (mín {min(com)}, máx {max(com)})"
              f" · palabras {sum(tot)/len(sub):5.1f} · vocales {sum(vow)/len(sub):.1f}")
    print("palabras promedio por ronda:", round(sum(len(r["palabras"]) for r in rs) / len(rs), 1))


def load_previous(path):
    """Rondas del archivo anterior: letras ordenadas -> nivel, y las letras tal como se mostraban."""
    if not os.path.exists(path):
        return {}, {}
    with open(path, encoding="utf8") as f:
        old = json.load(f)
    return ({"".join(sorted(r["letras"])): r["nivel"] for r in old["rondas"]},
            {"".join(sorted(r["letras"])): r["letras"] for r in old["rondas"]})


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--muestra", action="store_true")
    ap.add_argument("--desde-cero", action="store_true", help="ignora el archivo anterior y vuelve a elegir las 600 rondas")
    args = ap.parse_args()
    previous, letters_of = load_previous(OUT_JSON) if not args.desde_cero else ({}, {})
    words = build_dictionary()
    print("diccionario válido:", len(words), "palabras")
    cands, hidden = make_rounds(words)
    print("combinaciones candidatas (>= 12 comunes y estrella común):", len(cands))
    chosen, level_of = pick_rounds(cands, words, previous)
    data = build_json(chosen, level_of, words, hidden, letters_of)
    cambiadas = [c["set"] for c in chosen if c["set"] not in previous]
    if previous:
        print(f"rondas anteriores: {len(previous)}; reemplazadas: {len(cambiadas)}")
    os.makedirs(os.path.dirname(OUT_JSON), exist_ok=True)
    with open(OUT_JSON, "w", encoding="utf8", newline="\n") as f:
        json.dump(data, f, ensure_ascii=False, separators=(",", ":"))
    print(OUT_JSON, os.path.getsize(OUT_JSON) // 1024, "KB")
    summary(data)
    if args.muestra:
        import muestra
        muestra.write(data, words, OUT_MD)


if __name__ == "__main__":
    main()
