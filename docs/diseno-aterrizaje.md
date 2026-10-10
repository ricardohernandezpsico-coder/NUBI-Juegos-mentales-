# Aterrizaje Lunar: «la misma tarea, más grande, con más vida y un final con sentido» (10-oct-2026)

Boceto aprobado por Ricardo el 10-oct, versión 4: `docs/previews/aterrizaje-boceto.html` (artifact https://claude.ai/artifact/GergAmEaGjkAPNmWCFkG2y). Mismo id `aterrizaje`, área Razonamiento, carpeta `Games/Aterrizaje/`. Con este juego se cierra la Etapa 2.

## 1. Por qué

- **Hoy:** el juego funciona y ya tiene tutorial, pero se veía plano (nave chica, regla delgada, cielo vacío) y no estaba al nivel de Rescate v4 ni de Acoplamiento.
- **Las pruebas del boceto con Ricardo:**
  - v1: «me gusta bastante cómo se ve, el funcionamiento especialmente».
  - Reparo al final: el gráfico «Tu línea» «no tiene ningún dato relevante… no enseña nada… ¿para qué le sirvió a la persona?». Pidió llevarlo a algo comprensible en el día a día.
  - Reparo al fondo: «muy básico, un espacio grande donde no hay nada».
  - v2: los ejemplos muy específicos («cuánta bencina queda») no le gustaron. Pidió algo «más genérico… interpretable de diferentes maneras, pero con una base sólida».
  - v3: «es demasiada información». Lo más importante es lo que hizo la persona; el porqué, breve y abajo.
  - v4: aprobado.

## 2. Reglas: SIN CAMBIOS (`LandingContract`)

- 12 niveles:
  - 1-4: de 0-10 a 0-100, con y sin marca del medio;
  - 5-6: 0-1000;
  - 7: fracciones;
  - 8: decimales y porcentajes;
  - 9: regla que no empieza en 0;
  - 10: sumas;
  - 11: fracciones de 0 a 2;
  - 12: negativos.
- Acierto si el error es ≤ 5 % del largo; «diana» si es ≤ 1,2 %.
- Bajada de 6 → 3,5 s (Precisión: 10 s). Reto de 120 s; Precisión de 15 aterrizajes; versión corta de 8.
- Motor común `stepUp` 0,3. Formato de números fijo, con coma decimal.
- El tutorial guiado actual (zonas que se achican) se queda; solo cambia su aspecto.

## 3. Pantalla (referencia 360 × 780 dp; 16:9 escala como Rescate)

| Pieza | Valor |
|---|---|
| Marcador | «Aterrizaje Lunar», «Aterrizaje N de 15» (Reto: «Aterrizaje N» + barra), «Nivel N». A la derecha, una cúpula y el número de cúpulas, más 5 puntos de avance hacia la próxima; «Racha ×N» desde 2 |
| Misión | «Aterriza en» (16 dp) y el número en sol, 52 dp. Se achica si no cabe; sin tarjeta |
| Regla | de x 36 a x 324, y 604, alto 20, arcilla crema con borde de tinta. Marcas de extremo más largas; marca del medio solo en los niveles con medio. Extremos en 20 dp |
| Nave | módulo lunar ×1,25 (unos 75 dp con patas): cabina crema con ventana celeste y base sol. Llama mientras baja; dos luces (coral y lima) que parpadean |
| Guía | puntos lima desde la nave hasta la regla y una flecha sobre la regla |
| Instrucción | «Arrastra para mover la nave · suelta para aterrizar» (14 dp, y 748), solo en los primeros 3 aterrizajes |

- **Control:** el dedo en CUALQUIER parte del cielo (y > 160) mueve la nave a lo ancho, con suavizado (factor 14/s).
  - Al soltar, cae en 320 ms.
  - Si la bajada termina sin soltar, se posa donde está.
- **Al posarse:**
  - polvo (14 motas);
  - el tramo entre la nave y el lugar justo, en lima (acierto) o coral, más un badge ✓ o aspa bajo el tramo: **nunca solo color**;
  - una bandera de 80 dp que sube en el lugar justo, con su etiqueta. En las sumas, la etiqueta es el RESULTADO.
- **Avisos**, en el cielo vacío (y ≈ 300-340), después de posarse:

| Caso | Aviso |
|---|---|
| Diana | «¡Diana lunar!» |
| Acierto | «¡Justo! a N del blanco» (desde la racha 3, con «· Racha ×N») |
| Fuera del 5 % | «Cerca: a N del blanco» |

  - En reglas enteras, N va en unidades; en el resto, en %.
  - Nunca sobre la nave, la regla ni la bandera.
- **Cúpulas:** cada 5 aterrizajes justos se arma una, con el aviso «¡Nueva cúpula en tu base!» y un acorde. Va SOLO en el marcador: nada sobre el suelo.

## 4. El mundo: nada fijo junto a la regla (serían pistas para estimar)

- **Cielo:**
  - franja suave de nebulosa en diagonal;
  - 26 estrellas que titilan;
  - una estrella fugaz cada 8-15 s;
  - un satélite que cruza despacio (9 dp/s).
- **La Tierra** (versión 3 del boceto, `drawEarth`), en (318, 300) con r 58:
  - océano en degradado, tierras suaves lima y nubes en trazos;
  - la noche como degradado suave;
  - borde de atmósfera, resplandor y borde de tinta.
  - Ricardo la aprobó. La versión «con anillo» queda en el boceto como alternativa, sin usar.
- **Dos cordilleras lunares** detrás del borde de la luna, que se desplazan despacio (3 y 7 dp/s). Como se mueven, no sirven de marca.
- **La luna:** el suelo, con borde de tinta y cráteres tenues solo lejos de la regla (y > 690).
- **«Quitar animaciones»:**
  - sin titileo, estrella fugaz ni satélite;
  - **sin las dos cordilleras**: quietas junto a la regla serían marcas;
  - luces fijas, sin polvo ni confeti;
  - la bandera aparece de una vez.

## 5. Sonido «Madera cálida» (con `Games/Shared/SoundKit.cs`)

| Momento | Receta (boceto: `SFX`) |
|---|---|
| Mientras baja | bucle suave de ruido café con paso bajo de 420 Hz (4 s sin costura), volumen 0,09. Baja a 0 al soltar; respeta la congelación de Nubi (30 %) |
| Soltar | soplo de paso bajo 1400 → 300 Hz, pico 0,08 |
| Posarse | golpe grave de 110 → 55 Hz + bloque de madera de 520 Hz, pico 0,26 |
| Sube la bandera | kalimba de 784 Hz, pico 0,2 |
| Acierto | marimba de 659 y 880 Hz, pico 0,26 |
| Diana | marimba do-mi-sol-do + kalimba, pico 0,34 |
| Lejos | marimba de 196 y 165 Hz, pico 0,2 |
| Cúpula | acorde de marimba + kalimba, pico 0,36 |
| Final | rodado de marimba, pico 0,4 |

## 6. El final con sentido (pantalla de resultado de la APP)

En orden de importancia. Es la plantilla que después se puede llevar a los demás juegos, si Ricardo lo confirma.

1. **Título:** «N aterrizajes justos de M».
2. **Lo que hiciste (recuadro principal):**
   - «Tu distancia promedio al lugar justo» (rótulo chico);
   - en grande y en sol, **«X % de la regla»**, con debajo la línea simple «Como quedar a X en una regla de 0 a 100.». X es `numline_error_pct` redondeado, mínimo 1. (Cambiado el 10-oct por Ricardo, Tarea 71: «a 9 de cada 100 qué… metros, centímetros, números… a ojos de un técnico sería raro»; antes decía «a X de cada 100 del lugar justo».);
   - el dato tuyo, sacado de `NumberLine.reading`:

| Lectura | Línea |
|---|---|
| INICIO | «Te costó más el comienzo de la regla» |
| CENTRO | «Te costó más el medio de la regla» |
| FINAL | «Te costó más el final de la regla» |
| PAREJA | «Quedaste igual de cerca en toda la regla» |
| SIN_DATOS | sin línea |

   - una línea chica: «N dianas lunares · racha mayor ×N · N cúpulas», en singular cuando corresponde.
3. **Tu avance, solo contigo,** en una fila de tres chips: «Hoy», «Tu promedio» y «Tu mejor», cada uno «N %». El promedio y el mejor salen de las partidas ANTERIORES de este juego (`StarMeasures`). Frase según el caso:

| Caso | Frase |
|---|---|
| Sin partidas anteriores | «Juega otra vez para ver tu avance.» |
| Mejor que su mejor anterior | «¡Tu partida más precisa hasta ahora!» |
| Más cerca que el promedio (> 0,5) | «Hoy quedaste más cerca que tu promedio.» |
| Más lejos que el promedio (> 0,5) | «Un poco más lejos que tu promedio: es normal que varíe.» |
| Parecido al promedio | «Igual que tu promedio: vas parejo.» |

   Nunca percentiles ni comparación con otras personas.
4. **«Truco para la próxima»** (recuadro chico), según la lectura:

| Lectura | Truco |
|---|---|
| INICIO | «En el primer tramo, mide desde el 0 y desde la mitad: el cuarto queda entre los dos.» |
| CENTRO | «Cerca del medio, ubica primero la mitad exacta y corrige desde ahí.» |
| FINAL | «En el último tramo, mide hacia atrás desde el final, no solo desde el 0.» |
| PAREJA o SIN_DATOS | «Antes de soltar, busca la mitad: ¿tu número está antes o después?» |

5. **Abajo, en chico y sin recuadro:**
   - «¿Por qué importa?»;
   - «Pusiste en juego la estimación en proporción. Ubicar números a ojo se relaciona con cómo valoramos montos, tiempos y riesgos al decidir.»;
   - «Fuente: Schley y Peters, 2014»;
   - «Medida de esta partida. No es un diagnóstico.»

- **Se va:** el gráfico «Tu línea» del resultado (puntos y marcas), que no le decía nada a la persona.
- **Construirlo como componente reutilizable** de Compose, por ejemplo `ui/components/MeaningfulResult.kt`, con cuatro piezas: lo que hiciste, tu avance, el truco y el porqué con su fuente. Por ahora lo usa solo Aterrizaje.

## 7. Base científica (PubMed, 10-oct)

- **La tarea:** línea numérica de Siegler y Opfer, 2003. En niños se asocia con la competencia matemática (metaanálisis de 263 efectos, r = 0,44; Schneider et al. 2018, doi:10.1111/cdev.13068).
- **En adultos se resuelve como juicio de proporción con puntos de referencia:** Barth y Paladino 2011. Es la base de los trucos.
- **Por qué importa:** la forma en que cada persona ubica los números en la mente guía cómo valora montos y elecciones con riesgo (Schley y Peters 2014, doi:10.1177/0956797613515485).
- **El manejo de números** se relaciona con la calidad de decisiones de salud y de dinero, y empieza a bajar desde la mitad de la vida (Best et al. 2021, seguimiento de 11 años, doi:10.1037/pag0000657).
- **Lo que NO se promete:** en un ensayo con 122 adultos, practicar la línea numérica afinó la ubicación de números y la suma de precios a ojo, pero no mejoró otras medidas de manejo numérico ni la toma de decisiones (Sobkow et al. 2019, doi:10.1037/xap0000207).

## 8. Récord y telemetría

- **Prefs `aterrizaje_record`**, con respaldo como las demás: cúpulas totales y mejor partida.
- **Telemetría nueva:** `bulls` y `domes`. `numline_true` y `numline_given` siguen como hoy.

## 9. Qué se va

- La nave chica y la regla delgada.
- El cielo vacío.
- El gráfico «Tu línea» del resultado.

## 10. Cómo quedó hecho (Tarea 70, 10-oct)

**Código** (`Games/Aterrizaje/`):
- `LandingContract.cs`: las reglas de hoy (sin cambios) más la unidad de la regla (`Unit`), el texto de la bandera (`FlagText`: en las sumas dice el RESULTADO), `DistanceText`, las cúpulas (`DomeEvery`), los tiempos del resultado y todos los textos.
- `LandingLayout.cs`: `LandingPlan(alto, guiada)`, la pantalla en dp como lógica pura probada, y las cajas de la nave, la bandera, los avisos, la regla y la misión que usan las pruebas y las guardias.
- `LandingSprites.cs` (todo el arte hecho con `ClayRaster`: nave, bandera, Tierra, luna, cordilleras, nebulosa, satélite, estrella fugaz, cúpula, insignias) y `LandingSounds.cs` (los ocho sonidos y el bucle de vuelo, con los instrumentos de `RadarSounds`).
- `LandingGameController.cs` y sus partes `.Build.cs` (la interfaz), `.Scene.cs` (el mundo, la nave, el resultado, los avisos, el marcador y el cierre), `.Guided.cs` (el tutorial) y `.Shots.cs` (las capturas; solo en el Editor).

**App** (`app/.../`): `ui/components/MeaningfulResult.kt` (el componente reutilizable: lo que hiciste, tu avance, el truco y el porqué con su fuente), `data/Aterrizaje.kt` (la lectura del final y las cúpulas), `data/NumberLine.kt` (`line` y `trick`), preferencias `aterrizaje_record` (claves `total` y `best`, con respaldo) y la sección de Aterrizaje de `GameResultScreen`, sin el gráfico «Tu línea».

**Lo que quedó distinto del boceto o del diseño, y por qué:**
1. **La nave empieza en un lugar al azar** (entre el 10 y el 90 % del ancho) en cada aterrizaje. Empezar siempre en el centro dejaría una pista fija (la mitad) justo cuando se estima.
2. **La bandera se dibuja ENCIMA de la nave** y su etiqueta sube 22 dp sobre el mástil: si la nave cae justo en el blanco, el lugar justo igual se ve.
3. **Decimales en la distancia.** En reglas enteras el aviso dice «a 3 del blanco»; con reglas cortas lleva decimales (largo hasta 2: «0,05»; hasta 25: «0,3») para que un aterrizaje justo nunca diga «a 0». En fracciones y decimales se dice en % de la regla, nunca menos de 1.
4. **El aviso de la cúpula va en una segunda fila** a los 1,5 s de posarse (620 ms + 900 ms) y el resultado se mantiene hasta que ese aviso termina; el aviso del resultado sale antes, así no se pisan.
5. **Una línea más en el final de la app:** «Tu base lunar: N cúpulas» (las de toda la vida; solo si hay alguna), debajo de «N dianas lunares · racha mayor ×N · N cúpulas». El diseño pedía solo la segunda.
6. **Telemetría:** las dianas reutilizan `numline_bullseyes` (ya existía) en vez de un campo nuevo `bulls`; `domes` y `land_streak` (la racha mayor) son nuevos en `StroopTelemetry`.
7. **16:9.** La regla va anclada abajo (`RulerY = máx(380, alto − 176)`): una pantalla más baja acorta la bajada y una más alta la alarga. Con el tutorial se reservan 96 dp abajo para los controles de Nubi.
8. **«Quitar animaciones» no dibuja las cordilleras** (quietas junto a la regla serían marcas) y el smoke lo vigila (`GuardWorld`, además de la guardia de avisos `GuardNotices`: ningún aviso toca la nave, la regla, la bandera ni la misión).
9. **El cierre de Unity** («¡MISIÓN CUMPLIDA!») es corto (título, la estimación y una línea); el detalle lo da la app. El truco de Aterrizaje ya no pasa por `ResultAdvice`: viene en su propio final.
10. **Tutorial.** Se queda (una ronda guiada de un aterrizaje de práctica con la franja sol sobre el lugar justo) y se viste con el aspecto nuevo, con las reglas de la Tarea 67: la práctica ASEGURA (nunca alterna), no alimenta el motor, las medidas ni las cúpulas, y el smoke mira el estado. La primera tarjeta de Nubi deja a la vista la nave (zona protegida). Al sacar las capturas se vio y se arregló una barra de tiempo del Reto que aparecía durante el tutorial.

**Pruebas.** Unity: `LandingContractTests` (6), `LandingV2Tests` (19: las reglas de hoy intactas, textos, distancias, cúpulas, tiempos, la pantalla de 780 y varias formas de teléfono, ningún aviso sobre la nave, la regla ni la bandera, y el arte: cráteres solo lejos de la regla, cordilleras que se repiten sin saltos, insignias nunca solo color) y `LandingSoundTests` (13: los ocho sonidos contra la salida del propio boceto corrida en Node, `tools/sonido/referencia-aterrizaje.js`, el bucle de vuelo y la memoria). Kotlin: `AterrizajeTest` (12), `AterrizajeRecordTest` (4), `NumberLineTest` (6), `BackupRulesTest` y `AterrizajeResultScreenshotTest` (3 capturas Roborazzi en `app/src/test/screenshots/aterrizaje-final-*.png`).

**Verificación completa (10-oct):** EditMode 815 pruebas, smoke de 118 juegos (con las corridas «Reto», «Mudo», «Pantalla», «CómoSeJuega» y «Corto» de Aterrizaje y el tutorial en tres formas de pantalla), Kotlin 590, instalado en el teléfono.

**Capturas reales:** `bash tools/verificar-todo.sh --capturas Aterrizaje` → `docs/previews/capturas/aterrizaje.png` (20:9 y 16:9).

## 11. Pendientes (para la próxima tarea de Unity)

- **RESUELTO (Tarea 72, 10-oct) — el cierre «¡Listo!» del juego decía «a N de cada 100»** (`LandingContract.EndEstimate`, `EndBoxTitle`, `EndBoxUnit`). Ahora dice lo mismo que la app: «Tu distancia promedio al lugar justo» (se achica hasta 14 dp para caber en una línea del recuadro), «N % de la regla» (`EndEstimate`) y «Como quedar a N en una regla de 0 a 100.» (`EndAnalogy`, que reemplaza a la constante `EndBoxUnit`), con el mismo N de la app y vacía sin aterrizajes.
- **RESUELTO (Tarea 72) — el «14 de 13» de las capturas.** El culpable no era `AddFakeProgress` (que sí suma `_trials`), sino el guion de capturas, que para que el quinto justo armara la cúpula ponía `_hits = 4` sin sumar `_trials`; en el guion corto de 16:9 solo se habían jugado dos aterrizajes. Ahora `ForceHits(n)` (`.Shots.cs`) suma los aterrizajes que falten como justos: los justos nunca superan los aterrizajes. En una partida real no pasaba.
