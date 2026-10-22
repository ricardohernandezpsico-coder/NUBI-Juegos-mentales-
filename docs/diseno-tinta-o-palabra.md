# Tinta o Palabra — modelo «Dos orillas» (aprobado por Ricardo el 3-oct)

Renueva Tinta o Palabra (id interno `stroop`, que SE MANTIENE para no perder avance, marcas ni historial). Área: Atención
y velocidad. Boceto jugable: `docs/previews/tinta-o-palabra-boceto.html` (https://claude.ai/artifact/F3UWUHcSdv2TtKb8zPxWYu),
modelo 3. Ricardo probó los tres y eligió «Dos orillas»: «el dinamismo que se da cuando las palabras se van hacia los
costados es suficiente». Descartó Acuarela (pintar un paisaje) porque no calza con el estilo espacial y tecnológico de
Nubi, y Faroles quedó atrás. Aprobó la paleta nueva por inclusión de personas con daltonismo.

## 1. La idea en una línea

Una palabra de color llega deslizándose desde una de dos orillas: la de la **TINTA** (izquierda, violeta, gotas) o la de
la **PALABRA** (derecha, turquesa, libros). La orilla de la que llega manda: desde la TINTA se toca el color con que está
escrita; desde la PALABRA, lo que dice. Al acertar, la tarjeta sale hacia la orilla contraria.

## 2. Ciencia (PubMed, 3-oct)

- Efecto Stroop: nombrar la tinta es más lento cuando la palabra nombra otro color. Original: Stroop, 1935, *J Exp
  Psychol* 18:643-62, [doi:10.1037/h0054651](https://doi.org/10.1037/h0054651) (no está en PubMed). Revisión de 50 años:
  MacLeod, 1991, *Psychol Bull* 109:163-203, [doi:10.1037/0033-2909.109.2.163](https://doi.org/10.1037/0033-2909.109.2.163)
  (PMID 2034749). La medida clásica es la **interferencia**: tiempo con palabra que choca menos tiempo con palabra que
  coincide.
- Cambiar de regla de un ensayo a otro cuesta tiempo aunque se sepa cuál toca (**costo de cambio**), y una señal clara de la
  regla lo reduce: Monsell, 2003, *Trends Cogn Sci* 7:134-40, [doi:10.1016/S1364-6613(03)00028-7](https://doi.org/10.1016/S1364-6613(03)00028-7)
  (PMID 12639695). Por eso la regla se ve en tres señales a la vez (orilla, ícono y cinta).
- Simulación de daltonismo: Machado, Oliveira y Fernandes, 2009, *IEEE Trans Vis Comput Graph* 15:1291-8,
  [doi:10.1109/TVCG.2009.113](https://doi.org/10.1109/TVCG.2009.113) (PMID 19834201).
- Qué NO decir: que mejora la concentración en la vida diaria o que mide «control ejecutivo» como un test clínico. Sí se
  puede decir: «mide cuánto te frena la palabra y cuánto te cuesta cambiar de regla en esta partida».
- Originalidad: el juego de colores de Lumosity compara dos tarjetas y pide sí/no. Aquí no hay sí/no: la regla viene
  dada por la orilla y se responde con 4 tintas. El efecto Stroop es de dominio público (1935).

## 3. Paleta nueva: 4 tintas para todos

Con la paleta de hoy (ROJO EF4444, AZUL 3B82F6, VERDE 22C55E, AMARILLO FACC15, MORADO A855F7), simulando deuteranopía,
**azul y morado quedan a 0,9 de diferencia** (CIEDE2000; menos de 2 no se distingue) y rojo con verde se confunden. Cerca
de 1 de cada 12 hombres tiene uno de esos daltonismos.

| Tinta | Color | Contraste sobre la tarjeta #141B36 |
|---|---|---|
| ROJO | `#C93C3C` | 3,4:1 (palabra grande y con brillo) |
| AZUL | `#4A86E8` | 4,7:1 |
| AMARILLO | `#F2CC1D` | 10,8:1 |
| BLANCO | `#F4F4F4` | 15,4:1 |

Diferencia mínima entre pares: típica 29,8; protanopía 30,5; deuteranopía 28,6; tritanopía 22,1. Se comprueba con
`python tools/paleta_daltonismo.py C93C3C 4A86E8 F2CC1D F4F4F4 --fondo 141B36` (regla: ≥ 20 en típica, protanopía y
deuteranopía). Una sola paleta para todos: no hace falta usar `color_vision` en este juego. Cualquier juego futuro que
pida distinguir colores debe pasar el mismo script.

**Botones (decisión del 3-oct, tras la prueba de Ricardo, opción A):** cada botón se rellena ENTERO con el color de su tinta, en arcilla (borde tinta `#1A1240` grueso y sombra dura hacia
abajo, como lo tocable del resto de la app; se hunde al tocarlo), con el nombre centrado en Fredoka seminegrita (≥ 20 dp). Antes llevaban una gotita de color al lado del nombre y «se veían
poco». El color del nombre va por botón, para que siempre se lea: **BLANCO `#FFFFFF` sobre ROJO** (5,0:1) y **tinta `#1A1240` sobre AZUL** (4,9:1), **AMARILLO** (11,2:1) y **BLANCO** (15,9:1).
El nombre escrito sigue en todos (el color nunca va solo). El aro que marca el botón correcto (al fallar y en la ronda guiada) va por FUERA del botón, celeste `#7FD8FF` con borde tinta: se
distingue alrededor de las cuatro tintas, también del botón blanco. Los colores de las orillas (violeta `#7C5CE0`, turquesa `#159A8C`) no son tintas de respuesta y siempre van con ícono y texto.

## 4. Cómo se juega

- **Pantalla:** puntos de avance arriba; cinta «Responde: TINTA/PALABRA» con su ícono; las dos orillas (franjas de luz a
  los costados, la activa encendida y la otra apagada al 35 %, con el nombre vertical); tarjeta central oscura con borde del
  color de su orilla y el ícono en la esquina del lado del que llegó; 4 botones grandes en 2 × 2 abajo.
- **Llegada:** la tarjeta entra deslizándose desde su orilla (≈ 300 ms; no se acepta respuesta hasta que llega). El tiempo
  de respuesta se mide desde que se detiene.
- **Acierto:** tono ascendente con la racha, chispas del color tocado y la tarjeta sale hacia la orilla contraria.
- **Error:** golpe suave, temblor corto de la tarjeta, aro en el botón correcto y Nubi explica sin culpa: «La tinta era
  azul» o «La palabra decía rojo». Pausa de ~1,5 s para leerlo.
- **Primer cambio a PALABRA:** tarjeta «Ahora: PALABRA — toca lo que DICE la palabra, no su color» (como en Rastro). Solo
  la primera vez por partida; después basta con la orilla.
- **Tema espacial:** el fondo es el cielo nocturno de la app. Las orillas son haces de luz, como compuertas de una
  estación. Nada de papel ni acuarela.
- **Movimiento reducido:** la tarjeta aparece en su lugar con un fundido (sin deslizarse ni temblar), sin chispas. Se
  mantienen los tiempos (`Motion.Hold`) y la orilla encendida, que es la señal de la regla.

## 5. Dificultad (DDA común, `docs/DDA-comun.md`)

Cinco niveles con `AdaptiveDifficulty`, igual que hoy (objetivo 0,80; 0,85 en mayores):

| Nivel | Reglas | Palabras que chocan | Llegada |
|---|---|---|---|
| 1 | Solo TINTA | 60 % | 360 ms |
| 2 | Solo TINTA | 75 % | 320 ms |
| 3 | Las dos, por tramos (la regla cambia cada 4-6 palabras) | 75 % | 300 ms |
| 4 | Las dos, al azar en cada palabra (~40 % de cambios) | 80 % | 280 ms |
| 5 | Las dos, al azar (~50 % de cambios) | 85 % | 240 ms |

- El resto (25-40 %) son palabras que coinciden con su tinta: hacen falta para medir la interferencia.
- Modo Precisión: 16 palabras sin reloj. Modo Reto: 60 s, todas las que alcances (`EndlessScore` como hoy).
- Mayores: llegada 1,3 veces más lenta y botones un poco más altos.

## 6. Medidas al final

- Aciertos y ritmo (segundos por palabra).
- **«Cuánto te frenó la palabra»:** promedio de tiempo de los aciertos con palabra que choca menos el de los que coinciden.
  Se muestra con 1 decimal y «casi nada» si da menos de 0,05 s. Hace falta al menos 3 aciertos de cada tipo.
- **«Cambiar de orilla te costó»:** promedio de tiempo de los aciertos tras un cambio de orilla menos el de los que
  repiten orilla. Solo desde el nivel 3, y también con al menos 3 de cada tipo.
- Telemetría: `interference_ms` y `switch_cost_ms` (−1 si no hay datos suficientes).

## 7. Tutorial guiado y versión corta

Con el sistema común (`GuidedTutorial`, `GameControllerBase.BuildTutorial`/`GuidedRound`): 1) Nubi muestra una palabra
desde la orilla TINTA y pide tocar su color; 2) una desde la orilla PALABRA; 3) dos rondas guiadas sin puntaje. El menú de
pausa «Cómo se juega» lo repite. Versión corta (`Assessment`): no hace falta por ahora, porque el juego no está
en el inicio.

## 8. Ficha técnica (programado el 3-oct, tarea 23)

- `Games/Stroop/StroopContract.cs`: reglas puras. `Names`/`Colors` (4 tintas), `Spec(level)` (reglas, % que chocan, llegada), `ArrivalSeconds(level, senior)` (mayores ×1,3), `Sequencer`
  (genera las palabras: 3 de apertura de TINTA, tramos de 4-6 en el nivel 3, cambios al azar de ~40 % y ~50 % en los niveles 4 y 5; marca `Switched` y `Clash`),
  `InterferenceMs`/`SwitchCostMs` (−1 con menos de 3 aciertos de cada tipo; nunca negativas), `Explain` («La tinta era azul» / «La palabra decía rojo»), `GuidedTrials` (la ronda guiada).
  `BaseTimeForLevel` y `RuleFlipChance` se quitaron (el tiempo por palabra ya no existe: manda el reloj del Reto).
- `StroopGameController.cs` (rehecho): cinta «Responde: …», dos orillas (haces de luz con gotas/libros y el nombre en vertical; la activa al 100 %, la otra al 35 %), tarjeta oscura
  #141B36 con borde del color de su orilla y el ícono en la esquina, 4 botones 2 × 2 de arcilla teñidos enteros con su tinta y el nombre centrado (ver §3). La tarjeta llega deslizándose (no se acepta respuesta hasta que
  llega; el tiempo de respuesta se mide desde ahí) y sale hacia la orilla contraria al acertar. Al fallar: aro en el botón correcto, el texto en la píldora y 1,5 s de pausa que NO le gasta
  tiempo al Reto. «Ahora: PALABRA» solo la primera vez por partida (tampoco gasta tiempo). `StroopSprites.cs`: gota, libro y haz de luz (se tiñen con `Image.color`).
- Movimiento reducido (`Motion.Decorative`): sin deslizamiento, temblor, chispas ni golpe de la cinta; fundidos con los mismos tiempos (`Motion.Hold`).
- Tutorial: `GuidedRound` (4 palabras que chocan: TINTA, PALABRA y las mismas dos sin aro; un error repite el paso y explica sin culpa; no suma puntos ni toca el DDA) y «Cómo se juega» en
  la pausa (`HowToReady/Suspend/Resume`; el Reto no pierde tiempo). Durante la práctica los botones suben 270 u para dejar lugar a «Práctica (sin puntos)» y «Saltar tutorial».
  `UnityGameLauncher.TUTORIAL_GAMES` incluye `stroop`; el smoke tiene `TutorialStroop`. Sin versión corta.
- Telemetría: `interference_ms` y `switch_cost_ms` en `StroopSessionMetrics` (lo leen `NativeReceiver` y `GamePlayResult.interferenceMs/switchCostMs`; solo para `stroop`).
  App: `data/DosOrillas.kt` (texto de las medidas y consejo), bloques en `GameResultScreen`, el consejo es el «Truco para la próxima» de su final con sentido (`FinalModels.tinta`; `ResultAdvice.tipOf` separa el «Truco: …»).
  La marca de evolución SÍ se guarda desde la Tarea 77 (10-oct): clave `stroop` en `StarMeasures` (los ms de interferencia; menos es mejor; `NeuroVidaRepository.starPoints` toma `interferenceMs`). Se dice en palabras
  («casi nada», «0,4 segundos más»; `StarMeasures.zoneText` → `DosOrillas.cost`) en Hoy y en la ficha del juego, y alimenta «Tu avance» de su final con sentido.
- Pruebas: `StroopContractTests` (paleta, niveles, generación, medidas, guion), `DosOrillasTest`, `StroopTelemetryTest`; smoke `TutorialStroop` y `Stroop`.


## 9. Plasticina (Tarea 100, 14-oct): el color de la tinta, medido

Ricardo: todos los juegos en plasticina. Es SOLO arte: las reglas, el DDA, los tiempos, las 4 tintas de diseño, la palabra (sigue siendo `Text`) y las medidas no cambian. Técnica y pesos: [`tools/juegos/README.md`](../tools/juegos/README.md); seguimiento de los 17 juegos: [`plasticina-juegos.md`](plasticina-juegos.md). **Fecha del cambio de arte: 14-oct-2026** (marca `estilo 14-oct · plasticina tinta o palabra`; sin `art_version`: si hace falta separar series, se hace por fecha).

**El color de la tinta ES el estímulo**, así que este juego se migró con un cuidado distinto: las losas de plasticina de los 4 botones de respuesta y del fondo de la tarjeta se renderizan con `material_kit(spec=0.05, sheen=0.0)` (sin el brillo normal del kit, que le suma un gris a la cara plana y movería el tono) y con el color de Blender CORREGIDO por lo que la luz cálida del estudio le hace al tono (sube el rojo hasta 13/255 y baja el azul): se renderiza una primera pasada, se mide y se corrige (diseño − diferencia). Resultado, medido sobre el atlas ya empaquetado (`tools/juegos/mide_stroop.py`):

| Medida | Resultado |
|---|---|
| Color medio de la cara de cada botón contra su tinta de diseño | ΔE CIELAB 0,3 a 1,6 (meta ≤ 5): ROJO C83C3B, AZUL 4A87E8, AMARILLO EFCA1D, BLANCO F5F2F0 |
| Fondo de la tarjeta contra #141B36 | ΔE 1,1 (151B35) |
| Separación de las 4 tintas (CIEDE2000, mínimo entre pares; regla ≥ 20) | típica 29,0 (de diseño 29,8); protanopía 29,7 (30,5); deuteranopía 28,3 (28,6); tritanopía 21,0 (22,1) |
| El nombre sobre cada botón | blanco sobre ROJO 5,1:1 (diseño 5,0); tinta sobre AZUL 4,9:1 (4,9), AMARILLO 10,9:1 (11,2) y BLANCO 15,7:1 (15,9) |
| La palabra sobre el fondo de la tarjeta | ROJO 3,4:1, AZUL 4,7:1, AMARILLO 10,8:1, BLANCO 15,3:1: igual que en el diseño |

| De plasticina (atlas `Resources/Plasticina/stroop`, `tools/juegos/stroop.py`) | Se queda procedural |
|---|---|
| los **4 botones de respuesta** (losa con el color EXACTO de la tinta, en una hija que ocupa el 86 % del botón como la ficha de arcilla de siempre; al tocarlos se hunde la losa (la presionada es otra imagen más oscura) y el nombre baja con ella, sin escala) | los haces de luz de las orillas y su línea de borde, el resplandor y la sombra detrás de la tarjeta |
| la **tarjeta**: fondo #141B36 y borde de la orilla de la que llegó (violeta 7C5CE0 / turquesa 159A8C; coral al fallar), y la esquina con el ícono | la palabra (`Text`), el brillo detrás de la palabra |
| la **cinta** «Responde: TINTA / PALABRA» (losa de la regla), la **tarjeta «Ahora: PALABRA»** y el **panel del final** (losa «noche») | el aro que marca el botón correcto al fallar y en la ronda guiada (celeste, por fuera), los puntos de avance, la barra del Reto |
| los íconos de las reglas: la **gota** (TINTA) y el **libro** (PALABRA), en gris claro para teñir con `Image.color` (misma huella: razón 1,05 y 1,01) | las marcas de las orillas siguen siendo gota y libro: ahora de plasticina |

`PlasticSlab` ganó las losas con el color exacto de un juego (`AttachCustom`, `TryStyleCustom`, `SetCustom`, `PlasticPress.InitCustom`): viven en el atlas del juego, con el mismo nine-slice y los mismos márgenes que las del kit de su clase. Sin el atlas, todo cae al arte de siempre (`TileSprites` y `ClayFrame`). Pesa 0,77 MB en el APK. Pruebas: `StroopPlasticTests`.

## «Juice» del 18-oct-2026 (Tarea 130): la moneda con forma en el botón

Como en los demás juegos, la respuesta lleva una marca con forma: al ACERTAR, la moneda ✓ (`marca_ok` del kit, 40 dp) en la esquina de arriba a la derecha del botón tocado; al ERRAR, ✗ (`marca_no`) en el tocado y ✓ en el correcto. Entra con `PopIn` (0,30 s) y se va con el deslizamiento de la tarjeta (fundido de 0,15 s).
Las monedas viven EN EL BOTÓN, no en la tarjeta: no demoran el ensayo siguiente ni tocan `ArrivalSeconds`. **Nunca se tiñe la losa** (la tinta es el estímulo: ΔE ≤ 1,6, §9). Sin sonido nuevo (`SoundRulesTests` sigue verde) y ninguna medida ni tiempo de ensayo cambia. Con «quitar animaciones» cada cosa va a su estado final de una vez, con la misma duración.
