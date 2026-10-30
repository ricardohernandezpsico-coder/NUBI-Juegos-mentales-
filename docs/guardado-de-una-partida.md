# Guardar una partida: todo o nada (Tarea 192, 30-oct-2026)

Antes, `NeuroVidaRepository.recordGameResult` escribía la partida, el avance del juego, la ruta de hoy, la maestría y los desafíos por separado (sin transacción), mezclados con las preferencias, y `UnityResultInbox.claim` anotaba el lanzamiento como hecho ANTES de guardar. Dos cosas que podían salir mal: un fallo a la mitad dejaba Room a medias (y las preferencias contando una partida que Room no tenía), y si el proceso moría entre la marca y el guardado, la partida se perdía.

## El diseño

1. **Una transacción** (`database.withTransaction`, con las lecturas que calculan el nivel adentro): `game_results`, `game_progress`, `daily_session`, `domain_mastery` y `claimed_weekly_challenges`. Si algo falla, no queda nada de Room.
2. **Después de confirmar**, y solo entonces, se escriben las preferencias y los `StateFlow` (el avance de `skill`, la historia del avance, los logros, las ligas, las medidas, los récords…), en el mismo orden de siempre (`applySavedGame`). Un fallo dentro de la transacción no deja las preferencias contando una partida que Room no tiene.
3. **Idempotente por la llave primaria.** El `id` de la fila (`game_results.id` y `time_trial_results.id`, que ya eran `TEXT PRIMARY KEY`) es el **id del lanzamiento** de Unity; sin id (versiones viejas, botones de prueba) sigue siendo un UUID nuevo. Dentro de la transacción, si esa fila ya está, no se guarda nada más y se devuelve `RecordOutcome.alreadySaved` (la interfaz no la celebra otra vez). **Sin migración y sin esquema nuevo** (Opus propuso la columna `launch_id` + Room v17; usar la llave primaria dio lo mismo con un solo mecanismo para las dos tablas y cero riesgo de Room).
4. **La marca de la bandeja** (`UnityResultInbox`, preferencia `unity_results`) es solo el filtro rápido y se escribe **DESPUÉS** de guardar (`markHandled`). Mientras se guarda, el lanzamiento queda reservado en memoria (`claim`): la vuelta con el Intent y el broadcast de respaldo no guardan a la vez; si el guardado falla, `release` suelta la reserva sin anotarla y otra entrega puede reintentar. Un `Mutex` en el repositorio serializa los guardados.
5. **El Contrarreloj** (`time_trial_results`) pasa por el mismo camino (`recordTimeTrial`): `TimeTrialDay.resultOf` toma el `id` del lanzamiento, una transacción y la misma regla.

## Por qué el id de lanzamiento nunca se repite entre dos partidas terminadas

- **Cómo se genera:** `UUID.randomUUID()` (aleatorio, versión 4): en `UnityGameHost` (`rememberSaveable { session.resumeLaunchId ?: UUID.randomUUID() }`, dentro de `key(session.sessionToken)`: cada sesión es su propio grupo de composición y su `sessionToken` es otro UUID) y en `UnityGameLauncher.buildIntent` cuando no se le pasa uno. No es un contador: no vuelve a empezar tras «Borrar datos» ni al restaurar un respaldo (una colisión entre dos UUID aleatorios no ocurre).
- **«Jugar de nuevo», «Otra vez» del Contrarreloj y la ruta de hoy:** `launchGame` crea una sesión nueva (sin `resumeLaunchId`) y su host genera un id nuevo.
- **Retomar una partida en pausa:** usa el id de esa partida (`resumeLaunchId`): es la MISMA partida, que todavía no informó (solo informa la que termina); su resultado sale una sola vez.
- **«Reiniciar» desde la pausa:** Unity recarga la escena con el MISMO Intent (mismo id), pero la partida abandonada desaparece sin informar (`onGameFinished` solo se llama al terminar una partida) y, una vez terminada, el botón Atrás vuelve a la app (con la partida terminada no abre la pausa: `GameEntryPoint.Update`), así que no hay un «Reiniciar» posterior a un resultado. Un solo informe por id.
- **Nada lee `game_results.id` ni `time_trial_results.id` como aleatorio:** se ordena por `timestamp`, ningún logro, exportación ni pantalla usa el id, y las pruebas usan ids como «r1».
- Pruebas: `GuardadoTodoONadaTest` (12): un fallo forzado dentro de la transacción no deja nada (Room, preferencias ni memoria); la doble entrega, también simultánea (4 a la vez), guarda una sola vez; un «reinicio» tras la transacción y antes de la marca no duplica; el Contrarreloj, igual; el id es un UUID v4 y cada lanzamiento trae uno nuevo; «Jugar de nuevo» y «Otra vez» guardan las dos; «Reiniciar» y retomar una pausa guardan una; restaurar un respaldo con la base vieja no pierde partidas nuevas ni choca con las filas restauradas (la misma partida que ya vino en el respaldo no se duplica: es la misma).

## Inventario: lo que se escribe por cada partida y qué pasa si el proceso muere

Orden real: (T) = DENTRO de la transacción (todo o nada); (P) = preferencia, DESPUÉS de confirmar, con `apply()` (en memoria al instante, al disco en milisegundos); el último paso es la marca de la bandeja. «Muere entre Room y…» = el proceso cae con la partida ya confirmada en Room y antes de ese paso.

| # | Qué | Dónde | Cuándo | Si muere entre Room y este paso |
|---|---|---|---|---|
| 1 | La partida | `game_results` (T) | siempre | — (ya está) |
| 2 | Avance del juego (nivel, trofeos, `ddaRating`, partidas, mejor puntaje) | `game_progress` (T) | siempre | — |
| 3 | Ruta de hoy | `daily_session` (T) | si es la que toca | — |
| 4 | Maestría por dominio y bono de desafíos | `domain_mastery`, `claimed_weekly_challenges` (T) | siempre / al completarse | — |
| 5 | Avance (Inicio…Maestro, «medido») | `skill` → `state` (P) | si la partida cuenta (A tu medida con `endRating`, o Desafío superado) | Se pierde ESTE aporte (puntos de área y «medido» de esa partida); `ddaRating` sí quedó en Room y el siguiente guardado recalcula desde Room |
| 6 | Historia del avance (la semana de Hoy) | `progress_log` → `points` (P) | si cuenta | Falta un punto de la semana |
| 7 | Logros | `achievements` → `unlocks` (P) | siempre | Se recupera solo: se derivan del historial (Room) y el siguiente guardado celebra el que falte |
| 8 | Ligas | `league_events` → `events` (P) | solo si hay ascenso | Falta el evento de ascenso (los trofeos sí están en Room); las ligas están ocultas en la v1 |
| 9 | Medida propia del juego | `star_measures` → `points` (P) | A tu medida | Falta UN punto de «Tu avance» del final (no se puede reconstruir: viene de la telemetría, no de Room) |
| 10 | Palabras raras, frases poco claras, láminas del atlas | `word_collection`, `unclear_sentences`, `atlas` (P) | según el juego | Faltan los hallazgos de esa partida |
| 11 | Palabras azules de Punta | `punta_words` → `pending` (P) | Punta | Faltan las azules nuevas (no vuelven) |
| 12 | Cohete de Engranajes (luces, órbita) | `engranajes_rocket` (P) | Engranajes | Se pierde lo ganado en esa partida (la app se lo manda a Unity en la siguiente) |
| 13 | Récords y totales: Bodega, Constelaciones, Correo, Satélites, Rescate, Acoplamiento, Aterrizaje | `*_record` (P) | cada juego | Un récord nuevo de esa partida no queda hasta que se supere otra vez (Unity parte del récord que le manda la app) |
| 14 | Marca de la bandeja | `unity_results` → `handled_launch_ids` (P) | último | Se acepta la entrega otra vez (si llega); la base reconoce el id y no guarda de nuevo |

**Lectura:** lo que importa de verdad (la partida, el avance de cada juego, la ruta, la maestría) es todo o nada. Las preferencias (5 a 13) son efectos de segundo orden: una muerte del proceso en la ventana entre la transacción y su `apply()` (milisegundos, con la app al frente) deja la partida en Room y se pierde ese aporte. Y como la entrega repetida de ese resultado la reconoce la base y NO repite los efectos, tampoco se «reparan» después. **No se migraron a Room** (decisión de Opus); si algún día se quiere cerrar esa ventana, el camino es guardar los efectos pendientes junto a la partida (una columna o una tabla) y aplicarlos al arrancar; no vale el costo hoy.

**La ventana que queda abierta:** si el proceso muere entre que el resultado llega a la app y la transacción, la partida se pierde, porque nadie la reentrega (el Intent de vuelta y el broadcast son de una sola vez). Cerrarlo pide escribir el JSON pendiente antes de procesar (`GameSessionStore.savePendingResult` con `commit`) y borrarlo tras la transacción: un `commit` más en el hilo principal al volver de cada juego. Con la app al frente la ventana es de milisegundos y se decidió solo anotarlo.
