# Nubi — memoria del proyecto (al 1-oct)

**Nombre público desde el 29-sep: Nubi** ("Nubi – Brain Games"; el personaje es una nebulosa pequeña de arcilla).

> **Nombres internos.** La app se llama Nubi, pero por dentro todavía dice "NeuroVida" en tres lugares, y NO se cambia:
> (1) el archivo de la base de datos `neurovida_database` y los nombres de las preferencias: renombrarlos haría perder el
> progreso de quienes ya la usan; (2) los namespaces y asmdefs de Unity (`NeuroVida.*`, `NeuroVidaCore`, la carpeta
> `unity/NeuroVidaCore/`) y clases Kotlin (`NeuroVidaViewModel`, `NeuroVidaRepository`…): un refactor enorme sin ningún
> beneficio visible; (3) el `applicationId` (`com.aistudio.neurovida.cgnv`): se define UNA sola vez antes de publicar (ver
> `docs/auditoria-30-sep.md`). **La carpeta del proyecto ya se llama `Nubi/`** (renombrada desde `NeuroVida/` el 1-oct): la ruta
> es `C:/Users/RURAL7/Desktop/Proyectos/Nubi`. La primera vez que Ricardo abra Unity tendrá que volver a agregar el proyecto
> (`unity/NeuroVidaCore`) en Unity Hub y Unity reconstruirá su caché (`Library/`). La app no muestra "NeuroVida" en ningún texto
> visible (se comprobó el 1-oct en `strings.xml` y en la interfaz). En los documentos, "Nubi" nombra la app y "NeuroVida" solo
> aparece en identificadores o cuando se habla del nombre viejo.

App de estimulación cognitiva para Android: 22 juegos cortos en 4 áreas (Memoria · Atención [foco y velocidad] · Razonamiento [lógica y números] · Lenguaje;
de 6 a 4 el 30-sep, Room v12), dificultad que se adapta, camino diario de 3 juegos, ligas con trofeos, logros,
racha y un punto de partida inicial. La app (menús, progreso, datos) es Kotlin + Compose; los juegos corren en
Unity embebido ("Unity as a Library"). Dueño y quien prueba: Ricardo (psicólogo). Meta: superar a
Lumosity/Peak/Elevate en calidad y en motivación.

El diario detallado de cómo se llegó hasta aquí (decisiones, bugs, pedidos de Ricardo) está en
[`docs/historial-desarrollo.md`](docs/historial-desarrollo.md). Es historia: lo que manda es este archivo y el código.

## Cómo trabajamos (nube ↔ PC)

- Las sesiones en la nube editan, verifican C# con `dotnet build tools/unity-compile-check -v q` y hacen push a la
  rama de trabajo. En la nube no hay Unity, pero **la app Kotlin SÍ se compila y prueba**: `bash tools/nube-compilar-app.sh`
  (instala el SDK la primera vez, pone un unityLibrary FALSO en `unity/AndroidExport/` y corre `:app:testDebugUnitTest`;
  con `fotos` graba las capturas Roborazzi, p. ej. `HomePlanetScreenshotTest`, para ver pantallas sin teléfono).
  No correrlo en el PC de Ricardo (allá `unity/AndroidExport/` es el export real).
- En el PC de Ricardo (Git Bash, carpeta del repo): `git pull && bash tools/verificar-todo.sh --instalar` →
  escena piloto, pruebas EditMode, smoke de los 22 juegos, REEXPORTAR Unity, Gradle con pruebas Kotlin, instalar.
  Si algo falla, él pega las últimas 40 líneas de `unity/test-results/v-*.log`.
- `unity/AndroidExport/` está fuera de git: si no se reexporta, el APK lleva los juegos viejos sin avisar.
  Marca de verificación: en builds de depuración la cuenta regresiva muestra `CountdownScreen.StyleStamp`
  (hoy `estilo 1-oct · cosecha`). **Cambiarla con cada cambio visible de Unity.**
- Vistas previas sin Unity ni teléfono: `tools/art-preview` (compila los generadores de sprites REALES contra un
  UnityEngine mínimo y vuelca PNG; `compose.py` y `juegos.py` arman láminas) y `tools/previews/*.py` (réplicas PIL
  de pantallas Compose). Resultados en `docs/previews/`. Si se cambia el arte, actualizar la lámina.
- Estilo con Ricardo: en español, sin jerga, cambios chicos y verificables, y decirle siempre qué probar.
- Qué sigue (juegos que faltan para llegar a 5 por área, Avance, orden): [`docs/hoja-de-ruta.md`](docs/hoja-de-ruta.md).
- Repositorios externos: si alguno de GitHub puede potenciar la app, COMENTARLO a Ricardo y él decide; nunca agregarlo
  sin preguntar. Descartados: Zenject/Extenject y awesome-unity (razones en la hoja de ruta).

## Toolchain (PC Windows de Ricardo)

- Android SDK `C:\Users\RURAL7\AppData\Local\Android\Sdk` (android-36; emulador `medium_phone` x86_64: sirve para
  la UI Compose, no para los juegos Unity, que son arm64). Teléfono: Motorola (`DEVICE` en `verificar-todo.sh`).
- JDK 21 Temurin `C:\Users\RURAL7\AppData\Local\Temurin21\jdk-21.0.12.1+1` (NO el JBR de Android Studio).
- Unity 6000.0.84f1 LTS + soporte Android. Config-cache de Gradle desactivado (choca con el export de Unity).
- `local.properties` y `debug.keystore` fuera de git. Compilar a mano: `./gradlew.bat assembleDebug`.

## Arquitectura

**App Android** (`app/src/main/java/com/example/`, paquete `com.example`, applicationId `com.aistudio.neurovida.cgnv`):
- `MainActivity` + `viewmodel/NeuroVidaViewModel` (un solo ViewModel) + `data/NeuroVidaRepository`.
- 3 pestañas (29-sep, `docs/previews/navegacion-nubi.png`; `ui/components/NeuroNavBar`, SIN botón "play" central: el
  desafío del día se empieza desde Hoy): Hoy (casa; `HomeScreen`, Nubi al centro con las 4 áreas: ver abajo) · Juegos
  (player; `GamesLibraryScreen`: ver abajo) · Avance (cerebro dibujado, `BrainIcon`; `ProgressScreen`: tu liga con
  compartir, tus 4 áreas con la barra de Hoy y su detalle, desafíos de la semana, avance por juego, punto de partida y
  "Ver más detalles"). No se dice "Mi cerebro" (Ricardo: suena a Lumosity). Arriba a la derecha en las 3
  (`TabTopBar` + `components/TopActions`): tu inicial en arcilla celeste = perfil (`ProfileScreen`: escudo, cifras,
  logros) y engranaje = opciones (`SettingsPanel` → `SettingsScreen`); se abren como paneles (`viewModel.topPanel`).
  Onboarding (`OnboardingScreen`, 9 pasos; bienvenida "Hola, soy Nubi": Nubi como compañía) mientras
  `UserSettings.ageBand == null`. Evaluación y mapa inicial: `BaselineScreen`.
  Las pestañas se pasan deslizando con el dedo (`HorizontalPager` en `MainActivity`, sincronizado con
  `viewModel.currentTab`). Una ventana abierta sobre una pestaña (área en Juegos, detalle de un área en Hoy o Avance)
  llama `LockTabSwipe()`: mientras esté, el dedo no cambia de pestaña y la barra de abajo se esconde.
- Sesión diaria = SOLO los 3 juegos del camino (`startDailySession` → `continueDailyFlow`). Al "Continuar" del tercero,
  resumen (`data/SessionSummary` + `ui/screens/SessionSummaryScreen`): Nubi celebra, qué áreas se trabajaron, puntaje
  de cada juego, racha y la barra de avance de cada área con "Hoy avanzó de X a Y" (cambio desde el comienzo de la
  sesión, `AreaProgress.status(since = ...)`). Captura `docs/previews/resumen-sesion-real.png`.
- Lógica pura con pruebas en `data/`: `Achievements`, `AreaProgress`, `Baseline`, `DdaRating`, `Homing`, `Mail`, `LeagueEvents`, `MissionLog`, `NumberLine`, `Percentile`,
  `Planet`, `SessionSummary`, `StarMeasures`, `Library`, `Skill`; y
  `notification/ReminderContent`. Modelos y catálogo de juegos (`GameRegistry`, `RankTier`, ...) en `model/Models.kt`.
- Persistencia: **Room v12** (`data/local/`, `exportSchema`, esquemas en `app/schemas/`; resultados, progreso por
  juego con `ddaRating`, sesión diaria, perfiles, maestría, desafíos). Datos que solo se agregan o salen del
  onboarding van en **SharedPreferences** para no migrar: `league_events`, `achievements`, `profile_extra`
  (educación, metas, mapa, `prior_at`), `paused_game`, bandeja de resultados de Unity, `skill` (avance: ver abajo), `star_measures` (la medida
  propia de cada partida de los juegos estrella, `StarMeasures.encode`), `mission_log`, `progress_log` (historia del
  avance de cada juego, ~60 días, `AreaProgress`: el cambio de la semana de cada área en Hoy).
  Al cambiar el esquema de Room: entidad → subir versión → `Migration(N, N+1)` en SQL → compilar → comitear `schemas/<N+1>.json`.
- Diseño "noche + arcilla" (`ui/theme/Clay.kt`, `Type.kt` con Fredoka, `ui/components/CosmosBackground.kt`).

**Puente app ↔ Unity** (`bridge/`):
- Lanzar: `UnityGameLauncher.buildGameIntent` → JSON de configuración en un extra del Intent (juego, nivel, modo,
  edad, sonido, vibración, rating guardado, evaluación) + `EXTRA_LAUNCH_ID` por partida. Unity corre en el proceso
  `:unity` y queda VIVO detrás de la app entre partidas (`REORDER_TO_FRONT`): solo la primera partida arranca en frío
  (se tapa con `UnityLoadingOverlay` / `GameLoadingScreen`).
- Volver: Unity → `NativeBridge.CloseGameScreen` → `NativeReceiver.returnToApp` trae `MainActivity` con el resultado
  → `viewModel.onReturnedFromGame`. Respaldo: broadcast `ACTION_GAME_FINISHED` → `UnityResultReceiver`;
  `UnityResultInbox` evita guardar dos veces. Pausa: Atrás en Unity abre menú de pausa; "Salir" vuelve con
  `paused = true` y la partida se retoma con el mismo launch id.
- Telemetría: `NativeReceiver` elige el adaptador por `game_id` (Secuencia, Parejas, y `StroopTelemetry` para los 7
  del DDA común) y llama a `repository.recordGameResult` (devuelve `RecordOutcome`: nivel, ascensos, logros).

**Unity** (`unity/NeuroVidaCore/Assets/Scripts/`, asmdefs `Contracts ← Bridge ← Games ← Bootstrap`):
- `Bootstrap/`: `GameEntryPoint` (config, orientación vertical, Atrás), `LaunchIntentConfigReader` (lee el Intent,
  arranca/reinicia partidas por launch id). `Bootstrap/Editor/`: exportar librería, escena piloto, smoke tests,
  `SymbolPreviewExporter`.
- `Contracts/`: `SequenceInitConfig` (config de entrada de TODOS los juegos), `SequenceTelemetry`, `CardsTelemetry`,
  `StroopTelemetry` (salida común de los 7 del DDA común; lleva `end_rating` y `peak_level`).
- `Games/AdaptiveDifficulty.cs`: DDA común (up-down ponderado de Kaernbach hacia 80% de aciertos, 85% en mayores;
  ver `docs/DDA-comun.md`). Lo usan Tinta o Palabra (`Stroop/`), Comparación, Cambio de Chip, Ruta del Tesoro,
  Series, Cálculo y Anagramas; **Piloto Estelar** usa DOS instancias (pilotaje y señales); **Radar** y **Satélites** una, sin tiempo de reacción; **Freno de Emergencia** una para la tarea de ir (el alto tiene su escalera propia). **Secuencia** tiene su escalera fija de 16 niveles (`SequenceLevelConfig`, sube con 2
  aciertos seguidos, 3 vidas) y **Parejas** su motor propio (`VisualWorkingMemoryDDA`, escalera de 10 tableros).
- Cada juego: `XContract.cs` (reglas puras con pruebas) + `XGameController.cs` (UI construida por código). Los 7 del
  DDA común heredan de `Shared/GameControllerBase` y usan `Shared/GameHud`.
- `Games/Shared/`: sello visual y piezas comunes — `NeuroStyle` (paleta de la app, `ClayText`, `ClayFrame`),
  `ClayRaster` (pincel SDF para todo el arte en arcilla), `WorldBackdrop` (cielo + un elemento propio por juego),
  `StarfieldFx`, `CountdownScreen`, `FinishCurtain` + `ExitButton` (cierre "¡Listo!" → resultado en la app),
  `GameFeel` (sonidos sintetizados y vibración), `GameClock` (tiempo pausable), `PauseMenu`, `Assessment`
  (modo evaluación), `UiKit`, `Toast`, `PhasePill`, `LivesHud`, `PressScale`, sprites varios.

## Juegos estrella (27-sep)

Pedido de Ricardo: juegos que diferencien a la app, con respaldo científico y mucho enganche. Orden acordado:
**Piloto Estelar** (hecho, primera versión) → **Radar** (hecho, primera versión; velocidad de procesamiento / campo visual útil, ensayo ACTIVE)
→ **Satélites** (hecho, primera versión; seguimiento de múltiples objetos). Después, catálogo: Formación (flancos), Eco Estelar (N-back),
Torre de Lunas (Torre de Londres), Matriz Perdida (tipo Raven), Constelación de Palabras (fluidez verbal).

Segunda tanda (27-sep, pedido de Ricardo: "que generen enganche", paso a paso y probando cada uno): **Freno de
Emergencia** (hecho; Ricardo: "funciona muy bien") → **Aterrizaje Lunar** (hecho; Ricardo: "todo ok") →
**Acoplamiento** (hecho; Ricardo lo instaló y probó) → **Tráfico Estelar** (pedido de Ricardo, inspirado en la
mecánica de trenes y desvíos que recordaba de Lumosity; hecho, primera versión). Después: Escuadrón (ANT: perfil alerta /
orientación / control, reemplaza a Formación), Eco Estelar (n-back doble con notas), Torre de Lunas, Constelación.
Cada juego estrella lleva una MEDIDA PROPIA al final (lo que más le gustó a Ricardo de Radar).

**Piloto Estelar** (`Games/Piloto/`, id `piloto`, dominio atención): multitarea al estilo NeuroRacer (Anguera et al.,
Nature 2013) + señales periféricas breves (UFOV). La nave vuela por una ruta de balizas que serpentea: un pulgar la
guía (tocar o arrastrar en la franja de abajo); el otro dedo atrapa solo las señales de la misión (forma + color,
íconos de `SymbolSprite`) que aparecen un instante con un anillo que se vacía.
- Reglas puras y pruebas: `PilotContract` / `PilotContractTests` (11). Ruta: `CenterAt` (dos senos), cada tramo guarda
  su amplitud/ancho al crearse (un cambio de nivel no deforma lo visible). Señales go/no-go, 40% de misión; desde el
  nivel 3 distractores de la misma forma y otro color; desde el 5, en la periferia.
- Vuelo: 15 s de piloto automático (solo señales) → "¡A LOS MANDOS!" → las dos tareas. Reto 90 s; Precisión = más
  lento, termina tras 24 señales. Dos `AdaptiveDifficulty` (pilotaje por ventanas de 1,5 s con ≥85% en ruta; señales
  por señal). Hiperimpulso: racha ≥5 + 4 ventanas limpias = puntos x2 6 s y estrellas a hiperespacio.
- **Costo de multitarea** = caída relativa de Pr (aciertos − falsas alarmas) entre piloto automático y a los mandos.
  Viaja en `StroopSessionMetrics.multitask_cost` → `GamePlayResult.multitaskCost` (no se guarda en Room) → línea en
  `GameResultScreen`. Pendiente: guardarlo por partida para mostrar su evolución.
- Nuevos compartidos: `Shared/AnswerMarkSprite` (✓/✗ de arcilla, sirve para la marca pendiente en los otros juegos),
  `GameWorld.Hyperspace`. App: `GameRegistry`, ícono en `GameIcon.kt`, botón Debug (Reto 90 s).
- Vista previa: `python3 tools/art-preview/piloto.py <raw>` → `docs/previews/piloto-estelar.png`.
- Probado por Ricardo (27-sep): "me encantó", sonidos muy bien. La primera vez no captó el momento de tomar los
  mandos y el cartel de la misión confunde un poco al principio, pero el piloto automático sirve de práctica: por
  ahora sin ajustes. Falta: respetar "quitar animaciones" (la config de Unity no lo trae), sonido propio del motor.

**Radar** (`Games/Radar/`, id `radar`, dominio velocidad) = **Rescate relámpago** (rediseño aprobado por Ricardo el
29-sep; la versión con nave central tipo UFOV se parecía a la patente US 8,348,671 de Posit). Informe total (Sperling,
1960; TVA: Bundesen, 1990): 1) atento, el haz gira y el destello llega sin aviso (1,5-3,5 s: alerta propia, Penning et
al. 2021); 2) destello con VARIOS astronautas en 16 lugares (8 direcciones x 2 anillos, cerca/lejos del centro); desde
el nivel 5, robots (casco CUADRADO gris, `RadarSprites.Robot`) que no se rescatan; 3) interferencia 350 ms; 4) "¿Dónde
estaban los N?": tocar pone/saca balizas (`NearestSlot`: toda la zona cercana cuenta; máximo N) y "¡RESCATAR!";
5) revelación: rescatado ✓ + resplandor lima (vuela a la fila), el que se escapó con aro sol, baliza de más con ✗.
Cada 5 rondas "¡Lluvia de astronautas!" (6, 300 ms, fuera de la escalera).
- Reglas y pruebas: `RadarContract` / `RadarContractTests` (13). 12 niveles: destello 600 → 80 ms, astronautas 2/3/4/5
  (niveles 1-2/3-5/6-8/9-12), robots 0/1/2 (1-4/5-8/9-12). Ronda lograda = todos hasta 3, todos menos uno con 4+
  (`Needed`). DDA común `stepUp` 0.3, sin tiempo de reacción. Reto 120 s; Precisión 20 destellos. 60 cuadros/s y
  duración REAL del destello; una pausa en pleno destello lo anula.
- Medidas: **tu vistazo** (`GlanceMs`, como antes, + `GlanceLoad`: con cuántos a la vez), **tu captura** (`Capture`:
  promedio de las lluvias, rescatados − balizas de más; ≈ K de TVA, adultos 3-4), **tu filtro** (robots tocados de los
  mostrados, se nombra con 6+) y **tu radar** (por dirección + cerca/lejos, `ringSummary`). Telemetría `glance_ms /
  glance_load / sector_* / ring_* / capture / robots_shown / robots_touched` → `GamePlayResult` → `GameResultScreen`.
  Justificación en `docs/medidas-juegos-estrella.md`.
- Arte: `RadarSprites` (radar, haz, interferencia, robot, baliza, aro de lugar), `GameWorld.RadarStation`. Vista previa:
  `python3 tools/art-preview/radar.py <raw>` → `docs/previews/radar.png`; maqueta del diseño `docs/previews/radar-rescate.png`.
  Sin probar en el teléfono (la versión anterior: "espectacular", le encantó la información del final).

**Satélites** (`Games/Satelites/`, id `satelites`, dominio atención): seguimiento de múltiples objetos (Pylyshyn y
Storm, 1988; NeuroTracker: Faubert y Sidebottom, 2012). Satélites iguales (`SymbolSprite` Satellite 0 sobre un disco
tenue); k encienden su señal 2,2 s, se apagan, todos se mueven 5-8 s y al detenerse se tocan los k (tocar de nuevo
desmarca). Al revelar: ✓/✗, los que faltaron vuelven a brillar, se dibuja la estela del recorrido de los de la señal y,
si se confundió uno por otro, un aro coral "¿Aquí se cruzaron?" en el punto donde el que faltó pasó más cerca del
elegido por error (`ClosestApproach` sobre las trayectorias muestreadas cada 0,1 s).
- Reglas y pruebas: `SatelliteContract` + `SatelliteSwarm` (movimiento puro con semilla: velocidad constante, rumbo
  que deriva, rebotes en bordes y entre ellos, nunca se tapan) / `SatelliteContractTests` (9). 12 niveles: a seguir
  2 → 5, en total 6 → 11, velocidad 0,16 → 0,52 anchos de campo/s (factor 1× → 3,3×), 5 → 8 s de movimiento. DDA con
  `stepUp` 0.5 (pocas rondas); acierto = todos los de la señal. Reto 120 s; Precisión 8 rondas. 60 cuadros por segundo.
- Medida propia: **tu seguimiento** (`TrackedEstimate`: cuántos se siguieron de verdad descontando la suerte, modelo
  aciertos = m + (k − m)² / (n − m); promedio de las rondas) y la velocidad más alta superada completa. Viajan en
  `StroopSessionMetrics.tracking_capacity / tracking_targets / tracking_speed` → `GamePlayResult.trackingCapacity /
  trackingTargets / trackingSpeed` → `GameResultScreen`: "Tu seguimiento: 2,6 de 3 a la vez" (el "de N" = cuántos había
  que seguir en promedio: es el techo de esa partida, no un límite personal) con 5 discos que se llenan + velocidad; la
  referencia "a velocidad moderada, los adultos suelen seguir entre 3 y 4; más rápido, menos (Alvarez y Franconeri,
  2007)" solo aparece si había que seguir 3,5 o más.
- Arte y vista previa: `GameWorld.MissionControl` (cielo quieto); `python3 tools/art-preview/satelites.py <raw>` →
  `docs/previews/satelites.png`. Sin probar en el teléfono: revisar tamaño de los satélites, ritmo (~12 s por ronda),
  que se entienda el "¿Aquí se cruzaron?".

**Freno de Emergencia** (`Games/Freno/`, id `freno`, dominio atención): tarea de señal de alto (Logan y Cowan, 1984;
consenso de Verbruggen et al., eLife 2019). Base de lanzamiento con 2-4 plataformas sobre la luna: se enciende un cohete
(baliza lima + botón lima) y se lanza tocando su carril (cualquier toque en la mitad de abajo; cuenta al PRESIONAR);
en ~30% aparece la señal ¡ALTO! (octágono coral con texto + sirena) un instante después y hay que no tocar.
- Reglas y pruebas: `BrakeContract` / `BrakeContractTests` (7). SSD en escalera (250 ms inicial, ±50, entre 50 y el
  límite − 150). DDA común solo para la tarea de ir: plataformas 2/3/4 (niveles 1-4/5-8/9-12) y tiempo para lanzar
  1400 → 800 ms (Precisión +300). 3 primeros sin alto, nunca más de 2 altos seguidos, espera previa 600-1200 ms
  (tocar antes = "¡Espera la luz!" y se repite sin contar). Si empieza a esperar el alto (últimos 6 lanzamientos 30% más
  lentos que los 6 primeros), aviso "¡No esperes al ALTO!". Reto 120 s; Precisión 40 lanzamientos. 60 cuadros/s.
- Enganche: cada despegue con llama, humo y temblor deja una estrella en el cielo; frenar da más puntos (más cuanto
  más tarde llegó el alto) y empuja el medidor "Límite del freno" (marca sol = récord, aviso "¡Nuevo límite!"); no
  frenar = el cohete da un salto y vuelve (sin choques).
- Medida propia: **tu freno** (SSRT por integración con reemplazo de omisiones; -1 con menos de 6 altos o
  p(responder|alto) fuera de 0,25-0,75, el criterio del consenso de Verbruggen et al., 2019; si no hay estimación, el
  final lo explica y pide lanzar sin esperar al ALTO). Viaja en `brake_ms / stops_ok / stops_total / brake_best_ssd_ms` →
  `GamePlayResult.brakeMs / stopsOk / stopsTotal / brakeBestSsdMs` → `GameResultScreen`: velocímetro de arcilla
  (450 ms lento → 150 ms rápido) + "Frenaste N de M altos · récord".
- Arte: `BrakeSprites` (alto, plataforma, botón), `GameWorld.LaunchBase`. Vista previa:
  `python3 tools/art-preview/freno.py <raw>` → `docs/previews/freno.png`. Probado por Ricardo (27-sep): "me gustó,
  funciona muy bien".

**Aterrizaje Lunar** (`Games/Aterrizaje/`, id `aterrizaje`, dominio cálculo): estimación en la línea numérica (Siegler
y Opfer, 2003; Siegler y Ramani, 2008; meta-análisis de Schneider et al., 2018). Arriba la misión ("Aterriza en 37",
número grande en sol, sin tarjeta); abajo la regla de arcilla crema sobre el borde de la luna, solo con los extremos
(y la marca del medio en niveles bajos). El módulo lunar baja solo; el dedo lo mueve a lo ancho (haz de puntos lima
hasta la regla) y al soltar cae rápido; se posa con polvo, crece la bandera en el blanco con el número, y se ve el
tramo "a 3" (lima si ≤5%, coral si no). "¡DIANA LUNAR!" con ≤1,2% de error.
- Reglas y pruebas: `LandingContract` / `LandingContractTests` (6). 12 niveles: 0-10 → 0-20 → 0-100 (con y sin marca
  del medio) → 0-1000 → fracciones → decimales/porcentajes → regla que no empieza en 0 → sumas → fracciones 0-2 →
  negativos (-50 a 50). Acierto = error ≤ 5% del largo. Bajada 6 → 3,5 s (Precisión 10 s). DDA `stepUp` 0.3, sin tiempo
  de reacción. Reto 120 s; Precisión 15 aterrizajes. Formato de números sin culturas del teléfono (coma decimal fija).
- Medida propia: **tu estimación** ("a X% del blanco": distancia media en % del largo de la regla; NO se llama
  "precisión numérica": en adultos la tarea con regla acotada se resuelve como juicio de proporción con puntos de
  referencia y se apoya en habilidades visoespaciales, Barth y Paladino 2011; Sullivan et al. 2011; Simms et al. 2016)
  y **tu línea**: `numline_true / numline_given` (0..1 por aterrizaje) → `GamePlayResult.numlineTrue / numlineGiven` →
  `GameResultScreen` dibuja la regla con cada blanco y dónde se posó, y `data/NumberLine.reading` (lógica pura con
  pruebas) nombra el TRAMO de la regla (inicio / centro / final) donde más se aleja del blanco, con distancia SIN signo
  (el error con signo cerca de los extremos sale sesgado por construcción), y da un truco de puntos de referencia.
  Revisión del 27-sep por pedido de Ricardo ("¿de qué sirve saber que pongo los grandes a la izquierda?").
- Arte: `LandingSprites` (módulo lunar, bandera), `GameWorld.LunarRange`. Vista previa:
  `python3 tools/art-preview/aterrizaje.py <raw>` → `docs/previews/aterrizaje.png`. Probado por Ricardo (27-sep): ok.

**Lluvia de meteoros** (`Games/Meteoros/`, id `meteoros`, dominio lenguaje; 30-sep, diseño en
`docs/diseno-lluvia-de-meteoros.md`, maqueta aprobada `docs/previews/meteoros.png`): decisión léxica "ir / no ir" (Meyer y
Schvaneveldt, 1971; Perea, Rosa y Gómez, 2002). Caen meteoros de arcilla con relieve, cráteres y brasa en el borde delantero con estela de calor (opción B, 1-oct); cada uno lleva una placa
crema con una palabra (siempre horizontal; letra Atkinson Hyperlegible Bold, 26 dp, 30 en mayores; una palabra que no cabe baja de a 1 dp, nunca de 22 dp). Se TOCAN las palabras que existen (estallan en estrellas
que vuelan a la constelación de arriba, con ✓) y se DEJAN PASAR las inventadas (se deshacen en chispas al llegar a la
atmósfera: acierto sin ruido); tocar una inventada la agrieta en polvo gris con ✗ y la palabra tachada ("esa no
existía"); una palabra que se va queda anotada ("se fue: brújula"). El toque cuenta al PRESIONAR; zona = roca + 12 dp
(mínimo 56 dp, 64 en mayores). Mundo `GameWorld.Observatory` (borde curvo de planeta + cúpula `ObservatorySprite`).
- Reglas y pruebas: `MeteorContract` / `MeteorDirector` / `MeteorTally` / `MeteorLexicon` y `MeteorContractTests` (14).
  12 niveles (bandas de palabras 1-1 → 5-6, señuelos obvios → una letra → traspuestas, 1 → 3 a la vez sin pisarse (`MeteorContract.Overlaps`: se prueba el recorrido contra los que ya caen), caída 7 → 4 s,
  largo 4-6 → 6-12). 50% palabras y 50% inventadas, nunca más de 3 inventadas seguidas; meteoro DORADO 1 de cada 12
  (palabra de 2 bandas más rara, aro sol, × 2); LLUVIA DE ESTRELLAS cada 25 (6 palabras comunes y rápidas, × 2, fuera de la
  escalera); racha ≥ 8 = estela encendida (× 1,5). DDA común `stepUp` 0.15, sin tiempo de reacción. Reto 120 s;
  Precisión 40 meteoros, caída × 1,5 y máximo 2 a la vez; mayores caída × 1,25.
- Léxico (`Resources/Lexico/meteoros_es.json`, lo escribe `python tools/lexico/meteoros.py`, determinista): 1.200 palabras
  (200 por banda 1-6, SPALEX con el mínimo de España y Latinoamérica ∩ lemas del diccionario Hunspell de LibreOffice,
  menos `tools/lexico/excluir.txt`) y ~1.500 inventadas (obvias, una letra cambiada, dos letras interiores traspuestas;
  filtradas contra el diccionario y un modelo de trigramas de letras). Los datos ajenos viven en
  `tools/lexico/fuentes/` (fuera de git). Muestra para revisar: `docs/lexico-muestra-meteoros.md`.
- Medidas propias (telemetría `lex_*` → `GamePlayResult.lex*`; lectura pura en `data/Vocabulary.kt` con pruebas):
  **tu vocabulario** (cifra grande "N de cada 10" + 3 barras comunes / intermedias / raras: % reconocidas − % de inventadas tocadas; 1-oct, mezcla de las propuestas A y C), **tu reconocimiento**
  (mediana de toque en comunes contra raras), **tu filtro** (inventadas tocadas por tipo; consejo solo si las
  traspuestas engañan más y hay ≥ 5 de cada tipo) y **tu colección** (palabras raras acertadas, preferencias
  `word_collection`, respaldada). Marca para la evolución (`StarMeasures` `vocab`): promedio ponderado del % reconocido
  en las bandas 3-6. Detalle y referencias: `docs/medidas-juegos-estrella.md`. La pantalla final se desplaza.
- Arte: `MeteorSprites` (roca con relieve y brasa en 4 formas y 5 colores, `HeatTrail` con o sin chispas, estrellas de la constelación; las rocas se hornean durante la cuenta regresiva), `ObservatorySprite`. Letra de las palabras: `UiFonts.Word` (Atkinson, SIL OFL, créditos en Ajustes → Licencias).
  Vista previa de diseño: `python tools/art-preview/meteoros.py` → `docs/previews/meteoros.png`.
- Pendiente: licencia de SPALEX para uso comercial (Ricardo pidió permiso a los autores; se avanzó como si lo dieran);
  que Ricardo revise la muestra de palabras e inventadas; probarlo en el teléfono.

**¿Verdad o disparate?** (`Games/Disparate/`, id `disparate`, dominio lenguaje; 1-oct, diseño en
`docs/diseno-verdad-o-disparate.md`, maqueta aprobada `docs/previews/disparate.png`): verificación de frases (Collins y
Quillian, 1969; Wilson y Baddeley, 1988; frases generadas por programa, Crossland, Legge y Dakin, 2008). Desde una sala de radio
llegan frases cortas en una placa crema (Atkinson Hyperlegible Bold, 24 sp, 28 en mayores, hasta 2 renglones, 3 en mayores): se
decide VERDAD (lima, ✓) o DISPARATE (coral, onda rota) con los botones (cuentan al PRESIONAR) o deslizando la placa (derecha =
verdad). Mantener presionada la placa ~0,8 s = "Esta frase no está clara" (no cuenta; el id viaja en `sv_unclear`, la app lo
guarda en `unclear_sentences`, con respaldo, y lo suma al informe de errores de Ajustes; `python tools/frases/buscar.py <id>`).
- Reglas y pruebas: `DisparateContract` / `DisparateDirector` / `DisparateTally` / `SentenceBank` y `DisparateContractTests`
  (20). 12 niveles: el tipo de frase sube cada 2 niveles (1-2 cortas, 3-4 con complemento, 5-6 negación, 7-8 con pausa
  «, que …,», 9-10 todos/algunos/ningún, 11-12 comparaciones); señal 9 → 5 s (mayores × 1,3). Reto 120 s; Precisión 30 frases
  sin señal. 50% verdad / 50% disparate con NUNCA más de 3 respuestas iguales (se garantiza al sacar, no por el orden del
  JSON); sin repetir frase y evitando las de las últimas 3 partidas (PlayerPrefs `disparate_recent`); Ráfaga cada 12
  (5 frases de 3 palabras, 4 s, fuera de la escalera y de las medidas); 10 seguidas = "Transmisión perfecta" (× 1,5). DDA común
  `stepUp` 0.15 sin tiempo de reacción. NINGÚN perfil de sesgo (verdad/disparate): regla de patentes (US 11,839,472).
- Frases (`Resources/Frases/disparate_es.json`, lo escribe `python tools/frases/disparate.py`, determinista): base de conocimiento
  propia (`conocimiento.py`: ~210 entidades con propiedades categóricas SIN excepciones, 20 grupos para los cuantificadores, 6
  escalas por niveles) y plantillas de los 6 tipos con su corrección. Las verdades usan verbos PROPIOS (nunca "crecen/respiran");
  la frase con pausa lleva una explicativa siempre verdadera y DESCRIPTIVA (qué tiene, dónde vive, cómo es) de otra familia que el predicado final, sin repetir verbo, "sirve para" ni palabras; disparates evidentes (cruzan categorías) y sutiles; tope de 50
  caracteres; abecedario ≤ 15% del tipo 6; id estable por frase (SHA-1). Muestra para revisar: `docs/frases-muestra-disparate.md`.
- Efectos (docs, sección "Efectos y fluidez"): la frase sintoniza (estática → letras en ~200 ms; el reloj de respuesta arranca recién
  al quedar nítida), la siguiente se prepara mientras se lee (transición < 300 ms), señal que se vacía con temblor y estática en los
  bordes, acierto con onda de la antena + chispas + medidor de 5 barras, error con ✗ y la corrección en su lugar, señal perdida,
  ráfaga con estrellas veloces y aurora a las 10 seguidas. Con "quitar animaciones": sin estática, temblor ni aurora.
- Medidas propias (telemetría `sv_*` → `GamePlayResult.sv*`; lectura pura en `data/Reading.kt` con pruebas): **tu lectura con
  comprensión** (palabras por minuto, mediana de palabras ÷ tiempo en los aciertos; -1 con < 10), **qué te frena** (tiempo medio por
  tipo con ≥ 4 aciertos, la más lenta marcada con texto y un truco por tipo), **tu precisión** (total y disparates sutiles) y
  **tu mejor racha**. Marca para la evolución (`StarMeasures` `wpm`): palabras por minuto. Detalle y referencias:
  `docs/medidas-juegos-estrella.md`. Captura real: `docs/previews/disparate-final-real.png`.
- Arte: `DisparateSprites` (antena de arcilla, íconos ✓ y onda rota, cinta de luz, estática), `DisparateSounds`,
  `GameWorld.Radio`. Maqueta: `python tools/art-preview/disparate.py` → `docs/previews/disparate.png`.
- Pendiente: que Ricardo lo pruebe en el teléfono (fluidez, deslizar, mantener) y revise la muestra de frases.

**Cosecha de palabras** (`Games/Cosecha/`, id `cosecha`, dominio lenguaje; 1-oct, diseño aprobado en
`docs/diseno-cosecha-de-palabras.md`, maqueta `docs/previews/cosecha.png`): fluidez verbal con 7 letras fijas (producir palabras bajo
presión de tiempo; Troyer, Moscovitch y Winocur, 1997, solo como DESCRIPCIÓN de cómo se busca). Siete fichas-luna de arcilla (letra
Atkinson Hyperlegible Bold 30 sp, 34 en mayores, toque mínimo 64 dp, cuenta al PRESIONAR) giran en una órbita elíptica (una vuelta
cada ~40 s; quietas con "quitar animaciones") alrededor de un planeta de tierra; tocar letras en orden las enciende con su número y
las pasa a la bandeja, tocar la última de la bandeja la devuelve, "Sembrar" (lima) siembra y "Borrar" vacía. 3 cosechas por partida
(Reto 60 s, Precisión 90 s), cada una con letras y planeta nuevos; entre cosechas, 3 s de "¡Cosecha lista!".
- Reglas y pruebas: `CosechaContract` / `CosechaSession` (la bandeja y lo sembrado) / `CosechaTally` (medidas) / `CosechaBank` /
  `Garden` (`HuertoLayout.cs`: dónde brota cada planta) con `CosechaContractTests` (26, NUnit puro) y `CosechaBankTests` (7: el archivo
  real exige 600 rondas, 12 comunes y una estrella común cada una). Validación SIN tildes (la ficha "a" vale para "á"; la ñ es su
  letra) y la palabra se muestra con su tilde. Puntos por largo (3 → 10 … 7 → 100; rara × 1,5; estrella × 3). Palabras ocultas
  (`oculta`): se aceptan (planta, puntos) pero no entran en la lista, en "también podías" ni en las medidas. Pista tras 15 s sin
  sembrar (10 en mayores): ilumina la primera letra de la palabra común MÁS CORTA que falta. Cosecha lograda = ≥ 35% de las comunes
  de la ronda; DDA común (`stepUp` 0.5, una decisión por cosecha, sin tiempo de reacción, mayores con rondas de más comunes).
  Rondas: `PlayerPrefs cosecha_recent` evita las de las últimas 5 partidas.
- Rondas (`Resources/Lexico/cosecha_es.json`, lo escribe `python tools/cosecha/rondas.py`, determinista y estable: conserva las que
  siguen cumpliendo): 600 rondas en 10 niveles (60 por nivel; comunes disponibles ~37 → ~14), palabras de SPALEX (≥ 80%) con sus
  formas generadas por Hunspell es_ES (plural, femenino, presente, pretérito, imperfecto, gerundio, participio). 17 pruebas en
  `tools/cosecha/test_rondas.py`. Palabras ocultas por pedido de Ricardo: `tools/lexico/excluir.txt` (bloque «Ampliación del 1-oct»);
  "cola" y "pasta" se dejaron visibles. Muestra: `docs/cosecha-muestra.md`.
- El huerto (`CosechaSprites`, ver docs sección 8): planeta de tierra de arcilla con vetas, relieve y brillo de atmósfera, pasto ralo y
  3 brotes al inicio, y una planta por palabra (brote, flor, tulipán, arbusto, girasol, hongo; tamaño según el largo; flor dorada si
  es rara); la palabra estrella = árbol dorado con destello y "× 3". La semilla cae en arco y brota con rebote; las letras de la
  bandeja vuelan hacia ella. Sonido (`CosechaSounds`): cada letra sube una nota de la pentatónica, sembrar = plop + campanita,
  brotar = cuerda suave, repetida o no válida = madera sorda, 3 palabras en < 10 s = brillo de la órbita. `GameWorld.Huerto`.
- Medidas propias (telemetría `harv_*` → `GamePlayResult.harv*`; lectura pura en `data/Harvest.kt` con pruebas): **tu cosecha**
  (palabras y "de las comunes, N de M"), **tu manera de buscar** (% de racimos frente a saltos con ≥ 10 palabras: racimo = comparte
  las 2 primeras letras o la raíz con la anterior; "cómo buscaste en esta partida", sin normas), **tu ritmo** (primeros contra
  últimos 20 s, promedio de las cosechas), **tu palabra estrella** (o la más larga) y **también podías** (5 comunes). Marca para la
  evolución (`StarMeasures` `harvest`): % de las comunes encontradas. Captura real: `docs/previews/cosecha-final-real.png`.
- Créditos: Ajustes → Licencias y créditos cita SPALEX y el diccionario Hunspell es_ES (MPL 1.1 / LGPL / GPL, a elección).
- Pendiente: que Ricardo lo pruebe en el teléfono (órbita, tamaño de las fichas, ritmo de las cosechas, sonidos, la pista).

**Acoplamiento** (`Games/Acoplamiento/`, id `acoplamiento`, dominio razonamiento): rotación mental (Shepard y Metzler,
1971; Cooper y Shepard, 1973; meta-análisis de Uttal et al., 2013). Abajo el puerto de la estación con el hueco de una
pieza (poliominó quiral al azar); arriba llega el módulo girado, que es la pieza o su reflejo. Botones grandes
"ENCAJA" (lima) y "ESPEJO" (uva), con texto e ícono; la respuesta cuenta al PRESIONAR. Siempre se muestra la verdad:
el módulo gira hasta quedar derecho; si era el reflejo se da vuelta como un espejo (escala x 1 → −1); si encajaba,
baja al puerto (clunk, puerto lima) y se suma a "tu estación" (fila de arriba, crece en la partida).
- Reglas y pruebas: `DockingContract` / `DockingContractTests` (7): piezas conexas y quirales (`IsChiral` compara con
  los 4 giros del reflejo), 12 niveles: 4 → 7 bloques, giro máximo 90/135/180°, ángulos de a 45° y desde el 5 de a
  15°, "combustible" (tiempo para decidir) 6 → 2,5 s en Reto (Precisión 12 s, 24 módulos). DDA `stepUp` 0.3 SIN
  modular por tiempo (el tiempo es la medida).
- La pieza se hornea ENTERA en cada intento (`DockingSprites.PieceSprite`: SDF de bloques + "puentes" entre vecinos,
  una sola silueta; juntas tenues y remaches simétricos, que no delatan la orientación). La sombra dura va aparte
  (silueta en tinta, en un contenedor que no gira): cae siempre hacia abajo aunque la pieza gire.
- Medida propia: **tu giro mental** (`RotationSpeed`: recta de mínimos cuadrados del tiempo contra el ángulo en los
  aciertos; 1000 / pendiente = grados por segundo; -1 con menos de 8 aciertos, menos de 3 ángulos o menos de 70% de
  aciertos en total: con mucho azar la curva sale plana y parecería un giro rapidísimo) y **tu curva de
  giro** (tiempo medio a 0/45/90/135/180°). Viajan en `rotation_speed_dps / rotation_curve_ms` →
  `GamePlayResult.rotationSpeedDps / rotationCurveMs` → `GameResultScreen`: "Tu giro mental: N° por segundo" y cinco
  columnas uva con el tiempo encima y el ángulo debajo.
- Arte: `DockingSprites`, `GameWorld.DockingBay` (cielo quieto: nada gira en el fondo). Vista previa:
  `python3 tools/art-preview/acoplamiento.py <raw>` → `docs/previews/acoplamiento.png`. Probado por Ricardo (27-sep).

**Tráfico Estelar** (`Games/Trafico/`, id `trafico`, dominio razonamiento): ruteo con desvíos (mecánica genérica;
nombre, arte y medidas propios), atención dividida y planificación bajo presión de tiempo, en la línea de la tarea de
control de tráfico aéreo de Kanfer y Ackerman (1989). De la compuerta de una estación de carga (cúpula crema sobre
plataforma azul, arriba; reemplazó al "vórtice" que no le gustó a Ricardo) salen cápsulas de colores por rutas CURVAS;
tocando los desvíos (discos con flecha que apunta hacia donde sale la ruta activa; alcance de toque 100 u) cada una
debe llegar al planeta-puerto de su color Y su símbolo (8 pares color + forma: corazón, estrella, rombo, triángulo,
luna, cruz, cuadrado, aro). Rutas activas: riel celeste con resplandor y luces que corren en el sentido del viaje;
las otras, riel de arcilla apagado (visible para planificar). Cápsulas con estela de su color.
- Reglas y pruebas: `TrafficContract` + `TrafficNetwork` + `TrafficSim` (simulación pura: las cápsulas avanzan por el
  RECORRIDO de cada tramo, `PathX/PathY`, a velocidad constante; toman la salida activa AL LLEGAR al desvío; mover un
  desvío después no cambia a la que ya pasó) / `TrafficContractTests` (11). La red es un árbol: puertos en una "U" con
  algo de desorden, el grupo se parte al azar en dos, cada desvío entre el portal y el centro de sus puertos; `Relax`
  separa desvíos pegados; se acepta si no hay cruces, nodos a < 0,14 anchos, rutas rozando nodos (0,08) ni rutas
  pegadas (0,055). Después `CurveRoutes`: cada tramo sale del desvío girado ±38° respecto de la ruta que llega (como un
  cambio de vía) y llega en Bézier; según `Twist(nivel)` la ruta de la estación es una "S" (niveles 1-3) o una cornisa
  que va y vuelve con medias vueltas redondas (`Switchback`: 2 tramos desde el 4, 3 desde el 8), y algunos tramos
  largos ondulan. Cada curva se acepta solo si no cruza ni roza nada y su radio de giro es ≥ 0,035; si no, una más
  suave, y en el peor caso la recta.
- Ritmo "lento y lleno" (Ricardo, tras probarlo: iban "casi una a la vez" y muy rápido; el juego de trenes de
  Lumosity le parecía mejor): lo que sube con el nivel es CUÁNTAS van a la vez (`TargetInFlight` 2,5 → 7,5), no el
  apuro. Velocidad 0,09 → 0,18 alturas/s (viaje ~8-9 s); cada cuánto sale una = viaje medio de ESTA red
  (`MeanRouteLength` / velocidad) / `TargetInFlight`, mínimo 0,9 s (Precisión: 20% más lento y 30% más espaciado, 30
  cápsulas). Flujo continuo: la pantalla no se vacía; 10 entregas seguidas sin error = "¡Serie perfecta!" +200. Si
  cambia la cantidad de puertos (y la red lleva ≥ 20 s), deja de sacar, se entregan las que van y la red se rearma.
  12 niveles: puertos 2 → 8. DDA `stepUp` 0.2 por cápsula entregada, sin tiempo de reacción. Reto 120 s.
- "Próximas" (propio, no está en el juego de trenes): las 3 que vienen esperan a la derecha de la compuerta (rótulo
  suelto "próximas", la primera más grande); se puede preparar la ruta antes de que salgan (`Spawn(..., announcedAt)`:
  mover un desvío mientras espera cuenta para "tu anticipación"). **Cápsula urgente** desde el nivel 5 (8% → 18%,
  nunca dos seguidas): aro sol que late, un poco más grande, ×1,35 de velocidad, vale el doble, salida con dos
  campanas; la primera vez, aviso "¡Cápsula urgente!".
- Sonido (`TrafficSounds`, sintetizado) con la identidad de la app, NO arcade (Ricardo probó una versión arcade y la
  rechazó: "debe ser mejor y asociado a lo que queremos en la app"): marimba y campana en la pentatónica de do de
  `GameFeel`, así todo suena afinado entre sí. Vuelo en bucle (fuente aparte `_engine`, colchón do + sol con soplo
  suave que respira) solo mientras hay cápsulas en viaje: más cápsulas = algo más de volumen (el tono NO cambia), y se
  corre a izquierda/derecha según dónde van. Salida = soplo + marimba grave; desvío = golpecito de madera (mi/sol según
  el lado); al pasar por un desvío, campanita con la NOTA DEL COLOR de la cápsula (color + símbolo + nota: el tráfico
  arma una melodía); entrega = "pling" de racha de `GameFeel` + marimba grave del color; error y subir de nivel =
  los de `GameFeel`; serie perfecta = lluvia de campanas; urgente = salida con dos campanas agudas. Respeta sonido apagado y pausa. Muestra escuchable:
  ArtPreview vuelca `trafico-sonidos.wav` (copia en `docs/previews/`; los sonidos de `GameFeel` van replicados ahí).
- Dibujo: `RailLine` (malla propia de la UI que sigue los puntos; sección de textura con bordes suaves: arcilla con
  borde tinta, cinta pareja o resplandor). Por tramo: resplandor, sombra dura, riel y línea de luz, cada uno en su capa.
- Medida principal: **tu carga** (`CleanPeakLoad`: la mayor cantidad en viaje a la vez en un momento LIMPIO, sin
  ninguna mal entregada en viaje; cada error ensucia desde que esa cápsula se vio hasta que llegó; muestras cada 0,5 s).
  Viaja en `traffic_peak_pods` → `GameResultScreen`: "Tu carga: N cápsulas a la vez" + 8 cápsulas que se encienden.
  Además **tu anticipación** (mediana de cuánto antes de que pase la cápsula se movió el desvío que la mandó
  bien; solo desvíos movidos para ella) y **planificas / a último momento** (% con ≥ 1 s: control proactivo vs reactivo,
  Braver 2012). Viajan en `traffic_lead_ms / traffic_proactive_pct / traffic_peak_pods` →
  `GamePlayResult.trafficLeadMs / trafficProactivePct / trafficPeakPods` → `GameResultScreen`: barra partida lima/sol.
- Arte: `TrafficSprites` (estación, puertos, cápsulas, desvío), `GameWorld.TrafficHub`. Vista previa (redes y curvas
  reales de `BuildNetwork` que vuelca ArtPreview): `python3 tools/art-preview/trafico.py <raw>` →
  `docs/previews/trafico.png`. Ricardo vio la primera maqueta (27-sep): "muy rígida, poco llamativa", pidió curvas y
  cambios de sentido → rehecho así. Probado por Ricardo (27-sep): le gustó la estructura; pidió que la nave suene al viajar (ver Sonido) y más
  trabajo simultáneo (ver Ritmo). Versión "lento y lleno" sin probar en el teléfono.

**Bitácora de Misión** (`Games/Bitacora/`, id `bitacora`, dominio MEMORIA; primer juego estrella de memoria, 28-sep,
pedido de Ricardo: "muy innovadora, con enganche, llamativa, sonidos modernos, destellos"): memoria episódica (qué,
dónde y en qué orden) con recuerdo DIFERIDO, como las pruebas de aprendizaje y recuerdo diferido (tipo RAVLT). Ninguna
app de la competencia mide memoria con demora a lo largo de la sesión.
- Partida: **transmisión** (una sonda recorre planetas del mapa, los de Tráfico Estelar con color + símbolo + nombre:
  Coral, Sol, Cielo...; en cada parada aparece un hallazgo con destello, su nota y "Planeta Sol · una llave"; la
  persona lo TOCA para guardarlo en la bitácora, fila de abajo; si no, se guarda solo a los 5 s) → **primer repaso**
  (planeta por planeta, en otro orden, elegir el hallazgo en un cajón con los de la misión + señuelos; respuesta al
  instante: se aprende) → **espera** → **informe** (lo mismo sin ayuda) → **la ruta** (tocar los planetas en el orden de
  la sonda) → **revelación** (la sonda repite la ruta; ✓ lo recordado vuela a la bitácora, ✗ muestra lo que era).
  Consejo del juego: imaginar el hallazgo EN su planeta como una escena (Bower, 1970).
- Fases por configuración (`SequenceConfigDetails.memory_phase / memory_seed / memory_level / memory_elapsed_s`):
  "" = completa (la espera es una **patrulla** de 45 s atrapando cometas, que ocupa la atención sin repasar);
  "encode" = transmisión y repaso; "recall" = informe. La misión se rearma con la semilla (`MissionRng` xorshift propio:
  igual en cualquier versión) y el nivel.
- **Misión del día** (app, `data/MissionLog` + `MissionLogStore` en SharedPreferences `mission_log`). Desde el 29-sep
  va APARTE de la sesión diaria (Ricardo: el botón decía un juego y abría otro, y jugó 5 en vez de 3): ya no se
  intercala sola. Se abre tocando la línea de la Bitácora en Hoy (`MissionLine`: transmisión / espera con minutos /
  informe listo / al día con la colección); el informe se abre al terminar la sesión o pasados 10 min, y uno pendiente
  de otro día mide memoria a un día. Bitácora no entra al camino de 3 (`BOOKEND_GAMES`). La transmisión sola NO se guarda en Room (no es partida completa): muestra su pantalla y sigue.
- Reglas y pruebas: `BitacoraContract` / `BitacoraContractTests` (5). 10 niveles: paradas 3 → 8, planetas extra que la
  sonda no visita (0, 1 desde el 3, 2 desde el 6), señuelos 2 → 5. DDA `stepUp` 0.5 por parada del informe.
- Medidas: **tu memoria a los X minutos** (paradas recordadas en su planeta), **retención** (de lo aprendido en el
  repaso, cuánto seguía en el informe: la app la calcula con la máscara guardada, `MissionLog.retentionPct`), **la
  ruta** (paradas en su lugar), **hallazgos que no estaban** (intrusiones, dicho sin culpa) y **tu bitácora**
  (colección acumulada). Telemetría `mem_*` → `GamePlayResult.mem*` → `GameResultScreen` (`FilledSlots`).
- Arte: `BitacoraSprites` (16 hallazgos de silueta distinta y fácil de nombrar: llave, campana, pluma, concha, reloj de
  arena, brújula, farol, corona, bellota, libro, copa, gema, hongo, ancla, estrella de mar, paraguas; y la sonda),
  `GameWorld.Logbook`. Sonido `BitacoraSounds`: cristal y "destellos" (parciales muy agudos) con eco suave, en la
  pentatónica de la app; cada parada con su nota (la misión suena como melodía). Vista previa:
  `python3 tools/art-preview/bitacora.py <raw>` → `docs/previews/bitacora.png`; sonidos → `docs/previews/bitacora-sonidos.wav`.
  Sin probar en el teléfono.

**Rumbo a Casa** (`Games/Rumbo/`, id `rumbo`, dominio MEMORIA; juego estrella de orientación, 28-sep, elegido por
Ricardo entre varias propuestas: "calidad, enganche, teoría y que se diferencie del mercado"): INTEGRACIÓN DE TRAYECTO,
volver al punto de partida sin verlo (tarea de completar el triángulo: Klatzky et al., 1990; Loomis et al., 1993;
células de red, Hafting et al., 2005; realidad virtual: Howett et al., 2019; Sea Hero Quest, Coutrot et al., 2018). Ni
Lumosity, Peak, Elevate ni NeuroNation tienen algo así. En la app NO se nombran enfermedades.
- Vista de CABINA: la nave fija un poco abajo del centro, mirando hacia arriba; el mundo (`_pivot` gira/escala,
  `_content` lleva las coordenadas del mapa) gira y pasa alrededor. Polvo de estrellas propio (150 puntos que se
  envuelven alrededor de la cámara) = flujo óptico; niebla redonda (`HomingSprites.Fog`: se ve hasta 380, nada desde
  520). Fondo `GameWorld.DeepSpace` SIN estrellas (serían una brújula).
- Ida: aparece la SEÑAL del próximo cristal en el borde de la vista (anillo del color del cristal); se toca (o sola a
  los 8 s), la nave gira hacia él (80 → 150°/s) y vuela a velocidad FIJA (300 u/s, igual en ida y vuelta: el paso del
  polvo mide la distancia). 2 → 5 tramos (el primero sale derecho de la base). Vuelta: "¿Hacia dónde está casa?"
  (dial con 12 marcas que gira con la nave + flecha sol; tocar/arrastrar) → FIJAR RUMBO → la nave avanza → ¡AQUÍ!
  (si pasa 2,2 × la distancia: "Sin combustible"). Revelación: la cámara se aleja hasta el mapa con el norte arriba;
  ida lima, vuelta sol, vuelta justa crema, tramo que faltó; ✓/✗ + "Rumbo: 14° a la derecha · distancia justa".
- **Faro** en la mitad de los viajes (de a pares, orden al azar): estrella sol lejanísima que solo cambia de lugar al
  girar (en el infinito); da el rumbo, no la posición. Así el final compara con faro / sin faro.
- Reglas y pruebas: `HomingContract` / `HomingContractTests` (7). `NextTrip` acepta el viaje si la vuelta mide 820-2600,
  la ruta no vuelve cerca de la base tras la primera parada (600), las paradas quedan separadas y no se cruza.
  `Evaluate` separa rumbo y distancia. Llegada = a ≤ 35% de la distancia a casa; perfecta ≤ 12%. DDA `stepUp` 0.5 por
  viaje, sin tiempo de reacción. 10 niveles. Reto 150 s (termina el viaje en curso); Precisión 8 viajes. 60 cuadros/s.
- Medidas: **tu brújula interna** ("a X% de casa") y **tus llegadas** (diana alrededor de la base: encima = te
  pasaste, debajo = corto, a los lados = rumbo). Telemetría `homing_error_pct / homing_along / homing_lateral /
  homing_beacon / homing_perfect` → `GamePlayResult.homing*` → `GameResultScreen` (`HomingTarget`); la lectura
  (`data/Homing.kt`, 5 pruebas): desvío medio del rumbo, distancia (justa / corto / largo / varía, desde 4 viajes y
  70% para el mismo lado), qué aleja más de casa (rumbo o distancia, 1,5 veces y 5 puntos) con un truco, y faro vs
  sin faro (3+ de cada uno, 8° de diferencia).
- Sonido `HomingSounds` (identidad de la app): la ida sube nota a nota (un cristal = una nota) y la llegada "vuelve a
  do": justo en casa, acorde completo con destellos; cerca, do y sol; lejos, acorde en suspenso. Ping de sonar con eco
  para la señal, soplo al girar, madera al fijar rumbo; vuelo con el colchón de `TrafficSounds.EngineLoop`.
- Arte: `HomingSprites` (nave con y sin sombra, base en anillo con casita, 5 cristales, faro, flecha, dial, niebla).
  Vista previa: `python3 tools/art-preview/rumbo.py <raw>` → `docs/previews/rumbo.png`; sonidos →
  `docs/previews/rumbo-sonidos.wav` y `rumbo-sonidos-llegadas.wav` (las tres llegadas). Sin probar en el teléfono:
  revisar si marea el giro, si se entiende tocar la señal y el dial, y el ritmo (~25 s por viaje).

**Constelación de Palabras (DESCARTADA, 28-sep)**: fluidez verbal por voz (reconocedor de Android, puntuación de
Troyer: agrupar y saltar). Funcionaba, pero a Ricardo no le convenció: la voz no se sentía fluida (palabras que no
reconocía, pausas), incluso tras la "opción A" (escucha continua, lista al reconocedor, parecidos al oído). Se sacó
todo, también el permiso de micrófono. El código completo queda en el historial de git (commit `caf19b8`: juego Unity,
`SpeechBridge.kt`, prueba de voz, léxico de ≈700 palabras). Lección para lenguaje: nada de voz; interacción táctil y fluida.

**Primer Contacto (DESCARTADO, 28-sep)**: aprender palabras de un idioma extraterrestre deduciéndolas de escena en escena
(aprendizaje entre situaciones, Yu y Smith 2007), con voz sintetizada propia (`NuriVoice`, formantes) y diccionario que
crecía día a día. Ricardo: la voz y la fluidez bien, pero "poco entendible"; ni con guía de la primera vez y pistas le
convenció ("una persona que no lo entienda no lo vuelve a jugar"). Código en el historial de git (commit `20eb246`).
Lección (tras Constelación y Primer Contacto): nada de juegos "pesados" de explicar; lo que funciona es lo de Piloto
Estelar: movimiento continuo, se entiende al instante, enganche inmediato. Queda de esa etapa: `Toast.FitSize` cuenta
los renglones reales (los avisos largos ya no se salen del recuadro, en todos los juegos).

**Correo Estelar** (`Games/Correo/`, id `correo`, dominio MEMORIA; 28-sep, elegido por Ricardo entre 4 propuestas con el
movimiento de Piloto Estelar; maqueta aprobada antes de programar: `tools/art-preview/correo.py` →
`docs/previews/correo-estelar.png`): MEMORIA PROSPECTIVA, acordarse de hacer algo en el momento justo (Rummel y
Kvavilashvili, 2023). Nadie en la competencia la mide.
- Hoja de ruta antes de salir (sin recuadros: ícono + texto; "durante el vuelo no los verás"; truco de intención de
  implementación, Chen et al. 2015) y botón "¡A volar!". Vuelo de 150 s con la ruta de Piloto (reusa `PilotContract`:
  ancho, curvas, velocidad × 0,85; sin piloto automático) y sobres sobre la ruta (+10, tarea en curso).
- Por lugar: planetas-puerto de Tráfico (`TrafficSprites.Port`) a los costados de la ruta; tocar el del color del
  encargo = el paquete vuela en arco, ✓ "¡Entregado!"; otro = ✗ "No es de tu encargo"; si se va = "Se fue sin su
  paquete" (suave). Por hora: radio abajo a la derecha, ventana ±5 s ("¡Aviso recibido!" / "Aún no es la hora" / "Ya
  pasó la hora"; al cerrarse sin aviso, "Se pasó la hora del aviso"); reloj tapado arriba a la derecha, se destapa 1,6 s.
- Reglas y pruebas: `MailContract` / `MailContractTests` (6): 10 niveles; los dos tipos de encargo desde el nivel 1;
  2 colores desde el 4; radio cada 30 s (25 desde el 5, 20 desde el 8); parecidos desde el 2 (15% → 45%; pares
  coral/amarillo → naranjo, celeste → menta, lila → rosado); ~23% de planetas del encargo, nunca dos seguidos; un
  planeta cada 3,6 → 2,4 s. DOS dificultades, como en Piloto: la de encargos (`stepUp` 0.5, cada entrega, planeta
  perdido, error u hora con o sin aviso; los encargos se fijan al salir) y la de pilotaje (`stepUp` 0.14, ventanas de
  1,5 s con ≥ 85% en la ruta y sin chocar: velocidad, curvas, ancho, asteroides).
- Versión "con aceleración" (28-sep, Ricardo lo probó: "demasiado lento, monótono, sin dificultad; a nivel gráfico,
  excelente"; y un punto blanco tapaba "¡A volar!" = la píldora de estado vacía, ahora aparece al despegar): el vuelo
  tiene 3 TRAMOS (aviso "Tramo 2 de 3 · ¡La ruta se acelera!", destello e hiperespacio); a lo largo del vuelo la
  velocidad sube × 1 → × 1,5, las curvas × 1,5, el ancho × 0,85 y los planetas y asteroides salen 30-35% más seguido.
  **Asteroides** sobre la ruta (cargados a un lado, siempre hay por dónde pasar; `SymbolSprite` Asteroid): chocar =
  golpe sordo, la nave tiembla, "¡Asteroide!" (no quita encargos). El sobre suena siempre igual (la escala que subía
  y bajaba irritaba).
- **Escudo** (28-sep, idea de Ricardo: "que el cohete se vaya dañando", para cuidar la nave): 3 segmentos arriba a la
  izquierda (`ShipShield` en el contrato, 1 prueba). Cada choque rompe uno (cristal que se quiebra) y la nave se ve
  dañada (`MailSprites.ShipDamage`: grietas; con 1 segmento, humo); 20 s sin chocar reparan uno. Sin escudo:
  "¡Reparación de emergencia!" 3,5 s (nave a la mitad de velocidad y parpadeando, sin sobres, los asteroides la
  atraviesan, el DDA de pilotaje no cuenta) y sigue con 1 segmento. NUNCA termina el vuelo: los encargos necesitan los
  150 s para medirse igual (decisión razonada con Ricardo). Al final: "Nave intacta el N% del vuelo · reparaciones"
  (`Mail.shipMessage`; telemetría `mail_hull_intact_pct / mail_emergencies`). Lámina `docs/previews/correo-escudo.png`.
- Medida: **tu memoria para lo pendiente** (por lugar / por hora, `FilledSlots`), errores, **el reloj** (miradas y
  cuántas en el último 30% del intervalo) y lugar contra hora con consejo (`data/Mail.kt`, 4 pruebas). Telemetría
  `mail_*` → `GamePlayResult.mail*` → `GameResultScreen` (más "esquivaste N de M asteroides").
- Arte `MailSprites` (sobre, paquete, radio, reloj tapado/destapado), sonidos `MailSounds` (marimba y campanas: sobre,
  entrega, error, perdido, radio a tiempo/destiempo, tic-tac del reloj); muestra `docs/previews/correo-sonidos.wav`.
  Sin probar en el teléfono.

## Reglas que no se rompen

- **Diseño**: "noche + arcilla": cielo nocturno animado (**fondo C**, 30-sep, elegido por Ricardo en
  `docs/previews/fondo-oscuro.png`: #02030F → #050823 → #0A0F33 en `CosmosBackground`, `nv_night`, `UnityLoadingOverlay` y
  `NeuroStyle.Night*`; nebulosas de la app −40%; barra de pestañas en tinta #14112E con la elegida en pastilla uva); lo tocable es arcilla (borde tinta grueso `Ink` 0x1A1240,
  sombra dura, colores Coral/Sun/Sky/Grape/Lime/Cream). **Tipografía** (28-sep): `AppFamily` elige por peso: normal y
  medio = Nunito (subtítulos, comentarios, texto secundario), seminegrita y negrita = Fredoka (títulos, botones,
  números); los títulos de `Typography` son siempre Fredoka. Fuentes OFL en `res/font`. **Tamaños**: pensando en
  adultos mayores, ningún texto bajo 14 sp (13 solo en la barra de pestañas y rótulos de gráficos). Las pantallas principales NO usan
  recuadros para informar (texto suelto, objetos, líneas finas); tarjetas solo en diálogos. Nada de emojis como
  íconos; acierto/error nunca solo por color (forma o texto); contraste ≥ 4.5:1; respetar "quitar animaciones",
  sonido y vibración apagados. Usar la skill `ui-ux-pro-max` (`.claude/skills/`) para decisiones de diseño.
- **Juegos Unity**: medir el tiempo con `GameClock.Time/DeltaTime` (no `Time.unscaled*`), para que la pausa funcione.
  El arte se hornea en sprites con `ClayRaster` (`Image.color` blanco); la sombra dura cae siempre hacia abajo.
- **Textos**: español, cercanos, sin culpa ni promesas de salud ("no es un examen", nada de "fortalece neuronas").
  **Medidas del final de los juegos estrella** (revisión 27-sep): nombrar lo que se mide de verdad (no un rasgo:
  "tu estimación", no "tu precisión numérica"), decir cómo leerlo, no sacar conclusiones de pocos ensayos, dar un
  consejo concreto cuando se pueda, y comparar con estudios solo si la condición es comparable. Al pie va la nota
  común "Medida de esta partida... No es un diagnóstico". Justificación de cada medida, con referencias:
  [`docs/medidas-juegos-estrella.md`](docs/medidas-juegos-estrella.md). Al crear un juego estrella nuevo, agregar ahí su medida.
  Percentiles y comparaciones se rotulan "estimación provisional" (referencia media 0.45, sd 0.20: supuesto, no dato).
- **Licencias**: mecánicas genéricas, pero nombres, arte, textos y sonidos propios (no copiar a Lumosity & co.).
- **Decisiones de Ricardo**: servidores, cuentas, Firebase, suscripciones y requisitos de tiendas se dejan para el
  FINAL. La app debe ser masiva, social y motivadora (ligas, logros, compartir), no clínica.

## Cuando Android cierra la app durante un juego

Con Unity al frente, Android puede cerrar el proceso de la app (pasa en el Motorola de Ricardo). Por eso lo que el
flujo necesita vive en disco (`bridge/GameSessionStore`): la partida en curso con su launch id (la anota
`onUnityLaunched`), la evaluación en curso, y el resultado que llegue por broadcast sin ViewModel vivo (queda
pendiente y lo procesa el ViewModel al volver: `processPendingResult`, en `init` y en `onReturnedFromGame`).
`onUnityResult(result, launchId)` busca la sesión viva o la guardada. Solo las partidas sin sesión (botones Debug)
se guardan directo. Para reproducirlo: Opciones de desarrollador → "No conservar actividades".
Estos recorridos están cubiertos por `flow/GameFlowTest` (ViewModel y base de datos de verdad, sin Unity) y
`bridge/GameSessionStoreTest`: si se toca `NeuroVidaViewModel` o `GameSessionStore`, correrlas antes de instalar.

## Respaldo, registro de errores y verificación automática (29-sep)

- **Respaldo** (`res/xml/backup_rules.xml` para Android ≤ 11 y `data_extraction_rules.xml` para 12+, misma lista): solo
  el PROGRESO (base de datos `neurovida_database` + su `-wal`, y las preferencias `skill`, `star_measures`,
  `progress_log`, `league_events`, `achievements`, `mission_log`, `profile_extra`). NO el estado pasajero
  (`paused_game`, `game_session`, `unity_results`, `library_focus`): restaurarlo en otro teléfono dejaría una partida
  fantasma. Antes eran las plantillas de AI Studio (Android respaldaba todo sin decidirlo). **Si se agrega un archivo de
  preferencias nuevo**, `BackupRulesTest` falla hasta que se decida: sumarlo a las dos listas XML o a `transient` de la prueba.
- **Registro de errores en el teléfono** (`diag/ErrorLog`, sin servidores): guarda en `files/errores.txt` (tope ~96 KB)
  los cierres inesperados (app y `:unity`, Java; un cierre nativo del motor no pasa por ahí) y los avisos que antes solo
  iban al logcat: sobre todo un resultado de Unity que no se pudo leer y por eso NO se guardó (`NativeReceiver.parse`).
  Ajustes → "Enviar informe de errores" lo comparte como texto (versión + teléfono + lo registrado; sin nombre ni
  resultados). Para un error nuevo que valga la pena anotar: `ErrorLog.record("ETIQUETA", "qué pasó", e)`.
- **Migraciones de Room**: `data/local/MigrationTest` prueba 10→11 con datos reales y exige una `Migration` por cada
  versión exportada en `app/schemas/`. Al subir `version`: entidad → `Migration(N, N+1)` en `NeuroVidaDatabase.MIGRATIONS`
  → compilar (genera `schemas/<N+1>.json`) → correr las pruebas. Los esquemas van en los assets de la variante debug
  (solo para esta prueba; la versión de tienda no los lleva).
- **Verificación automática** (`.github/workflows/verificar.yml`, `tools/unity-falso.sh`): en cada push y pull request
  GitHub compila el C# de los juegos sin Unity y compila la app con sus pruebas Kotlin (con un unityLibrary falso).
  Un ✗ rojo en el commit = algo no compila o una prueba falló. NO corre las pruebas de Unity, el arranque de los 22
  juegos ni el export: eso sigue siendo `verificar-todo.sh` en el PC de Ricardo. Ricardo confirmó que corre en verde
  (pestaña Actions); usa `ubuntu-24.04` fijo (no `latest`) y acciones en su versión actual.
- Lo que NO se hizo de la revisión de arquitectura (medidas genéricas, idiomas, dividir el ViewModel, datos en dos
  lugares, versión de tienda, controladores grandes de Unity) está con pasos concretos en
  [`docs/plan-mejoras-arquitectura.md`](docs/plan-mejoras-arquitectura.md).
- **Competencia** (29-sep): Ricardo guardó capturas de Lumosity, NeuroNation y Peak en `Proyectos/pantallazos …/`
  (fuera del repo). Qué tomar, qué no copiar y el orden sugerido (registro de sueño y ánimo, tutorial por juego,
  comentarios por juego, escudo de racha, atajos Reforzar/Rápido…) está en
  [`docs/analisis-competencia.md`](docs/analisis-competencia.md). Nada de eso está implementado todavía.

## Pruebas

- Kotlin: 180 (`./gradlew.bat testDebugUnitTest`; lógica pura en `app/src/test/.../data`, `model`, `notification`; los
  recorridos del ViewModel en `flow/`, lo guardado en disco en `bridge/`, migraciones y respaldo). Las pruebas con
  Robolectric que crean el ViewModel usan `TestSupport` (suelta el singleton de la base entre pruebas y espera a que
  el hilo principal publique el resultado).
- Unity EditMode: 252 (contratos de cada juego, `AdaptiveDifficultyTests`, Parejas, perfil por edad) + 22 smoke tests.
- Frases de ¿Verdad o disparate?: 32 pruebas en `tools/frases/test_disparate.py` (`python -m unittest test_disparate`).
- Rondas de Cosecha de palabras: 17 pruebas en `tools/cosecha/test_rondas.py` (`python -m unittest test_rondas`, desde `tools/cosecha`; ~70 s;
  con `COSECHA_REGENERAR=1` reconstruye todo desde las fuentes). Diseño aprobado en `docs/diseno-cosecha-de-palabras.md`; el juego aún no está programado.
- Herramientas: botones "[Debug]" (`ui/screens/DebugTools.kt`, solo builds de depuración) para abrir cada juego,
  ver las celebraciones y repetir el onboarding. "Borrar datos" en Ajustes deja la app como recién instalada.

## Estado y pendientes (27-sep)

- 27-sep: prueba manual completa (`docs/prueba-manual.md`, incluida la sección H con "No conservar actividades")
  aprobada por Ricardo en su teléfono. Es el punto base `v0.1-base`.
- `ActiveGameSession.sessionToken` + `key(session.sessionToken)` en `MainActivity`: cada sesión de juego es su propio
  grupo de composición (si no, `UnityGameHost` heredaba el `launched` guardado de la anterior al recrearse la pantalla).
- Idea de Ricardo tras Radar (27-sep): la información del final de cada juego estrella es lo más valioso para el
  usuario. Desde el 28-sep la medida propia de cada partida se guarda (`star_measures`) y se muestra en Hoy.
- **Hoy = "Tu planeta"** (REEMPLAZADO el 29-sep por Nubi al centro: ver el punto de Nubi; queda como historia. 28-sep; Ricardo eligió la mezcla de las propuestas 1 y 2 de `docs/previews/inicio-propuestas.png`;
  maqueta `docs/previews/inicio-planeta.png`, `tools/previews/inicio_planeta.py`). Reemplaza al camino de días en
  perspectiva. `ui/components/HomePlanet`: planeta de arcilla con una zona por dominio (Memoria cristales, Atención
  faros, Razonamiento torres, Lenguaje árboles, Cálculo domos, Velocidad antenas, al centro) que crece con cada partida
  (`data/Planet`: crecimiento logarítmico, nunca baja; construcciones 1-5), gira despacio (quieto con "quitar
  animaciones"), las 3 partidas del día orbitan y aterrizan con ✓ en su zona, las zonas jugadas hoy brillan y arriba
  dice "¡Creció X!". Debajo: la línea "Esta semana creció / Quieta hace N días · Aún sin explorar" y el
  **descubrimiento del día** (`data/StarMeasures.discover`: con 3+ partidas de un juego estrella, primero un récord de los
  últimos 7 días, si no la mayor mejora ≥ 5%, si no "se mantiene"; gráfico de las últimas 6 con "mejor" hacia arriba; sin
  datos, invitación tocable "Juega Radar para descubrir tu vistazo"). Tocar una zona abre su ventana (`ZoneDialog`):
  partidas por semana (4), cada juego del dominio con su última medida o cuándo se jugó, y "Jugar X" (el sin jugar o el
  más olvidado). Captura real (Roborazzi): `docs/previews/inicio-planeta-real.png`. Sin probar en el teléfono.
- **Juegos (30-sep, 4 áreas) = encabezado "Nubi te sugiere" + rejilla 2 × 2** (maqueta aprobada `docs/previews/cuatro-areas.png`,
  Juegos · 2). Arriba Nubi científica + "¿Qué entrenamos hoy?" y "Te sugiero {Área}: {motivo}" (`data/AreaSuggestion`, lógica
  pura: primero un área nunca jugada, si no la que lleva ≥ 2 días sin jugarse, si no la de menor avance; el motivo no repite
  el nombre). `AreaGrid` llena el alto hasta la barra (planeta sugerido de 118 dp con aro de luz sol y rótulo "Sugerida"; los
  otros de 88 dp), y cada área muestra nombre, `DomainType.tagline`, `AreaBar` y "Hábil · 7 juegos". Las áreas son 4 desde
  el 30-sep: `DomainType` = MEMORIA, ATENCION, RAZONAMIENTO, LENGUAJE; Velocidad se unió a Atención y Cálculo a Razonamiento
  (Radar y Comparación → Atención; Cálculo Sereno y Aterrizaje → Razonamiento). Lo guardado con nombres viejos se lee con
  `DomainType.fromStored` (metas, punto de partida, foco de Juegos, XP de dominio) y Room v12 (`Migration(11, 12)`) suma el XP
  de `domain_mastery`. En Hoy: 2 áreas por lado (izquierda Memoria y Razonamiento, derecha Atención y Lenguaje) con planeta,
  nombre, subtítulo, barra y etapa; Nubi crece hasta llenar el centro. Lo siguiente es la 3.ª versión anterior, de historia:
- **Juegos = las 6 áreas + ventana del área** (29-sep, 3.ª versión: en la 2.ª se deslizaba para cambiar de área y
  chocaba con el deslizar de pestañas; maqueta `docs/previews/juegos-nubi.png`, capturas `juegos-areas-real.png` y
  `juegos-ventana-real.png`). `AreaGrid`: las 6 áreas en dos columnas (planeta `drawable-nodpi/area_*.webp` de
  `nubi_recursos.py`, Atención = DIANA; nombre, `AreaBar` y "Hábil · 6 juegos"; sello sol ✓ = jugada hoy). Tocar una
  abre `AreaWindow` (pantalla completa, `LockTabSwipe`, X arriba a la derecha o Atrás = `closeLibraryArea`): Nubi
  CIENTÍFICA (`nubi_cientifica.webp`, elegida por Ricardo sobre la estudiante) pregunta "¿Con cuál entrenamos tu
  memoria?" y debajo van TODOS sus juegos (`GameTile`: planeta con anillo del avance y ✓ si se jugó hoy, nombre,
  etapa y %, marca a la derecha). Tocar una casilla abre la FICHA superpuesta (`GameSheet`): avance con etapa y línea,
  marca con etiqueta y últimas partidas, cuándo jugaste y partidas en 2 semanas, "¿Cómo quieres jugar?" (4 filas con
  aciertos esperados; la explicación solo del elegido) y Jugar. Al cerrar el resultado se vuelve a Juegos, a la misma
  ventana y casilla (`libraryFocus` con `open`, en SharedPreferences `library_focus`, sobrevive a que Android cierre la app).
  El resultado dice si un Desafío se superó (`modeNote`). Sin probar en el teléfono.
- **Dificultad y avance** (28-sep, aprobado por Ricardo): [`docs/dificultad-y-avance.md`](docs/dificultad-y-avance.md) y
  `data/Skill.kt`. Una vara por juego: tu avance = nivel donde se aciertan 8 de 10 (el rating guardado se corrige por
  los aciertos que busca al entrenar: 85% mayores, 70% Ruta del Tesoro); etapas Inicio…Maestro = quintos. Modos
  Suave / A tu medida / Desafío / Experto por aciertos esperados según la edad, hechos con techo o piso sobre el DDA
  (`play_mode`, `mode_floor`, `mode_ceiling` → `AdaptiveDifficulty.ConfigureMode`). Solo mueven el avance las partidas
  a tu medida (bajada máx. 5 puntos) y un Desafío o Experto superado (`mode_trials/mode_hits` tras el calentamiento);
  las marcas solo a tu medida. Experto se abre al superar un Desafío. El reloj NO se elige antes de jugar (Ajustes).
  SharedPreferences `skill`: juegos medidos ("Sin medir aún" si no), Experto abierto, fecha de cada etapa.
  El nivel 1-5 de cada juego ES la etapa (`LevelTier` con los mismos nombres); Ajustes ya no tiene modo de dificultad.
  Cada marca (`MeasurePoint`) guarda rating y reloj; la evolución compara solo partidas parecidas
  (`StarMeasures.comparable`: mismo reloj y, en las marcas que dependen del nivel, a menos de un nivel).
  Pendiente: "fortalezas" por aspecto en los juegos estrella (hoy la carta muestra solo la constancia) y calibrar la
  pendiente `s` de cada juego con datos (sección 10 del documento).
- **Anagramas con burbujas** (28-sep, idea de Ricardo): en los niveles 5-7 (7 a 11 letras) las letras del banco son
  burbujas de arcilla que rebotan sin parar contra los bordes y entre ellas (`Anagramas/BubbleField`: física pura con
  3 pruebas; choque elástico, separación mínima siempre, rapidez constante). Por edad (`BubbleField.SpecFor`):
  mayores 64 dp, 12 dp de separación, 42 dp/s; adultos 56 dp, 8 dp, 70 dp/s; menores 56 dp, 8 dp, 80 dp/s; nunca bajo
  48 dp (se achican si el área se llena). Entra de a poco: nivel 5 al 60% de la velocidad, 6 al 80%, 7 completo.
  El toque cuenta al presionar (`TapDown`); una letra devuelta vuelve a un lugar libre. Adornos (28-sep, pedido de
  Ricardo): resplandor de color detrás de cada letra (capa `_glowLayer`, debajo de todo; late suave), burbujas con cara
  de color propio (celeste, uva, coral, lima, sol, aclarados hacia crema para que la letra se lea) y brillo grande y
  difuminado; niveles 1-4 (mecánica igual): el mismo resplandor, fichas que respiran (cada una a su ritmo) y un destello
  que recorre el borde de una ficha al azar cada 3,5-6,5 s. En las casillas la ficha toma los colores de estado, así el
  color decorativo nunca dice "acierto". Con "quitar animaciones" del teléfono (`reduce_motion` en la config →
  `GameFeel.ReduceMotion`) no hay respiración, pulso ni destellos. Lámina `docs/previews/anagramas-burbujas.png`.
  Sin probar en el teléfono.
- **Nombre e ícono: Nubi** (elegido por Ricardo el 29-sep tras siete rondas; "NeuroVida" es marca registrada de otros en
  EE. UU. y hay una app "NEUROVIDA PSICOLOGIA"). Mercado GLOBAL; en la tienda "Nubi – Brain Games". Personaje: Nubi, una
  nebulosa pequeña de arcilla (lila/uva con nubes celeste y rosa y estrellitas crema), "la nube donde nacen las
  estrellas": vive en tu planeta, cada partida hace nacer una estrella; poses saluda / celebra / piensa / descansa,
  NUNCA triste ni reaccionando a cómo le va a la persona. Estilo elegido (29-sep, tras 4 vueltas descartadas: ícono
  plano, 5 personajes planos, 6 nebulosas): **Nubi suave**, del tono de una lámina de referencia de Ricardo = nube de
  algodón con volumen, contorno fino morado (no el borde tinta grueso), brillo alrededor, ojos grandes con dos brillos,
  azul lavanda con toques celestes (`tools/previews/nubi_suave.py` → `docs/previews/nubi-suave.png`: momentos enfoque /
  explorador con casco / celebra / tu semana, ícono redondo, con casco y monocromo). Nubi GUÍA (`nubi_guia.py` →
  `nubi-guia.png`): científica con bata y lentes (explica la medida del final), maestra en pizarra (tutorial de primera
  vez), exploradora con lupa (descubrimiento del día), idea (consejo). Hoy con el mundo de Nubi (`nubi_mundo.py` →
  `hoy-mundo-nubi.png`): fondo de nebulosas de colores, planeta con paisajes por dominio (Valle de la Memoria, Picos
  de la Atención, Mar del Razonamiento, Bosque del Lenguaje, Domos del Cálculo, Meseta de la Velocidad), los 3 juegos
  del día como satélites, zona del día, gráfica "Tu semana" y mejoras del planeta que se ganan jugando. Ricardo
  aprobó todo MENOS el planeta ("rompe el hilo conductor"; también se descartaron `planeta-caminos.png`) y los
  satélites alrededor de Nubi. Avance sin planeta: `avance-sin-planeta.png` → eligió el anillo de luz
  (`anillo_luz.py` → `anillo-luz.png`: formas, estados animados, colores de áreas revisados con el validador de la
  skill dataviz: Lenguaje verde lima #4FAE2A, Cálculo celeste #1C9FCE, Atención #CF7A06) → y luego propuso **Tu
  galaxia** (`galaxia.py` → `galaxia.png`): galaxia ovalada que gira lento con Nubi al centro, un cúmulo por área,
  TAMAÑO = etapa (nunca baja) y BRILLO = partidas de los últimos 14 días (se apaga despacio, nunca del todo), se gira
  con el dedo y el cúmulo tocado abre su ventana con estadísticas. Descartada (con estrellas de luz, `galaxia_hd.py`,
  tampoco: "no da a entender el concepto"); también los orbes alrededor de Nubi (`nubi_halo.py`: "pueden confundir").
  **Elegido (29-sep): `nubi_hoy.py` → `nubi-hoy.png`** (de las 3 de `nubi_indicadores.py`): Hoy con Nubi grande al
  centro y halo detrás (resplandor, dos aros finos, pocas chispas); tres áreas a cada lado con nombre, BARRA de avance
  0-100 (un solo color lavanda; 4 marcas = 5 etapas; cada etapa = 20) y etapa en palabras. El cambio de la semana se ve
  EN LA BARRA, sin números (Ricardo: los "▲ +4" molestaban): tramo sol al final = lo avanzado esta semana; tramo lila
  punteado = lo que bajó. Nubi dice en palabras qué pasó (sin números sueltos: "¿4 qué?"). Tocar un área abre su
  DETALLE (vista C): "Etapa Intermedio · 52 de 100", barra con las 5 etapas nombradas, "Esta semana avanzó de 46 a 52",
  "Te faltan 8 para Avanzado", últimas 4 semanas, flechas para pasar de área y, en vez de "Jugar X", la pregunta
  "¿Le damos un empujón a tu memoria?" (Fredoka sol, mismo estilo que el botón) + botón "¡Sí, vamos!" + "Nubi eligió
  Secuencia Lumínica: hace días que no la juegas". **Programado (29-sep)**: `ui/components/NubiHome.kt` (`NubiWithHalo`,
  `AreaBar`, `NubiBubble`, `NubiHome`) + `HomeScreen.AreaDetail` + `data/AreaProgress` (avance del área = promedio de
  sus juegos medidos; cambio = juego por juego contra hace 7 días, solo los ya medidos entonces; 4 pruebas). Las etapas
  usan los nombres de `Skill.STAGES` (Inicio, Aprendiz, Hábil, Experto, Maestro). Se quitaron del inicio el planeta, la
  línea de zonas y el descubrimiento del día (las medidas de cada juego están en el detalle del área). Nubi en la
  bienvenida ("Hola, soy Nubi"). Ícono: `tools/previews/nubi_recursos.py` escribe en `res/` las capas del ícono
  adaptativo (fondo vector de noche, Nubi, monocromo), los íconos para Android 7, el de notificación y
  `drawable-nodpi/nubi_{hola,mira,celebra}.webp`. Capturas: `docs/previews/hoy-nubi-real.png`, `hoy-nubi-detalle-real.png`.
  Sin probar en el teléfono; después: ícono adaptativo (con monocromo) y Nubi en Hoy, bienvenida, logros y recordatorios. Ya cambiado: `app_name`, bienvenida, textos para compartir, tarjeta de liga y
  aviso de Ajustes. Revisión de nombres y patentes: [`docs/nombre-marca-y-riesgos.md`](docs/nombre-marca-y-riesgos.md).
  Antes de publicar: búsqueda oficial de marca (clases 9, 41) y el `applicationId` definitivo. **Reglas por patentes** (no romper): Radar rehecho como "Rescate relámpago" (sin nave central ni opciones entre
  las que elegir: no volver a eso; US 8,348,671 de Posit); nombres: cuarta ronda (estilo Synapp: el vocabulario del
  cerebro está casi todo tomado); Piloto nunca con inclinación ni sensores del cuerpo; ningún
  juego calcula un perfil "impulsivo / conservador" ni usa caras con emociones que reaccionen al desempeño (Akili);
  Parejas siempre con el tablero a la vez; Satélites siempre plano (sin 3D estereoscópico); las etapas de avance
  quedan en la escala común (nada de "máximo personal" partido en puertas: US 10,559,221); no usar nombres ajenos
  (UFOV, Double Decision, NeuroTracker...).
- Ideas en espera (NO implementar hasta que Ricardo lo pida): rangos de tripulación en vez de ligas de metales y
  "Tu astronauta" (avatar propio, color de acento elegido). Detalle en [`docs/ideas-guardadas.md`](docs/ideas-guardadas.md).
- Después, en la lista de Ricardo: revisar qué juegos usa la evaluación inicial ("los juegos no me quedan claros");
  re-chequeo mensual del punto de partida; tutorial de primera vez por juego; marca ✓/✗ de arcilla sobre la
  respuesta; alinear Secuencia y Parejas con el DDA común; calibrar el DDA y la referencia de percentiles con datos.
- Para el final: i18n completo (hoy `ui/i18n/AppStrings` cubre solo algunos textos); `applicationId` propio
  (cambiarlo = app nueva); `metadata.json` de AI Studio; firma release y Play Store; servidores.
