# Plan de mejoras de arquitectura (29-sep)

Sale de una revisión del código pedida por Ricardo ("ver mejoras que permitan desarrollar los aspectos débiles").
Aquí queda lo que se hizo, dos correcciones al diagnóstico inicial y, sobre todo, **lo que quedó pendiente con pasos
concretos**, para seguirlo sin repetir la revisión. No se midió rendimiento en un teléfono (arranque, memoria,
batería): esa revisión sigue pendiente.

## Hecho el 29-sep

| Tema | Qué se hizo | Dónde |
|---|---|---|
| Respaldo del progreso | Reglas explícitas: se respalda el progreso (base de datos + 7 preferencias) y no el estado pasajero | `res/xml/backup_rules.xml`, `data_extraction_rules.xml`, `BackupRulesTest` |
| Registro de errores | Cierres inesperados y resultados de Unity que no se pudieron leer quedan anotados en el teléfono; Ajustes → "Enviar informe de errores" | `diag/ErrorLog`, `ErrorLogTest` |
| Pruebas de los flujos frágiles | 13 recorridos del ViewModel (volver de un juego, pausa, camino diario, Android cerrando la app, misma casilla en Juegos) y 7 de lo guardado en disco | `flow/GameFlowTest`, `bridge/GameSessionStoreTest` |
| Migraciones de la base de datos | Prueba 10→11 con datos y prueba que exige una migración por cada versión exportada | `data/local/MigrationTest` |
| Verificación automática | GitHub compila el C# sin Unity y la app con sus pruebas Kotlin en cada subida | `.github/workflows/verificar.yml`, `tools/unity-falso.sh` |
| Pequeños arreglos | `clearInFlight` no borraba el modo guardado; un `catch` vacío en la vibración ahora deja rastro | `GameSessionStore`, `NeuroVidaViewModel` |

Las pruebas nuevas se comprobaron rompiendo el código a propósito (5 de ellas fallaron justo donde debían) y la de
migración también.

**Correcciones al diagnóstico inicial:**
- El respaldo no era "solo preferencias": las líneas de "incluir" estaban comentadas, así que Android respaldaba **todo**,
  incluso la partida en pausa. El problema real era que nunca se decidió qué respaldar.
- Las 28 marcas de "imagen sin descripción" no eran un problema: son íconos decorativos dentro de botones que ya tienen
  su nombre para el lector de pantalla ("Cerrar", "Tu perfil", "Opciones") o junto a un texto.

## Pendiente, en el orden que conviene

### 1. Medidas genéricas de los juegos estrella — antes del próximo juego estrella
**Problema.** Cada juego estrella suma campos a `StroopSessionMetricsDto` y a `GamePlayResult` (hoy 72 campos), ramas en
`NativeReceiver` (430 líneas, unas 38 ramas por juego) y composables en `GameResultScreen` (1.269 líneas). Un juego nuevo
toca unos 6 archivos entre C# y Kotlin, y cualquier error ahí rompe el final de todos los juegos.
**Propuesta.**
1. Unity manda `measures`: una lista de `{ key, value, values[] }` (números y series) en vez de un campo por medida.
2. En la app, un registro `MeasureSpec` por juego (título, cómo se formatea, referencia, gráfico) y `GamePlayResult.measures`.
3. Migrar de a un juego (empezar por Correo Estelar o Rumbo a Casa), dejando los campos viejos hasta migrar el último; la
   evolución de `star_measures` (`StarMeasures.encode`) debe seguir leyendo lo ya guardado.
4. Una prueba que exija que cada juego estrella tenga su `MeasureSpec`, y una por juego con un JSON real de Unity.
**Riesgo.** Toca el puente y los finales de los 10 juegos estrella: hacerlo con `GameFlowTest` en verde y probando cada
final en el teléfono.

### 2. Idiomas — antes de la tienda (es el trabajo más grande)
**Problema.** La app es para un mercado global pero todo el texto está en español dentro del código: `strings.xml` tiene
1 entrada, hay textos fijos en las pantallas y ~34.000 líneas de C# con textos en español; la configuración que la app
manda a Unity no lleva idioma.
**Propuesta.** (a) Decidir los idiomas (por ejemplo es + en). (b) Pasar los textos de la app a `strings.xml`. (c) Mandar
`language` en la configuración de Unity y una tabla de textos por idioma en C# (`Loc.T("clave")`), migrando juego por
juego. (d) Cuidar que las medidas del final sigan diciendo solo lo que miden en cada idioma (revisar con la regla de
`CLAUDE.md`).

### 3. Dividir el `NeuroVidaViewModel` (910 líneas, 51 funciones, 35 estados)
Ahora que hay red de seguridad (`GameFlowTest`), se puede dividir sin miedo: primero sesión diaria y biblioteca de
juegos, después perfil/ajustes y evaluación. Empezar dejando el ViewModel actual como fachada que delega, y solo después
mover las pantallas. En paralelo, reemplazar `NeuroVidaApplication.instance` (usado en 4 archivos del puente) por un
contenedor de dependencias simple (no hace falta Hilt) para poder probar el puente con un falso.

### 4. Datos repartidos entre la base de datos y ~10 archivos de preferencias
Cada archivo tiene su propio formato, sin versión ni migración (la base de datos sí las tiene). Opciones: pasar
`skill`, `star_measures`, `progress_log`, `league_events` y `achievements` a Room, o al menos guardar una versión de
formato en cada uno y probar que se lee lo guardado por la versión anterior. Sumar "exportar / importar mi progreso" como
archivo (sirve para cambiar de teléfono y para cuando haya cuentas). `BackupRulesTest` ya obliga a decidir el respaldo
de cada preferencia nueva.

### 5. Versión de tienda
Sin reducción de tamaño en release (`isMinifyEnabled = false`), `versionCode` 1, sin llave de firma (el `build.gradle`
la busca en variables de entorno: `KEYSTORE_PATH`, `STORE_PASSWORD`, `KEY_PASSWORD`), `applicationId` heredado de AI
Studio. Antes de publicar: probar un **release real** en el teléfono (Unity + Moshi necesitan reglas de conservación si
se activa la reducción), definir la firma y el `applicationId`, y activar las estadísticas de fallos de Google Play. Un
cierre nativo del motor de Unity no lo registra `ErrorLog`: solo lo verá Android vitals.

### 6. Controladores de Unity muy grandes (1.100 a 1.600 líneas)
La pantalla de cada juego se arma por código y cada prueba visual exige exportar y compilar (3 a 5 min). Separar cada
controlador en reglas/flujo y dibujo, y una escena "galería" para ver los estados de cada pantalla sin jugar. Hacerlo al
tocar cada juego, no en bloque.

## Cómo saber si la verificación automática funciona
`.github/workflows/verificar.yml`: pestaña **Actions** del repositorio. Funcionó a la primera (29-sep, los dos trabajos en
verde). Ese mismo día se actualizaron las versiones de las acciones (avisaban de Node 20 obsoleto) y se fijó
`ubuntu-24.04` en vez de `ubuntu-latest`, que pasa a Ubuntu 26 el 19-oct-2026 y podría no traer el mismo Android SDK.
Si algo falla, el informe queda como archivo `informe-pruebas-kotlin` en esa ejecución. Si molesta mientras se ajusta, se puede desactivar sin borrar nada
(Actions → Verificar → "…" → Disable workflow).
