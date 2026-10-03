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
