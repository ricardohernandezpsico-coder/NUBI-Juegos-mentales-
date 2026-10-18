# Los juegos en plasticina — seguimiento (Tarea 94 en adelante)

Ricardo (14-oct-2026): **todos los juegos de Unity pasan a plasticina**, de a un juego por tarea, con Rescate relámpago como piloto. Este es el ÚNICO lugar donde se lleva la cuenta: el inventario de lo que dibuja cada juego, el estado de cada uno y lo que pesa en el APK. Se actualiza en cada tarea de juego.

La técnica (Blender, el cargador de Unity, los pesos) está en [`tools/juegos/README.md`](../tools/juegos/README.md). El estilo es el del kit de la app (`tools/isla/isla.py --kit`: la misma luz, el mismo material `clay()`, sin borde tinta); ver [`diseno-isla.md`](diseno-isla.md).

## El proceso, para cada juego

1. **Ficha de arte** (esta página): qué objetos hay, cuáles son estímulo, qué estados tienen y qué no se puede tocar.
2. **Render en Blender** (`tools/juegos/<juego>.py`): cada objeto con fondo transparente, en sus estados y a la escala del lienzo de 1080.
3. **Integración**: `PlasticArt` entrega el sprite de plasticina si existe y, si no, cae al `ClayRaster` de siempre: se migra un juego sin romper los demás.
4. **Verificación de ese juego** (`verificar-todo.sh --juegos <Juego> --filtro-tests <Juego>`) y **hoja de capturas** reales (`--capturas <Juego>`).
5. **Ricardo lo prueba en el teléfono.** Mientras prueba uno, se prepara el siguiente.

**Reglas que valen para todos:**
- **La claridad del estímulo manda sobre la estética.** Misma silueta y mismo tamaño que hoy, contraste con el fondo (≥ 3:1 medido contra el fondo real del juego), nada que dependa solo del color, y las reglas de símbolos ([`simbolos-neutros.md`](simbolos-neutros.md): sin estrella con puntas, media luna ni cruz).
- **El arte no toca la jugabilidad:** la dificultad, el DDA, los tiempos y las medidas no cambian. Si un render puede cambiar lo difícil que es reconocer algo, se avisa y la medida empieza una serie nueva (versión de arte en el resultado).
- Lo que se anima por código (giros, estelas, halos, partículas, mallas) se queda procedural. El texto sigue siendo `Text` de Unity.
- **Todo objeto de plasticina que se toca dentro de un juego tiene su estado presionado con textura** (regla de Ricardo, 14-oct): la pieza **aplastada** (~8 % más baja y ~4 % más ancha), **un punto más oscura** y con una **huella de dedo** suave y poco profunda al centro; mismo lienzo y escala que la normal, el color medio dentro de la regla de ΔE de la pieza y, si el color es el dato, que se siga distinguiendo. Se muestra al apoyar el dedo (al menos ~80 ms) y vuelve al soltar con un rebote chico de escala; con «quitar animaciones» solo cambia la imagen. Lo que el JUEGO enciende (luz, halo, secuencia a recordar) no lleva huella: la huella es solo para el toque de la persona. Las losas del kit ya traen su imagen presionada, SIN huella (decisión de Ricardo: ya aprobó su forma de hundirse, y una huella en el centro estirado del nine-slice se deformaría): **la huella va en los objetos propios, no en las losas**; si un objeto propio es nine-slice (la roca de Meteoros), la huella va en una esquina, que no se estira. Cómo quedó cada juego: sección «Estado presionado» al final (Tarea 104); los juegos que falten migrar nacen con él.
- Lo que el código tiñe con `Image.color` entrega la imagen en BLANCO/gris (si es plasticina de color fijo, no se tiñe).

## Estado

Estados: **inventario** → **render** → **integrado** → **aprobado por Ricardo**. Pesos al APK: ver la sección al final.

| # | Juego | id | Objetos propios (aprox.) | Estado | Fecha | Capturas |
|---|---|---|---:|---|---|---|
| — | **Piezas comunes (fase 0)** | — | losas, marcas, anillo, Nubi 3D | integrado (verificación completa; falta que Ricardo lo vea en el teléfono) | 14-oct | [antes](previews/plasticina/antes-stroop.png) → [después](previews/plasticina/despues-stroop.png) · [Carga exacta](previews/plasticina/despues-calculo.png) · [Constelaciones](previews/plasticina/despues-parejas.png) |
| 1 | Rescate relámpago | `radar` | 6 cápsulas + 6 íconos, roca, aro del radar, nave, asiento | integrado; **falta que Ricardo lo pruebe** | 14-oct | [antes](previews/plasticina/antes-radar.png) → [después](previews/plasticina/despues-radar.png) |
| 2 | Piloto Estelar | `piloto` | 15 señales (+ versión chica), nave | integrado (14-oct); **falta que Ricardo lo pruebe** | 14-oct | [antes](previews/plasticina/antes-piloto.png) → [después](previews/plasticina/despues-piloto.png) |
| 3 | Freno de Emergencia | `freno` | botón ×2, ¡ALTO!, plataforma, cohete ×3 | integrado (14-oct); **falta que Ricardo lo pruebe** | 14-oct | [antes](previews/plasticina/antes-freno.png) → [después](previews/plasticina/despues-freno.png) |
| 4 | Satélites | `satelites` | 4 (satélite, planeta, sobre, nube); el resto, losas del kit | integrado (14-oct); **falta que Ricardo lo pruebe** | 14-oct | [antes](previews/plasticina/antes-satelites.png) → [después](previews/plasticina/despues-satelites.png) |
| 5 | Tinta o Palabra | `stroop` | 15 losas con el color exacto de la tinta, la tarjeta y las orillas + gota y libro | integrado (14-oct, con verificación completa); **falta que Ricardo lo pruebe** | 14-oct | [antes](previews/plasticina/antes-stroop-arte.png) → [después](previews/plasticina/despues-stroop-arte.png) |
| 6 | Constelaciones | `parejas` | 19 | 12 objetos + 4 gemelos (con borde de tinta), luz dormida, dormida presionada y abierta; los aros y las líneas, procedurales; la tarjeta de arriba y la de entrada, losas del kit | integrado (15-oct), con el estado presionado desde el inicio; **falta que Ricardo lo pruebe** | 15-oct | [antes](previews/plasticina/antes-parejas-arte.png) → [después](previews/plasticina/despues-parejas-arte.png) · [objetos](previews/plasticina/parejas-objetos.png) |
| 7 | Bodega de carga | `bodega` | 30 | 12 objetos (sin borde de tinta), escotilla (marco, aro, 3 interiores, hoja de la puerta y su presionada), robot, caja (una cinta), esclusa y 3 círculos del carro; el casco, los remaches y el aro de sello, procedurales; la tarjeta, el aviso y la entrada, losas del kit | integrado (15-oct), con el estado presionado desde el inicio; **falta que Ricardo lo pruebe** | 15-oct | [antes](previews/plasticina/antes-bodega.png) → [después](previews/plasticina/despues-bodega.png) · [piezas](previews/plasticina/bodega-piezas.png) |
| 8 | La estación de correo | `correo` | 28 | 12 cartas (4 planetas × normal, sello dorado, lazo), 4 planetas con figura, dial (ocho puntos), puerta y manija de la caja fuerte, torre, 2 lámparas, nave, saco, raya y reloj (base, tapa y tapa presionada); los buzones, la caja fuerte, el faro y las tarjetas, losas del kit; el marco punteado, la barra del día, el propulsor y los resplandores, procedurales | integrado (16-oct), con el estado presionado desde el inicio; **falta que Ricardo lo pruebe** | 16-oct | [antes](previews/plasticina/antes-correo.png) → [después](previews/plasticina/despues-correo.png) · [piezas](previews/plasticina/correo-piezas.png) |
| 9 | Rastro de luz | `secuencia` | 9 luceros + 9 presionados (con huella de dedo); el resto, losas del kit | integrado (14-oct), con el estado presionado el mismo día; **falta que Ricardo lo pruebe** | 14-oct | [antes](previews/plasticina/antes-secuencia.png) → [después](previews/plasticina/despues-secuencia.png) |
| 10 | Carga exacta | `calculo` | 2 propios (aro y núcleo del reactor); el resto, losas del kit | integrado (14-oct); **falta que Ricardo lo pruebe** | 14-oct | [antes](previews/plasticina/antes-calculo-arte.png) → [después](previews/plasticina/despues-calculo-arte.png) |
| 11 | Engranajes | `engranajes` | 27 | los 6 engranajes (+ el motor presionado), el radar, la turbina (5 aspas: la de 4 era una cruz «+»), la polea (+ presionada), el portón, el marco, la plataforma y la caja de la carga, la barra dentada, el casco, las 4 flechas del motor, las 2 del cartel y 3 triángulos; «Arrancar» es una losa Sky del kit y el cartel que falla lleva la aspa de lo común. **Los dientes son el trapecio exacto** (±0,04 dp; ±0,12 dp contra el contorno de antes, se pedía ±0,3) y el engrane en reposo no se pisa; lo que gira se renderizó desde arriba con la luz promediada en cuatro; la sala, el cubo, la sombra, las correas y la llave quedan procedurales | integrado (16-oct) y con **volumen** desde la Tarea 115 (canto que no gira, sombra de contacto, color por rol, placas, cohete de cilindro con remaches, losas del kit abajo; ver `diseno-engranajes.md` §16) y **con los botones del kit** desde la Tarea 116 (17-oct: «Arrancar», la cuenta de llaves, los carteles y MOTOR son las losas de VERDAD y Rescate sin canto ni halo propios; ver §17); **falta que Ricardo lo pruebe** | 17-oct | [antes](previews/plasticina/antes-engranajes.png) → [después](previews/plasticina/despues-engranajes.png) · [volumen: antes](previews/plasticina/antes-engranajes-volumen.png) → [después](previews/plasticina/despues-engranajes-volumen.png) · [piezas](previews/plasticina/engranajes-piezas.png) · [propuesta aprobada](previews/plasticina/propuesta-volumen.png) · [diagnóstico de botones](previews/plasticina/diagnostico-botones.png) |
| 12 | Acoplamiento | `acoplamiento` | 9 + pieza y hueco horneados por módulo | **integrado v3 (17-oct, Tarea 118)** tal como lo aprobó Ricardo en la hoja de la Tarea 117 ([`propuesta-acoplamiento-v3.png`](previews/plasticina/propuesta-acoplamiento-v3.png)): sin estación en la pantalla de juego (bloque de 38 dp), fila de 8 luces con onda y medalla, el «plop» al encajar, la pieza como almohada de plasticina con la lámpara horneada en la orientación que se ve (fuga del espejo 0), el hueco como cavidad con cordón, losas del kit en el panel, «Encaja», «Espejo» y los avisos, y la pantalla final con la escultura de las piezas acopladas (`diseno-acoplamiento.md` §13); **falta que Ricardo lo pruebe en el teléfono**. Con este son **17 de 17** los juegos de la app en plasticina | 17-oct | [estudio](estudio-acoplamiento-plasticina.md) · [hoja v3](previews/plasticina/propuesta-acoplamiento-v3.png) · [capturas](previews/capturas/acoplamiento.png) |
| 13 | Aterrizaje Lunar | `aterrizaje` | 7 | la nave (simétrica), el cuerpo de la regla (losa crema lisa, nine-slice), el gallardete, la Tierra, el satélite y las 2 cúpulas del marcador; las insignias son las monedas de lo común; **las marcas, los números y el asta de la regla quedan procedurales en tinta y en su x** (medido antes/después: ≤ 0,5 unidades), y la luna y las cordilleras también | integrado (16-oct); **falta que Ricardo lo pruebe** | 16-oct | [antes](previews/plasticina/antes-aterrizaje.png) → [después](previews/plasticina/despues-aterrizaje.png) · [piezas](previews/plasticina/aterrizaje-piezas.png) |
| 14 | En la punta de la lengua | `anagramas` | 3 (ficha, casilla, perla); la tarjeta, los botones y el panel, losas del kit | integrado (14-oct); **falta que Ricardo lo pruebe** | 14-oct | [antes](previews/plasticina/antes-anagramas.png) → [después](previews/plasticina/despues-anagramas.png) |
| 15 | Lluvia de meteoros | `meteoros` | 4 rocas nine-slice + 4 presionadas, brasa y grietas (capas), 2 estrellas; la placa y el panel, losas del kit | integrado (14-oct), con el estado presionado desde el inicio; **falta que Ricardo lo pruebe** | 14-oct | [antes](previews/plasticina/antes-meteoros.png) → [después](previews/plasticina/despues-meteoros.png) |
| 16 | ¿Verdad o disparate? | `disparate` | 5 (antena parabólica, 2 íconos, 2 barras del medidor); el resto, losas del kit | integrado (14-oct), con la antena y la onda corregidas el mismo día; **falta que Ricardo lo pruebe** | 14-oct | [antes](previews/plasticina/antes-disparate.png) → [después](previews/plasticina/despues-disparate.png) (la corrección: [antes](previews/plasticina/antes-correccion-disparate.png)) |
| 17 | La estrella intrusa | `intrusa` | 0 propios: placas, opciones, «Seguir» y panel, losas del kit (las 77 figuras se quedan procedurales) | integrado (14-oct); **falta que Ricardo lo pruebe** | 14-oct | [antes](previews/plasticina/antes-intrusa.png) → [después](previews/plasticina/despues-intrusa.png) |

(El orden de los números 2 a 17 es el **propuesto** más abajo; Opus decide.)

## Lo que NO entró en la Tarea 94 (a decidir en las siguientes)

- **Decorado de Rescate (y de todos los juegos):** el planeta lejano, la estación accidentada, los restos, el polvo y el eje lima del centro del radar siguen planos (procedurales). **Queda para UNA pasada final de decorados, cuando estén todos los juegos migrados** (decisión de Opus, 14-oct). El barrido, la estática, el rayo tractor, las llamas y los aros de la revelación se quedan como efecto (son animados o se tiñen).
- **`tools/art-preview` no compila (pendiente de la pasada final de decorados, decisión de Opus, 15-oct):** la herramienta que vuelca el arte PROCEDURAL sin Unity (`dotnet run --project tools/art-preview`) dejó de compilar: le falta `DockingPlan.cs` (de Acoplamiento, anterior a la plasticina) y desde las tareas de plasticina los generadores usan `PlasticArt`/`PlasticTouch`, que su `ArtPreview.csproj` no incluye (ni su `UnityStub.cs` tiene `Resources.Load`). Hasta entonces las láminas procedurales de `docs/previews/` (por ejemplo `constelaciones.png`) quedan como eran y las de plasticina salen de los scripts `tools/juegos/hoja_*.py` y de las capturas reales. Se arregla en la pasada final de decorados.
- **Fase 0 por completar:** ~~el punto de color de los avisos (`Toast`) y de `PhasePill` quedó en tinta sólida sobre la losa y se ve algo pesado~~: **hecho en la Tarea 96 (14-oct):** el punto va en tinta al 50 % (`NeuroStyle.WithAlpha(Ink, 0.5f)`), con verificación completa. `ProgressDots`, `TileSprites` (las fichas de Tinta o Palabra y otros), `RingSprite`, los destellos y el cielo siguen procedurales: cada juego pasa sus piezas propias en su tarea.
- **Sin `art_version` en las medidas (decisión de Opus, 14-oct):** el cambio de arte se marca con su fecha (Rescate, 14-oct-2026; cada juego migrado anota la suya en su fila de arriba); si hace falta separar series, se hace por fecha.
- **Dos cápsulas con el canto en sombra más tenue:** la gota y el triángulo rinden 2,0 y 2,3:1 en su borde más oscuro contra el vidrio MÁS claro (el cuerpo rinde ≥ 4:1): mirarlas en el teléfono antes de tocar la luz.

## Piezas comunes (fase 0): las que benefician a los 17

Todo el HUD, la pausa, la cuenta regresiva y el cierre son de `Games/Shared`. Hoy son `RoundedRectSprite` 9-slice (33 archivos) más `Outline`/`Shadow` de Unity (`NeuroStyle.ClayFrame`, 19 archivos) con borde tinta y sombra dura. La fase 0 las pasa a las losas del kit de la app.

| Pieza | Archivo | Hoy | Plasticina |
|---|---|---|---|
| Botones («Continuar», pausa, «¡Rescatar!», «Saltar tutorial») | `ExitButton`, `PauseMenu`, `GuidedTutorial`, `NubiCoach` | `RoundedRect(64)` + `ClayFrame` + `PressScale` | losa `md` sol/cielo/lila/crema con su estado presionado |
| Pastillas del HUD (nivel, ronda/puntos, racha) | `GameHud` | `RoundedRect(64)` oscura + `ClayFrame`, texto de color | losas `ch` (crema, sol) y `md` (racha), texto tinta |
| Aviso y píldora de estado | `Toast`, `PhasePill` | `RoundedRect(64)` + `ClayFrame` | losa `md` crema, punto de color |
| Casillas de respuesta (normal, elegida, acierto, «no estaba») | cada juego; Rescate y `TileSprites` son los modelos | gris 256 px estirado, sin 9-slice, teñido | losa `md` «noche» (oscura, para que los símbolos de color resalten), con marca ✓ / ✗ de plasticina y texto |
| Paneles de pausa y de final (el de final ya no existe en Unity: Tarea 126, el resultado vive en la app) | `PauseMenu`, `GameControllerBase`, `RadarGameController.BuildEnd` | `RoundedRect(64)` azul noche + `ClayFrame` | losa `gr` crema |
| Cuenta regresiva (título, anillo) | `CountdownScreen` | píldora de 3 capas, `RingSprite` | losa `ch` crema; anillo de plasticina |
| Cierre «¡Listo!» | `FinishCurtain` | texto con contorno tinta | placa `gr` sol detrás del texto; desde la Tarea 126 con el emblema del juego (atlas `emblemas`: las 17 fichas de la app a 6 px/dp, 2,4 MB de PNG / ≈ 1,5 MB con ASTC 6×6) en el momento del objeto y sobre «¡Listo!» |
| Marca de respuesta ✓ / ✗ | `AnswerMarkSprite` (Estrella intrusa, Disparate, Meteoros, Freno, Satélites, Acoplamiento, Rescate) | disco lima/coral con glifo tinta | disco de plasticina con glifo de churro tinta (la ✗ es un aspa, no una cruz) |
| La Nubi de los tutoriales | `NubiTeacherSprite` | Nubi plana vieja con destellos en cruz y pizarra | `nubi_3d` (el render de la app) con su sombra |

Se quedan procedurales: `RingSprite`, `RadialGlowSprite`, `DiscSprite`, `SparkleSprite` (brillo redondo), el cielo (`WorldBackdrop`), las mallas (`LineGraphic`, `LightMesh`, `WedgeGraphic`, `RouteGraphic`…) y todos los efectos.

## Orden propuesto para los otros 16

Criterio: agrupar los juegos que comparten piezas, empezar por los que más se benefician de la fase 0 o se reaprovechan entre sí y dejar para el final los que tienen la mecánica en la silueta (cualquier bisel cambia la respuesta).

| # | Grupo y juegos | Qué se comparte | Por qué en este orden |
|---|---|---|---|
| 2–3 | **Naves y formas fijas:** Piloto Estelar, Freno de Emergencia | Piloto usa las MISMAS 5 formas y colores fijos que las cápsulas de Rescate (hexágono, gota, círculo, cuadrado, triángulo) más un detalle (punto, anillo, franja); su nave se parece en estilo a la de Rescate. Freno comparte el cohete con Constelaciones | Es lo que Ricardo pidió ver primero («por ejemplo, Rescate y Piloto») y se reaprovecha la geometría de Rescate |
| 4–5 | **Solo losas y discos:** Carga exacta, ¿Verdad o disparate? | Todo sale del kit: botones, celdas, placas; solo el reactor/celdas y la antena son propios | Dan cobertura rápida y baratos; prueban el nine-slice con texto en el centro |
| 6–7 | **Un objeto repetido:** Satélites, Rastro de luz | Un solo satélite idéntico; 9 luceros (esferas) de color; ambos con aros procedurales que se quedan | Pocos sprites; la regla es que sean idénticos y planos (Satélites) o redondos y de colores muy parecidos (Rastro) |
| 8 | Tinta o Palabra | Botones de la tinta (color fijo), gota y libro blancos | El color ES el estímulo: va con cuidado, pero es corto |
| 9–10 | **Fichas de texto:** En la punta de la lengua, La estrella intrusa | Fichas, casillas, placas y luceros: discos y losas; las 77 figuras del grabado se quedan procedurales | Casi todo es kit |
| 11 | Lluvia de meteoros | Roca que se estira sin conservar proporción: hace falta una roca estirable (nine-slice) o 3 proporciones | Es el único del grupo con arte propio que se estira |
| 12–14 | **Objetos con silueta:** Constelaciones, Bodega de carga, La estación de correo | Constelaciones y Bodega dibujan 12 objetos cada uno (cosas); Correo tiene 12 cartas y 4 planetas con figura | Son los de más sprites (20, 24 y 27) y los de la figura más fina; conviene tener ya el estilo de objeto asentado |
| 15–17 | **La mecánica es la silueta:** Aterrizaje Lunar, Engranajes, Acoplamiento | Regla de precisión; dientes que encajan con holgura cero; módulos poliominós con hueco idéntico | Riesgo más alto: un bisel que ensancha un diente o corre una marca cambia la respuesta. Al final y con medición |

## Inventario por juego

Tamaños en unidades del lienzo de 1080 de ancho (**u**); 1 dp = 3 u. **E** = estímulo (lo que hay que percibir para responder), **D** = decorado, **I** = interfaz. Hecho por lectura del código el 14-oct-2026 (tamaños aproximados). Todos los juegos usan `CanvasScaler` 1080×1920 y comparten `WorldBackdrop` (cielo), `ExitButton`, `CountdownScreen`, `PauseMenu` y `NubiCoach`; esos están en la fase 0 y no se repiten abajo.

### 1. Rescate relámpago (`radar`, `Games/Radar/`) — piloto

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| 6 cápsulas (hexágono A6E36B, gota 7FD8FF, círculo FF8A6B, cuadrado FFC94A, triángulo B79BFF, rombo FFB3D1) con ventanilla de astronauta | E | una por forma (forma + color fijos); en el radar, voladora hacia la nave, en el tablero | forma de ~55 dp (165 u); sprite 1,28× | no |
| Emblema (la misma forma sola) | E / I | en las 6 casillas (~36 dp), ícono del contador (26 dp), final | 78–108 u | no |
| Roca gris | E (distractor) | 1; hasta 2 a la vez | ~55 dp (166 u) | no |
| Radar: bisel con 48 marcas, vidrio con 2 aros punteados y cruz de adorno | E (el vidrio) / D (el bisel) | 1; en la v4 el vidrio mide 146 dp de radio | sprite ~1080 u | no |
| Barrido (estela), línea del haz, cabeza del haz, estática (2 variantes), destello | efecto | animados | = vidrio | no |
| Nave de rescate (casco crema 200×38 dp, cabina, 2 propulsores, aletas) y 10 ventanas | D / I (cuenta lo rescatado) | ventana vacía o del color de la cápsula | sprite 240 dp (720 u) | no |
| Planeta lejano, estación accidentada, restos (5), polvo (30), nebulosas (4), humo | D | quietos o a la deriva | 138 dp–1000 u | `RadialGlow`, `Disc` |
| Botones del tablero ×6 y «¡Rescatar!» | I | vivo, apagado (45 %), elegido, acierto (verde, ✓), «no estaba» (vino, ✗), «faltó» (aro punteado coral) | 104×74 dp (312×222 u); 172×44 dp | `AnswerMark` |
| Marcador (ícono + contador, racha, barra de tiempo, franja de mensajes), tarjeta final | I | — | — | `GameHud`, `RoundedRect` |
| Llamas, rayos tractores (4), chispas | efecto | — | — | no |

Riesgos: la **silueta** es lo que se reconoce en el destello; el **contraste figura-fondo no cambia con el nivel** (patente) y el vidrio debe seguir oscuro; sin lugares fijos ni cuadrícula (se responde QUÉ se vio, no DÓNDE); la zona del destello no crece con el nivel; los tiempos del destello y de la máscara no cambian. El rombo y el cuadrado, el hexágono y el círculo son los pares que el redondeo de la plasticina puede acercar.

### 2. Tinta o Palabra «Dos orillas» (`stroop`, `Games/Stroop/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| La palabra (ROJO, AZUL, AMARILLO, BLANCO) en 4 tintas (C93C3C, 4A86E8, F2CC1D, F4F4F4) | E | `Text` con el color exacto; entra deslizando | fuente 190 u | no (NO es sprite) |
| Tarjeta de la palabra | I (soporte) | fondo fijo #141B36, borde del color de la orilla; rojo al fallar | ~792×(280–590) | `RoundedRect`, `ClayFrame` |
| Gota (TINTA) y libro abierto (PALABRA) | E (la regla) | 2 sprites blancos, se tiñen | 54–120 u | no |
| Orillas (haces violeta/turquesa) | E (la regla) | encendida 100 % / apagada 35 % | 120×330–640 | no |
| Cinta «Responde: TINTA/PALABRA» | E / I | 2 estados | ~787×104 | `RoundedRect` |
| 4 botones de respuesta 2×2 | I | color = su tinta; reposo, presionado (escala 0,9), atenuado | ~403×207 | `TileSprites` (gris 256 px sin 9-slice), `PressScale` |
| Reloj / 16 puntos de avance, píldora de estado | I | — | barra 960×16 | `ProgressDots`, `PhasePill` |

Riesgos: **el color de la tinta ES el estímulo** (4 hex validados con `tools/paleta_daltonismo.py`, separación ≥ 20 también en protanopía y deuteranopía): el botón de plasticina lleva la tinta como color base PLANO, sin luz de color ni oclusión que mueva el tono; la palabra sigue siendo `Text`; gota y libro se entregan blancos (se tiñen). El botón hoy se estira sin 9-slice: hace falta losa o aspecto ~2:1 exacto. Sin ruido ni sombras que bajen el contraste del nombre (5,0 / 4,9 / 11,2 / 15,9:1). No tocar: deslizamiento de la tarjeta, temblor, `SparkBurst`, `RingBurst`, pulso de la cinta, gradiente del haz.

**Ficha de arte (Tarea 100, hecha el 14-oct):** los 4 botones (con la tinta como color base y la luz de plasticina SIN mover el tono: ΔE ≤ 1,6 de su color, separación con daltonismo intacta), la tarjeta (fondo #141B36 y borde de la orilla), la cinta, la tarjeta de la regla y el panel del final son losas; la gota y el libro, renders en gris claro para teñir. Se quedan procedurales los haces de luz, el aro, los puntos, el reloj y la palabra. Medidas y detalle: [`diseno-tinta-o-palabra.md`](diseno-tinta-o-palabra.md) §9.

### 3. Piloto Estelar (`piloto`, `Games/Piloto/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| Señal: 5 formas (hexágono A6E36B, gota 7FD8FF, círculo FF8A6B, cuadrado FFC94A, triángulo B79BFF) × 3 detalles (punto, anillo, franja) = 15 | E | nace (fundido), viva, atrapada (×1,6 y fundido), equivocada, «se fue» | forma 34 dp (102 u); sprite 131 u | no |
| Nave | E (lo que se guía) | normal, fuera de ruta (tinte ×0,78), bamboleo, halo | 56 dp (169 u) | no |
| Anillo del tiempo restante | E / I | se vacía en radial | 54 dp (162 u) | `RingSprite` |
| Ruta (canal + bordes), arco dorado de sector | E | malla, se desvanece | 108–208 dp | no (malla) |
| Balizas (2 por fila, hasta 14 filas) | E | normal, encendida, mala | 22–32 u | `Disc`, `RadialGlow` |
| Tarjeta de misión (ícono + nombre), aviso «¡Nueva misión!» | E (referencia) | late 2 veces | ícono 84 u (forma ~66); aviso 156 u | no |
| Franja del dedo, barra del viaje, textos, final | I | — | — | `RoundedRect`, `Disc` |

Riesgos: la jugabilidad es silueta + detalle; las «parecidas» (nivel 3 en adelante) son MISMA forma y MISMO color con otro detalle: los detalles deben ser planos, incrustados y de alto contraste; las 5 formas siguen distintas (hexágono vs círculo, cuadrado vs círculo); orientación fija (punta de la gota y del triángulo arriba), vista cenital; deben leerse a 102 u y a ~66 u en la tarjeta. Nada de fondo que parezca señal. Comparte las 5 formas y los colores con Rescate. No tocar: ruta y arco, balizas, rayo, hiperimpulso, anillo del tiempo, bamboleo, latido de la tarjeta.

**Ficha de arte (Tarea 95, hecha el 14-oct):** señales = las MISMAS formas, colores fijos y grosor que las cápsulas de Rescate (`tools/juegos/formas.py`), sin ventanilla y con el detalle de churro de tinta incrustado (punto, anillo y franja: planos, grandes y de alto contraste); versión chica de 96 px para la tarjeta de misión; nave de arriba con la punta hacia arriba; los paneles pasan a las losas de la fase 0 (la franja del dedo, la más oscura). Se quedan procedurales la ruta, las balizas, el arco, el rayo, el anillo del tiempo y el halo de la nave. Medidas y detalle: [`diseno-piloto.md`](diseno-piloto.md) §16.

### 4. Freno de Emergencia (`freno`, `Games/Freno/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| Botón de lanzamiento (flecha arriba) | E + I | apagado (azul 2A3590), encendido (lima, más claro), apretado (×0,9) | 207–210 u | no |
| Baliza | E | apagada, lima, coral con ¡ALTO!, parpadeo | 46 u | `Disc` |
| Señal ¡ALTO! (octágono coral con aro crema) + texto | E | aparece de golpe y se desvanece | 400 u | no |
| Resplandor del carril | E | oculto / lima | ~460 u | `RadialGlow` |
| Plataforma de lanzamiento | D (indica carril) | 2, 3 o 4 carriles | 260–300 u | no |
| Cohete (coral, celeste, lima) | D | despega, vuelve, salta | 212–230 u | **sí: lo usa Constelaciones** (`SymbolSprite.Rocket`) |
| Marca ✓ / ✗, estrellas del cielo (brillo redondo), medidores | I / D | — | — | `AnswerMark`, `Sparkle` |

Riesgos: el estímulo «ir» se lee en luminancia (apagado oscuro, encendido claro); los carriles son idénticos entre sí; el cohete lima/coral choca con «lima = ir / coral = alto» (neutralizar); el cohete se dibuja en diagonal y el `Image` lo gira 45° (un render vertical exige quitar esa rotación); verlo de lado, no de frente (4 aletas = cruz). No tocar: llama, humo, vapor, salto, sirena, parpadeo de baliza.

**Ficha de arte (Tarea 96, hecha el 14-oct):**

| Pieza | De plasticina (atlas `Resources/Plasticina/freno`) | Cómo quedó |
|---|---|---|
| Botón de lanzamiento (2) | `boton_apagado`, `boton_encendido` (256 px, lienzo ±1,1) | el disco de hoy (radio 0,8 + 0,08 de borde) como cúpula de plasticina con la flecha de churro: crema sobre azul oscuro (apagado) y tinta sobre lima (encendido). El apagado va algo más oscuro que el de hoy (#222B78 contra #2A3590) porque sin el borde de tinta el suelo de la luna lo tapaba |
| Cohete (3) | `cohete_coral`, `cohete_cielo`, `cohete_lima` (256 px, lienzo de 1,9) | **el cohete de Piloto con el acento de cada carril** (`tools/juegos/cohete.py`): casco crema, punta y aletas del color del carril, ventanilla y llama. Recto (el procedural venía en diagonal y se giraba 45°: `BrakeSprites.RocketAngle`). El casco crema domina, así el lima («ir») y el coral («alto») ya no se confunden con el cohete |
| Plataforma | `plataforma` (320 px, lienzo ±1,05) | losa lila con franjas de precaución (sol y tinta) y dos torres con travesaños, con las medidas de hoy |
| Señal de ALTO | `alto` (400 px, lienzo ±1,1) | octágono coral de plasticina con el aro crema incrustado; el «ALTO» sigue siendo `Text` |
| Baliza, resplandor del carril, humo, llama, estrellas | se quedan procedurales | son luz o efecto (igual que las balizas de Piloto) |

El cohete de Freno ya NO usa `SymbolSprite.Rocket` (`BrakeSprites.Rocket(carril)`): las cartas de Constelaciones tuvieron el suyo hasta la Tarea 106 (fila 6: ya tienen su propio cohete de plasticina; `SymbolSprite.Rocket` queda solo como respaldo de otros juegos). Medidas, pruebas y pesos: [`juegos/freno.md`](juegos/freno.md) § Plasticina.

### 5. Satélites «enciende tu planeta» (`satelites`, `Games/Satelites/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| Satélite (disco + brazo + 2 paneles) | E | **todos idénticos**; hasta 13; se mueven, se detienen, se marcan | disco 84 u; sprite 191 u | no |
| Aro dorado de la señal, aro celeste «marcado», aro punteado | E / I | pulsa, marcado/desmarcado, «con mensaje y no marcado» | 125–131 u | `RingSprite`, `DashedRing` |
| Sobre del mensaje | E | sobre el satélite; vuela al planeta | 110 u | no |
| Nube de polvo | E (oclusión) | **opaca** (0,97), 5 bultos | silueta ~480×375 u | no |
| Planeta a oscuras + 4 continentes | D / meta | oscuro; al final ×1,75 | 334 u | no |
| Luces del planeta (hasta 80) | I | apagada / encendida | 13–54 u | `Disc`, `Glow` |
| Discos de rondas (8), órbitas (malla) | I / E | lleno = perfecta, medio = parcial | 42–60 u | `RingSprite`, `Disc` |

Riesgos: los satélites son IDÉNTICOS (la señal es el aro y el sobre, nunca el dibujo): UN solo asset, sin rotación ni luz por instancia, vista plana; sin antena vertical sobre el brazo (cruz); la nube plana y OPACA; el planeta oscuro y de bajo contraste; los estados de la revelación se leen por forma (aro continuo/punteado, visto/aspa). No tocar: órbitas, asentamiento, pulso del aro, halos, vuelo de sobres.

**Ficha de arte (Tarea 98, hecha el 14-oct):** satélite (UN solo dibujo para todos), planeta (esfera mate azul noche, sin continentes), sobre y nube de polvo (opaca y de relieve bajo) son renders propios en el mismo lienzo y escala que los de hoy; el rótulo «¿Aquí se cruzaron?» y el aviso de sorpresa pasan a losas «noche» del kit. Se quedan procedurales los aros y el medio disco (se tiñen), las luces del planeta y sus resplandores, las órbitas y el vuelo del sobre. Medidas y detalle: [`diseno-satelites.md`](diseno-satelites.md) §13.

### 6. Constelaciones (`parejas`, `Games/Parejas/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| 12 objetos (planeta, cohete, cometa, ovni, cristal, casco, luna llena, satélite, telescopio, asteroide, antena, brújula) | E | 1 sprite c/u, color propio | caja 155–210 u (dibujo ~123–165) | `SymbolSprite` (cohete: también Freno) |
| 4 gemelos (planeta sin anillo, cohete con llama grande, ovni con patas, casco con antena) | E | `variant=1` | igual | no |
| Luz dormida (reverso, idéntico en todas), luz abierta | E + toque | 1 y 1 | Ø168–228 u | no |
| Aros de pareja hecha (dorado continuo, celeste punteado) | I | 2 | 221–300 u | no |
| Líneas de unión (3 mallas), fila «De memoria» (puntos y aros), tarjetas, barra | I | — | — | `LineGraphic`, `Disc`, `Ring` |

**Ficha de arte (Tarea 106, 15-oct):** los 12 objetos y los 4 gemelos son el dibujo del boceto aprobado hecho de plasticina (`tools/juegos/constelaciones.py`): piezas planas apiladas con el canto blando, la misma silueta, el mismo tamaño (caja de 56 u, 4 px por unidad) y el mismo detalle GRANDE que distingue a cada gemelo; el planeta es una esfera de verdad con su anillo inclinado (flota, sin sombra de contacto) y el resto son relieves con la sombra de contacto del kit (los paneles del satélite llevan tres rayas paralelas, nunca un «+»). El **borde de tinta** del boceto (1,3 u hacia afuera de la silueta) lo agrega `empaqueta.py` desde la silueta: separa a los objetos claros (casco, cohete, brújula) del disco claro de la luz abierta. La **luz dormida** es una esfera de arcilla azul noche con un grano claro (el núcleo) y su brillo; su **estado presionado** (regla del 14-oct) es la misma esfera aplastada, un punto más oscura y con la huella de un dedo hundida LEJOS del núcleo (la huella no pisa el estímulo); la luz que se toca se ve así 80 ms ANTES de voltearse (el volteo espera; con «quitar animaciones» también). La **luz abierta** es un disco claro de arcilla. Los aros de las parejas hechas (dorado continuo y celeste punteado), las líneas, los puntos de «De memoria» y las chispas se quedan procedurales (se tiñen, se trazan y se encogen). La **tarjeta de arriba** y la **tarjeta de entrada** son losas «noche» del kit (el aviso de «no son iguales» sigue en el texto, ya no en el borde); el panel del final ya lo era (fase 0). Medidas, pruebas y pesos: [`diseno-constelaciones.md`](diseno-constelaciones.md) §13.

Riesgos: los gemelos se distinguen por un detalle grande (anillo, llama, patas, antena) y los tests de píxeles lo fijan (> 700 px distintos; anillo en (±19,5; ∓6), llama en (0,−24)): no afinar las patas ni la antena; planeta, luna, brújula, casco y cometa son discos (el rasgo interior debe ser grande); el dorso es un único render sin variación por luz; dormida vs abierta se lee en luminancia; luna LLENA sin terminador, cristal sin destello de 4 puntas, brújula de 2 agujas (`NoSymbolOutsideTheRule` busca `Star(/Crescent(/Sun(/Cross(`). **`ConstelacionArtTests` depende de las coordenadas del sprite procedural**: pasar a render obliga a reescribirlas. No tocar: líneas, volteo (la luz se escala en X), pista, chispas.

### 7. Bodega de carga (`bodega`, `Games/Bodega/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| Casco de la bodega, remaches (11) | D | giran con la bodega | Ø1032 u ×0,8–1 | `Disc` |
| Esclusa (vidrio con marco dorado) y aro de sello (18 marcas) | E (referencia del giro) | 2 sprites + destello | 210 y 240 u | no |
| Escotilla ×6/8/10 (marco, interior, aro tinta, 2 hojas, junta) | I (casilla; el LUGAR es el estímulo) | vacía (oscura), con algo (luz cálida), cerrada | marco 207 u, interior 168 u | no |
| 12 objetos (llave, campana, farol, manzana, hongo, taza, paraguas, libro, gema, reloj de arena, pluma, bellota) | E | 1 c/u | 120 u (carro 78 u) | **10 salen de `BitacoraSprites.RenderFind`** (Bitácora, retirada, código vivo) |
| Caja cerrada, robot (cuerpo, visor, ojos, antena), haz y cuerda | E / D | parpadea cada ~3,6 s | 114×96 u; robot 177 u | no |
| Carro de reparto (hasta 7 círculos) | I | pendiente, llegó al primer intento (menta + ✓), llegó con error | 114 u | `Disc`, `Check` |

**Ficha de arte (Tarea 107, 15-oct):** los **12 objetos** (`tools/juegos/bodega.py`) son las siluetas de siempre (las de Bitácora y la manzana y la taza de Bodega) hechas de relieves de plasticina apilados con el canto blando (la llave y los asas, donas de verdad; la gema, facetada), SIN borde de tinta: se ven sobre fondos oscuros (el interior de la escotilla, la tarjeta, el carro). **La escotilla** (lo que se toca): el marco (plato azul noche), el **aro** (el borde que tapa el de la máscara redonda), los **tres interiores** (vacío, con algo y cerrada; el degradé hacia el borde es una capa de sombra) y la **hoja de la puerta** (media escotilla, que GIRA por código: se renderiza SIN luz direccional, promediando cuatro renders con la luz girada 0°, 90°, 180° y 270°, para que no lleve una sombra que gire con ella); su **estado presionado** es la escotilla entera aplastada (+4 % de ancho, −8 % de alto, escalando la escotilla para que gire bien la hoja), un punto más oscura (×0,93) y la hoja de arriba con la **huella de un dedo** hundida en su cara libre (`puerta_p`); la escotilla tocada se ve así 80 ms ANTES de abrirse (la puerta espera; el estado del juego no). El **robot** (cuerpo esférico, visor, ojos —que parpadean achicándose—, tallo y bolita de la antena), la **caja** (con UNA cinta: la de siempre cruzaba en «+», que se lee como cruz), la **esclusa** (anillo dorado, vidrio, planeta con anillo y estrellas; el reflejo del vidrio es luz) y los **tres círculos del carro** (pendiente, de memoria con ✓, con error). La tarjeta de arriba, el aviso y la tarjeta de entrada son losas «noche» del kit (los bordes de color se van: «Pedido», «bien» o «mal» siguen en el texto); el ✓ del carro es la moneda de lo común. Se quedan procedurales (decoración, pasada final): el casco, los 11 remaches, el aro de sello punteado (gira con la bodega), los resplandores, el haz, la cuerda y las chispas. Medidas, pruebas y pesos: [`diseno-bodega-de-carga.md`](diseno-bodega-de-carga.md) §11.

Riesgos: los objetos se leen a 26–40 dp; pares por silueta (hongo/paraguas, llave/pluma) y por color (campana/farol/bellota naranja; taza/libro/gema/reloj celestes: los separa la silueta); `DoorLeaf` rota por código y `SealRing` gira: nada de luz ni sombra direccional horneada en ellas; marco + interior con `Mask` redonda + hojas + aro son capas con las mismas dimensiones; cara neutra fija en el robot (patente: nada de caras que reaccionen). No tocar: resortes del robot, haz, puertas, vuelo en arco, impulso y giro.

### 8. La estación de correo (`correo`, `Games/Correo/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| 12 cartas = 4 planetas × {normal, sello dorado, lazo} | E | papel crema 112×76 dp con sello liso; dorado: sello 1,2× con borde dentado y glow; lazo: cinta vertical | caja 128×92 dp (384×276 u) | no |
| Planeta del buzón ×4 (Coralia círculo, Celesta triángulo, Lima cuadrado, Uva gota) | E | figura blanca | 160–192 u | no |
| Buzones ×2–4 | I (casilla) | reposo, pulso, tiembla | 234/306/450×336 u | `RoundedRect` (ancho variable) |
| Caja fuerte (botón + dial), faro (botón + torre + lámpara apagada/encendida + haces) | I | brilla dorado; lámpara off/on | 474×264 u; torre 132×198 | parcial |
| Reloj tapado, panel «Hora del día», cinta con 16 rodillos y marco punteado | I / D | tapado/destapado | 168 u; banda 380×100 dp | `Disc`, `RoundedRect` |
| Nave del correo + propulsor + saco, destellos | D | sale SOLO tras encender el faro | 300×132 u | no |

Riesgos: clasificar depende de la **figura del sello** (círculo, triángulo, cuadrado redondeado, gota) a 17 dp (≈51 u) y con las cartas de atrás ×0,7: plana, grande y sin relieve que confunda triángulo/gota ni cuadrado/círculo; **dorado vs lazo es la palanca de dificultad** (sello focal 1,2× con borde dentado vs lazo no focal): el render no debe volver más o menos saliente a ninguno; `MailArtTests` fija `SealShape` y el luma del dorado en grises; caja fuerte y faro se ven siempre igual haya o no encargo; el reloj tapado no insinúa la hora; el dial lleva 8 marcas (no rueda de 3 rayos), el moño es cinta vertical (nunca «+»). Buzones, caja fuerte y faro son 9-slice de ancho variable. No tocar: resortes de la cinta, vuelo en arco, dial, haces, brillo que recorre la cinta, resplandor del sello.

**Ficha de arte (Tarea 108, 16-oct):** las **12 cartas** (`tools/juegos/correo.py`) son el dibujo de siempre (`MailSprites.PaintLetter`) en relieves de plasticina: papel crema con su doblez y sus dos líneas de dirección, el **sello** (blanco con borde de tinta el normal; el **dorado**, ×1,2 más grande, con el borde dentado de una estampilla —cinco mordidas redondas por lado— y borde café, la señal «focal») con la **figura neutra** del planeta al centro (círculo, triángulo, cuadrado redondeado, gota; con un aro fino de tinta por adentro de su contorno para llegar a ≥ 3:1), y en la del lazo la **cinta roja con su moño** (nunca un «+»); 3 px por dp. Los **4 planetas** son esferas con su figura blanca en relieve y el mismo aro fino de tinta por adentro. **Los cuerpos** de los buzones, de la caja fuerte y del faro son losas «noche» del kit; encima, la **puerta** y la **manija** de plasticina, el **dial** (ocho puntos dorados en un aro, que GIRA por código: se renderiza sin luz direccional, promediando cuatro renders con la luz girada), la **torre** y las dos **lámparas**. El **reloj** es una base (aro de tinta y fondo azul noche) con su **tapa** lila y su **tapa presionada** (hundida, con la huella de un dedo). Todo lo que se toca (buzones, caja fuerte, faro, reloj, botones de la hoja y del resumen) tiene su estado presionado (≥ 80 ms; lo de encima baja 2 dp). Se quedan procedurales (decoración, pasada final): el marco punteado de la cinta, la barra del día, el propulsor, los haces, los resplandores y las chispas. Medidas, pruebas y pesos: [`diseno-correo-estacion.md`](diseno-correo-estacion.md) §13.

### 9. Rastro de luz (`secuencia`, `Games/Secuencia/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| 9 luceros de cristal (7FD8FF, B8A4FF, FFC94A, 8EE07A, FF9F7A, 9FF5D6, F7A8E0, FFE27A, A9C7FF) | E + toque | no hay sprite de «encendido»: halo que respira, escala 1,12, florecimiento | Ø168 u (mayores 192); halo 320–400 u | halo = `RadialGlow` |
| Chispa, rastro, cinta del dedo | E | mallas | — | `Glow`, `Disc`, `LightMesh` |
| Íconos de modo ×4 (→ ← ↻ ≫), ✗ de error | I | blancos, se tiñen al color del modo | 66–360 u | no |
| Aros de error/correcto/pista, disco punteado, viñeta | I / D | — | 216–900 u | `RingSprite` |

Riesgos: **la identidad de un lucero es solo su posición, su color y su nota**; pares cercanos (7FD8FF/A9C7FF, FFC94A/FFE27A, 9FF5D6/8EE07A) que la plasticina no debe desaturar; esferas idénticas, sin grumos propios; hoy son translúcidos y una esfera opaca pesa más y tapa el halo; la ✗ coral pierde contraste sobre FF9F7A y F7A8E0; el cuerpo no crece (toque 72 dp, separación 79 dp). Íconos, ✗, aros, disco punteado y viñeta se quedan procedurales (se tiñen o giran). No tocar: chispa en curva, rastro, partículas, giro del tablero.

**Ficha de arte (Tarea 99, hecha el 14-oct):** los 9 luceros son renders de Blender (esferas idénticas, del color de diseño, opacas, sin sombra de contacto) en el mismo lienzo y escala que los de hoy; las píldoras del marcador y el panel del final, losas «noche» del kit; la ✗ del lucero errado pasa a tinta. Se quedan procedurales el halo, el florecimiento, la chispa, el rastro, la cinta, los íconos de los modos, los aros, el disco punteado y la viñeta. Medidas y detalle: [`diseno-rastro-de-luz.md`](diseno-rastro-de-luz.md) §8.

### 10. Carga exacta (`calculo`, `Games/Calculo/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| Celda de energía (cara, ventanita, brillo, halo) con el número como texto | E | normal E9E5FF, elegida BFE9FF (sube, ×1,08), ganadora FFE08A, con pista (halo dorado) | 180–186×222 u | `RoundedRect` + `ClayFrame` |
| Reactor (sombra, borde, cuerpo, núcleo, brillo) + número de carga | E | normal, logrado (núcleo dorado, ×1,11) | cuerpo Ø504 u, núcleo Ø432 u | `Disc` |
| Aro de 12 luces | D (animado) | cian → dorado | 19 u en radio 225 u | `Disc` |
| Botones + − × ÷ | I | reposo, elegido dorado, atenuado 45 % | 192×174 u | `RoundedRect` + `ClayFrame` |
| Deshacer / Empezar de nuevo | I | activo / 45 % | 450×156 u | igual |
| Puntitos de progreso (8), barra de tiempo | I | pendiente, actual, dorado, plata, gris | 21–30 u | `Disc`, `ProgressDots` |

No tiene `*Sprites.cs`: todo es `CalculoGameController.cs` con sprites genéricos. Riesgos: el número es texto sobre una zona lisa (contraste ≥ 4,5:1, hasta 5 cifras); con «quitar animaciones» la celda elegida solo cambia de color (darle forma: aro o hundida); los operadores «+» y «×» no deben leerse como cruz (brazos iguales); el aro de luces y el reactor son piezas separadas (las luces orbitan por código).

**Ficha de arte (Tarea 97, hecha el 14-oct):** celdas (crema, elegida cielo y hundida con «quitar animaciones», lograda sol), botones de operación (losa «noche»; la elegida, sol), «Deshacer» / «Empezar de nuevo» (losa apagada) y el panel del final salen de las losas del kit; el reactor trae 2 renders propios: el aro (azul noche) y su núcleo (gris claro, teñido por el código de azul noche a dorado). Se quedan procedurales la ventanita de la celda, el halo de la pista, las luces que giran, el resplandor y las chispas. Medidas y detalle: [`diseno-carga-exacta.md`](diseno-carga-exacta.md) §8.

### 11. Engranajes «Taller de reparación» (`engranajes`, `Games/Engranajes/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| Engranaje grande (15 dientes) y chico (10), 3 paletas (motor azul, camino lila, pieza dorado), con aro y 5 o 3 rayos | E | gira | Ø66 dp → 109–174 u; Ø46 dp → 76–121 u | no |
| Cubo, sombra, disco de apoyo oscuro | D | — | ≈0,36× punta | `Disc` |
| Eje, polea + correa (recta o cruzada, 3 capas), barra dentada | E | recta (óvalo) / cruzada (ocho), transición 0,32 s | polea Ø34 dp | no |
| Flecha del motor (arco con punta, cw/ccw), rótulo «MOTOR», carteles de pieza (ok menta + ✓, mal coral) | E / I | se da vuelta en 0,3 s | 158–253×63–100 u | no |
| Piezas del cohete (radar, turbina, portón, marco, caja), sala, casco | E / D | giran, suben, bajan; inactivas 45 % | radar 48×24 dp… | no |

Riesgos: **los dientes** (trapecio; punta ±0,17 y raíz ±0,32 del paso angular, vecinos con holgura cero: la punta del grande llega a la raíz del chico; ancho de punta ≈ 4,7 dp, hueco ≈ 4,1 dp): un bisel que ensanche el diente hace que se pisen (el de `pieza` también ESTRECHA la punta y rellena la raíz: se limitó a los cantos de arriba y de abajo); los huecos necesitan fondo oscuro; los rayos (5 y 3) y el degradé radial son lo que muestra el sentido de giro (no hacerlo perfectamente simétrico); cw/ccw se distinguen por la punta de la flecha (espejo); el cartel que falla no tiene ✗ (agregar marca); radar y turbina asimétricos (4 aspas = cruz); `Arrow`, `MiniArrow`, `Glyph`, `Wrench`, `Check` y `BeltCap` se tiñen. No tocar: giro, pulsos por las uniones, llama, despegue, transición de la correa.

**Volumen (Tarea 115, 16-oct):** el canto (la silueta más oscura corrida 4,5 dp HACIA ABAJO en la pantalla: no gira), la sombra de contacto, la cara mate con bisel isótropo (lo que gira no lleva luz horneada), el color por rol (motor azul, camino lila, pieza dorada con un mapa de color), las placas de los carteles y de MOTOR, el cohete de cilindro con remaches de bolita y luces con aro, y «Arrancar» y la cuenta de llaves como losas del kit. Ricardo aprobó la hoja de propuesta tal cual; detalle, medidas (hueco entre dientes con el canto detrás, costo de las capas) y decisiones en [`diseno-engranajes.md`](diseno-engranajes.md) §16. `Games/Shared/PlasticCanto` (el canto como efecto de malla) queda para los demás juegos.

### 12. Acoplamiento «muelle» (`acoplamiento`, `Games/Acoplamiento/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| Módulo (poliominó quiral de 4–7 bloques, ≤ 4×4) | E | **v3:** almohada de plasticina horneada por módulo con la lámpara fija en la pantalla (la mostrada, la alineada y la asentada) | bloque 38 dp (114 u; 96 px) | no |
| Hueco del puerto (cavidad + cordón) | E | cordón verde agua / lima (encajó) / celeste; el tubo es gris y se tiñe | misma escala | no |
| Panel del puerto, botones «Encaja» y «Espejo» y avisos | I | losas del kit (noche; lima y uva; chica dorada o crema), presionado y rebote | 245×214 y 158×76 dp | losas, `AnswerMark` |
| 4 chevrones, 8 luces de avance (+ medalla), 2 íconos | I | atlas `acoplamiento` (Blender) | 24, 30 y 54 dp | no |
| Escultura de la pantalla final (núcleo, 2 pistas, hasta 24 piezas chicas) | D | horneada al terminar la partida | 46 × 150 dp; piezas de 10 dp por bloque | no |

Riesgos: **la silueta ES la respuesta** (módulo y hueco salen del mismo SDF, radio de esquina 0,16 bloque): cualquier bisel o inflado que cambie una concavidad de 1 bloque (≈ 47 u) cambia la respuesta; hay ~83 siluetas quirales distintas: no se prerenderizan, hace falta un **bloque de plasticina con 16 variantes de vecindad (autotile)** compuesto en tiempo de ejecución; la luz fija en pantalla (el render no puede traerla horneada: gira con la pieza y delata el espejo); textura isótropa; remaches omitidos a K = 0,6; juntas que no formen «+»; la aspa de la insignia en diagonal.

**Estudio de la Fase A (Tarea 113, 16-oct; el juego no se tocó):** [`estudio-acoplamiento-plasticina.md`](estudio-acoplamiento-plasticina.md). Se compararon el camino 1 (cada bloque en 4 cuartos y 4 baldosas de Blender según los 3 vecinos) y el camino 2 (el contorno exacto del SDF y el relieve de una tabla 1-D medida en un borde recto) con 5 piezas reales. Los dos pasan la silueta (IoU ≥ 0,991 y desvío máximo ≤ 0,2 px con el bloque de 47 u; ≤ 0,55 px a 78 u) y no delatan el espejo (con la luz promediada en 4 giros y espejos); el camino 1 hornea 700 veces más rápido (0,55 ms contra ~330–380 ms en el Editor), el camino 2 es más simple y deja módulo y hueco en UNA sola fuente (el SDF). Hallazgos: las 166 siluetas (83 pares) caben en 4 baldosas (cero «pellizcos»); **el arte de hoy tiene un agujero de 2 × 2 px en el fondo del hueco en todo 2 × 2 de bloques (30 % de las siluetas)** porque el SDF queda en d = 0 donde se juntan 4 bloques (se arregla con una caja de 2 × 2 por cada 2 × 2); y `BakePiece` tarda ~380 ms en el Editor.

**Propuesta de volumen (Tarea 114, parte 2, 16-oct; solo una hoja, ningún juego se tocó):** [`previews/plasticina/propuesta-volumen.png`](previews/plasticina/propuesta-volumen.png). Ricardo y Opus pidieron que la plasticina de Engranajes y de Acoplamiento se vea con volumen para una persona sin ojo entrenado: cada objeto con un CANTO visible (su misma silueta, más oscura, corrida ~4,5 dp HACIA ABAJO en la pantalla, así que no gira ni delata un espejo), sombra de contacto, cara mate con un bisel marcado por la distancia al borde (sin dirección) y color por rampas (motor azul, camino lila, pieza dorada; el módulo, un celeste más cálido). La hoja trae «hoy | propuesta» en pantallas de teléfono y recortes al 100 % de las dos pantallas más sus medidas: la silueta no cambia (IoU 0,998 contra el SDF en Acoplamiento; en Engranajes cada objeto sale de su capa exacta), el espejo con el canto incluido no se delata (la pieza espejada en pantalla y la de celdas reflejadas dan lo mismo) y el hueco entre dientes se sigue leyendo (razón de contraste diente/hueco, mediana: arriba 8,2 → 5,9 : 1 y abajo, donde cae el canto, 9,5 → 3,1 : 1). Pendiente de Ricardo: si aprueba, siguen la Tarea 115 (Engranajes) y la 116 (Acoplamiento, Fase B). Scripts y receta: `tools/juegos/propuesta_*.py` (ver `tools/juegos/README.md`).

**Acoplamiento v2 (Tarea 115, parte 2, 16-oct; solo una hoja, ningún juego se tocó):** [`previews/plasticina/propuesta-acoplamiento-v2.png`](previews/plasticina/propuesta-acoplamiento-v2.png). Ricardo y Opus vieron la v1 y la encontraron todavía lisa y plana («plástico recortado»): perdía el encanto de hoy, los cuadritos y puntitos. La v2 pone una **bolita de plasticina en el centro de cada cuadrito** (un domo de 0,33 del bloque, sombreado ISÓTROPO: brillo centrado y borde parejo, ninguna luz que gire con la pieza y delate el espejo; su sombrita de contacto va en la pantalla, hacia abajo, como el canto) y un módulo más inflado (almohada: el sombreado sale de la distancia al borde y se oscurece hacia él), sin juntas ni «+»; el hueco del puerto queda como en la v1. Medido: la cara es exactamente la silueta de la v1 (IoU 0,998 contra el SDF), el espejo con bolitas y canto no se delata (diferencia < 1e-11 sobre 255), la bolita contra la cara 1,47–1,72 : 1 de luminancia (≥ 1,3) también con el bloque de 47 u (bolita de 31 u). Pendiente de Ricardo; si aprueba, sigue la Tarea 116 (integrar). Scripts: `tools/juegos/propuesta_acoplamiento.py` (`compone_modulo_v2`), `propuesta_acoplamiento_v2.py` (la hoja) y `propuesta_medidas.py aco2`.

**Acoplamiento v3 (Tarea 117, 17-oct; solo una hoja, ningún juego se tocó):** [`previews/plasticina/propuesta-acoplamiento-v3.png`](previews/plasticina/propuesta-acoplamiento-v3.png). Ricardo rechazó las bolitas de la v2 («rellenar por rellenar») y pidió el objeto mismo bien hecho, la casilla bien definida y **sacar la estación de la pantalla de juego**. La hoja propone: la pantalla sin estación con el bloque de 26 a **38 dp** (la pieza gira en un círculo de 215 dp, el hueco de 4 × 4 mide 152 dp; en 16:9, de 20 a 31 dp), la franja de avisos más arriba (y 118 a 150) y **una fila de 8 luces redondas de plasticina junto al HUD** como avance (el hito de cada 8: una onda lima, «¡Anillo completo!» y una medalla; la telemetría no cambia); el «plop» al encajar en 3 cuadros; la pieza como una **almohada de plasticina sin adornos** (la silueta del SDF desenfocada como relieve, canto de 5 dp hacia abajo, lámpara FIJA en la pantalla con brillo arriba a la izquierda y sombra abajo a la derecha, celeste cálido, grano fino); el puerto como una **cavidad** (cordón de plasticina verde agua / lima / celeste, pared de adentro con la lámpara, fondo oscuro, panel «noche» del kit, chevrones de churro) con el arreglo del alfiler del 2 × 2; «Encaja» y «Espejo» con la receta de VERDAD / DISPARATE; y la pantalla final con «tu estación» como escultura de las piezas que acopló la persona en anillos de 8 alrededor de un núcleo. **La luz se hornea en la orientación que se muestra** (el SDF se evalúa ya girado y espejado en pantalla): fuga del espejo 0 (menos de 1e-10 sobre 255) contra 56 px de brillo cambiado de lado si se hornea en el marco de la pieza; IoU de la silueta 0,997 o más contra el SDF del juego; contraste de la cara 9,6 : 1 contra el cielo (su filo más oscuro, 4,6 : 1); nada se pisa en 8 formas de teléfono. Scripts: `tools/juegos/acoplamiento_v3.py` (Blender: íconos, chevrones, luces), `acopla_v3.py` (pieza y puerto), `acopla_v3_pantalla.py` (las pantallas) y `propuesta_acoplamiento_v3.py` (la hoja y las medidas). **Pendiente de Ricardo:** que la apruebe; recién entonces la Fase B (implementar).

### 13. Aterrizaje Lunar (`aterrizaje`, `Games/Aterrizaje/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| Regla (relleno crema F3EBD3, borde tinta), marcas de extremo y del medio, números | E | con y sin marca del medio | 912×60 u; marcas 10×96 y 8×42 u | `RoundedRect` |
| Nave | E (su eje x es la respuesta) | inclina ±10°, salta al aparecer; llama y 2 luces parpadeantes | 270 u | no |
| Guía (40 puntos + flecha) | E (ayuda) | solo mientras baja | puntos 13 u | `Disc` |
| Bandera (asta, gallardete, etiqueta), tramo + insignia ✓/aspa | E (la verdad) | sube en 380–800 ms | asta 9×240 u; gallardete 108×72 | insignia propia |
| Luna, 2 cordilleras, Tierra, nebulosa, satélite, estrella fugaz | D | cordilleras a la deriva (se quitan con «quitar animaciones») | luna 1200×900 u | no |
| Cúpula del marcador + 5 puntos, avisos, barra | I | encendida/apagada | 92×66 u | `Disc` |

Riesgos: **la precisión de la regla**: acierto con error ≤ 5 % del largo (≈ 43 u), diana ≤ 1,2 % (≈ 10 u); las marcas de extremo (10 u) y el asta (9 u) centradas a ±1–3 u: un bisel que corra el centro visual cambia la medida; **nada decorativo junto a la regla** (sin marcas, rayas, grano, cráteres cerca); la nave simétrica respecto de su eje vertical (su centro x es lo que se mide); la luna y el satélite sin «+»; la aspa diagonal. No tocar: descenso, llama, inclinación, bandera, deriva.

**Ficha de arte (Tarea 111, 16-oct):** la **nave** (`tools/juegos/aterrizaje.py`) es el módulo lunar de siempre en relieves de plasticina (cabina crema con ventanilla celeste, base sol, cuatro patas) y se promedia con su espejo al empaquetarla: su centro en x es la respuesta. **La regla** es una losa crema lisa del kit (radio 7,5 dp: 20 dp de alto) que el controlador estira a 304 dp (nine-slice a lo ancho): SIN marcas, rayas, grano ni cráteres; las **marcas** de extremo y del medio, los **números** y el **asta** siguen siendo tinta procedural en el mismo x (medido antes y después sobre capturas: ≤ 0,5 unidades; prueba con y sin plasticina). El **gallardete**, la **Tierra** (lejos de la regla), el **satélite** (sin «+») y las **cúpulas** del marcador son de plasticina; la **luna** y las **cordilleras** (pegadas a la regla), la nebulosa, la estrella fugaz, la guía, la llama y los avisos se quedan procedurales. Nada se toca como objeto (el dedo mueve la nave desde cualquier parte): no hay estado presionado nuevo. Medidas, pruebas y pesos: [`diseno-aterrizaje.md`](diseno-aterrizaje.md) §12.

### 14. En la punta de la lengua (`anagramas`, `Games/Anagramas/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| Tarjeta de definición (texto de 16–20 dp, hasta 90 caracteres) | E | 1; las ondas se aclaran con cada ayuda | 960×492 u | `RoundedRect` |
| Ficha-letra (banco de 4 a 14; la letra la pone Unity) | E | normal F4F1FF, fija por ayuda F7DD94, encendida ×4 (oro, plata, cobre, azul), colocada (0,78 → 1) | 138 u (108–138) | `Disc`, `Ring`, `Glow` |
| Casilla (hueco) | I | 1; tiembla | 90–96 + 9 u | `Disc` |
| Lucero del cielo (fila de 8 a 16) | I (progreso) | vacío / lleno ×4 por color | núcleo 42 u, halo 126 | solo este |
| Botones («¡La tengo!», «Una ayuda», «Borrar», «Ver la palabra») | I | pulsado: baja 12 u y pop | 300–600×174 u | `RoundedRect` + `ClayFrame` |
| Escalera de ayudas (3 puntos), barra de tiempo, antena de la tarjeta, nebulosa | I / D | — | 33 u; 960×8 | `Disc`, `Ring` |

Es el único de los cuatro de Memoria/Atención/Razonamiento/Lenguaje sin arcilla real: todo son 4 sprites genéricos horneados. Riesgos: la letra oscura (25 dp) va sobre una cara casi blanca y lisa; con 14 fichas en 3 filas el grosor del aro no baja; texto tinta sobre cobre y azul ≥ 4,5:1; **los estados son solo color** (ficha fija vs normal, y los 4 luceros oro/plata/cobre/azul se distinguen solo por color, que ES el dato: darles marca de forma); núcleo del lucero de 42 u y puntos de 33 u: siluetas simples (la textura no se ve); la tarjeta es 9-slice con texto blanco sobre oscuro; los arcos de la antena no deben leerse como media luna.

**Ficha de arte (Tarea 101, hecha el 14-oct):** la ficha-letra (disco gris claro con cúpula que el código tiñe: crema, fija y los cuatro metales), la casilla (cuenco oscuro con aro celeste) y la perla del lucero del cielo (esfera gris clara teñida por el código) son renders propios; la tarjeta de la definición, los botones y el panel del final, losas del kit. Se quedan procedurales los resplandores, el lucero que vuela, las ondas de la antena y la escalera de ayudas. Medidas y detalle: [`diseno-punta-de-la-lengua.md`](diseno-punta-de-la-lengua.md) §9.

### 15. Lluvia de meteoros (`meteoros`, `Games/Meteoros/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| Roca-meteoro | E (soporta la palabra y se toca) | 4 formas × 5 tintes (lila, celeste, coral al azar; dorada = palabra real rara 1 de 12, con halo y aro; polvo gris con grietas = inventada tocada); brasa abajo a la derecha, nunca rota | ancho 410–960 u, alto 374 (432 mayores); **se estira sin conservar proporción** (1,1:1 a 2,6:1) | solo este |
| Placa crema con la palabra (Atkinson Bold 26 dp) | E | normal, tachada (barra coral de 10 u) | 240–590×117 u | `RoundedRect` + `ClayFrame` |
| Estela de calor | D | con o sin chispas; larga con la racha | 430–620 u | solo este |
| Estrella de la constelación (5, discos redondos) | I | encendida / apagada | 78 u | solo este |
| Cúpula del observatorio, superficie lunar, atmósfera | D | — | 330 u; 1080×326 | `LunarSurface` (compartida con Disparate) |

**Ficha de arte (Tarea 105, hecha el 14-oct):** la roca es de plasticina gris clara que el código tiñe, como NINE-SLICE (esquinas con cráteres fijas, centro liso que se estira); la roca presionada (con la huella en una esquina), las dos estrellas de la constelación (esferas) son renders propios; la brasa y las grietas, capas de numpy sobre el mismo lienzo; la placa de la palabra y el panel del final, losas del kit. Se queda procedural la estela de calor, el halo y el aro de la dorada. Medidas y detalle: [`diseno-lluvia-de-meteoros.md`](diseno-lluvia-de-meteoros.md) §14.

Riesgos: **la roca se estira**: un render fijo se deformaría (≥ 3 variantes de proporción o una roca estirable); el centro (la placa) sin detalle; texto tinta sobre crema ≥ 4,5:1; la dorada es SIEMPRE palabra real (pista que está en el diseño: mantenerla inconfundible y que el color de las demás no sea información); el error se lee sin color: grietas + ✗ + tachado + texto (conservar las grietas); luz arriba a la izquierda y las rocas nunca giran; `ObservatorySprite.BaseFraction` y `_atmosphereY` dependen de la cúpula; `LunarSurfaceSprite.Get(color)` guarda un solo sprite.

### 16. ¿Verdad o disparate? (`disparate`, `Games/Disparate/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| Placa de la transmisión (frase de 1 a 3 renglones, Atkinson 24 sp) | E | crema; acierto: brillo lima + ✓; error: brillo coral + ✗ + corrección; señal baja: tiembla y se inclina hasta ~26° | 420–960×186–360 u | `RoundedRect` + `ClayFrame` |
| Botones VERDAD (lima) y DISPARATE (coral) con íconos tinta (visto, ola rota) | I | pulsado | 462×307 u; ícono 120 u | íconos solo este |
| Antena | D (ancla de los efectos) | la luz de la punta pulsa, más fuerte con racha ≥ 10 | 420×420 u | solo este |
| Medidor de señal (5 barras), barra de tiempo | I | encendida (sol) / apagada | 34×(50–154) u | `RoundedRect` |
| Marca ✓ / ✗ | I | — | 104 u | `AnswerMark` |

Riesgos: ancho y alto variables → 9-slice; la inclinación y el temblor giran la luz horneada; las anclas de la antena son fracciones fijas del sprite (`AntennaDish` (0,5; 0,825), `AntennaTip` (0,5; 0,925), `AntennaFloor` ≈ 0,0875): si el modelo cambia hay que actualizarlas; remaches de 0,02 se pierden a 420 u; el medidor cambia solo de color y brillo (darle forma); el plato es un semicírculo con borde elíptico (que no se lea como media luna).

**Ficha de arte (Tarea 97, hecha el 14-oct):** la placa, los botones VERDAD (lima) y DISPARATE (coral) y el panel del final salen de las losas del kit; la antena **parabólica** (mismo lienzo ±1,2 y `AntennaFloor`; `AntennaTip` y `AntennaDish` se movieron: el plato mira a la derecha), los íconos ✓ y onda cortada en dos tramos (churros de tinta) y las barras del medidor (nine-slice vertical) son renders propios. **Corrección del 14-oct:** la antena se leía como copa o cáliz y la onda como dos manchas; detalle en [`diseno-verdad-o-disparate.md`](diseno-verdad-o-disparate.md) §11. Se quedan procedurales la cinta de luz, las auroras, las ondas, el resplandor de la punta, la estática y las barras de señal y de tiempo. Medidas y detalle: [`diseno-verdad-o-disparate.md`](diseno-verdad-o-disparate.md) §11.

### 17. La estrella intrusa (`intrusa`, `Games/Intrusa/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| Estrella-palabra ×5 (4 anclas + intrusa): disco crema + halo + destello redondo + aro | E | normal, latiendo, tocada por error (aro coral + ✗), intrusa revelada (aro sol que late), cae como estrella fugaz | núcleo 21 u, halo 132, destello 72, aro 102 | `Disc`, `Glow`, `Ring`, `AnswerMark` |
| Placa de palabra ×5 (Atkinson 17 sp) | E | crema | 156–414×90 u | `RoundedRect` |
| Opciones «¿Qué las une?» ×3 y botón «Seguir» | I | elegida correcta (lima + ✓), errónea (coral + ✗), la correcta no elegida se tiñe lima (sin marca) | 328–900×192 u | `RoundedRect` |
| Figura de la recompensa (77 figuras: líneas en 3 pasadas, estrellas menores, grabado dorado con sombreado) | D (recompensa) | espejada al azar y girada ±12° | hasta ~890×830 u | **no son sprites** (puntos y aristas por ronda) |
| Cometa del tiempo, línea punteada de trampa | I / efecto | — | — | `Disc` |

Riesgos: **77 figuras no se modelan**: el grabado se queda procedural (la plasticina, como mucho, para la lámina del atlas); el espejado del 50 % de las rondas invertiría la luz horneada; el núcleo de 21 u es un punto luminoso con halo (la textura no se ve) y debe resaltar sobre las ~230 estrellas del fondo; las placas no pueden crecer (3 dp de aire); la correcta no elegida solo se tiñe (agregar marca); nunca puntas ni cruz de luz. A revisar por silueta si algún día se modelan: Las Tijeras (aspa), El Martillo (T), La Llave, La Bicicleta, El Arcoíris (media luna), La Paloma, El Velero, El Planeta con Anillos.

**Ficha de arte (Tarea 101, hecha el 14-oct):** sin renders propios: las placas de las palabras (losa chica crema), las 3 opciones de «¿Qué las une?» (crema; lima al acertar, coral al fallar; la correcta no elegida, lima y sin marca), «Seguir» y el panel del final salen de las losas del kit. Se quedan procedurales las estrellas-palabra (luz), el grabado, las 77 figuras y el cometa. Medidas y detalle: [`diseno-estrella-intrusa.md`](diseno-estrella-intrusa.md) §10.

## Peso al APK

Meta: **menos de ~1,5 MB por juego** sumados al APK. El atlas va sin compresión de textura (la plasticina tiene degradés suaves); entre paréntesis, lo que pesaría con ASTC 6×6. Cómo se mide y se baja: [`tools/juegos/README.md`](../tools/juegos/README.md) § Peso. `PlasticArtTests` falla si un grupo pasa del tope.

| Grupo | Sprites | Atlas | PNG en disco | En el APK (estimado) | Con ASTC 6×6 |
|---|---:|---|---:|---:|---:|
| `comun` (losas del kit y «noche», marcas ✓ ✗, anillo de la cuenta): se paga UNA vez para los 17 juegos | 29 | 896×2472 | 849 KB | 1,11 MB | 0,97 MB |
| `nubi` (la Nubi 3D de los tutoriales) | 1 | 768×552 | 196 KB | 0,24 MB | 0,18 MB |
| `radar` (Rescate relámpago) | 16 | 1024×1776 | 790 KB | **1,11 MB** | 0,77 MB |
| `piloto` (Piloto Estelar: 15 señales a 192 y 96 px, 15 presionadas, nave) | 46 | 1664×884 | 912 KB | **1,21 MB** | 0,64 MB |
| `freno` (Freno de Emergencia: 2 botones y sus 2 presionados, 3 cohetes, plataforma, ALTO) | 9 | 1408×664 | 473 KB | **0,66 MB** | 0,41 MB |
| `calculo` (Carga exacta: aro y núcleo del reactor) | 2 | 1152×556 | 242 KB | **0,32 MB** | 0,27 MB |
| `disparate` (¿Verdad o disparate?: antena, 2 íconos, 2 barras del medidor) | 5 | 512×574 | 125 KB | **0,16 MB** | 0,13 MB |
| `satelites` (Satélites: satélite y su presionado, planeta, sobre, nube de polvo) | 5 | 1024×712 | 204 KB | **0,25 MB** | 0,32 MB |
| `rastro` (Rastro de luz: 9 luceros + 9 presionados) | 18 | 1792×392 | 444 KB | **0,83 MB** | 0,31 MB |
| `stroop` (Tinta o Palabra: 15 losas con el color de la tinta, la tarjeta y las orillas; gota y libro) | 17 | 1536×708 | 547 KB | **0,77 MB** | 0,46 MB |
| `punta` (En la punta de la lengua: ficha y su presionada, casilla, perla) | 4 | 640×196 | 75 KB | **0,10 MB** | 0,06 MB |
| `meteoros` (Lluvia de meteoros: 4 rocas nine-slice, 4 presionadas, brasa, grietas, 2 estrellas) | 18 | 640×1956 | 386 KB | **0,52 MB** | 0,55 MB |
| `parejas` (Constelaciones: 12 objetos + 4 gemelos, luz dormida, dormida presionada y abierta) | 19 | 1152×940 | 431 KB | **0,55 MB** | 0,46 MB |
| `bodega` (Bodega de carga: 12 objetos, marco, aro, 3 interiores, 3 puertas, robot, caja, esclusa, carro) | 30 | 1024×1384 | 684 KB | **0,89 MB** | 0,60 MB |
| `correo` (La estación de correo: 12 cartas, 4 planetas, dial, puerta, manija, torre, 2 lámparas, nave, saco, raya, reloj) | 28 | 1280×1852 | 1140 KB | **1,49 MB** | 1,01 MB |
| `aterrizaje` (Aterrizaje Lunar: nave, regla nine-slice, gallardete, Tierra, satélite, 2 cúpulas) | 7 | 512×780 | 152 KB | **0,21 MB** | 0,17 MB |
| `engranajes` (Engranajes: 6 engranajes + presionado, radar, turbina, polea + presionada, portón, marco, plataforma, caja, barra, casco, 6 flechas, 3 triángulos; desde la Tarea 115 + domo y moneda; desde la 116, 3 losas chicas del kit, el triángulo de «Arrancar» y 2 monedas de llave) | 35 | 768×2890 | 1115 KB | **1,18 MB** | 0,94 MB |
| `acoplamiento` (Acoplamiento v3: 2 íconos de botón, 4 chevrones, 3 luces; la pieza, el hueco y la escultura se hornean por módulo, sin atlas) | 9 | 512×232 | 72 KB | **0,09 MB** | 0,05 MB |

Lo nuevo en el APK con Rescate, Piloto, Freno, Carga exacta, ¿Verdad o disparate?, Satélites, Lluvia de meteoros, Constelaciones, Bodega de carga, La estación de correo, Aterrizaje Lunar, Engranajes, Rastro de luz, Tinta o Palabra, En la punta de la lengua y La estrella intrusa (radar + piloto + freno + calculo + disparate + satelites + rastro + stroop + punta + meteoros + parejas + bodega + correo + aterrizaje + engranajes + comun + nubi) son unos 11,5 MB sin comprimir la textura y unos 8,3 MB con ASTC; cada juego que se migre suma solo su atlas (≤ 1,5 MB).

## Estado presionado (regla del 14-oct): cómo quedó cada juego (Tarea 104)

La regla (arriba, «Reglas que valen para todos»): todo objeto de plasticina que se toca tiene su estado presionado con textura. Quedó hecha para los juegos ya migrados; **los que falten migrar nacen con ella**. La ayuda común para los juegos que leen `Input` directo es
[`PlasticTouch`](../unity/NeuroVidaCore/Assets/Scripts/Games/Shared/PlasticTouch.cs): el juego la llama cuando ACEPTA un toque (`Press()`), y la losa pasa a su imagen presionada (`_p`) mientras el dedo siga encima y al menos 80 ms, el texto baja con ella, y al soltar vuelve con un
rebote chico de escala (con «quitar animaciones» solo cambia la imagen). Dos modos: dueña de la losa (Disparate, Intrusa, Punta) o solo lleva la cuenta (`Down`, `Drop`, `Bounce`) para los juegos que ya redibujan la losa en cada cuadro (Rescate, Carga). Las losas del kit traen su `_p`
(más baja, más oscura, sombra más chica); la de las herramientas de Carga («apagada») es nueva en `comun`. Los objetos propios traen su render presionado: la pieza **aplastada** (~8 % más baja y ~4 % más ancha), **un punto más oscura** (×0,93 como las losas) y con la **huella** de un dedo (un óvalo
suave y poco profundo con crestas finas), en el mismo lienzo y la misma escala, sin «saltar» de lugar (`aplasta`, `pieza_huella` y `espacio_libre` de `tools/juegos/plasticina.py`).

| Juego | Qué se ve presionado | Cómo |
|---|---|---|
| Rescate relámpago | las 6 casillas de respuesta y «¡Rescatar!» | `PlasticTouch` (modo cuenta): losa `_p` al tocar, además de la «elegida» (hundida); sin el hundir de 3 dp de siempre |
| Piloto Estelar | la señal que la persona toca (atrapada o falsa alarma) | las 15 `senal_p_*` (a 192 px): se ve presionada mientras se va; las que se van solas, no. **La huella va en el espacio libre más grande entre el detalle de tinta y el borde** (el detalle es el estímulo: no se pisa) |
| Freno de Emergencia | el botón del carril | `boton_encendido_p` y `boton_apagado_p`: ≥ 0,1 s y rebote chico (el 90 % de escala de siempre solo sin atlas) |
| Carga exacta | celdas, opciones y herramientas | `PlasticTouch` (modo cuenta): losa `_p` al tocar con o sin animaciones; `losa_md_apagada_p` nueva |
| ¿Verdad o disparate? | VERDAD y DISPARATE | `PlasticTouch` en lugar del `PopRect` (el ícono y el texto bajan con la losa) |
| Satélites | el satélite al marcarlo o desmarcarlo | `satelite_p`: ≥ 0,1 s y rebote chico, junto al aro de marca de siempre |
| En la punta de la lengua | las fichas-letra y los dos botones | `ficha_p` (gris claro: el código la tiñe como a la normal) y `PlasticTouch` en los botones |
| La estrella intrusa | las 3 opciones y «Seguir» | `PlasticTouch`: la opción tocada se ve presionada (crema, y al acertar o fallar, lima o coral presionada); «Seguir» se ve presionado 0,1 s antes de irse |
| Rastro de luz (Tarea 103) | los luceros al tocarlos | `lucero_p_0..8` |
| Tinta o Palabra | los botones | `PlasticPress` con `losa_md_boton_*_p` desde la Tarea 100 |
| Engranajes (Tarea 112) | el engranaje del motor, la correa tocada (sus dos poleas) y «Arrancar» | `gear_*_0_p` (más oscuro y más plano; **sin huella**: un engranaje gira y una huella fuera del centro giraría con él; el contorno de los dientes no cambia), `polea_p` y `PlasticTouch` (modo cuenta) en la losa Sky de «Arrancar»; ≥ 80 ms, también con el toque guiado del tutorial |

**Medido** (`tools/juegos/mide_presionados.py`, informe en `tools/juegos/out/presionados_medidas.md`; pruebas `PressedSpritesTests`, `PlasticTouchTests` y las de cada juego): la pieza presionada es 0,5 a 4 % más ancha (las señales, hasta 2,8 %: la gota y el triángulo llegan casi al borde del lienzo y no pueden crecer más), el centro
se corre 2,5 px como máximo (no «salta») y queda a 4 px o más del borde del lienzo (no se recorta); queda a 0,83-0,91 de la luminosidad de la normal (ΔE 3,2 a 7,0: un punto más oscura sin cambiar de color: el color de los luceros, los metales y las señales sigue siendo el dato); la tinta del detalle de las
señales se conserva (≥ 104 %); la letra de tinta sobre la ficha presionada de Punta rinde 11,9 (normal), 10,0 (fija), 8,9 (oro), 9,8 (plata), 5,9 (cobre) y 5,8:1 (azul). **La huella se ve** (el detalle fino de la superficie en su zona pasa de 0,5-2,6 a 3,0-12,6), pero es **chica donde el espacio libre es chico**: en las señales,
en el botón y en el satélite mide unos 0,3 de diámetro (en el botón apagado, azul oscuro, se ve poco). Hoja con un cuadro normal y uno presionado de cada objeto, armada con los sprites reales (la herramienta de capturas no puede simular un dedo apoyado): [`previews/plasticina/presionados.png`](previews/plasticina/presionados.png)
(`tools/juegos/hoja_presionados.py`).

Peso (Tarea 104): `piloto` pasa de 0,58 a 1,21 MB (las 15 señales presionadas; 0,64 MB con ASTC), `freno` de 0,50 a 0,66 MB, `satelites` de 0,23 a 0,25 MB, `punta` de 0,06 a 0,10 MB y `comun` de 1,05 a 1,11 MB (la losa apagada presionada); todos bajo el tope de ~1,5 MB por juego.

## El kit sobre el cielo navy (Tarea 129, 18-oct-2026)

La lección del 16-oct aplicada a los juegos: la plasticina se nota en superficies claras y gruesas; una losa noche sobre un cielo del mismo azul no deja ver ni la sombra ni el canto. Reglas del kit (spec de Fable §1.2): **R1** noche solo para casillas de respuesta con un símbolo de color calibrado sobre oscuro; **R2** paneles de información en crema con la letra en tinta (`NeuroStyle.Ink`, rótulos secundarios en `NeuroStyle.InkMuted` #514B78, acento azul `NeuroStyle.PanelAccent` #2B57B8); **R3** botones secundarios en lila (el principal sigue cielo, sol o lima; «apagada» solo para lo que de verdad no se puede); **R4** un símbolo calibrado sobre oscuro dentro de una tarjeta crema va en su ventanilla noche. Hoja: `docs/previews/kit-lo-que-falta.png` (`tools/previews/kit_lo_que_falta.py`).
