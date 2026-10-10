# Aterrizaje Lunar (`aterrizaje`) — ficha técnica

> **RENOVADO el 10-oct (Tarea 70): la ficha vigente es [`docs/diseno-aterrizaje.md`](../diseno-aterrizaje.md)** («la misma tarea, más grande, con más vida y un final con sentido»: nave grande, regla gruesa, Tierra y cordilleras que se mueven, cúpulas de una base lunar, sonido «Madera cálida» y un final con «X % de la regla» (distancia promedio al lugar justo), tu avance solo contigo y un truco; sin el gráfico «Tu línea»). Las reglas, los 12 niveles y la medida «tu estimación» NO cambiaron. Lo de abajo queda como historia: lo que dice del dibujo «Tu línea» y de la nave, la regla y el cielo de entonces ya no vale.

> Ficha técnica movida TAL CUAL desde `CLAUDE.md` el 2-oct (CLAUDE.md quedó como índice). Lo que manda es el código; esta ficha explica cómo y por qué.

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
- Medida propia: **tu estimación** ("a X% del blanco": distancia media en % del largo de la regla; NO se llama
  "precisión numérica": en adultos la tarea con regla acotada se resuelve como juicio de proporción con puntos de
  referencia y se apoya en habilidades visoespaciales, Barth y Paladino 2011; Sullivan et al. 2011; Simms et al. 2016)
  y **tu línea**: `numline_true / numline_given` (0..1 por aterrizaje) → `GamePlayResult.numlineTrue / numlineGiven` →
  `GameResultScreen` dibuja la regla con cada blanco y dónde se posó, y `data/NumberLine.reading` (lógica pura con
  pruebas) nombra el TRAMO de la regla (inicio / centro / final) donde más se aleja del blanco, con distancia SIN signo
  (el error con signo cerca de los extremos sale sesgado por construcción), y da un truco de puntos de referencia.
  Revisión del 27-sep por pedido de Ricardo ("¿de qué sirve saber que pongo los grandes a la izquierda?").
- Arte: `LandingSprites` (módulo lunar, bandera), `GameWorld.LunarRange`. Vista previa:
  `python3 tools/art-preview/aterrizaje.py <raw>` → `docs/previews/aterrizaje.png`. Probado por Ricardo (27-sep): ok.


## Tutorial guiado, «Cómo se juega» y versión corta del inicio (3-oct, tarea 21a)

- **Tarjeta de Nubi**: «Aterrizaje Lunar» · «Aterriza en el número que te piden. La regla solo marca sus dos extremos.» · «Probar una ronda» / «Saltar tutorial».
- **Ronda guiada** (`LandingContract.GuidedTrial`: regla de 0 a 10 con la marca del medio, número de 2 a 8 que no sea 5 ni el anterior; dos aterrizajes con una franja sol
  sobre el lugar justo, ancha ±12 % y luego ±6 %, `GuidedZones`; entre `// <guided>` y `// </guided>`). Avisos de Nubi: «La regla va de 0 (izquierda) a 10 (derecha).
  Aterriza en el N» · «Otra vez: aterriza en el N, dentro de la zona amarilla» · «Ahora la zona es más chica. Aterriza en el N» · «¡Bien! Ese es el N» · error sin culpa:
  «Casi: la zona amarilla es el lugar justo del N. Probemos otra vez» · «¡Así se juega! Ahora sin ayuda». No cuenta para nada ni se guarda.
- **«Cómo se juega» desde la pausa**: descarta el aterrizaje en curso, tarjeta + ronda guiada, y vuelve a la partida con el reloj del Reto corrido lo que duró.
- **Versión corta**: `LandingContract.AssessmentTrials` = **8 aterrizajes** (≈ 60 s, sin reloj, bajada de Reto/estándar, no la lenta de Precisión). Arranque suave
  (`CreateEngine`: nivel 1 en mayores, 3 en el resto; paso 0,3). **Primer dato** (app, `data/FirstData.kt`): «En promedio, aterrizaste a un X % de distancia del lugar justo.» (de `numline_error_pct`, redondeado, sin decimales).
- Pruebas: `AterrizajeTutorialTests` (aterrizaje guiado fácil y sin repetir, zonas decrecientes, 8 fijos, arranque suave) y `GuidedTutorialTests`.
