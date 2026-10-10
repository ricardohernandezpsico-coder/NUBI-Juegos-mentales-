# Arquitectura de la app: pestañas, inicio y sesión diaria

Movido TAL CUAL desde `CLAUDE.md` el 10-oct-2026 (Tarea 69) para dejarle lugar al índice; lo que manda es el código. El resto de la arquitectura (ViewModel, repositorio, persistencia, puente con Unity) sigue en `CLAUDE.md`.

## Pestañas, perfil y opciones, Primer vuelo

- 3 pestañas (29-sep, `docs/previews/navegacion-nubi.png`; `ui/components/NeuroNavBar`, SIN botón "play" central: el
  desafío del día se empieza desde Hoy): Hoy (casa; `HomeScreen`, Nubi al centro con las 4 áreas: ver abajo) · Juegos
  (player; `GamesLibraryScreen`: ver abajo) · Avance (cerebro dibujado, `BrainIcon`; `ProgressScreen`: tu liga con
  compartir, tus 4 áreas con la barra de Hoy y su detalle, desafíos de la semana, avance por juego, punto de partida y
  "Ver más detalles"). No se dice "Mi cerebro" (Ricardo: suena a Lumosity). Arriba a la derecha en las 3
  (`TabTopBar` + `components/TopActions`): tu inicial en arcilla celeste = perfil (`ProfileScreen`: escudo, cifras,
  logros) y engranaje = opciones (`SettingsPanel` → `SettingsScreen`); se abren como paneles (`viewModel.topPanel`).
  Inicio «Primer vuelo con Nubi» (`FirstFlightScreen` + `FirstFlightHost`, `data/FirstFlight.kt`, `docs/diseno-inicio.md`): 19 pasos con 4 juegos (uno por área) y preguntas intercaladas, mientras `UserSettings.ageBand == null` o haya un recorrido guardado.
  Las pestañas se pasan deslizando con el dedo (`HorizontalPager` en `MainActivity`, sincronizado con
  `viewModel.currentTab`). Una ventana abierta sobre una pestaña (área en Juegos, detalle de un área en Hoy o Avance)
  llama `LockTabSwipe()`: mientras esté, el dedo no cambia de pestaña y la barra de abajo se esconde.
  La ficha de un juego (`GameSheet` en `GamesLibraryScreen`, ventana superpuesta) ocupa toda la pantalla pero su tarjeta vive DENTRO de las barras del sistema (`WindowInsets.safeDrawing`) con un margen y mide como máximo el alto que queda: lo demás hace scroll por dentro y «Jugar» va FIJO abajo, fuera del scroll (Tarea 73, 10-oct: en el Motorola, en juegos con medida, gráfico y reloj, «Jugar» caía bajo la barra de navegación). Prueba: `GameSheetFitScreenshotTest` (360 × 780 y 360 × 640).

## Sesión diaria

- Sesión diaria = SOLO los 3 juegos del camino (`startDailySession` → `continueDailyFlow`), elegidos por avance real y con variedad por `data/DailyPath` (reglas en su cabecera; nunca un juego sin tutorial que la persona no jugó). El día del Primer vuelo el camino queda cumplido con el vuelo (`completeTodayWithFlight`). Al terminar, el resumen (`data/SessionSummary` + `SessionSummaryScreen`): áreas trabajadas, puntaje de cada juego, racha y «Hoy avanzó de X a Y» por área.
