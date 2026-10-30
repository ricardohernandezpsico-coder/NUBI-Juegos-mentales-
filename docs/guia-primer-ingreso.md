# La guía de Nubi en la primera entrada a Hoy (Tarea 193, 30-oct-2026)

Ricardo decidió, con la propuesta A de Fable (`auditoria-fable/guia-primer-ingreso.md`, solo lectura), tres cosas para el cierre del Primer vuelo: una **guía de Nubi** la primera vez que se entra a Hoy, **solo la primera medalla** y la tarjeta de ese día con **los juegos que de verdad se jugaron**. Lámina «hoy | propuesta» con capturas reales: `docs/previews/tarea-193.png` (`tools/previews/tarea_193.py`).

## 1. La guía (propuesta A)

Tres pasos con velo, un hueco con aro lima sobre el elemento REAL y el globo de Nubi, como los tutoriales de los juegos: 1) el botón **Contrarreloj**, 2) **Todos los juegos**, 3) la **barra** (solo Avance y Tú).

| Paso | Texto del globo | Lo que dice TalkBack |
|---|---|---|
| Paso 1 de 3 | «El Contrarreloj dura 2 minutos y su juego cambia cada día.» | «Nubi te muestra Hoy. Paso 1 de 3. El botón Contrarreloj, bajo tu ruta. …» |
| Paso 2 de 3 | «Aquí están los 17 juegos: eliges el que quieras.» | «Paso 2 de 3. El botón Todos los juegos, debajo del Contrarreloj. …» |
| Paso 3 de 3 | «En Avance ves tu punto de partida. En Tú, tu nombre y tus ajustes.» | «Paso 3 de 3. Las pestañas Avance y Tú, en la barra de abajo. …» |

- **Cuándo sale** (`HoyGuide.shouldShow`, `HoyGuideHost`): solo con `flightDay` (`DailySessionState.isFlightDay`), sin la marca, con Hoy a la vista y nada encima (la condición `barVisible`, la pestaña Hoy quieta y sin ventana abierta), con `StartupStage.READY` y 600 ms después de eso (más los 200 ms del fundido del arranque). Si llega algo mientras espera (una medalla), la espera se cancela; si llega con la guía abierta, el velo se esconde y vuelve cuando Hoy queda libre.
- **Cómo avanza:** «Seguir ›» (lila; en el paso 3, «Listo», sin flecha); **tocar lo iluminado = Seguir** (no abre nada ni llama a `startTimeTrial`); **tocar el velo no hace nada** y no se desliza nada de abajo; **Atrás = saltar**; «Saltar la guía» (chip crema arriba a la derecha) cierra desde cualquier paso. El contador dice «Paso N de 3» (como el Primer vuelo, «sin palabras sueltas»). Ningún sol: los chips son lila y crema (el único sol es el Contrarreloj iluminado).
- **La marca** es `hoy_guide_seen` en `profile_extra` (va en el respaldo, no toca `BackupRulesTest`, «Borrar datos» la limpia: `resetData` pone `hoyGuideSeen` en falso). Se escribe al TERMINAR («Listo» o «Saltar»). Si la app se cierra a mitad, la guía vuelve a salir desde el paso 1 ese mismo día y nunca después (`flightDay` es verdadero solo ese día). El paso vive en el ViewModel (`guideStep`), así que sobrevive a que se recree la Activity. Sin puerta para volver a verla.
- **Dónde vive el código:** `data/HoyGuide.kt` (lógica pura: pasos, textos, `shouldShow`, `STEP_DELAY_MS`), `ui/components/HoyGuideOverlay.kt` (`HoyGuideOverlay` y `HoyGuideHost`), `ui/components/GuideTargets.kt` (`Modifier.guideTarget`: Hoy y la barra publican sus rectángulos), `ui/components/HiddenUntil.kt` (la pieza que antes era privada de `StartupGate`, ahora compartida: sin lector de pantalla y sin toques mientras la guía está abierta), `NeuroVidaApp` (la aloja encima de todo, también de la barra) y `NeuroVidaViewModel` (`guideStep`, `startGuide`, `guideNext`, `guideSkip`, `hoyGuideSeen`).
- **Elemento a la vista (decisión de Opus):** antes de CADA paso el blanco se trae a la vista con `bringIntoView` (no solo con letra 2,0) y el hueco se dibuja recién cuando terminó de deslizar (mientras tanto queda el del paso anterior). Con letra 1,3 el día del vuelo, «Todos los juegos» queda entero y sobre la barra.
- **El hueco** lleva 8 dp de aire y esquinas de 22 dp; el de la barra cubre solo sus dos tercios de la derecha y **no pasa de su borde de abajo ni de la derecha** (así no entra en la franja de gestos ni en los 3 botones de Android).
- **Letra:** con 1,3 Nubi pasa a S (72 dp); con 1,5 o más el contador y el chip van uno bajo el otro (si no, «Paso 1 de 3» se partía); si el texto no cabe, se desliza dentro del globo. «Quitar animaciones»: el velo, el hueco y el texto cambian de golpe.

## 2. Solo la primera medalla

`finishFlight()` (Primer vuelo COMPLETO, `FlightMode.FULL`) celebra SOLO «Primer paso» (`primer_paso`). «Día completo» y «Mente completa» (`dominios`) se ganan igual —`unlockNewAchievements` las guarda al registrar cada partida, antes de la celebración— y quedan en Tú › Medallas; si «Primer paso» no está entre las nuevas, no se celebra ninguna. «Repetir la evaluación» (`FlightMode.GAMES`) queda como estaba (las celebra todas). La guía espera a que se cierre esa única celebración.

**Una medalla ganada sin celebrar no deja nada raro:** no hay marca de «nueva» ni contador de pendientes (Tú › Medallas se arma de `achievementUnlocks`); la celebración es solo una pantalla que consume `achievementQueue`, que `finishFlight` ya no llena con las otras dos; y otra partida el mismo día no las vuelve a celebrar (`unlockNewAchievements` solo devuelve las que no estaban). El número que dice la celebración («3 de 15 medallas») cuenta lo ganado, también lo no celebrado.

## 3. La tarjeta del día del vuelo con los juegos de verdad

`completeTodayWithFlight(playedGameIds)` guarda en Room (`daily_sessions`) los juegos que se jugaron en el vuelo, en el orden del vuelo (`BaselinePlan.steps` filtrado por `FlightState.measured`), con `completedCount` = cuántos. Hoy los muestra con «Hecho» y el encabezado de siempre («4 de 4 · ruta completa»; `homeStops` muestra hasta 4). Con «Terminar después», los juegos sin jugar no aparecen; **si no se jugó ninguno, el día NO se da por cumplido** (la ruta queda para jugarla). «Si ya hay puntajes, no se toca» se mantiene. `isFlightDay` sigue siendo verdadero ese día, y mañana la ruta normal arranca bien (`GuiaPrimerIngresoTest`: ayer con los 4 del vuelo, hoy 3 juegos sin empezar).

**La línea de la tarjeta** (decisión de Opus) pasó a UNA frase: «Tu primer vuelo fue tu ruta de hoy.» (el cierre del vuelo ya dice «Mañana empieza tu ruta…»), igual con 1, 2, 3 o 4 filas.

**Cabida (393 × 873 dp, `HoyTarjetaTest`):** con letra 1,0, el día del vuelo cabe sin deslizar con 1, 2, 3 y 4 filas (con 4, 16 dp de sobra bajo la regla de la prueba). Con letra 1,3 es el único estado en que la regla conservadora de la prueba (la barra y 75 dp de aire: 163 dp abajo) se pasa: 3 filas, 29 dp; 4 filas, 90 dp; con 1 y 2 caben. En el emulador con la barra real (88 dp) la tarjeta de 4 filas con letra 1,3 cabe justo. La prueba deja los números documentados.

## 4. Pruebas

- `HoyGuideTest` (pura): orden, textos (las reglas de los globos), contador, chips, TalkBack, `next`, la matriz de `shouldShow` (16 combinaciones) y la espera.
- `HoyGuideOverlayTest` (Robolectric, con el Hoy y la barra reales): el hueco cae sobre cada elemento (los rectángulos medidos), avanzar y cerrar, «Saltar», Atrás, tocar lo iluminado sin abrir nada, tocar el velo, lo de abajo fuera del árbol de accesibilidad, chips de 48 dp o más (1,0 y 2,0), el globo fuera del hueco con letra 1,0, 1,3 y 2,0 y a 360 × 780, `bringIntoView` con 1,3, el archivo no pide `Clay.Sun` ni letra menor de 16 sp, y quien la aloja con el ViewModel de verdad (sale a los 600 ms, no con una medalla encima, la espera se cancela, se esconde y vuelve, con la marca no sale, sin `flightDay` no sale, el paso vive en el ViewModel y si la app se cierra vuelve desde el paso 1).
- `GuiaPrimerIngresoTest` (flujo del ViewModel y la base): solo «Primer paso», sin «Primer paso» ninguna, «Repetir la evaluación» como estaba, las medallas sin celebrar siguen ganadas y no saltan, la tarjeta con 4 juegos / con 2 / con ninguno / con puntajes, `finishFlight` con el orden del vuelo, y mañana.
- `HoyTarjetaTest`: el día del vuelo con 4 filas cabe con 1,0; con 1,3 se mide.
- Capturas: `HoyGuideScreenshotTest` (solo al grabar) y las de letra 2,0 del emulador (fuera de git).

## 5. Emulador (`medium_phone`, API 36)

- **Letra 2,0:** los tres pasos se ven, Hoy se desliza y trae cada elemento a la vista, el hueco cae sobre el botón real, el texto cabe y el contador y el chip van uno bajo el otro.
- **Barras:** con gestos y con 3 botones, el hueco del paso 3 coincide con la barra real y no pasa de su borde de abajo (antes llevaba 8 dp de aire y entraba unos 9 dp en la franja de gestos: corregido).
- **Marca:** «Listo» escribe `hoy_guide_seen`; al reabrir la app la guía no vuelve.
- **TalkBack:** pasada PARCIAL. Con TalkBack activado, el foco cae primero en el globo (captura con el aro de TalkBack sobre el texto) y el árbol de accesibilidad solo trae el globo, «Seguir» y «Saltar la guía», en ese orden; Hoy y la barra no están. Recorrer el foco con gestos desde `adb` no es confiable (TalkBack toma los gestos inyectados como toques): queda una pasada manual en un teléfono con TalkBack para oír el anuncio y deslizar globo → «Seguir» → «Saltar la guía».
