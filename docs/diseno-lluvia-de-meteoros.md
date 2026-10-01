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
- **El meteoro**: roca de arcilla con una placa crema donde va la palabra (Fredoka, ≥ 24 sp; 28 sp en mayores), en
  minúsculas y con tildes. La placa es lo que se lee: siempre horizontal aunque la roca gire.
- **Tocar una palabra real** → la roca estalla en estrellas que vuelan a la **constelación** de arriba (contador de
  palabras rescatadas) + ✓ de arcilla. Nota musical de la pentatónica de `GameFeel` (la racha arma una melodía).
- **Dejar pasar una inventada** → al llegar a la atmósfera se deshace en chispas suaves (sin castigo; cuenta como
  acierto, sin puntos ruidosos).
- **Tocar una inventada** → la roca se agrieta en polvo gris + ✗ y un golpe sordo suave; la palabra queda tachada un
  instante ("brúgala"). Nunca solo color: forma (✗, grietas) + texto.
- **Dejar pasar una real** → se va con su nombre en pequeño ("se fue: brújula"). Sin culpa.
- El toque cuenta al PRESIONAR. Zona de toque = toda la roca + 12 dp (mínimo 56 dp; 64 en mayores).

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

1. **Tu vocabulario**: por banda (de común a rara), % de palabras reconocidas, corregido por inventadas tocadas en
   esa misma partida (aciertos − falsas alarmas de su nivel, como Lextale). Se dibuja como 6 columnas de estrellas que
   se encienden. Lectura: "Reconoces casi todas hasta las poco frecuentes; las raras, la mitad". Solo bandas con ≥ 6
   palabras vistas; si no, "juega más para medir esta banda".
2. **Tu reconocimiento**: mediana del tiempo de toque en palabras comunes contra raras ("comunes 0,7 s · raras 1,1 s").
   Es el efecto de frecuencia: normal que las raras tarden más.
3. **Tu filtro**: inventadas que engañaron, por tipo (obvias / una letra / letras cambiadas). Si las cambiadas
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

- Roca de arcilla (`ClayRaster`, borde tinta, sombra dura abajo), 4 formas al azar; placa crema; estela con el color
  del meteoro (lila, celeste, coral). Dorado = aro sol. Estrellas que vuelan a la constelación.
- Sonidos sintetizados en la pentatónica de `GameFeel`: toque correcto = campana (nota según la racha), inventada
  bien dejada pasar = soplo suave, error = madera sorda, lluvia de estrellas = cascada de campanas. Nada arcade.
- Respeta "quitar animaciones" (sin giro de la roca ni estela larga), sonido y vibración apagados.

## 8. Pendiente antes de programar

1. **Licencia de SPALEX**: el artículo es de acceso abierto (CC BY), pero la página de los datos en OSF
   (osf.io/m8r9s) no declara licencia, así que hay que **pedir permiso** a los autores para uso comercial (borrador
   de correo en la conversación del 30-sep). Plan B: frecuencias de `wordfreq` (datos CC BY-SA 4.0): el archivo de
   léxico derivado iría con esa licencia y atribución en la pantalla de licencias.
2. Aprobar la maqueta (`docs/previews/meteoros.png`).
3. Aprobar la muestra de palabras e inventadas.
