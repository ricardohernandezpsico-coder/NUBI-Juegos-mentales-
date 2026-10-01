# Cosecha de palabras — diseño (1-oct, para aprobar con maqueta)

Juego estrella de **Lenguaje** (id `cosecha`), el cuarto del área, elegido por Ricardo el 1-oct entre Cosecha de
palabras, La palabra intrusa y ¿Cómo se llama?. Llena el hueco del área: los otros tres juegos RECONOCEN o
COMPRENDEN; este pide **producir** palabras desde la memoria (lo que más cuesta con la edad). Retoma la idea de
Constelación de Palabras (descartada el 28-sep por la voz) sin voz: todo táctil.

## 1. La idea en una línea

Siete letras giran en órbita alrededor de un planeta: **toca letras en orden para formar todas las palabras que
puedas antes de que se acabe la cosecha.** Cada palabra cae como semilla y brota en el planeta.

## 2. Ciencia y honestidad

- **Fluidez verbal**: producir palabras bajo presión de tiempo es una de las pruebas más usadas en neuropsicología.
  Su análisis en **agrupar y saltar** (Troyer, Moscovitch y Winocur, 1997, *Neuropsychology* 11:138-146,
  [doi:10.1037//0894-4105.11.1.138](https://doi.org/10.1037//0894-4105.11.1.138)) muestra dos componentes que se
  disocian y cambian con la edad.
- **Ojo (honestidad)**: Troyer validó el análisis en fluidez por categoría ("animales") y por letra inicial ("F"), no
  en formar palabras con un juego de letras fijo. Acá se usa como **descripción** de cómo buscas ("en racimos" o
  "saltando"), sin normas ni comparación con estudios. Se dice así en la pantalla final.
- **Curva de producción**: en las pruebas de fluidez la mayoría de las palabras salen al principio y después cuesta
  más; mirar el arranque contra el final es descriptivo y útil ("arrancas fuerte; al final, prueba cambiar de
  idea").
- Palabras raras acertadas = profundidad de vocabulario (bandas de SPALEX, CC BY 4.0, ya atribuido en Licencias).

## 3. Diferencias con lo que hay en el mercado (patentes y marcas)

Formar palabras con letras es una mecánica genérica (anagramas, siglos de juegos de mesa). Para no parecernos a
juegos conocidos:
- **No** deslizar el dedo por letras en círculo para llenar un crucigrama (eso es Wordscapes): acá se TOCAN letras
  que se MUEVEN en órbita y no hay grilla de crucigrama.
- **No** letra central obligatoria (eso es Spelling Bee del New York Times).
- **No** escribir palabras que empiezan con 3 letras dadas (eso es Word Bubbles de Lumosity).
- Lo propio: la órbita, el jardín que crece en el planeta, la medida de cómo buscas y el "también podías" al final.
Búsqueda rápida de patentes (1-oct): sin hallazgos específicos de la mecánica; va a la lista del abogado con el resto.
Nunca usar nombres ajenos (Wordscapes, Spelling Bee, Boggle, Word Bubbles, Apalabrados) ni en la tienda.

## 4. Cómo se juega

- **Escena**: un planeta pequeño abajo-centro (`GameWorld` nuevo "Huerto"); 7 letras-luna (fichas de arcilla redondas,
  letra en Atkinson Hyperlegible Bold 30 sp, 34 sp mayores) en una órbita elíptica que gira despacio (una vuelta cada
  ~40 s; quietas con "quitar animaciones"). Mínimo 64 dp por ficha.
- **Formar**: tocar letras en orden las enciende y las va poniendo en la **bandeja** (arriba del planeta). Tocar la
  última de la bandeja la devuelve. Botón grande **"Sembrar"** (lima) envía; botón chico "Borrar" vacía. Cada ficha se
  usa una vez por palabra. Palabras de 3 letras o más.
- **Palabra válida**: la bandeja se convierte en semilla que cae al planeta y brota una planta (más alta cuanto más
  larga la palabra; flor dorada si es rara). Nota musical por largo. La palabra se suma a la lista "Tu cosecha".
- **Repetida**: la bandeja late y dice "Ya la tienes" (sin castigo). **No válida**: se sacude suave y dice "No está
  en el diccionario de Nubi" (no "error").
- **Palabra estrella**: usa las 7 letras → árbol grande y destello ("¡Palabra estrella!"), puntos × 3.
- **Pista** (anti-frustración, sobre todo mayores): si pasan 15 s sin sembrar, Nubi ilumina la primera letra de una
  palabra común que falte. Las pistas usadas se anotan (la medida lo considera).
- Duración: **3 cosechas de 60 s** (Reto; cada una con letras nuevas y planeta propio); Precisión = 3 cosechas de 90 s.
  Entre cosechas, 3 s de "¡Cosecha lista!" con el conteo.

## 5. Contenido: las rondas (español; generadas fuera del juego)

- `tools/cosecha/rondas.py` arma ~600 juegos de 7 letras (español, con tildes normalizadas: se tocan sin tilde y se
  aceptan con o sin tilde) y, para cada uno, TODAS las palabras válidas que se pueden formar.
- **Diccionario válido**: formas reales del español (incluye plurales y conjugaciones comunes), generadas al construir
  desde Hunspell es_ES (spylls) e intersectadas con SPALEX para quitar rarezas; fuera las de `tools/lexico/excluir.txt`
  (groserías, etc.: si alguien forma una, se acepta en silencio pero NO se muestra en la lista ni en "también
  podías"). Licencia: el diccionario no va en la app; solo las listas por ronda (palabras sueltas) → anotar la fuente
  en Licencias si Hunspell lo exige (revisar su licencia MPL/LGPL al construir).
- Cada ronda guarda: letras, palabras válidas con su banda SPALEX (1-6) y si es "común" (bandas 1-3), y la(s)
  palabra(s) estrella. Se aceptan rondas con ≥ 12 palabras comunes y ≥ 1 palabra estrella de uso común.
- Salida `unity/NeuroVidaCore/Assets/Resources/Lexico/cosecha_es.json` (formato `JsonUtility`).
- Muestra para Ricardo: 10 rondas con sus palabras (para ver si sobran rarezas o faltan obvias).

### Estado de las rondas (1-oct)

`python tools/cosecha/rondas.py [--muestra]` (determinista, ~75 s; pruebas en `tools/cosecha/test_rondas.py`): 600 rondas, 60 por nivel,
unas 43 palabras válidas por ronda (diccionario de ~23.700 palabras). Comunes disponibles por nivel (promedio): 37 → 29 → 27 → 25 → 22 →
19 → 18 → 17 → 15 → 14. Las formas salen de los lemas de SPALEX con prevalencia ≥ 80%: plural y femenino de nombres y adjetivos; de los
verbos, infinitivo, presente, pretérito, imperfecto, gerundio y participio (sin vosotros, subjuntivo imperfecto/futuro ni pronombres
pegados). Una forma conjugada solo es «común» si el verbo es de uso corriente (zipf ≥ 3,8), no es presente de subjuntivo y, en el
pretérito, el verbo es muy usado. Las no comunes se aceptan en silencio y no se muestran. Listas a mano en `rondas.py`: `NO_VALIDAS`
(nombres propios y formas arcaicas), `EXTRA_OCULTAS` (vulgares) y `EXTRA_NO_COMUN`.

**Licencia del diccionario**: el Hunspell es_ES de LibreOffice se distribuye con GPL v3, LGPL v2.1/v3 o MPL 1.1 (a elección). Solo se usa
al GENERAR (`tools/lexico/fuentes/`, fuera de git); a la app van listas de palabras sueltas por ronda, no el diccionario. Para quedar en
orden basta una línea de crédito en Ajustes → Licencias y créditos («Palabras: SPALEX, CC BY 4.0; formas verbales y plurales con el
diccionario Hunspell es_ES de LibreOffice»), que se agrega al programar el juego.

## 6. Dificultad (DDA común por cosecha, `stepUp` 0.5 como Satélites; 10 niveles)

- Lo que sube: letras más "difíciles" (menos vocales, consonantes menos frecuentes), menos palabras comunes
  disponibles en la ronda (de ~40 a ~15), y la palabra estrella menos común. Lo que NO cambia: el tiempo (60 s).
- Acierto de la cosecha = haber encontrado ≥ 35% de las palabras comunes disponibles (ajustable con datos).
- Mayores: rondas con más palabras comunes y pista a los 10 s.

## 7. Medidas al final (juego estrella)

1. **Tu cosecha** (cifra grande): palabras encontradas en las 3 cosechas, y "de las comunes, encontraste N de M".
2. **Tu manera de buscar** (descriptiva, basada en Troyer): % de palabras que salieron en **racimos** (comparte las 2
   primeras letras o la raíz con la anterior: casa → casas → caso) frente a **saltos** (palabra nueva sin relación).
   Frase con consejo: muchos racimos → "Exprimes bien cada idea; prueba también saltar a otra letra inicial"; muchos
   saltos → "Saltas rápido entre ideas; cuando una funciona, busca sus parientes (plural, otra terminación)". Solo con
   ≥ 10 palabras. Rotulado "cómo buscaste en esta partida".
3. **Tu ritmo**: palabras en los primeros 20 s contra los últimos 20 s (promedio de las 3 cosechas) con una mini barra
   doble y frase ("Arrancas fuerte y bajas al final: es lo normal").
4. **Tu palabra estrella / la más rara** y **También podías**: 5 palabras comunes que no encontraste (aprendizaje).
Nota común al pie. Sin comparación con otras personas. Agregar a `docs/medidas-juegos-estrella.md`.

## 8. Arte, sonido y efectos (fluidez, como ¿Verdad o disparate?)

- Fichas-luna de arcilla con brillo; órbita con estela suave; bandeja de arcilla crema; semillas que caen con arco;
  plantas de arcilla de 4-5 tipos que brotan con un rebote; el planeta se llena de verde y flores; árbol dorado para la
  palabra estrella. Al final de cada cosecha, el huerto se ve completo un instante.
- Sonidos de la pentatónica de `GameFeel`: cada letra tocada sube una nota (la palabra suena como melodía), sembrar =
  "plop" de tierra + campanita, brotar = cuerda suave, repetida/no válida = madera sorda suave. Nada arcade.
- Respeta "quitar animaciones", sonido y vibración apagados.

## 9. Pendiente antes de programar

1. Aprobar la maqueta `docs/previews/cosecha.png`.
2. Aprobar la muestra de 10 rondas.
