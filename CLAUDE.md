# NeuroVida — memoria del proyecto (al 26-sep)

App de estimulación cognitiva para Android: 16 juegos cortos en 6 dominios (memoria, atención, razonamiento,
lenguaje, cálculo, velocidad), dificultad que se adapta, camino diario de 3 juegos, ligas con trofeos, logros,
racha y un punto de partida inicial. La app (menús, progreso, datos) es Kotlin + Compose; los juegos corren en
Unity embebido ("Unity as a Library"). Dueño y quien prueba: Ricardo (psicólogo). Meta: superar a
Lumosity/Peak/Elevate en calidad y en motivación.

El diario detallado de cómo se llegó hasta aquí (decisiones, bugs, pedidos de Ricardo) está en
[`docs/historial-desarrollo.md`](docs/historial-desarrollo.md). Es historia: lo que manda es este archivo y el código.

## Cómo trabajamos (nube ↔ PC)

- Las sesiones en la nube editan, verifican C# con `dotnet build tools/unity-compile-check -v q` y hacen push a la
  rama de trabajo. En la nube NO hay Android SDK ni Unity: el Kotlin no se compila acá (revisarlo con cuidado;
  `kotlinc` sirve para lógica pura con stubs y para detectar errores de sintaxis).
- En el PC de Ricardo (Git Bash, carpeta del repo): `git pull && bash tools/verificar-todo.sh --instalar` →
  escena piloto, pruebas EditMode, smoke de los 16 juegos, REEXPORTAR Unity, Gradle con pruebas Kotlin, instalar.
  Si algo falla, él pega las últimas 40 líneas de `unity/test-results/v-*.log`.
- `unity/AndroidExport/` está fuera de git: si no se reexporta, el APK lleva los juegos viejos sin avisar.
  Marca de verificación: en builds de depuración la cuenta regresiva muestra `CountdownScreen.StyleStamp`
  (hoy `estilo 27-sep · t`). **Cambiarla con cada cambio visible de Unity.**
- Vistas previas sin Unity ni teléfono: `tools/art-preview` (compila los generadores de sprites REALES contra un
  UnityEngine mínimo y vuelca PNG; `compose.py` y `juegos.py` arman láminas) y `tools/previews/*.py` (réplicas PIL
  de pantallas Compose). Resultados en `docs/previews/`. Si se cambia el arte, actualizar la lámina.
- Estilo con Ricardo: en español, sin jerga, cambios chicos y verificables, y decirle siempre qué probar.

## Toolchain (PC Windows de Ricardo)

- Android SDK `C:\Users\RURAL7\AppData\Local\Android\Sdk` (android-36; emulador `medium_phone` x86_64: sirve para
  la UI Compose, no para los juegos Unity, que son arm64). Teléfono: Motorola (`DEVICE` en `verificar-todo.sh`).
- JDK 21 Temurin `C:\Users\RURAL7\AppData\Local\Temurin21\jdk-21.0.12.1+1` (NO el JBR de Android Studio).
- Unity 6000.0.84f1 LTS + soporte Android. Config-cache de Gradle desactivado (choca con el export de Unity).
- `local.properties` y `debug.keystore` fuera de git. Compilar a mano: `./gradlew.bat assembleDebug`.

## Arquitectura

**App Android** (`app/src/main/java/com/example/`, paquete `com.example`, applicationId `com.aistudio.neurovida.cgnv`):
- `MainActivity` + `viewmodel/NeuroVidaViewModel` (un solo ViewModel) + `data/NeuroVidaRepository`.
- Pestañas (`ui/components/NeuroNavBar`): Hoy (`HomeScreen`, camino de días en perspectiva) · Juegos
  (`GamesLibraryScreen`, planetas) · Entrenar (botón central = sesión diaria) · Liga (`ProgressScreen`) ·
  Perfil (`ProfileScreen`, logros, punto de partida; abre `SettingsScreen`). Onboarding (`OnboardingScreen`,
  9 pasos) mientras `UserSettings.ageBand == null`. Evaluación y mapa inicial: `BaselineScreen`.
- Lógica pura con pruebas en `data/`: `Achievements`, `Baseline`, `DdaRating`, `LeagueEvents`, `NumberLine`, `Percentile`; y
  `notification/ReminderContent`. Modelos y catálogo de juegos (`GameRegistry`, `RankTier`, ...) en `model/Models.kt`.
- Persistencia: **Room v11** (`data/local/`, `exportSchema`, esquemas en `app/schemas/`; resultados, progreso por
  juego con `ddaRating`, sesión diaria, perfiles, maestría, desafíos). Datos que solo se agregan o salen del
  onboarding van en **SharedPreferences** para no migrar: `league_events`, `achievements`, `profile_extra`
  (educación, metas, mapa, `prior_at`), `paused_game`, bandeja de resultados de Unity.
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

**Radar** (`Games/Radar/`, id `radar`, dominio velocidad): tarea UFOV (Ball y Owsley; ensayo ACTIVE, Ball et al.,
JAMA 2002; Edwards et al., 2017) con tema de rescate. Destello: una nave en la pantalla central + un astronauta (casco
de `SymbolSprite`) en una de 8 direcciones y 3 anillos; interferencia (máscara) 350 ms; responder 1) qué nave pasó
por el centro (2 opciones) y 2) tocar la dirección del astronauta (vale tocar cualquier parte del sector). Las dos
bien = rescatado (vuela a la fila de abajo).
- Reglas y pruebas: `RadarContract` / `RadarContractTests` (10). 12 niveles: destello 500 → 40 ms (~20% menos por
  nivel); asteroides desde el 4 (7 → 15 → 23, por anillos completos); astronauta más lejos desde el 3 y el 6; pares del
  centro fácil / mismo color / silueta parecida (5 y 9). DDA común con `stepUp` 0.3, sin tiempo de reacción.
- El juego pide 60 cuadros por segundo mientras dura (`Application.targetFrameRate`; Android da 30) y mide la duración
  REAL de cada destello. Una pausa en pleno destello lo anula y se repite. Reto 90 s; Precisión 20 destellos.
- Medidas propias: **tu vistazo** (`GlanceMs`: media geométrica de las duraciones reales de los últimos 12 destellos,
  sin los 4 primeros = donde se asentó la escalera, ~80% de aciertos) y **tu radar** (aciertos de ubicación por
  dirección). Viajan en `StroopSessionMetrics.glance_ms / sector_hits / sector_trials` → `GamePlayResult.glanceMs /
  sectorHits / sectorTrials` (no se guardan en Room) → `GameResultScreen`: "Tu vistazo: N ms" y un radar con una cuña
  por dirección (largo = proporción rescatada) + dónde más y dónde menos (con 2+ destellos por dirección).
- Arte: `RadarSprites` (radar, haz, interferencia, botones de dirección), `GameWorld.RadarStation` (cielo quieto).
  Vista previa: `python3 tools/art-preview/radar.py <raw>` → `docs/previews/radar.png`.
- Probado por Ricardo (27-sep): "espectacular", le encantó sobre todo la información del final ("un área
  increíblemente buena ... para entregar a los usuarios"; quiere explorarla más: ver pendientes).

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
  `StroopSessionMetrics.tracking_capacity / tracking_speed` → `GamePlayResult.trackingCapacity / trackingSpeed` →
  `GameResultScreen`: "Tu seguimiento: 3,4 a la vez" con 5 discos que se llenan + velocidad + "la mayoría de los
  adultos sigue alrededor de 4".
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
  p(responder|alto) fuera de 0,15-0,85). Viaja en `brake_ms / stops_ok / stops_total / brake_best_ssd_ms` →
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
- Medida propia: **tu precisión numérica** (error medio %) y **tu línea**: `numline_true / numline_given` (0..1 por
  aterrizaje) → `GamePlayResult.numlineTrue / numlineGiven` → `GameResultScreen` dibuja la regla con cada blanco y dónde
  se posó, y `data/NumberLine` (lógica pura con pruebas) lee el sesgo: pareja, agranda los chicos, achica los grandes o
  comprime (el patrón logarítmico de Siegler).
- Arte: `LandingSprites` (módulo lunar, bandera), `GameWorld.LunarRange`. Vista previa:
  `python3 tools/art-preview/aterrizaje.py <raw>` → `docs/previews/aterrizaje.png`. Probado por Ricardo (27-sep): ok.

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
  aciertos; 1000 / pendiente = grados por segundo; -1 con menos de 8 aciertos o menos de 3 ángulos) y **tu curva de
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

## Reglas que no se rompen

- **Diseño**: "noche + arcilla": cielo nocturno animado; lo tocable es arcilla (borde tinta grueso `Ink` 0x1A1240,
  sombra dura, colores Coral/Sun/Sky/Grape/Lime/Cream); tipografía Fredoka. Las pantallas principales NO usan
  recuadros para informar (texto suelto, objetos, líneas finas); tarjetas solo en diálogos. Nada de emojis como
  íconos; acierto/error nunca solo por color (forma o texto); contraste ≥ 4.5:1; respetar "quitar animaciones",
  sonido y vibración apagados. Usar la skill `ui-ux-pro-max` (`.claude/skills/`) para decisiones de diseño.
- **Juegos Unity**: medir el tiempo con `GameClock.Time/DeltaTime` (no `Time.unscaled*`), para que la pausa funcione.
  El arte se hornea en sprites con `ClayRaster` (`Image.color` blanco); la sombra dura cae siempre hacia abajo.
- **Textos**: español, cercanos, sin culpa ni promesas de salud ("no es un examen", nada de "fortalece neuronas").
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

## Pruebas

- Kotlin: 46 (`./gradlew.bat testDebugUnitTest`; lógica pura en `app/src/test/.../data`, `model`, `notification`).
- Unity EditMode: 155 (contratos de cada juego, `AdaptiveDifficultyTests`, Parejas, perfil por edad) + 16 smoke tests.
- Herramientas: botones "[Debug]" (`ui/screens/DebugTools.kt`, solo builds de depuración) para abrir cada juego,
  ver las celebraciones y repetir el onboarding. "Borrar datos" en Ajustes deja la app como recién instalada.

## Estado y pendientes (27-sep)

- 27-sep: prueba manual completa (`docs/prueba-manual.md`, incluida la sección H con "No conservar actividades")
  aprobada por Ricardo en su teléfono. Es el punto base `v0.1-base`.
- `ActiveGameSession.sessionToken` + `key(session.sessionToken)` en `MainActivity`: cada sesión de juego es su propio
  grupo de composición (si no, `UnityGameHost` heredaba el `launched` guardado de la anterior al recrearse la pantalla).
- Idea de Ricardo tras Radar (27-sep): la información del final de cada juego estrella es lo más valioso para el
  usuario; explorar más ese camino (propuesta pendiente: guardar las medidas propias por partida y mostrar su evolución).
- Ideas en espera (NO implementar hasta que Ricardo lo pida): rangos de tripulación en vez de ligas de metales y
  "Tu astronauta" (avatar propio, color de acento elegido). Detalle en [`docs/ideas-guardadas.md`](docs/ideas-guardadas.md).
- Después, en la lista de Ricardo: revisar qué juegos usa la evaluación inicial ("los juegos no me quedan claros");
  re-chequeo mensual del punto de partida; tutorial de primera vez por juego; marca ✓/✗ de arcilla sobre la
  respuesta; alinear Secuencia y Parejas con el DDA común; calibrar el DDA y la referencia de percentiles con datos.
- Para el final: i18n completo (hoy `ui/i18n/AppStrings` cubre solo algunos textos); `applicationId` propio
  (cambiarlo = app nueva); `metadata.json` de AI Studio; firma release y Play Store; servidores.
