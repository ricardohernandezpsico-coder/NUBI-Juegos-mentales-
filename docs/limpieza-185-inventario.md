# Tarea 185: lo que salió de la app con la isla (inventario corto, con su prueba)

28-oct-2026. Ricardo decidió (27-oct) que arriba de Tú va SOLO Nubi, sin la isla. La isla (`Island`) tenía DOS sitios en la app: la pestaña Tú y la última página del Primer vuelo («Tu isla, la que verás en Tú»); con Tú sin ella, el cierre del Primer vuelo también se quedó sin imagen y pasó a Nubi (decidido con Opus el 28-oct). Se borró lo que quedó sin uso, como en la Tarea 182; el historial de git lo guarda. Las claves de preferencias y las reglas de respaldo NO se tocaron.

**Cómo se probó que nada más lo usa:** (1) el código compila sin ellos (`:app:compileDebugKotlin` y `:app:compileDebugUnitTestKotlin`); (2) el barrido de alcanzabilidad de la Tarea 182 (`unused_kt2.py`: cada declaración de primer nivel de `main` contra quién la nombra, con las raíces de la app) listó, justo después de quitar los usos de Tú, del Primer vuelo y de la celebración, exactamente estos bloques como muertos, y después de borrarlos no apareció ninguno nuevo del lado de la isla; (3) el barrido de recursos (`unused_res2.py`: cada `R.<tipo>.<nombre>` y `@<tipo>/<nombre>` del código vivo y de los XML) listó exactamente los 26 archivos de abajo y, tras borrarlos, 0; (4) `NubiAssetsTest` («el APK no lleva arte sin uso en drawable-nodpi») pasa.

## Código de `app/src/main` (≈ 480 líneas)

| Qué | Dónde | Líneas | Por qué estaba y por qué ya no |
|---|---|---|---|
| `Island` (la isla de tres capas con Nubi, las piezas y la mirada), `IslaPieceImage`, `islaPiezaDrawable`, `islaPiezaChica`, `rememberIslaLayout`, `drawNubiShadow`, `drawHalo` | `ui/components/Island.kt` (archivo entero) | 181 | Solo la dibujaban Tú y el cierre del Primer vuelo; ya no están |
| `PieceFx` y `rememberPieceFx` (la revelación de la pieza nueva al abrir Tú) | `ui/components/PieceFx.kt` (entero) | 80 | Solo Tú |
| `IslaLayout`, `IslaPoint`, `IslaPlace`, `IslaShadow`, `IslaDeco`, `IslaHalo`, `IslaLitLamp` (las coordenadas de `isla.json`) | `data/IslaLayout.kt` (entero; `IslaLugar` se queda en su propio archivo, ver abajo) | 126 | Solo `Island` |
| `IslaPiezas`, `IslaPieza`, `IslaCapa`, `PiezasPlan` (las diez piezas derivadas de las medallas) | `data/IslaPiezas.kt` (entero) | 66 | Solo Tú y la celebración |
| `IslaPiezasStore` y `PrefsIslaPiezasStore` (qué piezas ya se mostraron, preferencia `isla_piezas`) | `data/IslaPiezasSeen.kt` (entero) | 29 | Solo `PieceFx` y la siembra del repositorio |
| `NewPieceSound` (el `New` de la paleta Madera) | `ui/components/ResultSound.kt` | 10 | Solo la línea «Y en tu isla aparece: …» |
| La línea «Y en tu isla aparece: …» con la pieza chica, su sonido y el parámetro `pieceSound` | `ui/components/AchievementOverlay.kt` | ≈ 35 | La celebración queda igual para todas las medallas |
| `debugIslaPieces`, `debugSetIslaPieces` y la siembra de `islaPiezasSeen` | `viewmodel/NeuroVidaViewModel.kt`, `data/NeuroVidaRepository.kt` | ≈ 20 | Gancho de los botones de depuración y siembra del almacén |
| Los 4 botones «[Debug] Isla: …» | `ui/screens/DebugTools.kt` | 12 | Probaban la isla con 0, 3 y 10 piezas |
| `StatText` (las «tres cifras sin unidad» de Tú) | `ui/screens/ProfileScreen.kt` | 13 | Los cuatro cuadros las reemplazan |
| `IslaLugar.withArticle` («el Observatorio», «la Antena»…) | `data/IslaLugar.kt` | 3 | Quedó sin ninguna llamada al borrar `IslaJsonTest` |
| 5 `Toast.makeText` de Android en Ajustes («Recordatorio diario activado», «Permiso de notificaciones denegado», «Recordatorio desactivado», «Recordatorio fijado a las …», «Aviso de prueba enviado») | `ui/screens/SettingsScreen.kt` | 5 | Ya no hay Toast: el interruptor y las horas muestran el estado; lo que se explica es lo que no sale bien |

## Recursos de `app/src/main/res` (26 archivos, 329 KB)

- `raw/isla.json` (5 KB) y `raw/madera_nuevo.wav` (75 KB).
- `drawable-nodpi/isla_base.webp`, `isla_edificios.webp`, `isla_nubi_hola.webp`, `isla_nubi_descansa.webp` y las diez capas `isla_deco_{c1,c2,c3,c4,v1,v2,v3,vo1,vo2,vo3}.webp` (14 archivos).
- `drawable-nodpi/kit_pz_deco_{c1,c2,c3,c4,v1,v2,v3,vo1,vo2,vo3}.webp`, las diez piezas chicas de la celebración (10 archivos).

## Pruebas

Se borraron `TuPiezasTest`, `IslaPiezasTest`, `IslaPiezasImagesTest`, `PieceFxTest` e `IslaJsonTest` (cada una probaba solo lo de arriba); de `NubiAssetsTest` salió la prueba de «las diez piezas de la isla» (y la pareja `isla_nubi_*` de la de los ojos cerrados) y de `AppActualScreenshotTest` la captura de la isla con 0 y 10 piezas. Se ajustaron `CosturasDeAntesTest` (el cierre del Primer vuelo ahora muestra a Nubi, grande y centrada, sin la palabra «isla»), `AchievementOverlayScreenshotTest` (la celebración ya no habla de la isla), `PulidoFinalTextosTest` y `AjustesDepuradosTest`.

## Lo que NO se borró, y por qué

- **`IslaLugar`** (ahora en `data/IslaLugar.kt`): los nombres «Observatorio», «Antena», «Hangar» y «Biblioteca» de las áreas los siguen usando Hoy (`HoyStop.place`), `PlaceSheet` y «Todos los juegos». Es del mapa que se fue de Hoy en la Tarea 165, pero sigue vivo: si Ricardo quiere sacar esos nombres, es otra limpieza.
- **`tools/isla/`** y **`docs/diseno-isla.md`**: el pipeline de Blender de la isla y su historia viven fuera de la app y se conservan (`diseno-isla.md` lleva una nota de que la isla salió de la app).
- **La preferencia `isla_piezas`** (pasajera, ya sin ningún código que la lea ni la escriba) y su entrada en `BackupRulesTest`: las reglas de respaldo no se tocan; un teléfono que conserve el archivo queda con un archivo sin uso, inofensivo.
- Lo que el inventario de la Tarea 182 dejó fuera (K1.17, K2.1 a K2.3, K2.6, K3.2, `Translations`…) sigue igual.

**Qué entró y qué no, en una línea:** entró todo lo que dejó de usarse al sacar la isla (código, 26 recursos, 5 pruebas y 5 `Toast`); no entró `IslaLugar` (sigue vivo), `tools/isla/`, la preferencia `isla_piezas` ni lo que ya era «dudoso» en la Tarea 182.
