# Cuando Android cierra la app, respaldo, registro de errores y verificación automática

> Movido TAL CUAL desde `CLAUDE.md` el 2-oct. Los resúmenes con las reglas que no se rompen quedaron en `CLAUDE.md`.

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
- **Pruebas con base de datos (8-oct, tarea 45): sin archivos, sin reintentos.** Antes la base de las pruebas con Robolectric era el archivo `neurovida_database` dentro de la carpeta temporal que Robolectric borra entre pruebas, y
  fallaba sola con «unable to open database file» (con la máquina cargada, ~1 vuelta de cada 2 en el bucle de medición); se tapaba con reintentos. Ahora, **toda prueba que cree el `NeuroVidaViewModel` o el
  `NeuroVidaRepository` o lea la base** hace esto:
  - `@Before` y `@After`: `TestSupport.resetDatabase()`. Cierra lo de la prueba anterior, **cancela el repositorio de la aplicación** (su flujo de Room ya no sigue vivo) y deja una base de Room **en memoria**, nueva y vacía,
    fijada con la costura `NeuroVidaDatabase.setInstanceForTesting` (solo pruebas; en producción nadie la llama). Una base en memoria no tiene archivo que pueda desaparecer.
  - Si la prueba crea su propio `NeuroVidaRepository(app)`: `TestSupport.release(repo)` en el `@After`, antes de `resetDatabase()`. Si crea ViewModels: `TestSupport.release(*viewModels)`. **Soltar ESPERA** (hasta 10 s) a que termine el trabajo que ya estaba en marcha:
    `NeuroVidaRepository.close()` cancela su alcance y espera a que se complete, y `release` espera a los trabajos de cada ViewModel. Motivo (Tarea 57, el rojo intermitente de GitHub: `RetiredBitacoraTest` con «UncaughtExceptionsBeforeTest … attempt to re-open an already-closed object: SQLiteDatabase: :memory:»):
    antes solo se CANCELABA; una lectura de Room que ya estaba a medias no se puede interrumpir y seguía corriendo cuando se cerraba la base de esa prueba; al terminar, su `endTransaction` encontraba la base cerrada y el error llegaba a la prueba siguiente (dependía de la velocidad de la máquina). Las pruebas que
    creaban ViewModels sin soltarlos (`GameFlowTest` y otras cinco) eran la fuente. `BackgroundWorkShutdownTest` lo reproduce siempre (una base cuyas consultas tardan 150 ms) y falló con el código viejo; `NoDiskDatabaseGuardTest` revisa que ninguna prueba cree el ViewModel o el repositorio sin soltarlos.
  - **WorkManager queda apagado** en esas pruebas (`CognitiveReminderWorker.disabledForTests = true`, lo pone `resetDatabase()`): WorkManager abre su propia base en archivo y trabaja en hilos propios. Ya no hace falta
    `WorkManager.initialize` ni crear carpetas a mano. La única prueba que usa WorkManager de verdad (`ExampleRobolectricTest`, «workmanager reminder schedules») lo enciende mientras corre y lo deja como estaba.
  - `NoDiskDatabaseGuardTest` es el guardián: comprueba que la base de las pruebas es en memoria y sin archivo, y revisa el código de las pruebas: una que use la base, el repositorio o el ViewModel sin `TestSupport.resetDatabase()`,
    o que abra `Room.databaseBuilder(` en archivo, falla con su nombre.
  - Para medir una prueba sospechosa: bucle de `./gradlew.bat :app:testDebugUnitTest --tests "*Nombre" --rerun` (30 vueltas), con carga de CPU en paralelo (otra compilación) para imitar a IL2CPP.
- **Verificación automática** (`.github/workflows/verificar.yml`, `tools/unity-falso.sh`): en cada push y pull request
  GitHub compila el C# de los juegos sin Unity y compila la app con sus pruebas Kotlin (con un unityLibrary falso).
  Un ✗ rojo en el commit = algo no compila o una prueba falló. NO corre las pruebas de Unity, el arranque de los 22
  juegos ni el export: eso sigue siendo `verificar-todo.sh` en el PC de Ricardo. Ricardo confirmó que corre en verde
  (pestaña Actions); usa `ubuntu-24.04` fijo (no `latest`) y acciones en su versión actual.
- Lo que NO se hizo de la revisión de arquitectura (medidas genéricas, idiomas, dividir el ViewModel, datos en dos
  lugares, versión de tienda, controladores grandes de Unity) está con pasos concretos en
  [`docs/plan-mejoras-arquitectura.md`](plan-mejoras-arquitectura.md).
- **Competencia** (29-sep): Ricardo guardó capturas de Lumosity, NeuroNation y Peak en `Proyectos/pantallazos …/`
  (fuera del repo). Qué tomar, qué no copiar y el orden sugerido (registro de sueño y ánimo, tutorial por juego,
  comentarios por juego, escudo de racha, atajos Reforzar/Rápido…) está en
  [`docs/analisis-competencia.md`](analisis-competencia.md). Nada de eso está implementado todavía.

## El smoke y la guardia de textos (detalle que antes vivía en CLAUDE.md)

- Los tutoriales de 15 juegos corren en 3 formas de pantalla (20:9, 18:9, 16:9) y fallan si Nubi, su globo o «Saltar tutorial» tapan algo (ver `docs/tutoriales-con-nubi.md`; `--juegos Tutorial,TutorialFreno,CortoFreno…`). Los juegos que no tienen tutorial corren además una
  corrida «Pantalla…» en forma de teléfono (lienzo de 1080×2400), y Correo dos (20:9 y 16:9). Satélites (Tarea 59) corre además `Satelites` (55 s: se juega solo desde el nivel 6 y pasa por las tres sorpresas) y `PantallaSatelites` (la misma partida en forma de teléfono, con la guardia de textos). Las corridas `HowTo…` (Tarea 56: Cosecha, ¿Verdad o disparate? y La estrella intrusa; Tarea 59: Satélites) abren «Cómo se juega» a los 9 s de partida y fallan si no termina, si hay un error o si la
  partida no vuelve a estar en marcha (prueban que la partida se aparta y se retoma bien). En los pasos de «tocar» de esos tutoriales el smoke da además un toque «de verdad» en el centro del hueco y comprueba que llega al juego.
- **Guardia de textos** (todas esas corridas): todo texto visible cae dentro de la pantalla y, si es hijo de un botón o una píldora, dentro de él; mide lo que se DIBUJA (no el rect entero) y solo informa un texto que queda fuera en dos revisiones seguidas. Hace FALLAR el smoke en cualquier juego;
  las únicas excepciones (rótulos que cuelgan a propósito de su imagen) están en `TextGuardExceptions` de `HeadlessPlaymodeSmokeTest.cs`.
  Desde el 8-oct (Tarea 54) la guardia tiene dos reglas más: **(1) letra ≥ 14 dp EXACTOS** (el tamaño efectivo —el del ajuste automático, por la escala del objeto— entre las unidades por dp del lienzo: 1080 de ancho = 360 dp, 42 unidades; excepciones solo en `TextSizeExceptions`, con su porqué: la marca de versión de la cuenta regresiva y Bitácora, retirada) y **(2) nada tapa el HUD** (`ScanHudCover`: todo lo visible que no es del HUD, del mismo lienzo y dibujado DESPUÉS de un texto del HUD, que cubra más de la cuarta parte de lo que ese texto dibuja; no cuentan los fondos, los velos, los destellos ni las piezas de foco del tutorial). Los avisos (`Toast`) van DEBAJO del marcador y, además (Tarea 55), NO tapan el juego: al mostrarse buscan la primera franja libre (`ToastPlacement`: no pisan ningún texto, botón ni el estímulo —imágenes con dibujo—, ni las zonas que el juego declara con `Toast.KeepOut`: el campo de los meteoros, la arena de Satélites, el radar…). Si en ese momento no hay franja (pantalla corta con el estímulo puesto), esperan «entre ensayos» hasta 4 s y entonces salen donde menos chocan. El smoke tiene corridas `Aviso*` (13 juegos, pantalla alta y baja) que MUESTRAN un aviso de prueba de dos renglones y comprueban que no tapa nada (`ScanToastCover`); excepciones con su porqué en `ToastCoverExceptions` (Piloto y Rumbo, que se rehacen en las etapas 1 y 2).  Lo que el smoke no ve (un texto que solo sale en cierta fase) lo cubre `HudRulesTests` mirando el CÓDIGO: ningún `MakeText`/`BestFit`/`CenteredText`/`AddResultText` con letra bajo 42 unidades, todos los juegos con aviso lo ponen debajo del HUD, los puntos de avance caben con 16 dp de margen y un aviso del tutorial no deja esquinas del foco anterior. Cada arranque deja su duración en el log (`[SmokeTest] tiempo …`).
- **Capturas reales** (opcional, 8-oct): `bash tools/verificar-todo.sh --capturas todos` (los 19 juegos, ~18 min; o `--capturas Radar` / `Radar,Freno`; Unity SIN `-nographics`, necesita tarjeta de video; no corre en el CI y no toca el smoke). `Bootstrap/Editor/ScreenshotRunner.cs` corre cada juego en forma de teléfono (1080×2400) y saca, SIN guion por juego: la primera pantalla jugable (cuando termina la cuenta regresiva), a los 8, 20 y 40 s de juego (donde el smoke tiene piloto automático: Engranajes, Bodega, Constelaciones y Correo, se juegan solos; los demás repiten su primera situación), la pausa, la cortina «¡Listo!» y la pantalla final si la partida llega a su fin, UNA toma con «quitar animaciones» y, en los juegos con tutorial, el paso 1 y el paso 3. Correo conserva su guion propio (`MailGameController.EditorShotScript`) y suelto (`--capturas Correo`) sigue dando `docs/previews/correo-estacion.png`. `tools/capturas/hoja.py` arma una lámina por juego en `docs/previews/capturas/<juego>.png` (con `todos`, también el índice `README.md` con fecha, commit y tiempos); `docs/previews/capturas/hallazgos.md` es la lista, hecha a mano, de lo que se ve mal a primera vista. Las tomas sueltas, los errores de consola de cada juego (`errores.txt`) y los tiempos (`_resumen.txt`) quedan en `unity/test-results/capturas/<juego>/` (fuera de git). Primera corrida completa (8-oct): 141 capturas en 17,5 min (≈ 55 s por juego).
