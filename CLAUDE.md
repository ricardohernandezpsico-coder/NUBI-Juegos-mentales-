# Nubi — memoria del proyecto (al 2-oct)

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

App de estimulación cognitiva para Android: 19 juegos cortos en 4 áreas (Memoria [5: Ruta del Tesoro y Bitácora de Misión se retiraron el 4 y el 5-oct, y Bodega de carga ocupa el lugar de Bitácora desde el 5-oct] · Atención [foco y velocidad] · Razonamiento [lógica y números, 4 juegos: Engranajes (5-oct) ocupa el lugar de los retirados Detective de Series y Tráfico Estelar] · Lenguaje;
de 6 a 4 el 30-sep, Room v14), dificultad que se adapta, camino diario de 3 juegos, ligas con trofeos, logros,
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
  escena piloto, pruebas EditMode, smoke de los juegos de Unity (en UN solo Unity; Bitácora, retirada de la app, sigue en Unity y en el smoke), REEXPORTAR Unity, Gradle con pruebas Kotlin, instalar.
  Salida corta (una línea por etapa); los logs completos van a `unity/test-results/v-*.log` y solo se leen si algo falla.
  Modos para iterar sin verificar todo cada vez: ver «Cómo trabajar una tarea de un juego».
- `unity/AndroidExport/` está fuera de git: si no se reexporta, el APK lleva los juegos viejos sin avisar.
  Marca de verificación: en builds de depuración la cuenta regresiva muestra `CountdownScreen.StyleStamp`
  (hoy `estilo 8-oct · estación de correo`). **Cambiarla con cada cambio visible de Unity.**
- Vistas previas sin Unity ni teléfono: `tools/art-preview` (compila los generadores de sprites REALES contra un
  UnityEngine mínimo y vuelca PNG; `constelaciones.py`, `bodega.py`, `correo.py`, etc. arman las láminas de cada juego) y `tools/previews/*.py` (réplicas PIL
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
  Inicio nuevo «Primer vuelo con Nubi» (`FirstFlightScreen` + `FirstFlightHost`, lógica en `data/FirstFlight.kt`, ficha en `docs/diseno-inicio.md`): 19 pasos con 4 juegos (uno por
  área) y preguntas intercaladas, mientras `UserSettings.ageBand == null` o haya un recorrido guardado. Reemplaza al onboarding de 9 pantallas y a la evaluación aparte.
  Las pestañas se pasan deslizando con el dedo (`HorizontalPager` en `MainActivity`, sincronizado con
  `viewModel.currentTab`). Una ventana abierta sobre una pestaña (área en Juegos, detalle de un área en Hoy o Avance)
  llama `LockTabSwipe()`: mientras esté, el dedo no cambia de pestaña y la barra de abajo se esconde.
- Sesión diaria = SOLO los 3 juegos del camino (`startDailySession` → `continueDailyFlow`). Al "Continuar" del tercero,
  resumen (`data/SessionSummary` + `ui/screens/SessionSummaryScreen`): Nubi celebra, qué áreas se trabajaron, puntaje
  de cada juego, racha y la barra de avance de cada área con "Hoy avanzó de X a Y" (cambio desde el comienzo de la
  sesión, `AreaProgress.status(since = ...)`). Captura `docs/previews/resumen-sesion-real.png`.
- Lógica pura con pruebas en `data/`: `Achievements`, `AreaProgress`, `Baseline`, `FirstFlight`, `DdaRating`, `Homing`, `Mail`, `LeagueEvents`, `NumberLine`, `Percentile`, `RetoChoice`,
  `Planet`, `SessionSummary`, `StarMeasures`, `Library`, `Skill`; y
  `notification/ReminderContent`. Modelos y catálogo de juegos (`GameRegistry`, `RankTier`, ...) en `model/Models.kt`.
- Persistencia: **Room v14** (`data/local/`, `exportSchema`, esquemas en `app/schemas/`; resultados, progreso por
  juego con `ddaRating`, sesión diaria, perfiles, maestría, desafíos). Datos que solo se agregan o salen del
  onboarding van en **SharedPreferences** para no migrar: `league_events`, `achievements`, `profile_extra`
  (educación, metas, mapa, `prior_at`), `paused_game`, bandeja de resultados de Unity, `skill` (avance: ver abajo), `star_measures` (la medida
  propia de cada partida de los juegos estrella, `StarMeasures.encode`), `mission_log` (de Bitácora, retirada: ya no se lee, sigue en el respaldo), `progress_log` (historia del
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
- Telemetría: `NativeReceiver` elige el adaptador por `game_id` (Secuencia = Rastro de luz, Parejas = Constelaciones, y `StroopTelemetry` para los demás);
  los tres traen `end_rating`. Llama a `repository.recordGameResult` (devuelve `RecordOutcome`: nivel, ascensos, logros).

**Unity** (`unity/NeuroVidaCore/Assets/Scripts/`, asmdefs `Contracts ← Bridge ← Games ← Bootstrap`):
- `Bootstrap/`: `GameEntryPoint` (config, orientación vertical, Atrás), `LaunchIntentConfigReader` (lee el Intent,
  arranca/reinicia partidas por launch id). `Bootstrap/Editor/`: exportar librería, escena piloto, smoke tests,
  `SymbolPreviewExporter`.
- `Contracts/`: `SequenceInitConfig` (config de entrada de TODOS los juegos; lleva `show_tutorial`), `SequenceTelemetry` (Rastro de luz, con `ras_*`), `CardsTelemetry`,
  `StroopTelemetry` (salida común de casi todos; lleva `end_rating` y `peak_level`; `SequenceTelemetry` y `CardsTelemetry` llevan los mismos campos).
- `Games/AdaptiveDifficulty.cs`: DDA común (up-down ponderado de Kaernbach hacia 80% de aciertos, 85% en mayores;
  ver `docs/DDA-comun.md`). Lo usan Tinta o Palabra (`Stroop/`),
  «Carga exacta» (id `calculo`, antes Cálculo Sereno), «Engranajes: Taller de reparación» (id `engranajes`, 12 niveles = 12 etapas; se arregla la máquina con 1 o 2 cambios), «En la punta de la lengua» (id `anagramas`), **Rastro de luz** (id `secuencia`: escalera de 16 niveles, `RastroLadder`/`RastroContract`, sin tiempo de reacción; las 3 vidas solo terminan la partida) y **Constelaciones** (id `parejas`: escalera de 18 etapas en 6 grupos, `ConstelacionContract`; cada oportunidad de memoria es un ensayo), **Correo Estelar** (id `correo`: escalera de 10 etapas, `MailContract`; cada encargo es un ensayo) y **Piloto Estelar** usa DOS instancias (pilotaje y señales); **Radar** y **Satélites** una, sin tiempo de reacción; **Freno de Emergencia** una para la tarea de ir (el alto tiene su escalera propia). Desde el 3-oct los 23 juegos usan el motor común.
- Cada juego: `XContract.cs` (reglas puras con pruebas) + `XGameController.cs` (UI construida por código). Los 7 del
  DDA común heredan de `Shared/GameControllerBase` y usan `Shared/GameHud`.
- `Games/Shared/`: sello visual y piezas comunes — `NeuroStyle` (paleta de la app, `ClayText`, `ClayFrame`),
  `ClayRaster` (pincel SDF para todo el arte en arcilla), `WorldBackdrop` (cielo + un elemento propio por juego),
  `StarfieldFx`, `CountdownScreen`, `FinishCurtain` + `ExitButton` (cierre "¡Listo!" → resultado en la app),
  `GameFeel` (sonidos sintetizados y vibración), `Motion` («quitar animaciones»: ver [docs/movimiento-reducido.md](docs/movimiento-reducido.md)), `GameClock` (tiempo pausable), `PauseMenu`, `Assessment`
  (modo evaluación), `UiKit`, `Toast`, `PhasePill`, `PressScale`, sprites varios, y el **tutorial guiado común** (`GuidedTutorial` + `NubiTeacherSprite`;
  gancho `GuidedRound` en `GameControllerBase`; la app manda `show_tutorial` si no hay partidas del juego: `UnityGameLauncher.TUTORIAL_GAMES` = Rastro de luz, Freno, Aterrizaje, Meteoros, Tinta o Palabra, En la punta de la lengua, Carga exacta, Engranajes, Bodega de carga, Constelaciones y Correo Estelar; «Cómo se juega» en la pausa; cómo sumar otro en `docs/diseno-rastro-de-luz.md`). Nubi y su globo se colocan sin tapar el hueco, las zonas protegidas que declara cada paso ni ningún texto del juego (`CoachLayout`, textos en `CoachTexts`; `docs/tutoriales-con-nubi.md`).

## Juegos (índice)

Cada juego tiene su ficha técnica (cómo funciona, reglas, medidas, decisiones, pruebas) en el documento de la tabla: los cuatro de
Lenguaje con diseño propio y los demás en `docs/juegos/<id>.md` ([catálogo y orden](docs/juegos/README.md); descartados en
[docs/juegos/descartados.md](docs/juegos/descartados.md)). Cada juego estrella lleva una MEDIDA PROPIA al final de la partida.

| Juego | id | Área | Carpeta Unity | Documento (ficha técnica) | Lectura en la app (`data/`) |
|---|---|---|---|---|---|
| Constelaciones (antes Parejas Ocultas; renovado el 7-oct, conserva el id) | `parejas` | Memoria | `Games/Parejas/` | [docs/diseno-constelaciones.md](docs/diseno-constelaciones.md) (DDA común: [docs/DDA-comun.md](docs/DDA-comun.md)) | `Constelaciones.kt` |
| Rastro de luz (antes Secuencia Lumínica) | `secuencia` | Memoria | `Games/Secuencia/` | [docs/diseno-rastro-de-luz.md](docs/diseno-rastro-de-luz.md) | `Trail.kt` |
| Bodega de carga (nuevo, 5-oct; ocupa el lugar de Bitácora de Misión con otro id; la esclusa de carga entra y saca todo) | `bodega` | Memoria | `Games/Bodega/` | [docs/diseno-bodega-de-carga.md](docs/diseno-bodega-de-carga.md) (DDA común: [docs/DDA-comun.md](docs/DDA-comun.md)) | `Bodega.kt` |
| ~~Bitácora de Misión~~ **RETIRADA el 5-oct-2026** (Ricardo: «no me convence»; la app ya no la lanza, el código de Unity se queda, el historial se conserva; ver `docs/juegos/descartados.md`) | `bitacora` | (Memoria) | `Games/Bitacora/` | [docs/juegos/bitacora.md](docs/juegos/bitacora.md) | — |
| Rumbo a Casa | `rumbo` | Memoria | `Games/Rumbo/` | [docs/juegos/rumbo.md](docs/juegos/rumbo.md) | `Homing.kt` |
| Correo Estelar — «La estación de correo» (antes un vuelo con la ruta de Piloto; renovado el 8-oct, conserva el id) | `correo` | Memoria | `Games/Correo/` | [docs/diseno-correo-estacion.md](docs/diseno-correo-estacion.md) (medida: [docs/medidas-juegos-estrella.md](docs/medidas-juegos-estrella.md); DDA común: [docs/DDA-comun.md](docs/DDA-comun.md); la ficha del vuelo viejo, solo historia: [docs/juegos/correo.md](docs/juegos/correo.md)) | `Mail.kt` |
| Tinta o Palabra («Dos orillas», 3-oct) | `stroop` | Atención | `Games/Stroop/` | [docs/diseno-tinta-o-palabra.md](docs/diseno-tinta-o-palabra.md) (DDA común: [docs/DDA-comun.md](docs/DDA-comun.md)) | `DosOrillas.kt` |
| Piloto Estelar | `piloto` | Atención | `Games/Piloto/` | [docs/juegos/piloto.md](docs/juegos/piloto.md) | — |
| Freno de Emergencia | `freno` | Atención | `Games/Freno/` | [docs/juegos/freno.md](docs/juegos/freno.md) | — |
| Satélites | `satelites` | Atención | `Games/Satelites/` | [docs/juegos/satelites.md](docs/juegos/satelites.md) | — |
| Radar | `radar` | Atención | `Games/Radar/` | [docs/juegos/radar.md](docs/juegos/radar.md) | — |
| Carga exacta (reemplaza a Cálculo Sereno el 4-oct; conserva el id) | `calculo` | Razonamiento | `Games/Calculo/` | [docs/diseno-carga-exacta.md](docs/diseno-carga-exacta.md) (DDA común: [docs/DDA-comun.md](docs/DDA-comun.md)) | `Carga.kt` |
| Engranajes: Taller de reparación (nuevo, 5-oct, rehecho ese día; ocupa el lugar de Tráfico Estelar con otro id) | `engranajes` | Razonamiento | `Games/Engranajes/` | [docs/diseno-engranajes.md](docs/diseno-engranajes.md) (DDA común: [docs/DDA-comun.md](docs/DDA-comun.md)) | `Engranajes.kt` |
| Acoplamiento | `acoplamiento` | Razonamiento | `Games/Acoplamiento/` | [docs/juegos/acoplamiento.md](docs/juegos/acoplamiento.md) | — |
| Aterrizaje Lunar | `aterrizaje` | Razonamiento | `Games/Aterrizaje/` | [docs/juegos/aterrizaje.md](docs/juegos/aterrizaje.md) | `NumberLine.kt` |
| En la punta de la lengua (reemplaza a Anagramas el 3-oct; conserva el id) | `anagramas` | Lenguaje | `Games/Anagramas/` | [docs/diseno-punta-de-la-lengua.md](docs/diseno-punta-de-la-lengua.md) (DDA común: [docs/DDA-comun.md](docs/DDA-comun.md)) | `Punta.kt` |
| Lluvia de meteoros | `meteoros` | Lenguaje | `Games/Meteoros/` | [docs/diseno-lluvia-de-meteoros.md](docs/diseno-lluvia-de-meteoros.md) | `Vocabulary.kt` |
| ¿Verdad o disparate? | `disparate` | Lenguaje | `Games/Disparate/` | [docs/diseno-verdad-o-disparate.md](docs/diseno-verdad-o-disparate.md) | `Reading.kt` |
| Cosecha de palabras | `cosecha` | Lenguaje | `Games/Cosecha/` | [docs/diseno-cosecha-de-palabras.md](docs/diseno-cosecha-de-palabras.md) | `Harvest.kt` |
| La estrella intrusa | `intrusa` | Lenguaje | `Games/Intrusa/` | [docs/diseno-estrella-intrusa.md](docs/diseno-estrella-intrusa.md) | `Atlas.kt`, `FigureBank.kt` |

## Cómo trabajar una tarea de un juego

- Lee SOLO la carpeta del juego (`unity/NeuroVidaCore/Assets/Scripts/Games/<Carpeta>/`), su documento (tabla de arriba) y los puntos
  de contacto: la columna «Lectura en la app» y los comunes de todo juego (`model/Models.kt` → `GameRegistry`, `bridge/NativeReceiver.kt`
  y su DTO, `Bootstrap/GameEntryPoint.cs`, `Contracts/StroopTelemetry.cs` o la telemetría propia, `games/GameResultScreen.kt`,
  `data/StarMeasures.kt`, `data/Skill.kt`, `HeadlessPlaymodeSmokeTest.cs`).
- NO leas `docs/historial-desarrollo.md` ni los documentos o carpetas de otros juegos salvo que la tarea lo pida.
- Mientras iteras: `bash tools/verificar-todo.sh --juegos <Juego> --filtro-tests <Juego>` (smoke solo de ese juego en un Unity y
  EditMode solo de sus pruebas). Si el cambio es solo de Kotlin: `--solo-app` (avisa si `unity/` cambió desde el último export).
  `--instalar` en cualquiera para llevarlo al teléfono. Capturas: `./gradlew.bat :app:recordRoborazziDebug --tests "*<Juego>*"`.
- Verificación COMPLETA (`verificar-todo.sh` sin flags) solo antes de un merge a `main` o cuando se toca código compartido
  (`Games/Shared`, `Bridge`, la parte común de `GameResultScreen`, Room, DDA común, `verificar-todo.sh` o el smoke).

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
- **Símbolos** (4-oct): nada de estrella con puntas, media luna ni cruz en juegos ni íconos (luceros redondos, hexágono, gota, ola): ver [docs/simbolos-neutros.md](docs/simbolos-neutros.md). Los planetas-puerto de colores viven en `Games/Shared/PortSprites` (antes en Tráfico Estelar).
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
- **Reglas por patentes** (no romper): Radar rehecho como "Rescate relámpago" (sin nave central ni opciones entre las que elegir: no volver a eso; US 8,348,671 de Posit); nombres: cuarta ronda (estilo Synapp: el vocabulario del cerebro está casi todo tomado); Piloto nunca con inclinación ni sensores del cuerpo; ningún juego calcula un perfil "impulsivo / conservador" ni usa caras con emociones que reaccionen al desempeño (Akili); Parejas siempre con el tablero a la vez; Satélites siempre plano (sin 3D estereoscópico); las etapas de avance quedan en la escala común (nada de "máximo personal" partido en puertas: US 10,559,221); no usar nombres ajenos (UFOV, Double Decision, NeuroTracker...). (Detalle de nombres y patentes: [docs/nombre-marca-y-riesgos.md](docs/nombre-marca-y-riesgos.md).)

## Cuando Android cierra la app, respaldo y verificación automática (resumen; detalle en [docs/respaldo-y-diagnostico.md](docs/respaldo-y-diagnostico.md))

- Con Unity al frente Android puede cerrar el proceso de la app: lo que el flujo necesita vive en disco (`bridge/GameSessionStore`) y el
  resultado pendiente lo procesa el ViewModel al volver. Si se toca `NeuroVidaViewModel` o `GameSessionStore`, correr `flow/GameFlowTest`
  y `bridge/GameSessionStoreTest` antes de instalar (reproducir: Opciones de desarrollador → «No conservar actividades»).
- Respaldo: solo el PROGRESO (base de datos y preferencias `skill`, `star_measures`, `progress_log`, `league_events`, `achievements`,
  `mission_log`, `profile_extra` — incluye `result_focus` y `color_vision` —, `atlas`, `punta_words` —las palabras azules de En la punta de la lengua—, `engranajes_rocket` —las luces y los cohetes en órbita de Engranajes—, `bodega_record` —el récord de Bodega de carga: la bodega más grande sin errores—, `constelaciones_record` —el récord de Constelaciones: la racha de memoria más larga—, `correo_record` —el récord de La estación de correo: las cartas bien puestas en un día perfecto—). Pasajero (NO se respalda): `paused_game`, `game_session`, `unity_results`, `library_focus`, `first_flight`. **Si se agrega un archivo de preferencias nuevo**, `BackupRulesTest` falla hasta que se sume
  a `res/xml/backup_rules.xml` y `data_extraction_rules.xml` o a `transient` de la prueba.
- Room: al subir `version`: entidad → `Migration(N, N+1)` en `NeuroVidaDatabase.MIGRATIONS` → compilar (genera `schemas/<N+1>.json`) →
  correr pruebas (`MigrationTest`).
- Errores en el teléfono: `diag/ErrorLog` (`files/errores.txt`; Ajustes → «Enviar informe de errores»). GitHub Actions
  (`.github/workflows/verificar.yml`) solo compila el C# y la app con pruebas Kotlin: NO corre Unity; eso es `verificar-todo.sh`.
- Competencia (capturas en `Proyectos/pantallazos …/`, fuera del repo): [docs/analisis-competencia.md](docs/analisis-competencia.md).
  Lo que NO se hizo de la revisión de arquitectura: [docs/plan-mejoras-arquitectura.md](docs/plan-mejoras-arquitectura.md).

## Pruebas

- Kotlin: 461 (`./gradlew.bat testDebugUnitTest`; lógica pura en `app/src/test/.../data`, `model`, `notification`; los
  recorridos del ViewModel en `flow/`, lo guardado en disco en `bridge/`, migraciones y respaldo). Las pruebas con
  Robolectric que crean el ViewModel, el repositorio o leen la base llaman `TestSupport.resetDatabase()` en `@Before` y en `@After`:
  deja una base de Room EN MEMORIA (nunca el archivo `neurovida_database`), cancela el trabajo de fondo del repositorio anterior y apaga los recordatorios de
  WorkManager (`CognitiveReminderWorker.disabledForTests`); `NoDiskDatabaseGuardTest` falla si una prueba nueva no lo hace. Detalle en
  [docs/respaldo-y-diagnostico.md](docs/respaldo-y-diagnostico.md).
- Unity EditMode: 576 (contratos de cada juego, `AdaptiveDifficultyTests`, Constelaciones, La estación de correo, perfil por edad, Rastro de luz, tutoriales guiados, colocación de Nubi `CoachLayoutTests`) + 54 arranques de smoke (los 20 juegos de Unity, las 3 versiones cortas y los tutoriales: los de 10 juegos corren en 3 formas de pantalla —20:9, 18:9, 16:9— y fallan si Nubi, su globo o «Saltar tutorial» tapan algo; ver `docs/tutoriales-con-nubi.md`; `--juegos Tutorial,TutorialFreno,CortoFreno…`).
- Léxico de Lluvia de meteoros: 11 pruebas del filtro de nombres propios en `tools/lexico/test_propios.py` (`python -m unittest test_propios`, desde `tools/lexico`).
- Banco de definiciones de «En la punta de la lengua» (reemplaza a Anagramas): 16 pruebas en `tools/punta/test_banco.py` (`python -m unittest test_banco`, desde `tools/punta`; ~1 s). Diseño: `docs/diseno-punta-de-la-lengua.md`.
- Frases de ¿Verdad o disparate?: 32 pruebas en `tools/frases/test_disparate.py` (`python -m unittest test_disparate`).
- Rondas de Cosecha de palabras: 17 pruebas en `tools/cosecha/test_rondas.py` (`python -m unittest test_rondas`, desde `tools/cosecha`; ~70 s;
  con `COSECHA_REGENERAR=1` reconstruye todo desde las fuentes). Diseño aprobado en `docs/diseno-cosecha-de-palabras.md`.
- Herramientas: botones "[Debug]" (`ui/screens/DebugTools.kt`, solo builds de depuración) para abrir cada juego,
  ver las celebraciones y repetir el inicio sin borrar datos. "Borrar datos" en Ajustes deja la app como recién instalada.

## Pendientes vigentes

El estado y las decisiones de diseño de la app (Hoy = Nubi, Juegos en 4 áreas, dificultad y avance, Anagramas —hoy «En la punta de la lengua»—, nombre e ícono)
están en [docs/historial-desarrollo.md](docs/historial-desarrollo.md) § «Estado y notas de diseño de la app». Dificultad y avance: [docs/dificultad-y-avance.md](docs/dificultad-y-avance.md).
Qué sigue de juegos y orden: [docs/hoja-de-ruta.md](docs/hoja-de-ruta.md).

- Ideas en espera (NO implementar hasta que Ricardo lo pida): rangos de tripulación en vez de ligas de metales y
  "Tu astronauta" (avatar propio, color de acento elegido). Detalle en [`docs/ideas-guardadas.md`](docs/ideas-guardadas.md).
- Después, en la lista de Ricardo: revisar qué juegos usa la evaluación inicial ("los juegos no me quedan claros");
  re-chequeo mensual del punto de partida; tutorial guiado en los otros 19 juegos (la pieza común ya está); marca ✓/✗ de arcilla sobre la
  respuesta; calibrar el DDA y la referencia de percentiles con datos.
- Para el final: i18n completo (hoy `ui/i18n/AppStrings` cubre solo algunos textos); `applicationId` propio
  (cambiarlo = app nueva); `metadata.json` de AI Studio; firma release y Play Store; servidores.
- Antes de publicar: búsqueda oficial de marca (clases 9, 41) y el `applicationId` definitivo ([docs/nombre-marca-y-riesgos.md](docs/nombre-marca-y-riesgos.md)).
- Pendiente de licencia: SPALEX (léxico de Lluvia de meteoros), ver [docs/diseno-lluvia-de-meteoros.md](docs/diseno-lluvia-de-meteoros.md).
