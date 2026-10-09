# Nubi — memoria del proyecto (al 2-oct)

**Nombre público desde el 29-sep: Nubi** ("Nubi – Brain Games"; el personaje es una nebulosa pequeña de arcilla).

> **Nombres internos.** La app es Nubi, pero por dentro todavía dice "NeuroVida" y NO se cambia: el archivo de la base `neurovida_database` y los nombres de las preferencias (renombrarlos perdería el progreso), los namespaces y asmdefs de Unity
> (`NeuroVida.*`, `unity/NeuroVidaCore/`) y clases Kotlin (`NeuroVidaViewModel`…), y el `applicationId` (`com.aistudio.neurovida.cgnv`, se define UNA vez antes de publicar: `docs/auditoria-30-sep.md`). La carpeta es `C:/Users/RURAL7/Desktop/Proyectos/Nubi`
> (renombrada el 1-oct; Unity Hub necesita agregar el proyecto `unity/NeuroVidaCore` de nuevo). La app no muestra "NeuroVida" en ningún texto visible; en los documentos, "NeuroVida" solo aparece en identificadores o para el nombre viejo.

App Android de juegos mentales cortos para mantener la mente activa: 19 juegos en 4 áreas (Memoria, Atención, Razonamiento y Lenguaje; de 6 a 4 áreas el 30-sep; juegos retirados en `docs/juegos/descartados.md`), Room v15, dificultad que se adapta, camino diario de 3 juegos, ligas con trofeos, logros,
racha y un punto de partida inicial. La app (menús, progreso, datos) es Kotlin + Compose; los juegos corren en Unity embebido ("Unity as a Library"). Dueño y quien prueba: Ricardo (psicólogo). Meta: superar a Lumosity/Peak/Elevate en calidad y en motivación.

El diario detallado de cómo se llegó hasta aquí (decisiones, bugs, pedidos de Ricardo) está en
[`docs/historial-desarrollo.md`](docs/historial-desarrollo.md). Es historia: lo que manda es este archivo y el código.

## Cómo trabajamos (nube ↔ PC)

- Las sesiones en la nube editan, verifican C# con `dotnet build tools/unity-compile-check -v q` y hacen push a la rama de trabajo; **la app Kotlin SÍ se compila y prueba allá**: `bash tools/nube-compilar-app.sh` (unityLibrary FALSO; con `fotos` graba capturas Roborazzi). No correrlo en el PC de Ricardo (allá `unity/AndroidExport/` es el export real).
- En el PC de Ricardo (Git Bash, carpeta del repo): `git pull && bash tools/verificar-todo.sh --instalar` →
  escena piloto, pruebas EditMode, smoke de los juegos de Unity (en UN solo Unity; Bitácora, retirada de la app, sigue en Unity y en el smoke), REEXPORTAR Unity, Gradle con pruebas Kotlin, instalar.
  Salida corta; los logs completos van a `unity/test-results/v-*.log`. Modos para iterar: ver «Cómo trabajar una tarea de un juego».
- `unity/AndroidExport/` está fuera de git: si no se reexporta, el APK lleva los juegos viejos sin avisar.
  Marca de verificación: en builds de depuración la cuenta regresiva muestra `CountdownScreen.StyleStamp`
  (hoy `estilo 9-oct · enciende tu planeta`). **Cambiarla con cada cambio visible de Unity.**
- Vistas previas sin Unity ni teléfono: `tools/art-preview` (compila los generadores de sprites REALES y vuelca PNG; `correo.py`, `satelites.py`… arman las láminas de piezas) y `tools/previews/*.py` (réplicas PIL de pantallas Compose); resultados en `docs/previews/`. Si se cambia el arte, actualizar la lámina. **Capturas REALES** (el juego de verdad, no una composición): `bash tools/verificar-todo.sh --capturas todos` (los 19 juegos, ~18 min; necesita tarjeta de video; láminas en `docs/previews/capturas/`; detalle en `docs/respaldo-y-diagnostico.md`).
- Estilo con Ricardo: español, sin jerga, cambios chicos y verificables, y decirle siempre qué probar.
- Qué sigue y en qué orden (la ruta por etapas que Ricardo aprobó el 8-oct): [`docs/hoja-de-ruta.md`](docs/hoja-de-ruta.md).
- Repositorios externos: si alguno de GitHub puede potenciar la app, COMENTARLO a Ricardo y él decide; nunca agregarlo sin preguntar (descartados y razones: hoja de ruta).

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
  Inicio «Primer vuelo con Nubi» (`FirstFlightScreen` + `FirstFlightHost`, `data/FirstFlight.kt`, `docs/diseno-inicio.md`): 19 pasos con 4 juegos (uno por área) y preguntas intercaladas, mientras `UserSettings.ageBand == null` o haya un recorrido guardado.
  Las pestañas se pasan deslizando con el dedo (`HorizontalPager` en `MainActivity`, sincronizado con
  `viewModel.currentTab`). Una ventana abierta sobre una pestaña (área en Juegos, detalle de un área en Hoy o Avance)
  llama `LockTabSwipe()`: mientras esté, el dedo no cambia de pestaña y la barra de abajo se esconde.
- Sesión diaria = SOLO los 3 juegos del camino (`startDailySession` → `continueDailyFlow`), elegidos por avance real y con variedad por `data/DailyPath` (reglas en su cabecera; nunca un juego sin tutorial que la persona no jugó). El día en que se completa el Primer vuelo, el camino de ese día queda cumplido con el vuelo (`completeTodayWithFlight`: completado y sin puntajes; Hoy lo dice). Al terminar, el resumen (`data/SessionSummary` + `SessionSummaryScreen`): áreas trabajadas, puntaje de cada juego, racha y «Hoy avanzó de X a Y» por área.
- Lógica pura con pruebas en `data/`: `Achievements`, `AreaProgress`, `Baseline`, `DailyPath`, `FirstFlight`, `DdaRating`, `Homing`, `Mail`, `LeagueEvents`, `NumberLine`, `RetoChoice`,
  `Planet`, `SessionSummary`, `StarMeasures`, `Library`, `Skill`; y
  `notification/ReminderContent`. Modelos y catálogo de juegos (`GameRegistry`, `RankTier`, ...) en `model/Models.kt`.
- Persistencia: **Room v15** (`data/local/`, `exportSchema`, esquemas en `app/schemas/`; resultados, progreso por
  juego con `ddaRating`, sesión diaria, perfil —uno solo: desde el 8-oct no se crean ni cambian perfiles; la tabla `user_profile` y su DAO se quedan—, maestría, desafíos). Datos que solo se agregan o salen del
  onboarding van en **SharedPreferences** para no migrar (`league_events`, `achievements`, `profile_extra`, `skill`, `star_measures`, `progress_log`, `mission_log` —de Bitácora, retirada—, `paused_game`, bandeja de resultados de Unity…).
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
  «Carga exacta», «Engranajes», «En la punta de la lengua», **Rastro de luz** (escalera de 16 niveles), **Constelaciones** (18 etapas en 6 grupos) y **Correo Estelar** (10 etapas; cada encargo es un ensayo); **Piloto Estelar** usa DOS instancias (pilotaje y señales); **Radar**, **Satélites** y **Freno de Emergencia** (solo la tarea de ir) una. Desde el 3-oct los 23 juegos usan el motor común. Fundamento: Levitt 1971 y Kaernbach 1991; el 80/85 % es decisión de diseño (`docs/DDA-comun.md`).
- Cada juego: `XContract.cs` (reglas puras con pruebas) + `XGameController.cs` (UI construida por código). Los 7 del
  DDA común heredan de `Shared/GameControllerBase` y usan `Shared/GameHud`.
- `Games/Shared/`: sello visual y piezas comunes — `NeuroStyle` (paleta de la app, `ClayText`, `ClayFrame`),
  `ClayRaster` (pincel SDF para todo el arte en arcilla), `WorldBackdrop` (cielo + un elemento propio por juego),
  `StarfieldFx`, `CountdownScreen`, `FinishCurtain` + `ExitButton` (cierre "¡Listo!" → resultado en la app),
  `GameFeel` (sonidos sintetizados y vibración), `Motion` («quitar animaciones»: ver [docs/movimiento-reducido.md](docs/movimiento-reducido.md)), `GameClock` (tiempo pausable), `PauseMenu`, `Assessment`
  (modo evaluación), `UiKit`, `Toast`, `PhasePill`, `PressScale`, sprites varios, y el **tutorial guiado común** (`GuidedTutorial` + `NubiTeacherSprite`;
  gancho `GuidedRound` en `GameControllerBase`; la app manda `show_tutorial` si no hay partidas del juego: `UnityGameLauncher.TUTORIAL_GAMES` = Rastro de luz, Freno, Aterrizaje, Meteoros, Tinta o Palabra, En la punta de la lengua, Carga exacta, Engranajes, Bodega de carga, Constelaciones, Correo Estelar, Cosecha de palabras, ¿Verdad o disparate?, La estrella intrusa y Satélites; «Cómo se juega» en la pausa; cómo sumar otro: `docs/diseno-rastro-de-luz.md`; colocación de Nubi: `docs/tutoriales-con-nubi.md`).

## Juegos (índice)

Cada juego tiene su ficha técnica (cómo funciona, reglas, medidas, decisiones, pruebas) en el documento de la tabla: los cuatro de
Lenguaje con diseño propio y los demás en `docs/juegos/<id>.md` ([catálogo y orden](docs/juegos/README.md); descartados en
[docs/juegos/descartados.md](docs/juegos/descartados.md)). Cada juego estrella lleva una MEDIDA PROPIA al final de la partida.

| Juego | id | Área | Carpeta Unity | Documento (ficha técnica) | Lectura en la app (`data/`) |
|---|---|---|---|---|---|
| Constelaciones (antes Parejas Ocultas; renovado el 7-oct, conserva el id) | `parejas` | Memoria | `Games/Parejas/` | [docs/diseno-constelaciones.md](docs/diseno-constelaciones.md) | `Constelaciones.kt` |
| Rastro de luz (antes Secuencia Lumínica) | `secuencia` | Memoria | `Games/Secuencia/` | [docs/diseno-rastro-de-luz.md](docs/diseno-rastro-de-luz.md) | `Trail.kt` |
| Bodega de carga (nuevo, 5-oct; ocupa el lugar de Bitácora de Misión con otro id) | `bodega` | Memoria | `Games/Bodega/` | [docs/diseno-bodega-de-carga.md](docs/diseno-bodega-de-carga.md) | `Bodega.kt` |
| ~~Bitácora de Misión~~ **RETIRADA el 5-oct-2026** (la app ya no la lanza; el código de Unity y el historial se conservan; ver `docs/juegos/descartados.md`) | `bitacora` | (Memoria) | `Games/Bitacora/` | [docs/juegos/bitacora.md](docs/juegos/bitacora.md) | — |
| Rumbo a Casa | `rumbo` | Memoria | `Games/Rumbo/` | [docs/juegos/rumbo.md](docs/juegos/rumbo.md) | `Homing.kt` |
| Correo Estelar — «La estación de correo» (renovado el 8-oct, conserva el id) | `correo` | Memoria | `Games/Correo/` | [docs/diseno-correo-estacion.md](docs/diseno-correo-estacion.md) (medida: [docs/medidas-juegos-estrella.md](docs/medidas-juegos-estrella.md); el vuelo viejo, solo historia: [docs/juegos/correo.md](docs/juegos/correo.md)) | `Mail.kt` |
| Tinta o Palabra («Dos orillas», 3-oct) | `stroop` | Atención | `Games/Stroop/` | [docs/diseno-tinta-o-palabra.md](docs/diseno-tinta-o-palabra.md) | `DosOrillas.kt` |
| Piloto Estelar | `piloto` | Atención | `Games/Piloto/` | [docs/juegos/piloto.md](docs/juegos/piloto.md) | — |
| Freno de Emergencia | `freno` | Atención | `Games/Freno/` | [docs/juegos/freno.md](docs/juegos/freno.md) | — |
| Satélites — «enciende tu planeta» (renovado el 9-oct, conserva el id) | `satelites` | Atención | `Games/Satelites/` | [docs/diseno-satelites.md](docs/diseno-satelites.md) (el anterior, historia: [juegos/satelites.md](docs/juegos/satelites.md)) | `Satelites.kt` |
| Radar | `radar` | Atención | `Games/Radar/` | [docs/juegos/radar.md](docs/juegos/radar.md) | — |
| Carga exacta (reemplaza a Cálculo Sereno el 4-oct; conserva el id) | `calculo` | Razonamiento | `Games/Calculo/` | [docs/diseno-carga-exacta.md](docs/diseno-carga-exacta.md) | `Carga.kt` |
| Engranajes: Taller de reparación (nuevo, 5-oct; ocupa el lugar de Tráfico Estelar con otro id) | `engranajes` | Razonamiento | `Games/Engranajes/` | [docs/diseno-engranajes.md](docs/diseno-engranajes.md) | `Engranajes.kt` |
| Acoplamiento | `acoplamiento` | Razonamiento | `Games/Acoplamiento/` | [docs/juegos/acoplamiento.md](docs/juegos/acoplamiento.md) | — |
| Aterrizaje Lunar | `aterrizaje` | Razonamiento | `Games/Aterrizaje/` | [docs/juegos/aterrizaje.md](docs/juegos/aterrizaje.md) | `NumberLine.kt` |
| En la punta de la lengua (reemplaza a Anagramas el 3-oct; conserva el id) | `anagramas` | Lenguaje | `Games/Anagramas/` | [docs/diseno-punta-de-la-lengua.md](docs/diseno-punta-de-la-lengua.md) | `Punta.kt` |
| Lluvia de meteoros | `meteoros` | Lenguaje | `Games/Meteoros/` | [docs/diseno-lluvia-de-meteoros.md](docs/diseno-lluvia-de-meteoros.md) | `Vocabulary.kt` |
| ¿Verdad o disparate? | `disparate` | Lenguaje | `Games/Disparate/` | [docs/diseno-verdad-o-disparate.md](docs/diseno-verdad-o-disparate.md) | `Reading.kt` |
| Cosecha de palabras | `cosecha` | Lenguaje | `Games/Cosecha/` | [docs/diseno-cosecha-de-palabras.md](docs/diseno-cosecha-de-palabras.md) | `Harvest.kt` |
| La estrella intrusa | `intrusa` | Lenguaje | `Games/Intrusa/` | [docs/diseno-estrella-intrusa.md](docs/diseno-estrella-intrusa.md) | `Atlas.kt`, `FigureBank.kt` |

## Cómo trabajar una tarea de un juego

- Lee SOLO la carpeta del juego (`unity/NeuroVidaCore/Assets/Scripts/Games/<Carpeta>/`), su documento (tabla de arriba) y los puntos de contacto: la columna «Lectura en la app» y los comunes (`GameRegistry` en `model/Models.kt`, `bridge/NativeReceiver.kt`, `Bootstrap/GameEntryPoint.cs`, `Contracts/StroopTelemetry.cs` o la telemetría propia, `games/GameResultScreen.kt`, `data/StarMeasures.kt`, `data/Skill.kt`, `HeadlessPlaymodeSmokeTest.cs`).
- NO leas `docs/historial-desarrollo.md` ni los documentos o carpetas de otros juegos salvo que la tarea lo pida.
- Mientras iteras: `bash tools/verificar-todo.sh --juegos <Juego> --filtro-tests <Juego>` (smoke solo de ese juego en un Unity y
  EditMode solo de sus pruebas). Si el cambio es solo de Kotlin: `--solo-app` (avisa si `unity/` cambió desde el último export).
  `--instalar` en cualquiera para llevarlo al teléfono. Capturas: `./gradlew.bat :app:recordRoborazziDebug --tests "*<Juego>*"`.
- Verificación COMPLETA (`verificar-todo.sh` sin flags) solo antes de un merge a `main` o cuando se toca código compartido
  (`Games/Shared`, `Bridge`, la parte común de `GameResultScreen`, Room, DDA común, `verificar-todo.sh` o el smoke).

## Reglas que no se rompen

- **Diseño**: "noche + arcilla": cielo nocturno animado (**fondo C**, `docs/previews/fondo-oscuro.png`; colores en `CosmosBackground` y `NeuroStyle.Night*`); lo tocable es arcilla (borde tinta grueso `Ink`, sombra dura, colores Coral/Sun/Sky/Grape/Lime/Cream). **Tipografía**: `AppFamily` elige por peso: normal y medio = Nunito, seminegrita y negrita = Fredoka (títulos, botones, números); fuentes OFL en `res/font`. **Tamaños**: ningún texto bajo 14 sp (13 solo en la barra de pestañas y rótulos de gráficos). En los juegos de Unity ningún texto bajo 14 dp (42 unidades del lienzo de 1080): lo mide el smoke y `HudRulesTests`; los avisos (`Toast`) buscan solos una franja libre bajo el HUD (`ToastPlacement`): no tapan texto, botones ni estímulo; el smoke lo prueba (corridas «Aviso*»). Las pantallas principales NO usan recuadros para informar (texto suelto, objetos, líneas finas); tarjetas solo en diálogos. Nada de emojis como íconos; acierto/error nunca solo por color (forma o texto); contraste ≥ 4.5:1; respetar "quitar animaciones", sonido y vibración apagados. Usar la skill `ui-ux-pro-max` (`.claude/skills/`) para decisiones de diseño.
- **Juegos Unity**: medir el tiempo con `GameClock.Time/DeltaTime` (no `Time.unscaled*`), para que la pausa funcione.
  El arte se hornea en sprites con `ClayRaster` (`Image.color` blanco); la sombra dura cae siempre hacia abajo.
- **Símbolos** (4-oct): nada de estrella con puntas, media luna ni cruz en juegos ni íconos (luceros redondos, hexágono, gota, ola): ver [docs/simbolos-neutros.md](docs/simbolos-neutros.md). Los planetas-puerto de colores viven en `Games/Shared/PortSprites` (antes en Tráfico Estelar).
- **Textos**: español, cercanos, sin culpa ni promesas de salud ("no es un examen", nada de "fortalece neuronas").
  **Medidas del final de los juegos estrella**: nombrar lo que se mide de verdad (no un rasgo), decir cómo leerlo, no sacar conclusiones de pocos ensayos, dar un consejo concreto cuando se pueda y comparar con estudios solo si la condición es comparable. Al pie va la nota común "Medida de esta partida... No es un diagnóstico". Justificación y referencias de cada medida: [`docs/medidas-juegos-estrella.md`](docs/medidas-juegos-estrella.md); al crear un juego estrella nuevo, agregar ahí su medida.
  No se muestran percentiles ni comparaciones con una referencia supuesta (8-oct). Vocabulario visible: juego, partida, camino, tu avance, áreas, mente activa; nada de «cognitivo», «entrenamiento», «cerebro» ni promesas de salud.
- **Licencias**: mecánicas genéricas, pero nombres, arte, textos y sonidos propios (no copiar a Lumosity & co.). Las tipografías (OFL 1.1) llevan su aviso y el texto completo de la licencia en «Licencias y créditos» (`res/raw/ofl.txt`).
- **Decisiones de Ricardo**: servidores, cuentas, Firebase, suscripciones y requisitos de tiendas se dejan para el
  FINAL. La app debe ser masiva, social y motivadora (ligas, logros, compartir), no clínica.
- **Reglas por patentes** (no romper): Radar es «Rescate relámpago» (sin nave central ni opciones entre las que elegir: US 8,348,671 de Posit); Piloto nunca con inclinación ni sensores del cuerpo; ningún juego calcula un perfil «impulsivo / conservador» ni usa caras con emociones que reaccionen al desempeño (Akili); Parejas siempre con el tablero a la vez; Satélites siempre plano, sin respuesta durante el seguimiento y sin «firma» por series; las etapas de avance en la escala común (US 10,559,221); nombres propios, sin los ajenos (UFOV, Double Decision, NeuroTracker…). Detalle: [docs/nombre-marca-y-riesgos.md](docs/nombre-marca-y-riesgos.md).

## Cuando Android cierra la app, respaldo y verificación automática (resumen; detalle en [docs/respaldo-y-diagnostico.md](docs/respaldo-y-diagnostico.md))

- Con Unity al frente Android puede cerrar el proceso de la app: lo que el flujo necesita vive en disco (`bridge/GameSessionStore`) y el
  resultado pendiente lo procesa el ViewModel al volver. Si se toca `NeuroVidaViewModel` o `GameSessionStore`, correr `flow/GameFlowTest`
  y `bridge/GameSessionStoreTest` antes de instalar (reproducir: Opciones de desarrollador → «No conservar actividades»).
- Respaldo: solo el PROGRESO (la base de datos y las preferencias `skill`, `star_measures`, `progress_log`, `league_events`, `achievements`, `mission_log`, `profile_extra`, `atlas`, `punta_words`, `engranajes_rocket` y los récords `bodega_record`, `constelaciones_record`, `correo_record` y `satelites_record`). Pasajero (NO se respalda): `paused_game`, `game_session`, `unity_results`, `library_focus`, `first_flight`. **Si se agrega un archivo de preferencias nuevo**, `BackupRulesTest` falla hasta que se sume a `res/xml/backup_rules.xml` y `data_extraction_rules.xml` o a `transient` de la prueba.
- Room: al subir `version`: entidad → `Migration(N, N+1)` en `NeuroVidaDatabase.MIGRATIONS` → compilar (genera `schemas/<N+1>.json`; comitearlo) →
  correr pruebas (`MigrationTest`, dos veces la primera: lee el esquema nuevo recién generado).
- Errores en el teléfono: `diag/ErrorLog` (`files/errores.txt`; Ajustes → «Enviar informe de errores»). GitHub Actions
  (`.github/workflows/verificar.yml`) solo compila el C# y la app con pruebas Kotlin: NO corre Unity; eso es `verificar-todo.sh`.
- Competencia (capturas en `Proyectos/pantallazos …/`, fuera del repo): [docs/analisis-competencia.md](docs/analisis-competencia.md).
  Lo que NO se hizo de la revisión de arquitectura: [docs/plan-mejoras-arquitectura.md](docs/plan-mejoras-arquitectura.md).

## Pruebas

- Kotlin: 515 (`./gradlew.bat testDebugUnitTest`; lógica pura en `app/src/test/.../data`, `model`, `notification`; los
  recorridos del ViewModel en `flow/`, lo guardado en disco en `bridge/`, migraciones y respaldo). Las pruebas con
  Robolectric que crean el ViewModel, el repositorio o leen la base llaman `TestSupport.resetDatabase()` en `@Before` y `@After` (Room EN MEMORIA, nunca `neurovida_database`) y SUELTAN lo creado con `TestSupport.release(...)`
  (cancela y ESPERA su trabajo de fondo); `NoDiskDatabaseGuardTest` falla si falta. Detalle: [docs/respaldo-y-diagnostico.md](docs/respaldo-y-diagnostico.md).
- Unity EditMode: 645 (contratos de cada juego, `AdaptiveDifficultyTests`, Constelaciones, La estación de correo, perfil por edad, Rastro de luz, tutoriales guiados, `CoachLayoutTests`) + 104 arranques de smoke (los juegos, las versiones cortas, los tutoriales en 3 formas de pantalla y las corridas «Pantalla…» en forma de teléfono, todos con la **guardia de textos**: un texto fuera de lugar hace fallar el smoke en cualquier juego; detalle y excepciones en [docs/respaldo-y-diagnostico.md](docs/respaldo-y-diagnostico.md)).
- Datos de los juegos de Lenguaje (Python, `python -m unittest <módulo>` desde su carpeta): `tools/lexico` (11 pruebas, nombres propios de Lluvia de meteoros), `tools/punta` (16, banco de definiciones), `tools/frases` (32, ¿Verdad o disparate?) y `tools/cosecha` (17; ~70 s; `COSECHA_REGENERAR=1` reconstruye desde las fuentes).
- Herramientas: botones "[Debug]" (`ui/screens/DebugTools.kt`, solo builds de depuración) para abrir cada juego,
  ver las celebraciones y repetir el inicio sin borrar datos. "Borrar datos" en Ajustes deja la app como recién instalada.

## Pendientes vigentes

Qué sigue y en qué orden: [docs/hoja-de-ruta.md](docs/hoja-de-ruta.md) (etapas 0 a 5 y «lo que NO hacemos ahora»; ahí están los tutoriales y juegos por revisar, la prueba con personas, Avance, la publicación y las cuentas). No se duplica aquí.
El estado y las decisiones de diseño de la app: [docs/historial-desarrollo.md](docs/historial-desarrollo.md) § «Estado y notas de diseño de la app». Dificultad y avance: [docs/dificultad-y-avance.md](docs/dificultad-y-avance.md).
Ideas en espera (NO implementar hasta que Ricardo lo pida): [`docs/ideas-guardadas.md`](docs/ideas-guardadas.md).
