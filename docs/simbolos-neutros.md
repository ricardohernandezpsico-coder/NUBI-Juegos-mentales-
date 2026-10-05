# Símbolos neutros (regla de Ricardo, 4-oct-2026)

**Regla:** en los juegos y los íconos de la app, nada de estrella con puntas, media luna ni cruz (para evitar cualquier lectura religiosa o política y por coherencia con los «luceros» redondos de la app). Un planeta o una luna REDONDOS, los luceros redondos, un
hexágono, una gota o una ola están bien. Lámina para ver el resultado: [`docs/previews/simbolos-neutros.png`](previews/simbolos-neutros.png) (script `tools/art-preview/simbolos_neutros.py`).

## Qué cambió

| Dónde | Antes | Ahora |
|---|---|---|
| Planetas-puerto (`Shared/PortSprites.Glyph`; Bitácora de Misión y Correo Estelar) | estrella · media luna · cruz | **hexágono · gota · ola** (se quedan corazón, rombo, triángulo, cuadrado y aro; cada glifo conserva su color) |
| Cartas de Parejas (`ShapeKind` de `SymbolSprite`) | estrella · media luna | **galaxia espiral** (`Galaxy`) · **luna llena con cráteres** (`FullMoon`) |
| Señales de Piloto (`PilotContract.Shapes`) | estrella · media luna | **hexágono** (`ShapeKind.Hexagon`, nuevo) · **gota** (`ShapeKind.Drop`) |
| `SymbolSprite`: cristal y constelación | brillo de 4 puntas · tres estrellas de 5 puntas | brillo redondo · tres luceros redondos |
| Cartas de Parejas (dorso) | destello central de 4 puntas | lucero redondo |
| Bitácora de Misión, hallazgo 14 | «una estrella de mar» (estrella de 5 puntas) | «una flor» (seis pétalos redondos) |
| Meteoros: el lucero de la constelación (`StarLit` / `StarDim`) | estrella de 5 puntas | lucero redondo |
| Aterrizaje Lunar: la bandera del blanco | estrella de 5 puntas | punto redondo |
| Rumbo a Casa: el faro de la base | estrella de 4 puntas | lucero redondo con su aro |
| `SparkleSprite` (el «brillo» de cuenta regresiva, celebraciones, racha y cielo) y el brillo de La estrella intrusa | destello de 4 puntas | brillo redondo (núcleo con halo) |
| Íconos de la app (`GameIcon.kt`): Parejas, Piloto, La estrella intrusa, Aterrizaje y el «brillo» (`sparkle`) | estrellas de 5 y 4 puntas | lucero redondo / hexágono / brillo redondo |

## Lo que NO se cambió (pendiente de decisión)

Son estrellas de 5 o 4 puntas que funcionan como **premio o calificación**, no como símbolo de un objeto: las estrellas ganadas de la pantalla de resultado (`ResultStar`), la estrella del escudo de liga (`LeagueShield`), el destello de 4 puntas de
la celebración de ascenso de liga (`LeaguePromotionOverlay`) y la «primera estrella» del inicio (`FirstStar`). Si Ricardo quiere que también sean redondas, es otra tarea.
