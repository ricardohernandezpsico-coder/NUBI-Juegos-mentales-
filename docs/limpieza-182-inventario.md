# Tarea 182: inventario de lo que ya no se usa (fase A, SOLO LECTURA)

27-oct-2026. No se tocó ni se borró nada. Cómo se buscó (todo reproducible; los scripts están en el scratchpad de la sesión y la lógica es esta):

- **Kotlin, nivel 1 (nombre suelto):** se toman todas las declaraciones (`fun`, `class`, `object`, `val`, `var`) de `app/src/main/java` y se cuenta cuántas veces aparece su NOMBRE como palabra entera en TODO `app/src/main` (código sin comentarios, `res/**/*.xml`, `AndroidManifest.xml`). Una coincidencia = la definición = 0 usos.
- **Kotlin, nivel 2 (alcanzabilidad):** cada declaración de primer nivel es un nodo; se parte de las raíces (`MainActivity`, la `Application`, todo `bridge/` —lo llama Unity por JNI—, `data/local/` —Room—, `diag/`, `notification/`, `Features`, lo que cita el manifest o un XML, `@Preview`) y se sigue quién nombra a quién. Lo que no se alcanza es código muerto, aunque se nombre a sí mismo o a otro muerto. Después se repite para los miembros de clases y objetos sobre lo que quedó vivo.
- **Usos a mano** cuando el nombre es común o hay un caso especial (se anota cuál).
- **Recursos de Android:** Android Lint `UnusedResources` (`./gradlew :app:lintDebug`) Y un barrido propio (`R.<tipo>.<nombre>` en el código vivo y `@<tipo>/<nombre>` en XML), porque Lint NO ve el recurso que solo nombra código muerto.
- **Pruebas:** «solo pruebas» = el nombre aparece en `app/src/test` pero en `main` solo en su definición. Según la regla de la tarea esas NO se proponen (salvo que la prueba ya no tenga sentido: se marca en la columna de riesgo).

**Tamaño del APK ANTES** (para la fase B): `app/build/outputs/apk/debug/app-debug.apk` = **176 869 821 bytes (168,7 MiB)** (build del 27-oct, T181). Casi todo es Unity. Desglose por dentro (comprimido / sin comprimir): `lib/arm64-v8a` 51,4 / 51,4 MB (`libil2cpp.so` 34,4 y `libunity.so` 13,1); `assets/bin/Data` (los datos de Unity, donde entra todo `Resources/`) **22,9 / 106,4 MB**; `classes*.dex` (Kotlin, SIN minimizar en debug) ≈ 19 / 61 MB; `res/drawable-nodpi` 1,3 MB; `res/font` 0,3 MB; `resources.arsc` 0,7 MB. **Qué esperar:** borrar ≈ 1 000 líneas de Kotlin y ≈ 120 KB de imágenes de la app mueve el APK de DEBUG por decenas de KB (el DEX de debug no se minimiza y las imágenes van sin comprimir); lo que de verdad pesa y podría bajar está en `assets/bin/Data` (sección U).

Riesgo: **bajo** = 0 usos en ninguna parte (ni pruebas) y no es JNI, JSON, Room, XML ni nombrado con texto; **medio** = 0 usos en `main` pero lo toca una prueba o una firma pública y hay que ajustarla; **dudoso** = parece sin uso pero es andamio a propósito, flag, idioma u otra intención: NO se borra sin decisión.

---

## K1. Kotlin: código muerto (0 usos en `main`, 0 en pruebas)

| # | Qué es | Archivo | Prueba de que no se usa | Tamaño | Riesgo |
|---|---|---|---|---|---|
| K1.1 | `CreamSparkline` (mini-gráfico de la marca que se fue de la ficha en la T171) | `ui/screens/GameSheet.kt:685` | `rg -w CreamSparkline app/src` → 1 (su definición); en pruebas 0 | 16 líneas | bajo |
| K1.2 | `TileClay`, `ModeRowOn` (colores privados) | `ui/screens/GameSheet.kt:129,134` | `rg -w TileClay app/src` → 1; `rg -w ModeRowOn app/src` → 1 | 2 líneas | bajo |
| K1.3 | `LeagueHero`, `GameLevelsList`, `SpaceToggle` (secciones de la pestaña «Espacio» de antes) | `ui/components/SpaceSections.kt:55,95,146` | `rg -w "LeagueHero\|GameLevelsList\|SpaceToggle" app/src` → solo las 3 definiciones; alcanzabilidad: ninguna raíz | **98 de 120 líneas** del archivo (quedan 22: `SpaceSections.kt` entero está muerto salvo comentarios) | bajo |
| K1.4 | `BuildingOnPedestal`, `BuildingIcon`, `PedestalImage`, `BubbleTail`, `buildingRes`, `BUILDINGS`, `BUILDING_ABOVE_DP`, `PEDESTAL_FACE_Y_DP`, `EMBLEM_GAME_IDS` (los edificios de la isla como mapa; el ícono se fue en la T165) | `ui/components/AvanceKit.kt:69,173-253` | cada nombre → 1 coincidencia (definición) salvo `BUILDINGS`/`buildingRes`/`PEDESTAL…`, que solo se nombran entre sí; `BuildingIcon` y `BuildingOnPedestal` solo aparecen como `import` sin uso en `GameSheet.kt` | 55 líneas | bajo |
| K1.4b | **Los 7 recursos que SOLO nombra K1.4**: `kit_edif_lenguaje.webp` 19,5 KB, `kit_edif_atencion.webp` 17,0 KB, `kit_edif_razonamiento.webp` 16,2 KB, `kit_edif_memoria.webp` 14,3 KB, `kit_pedestal.webp` 11,8 KB, `kit_cola_izq.webp` 1,4 KB, `kit_cola_der.webp` 1,3 KB | `res/drawable-nodpi/` | barrido propio: `R.drawable.<nombre>` solo en bloques muertos; Lint no los marca (los ve «usados») | **79 KB**, 7 archivos | bajo (van con K1.4; revisar `kit_avance.json` por si cita alguna pieza) |
| K1.5 | `FirstFlightScreen`: `RowFill`, `RowSelectedFill`, `TrackColor` (colores privados), `CheckMark` (composable privado) | `ui/screens/FirstFlightScreen.kt:108-110,357` | `rg -w CheckMark app/src` → 1; los 3 colores → 1 cada uno | 19 líneas | bajo |
| K1.6 | `FredokaFamily` (familia de letra suelta; la app usa `AppFamily`/`TitleFamily`) | `ui/theme/Type.kt:25` | `rg -w FredokaFamily app/src` → 1 | 16 líneas | bajo (los `.ttf` de `res/font` siguen en uso por `AppFamily`: no se tocan) |
| K1.7 | `DomainLegend`, `domainScores` (leyenda de áreas de antes) | `ui/components/ProgressInsights.kt:31,48` | `rg -w "DomainLegend\|domainScores" app/src` → 2 coincidencias, ambas definiciones | 28 de 39 líneas | bajo |
| K1.8 | `LockTabSwipe` | `ui/components/TabSwipeLock.kt:17` | `rg -w LockTabSwipe app/src` → 1 definición + 1 `import` sin uso en `GameSheet.kt` | 9 de 12 líneas | bajo |
| K1.9 | `NubiBubble`, `BubbleLabel` (globo de Nubi; la T165 quitó los globos de Hoy) | `ui/components/NubiHome.kt:149,215` | `rg -w NubiBubble app/src` → definición + 1 `import` sin uso (`GameSheet.kt`); pruebas 0 | 16 líneas | bajo |
| K1.10 | `domainTextColor` | `ui/components/MiniPlanet.kt:34` | `rg -w domainTextColor app/src` → 1 | 3 líneas | bajo |
| K1.11 | `pipCount` | `ui/components/LeagueShield.kt:135` | `rg -w pipCount app/src` → 1 | 2 líneas | bajo (el resto del archivo es de ligas: bandera apagada, NO se toca) |
| K1.12 | `rememberKitConfirm` y `KitHaptics.confirm` | `ui/components/KitHaptics.kt:29,42` | `rg -w "rememberKitConfirm\|confirm" app/src/main -g KitHaptics.kt`; `rg -w rememberKitConfirm app/src` → 1; `KitHaptics.confirm` 0 llamadas | 7 líneas + 1 miembro | bajo |
| K1.13 | `ShadowInk` | `ui/components/Island.kt:49` | `rg -w ShadowInk app/src` → 1 | 3 líneas | bajo |
| K1.14 | `diamond`, `plus`, `chevron`, `letter` (trazos privados de íconos) | `ui/components/GameIcon.kt:400,404,459,467` | `rg -w "diamond\|chevron" app/src/main -g GameIcon.kt` solo definiciones; `plus`/`letter`: 0 llamadas con ese nombre en `GameIcon.kt` | 25 líneas | bajo |
| K1.15 | Miembros: `Aterrizaje.DOME_EVERY` (`data/Aterrizaje.kt:13`), `FlightStep.gameStep` (`data/FirstFlight.kt:54`), `FlightCopy.SKIPPED_LINE` (`:366`), `IslaLayout.lampIndex` (`data/IslaLayout.kt:72`), `Reading.MIN_TYPE_HITS` (`data/Reading.kt:18`), `RetoChoice.help` (`data/RetoChoice.kt:91`), `TimeTrialDay.newMarkDescription` (`data/TimeTrialDay.kt:161`), `Vocabulary.FILTER_LABELS` (`data/Vocabulary.kt:23`), `NubiTokens.onSky` (`ui/theme/NubiTokens.kt:31`), `Clay.Border` (`ui/theme/Clay.kt:65`), `TuScreen.prog` (`ui/screens/ProfileScreen.kt:135`), `ClayTextField.collectIsFocusedAsState` (`ui/components/ClayControls.kt:108`) | varios | nivel 1 y 2: cada nombre → 1 coincidencia; pruebas 0 | ~1 a 6 líneas cada uno (≈ 35 líneas en total) | bajo |
| K1.16 | Funciones sin llamadas: `NeuroVidaRepository.calculateStreak` (`data/NeuroVidaRepository.kt:1039`, 1 línea: la racha que perdona se calcula por otro camino) y `NeuroVidaViewModel.finishActiveGame` (`viewmodel/NeuroVidaViewModel.kt:700`, ≈ 20 líneas: arma una `GamePlayResult` a mano) | `data/NeuroVidaRepository.kt`, `viewmodel/NeuroVidaViewModel.kt` | `rg -w calculateStreak app/src` → 1; `rg -w finishActiveGame app/src` → 1; pruebas 0 | ≈ 22 líneas | **medio**: archivos compartidos de 1 000+ líneas (el borrado va solo, pero hay que compilar y correr `GameFlowTest`) |
| K1.17 | `NeuroVidaRepository.domainMastery` (`:132`), `wordCollection` (`:164`) y `unclearSentences` (`:361`): flujos que NADIE lee | `data/NeuroVidaRepository.kt` | cada nombre → 1 coincidencia en `main`, 0 en pruebas | ≈ 25 líneas | **dudoso**: cada uno cuelga de persistencia que se CONSERVA (el DAO de maestría y las preferencias `word_collection` y `unclear_sentences`, que van en el respaldo): se podría quitar solo el flujo, pero la regla de la tarea dice que Room, preferencias y respaldo no se tocan; lo dejo a tu decisión |

## K2. Kotlin: SOLO usado por pruebas (no se propone borrar; en la columna va si la prueba aún tiene sentido)

| # | Qué es | Archivo | Prueba | Tamaño | ¿La prueba tiene sentido? |
|---|---|---|---|---|---|
| K2.1 | `Library` (`Library.kt` entero, 84 líneas) con `LibraryOrder` (35) y `LibraryGame` | `data/Library.kt` | `rg -w "Library\|LibraryOrder\|LibraryGame" app/src/main` → solo `Library.kt` + 1 `import` sin uso; la pestaña Juegos se fue en la T158 | 84 líneas; pruebas: `LibraryTest` (42), `LibraryOrderTest` | **dudoso**: probar código que ya no corre; borrarlo arrastra 2 pruebas |
| K2.2 | `Planet` / `PlanetState` / `PlanetZone` / `PlanetPlay` (los planetas de Hoy anteriores a la T165) | `data/Planet.kt` | `rg -w "Planet\|PlanetState\|PlanetZone" app/src/main` → solo `Planet.kt` y `Library.kt` (muerto) | 92 líneas; `PlanetTest` (58) | **dudoso**, igual que K2.1 (K2.1 y K2.2 se borran juntos o no) |
| K2.3 | `DomainChip` | `ui/components/CommonUi.kt` (archivo entero) | `rg -w DomainChip app/src/main` → 1 (la definición); en pruebas solo `GreetingScreenshotTest` (una captura de ese chip) | 31 líneas | dudoso: la captura prueba un chip que la app ya no muestra; borrarlo arrastra esa prueba |
| K2.4 | `WeekLamp` y `WeekLampCream` (los discos de la semana de antes de las farolas) | `ui/components/WeekLamps.kt:34,72` | `rg -w "WeekLamp\|WeekLampCream" app/src/main` → solo las definiciones; en pruebas SOLO como texto en `DesignMdTest` (`!hoyWeek.contains("WeekLamp(on")`: pasa igual si no existe) | 23 líneas | **bajo** (en la práctica es K1: 0 usos reales) |
| K2.5 | `COVER_GAME_IDS` | `ui/components/GameCover.kt:38` | main 1; pruebas 2 (`PortadasTest`) | 3 líneas | **tiene sentido** (la prueba vigila las 17 portadas): NO se toca |
| K2.6 | `PlaceMotion.sample` (las farolas de la ruta lo usaban: T168), `LOOK_HOLD_MS`, `GLOW_MS` | `ui/components/PlaceMotion.kt` | main 1 c/u; pruebas 9, 1, 1 | ≈ 20 líneas | dudoso (ver D1: es parte de la «mirada» de la isla) |
| K2.7 | `RetoChoice.timedLabel`, `UNTIMED_LABEL`, `playLabel`, `summaryLabel` | `data/RetoChoice.kt:56,58,78,85` | main 1 c/u; pruebas 3, 1, 11, 8 | ≈ 25 líneas | **tiene sentido** (`RetoChoiceTest` vigila lo que dice «Jugar»): NO se toca |
| K2.8 | Otros miembros solo de pruebas: `Acoplamiento.SLOTS`, `GamePoints.toNextLine`, `FirstFlight.minutesLeft/minutesLeftText`, `IslaLitLamp.discRadius/haloRadius/footDy`, `IslaLugar.withArticle`, `KitAvancePiece.footX/isStrip`, `Skill.expectedHits`, `StarMeasures.gameNames`, `Streak.longestStrict`, `Suggestion.reasonForArea`, `TimeTrialStats.DETAIL`, `Trail.unlockedLine`, `Vocabulary.filterHot`, `NavIcon.reachDp`, `NubiMotion.SHEET_MS`, `GameCardData.routeStop`, `NubiTokens.Brand/Success/Neutral`, `NeuroVidaViewModel.setColorVision` | varios | main 1 c/u; pruebas 1 a 9 | ≈ 60 líneas | la mayoría tiene sentido (contratos documentados); NO se proponen |

## K3. Kotlin: campos que se escriben y nadie lee (escritura sin lectura)

| # | Qué es | Archivo | Prueba | Tamaño | Riesgo |
|---|---|---|---|---|---|
| K3.1 | Los 10 campos de `GameCardData` `measureTitle`, `measureValue`, `measureTag`, `measureSeries`, `lowerIsBetter`, `playsLast14`, `lastPlayed`, `playedToday`, `routeStop` y `routeDone` NUNCA se leen (la ficha usa solo `game`, `title` y `points`): solo los llena `cardData()` | `ui/screens/GameSheet.kt:143-163,194-260` | `rg "\.(measureTitle\|measureValue\|measureTag\|measureSeries\|playsLast14\|routeDone)\b" app/src` → 0 lecturas en `main` y en pruebas; los otros cuatro (`.lowerIsBetter`, `.playedToday`, `.routeStop`, `.lastPlayed`) solo aparecen leídos en OTRAS clases homónimas (`MeasureDef`, `PlanetZone`, `LibraryOrder.Entry`, `ReminderInput`); `playsLast14` se PASA como parámetro en ≈ 20 llamadas de pruebas, nunca se lee | ≈ 45 líneas (campos + el cálculo de `cardData` que los llena: `StarMeasures.discover/zoneText/comparable/defForGame` solo se llaman ahí desde la ficha) | **medio**: `cardData(...)` tiene la firma que usan ≈ 20 llamadas de pruebas (hay que ajustarlas) |
| K3.2 | `StarMeasures.discover` | `data/StarMeasures.kt` | su ÚNICO uso en `main` es el de K3.1 (alimenta `measureTag`, que nadie lee); lo prueban `StarMeasuresTest`, `EngranajesTest` y 3 pruebas `Retired*` | ≈ 30 líneas + sus clases (`Discovery`, `DiscoveryKind`) | **dudoso**: queda «solo pruebas» si se borra K3.1; Opus decide si la lógica se conserva |

## D1. La «mirada» de la isla (lo que pediste confirmar)

Confirmado: **no se usa en ninguna pantalla**. `Island(lookKey, lookAmount, lookMoves)` tiene los tres con valor por defecto y sus DOS llamadores (`FirstFlightScreen.kt:629` y `ProfileScreen.kt:151`) no los pasan, así que `lookKey` es siempre `null` y la rama `miraBox != null` nunca corre. Lo que cuelga de ahí:

| Qué es | Archivo | Prueba | Tamaño | Riesgo |
|---|---|---|---|---|
| Parámetros `lookKey`/`lookAmount`/`lookMoves`, la rama `miraBox` y `miraDrawable()` | `ui/components/Island.kt:75-77,87-88,118,133,140-148,205-212` | `rg "lookKey\|lookAmount\|lookMoves" app/src/main` → solo la firma y su cuerpo; 0 llamadores los pasan | ≈ 35 líneas | medio (composable público; `IslaMotion`/pruebas de captura de la isla) |
| `PlaceMotion.look`, `lookFade`, `LOOKS`, `Look`, `LOOK_FADE_MS`, `LOOK_SHIFT_MS`, `LOOK_HOLD_MS`, `GLOW_MS` | `ui/components/PlaceMotion.kt` | solo los nombra la rama muerta de `Island.kt` y `PlaceMotionTest` | ≈ 25 líneas (quedan `TAB_FADE_MS` y `REDUCED_FADE_MS`, que sí se usan en la barra) | medio (`PlaceMotionTest`, `NubiMotionTest`) |
| `IslaLayout.miraBoxes` y su lectura del JSON (`mira_<lugar>`) | `data/IslaLayout.kt:59,95-119` y `res/raw/isla.json` | `rg miraBoxes app/src/main` → solo `IslaLayout.kt` e `Island.kt` (rama muerta) | ≈ 15 líneas + las entradas `mira_*` del JSON | medio |
| `isla_nubi_mira_antena.webp` 11,3 KB, `_biblioteca` 10,7 KB, `_hangar` 11,1 KB, `_observatorio` 10,9 KB | `res/drawable-nodpi/` | Lint no los marca (los nombra `miraDrawable`); muertos en tiempo de ejecución | **44 KB**, 4 archivos | medio |

## R1. Recursos de Android

- **Lint `UnusedResources`** (`app/build/reports/lint-results-debug.txt`): 7 colores de la plantilla de AI Studio en `res/values/colors.xml` (`purple_200`, `purple_500`, `purple_700`, `teal_200`, `teal_700`, `black`, `white`), 7 líneas. Riesgo bajo; `rg "R.color.purple_200\|@color/purple_200" app/src` → 0.
- **Imágenes, raw y fuentes:** 182 archivos, 2 361 KB en total; con el barrido propio TODOS se nombran en el código; los únicos que solo nombra código muerto son los de K1.4b (79 KB) y los de D1 (44 KB). No hay ningún `raw/*.wav`, `.json`, `.ttf` ni mipmap sin uso.
- **Otros hallazgos de Lint, fuera de la tarea pero que conviene que veas:** `NewApi` = **14 errores** (`java.time.ZoneId/Instant` desde `minSdk 24` sin desugaring en `data/CountdownSteps.kt` y otros: en Android 7.x podrían cerrar la app); 38 `UseKtx` y 13 `GradleDependency` (solo avisos). No los toco.

## I1. Imports sin uso

**213 imports sin uso en 27 archivos** (el barrido ignora `getValue`/`setValue`, que `by` necesita). Los peores: `GameSheet.kt` 37, `Clay.kt` 21, `LeagueShield.kt` 18, `AvanceSections.kt` 18, `NubiHome.kt` 17, `GameResultScreen.kt` 16. Cero efecto visible; riesgo **bajo**.

## T1. Traducciones preparadas (dudoso: NO borrar sin que Ricardo decida)

`Translations` (`ui/i18n/AppStrings.kt`) tiene 24 campos y **21 no se leen en ningún lado** (`tabToday`, `settingsTitle`, `gamesLibraryTitle`, `greetingFormat`…): son ≈ 126 líneas (21 campos × 6: la declaración y los 5 idiomas). Y `getDomainName` (28 líneas, 0 usos: solo un `import` sin uso). Es el andamio de idiomas que `AppLanguage.translated` deja apagado a propósito («Ricardo decide cuándo se traduce»). **Riesgo: dudoso**; lo anoto para que lo decidas tú.

## C1. Candidatos que ya traías: veredicto

| Candidato | Veredicto |
|---|---|
| `CreamSparkline` | **Muerto** (K1.1). |
| `StarMeasures.discover` | Su único uso en `main` alimenta un campo que nadie lee (K3.1/K3.2): **muerto en la práctica, vivo en pruebas**. |
| La «mirada» de la isla | **Muerta** (D1), con 4 imágenes (44 KB). |
| `DailyChallenge`, `ChallengeButton`, `ChallengeDoneNote` | **Ya no existen**: `rg -w "DailyChallenge\|ChallengeButton\|ChallengeDoneNote" app/src/main` → solo una mención en un comentario de `TimeTrialDay.kt`; en pruebas solo textos que vigilan que el archivo viejo `desafio_hoy` quede sin uso (`BackupRulesTest`, `HoyTarjetaTest`). Nada que borrar. |
| Las «tres formas» del botón de Hoy (T180) | **Ya no existen**: `rg "TimeTrialRow\|timeTrialRow" app/src/main` → 0. Nada que borrar. |
| El cometa de la T167b | **Se usa**: `Glyph.COMET` lo usa el botón «Desafío» de la ficha (`GameSheet.kt`); `kit_i_cometa` y su pieza `cometa` de `kit.json` están vivos. NO se toca. |
| El reloj viejo del kit | **Se usa**: `Glyph.CLOCK` (`kit_i_reloj`) lo usan «¿Con reloj?» de los 12 juegos y el botón «Otra vez» del final del Contrarreloj; `Glyph.CLOCK_OFF` (`kit_i_sin_reloj`), «Sin reloj». NO se toca. |

---

## U. Unity

Inventario completo (98 filas de código, `Resources/` y descartes con su motivo): [`docs/limpieza-182-inventario-unity.md`](limpieza-182-inventario-unity.md). Lo armó un agente de solo lectura con un analizador propio de C# (338 `.cs`, 14 000 declaraciones) y yo contrasté 10 candidatos por muestreo, incluyendo `tools/`. Resumen:

- **Código (sección A del otro archivo):** 98 filas, ≈ 696 líneas: 89 de riesgo bajo (≈ 421 líneas) y 9 de riesgo medio (≈ 275 líneas: tocan pruebas o herramientas del Editor). Los más grandes: `SymbolSprite` con 12 de sus 15 formas sin pedido (≈ 214 líneas, medio: las enumeran `NeutralSymbolsTests` y `SymbolPreviewExporter`, y `Bitacora` usa `ShapeKind.Comet`), la clase `ArrowSprite` (73 líneas, bajo), 7 presets de `GameWorld` (48 líneas, bajo), `ResultMark` (34 líneas, bajo: lo reemplazó `GameHud.Announce`) y `NeonSignSprites` (28 líneas; **corrección mía: NO es 0 usos**, lo usan `tools/art-preview/Program.cs:200` y su `.csproj` → riesgo medio). Restos de T178/T179: `EngranajesGameController.WarnText`, `StroopContract.Guided*`, `ToastPlacement.ScreenWorldArea` y el campo `GameHud._annMainTone` (mío, de la T179). `ErrorPauseBase`, `Toast.cs` y `AnswerMarkSprite.Notice` ya no existen.
- **`Assets/Resources/` (B):** 13 246 KB en 51 archivos (19 atlas = 11 482 KB, 87 %; ≈ 15 952 KB deflatados en el APK). **Sin carga ≈ 171 KB (1,3 %)**, y TODO son sprites DENTRO de atlas (ningún archivo entero está huérfano): 2 cintas `losa_md_cinta_*` de Stroop (57 KB, medio: quitarlas exige regenerar el atlas con `tools/juegos/empaqueta.py`), `aviso_crema` de Engranajes (28 KB, medio, resto de la T179), 4 losas `_p` de «comun» (72 KB, **dudoso**: se piden con nombre armado), 3 textos OFL (13 KB, **se conservan**: la licencia pide que viajen con la fuente). Aparte, `cosecha_es.json` (732 KB) solo lo carga el código de Cosecha, que se conserva a propósito: NO se propone; es el ahorro más grande si algún día Cosecha sale de Unity.
- **Otros assets (C):** nada sin referencia (escena, manifest de Android y JSON de referencia de pruebas, todos usados).
- **Solo pruebas o Editor (D):** 189 miembros; no se proponen.
- **Dudosos y trampas:** ramas `#if UNITY_IOS` de `NativeBridge` (≈ 7 líneas); 10 módulos de Unity en `Packages/manifest.json` sin uso aparente; `EngranajesSfx.Step1..8` parecen muertos pero se usan por rango y cast; `IntrusaSprites.Bake` es falso positivo; pruebas de Kotlin leen `GameTitles.cs` y `TimeTrial.cs` POR TEXTO (no tocar); el arte procedural de reserva de cada `*Sprites.cs` es por diseño.
- **Cuidado en la fase B (mío):** `tools/art-preview/ArtPreview.csproj` compila a pelo muchos archivos de Unity; cada borrado en uno de ellos se comprueba también con `dotnet build tools/art-preview` y `dotnet build tools/unity-compile-check`.

---

## Resumen (lo que propongo borrar en la fase B, si das el OK)

| Lado | Qué | Líneas | Riesgo |
|---|---|---|---|
| App | K1.1 a K1.15 + K1.4b (7 imágenes, 79 KB) + K2.4 + I1 (213 imports) | ≈ 550 líneas de código + 213 imports + 7 imágenes | bajo |
| App | K1.16 (`calculateStreak`, `finishActiveGame`) | ≈ 22 | medio |
| App | K3.1/K3.2 (campos de `GameCardData` que nadie lee y su cálculo) | ≈ 45 | medio (firma de `cardData` en ≈ 20 llamadas de pruebas) |
| App | D1 (la «mirada» de la isla y sus 4 imágenes, 44 KB) | ≈ 75 + 4 imágenes | medio |
| Unity | 89 filas de riesgo bajo | ≈ 421 | bajo |
| Unity | las 9 filas medias, `NeonSignSprites` y `SymbolSprite` incluidos | ≈ 275 | medio |

**No se propone, a tu decisión («dudoso»):** K1.17, K2.1/K2.2 (`Library` y `Planet` con sus pruebas), K2.3 (`DomainChip`), T1 (idiomas), las losas `_p` de «comun» y los 3 OFL.

---

## Cierre de la fase B (27-oct-2026): qué entró, qué no y por qué

**Qué entró (app).** Todo lo de riesgo bajo y tres medios que Opus aprobó, ≈ 1 044 líneas borradas y 54 tocadas en 73 archivos (11 de ellos, imágenes):

- **K1.1 a K1.15 y K2.4** (`WeekLamp`/`WeekLampCream`), enteros: `CreamSparkline`, `TileClay`/`ModeRowOn`, las 3 secciones de «Espacio» de `SpaceSections.kt`, los edificios/pedestal/globo de `AvanceKit.kt`, los colores y `CheckMark` de `FirstFlightScreen`, `FredokaFamily`, `DomainLegend`/`domainScores`, `LockTabSwipe`, `NubiBubble`/`BubbleLabel`, `domainTextColor`, `pipCount`, `rememberKitConfirm`/`KitHaptics.confirm`, `ShadowInk`, 4 trazos de `GameIcon`, y los 12 miembros sueltos de K1.15.
- **K1.4b**: las 7 imágenes de edificios, pedestal y colas (79 KB) y sus 128 líneas de `kit_avance.json`.
- **K1.16**: `calculateStreak` y `finishActiveGame` (`GameFlowTest` verde).
- **K3.1**: los 10 campos de `GameCardData` que nadie leía y el cálculo que los llenaba; `cardData(...)` quedó `(g, title, progress, points = null)` y se ajustaron las 22 llamadas de las pruebas. `GameCardData` ahora es `(game, title, points)`.
- **D1**: la «mirada» de la isla (`lookKey`/`lookAmount`/`lookMoves`, `miraBox`, `PlaceMotion.look`/`lookFade`/`LOOKS`/`Look`/`LOOK_FADE_MS`/`LOOK_SHIFT_MS`, `IslaLayout.miraBoxes` y los `mira_*` de `isla.json`) y sus 4 imágenes (44 KB).
- **R1**: los 7 colores de la plantilla de AI Studio (`colors.xml` queda con `nv_night`).
- **I1**: **277 imports sin uso en 32 archivos**, no 213 en 27: el barrido se repitió DESPUÉS de borrar y salieron los que dejaron huérfanos los borrados.
- **Extras que aparecieron al borrar** (no estaban en este inventario; cada uno quedó en 0 usos al caer su único cliente): `OnNight` privado (en `FirstFlightScreen`, `GameSheet` y `SessionSummaryScreen`), y en `GameSheet` también `OnNightDim`, `ModeRow`, `Body` y `Tag` privados (el barrido por nombre no ve un nombre privado repetido en otro archivo); `ON_SKY_LIGHTEN` (solo lo usaba `onSky`, de K1.15); `DAY_MS` y `dayIndex` (solo los usaba el cálculo de K3.1; el comentario de archivo que colgaba de `DAY_MS` se conservó); `IslaNubiBox` (solo la mirada) y `footX`/`footY` de `KitAvancePiece` (solo los usaban los edificios).
- **Pruebas ajustadas**: `PlaceMotionTest` (se fueron las de la mirada), `IslaJsonTest` (la de `mira_*`), `AvanceKitTest` (esperan las piezas sin pedestal, colas ni edificios; dos pruebas renombradas), `DesignMdTest` (lee `HoyWeek` hasta el final del archivo).
- **Prueba obsoleta borrada**: `Tarea159CapturasTest.la_lamina_de_hoy_con_la_linea_de_puntos` tocaba el edificio `isla_observatorio` de Hoy para abrir la lámina de un lugar. Esa lámina y los edificios tocables se fueron con las Tareas 163 y 165, así que solo corría al grabar capturas y fallaba desde entonces (el único fallo del modo grabación). Ahora la clase tiene 6 pruebas y pasa entera al grabar.

**Qué no entró (app), y por qué:**

- **K1.17** (`domainMastery`, `wordCollection`, `unclearSentences`): cuelgan de persistencia que se conserva (DAO de maestría y preferencias del respaldo); la tarea dice que Room, preferencias y respaldo no se tocan.
- **K2.1 y K2.2** (`Library` y `Planet` con sus 3 pruebas) y **K2.3** (`DomainChip` con su captura): son «solo pruebas»; borrarlos arrastra pruebas y lo decide Ricardo.
- **K2.5, K2.7 y K2.8**: la prueba que los usa tiene sentido (contratos documentados).
- **K2.6** (`PlaceMotion.sample`, `LOOK_HOLD_MS`, `GLOW_MS`): Opus lo dejó fuera; siguen en `PlaceMotionTest`.
- **K3.2** (`StarMeasures.discover`): queda «solo pruebas»; Opus decide si la lógica se conserva.
- **T1** (`Translations`, `getDomainName`): andamio de idiomas a propósito.
- **Lint `NewApi` (14 errores) y los 38 `UseKtx`**: fuera de esta tarea (los `NewApi` son la Tarea 183, `minSdk 26`).

**Tamaño del APK (debug), mismo script antes y después.**

| | Antes (T181) | Después (T182) |
|---|---|---|
| Archivo `app-debug.apk` | 176 869 821 bytes (168,7 MiB) | 140 813 657 bytes (134,3 MiB) |
| Entradas del zip | 710 | **699** (las 11 imágenes) |
| Contenido sin comprimir / comprimido | 224 / 96 MB | 223 / 96 MB |
| `res/drawable-nodpi` · `resources.arsc` | 1,3 · 0,7 MB | **1,2 · 0,6 MB** |
| `classes*.dex` · `assets/bin/Data` · `lib/arm64-v8a` | 39,8 + 14,0 + 5,4 + … · 22,9 / 106,4 · 51,4 MB | iguales a la décima de MB |

**La diferencia de 36 MB del ARCHIVO no viene de los borrados.** El APK de depuración tiene huecos sin usar dentro del zip (Gradle lo actualiza en el sitio y no los recupera): el de ahora tiene 37,8 MiB en 61 huecos; la suma de sus entradas es de 96,5 MiB y es lo que hay que mirar. Lo más probable es que la verificación completa, que reexportó Unity y empaquetó de cero, no arrastrara los huecos que traía el APK anterior (no lo comprobé). Lo que sí bajaron los borrados es lo que se esperaba: ≈ 123 KB de imágenes (11 archivos) y unos pocos KB de recursos; el DEX de debug no se minimiza y no se movió a la décima de MB. Para una cifra que valga para Google Play hace falta un build de release (minimizado), que no se hizo.

**Qué no se tocó de Unity** está en el otro archivo, al final.
