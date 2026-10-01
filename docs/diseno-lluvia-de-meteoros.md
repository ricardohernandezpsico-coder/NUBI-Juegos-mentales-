# Lluvia de meteoros — diseño (30-sep, para aprobar con maqueta)

Juego estrella de **Lenguaje** (id `meteoros`). Ricardo lo eligió el 30-sep entre 7 propuestas. Este documento es la
especificación: la maqueta y después el programa salen de acá.

## 1. La idea en una línea

Caen meteoros con palabras: **tocas las palabras que existen; las inventadas las dejas pasar.** Se entiende en dos
segundos, hay movimiento continuo y al final te dice algo real de tu vocabulario.

## 2. Ciencia (verificada en PubMed el 30-sep)

- **Decisión léxica** "ir / no ir": responder solo cuando es palabra. Es una variante validada de la tarea clásica:
  conserva el efecto de frecuencia, con respuestas más rápidas, más precisas y menos exigentes que la de sí/no
  (Perea, Rosa y Gómez, 2002, *Memory & Cognition* 30:34-45, [doi:10.3758/bf03195263](https://doi.org/10.3758/bf03195263)).
  Por eso el juego solo pide TOCAR (no hay botón "no es palabra").
- **Vocabulario con decisión léxica**: un test de 5 minutos así predice bien el vocabulario (LexTALE: Lemhöfer y
  Broersma, 2012, [doi:10.3758/s13428-011-0146-0](https://doi.org/10.3758/s13428-011-0146-0); versión en español
  Lextale-Esp: Ferré y Brysbaert, 2017, [doi:10.3758/s13428-016-0728-y](https://doi.org/10.3758/s13428-016-0728-y)).
  No se copian sus ítems.
- **Qué tan conocida es cada palabra**: SPALEX midió, con una recolección masiva en línea, qué palabras del español
  conoce la gente (Aguasvivas et al., 2018, [doi:10.3389/fpsyg.2018.02156](https://doi.org/10.3389/fpsyg.2018.02156)).
  Con eso la dificultad de cada palabra es un dato, no una suposición. **Licencia pendiente** (sección 8).
- **Letras cambiadas de lugar engañan**: "chocloate" se lee como palabra; el efecto es fuerte cuando el cambio es en
  el INTERIOR de la palabra y no al final (Perea y Lupker, 2003, *Memory & Cognition* 31:829-841,
  [doi:10.3758/bf03196438](https://doi.org/10.3758/bf03196438)). Da los señuelos más difíciles y un consejo útil.

Patentes (revisión del 30-sep): la decisión léxica es una tarea pública de 1971 (Meyer y Schvaneveldt). Lo más
cercano que apareció: US 9,737,813 (entrenar significados combinando pistas; vencida por falta de pago, y es otra
mecánica) y US 6,409,513 (lectura veloz con reconocimiento de palabras, de 1999-2000: ya vencida). Sin hallazgos de
Lumos Labs o Posit sobre palabras que caen. Riesgo **bajo**; va a la lista del abogado como el resto. Nombre propio,
arte y sonidos propios; no usar nombres de juegos ajenos.

## 3. Cómo se juega

- **Escena**: el cielo de la app (fondo C) y abajo el borde curvo de un planeta con una cúpula-observatorio
  (`GameWorld` nuevo: "Observatorio"). Los meteoros entran desde arriba, en diagonal suave, con estela.
- **El meteoro**: roca de arcilla con una placa crema donde va la palabra (**Atkinson Hyperlegible Bold**, elegida por Ricardo el 1-oct
  porque la "a" y la "o" no se parecen; 26 dp, 30 en mayores; una palabra de 12 letras que no cabe a lo ancho baja de a 1 dp,
  nunca de 22 dp; solo las palabras usan esta letra), en minúsculas y con tildes. La placa es lo que se lee: siempre horizontal aunque la roca gire.
- **Tocar una palabra real** → la roca estalla en estrellas que vuelan a la **constelación** de arriba (contador de
  palabras rescatadas) + ✓ de arcilla. Nota musical de la pentatónica de `GameFeel` (la racha arma una melodía).
- **Dejar pasar una inventada** → al llegar a la atmósfera se deshace en chispas suaves (sin castigo; cuenta como
  acierto, sin puntos ruidosos).
- **Tocar una inventada** → la roca se agrieta en polvo gris + ✗ y un golpe sordo suave; la palabra queda tachada un
  instante ("brúgala"). Nunca solo color: forma (✗, grietas) + texto.
- **Dejar pasar una real** → se va con su nombre en pequeño ("se fue: brújula"). Sin culpa.
- El toque cuenta al PRESIONAR. Zona de toque = toda la roca + 12 dp (mínimo 56 dp; 64 en mayores).
- **Tamaño y separación (1-oct)**: la roca mide placa × 1,45 + 20 dp de ancho y 3,2 placas de alto (la placa ocupa ~1/3). Hasta 3
  a la vez (niveles 9-12) sin pisarse: antes de soltar un meteoro se prueba su recorrido contra el de los que ya caen (cada 0,2 s,
  caja de la roca + 28 u de aire); si no hay lugar, espera unos cuadros (`MeteorContract.Overlaps`, con pruebas).

## 4. Dificultad (DDA común, `AdaptiveDifficulty` + `GameControllerBase`, 12 niveles)

| Nivel | Palabras (banda de conocida) | Señuelos | A la vez | Caída | Largo |
|---|---|---|---|---|---|
| 1-2 | banda 1 (las conoce casi todo el mundo) | inventadas obvias (sílabas recombinadas) | 1 | 7 s | 4-6 |
| 3-4 | bandas 1-2 | obvias + 1 letra cambiada | 1-2 | 6 s | 4-7 |
| 5-6 | bandas 2-3 | 1 letra cambiada | 2 | 5,5 s | 5-8 |
| 7-8 | bandas 3-4 | 1 letra + letras traspuestas | 2-3 | 5 s | 5-9 |
| 9-10 | bandas 4-5 | traspuestas internas | 3 | 4,5 s | 6-10 |
| 11-12 | bandas 5-6 (raras) | traspuestas internas | 3 | 4 s | 6-12 |

- 50% palabras / 50% inventadas; nunca más de 3 inventadas seguidas.
- **Precisión**: caída × 1,5, máximo 2 a la vez, 40 meteoros. **Reto**: 120 s. Mayores: caída × 1,25 y letra mayor.
- **Lluvia de estrellas** (enganche, fuera de la escalera): cada 25 meteoros, 6 seguidos solo de palabras comunes,
  rápidos, para tocar sin pensar ("¡Lluvia de estrellas!", puntos × 2).
- **Meteoro dorado**: 1 de cada 12, una palabra de 2 bandas más rara que el nivel, con aro sol y el doble de puntos.
- Racha ≥ 8 sin error: la estela se enciende (puntos × 1,5) hasta el próximo error.

## 5. Medidas al final (juego estrella)

1. **Tu vocabulario** (rehecho el 1-oct: las 6 columnas de estrellas "no se entendían"; mezcla de las propuestas A y C): una cifra
   grande "N de cada 10 palabras poco frecuentes reconocidas" (bandas 3-6) y tres barras con %: comunes (bandas 1-2),
   intermedias (3-4) y raras (5-6), corregidas por inventadas tocadas en la partida (aciertos − falsas alarmas, como Lextale).
   Frase con rangos: casi todas ≥ 90, la mayoría 70-89, más de la mitad 55-69, la mitad 45-54, menos de la mitad 25-44,
   pocas < 25 ("Reconoces casi todas las comunes, la mayoría de las intermedias y menos de la mitad de las raras"). Un grupo
   con < 6 palabras vistas queda "aún sin medir".
2. **Tu reconocimiento**: mediana del tiempo de toque en palabras comunes contra raras ("Las comunes, en 0,7 s. Las raras, en
   1,1 s." + "Es normal: las raras tardan más"). Es el efecto de frecuencia.
3. **Tu filtro**: "Te engañaron 5 de 21 palabras inventadas" + un renglón por tipo (obvias / una letra / letras cambiadas; en
   coral solo el que más engañó). Si las cambiadas
   engañan más: "Las letras cambiadas de lugar engañan a casi todos: leemos la palabra entera. Truco: mira el centro
   de la palabra".
4. **Tu colección**: las palabras raras acertadas se guardan (como la bitácora de Bitácora de Misión).

Nota común al pie ("Medida de esta partida… No es un diagnóstico"). Nada de "tu vocabulario es de X palabras" ni
comparaciones con otras personas hasta tener datos propios. Agregar las medidas a `docs/medidas-juegos-estrella.md`.

## 6. Contenido: el léxico (español primero)

- Archivo de datos por idioma, fuera del código: `unity/NeuroVidaCore/Assets/Resources/Lexico/meteoros_es.json`
  (palabra, banda 1-6, largo) y una lista de inventadas pregeneradas por tipo. Otro idioma = otro archivo.
- **Palabras**: sustantivos, adjetivos y verbos en infinitivo comunes a España y Latinoamérica; 4-12 letras; sin
  nombres propios, extranjerismos, groserías, palabras sensibles (enfermedades, muerte, violencia) ni regionalismos.
  ~1.200 palabras (200 por banda) para empezar. La banda sale de qué tan conocida es (SPALEX si se obtiene permiso; si
  no, frecuencia de una lista abierta).
- **Inventadas** (generador propio en `tools/lexico/`, NO Wuggy ni listas ajenas): (a) obvias = sílabas españolas
  recombinadas respetando cómo se escribe en español; (b) una letra cambiada de una palabra real del mismo largo;
  (c) dos letras INTERIORES traspuestas (nunca la primera ni la última). Filtro: que no sea una palabra real (se
  compara contra un diccionario abierto solo al generar; el diccionario no va en la app), que no suene grosera ni a
  marca, y que se pueda pronunciar.
- Ricardo revisa una muestra de 100 palabras y 100 inventadas antes de programar.

## 7. Arte y sonido

- Roca de arcilla con volumen y **brasa** (opción B de `docs/previews/meteoros-rocas.png`, elegida por Ricardo el 1-oct): campo
  de alturas con cráteres y grano, el borde que va por delante al rojo vivo, borde tinta, sombra dura abajo; 4 formas al azar en
  lila, celeste o coral (`MeteorSprites`); placa crema; **estela de calor** con chispas (naranja → coral → lila). Dorado = roca
  sol con aro. Estrellas que vuelan a la constelación. Las rocas se hornean de a una por cuadro durante la cuenta regresiva.
- Sonidos sintetizados en la pentatónica de `GameFeel`: toque correcto = campana (nota según la racha), inventada
  bien dejada pasar = soplo suave, error = madera sorda, lluvia de estrellas = cascada de campanas. Nada arcade.
- Respeta "quitar animaciones" (sin giro de la roca, estela corta y SIN chispas; la brasa es estática), sonido y vibración apagados.

## 8. Pendiente antes de programar

1. ~~**Licencia de SPALEX**~~ **RESUELTA el 1-oct.** Respondió el autor, Prof. Jon Andoni Duñabeitia (Universidad Nebrija):
   los datos completos están en FigShare (https://figshare.com/projects/SPALEX/29722) bajo **CC BY 4.0**, que permite
   uso comercial respetando la atribución. Se verificó que son los mismos datos que los de OSF (44.853 palabras, mismas
   columnas y valores); el léxico se genera ahora desde FigShare y el JSON declara `licencia` y `cambios`. La atribución
   está en la app: **Ajustes → "Licencias y créditos"** (cita completa, licencia y el cambio hecho). Ya no hace falta el
   plan B (`wordfreq`).
2. Aprobar la maqueta (`docs/previews/meteoros.png`).
3. Aprobar la muestra de palabras e inventadas.
