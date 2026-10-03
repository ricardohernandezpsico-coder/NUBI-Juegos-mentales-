# Aterrizaje Lunar (`aterrizaje`) — ficha técnica

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
