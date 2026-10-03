# Rastro de luz — Secuencia Lumínica rehecha desde cero (aprobado por Ricardo el 3-oct)

Reemplaza a Secuencia Lumínica (id interno `secuencia`, que SE MANTIENE para no perder avance, marcas ni historial).
Juego estrella de **Memoria** y juego de Memoria del inicio nuevo (`docs/diseno-inicio.md`).
Boceto jugable aprobado: `docs/previews/rastro-de-luz-boceto.html` (https://claude.ai/artifact/JDeHESmN6KmzPF4rq7nXGc).
Ricardo (3-oct): «me parece bien la mecánica y los cambios dentro de una misma partida»; le pareció «un poco difícil,
pero es parte de la experiencia». En el boceto no hay DDA: cada acierto suma una luz. En el juego, la dificultad la lleva
el motor común y arranca más suave (ver §4).

## 1. La idea en una línea

Una chispa de luz vuela entre 9 luceros de cristal; cada lucero suena con su nota, así que el camino se ve y se oye como
una melodía. Lo repites deslizando el dedo (o tocando). Al subir de nivel, Nubi desbloquea giros: **al revés**, **el cielo
gira** y **en marcha**.

## 2. Ciencia (PubMed, 3-oct)

- Amplitud visoespacial tipo Corsi (orden de lugares): Kessels y otros, 2000, *Appl Neuropsychol* 7:252-8,
  [doi:10.1207/S15324826AN0704_8](https://doi.org/10.1207/S15324826AN0704_8).
- La forma del camino cambia lo que se recuerda (cruces, largo y ángulos lo hacen más difícil): Parmentier, Elford y
  Maybery, 2005, *J Exp Psychol Learn Mem Cogn* 31:412-27, [doi:10.1037/0278-7393.31.3.412](https://doi.org/10.1037/0278-7393.31.3.412).
  Es la palanca de dificultad «curvas y cruces».
- «Memoria en marcha» (running span): mide capacidad de memoria de trabajo y se relaciona con el razonamiento: Broadway y
  Engle, 2010, *Behav Res Methods* 42:563-70, [doi:10.3758/BRM.42.2.563](https://doi.org/10.3758/BRM.42.2.563).
- El límite real de la memoria de corto plazo ronda los 4 elementos: Cowan, 2001, *Behav Brain Sci* 24:87-114,
  [doi:10.1017/s0140525x01003922](https://doi.org/10.1017/s0140525x01003922). Por eso las escaleras no pasan de 8.
- «El cielo gira» (recordar el orden sobre objetos que se movieron) es una variante propia; se presenta como juego, sin
  afirmar que mida algo validado. Ojo de originalidad: Lumosity tiene un juego donde una cuadrícula con un patrón gira 90°
  («Rotation Matrix»). El nuestro es distinto (orden de un camino, no un patrón; ángulos libres; luceros que suenan), pero
  no decir que «ninguna app gira»; nombre, arte y sonido propios.
- Sin patentes conocidas sobre amplitud de secuencias con luz y sonido (el juego «Simon» de 1978: patente vencida y nombre
  ajeno, no usar). Lista del abogado: revisar «Rotation Matrix» de Lumosity antes de publicar.

## 3. Cómo se juega

- **Escena** (`GameWorld` nuevo, p. ej. `CieloDeCristal`): fondo C con nebulosas lila/celeste/coral tenues, ~240 estrellas
  fijas y ~30 que titilan; un disco punteado muy tenue marca el tablero. Cada modo tiñe suavemente el cielo (radial del
  centro, alfa ~0,12): rastro celeste `76,201,240` · al revés violeta `184,164,255` · gira verde agua `120,240,200` · en marcha
  coral `255,159,122`.
- **Luceros**: 9 esferas de cristal (degradé radial blanco → color propio → translúcido, borde blanco fino, brillo
  especular arriba a la izquierda, halo del color) en anillo irregular + centro, como el boceto. Redondos, SIN puntas ni
  símbolos. Diámetro ≥ 56 dp (mayores 64), zona de toque ≥ 72 dp, separación ≥ 12 dp. Respiración muy suave en reposo.
  Colores: `#7FD8FF #B8A4FF #FFC94A #8EE07A #FF9F7A #9FF5D6 #F7A8E0 #FFE27A #A9C7FF`; notas: pentatónica
  `392 440 523,25 587,33 659,25 783,99 880 1046,5 1174,66` Hz (una por lucero, siempre la misma).
- **Muestra**: la chispa (halo del color del modo + núcleo blanco) vuela de lucero en lucero por curvas (Bézier
  cuadrática, alternando el lado de la curva), deja un rastro que se angosta y se apaga en ~1,5 s y suelta partículas. Al
  llegar a cada lucero: florece (anillo que se expande 650 ms), suena su nota y salen chispas de su color.
- **Respuesta**: deslizar el dedo dibuja una cinta de luz crema que se apaga en 0,7 s; al pasar a ≤ 34 dp de un lucero
  se engancha (no cuenta el mismo lucero dos veces seguidas). También vale tocar uno por uno. Cada enganche correcto:
  florece, suena su nota y queda un tramo fijo del color del modo entre los luceros acertados.
- **Acierto de ronda**: el camino completo brilla más, acorde de 4 notas, y las chispas vuelan al contador «luces
  recordadas» (arriba a la derecha). Vibración corta.
- **Error**: el lucero tocado lleva un aro coral con una ✗ dibujada y el correcto un aro sol que late. Texto: «Esa no era:
  la correcta tiene el aro amarillo». Sonido suave y vibración doble. Se pierde una vida (3) y sigue otra ronda.
- **Modos (familias)**:
  - **El rastro** (→): repetir en el mismo orden.
  - **Al revés** (⟲): de la última a la primera. Rótulo «Al revés: de la última a la primera».
  - **El cielo gira** (↻): tras la muestra, el tablero gira (sonido de aire) y se repite sobre los mismos luceros.
  - **En marcha** (≫): la chispa recorre más luceros de los que se piden (sin aviso de cuántos); al final se piden «las
    últimas N».
  Cada modo: ícono dibujado (no depender de glifos de la fuente), nombre y tinte. Nunca solo color.
- **Desbloqueo**: la PRIMERA VEZ (de por vida, guardado) que se llega a un modo, pantalla «¡NUEVO!» con el ícono grande, el
  nombre y una línea («De la última luz a la primera» / «El cielo da un giro: sigue a tus luceros» / «Repite solo las
  últimas luces»), dos campanas, 2,3 s o toque para seguir.
- **HUD**: píldora con ícono + modo, píldora «N luces» (o «Últimas N»), 3 vidas (puntos crema), contador grande «luces
  recordadas». Textos ≥ 14 sp (18 en las instrucciones).
- **Partida**: Reto 90 s (con el reloj común) o Precisión 14 rondas; en ambos, 3 vidas. Nunca más de 2 rondas seguidas
  del mismo modo nuevo; el rastro simple aparece siempre en la mezcla.

## 4. Dificultad (motor común, 16 niveles; misma escala que la Secuencia anterior)

Ensayo = ronda completa (acierto si se repite entero bien). Sin tiempo de reacción (`useReaction:false`, como hoy).
Cada nivel define: largo por familia, velocidad de la chispa (px/s), espera en cada lucero (ms), giro (grados), cuántos
de más en «en marcha» y si se permiten cruces. Propuesta (Sonnet puede afinar manteniendo el espíritu):

| Nivel | Familias disponibles | Largo (rastro / revés / gira / marcha: últimas) | Chispa | Giro | Cruces |
|---|---|---|---|---|---|
| 1 | rastro | 2 | 200 px/s, espera 320 ms | – | no |
| 2 | rastro | 3 | 220 / 300 | – | no |
| 3 | rastro | 3 | 260 / 260 | – | no |
| 4 | rastro | 4 | 260 / 260 | – | no |
| 5 | + al revés | 4 / 3 | 280 / 240 | – | no |
| 6 | | 4 / 4 | 280 / 240 | – | no |
| 7 | + gira | 5 / 4 / 3 | 300 / 220 | 60-90° | no |
| 8 | | 5 / 4 / 4 | 300 / 220 | 60-120° | no |
| 9 | + en marcha | 5 / 4 / 4 / 2 (de 3-5) | 300 / 220 | 90-150° | no |
| 10 | | 5 / 5 / 4 / 3 (de 4-6) | 320 / 200 | 90-150° | 1 |
| 11 | | 6 / 5 / 5 / 3 | 320 / 200 | 90-180° | 1 |
| 12 | | 6 / 5 / 5 / 3 (de 5-7) | 340 / 190 | 90-180° | 2 |
| 13 | | 6 / 6 / 5 / 4 | 340 / 190 | libre | 2 |
| 14 | | 7 / 6 / 6 / 4 | 360 / 180 | libre | 2 |
| 15 | | 7 / 6 / 6 / 4 (de 6-8) | 380 / 170 | libre | sí |
| 16 | | 8 / 7 / 6 / 5 | 400 / 160 | libre | sí |

- Mayores (`Senior`): objetivo 0,85 del motor común con un `stepUp` que no choque con el tope de bajada por error
  (`MaxDropPerError`; con la escala de 16 niveles el de Secuencia actual converge a ~0,82: usar ≈ 0,17 para Senior), chispa
  −15 % y luceros de 64 dp.
- «Primera vez» y evaluación inicial: arrancan en nivel 2 (adultos) o 1 (mayores), solo el rastro simple.

## 5. Tutorial dentro del juego (primer uso del formato de `docs/diseno-inicio.md`)

- Sale si la persona nunca jugó este juego (la app lo sabe por el historial y manda `show_tutorial=true`).
- Tarjeta de entrada: Nubi maestra + «Mira el camino de la chispa y repítelo con el dedo».
- Ronda guiada de 2 luces: rótulo «Práctica: no cuenta», «Saltar tutorial» visible, el siguiente lucero correcto con un aro
  sol punteado; si hay error: «Casi: esa no era. Mira otra vez el camino» y se repite. Al acertar: «¡Así se juega! Ahora
  sin ayuda» y empieza la partida. No suma puntos ni ensayos al DDA ni se guarda.
- Construirlo como pieza común reutilizable (`Games/Shared/`: tarjeta de Nubi maestra, rótulo de práctica, botón saltar),
  porque los otros 22 juegos la usarán.

## 6. Medida al final (juego estrella)

1. **Tu rastro** (cifra grande): las luces más largas que repetiste bien en el rastro simple.
2. **Por modo**: al revés / con giro / en marcha, cada uno solo si tuvo ≥ 3 rondas; aciertos por modo; el de menor tasa
   marcado con TEXTO «el que más te costó».
3. Lectura en palabras (Nubi científica): qué es la memoria de trabajo y una línea por modo («Al revés pide reordenar en la
   cabeza: suele costar un poco más»). Sin percentiles. Nota común al pie.
Agregar la medida a `docs/medidas-juegos-estrella.md` y a `StarMeasures`.

## 7. Sonido y efectos

- Pentatónica por lucero (campana cristalina: fundamental + parcial 2,76× + 5,4×), acorde de acierto, error grave suave,
  soplo de aire al girar, dos campanas en «¡NUEVO!». Nada arcade. Respeta sonido apagado.
- Vibración solo en acierto de ronda, error y desbloqueo.
- «Quitar animaciones»: el giro SE QUEDA (es la tarea) pero lineal; sin partículas, sin respiración, sin vuelo de chispas
  al contador; el rastro y la cinta como tramos rectos con fundido de 300 ms; misma duración de las esperas
  (`Motion.Hold`).

## Ficha técnica (hecho el 3-oct)

**Dónde está.** Carpeta `unity/NeuroVidaCore/Assets/Scripts/Games/Secuencia/` (la carpeta y el id `secuencia` se mantienen para no perder avance, marcas
ni historial). Nombre visible «Rastro de luz» (GameRegistry, ícono dibujado, Debug, evaluación inicial).

| Archivo | Qué tiene |
|---|---|
| `RastroContract.cs` | Familias (`RastroMode`, `RastroModes`: nombres y textos), la escalera (`RastroLevel`, `RastroLadder`: 16 niveles), constantes, `StepUp` por edad, arranque, puntaje y `BuildMetrics` (telemetría). Sin escena. |
| `RastroBoard.cs` | Los 9 luceros (posiciones de 360 × 640 dp, colores, notas), rotación del cielo, `OrbAt`, tramos despejados, cruces y la curva de la chispa; `RastroPaths` genera los caminos. Sin escena. |
| `RastroSession.cs` | `RastroDirector` (qué modo sigue), `RastroTally`, `RastroRound` y `RastroSession` (motor común + vidas + cuándo termina + ronda guiada). Sin escena. |
| `RastroGameController.cs` | Hereda `GameControllerBase`: dibuja (luceros, chispa, rastro, cinta, marcador, «¡NUEVO!»), lee el dedo y arma la ronda guiada (`GuidedRound`). |
| `RastroSprites.cs`, `RastroSounds.cs`, `LightMesh.cs` | Arte horneado (lucero de cristal, disco punteado, aro punteado, ✗, 4 íconos de modo dibujados), sonido sintetizado, y la malla de UI de los tramos de luz. |
| `Tests/` | `RastroLadderTests`, `RastroPathTests`, `RastroSessionTests` (+ las de edad que ya había). |

Borrado: `SequenceGameController`, `SequenceLevelConfig`, `DistractorDrone` y `TileGlyphSprite` (las fichas con estrella, medialuna, etc.). Los sprites
compartidos que vivían en esa carpeta (`RoundedRectSprite`, `RingSprite`, `RadialGlowSprite`, `HeartSprite`, `HarmonicTone`, `TileSprites`,
`TilePalette`) se quedan donde están: los usan los demás juegos.

**Mundo y arte.** `GameWorld.CieloDeCristal` (en `WorldBackdrop.cs`): fondo C, 240 estrellas fijas (30 titilan), nebulosas lila y celeste; la coral y el
tinte de cada modo los pone el juego. El lucero de cristal es una textura de 192 px por color (9), no 9 figuras: `GradientT` reproduce el degradé de dos
círculos del boceto. Rastro de la chispa, cinta del dedo y tramos acertados son **tres mallas de UI** (`LightMesh`: un dibujo por capa, no cien `Image`);
partículas, florecimientos y chispas al contador, pools fijos (70, 10 y 12). `Application.targetFrameRate = 60`.

**Decisiones y desviaciones del boceto (todas pequeñas y probadas).**
- Cada lucero aparece **a lo más una vez por camino** (así nunca se repite el mismo lucero seguido) y cada tramo queda **despejado**: ningún tramo pasa a
  menos de 42 dp del centro de un lucero que no es suyo (si no, al deslizar el dedo en línea recta se «engancharía» el del medio y contaría como error;
  quedan ≥ 3 tramos posibles por lucero, y el anillo y el centro siempre se pueden recorrer). Los **cruces** entre tramos no vecinos no pasan del tope
  del nivel (0 hasta el 9, 1 en el 10-11, 2 en el 12-14, sin tope en el 15-16) y el generador busca llegar a 1, 2 y 3.
- Radio de enganche **36 dp** (38 en mayores), no 34: es la mitad de la zona de toque mínima de 72 dp; la separación entre luceros (79 dp) la respeta.
- Marcador propio (el del boceto: modo + «N luces», 3 vidas, contador «luces recordadas»), no `GameHud`. Los textos del boceto, a 15-20 sp.
- Rangos de «en marcha» que la tabla del §4 deja sin decir: nivel 11 de 4-6, 13 y 14 de 5-7, 16 de 7-9. «Libre» = giro de 45° a 180° para cualquier lado.
  En marcha espera 0,75 × (como el boceto: 160 ms contra 220).
- Mayores: chispa −15 %, luceros de 64 dp, enganche de 38 dp, y `stepUp` 0,17 (menores de 18: 0,22; adultos 0,25) para que el objetivo se cumpla sin chocar con
  `MaxDropPerError` (ver `docs/DDA-comun.md` §6: mayores ≈ 0,82 → ≈ 0,84 medido).
- **Primera vez** (sin rating guardado) y evaluación inicial: nivel 2 (1 en mayores). En la evaluación, solo el rastro simple, hasta 2 errores o ~75 s.
- **«¡NUEVO!»**: la primera vez de por vida que el director elige un modo (en la ronda siguiente a que el nivel lo abre). Se guarda en `PlayerPrefs` de Unity
  (`rastro_modes`, bits 1/2/4/8) y la pantalla (2,3 s o un toque) no le gasta tiempo al Reto. Nunca más de 2 rondas seguidas de un modo nuevo: el director
  fuerza el rastro simple tras 2 rondas de modos nuevos, así que el rastro simple siempre está en la mezcla.
- Una ronda sin terminar cuando se acaba el Reto no cuenta.

**Telemetría** (`SequenceTelemetry`): conserva `end_rating`, `peak_level`, `mode_trials`, `mode_hits`, y agrega por familia (orden: rastro, al revés, gira,
marcha): `ras_best_len[4]` (mejor largo repetido bien), `ras_rounds[4]`, `ras_hits[4]`, `ras_modes_seen` (bits) y `ras_new_modes` (bits desbloqueados en la
partida). La ronda guiada no entra. `average_response_time_ms` = ms por luz en las rondas acertadas.

**En la app.** `NativeReceiver.parseSequenceResult` lee los `ras_*` (solo con las 4 familias; una versión vieja de Unity no los manda) → `GamePlayResult.ras*`.
`data/Trail.kt` es la lectura (tu rastro, modos con ≥ 3 rondas, «el que más te costó» con texto, una línea por modo y un truco para ese, `unlockedLine`);
`StarMeasures` tiene la medida `trail` («Tu rastro», más es mejor, depende del nivel) y `GameResultScreen` la muestra sin recuadros y con cada dato una vez.
Captura real: `docs/previews/rastro-final-real.png`. La medida y sus límites están en `docs/medidas-juegos-estrella.md`.

**Tutorial guiado (pieza común, `Games/Shared/`).** `GuidedTutorial` (tarjeta de entrada con Nubi maestra —`NubiTeacherSprite`, dibujada con el pincel SDF
siguiendo `pose_pizarra` de `tools/previews/nubi_guia.py`—, rótulo «Práctica: no cuenta», botón «Saltar tutorial», `Say` para lo que dice Nubi) y el gancho
en `GameControllerBase`: `BuildTutorial`, `RunTutorialIfNeeded` y `GuidedRound`. **Cómo lo usa otro juego:** (1) llamar a `BuildTutorial(safe, topU)` al final de
`BuildUi`; (2) reemplazar `GuidedRound(GuidedTutorial t)` con su primera ronda corta (`t.BeginPractice()`, jugarla SIN pasar por el DDA, el puntaje ni el guardado,
`t.Say(...)`, parar si `t.Skipped`, `t.EndPractice()`); (3) en el `GameLoop`, antes de la cuenta regresiva, `yield return StartCoroutine(RunTutorialIfNeeded(titulo, meta))`;
(4) en el controlador, pasar cada toque a `t.TrySkip(pos)`; (5) sumar el id a `UnityGameLauncher.TUTORIAL_GAMES` (la app manda `show_tutorial=true` si la
persona no tiene partidas de ese juego en el historial). Para probar el flujo sin borrar nada: botón `[Debug] Probar Rastro de luz con tutorial`.

**Pruebas.** EditMode (`Games/Secuencia/Tests`): escalera (16 niveles válidos, nunca más fácil, familias y largos del diseño, cruces por nivel), motor
(objetivos, `stepUp` sin tope, simulación adulto y mayores, arranque, evaluación, techo y piso), caminos (largo, sin repetir, tramos despejados, cruces ≤ tope,
anillo de respaldo), «en marcha» (exactamente las últimas N), al revés, giro (identidad de los luceros), director (≤ 2 seguidas de un modo nuevo, rastro simple
siempre, «¡NUEVO!» una sola vez), ronda guiada sin rastro en el DDA, vidas y final de cada modo, telemetría. Kotlin: `TrailTest`, `TrailTelemetryTest`.
Smoke: `Run` (la partida) y `Tutorial` (con la tarjeta de Nubi); `verificar-todo.sh --juegos Run,Tutorial --filtro-tests "Secuencia|Rastro|Tutorial"`.
Marca de verificación: `estilo 3-oct · rastro`.

**Retoques del 3-oct (tarea 20b).** (1) Los textos nunca se cortan: el rótulo de abajo y su línea, «¡NUEVO!» y el tutorial usan ajuste de línea y, si no cabe, achican
hasta 16 sp (`UiKit.BestFit`); el error dice «Esa no era» y debajo «La correcta tiene el aro amarillo». (2) La frase común de la pantalla final («Buen equilibrio entre precisión
y velocidad», «Precisión y ritmo excelentes») hablaba de velocidad: `ResultPhrases.feedback` la reemplaza por «Buena / Muy buena precisión» en los juegos cuyo puntaje no se apoya en la
rapidez (`secuencia`, `rutatesoro`, `bitacora`, `rumbo`, `satelites`, `aterrizaje`, `anagramas`, `intrusa`); en los demás no cambia. (3) La lectura final es solo la línea y el truco del modo
«el que más te costó» (`Trail.readingLines`) y una oración sobre la memoria de trabajo.

**Retoques del 3-oct (tarea 20c, Ricardo lo jugó en el teléfono).**
1. **Fondo igual al resto.** Era más claro que el de los demás juegos (brillo medio ≈ 20,6 de 255 contra ≈ 13,9 de La estrella intrusa sin su Vía Láctea; ≈ 17,6 con ella). Causa: el tinte
   radial del modo (alfa 0,14 en el CENTRO) más tres nebulosas. Ahora el fondo es el degradé común (`NeuroStyle.NightGradient`) con dos nebulosas más tenues que las de Cielo profundo
   (lila 0,09 y coral 0,05; `GameWorld.CieloDeCristal`), sin la nebulosa coral extra del juego, y el tinte del modo es una **viñeta** (`RastroSprites.Vignette`: transparente en el centro,
   alfa 0,05 en los bordes). Brillo medio estimado ≈ 13,1: igual o menor que Intrusa. (Estimado con la misma fórmula de los sprites del juego, no medido en el teléfono.)
2. **Que el cambio de modo se entienda** (la instrucción de antes de la muestra se olvida al mirar la chispa):
   - **A. Aviso al cambiar de modo** (`ModeScreen`): si la ronda trae un modo distinto al de la anterior (no la primera ronda ni la guiada), antes de la chispa sale ~1,5 s «Ahora: AL REVÉS» con el
     ícono grande y una línea («De la última a la primera» / «Solo las últimas N» / «El cielo va a girar» / «En orden, como lo ves»), con un toque sigue y un sonido propio por modo
     (`RastroSounds.ModeCue`). No gasta tiempo del Reto. Si coincide con el «¡NUEVO!» de por vida, sale solo el «¡NUEVO!» (`RastroRound.Notice`).
   - **B. Recordatorio al responder:** un rótulo de 18 sp del color del modo, con el ícono dibujado, arriba del tablero durante toda la respuesta («AL REVÉS · empieza por la última»,
     «SOLO LAS ÚLTIMAS N», «MISMO ORDEN, LOS LUCEROS SE MOVIERON», y «EN ORDEN», más discreto, en el rastro simple), con ajuste de línea. La cinta del dedo toma el color del modo.
   - **C. Segunda oportunidad por confusión de modo** (`RastroSession.TryModeConfusion`): en «al revés», si el PRIMER toque es el primer lucero mostrado (y no el último); en «en marcha», si es
     el primer lucero de toda la muestra (y no el primero de las últimas N; si coinciden, es un acierto). No cuenta como error, no quita vida ni entra al DDA; Nubi dice «¡Ojo! Esta era al revés.
     Mírala de nuevo» y se repite la MISMA muestra. Una sola vez por modo y por partida (la segunda cuenta como error). No aplica en la ronda guiada. Telemetría `ras_mode_confusions[4]`;
     la medida final no cambia.


**Retoque 20d.** El ícono de «Al revés» era una flecha curva casi igual a la de «El cielo gira» (solo cambiaba el sentido): ahora es una flecha RECTA a la izquierda con un punto en el origen (←), espejo del → de «El rastro» (que también lleva su punto de origen); la única flecha curva es la del cielo que gira (↻) y «En marcha» sigue con ≫. Sale en la píldora del marcador, el cartel «Ahora:», el rótulo al responder y el «¡NUEVO!» (todos usan `RastroSprites.Icon`); la pantalla final de la app no muestra íconos de modo.


**Pieza común de tutorial, ampliada el 3-oct (tarea 21a).** Freno, Aterrizaje y Lluvia de meteoros ya la usan (ver su ficha). Cómo sumar otro juego:

1. En el controlador (hereda `GameControllerBase`): `RunTutorialIfNeeded()` antes de la cuenta regresiva (devuelve la corrutina; sale solo si la app mandó `show_tutorial`) y, en
   `Awake`/construcción de la UI, `BuildTutorial(safe, practiceTopU, "Nombre del juego", "Meta en una frase (≤ 2 líneas, ≥ 18 sp)", skipAtTop, captionFromBottomU, badgeAtBottom)`: los tres
   últimos mueven el botón «Saltar tutorial», el mensaje de Nubi y el rótulo «Práctica: no cuenta» para no tapar los botones de juego.
2. Implementar `GuidedRound()` entre las marcas `// <guided>` y `// </guided>`: arma el guion con `GuidedScript` (pasos, errores que repiten el mismo paso, `Skip`), usa `CreateHintRing`
   / `SpinHint` para el aro sol punteado y `Say(...)` para lo que dice Nubi. **No puede tocar** puntaje, DDA, rachas ni conteos: `GuidedTutorialTests` lee el código entre las marcas y
   falla si aparece alguno (lista de símbolos por juego en esa prueba: sumar los del juego nuevo).
3. «Cómo se juega» desde la pausa: sobrescribir `HowToReady` (¿ya hay partida?), `HowToSuspend()` (detener el bucle y limpiar lo que está en vuelo), `HowToResume(spent)` (correr cada ancla de
   tiempo con `HowToClock.Shift` y reiniciar el bucle principal). El `PauseMenu` muestra el botón solo si `CanShowHowTo`.
4. Kotlin: sumar el id a `UnityGameLauncher.TUTORIAL_GAMES`.
5. Smoke: entradas `TutorialX` en `HeadlessPlaymodeSmokeTest` (el Editor sigue solo la tarjeta tras 1 s con `GuidedTutorial.EditorAutoContinue`, solo en el Editor).
