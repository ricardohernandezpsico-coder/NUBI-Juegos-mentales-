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

Los botones llevan la gota de color **y el nombre escrito**. Los colores de las orillas (violeta `#7C5CE0`, turquesa
`#159A8C`) no son tintas de respuesta y siempre van con ícono y texto.

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
