# Alargar la dificultad: pistas del Contrarreloj y escaleras de los juegos (propuesta, 4-nov-2026)

Ricardo dijo que sí a las dos cosas (4-nov). Esta es una propuesta ESCRITA (solo documento: no cambia código, ni Unity, ni la app) sobre la base de [`techo-dificultad.md`](techo-dificultad.md) §6, §7 y §8. Las simulaciones de la parte A están en [`tools/alargar_pistas.py`](../tools/alargar_pistas.py) (Python, unos 10 s por juego); los hechos de cada juego (tablas por nivel, contenido, dónde vive cada cosa, pruebas) los leí del código y van con archivo y línea.

## 0. En pocas líneas

- **Pistas (parte A).** Tinta o Palabra pasa de 5 a **8 pasos, uno cada 12 aciertos**; ¿Verdad o disparate? de 12 a **14 pasos, uno cada 4**; Acoplamiento de 12 a **17 pasos, uno cada 2**. Con eso, en 18 a 29 años el 99 % llega al último paso entre el segundo 105 y el 118 y el 80 % casi nunca llega (hoy lo toca entre el 48 y el 90). En 65 a 74 y 75 o más **nadie se pega** al último paso. Tinta y Acoplamiento se hacen **solo con parámetros que ya existen**; Disparate pide **un tipo de frase nuevo** (unas 400 frases): sin contenido nuevo no hay cómo alargarlo.
- **Escaleras (parte B).** Primero Carga exacta, Tinta y Punta (3 niveles cada uno: Carga y Tinta son solo parámetros; Punta pide **150 a 240 palabras nuevas con definición**); después Acoplamiento (+5), Engranajes (+3, hay que tocar código), ¿Verdad o disparate? (+2, el tipo nuevo) y La estrella intrusa (**no se puede alargar con parámetros**: se queda en investigación). Satélites **baja su paso** de 1,0 a 0,25 (0,17 mayores, 0,22 menores de 18), como Rastro.
- **Puntos (tu propuesta A, afinada).** Hoy un rating guardado es una fracción de la escalera. Se separan dos números: el **avance de puntos**, anclado al largo de la escalera VIEJA (`pointsLevels`), y el **avance de la escalera**, con el largo nuevo. Se migra el rating para conservar el nivel (`r' = r · L_viejo / L_nuevo`) y los puntos no se mueven: nadie pierde puntos y los niveles nuevos no suman.
- **Algo que no estaba en el pedido y hay que arreglar**: el rating sembrado de quien no tiene historial (`Baseline.DEFAULT_LEVEL` = 0,45) es una fracción de la escalera: con una escalera más larga, un jugador NUEVO partiría varios niveles más arriba que hoy (Tinta: del nivel 3,25 al 4,6 de 8). Se escala igual que el rating guardado.
- **Dónde se ve hoy el número de nivel** (§B.4): en casi ningún lado. No hay «nivel N de M» al jugar; el único «N de M» es el paso de la pista del Contrarreloj («Llegaste al paso N de M de la pista») y, en un solo juego, Piloto. Lo que cambia para la persona es la frase «Tu nivel: ya …» de la página del área en Avance y el aviso «¡Un paso más!» dentro del juego.

---

## A. Pistas del Contrarreloj (Tinta o Palabra, ¿Verdad o disparate?, Acoplamiento; Rescate no se toca)

### A.1 El método y qué tan confiable es

La referencia estimada sale de robots que juegan la pista real (`Games/Tests/AgeReference.cs`). Para poder probar pistas nuevas sin Unity **porté el robot a Python** (lentitud lognormal por franja, exactitud normal, vaivén de 0,25, mejor marca de 3 partidas, las frases reales de `disparate_es.json`, los ángulos y plazos de `DockingContract`) y lo **calibré contra las tablas que ya están en la app**: reproduce p25, p50, p75 y p90 de las 6 franjas de los tres juegos con **diferencia de 0 o 1 acierto** (`python -I tools/alargar_pistas.py calibrar`). Lo que sigue es una estimación con esa porta; el valor exacto de cada tabla nueva sale de `bash tools/verificar-todo.sh --referencia` (en Unity, ~1 minuto) cuando la tarea esté hecha.

Lo que **no** sabe la porta (y el robot de Unity tampoco): el robot de Acoplamiento no ve cuántas celdas tiene la pieza, y el de Disparate no sabe nada de un tipo de frase nuevo. Lo digo en cada juego.

### A.2 Qué se busca (tu criterio) y qué se logra

En 18 a 29 años: la marca del 99 % llega al último paso cerca del final (≥ ~105 s) y la del 80 % no llega. Columnas: marca de la línea · paso al que llega esa marca · segundo en que llega al último paso (y qué % de esas personas llega).

| Juego y pista | 80 % (18-29) | 90 % (18-29) | 99 % (18-29) | 65-74: 80 · 90 · 99 % | 75 o más: 80 · 90 · 99 % |
|---|---|---|---|---|---|
| Tinta HOY: 5 pasos, uno cada 8 (último a los 32) | 76 · 5 de 5 · 48 s | 80 · 5 de 5 · 46 s | 92 · 5 de 5 · 42 s | llegan al paso 5 entre el segundo 49 y el 64 | entre el 52 y el 72 |
| **Tinta nueva: 8 pasos, uno cada 12 (último a los 84)** | 78 · paso 7 de 8 · no llega (2 %) | 83 · 8 de 8 · 118 s (65 %) | 94 · 8 de 8 · **105 s** (100 %) | 57 · 6 · no llega; 62 · 6 · no; 74 · 7 · no | 51 · 5; 57 · 6; 69 · 7: no llega |
| Disparate HOY: 12 pasos, uno cada 3 (último a los 33) | 48 · 12 de 12 · 75 s | 50 · 72 s | 55 · 67 s | 38 · 94 s; 41 · 87 s; 49 · 70 s | 34 · 104 s; 37 · 97 s; 45 · 79 s |
| **Disparate nuevo: 14 pasos, uno cada 4 (último a los 52)** | 49 · 13 de 14 · 119 s (13 %) | 51 · 14 de 14 · 118 s (51 %) | 55 · 14 de 14 · **111 s** (100 %) | 39 · 11 · no; 43 · 12 · no; 50 · 13 · 119 s (38 %) | 35 · 10 · no; 39 · 11 · no; 46 · 12 · no |
| Acoplamiento HOY: 12 pasos, uno cada 2 (último a los 22) | 29 · 12 de 12 · 90 s | 31 · 87 s | 33 · 82 s | 23 · 104 s; 26 · 97 s; 30 · 88 s | 21 · 110 s; 23 · 104 s; 28 · 94 s |
| **Acoplamiento nuevo: 17 pasos, uno cada 2 (último a los 32)** | 29 · 15 de 17 · no llega | 31 · 16 de 17 · 119 s (4 %) | 33 · 17 de 17 · **118 s** (50 %) | 23 · 13 · no; 26 · 14 · no; 30 · 16 · no | 21 · 12 · no; 23 · 13 · no; 28 · 15 · no |

- **Cómo se eligió el largo.** El último paso cae en el acierto `(pasos − 1) × cada_N`. Tiene que quedar entre los aciertos del 80 % y los del 99 %: Tinta 78 y 96 → 84; Disparate 49 y 57 → 52; Acoplamiento 31 y 35 → 32. En Acoplamiento esa ventana mide 4 aciertos, por eso la regla es la más justa de las tres (**con 15 pasos el 80 % llegaba en el segundo 114; con «cada 3» y 12 pasos las líneas del 10 al 90 % se juntan y dos divisiones quedan vacías**: descartados).
- **Se pueden hacer y por qué no más pasos.** Con 18 pasos en Acoplamiento ni el 99 % llega; con 14 en Disparate cada 3 el 80 % llega al 95 s. Las tres combinaciones elegidas son las únicas de la rejilla probada (`informe` de `alargar_pistas.py`) que cumplen las dos condiciones.
- **Regla de avance.** Tinta pasa de «cada 8» a **«cada 12»** porque la escalera gana pasos y se quiere que el último caiga tarde (cada paso dura unos 17 s, como un tramo de una frase en vez de 11 s); Disparate pasa de «cada 3» a **«cada 4»** por lo mismo; Acoplamiento **sigue en «cada 2»**: su comentario del código dice que con «cada 3» nadie pasaba del paso 8, y con las marcas de hoy «cada 3» junta líneas de la liga en la misma marca (18 a 29 y 30 a 44 años).
- **Las 10 divisiones de la liga siguen cayendo en marcas distintas** (nueve líneas del 10 al 90 %) en los tres juegos y las 6 franjas, **salvo** una combinación de las que probé en Acoplamiento (45 a 54 con la hipótesis de celdas lentas, que no se recomienda). La separación entre el 80 y el 90 % **no mejora** (Tinta 5, Disparate 2 a 4, Acoplamiento 1 a 3 aciertos, igual que hoy): **alargar la pista no ensancha la distribución** porque ningún robot se frena más en los pasos nuevos. Para ensancharla hay que hacer los pasos más lentos o más fallones (más celdas, frases más largas); eso solo se sabe midiendo con personas (ver A.5).
- **Las marcas suben 1 o 2 aciertos** (pasos nuevos más largos al principio): p50 de 18 a 29, Tinta 66 → 67 o 68, Disparate 42 → 43; Acoplamiento no cambia (el robot no ve los pasos nuevos). Los umbrales de la liga se mueven con la tabla, que se regenera.

### A.3 Los pasos nuevos, con parámetros que el juego YA tiene

| Juego | Pasos nuevos | Parámetros (los que ya existen) |
|---|---|---|
| Tinta o Palabra | 6, 7 y 8 | las dos reglas al azar como el 5; **cambios de orilla 0,55 · 0,60 · 0,65** (hoy 0,50 en el 5); **palabras que chocan 0,85** (sin cambio: ver abajo); **llegada de la tarjeta 210 · 180 · 150 ms** (hoy 240 en el 5) |
| ¿Verdad o disparate? | 13 y 14 | **tipo de frase 7 NUEVO** (A.4); señal 0 en el Contrarreloj |
| Acoplamiento | 13 a 17 | **celdas 8 · 8 · 9 · 9 · 10** (hoy 7 en el 12); giro 180° y paso de 15° como en el 12; plazo **2,5 s fijo** (como el 12) |

- **Tinta, por qué choques en 0,85 y no más.** El final mide interferencia solo con ≥ 3 palabras que coinciden (`MinPerKind`); con 16 palabras y 0,85 de choques ya salen ~2,4 coincidentes, y con 0,90 saldrían 1,6: la medida casi nunca aparecería. Con 0,85 en los tres pasos nuevos la porta da casi lo mismo que con 0,88 a 0,90 (p90 de 18 a 29: 80 contra 82; p50 de 65 a 74: 48 contra 47).
- **Acoplamiento, por qué no se acorta el plazo.** Con el plazo de hoy (2,5 s en el paso 12) una persona de 65 a 74 años ya agota el tiempo en muchas figuras (con el modelo del robot, su respuesta mediana en una figura típica ronda 2,6 s contra un plazo de 2,5 s); acortarlo más castigaría a los mayores, y en el Contrarreloj se acepta la respuesta recién a los 1,2 s. Por eso los pasos nuevos suben las celdas, que sí caben: la generación de piezas funciona hasta 10 celdas (aceptación por intento: 8 → 57 %, 9 → 52 %, 10 → 30 %; la probabilidad de caer en el respaldo de 4 celdas es menor que 1e-23) y el arte se dibuja por celdas sin tocar el diseño. Las pruebas de hoy solo ejercitan de 4 a 7 celdas (`DockingContractTests.cs:30`).
- **La fórmula del plazo depende de `MaxLevel`** (`6000 − round(3500 · (l − 1) / (MaxLevel − 1))`): al pasar de 12 a 17 cambiaría el plazo de los 12 pasos de hoy. Se reemplaza por una tabla explícita (los 12 valores de hoy, 6000 a 2500, y 2500 en los cinco nuevos) y una prueba que fija los 12.

### A.4 Contenido nuevo (solo Disparate)

El tipo 7 («**dos afirmaciones unidas por «y» o «pero»; la frase es verdad solo si las dos lo son**», de 8 a 12 palabras, ≤ 50 caracteres como el resto) hace falta porque en el Contrarreloj de Disparate el único «botón» que sube la dificultad es el TIPO de frase (el 12 ya es el tipo 6, el más alto; `DisparateContract.cs:71`). Cantidad: **160 a 200 verdades y 160 a 200 disparates** (el generador toma `min(V, D, 200)` por tipo; las pruebas exigen ≥ 120 de cada respuesta por tipo). Vive en `tools/frases/disparate.py` (un generador `tipo7`) y `tools/frases/conocimiento.py`; sale a `disparate_es.json` (hoy 2.242 frases, 246 KB). **Muestra para revisar en §B.5.**

El robot de Disparate necesita un número nuevo para el tipo 7: cuánto resta de exactitud (`DisparateHard[6]`). **No lo invento**: la porta usa 0,03, el mismo valor de los dos tipos más duros (3 y 6), y las palabras de cada frase salen del banco real. Mientras no haya frases, la tabla de Disparate es una estimación; se recalcula con las frases ya generadas.

### A.5 La referencia estimada: qué cambia y cuánto cuesta

- **Qué parámetros de robot usaría en los pasos nuevos.** Ninguno nuevo, salvo el 0,03 de arriba. Tinta: el robot YA modela los tres parámetros que cambian (cambios, choques, llegada: `StroopContract.Sequencer` y `ArrivalSeconds`). Acoplamiento: el robot depende del ángulo, del espejo y del plazo, no de las celdas; **no agrego un coeficiente de celdas porque no hay dato** (lo probé como hipótesis, +0,15 s por celda sobre 7: casi no cambia nada, porque pocos llegan a los pasos 13 y más). La tabla de Acoplamiento subestima, entonces, la carga de las celdas nuevas; la medida real vendría de las 5 personas de Ricardo con el «[Debug] Contrarreloj de invitado».
- **Tramos y borde de la pista (`topMark`).** No cambian: p99 cae en 94 de 100 (Tinta), 55 de 80 (Disparate) y 34 de 40 (Acoplamiento).
- **Versión de cada pista (`Track.Version`).** Tinta, Disparate y Acoplamiento pasan de 2 a **3**; entran en la semilla, en el nombre del archivo `<juego>_v3.json` (`EstimatedReference.kt:15,28`) y en los récords. El récord viejo queda como «tu récord de la pista 2» (lo que ya hace la app con las pistas anteriores). Rescate sigue en 3. La regla del 1-nov se mantiene: estructura fija (pasos y avance) y contenido al azar (T210).
- **Costo de recalcular.** `bash tools/verificar-todo.sh --referencia`: ~1 minuto, los tres JSON nuevos. Hay una cosa más: **`ReferenceScales`** (0,89 / 0,92 / 0,90) se calibraron para que la mediana de las 5 personas de Ricardo quedara en 50; con las pistas nuevas la mediana de cada franja sube 1 a 2 aciertos. Se pueden dejar las escalas (el cambio es chico) hasta tener la primera semana de datos con la pista nueva, o repetir la calibración; es decisión de Opus.
- **Kotlin que hay que mantener a mano**: `TimeTrialDay.kt:73,74,76` repite `Track(version, stepEvery, maxLevel)`, y `TimeTrialDayTest.kt:59-82` compara por regex con `TimeTrial.cs` y los contratos (falla si difieren).

---

## B. Escaleras de los juegos de siempre

### B.1 Lo común a todos

- **Largo nuevo y migración (`ladderVersion`).** Cada juego alargado lleva dos largos: `levels` (el de la escalera, nuevo) y `pointsLevels` (el de la v1, que ancla los puntos). `Skill.Ladder(levels, pointsLevels, slope)`; **la pendiente se fija a mano** (hoy `slope = if (levels <= 7) 2 else 1`: pasar de 5 a 8 niveles la cambiaría de 2 a 1 y movería el corrimiento de los mayores: `Skill.kt:25`).
- **Puntos anclados.** `Skill.progress` (lo que va a `progress_log` y a los puntos de área y de juego) pasa a `min(1, (rating · levels + corrimiento) / pointsLevels)`, con el corrimiento de la edad en niveles. Un jugador que estaba en el tope de la v1 sigue en 1,0 y los niveles nuevos **no suman**. Aparte, `Skill.ladderProgress` (sin ese tope) alimenta `LevelLines.levelOf` y las cotas de modo (`Skill.bounds`), que sí necesitan la escalera completa.
- **Migración que conserva el nivel.** `Migration(18, 19)`: `UPDATE game_progress SET ddaRating = ddaRating * L_viejo / L_nuevo WHERE gameId = … AND ddaRating >= 0` (el mismo molde de las migraciones 12→13, 13→14 y 14→15 en `NeuroVidaDatabase.kt:80-100`, con su par Kotlin `translateOldRating`), más `version = 19` y `schemas/19.json`. Una persona en el nivel 4,6 de 5 queda en el 4,6 de 8; sus puntos no cambian. Las preferencias que guardan el rating normalizado (`star_measures`, el campo `rating` de cada `MeasurePoint`) se reescriben con la misma razón **una sola vez al arrancar** (marca en la preferencia `skill`, que ya va en el respaldo): una migración de Room no toca preferencias. `progress_log` no se toca (guarda avance de puntos ya calculado).
- **El rating sembrado de quien no tiene historial** (`Baseline.DEFAULT_LEVEL` = 0,45, `Baseline.kt:68`) se escala igual: `0,45 · L_viejo / L_nuevo`, o un jugador nuevo parte 1,4 niveles más arriba que hoy en Tinta (nivel 4,6 de 8 en vez de 3,25 de 5). `UnityGameLauncher.kt:244` y `AdaptiveDifficulty.StartRating(chosenLevel, maxLevel, …)` (Unity) también reparten por fracción y se revisan.
- **Respaldo.** No cambia: la base entera ya está en `backup_rules.xml` y la columna no es nueva. Una preferencia nueva exigiría sumarla a los dos XML o a `transient` (`BackupRulesTest`); aquí se usa una marca dentro de `skill`.
- **Los arrays de Kotlin que dependen del largo** (se salen de rango si `Skill.ladders` crece sin ampliarlos): `LevelLines` (Carga `CARGA`, Stroop `STROOP`, Punta fórmula, Acoplamiento `4 + i / 3`, Engranajes `ENGRANAJES_PIEZAS/CORREAS`, Disparate e Intrusa por pares `(l−1)/2`) y `Skill.ladders`. `LevelLinesTest` recorre todos los niveles: cada frase empieza con «Ya », ≤ 12 palabras, sin cifras internas, sin «rápido», «velocidad» ni «alarma».

### B.2 Primero: Carga exacta, Tinta o Palabra y En la punta de la lengua (unos 3 niveles cada uno)

**Carga exacta (5 → 8). Solo parámetros.** Tabla de hoy en `CalculoContract.cs:150-157` (`CargaLevel`: celdas, operaciones, celdas que se juntan, número máximo, probabilidad de «usa todas», camino más corto mínimo). El máximo de celdas es 5 (`MaxCells`, `CalculoGameController.cs:29`; con 6 la celda mediría ~48,7 dp contra las 56 que exigen las pruebas), así que lo nuevo es exigir MÁS con las mismas 5:

| Nivel | Celdas | Operaciones | Celdas que se juntan | Número máx. | «Usa todas» | Camino más corto mín. |
|---|---|---|---|---|---|---|
| 5 (hoy) | 4-5 | + − × ÷ | 4 | 10 | 0,35 | 2 |
| **6** | 5 | + − × ÷ | 4 | 12 | 0,60 | 3 |
| **7** | 5 | + − × ÷ | 5 | 12 | 0,85 | 3 |
| **8** | 5 | + − × ÷ | 5 | 14 | 1,00 | 4 |

Contenido: **ninguno** (el generador `CargaGenerator` produce las cargas; carga entre 8 y 99 y intermedios hasta 120). Hay que verificar que el sorteo no caiga en las 5 cargas de respaldo (`CalculoContractTests.cs:25`) con el camino de 4 pasos. Vive en Unity (`CalculoContract.cs`, `MaxLevel`, el `default:` del respaldo, 3 filas) y Kotlin (`LevelLines.CARGA` +3, `Skill.ladders`). Frases: §B.5.

**Tinta o Palabra (5 → 8). Solo parámetros, y es LA MISMA tabla del Contrarreloj** (`Levels`, `StroopContract.cs:149-156`; la pista usa `Spec(nivel)`), así que se diseña una vez para las dos partes: ver §A.3 (cambios 0,55 · 0,60 · 0,65; choques 0,85; llegada 210 · 180 · 150 ms). En el juego de siempre el paso del DDA es 0,20 (Precisión). Unity: `Levels` +3 y `MaxLevel`; el aviso de subida «¡Un paso más! / Más difícil» ya existe (`StroopGameController.cs:237`). Kotlin: `LevelLines.STROOP` +3, `Skill.ladders`, `TimeTrialDay.kt:73` (`Track(version = 3, stepEvery = 12, maxLevel = 8)`).

**En la punta de la lengua (5 → 8). Hace falta contenido.** Tabla de hoy (`PuntaContract.cs:48-55`; `punta_banco.json`, 533 palabras): relleno 2/2/3/3/4 (un menos en mayores), largo 4-6 / 4-8 / 4-9 / 4-10 / 6-10, rareza (zipf mediana) 4,7 → 3,2. El tope duro es la bandeja de 15 pozos (10 letras + 5 de relleno; `PuntaLayout.cs:29-33`): **no se pasa de 10 letras**. Con eso, lo nuevo:

| Nivel | Relleno (adulto/mayor) | Largo | Rareza (zipf mediana) |
|---|---|---|---|
| 5 (hoy) | 4/3 | 6-10 (mediana 8) | 3,2 |
| **6** | 5/4 | 8-10 | 2,9 |
| **7** | 5/4 | 9-10 | 2,6 |
| **8** | 5/4 | 9-10, solo las más raras | 2,3 |

Contenido a crear: **150 palabras como mínimo (50 por nivel nuevo, el piso de las pruebas) y 240 recomendadas** (el director evita las últimas 80, `RecentMax`), de 8 a 10 letras y baja frecuencia, cada una con **definición propia de ≤ 90 caracteres**. Hoy hay solo 48 palabras de 9 a 10 letras (conteo por largo: 64, 111, 126, 109, 75, 33, 15), así que no alcanzan. Vive en `tools/punta/datos/*.txt` («palabra | definición») y `tools/punta/banco.py` (`CORTES_NIVEL` de 4 a 7 cortes; necesita SPALEX, que está fuera de git); genera `punta_banco.json`. Unity: `ExtraByLevel` +3 filas (`PuntaContract.cs:48-51`, también el respaldo por nivel); Kotlin: la frase de Punta es una **fórmula** (`palabras de ${l+3} o ${l+4} letras`, «8 letras o más» desde el 5: `LevelLines.kt:55`), que hay que reemplazar por entradas explícitas. Pruebas: `PuntaContractTests` (≥ 50 por nivel), `test_banco.py`.

### B.3 Después: Acoplamiento, Engranajes, ¿Verdad o disparate? y La estrella intrusa; Satélites baja su paso

**Acoplamiento (12 → 17, los de la pista).** Parámetros: celdas 8 · 8 · 9 · 9 · 10; giro 180°; paso del ángulo 15° (podría bajar a 10° en el 16 y el 17: la pieza se hornea con cualquier ángulo y la única comprobación es `ángulo % paso == 0`, `DockingV2Tests.cs:41`); plazo 2,5 s. **Contenido: ninguno** (formas aleatorias). Unity: `Cells`, `MaxAngle`, `AngleStep` y `DeadlineMs` (de fórmula a tabla), `MaxLevel = 17`; el paso del DDA 0,3 sigue. Pruebas a cambiar: `DockingV2Tests.cs:48 TheRulesOfTodayDidNotChange` (`MaxLevel == 12` → 17, y fijar que los niveles 1 a 12 no cambiaron), `DockingContractTests.cs:45 Ladder_AndTrials`, y `TimeTrialTests.cs:35,50` (paso 17 a los 32 aciertos).

**Engranajes (12 → 15). Hay que tocar código.** Hoy está en el techo de TRES cosas: piezas (el generador solo arma 1, 2 o 3: `PickTargets`, `EngranajesContract.cs:292-305`, aunque hay 4 estaciones), correas (4 = `BeltPool`) y llaves (2: `minK` solo calcula hasta pares, `:485-493`). Niveles nuevos:

| Nivel | Piezas | Correas | Llaves | Qué falla | Pista | Tarjeta NUEVO |
|---|---|---|---|---|---|---|
| 12 (hoy) | 3 | 4 | 2 | dos | dos | — |
| **13** | 3 | 4 | 2 | cualquiera, sin «ya bien» | dos | — |
| **14** | **4** | 4 | 2 | dos | dos | «Cuatro piezas» |
| **15** | 4 | 4 | **3** | dos | tres | «Hasta tres cambios» |

El riesgo es el generador: hoy reintenta 24 × 600 veces y lanza `InvalidOperationException` si no encuentra (`:311-316`), y cada correa necesita un tramo recto de 3 engranajes, disjunto, en una cuadrícula de 5 × 6 con la columna del cohete. Antes de dar los niveles 14 y 15 por buenos: una prueba que genere 5.000 máquinas por nivel y mida cuántas fallan. Vive en Unity (`Stages` +3, `EtapaOf`, `IntroText`, `PickTargets`, `minK`; las pruebas con `1..12` literal: 13 bucles en `EngranajesContractTests.cs`) y Kotlin (`ENGRANAJES_PIEZAS/CORREAS` +3, `Engranajes.kt:25-26` si sale un grupo 6). Contenido: ninguno (procedural). Por ser código, es el más caro de los que no tienen contenido.

**¿Verdad o disparate? (12 → 14).** Ver §A.3 y A.4: un tipo nuevo, 2 niveles. Lo que toca, porque los 6 tipos están clavados en muchos arreglos: `SentenceType` y `_by[7,2]` de `SentenceBank.cs:32,60` → 8, `DisparateTally int[6]` (`DisparateDirector.cs:68-70`) → 7, `Signal[]` de 12 a 14 (`DisparateContract.cs:73`; **si no, `IndexOutOfRange` en el primer paso 13, también en el Contrarreloj**, `Director.cs:59`), el filtro `6≠7` del controlador (`DisparateGameController.cs:254`), `Reading.kt:18` (`TYPE_LABELS`) y `:46` (`take(6)`), `LevelLines` (de 6 entradas a 7), la tarjeta/aviso de subida del tipo 7 (`LevelNews`). Señal del tipo 7 para el modo con reloj: 4,5 s (hoy 5 s en el 11-12). Contenido: A.4.

**La estrella intrusa: no se alarga con parámetros.** El nivel solo cambia el TIPO de grupo (6 tipos, `TypeForLevel`) y el bono de «¿qué las une?»; ya está en el tipo 6. Los parámetros que no varían con el nivel (ventana del bono 4 s o 6 s, cinco estrellas, ritmo) no son la dificultad de la tarea. Dos caminos, ninguno barato: **(a) contenido**: un tipo 7 con reglas «más abstractas» exige reglas nuevas (`REGLAS_TXT` en `tools/intrusa/grupos.py`), palabras (`lexico.py`) y una figura por regla (hoy 77 reglas, 77 figuras), unas 25 a 40 reglas y 160 grupos nuevos; **(b) una estrella más** (6 en vez de 5), que no sé si cabe en el campo de juego: hay que mirarlo en el emulador. Recomiendo **dejarla para el final** y decidirla con lo que muestren los datos de las 3 personas.

**Satélites baja su paso.** Hoy 1,0 (`SatelliteContract.cs:62`): una ronda perfecta sube un nivel y el tope de bajada (1,0 por error) siempre manda, así que la razón 4:1 de Kaernbach no actúa (se escribió «como en el boceto aprobado»: `diseno-satelites.md:168`). Propuesta: el molde de Rastro: **0,25 en adultos, 0,17 en mayores, 0,22 en menores de 18** (`SatelliteContract.StepUp(age)`), porque con 0,25 y 0,17 el tope no se activa (`StepUp_NeverHitsTheDropCap_SoTheTargetIsReached`, `RastroLadderTests.cs:142-153`). Efecto medido (95 % de rondas perfectas; la partida tiene 8): paso 1,0 → toca el nivel 12 en la partida 1; 0,50 → en la 3; 0,40 → en la 4; 0,30 → en la 5 (65+ en la 7); **0,25 (adultos) → en la 7, y ya empieza arriba desde la 9; 0,17 (mayores) → en la 13**. Con 0,25 y 0,17 la regla es la de Rastro, pero es lenta para los mayores (13 partidas contra 6 de Rastro, que juega 14 rondas por partida y no 8): si Opus prefiere una escalera más corta, 0,30 en adultos (partida 5) tiene el tope de bajada activo (1,2 contra 1,0) y un equilibrio de ~77 % en vez de 80 %, algo que Rastro evitó a propósito. No hay migración ni cambio de puntos (el rating guardado queda igual); lo que cambia es la velocidad con la que se mueve. Tests: `SatelliteContractTests.cs:302` (`TheCommonEngine_APerfectRoundClimbsALevel…`, la única que pinta 1,0) se reescribe y se agrega la copia de la de Rastro; `docs/DDA-comun.md` suma Satélites a su tabla. Nada en Kotlin conoce el paso.

### B.4 Dónde se ve hoy el número de nivel (para saber dónde se verán los niveles nuevos)

No existe «nivel N de M» al jugar: el HUD de Unity no lo dibuja (`GameHud.cs:200-207` solo hace brillar el chip; `HudRulesTests.cs:216` prohíbe «nivel»); `AppStrings.levelPrefix` («Nivel») no tiene ningún uso. Lo que la persona ve:

| Dónde | Qué sale | Qué cambia con niveles nuevos |
|---|---|---|
| **Avance › página del área › «Tu nivel: …»** (`AvanceAreaPage.kt:249` ← `LevelLines.inRow`) | la FRASE «ya giras piezas de 6 bloques…», no el número; sale del avance de la escalera | es **la única superficie** de los niveles nuevos: hacen falta sus frases (3 + 5 + 3 + 2 + …) y `levelOf` debe leer el `ladderProgress`, no los puntos |
| **Contrarreloj**: «Llegaste al paso N de M de la pista.» y «Tu récord es del …; llegaste al paso N de M de la pista» (`TimeTrialStats.kt:90`, `TimeTrialMarks.kt:153`, `ContrarrelojPage.kt:588,634`, `TimeTrialResultScreen.kt:89`) | N de M de la pista | M pasa a 8, 14 y 17; no se pierde el récord viejo (queda como pista 2) |
| **Dentro del juego** (Unity) | «¡Un paso más!» con subtítulo en Tinta, Punta, Meteoros, Disparate e Intrusa (`StroopGameController.cs:233-237`, `PuntaGameController.cs:336`, `DisparateGameController.cs:628`, `IntrusaGameController.cs:302`); Carga solo el brillo del chip; Acoplamiento, Engranajes y Satélites sin aviso | los avisos ya existen; hay que escribir el subtítulo de los niveles que traen una novedad (Disparate tipo 7; Engranajes 4 piezas y 3 llaves, con su tarjeta NUEVO) |
| Piloto, detalle del final | «Tu nivel de señales: 6 de 9» (`Piloto.kt:14,42`) | no se toca (Piloto no se alarga) |
| Ficha, Avance (cuadros), Hoy, Tú, carga | «Etapa Hábil · 180 de 250 puntos», «470 de 1000 puntos», «Etapa general» | **no cambian**: salen de los puntos (anclados), y «Etapa» sale con la T230 |

Como la etapa y los puntos no se mueven, **el único lugar donde alguien nota que la escalera creció es la frase de Avance** y, en Tinta, Disparate y Acoplamiento, el «paso N de M».

### B.5 Muestra de contenido en español (10, para revisar antes de escribir el resto)

**¿Verdad o disparate?, tipo 7 nuevo** («dos afirmaciones; es verdad solo si las dos lo son»):
1. «El hielo flota y el agua hierve a 100 grados» (verdad)
2. «Una semana tiene siete días y un año, doce meses» (verdad)
3. «El Sol es una estrella y la Luna también» (disparate)
4. «Chile está en América y Lima es su capital» (disparate)

**En la punta de la lengua, palabras de 9 a 10 letras con definición propia** (de muestra; el banco real exige baja frecuencia con SPALEX):
5. herborista | Persona que vende plantas medicinales y hierbas secas.
6. trapecista | Artista que salta y se columpia en el aire, colgada de una barra.
7. cartógrafo | Persona que dibuja mapas.

**Frases de nivel de Avance** («Tu nivel: ya …», ≤ 12 palabras, sin «rápido» ni «velocidad»):
8. Carga exacta, nivel 8: «Ya llegas a la carga con las cuatro operaciones y los cinco números»
9. Acoplamiento, nivel 17: «Ya giras piezas de 10 bloques, en giros finos»
10. Tinta o Palabra, nivel 8: «Ya alternas tinta y palabra casi en cada una»

---

## D. Hecho: la base común y Carga exacta de 5 a 8 (Tarea 233, 4-nov-2026)

Lámina `docs/previews/tarea-233.png` (`tools/previews/tarea_233.py`; capturas reales de la pantalla de Avance antes y ahora). Es la fila 0 de §C.1 más la fila 2 (Carga), juntas para que la base se probara con un caso real. Cómo quedó y dónde se apartó de lo escrito en §B.1:

- **`Ladder`** sigue siendo la clase de `data/Skill.kt` (no se anidó en `Skill`): `Ladder(levels, ownTarget, slope, pointsLevels = levels, version = 1)`. Carga exacta es `Ladder(8, slope = 2f, pointsLevels = 5, version = 2)`; `ratingScale` = `pointsLevels / levels` (5/8; 1 en los demás). La pendiente se fija a mano (con 8 niveles la fórmula daría 1).
- **`Skill.progress`** (puntos, `progress_log`, `Baseline`, Hoy, Tú) = `(rating × levels/pointsLevels + corrimiento/pointsLevels)` con tope 1: en los juegos sin alargar es la misma cuenta de siempre, bit a bit; en Carga, quien pasa los 5 niveles de antes se queda en 1,0 y los niveles 6 a 8 no suman. **`Skill.ladderProgress`** (sin ese ancla) alimenta `LevelLines.levelOf` (`bounds` ya trabaja con el rating sobre la escalera entera: no cambia); el ViewModel suma el flujo `gameLadderProgress` y la página de un área de Avance recibe `ladderProgress` (sin él, el nivel de un rating de 0,7 se leería como el 8 por el tope de los puntos: `SkillTest` lo fija).
- **Migración.** `Migration(18, 19)`: `UPDATE game_progress SET ddaRating = ddaRating * 5.0 / 8.0 WHERE gameId = 'calculo' AND ddaRating >= 0`; `Carga.translateOldRating` es su par Kotlin; `schemas/19.json` es el del 18 con otro número (no hay columna nueva). Las medidas de `star_measures` (el campo `rating`) las traduce la app UNA vez al arrancar (`NeuroVidaRepository.migrateLadderVersionsIfNeeded`), con la marca de versión por juego dentro de la preferencia `skill` (`SkillState.ladderMarks`, líneas `l|calculo|2`; ninguna preferencia nueva: `BackupRulesTest` sigue en verde). La marca se escribe PRIMERO y con `commit()`: si el proceso muriera entre las dos escrituras, a lo peor las medidas viejas quedarían sin traducir, nunca dos veces; un candado evita que dos llamadas a la vez traduzcan dos veces; «Borrar datos» deja las marcas de ahora (si no, lo que se jugara después se traduciría de más). Una instalación nueva solo recibe sus marcas.
- **Lo que no estaba escrito y se hizo:** (1) `Skill.nextRating` mide el tope de bajada en PUNTOS (`MAX_DROP × ratingScale`: en Carga 0,03125 de rating = 0,05 de avance); (2) sembrar escala por juego (`seedRatings`, `applyPriorIfNeeded` y `Baseline.defaultLevel`: 0,45 × 5/8 en Carga, así quien empieza de cero parte donde partía), y `currentLevel` (1 a 5) se deriva de la escala de los puntos; (3) en Unity, `AdaptiveDifficulty.StartRating(config, maxLevel, spreadLevels)` reparte el nivel elegido (1 a 5) de quien no tiene rating sobre los 5 niveles de antes (Carga pasa `V1MaxLevel`); (4) el sorteo con «usa todas» arma al menos las celdas del nivel (en el 6, las 5; en los niveles 1 a 5 no cambia); (5) `LevelLines.CARGA` +3 frases (Tarea 233b, de Opus): «Ya llegas a la carga con cinco celdas y cuentas más largas» (6), «… con cuentas que juntan las cinco celdas» (7) y «… con las cuentas más largas del juego» (8).
- **UnityGameLauncher.kt** pasa el rating guardado tal cual (ya viene en la escalera de ahora) y Unity lo lee como `1 + rating × MaxLevel`: no hace falta tocarlo.
- **Pruebas:** `SkillTest` (anclado, escalera, tope de bajada, modos, marcas), `LevelLinesTest` (los 8 niveles), `AreaPointsTest` (Carga anclada), `BaselineTest`, `MigrationTest` (19: 4,6 de 5 queda en 4,6 de 8 sin perder puntos ni nivel), `LadderMigrationTest` (la traducción única), `CalculoContractTests` (niveles 6 a 8, respaldos, `StartRating`); en el smoke, `CalculoNivel6` a `CalculoNivel8` (rating forzado con `EditorPlaytestBootstrap.DdaRatingOverride`; para capturas sueltas, `NUBI_EDITOR_RATING=0.6875 bash tools/verificar-todo.sh --capturas Calculo`, con (nivel − 0,5) / 8). Sin cambios en `FirstGameStartStageTests` ni `FirstGameTableTests` (no incluyen Carga).
- **MOLDE para el próximo juego que se alargue** (Tinta, Punta, Acoplamiento, Engranajes, Disparate): (1) en Unity, su tabla y `MaxLevel` (más su `default:` de respaldo si lo tiene) y un `V1MaxLevel` si usa `StartRating`; (2) `Ladder(levelsNuevo, slope = <la de antes>, pointsLevels = levelsViejo, version = 2)` en `Skill.ladders` (con `version = 3` solo si ya tenía una ampliación; esta base supone una sola); (3) su `Migration(19, 20)` (`ddaRating * L_viejo / L_nuevo`) en `NeuroVidaDatabase.MIGRATIONS` y `version = 20`, su `translateOldRating` y su prueba en `MigrationTest`; (4) las frases de `LevelLines` (los arrays que dependen del largo) y su `LevelLinesTest`; (5) sus pruebas en `SkillTest`/`AreaPointsTest`. La traducción de `star_measures` y las marcas salen solas de `Skill.ladders`.

## C. Orden de tareas, esfuerzo y riesgos

### C.1 Orden propuesto (esfuerzo en días de Sonnet; la suma de lo de contenido depende de quién lo escriba)

| # | Tarea | Esfuerzo | Depende de |
|---|---|---|---|
| 0 | **Base común**: `Ladder.pointsLevels` y `slope` fija, `Skill.progress` anclado y `Skill.ladderProgress`, `Migration(18,19)` + `translateOldRating` por juego + reescritura única de `star_measures`, `Baseline.DEFAULT_LEVEL` escalado, `LevelLines.levelOf` leyendo el avance de escalera; pruebas (`SkillTest`, `LevelLinesTest`, `MigrationTest` 19, `AreaPointsTest` con un juego anclado) | 3 a 4 | nada |
| 1 | **Satélites: bajar el paso** | 1 | nada (se puede hacer primero, sin migración) |
| 2 | **Carga exacta +3** (Unity `Levels` +3, verificar el sorteo; Kotlin frases) | 1,5 | 0 |
| 3 | **Tinta +3 y su pista** (nivel 6 a 8, `Track` v3 «cada 12», `TimeTrialDay.kt`, referencia nueva) | 2 | 0 |
| 4 | **Punta +3** (contenido de 150 a 240 palabras con definición, `banco.py`, `ExtraByLevel`, frases) | 3 a 4 (+ contenido) | 0 |
| 5 | **Acoplamiento +5 y su pista** (celdas, plazo a tabla, `Track` v3, referencia, pruebas) | 2 | 0 |
| 6 | **Engranajes +3** (código: 4 piezas, 3 llaves; 5.000 máquinas por nivel) | 4 | 0 |
| 7 | **Disparate +2 y su pista** (tipo 7: ~400 frases + generador, arreglos y `Signal[]`, Kotlin, `Track` v3 «cada 4», referencia) | 4 a 5 (+ contenido) | 0, revisión de la muestra |
| 8 | Intrusa: investigación (¿6 estrellas?, ¿tipo 7?) | 1 | datos de las 3 personas |
| 9 | Recalibrar `ReferenceScales` con la primera semana de la pista nueva | 0,5 | 3, 5 y 7 en uso |

Orden de entrega sugerido: **0 y 1** primero (1 no depende de nada y es lo más barato), **2 y 3** (solo parámetros), **5**, y después **4 y 7** (los de contenido, mientras Opus revisa la muestra de §B.5), **6** al final. La referencia estimada se regenera una vez, cuando 3, 5 y 7 estén (`--referencia`: ~1 minuto), y esas tres versiones de pista salen juntas.

### C.2 Riesgos

- **Pruebas que se rompen a propósito** (cada una fija el largo viejo): Unity — `TimeTrialTests.cs:35,50` (largo y paso de cada pista), `StroopContractTests.cs:59,71` (tabla y `MaxLevel == 5`), `DockingV2Tests.cs:48`, `DisparateContractTests.cs:55` (tipos por nivel), `SatelliteContractTests.cs:302` (paso), `FirstGameStartStageTests.cs:36-52` (Engranajes y Satélites parten en la etapa 6 con el rating 0,45; con otro largo parten en otra), `FirstGameTableTests.cs`, `HeadlessPlaymodeSmokeTest` y los 206 arranques (los juegos con nuevos niveles, la tarjeta NUEVO de Engranajes). Kotlin — `LevelLinesTest` (:19-62, 64-117 textos exactos), `SkillTest`, `CargaTest.kt:67`, `EngranajesTest.kt:80`, `TimeTrialDayTest.kt:59-88`, `TimeTrialMarksTest`, `ContrarrelojPageTest.kt:325,357` («paso 4 de 5»), `TimeTrialStatsTest`, `EstimatedReferenceTest.kt:38-43`, `MigrationTest` (nueva 18→19).
- **Láminas**: las que muestran «paso N de 12» o la frase «Tu nivel» (`docs/previews/tarea-215.png`, `tarea-219`, las de Avance, las de la ficha) quedan en su versión vieja; se regrababan solo las que se tocan.
- **Respaldo**: sin cambio de reglas (una marca dentro de `skill`). Si alguien restaura una copia de ANTES de la migración en un teléfono con la versión nueva, Room aplica la migración 18→19 al abrir la base y la marca de `star_measures` evita reescribirla dos veces.
- **Room**: la migración es de datos (sin columna nueva); en release, subir `version` sin `Migration` hace fallar la app (el fallback destructivo es solo de depuración: `NeuroVidaDatabase.kt:48-57,134-136`). `MigrationTest` exige `schemas/19.json`.
- **Contrarreloj**: la versión de pista nueva **reinicia el récord** (el viejo queda como «pista 2») y la referencia; mejor sacar Tinta, Disparate y Acoplamiento juntos para no repetir el aviso. Las 3 personas que se aburren perderían su récord: decirlo en la nota de la versión.
- **Calibración**: las escalas se heredan; si la mediana de las 5 personas de Ricardo se mueve más de 2 puntos, recalibrar.
- **Interferencia de Tinta**: ya es rara con 16 palabras (2,4 coincidentes esperadas contra el mínimo de 3); no la empeora el plan (choques 0,85), pero conviene que el final de Tinta no prometa la medida.
- **Lo que esta propuesta no resuelve**: que las líneas de la liga entre el 80 y el 90 % queden más separadas (A.2) ni que un jugador del Contrarreloj de Acoplamiento «se aburra» si lo que cuenta es la velocidad: eso necesita medir con personas reales.
