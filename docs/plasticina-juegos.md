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
| 5 | Tinta o Palabra | `stroop` | 3 (+ botones) | inventario | 14-oct | — |
| 6 | Constelaciones | `parejas` | 20 | inventario | 14-oct | — |
| 7 | Bodega de carga | `bodega` | 24 | inventario | 14-oct | — |
| 8 | La estación de correo | `correo` | 27 | inventario | 14-oct | — |
| 9 | Rastro de luz | `secuencia` | 9 luceros; el resto, losas del kit | integrado (14-oct); **falta que Ricardo lo pruebe** | 14-oct | [antes](previews/plasticina/antes-secuencia.png) → [después](previews/plasticina/despues-secuencia.png) |
| 10 | Carga exacta | `calculo` | 2 propios (aro y núcleo del reactor); el resto, losas del kit | integrado (14-oct); **falta que Ricardo lo pruebe** | 14-oct | [antes](previews/plasticina/antes-calculo-arte.png) → [después](previews/plasticina/despues-calculo-arte.png) |
| 11 | Engranajes | `engranajes` | 33 | inventario | 14-oct | — |
| 12 | Acoplamiento | `acoplamiento` | 13 + módulos | inventario | 14-oct | — |
| 13 | Aterrizaje Lunar | `aterrizaje` | 14 | inventario | 14-oct | — |
| 14 | En la punta de la lengua | `anagramas` | 0 propios (fichas, casillas, lucero) | inventario | 14-oct | — |
| 15 | Lluvia de meteoros | `meteoros` | 24 | inventario | 14-oct | — |
| 16 | ¿Verdad o disparate? | `disparate` | 5 (antena, 2 íconos, 2 barras del medidor); el resto, losas del kit | integrado (14-oct); **falta que Ricardo lo pruebe** | 14-oct | [antes](previews/plasticina/antes-disparate.png) → [después](previews/plasticina/despues-disparate.png) |
| 17 | La estrella intrusa | `intrusa` | 2 (las 77 figuras se quedan procedurales) | inventario | 14-oct | — |

(El orden de los números 2 a 17 es el **propuesto** más abajo; Opus decide.)

## Lo que NO entró en la Tarea 94 (a decidir en las siguientes)

- **Decorado de Rescate (y de todos los juegos):** el planeta lejano, la estación accidentada, los restos, el polvo y el eje lima del centro del radar siguen planos (procedurales). **Queda para UNA pasada final de decorados, cuando estén todos los juegos migrados** (decisión de Opus, 14-oct). El barrido, la estática, el rayo tractor, las llamas y los aros de la revelación se quedan como efecto (son animados o se tiñen).
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
| Paneles de pausa y de final | `PauseMenu`, `GameControllerBase`, `RadarGameController.BuildEnd` | `RoundedRect(64)` azul noche + `ClayFrame` | losa `gr` crema |
| Cuenta regresiva (título, anillo) | `CountdownScreen` | píldora de 3 capas, `RingSprite` | losa `ch` crema; anillo de plasticina |
| Cierre «¡Listo!» | `FinishCurtain` | texto con contorno tinta | placa `gr` sol detrás del texto |
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

El cohete de Freno ya NO usa `SymbolSprite.Rocket` (`BrakeSprites.Rocket(carril)`): las cartas de Constelaciones siguen con el suyo hasta que le toque a ese juego (fila 6). Medidas, pruebas y pesos: [`juegos/freno.md`](juegos/freno.md) § Plasticina.

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

Riesgos: **los dientes** (trapecio; punta ±0,17 y raíz ±0,32 del paso angular, vecinos con holgura cero: la punta del grande llega a la raíz del chico; ancho de punta ≈ 4,7 dp, hueco ≈ 4,1 dp): un bisel que ensanche el diente hace que se pisen; los huecos necesitan fondo oscuro; los rayos (5 y 3) y el degradé radial son lo que muestra el sentido de giro (no hacerlo perfectamente simétrico); cw/ccw se distinguen por la punta de la flecha (espejo); el cartel que falla no tiene ✗ (agregar marca); radar y turbina asimétricos (4 aspas = cruz); `Arrow`, `MiniArrow`, `Glyph`, `Wrench`, `Check` y `BeltCap` se tiñen. No tocar: giro, pulsos por las uniones, llama, despegue, transición de la correa.

### 12. Acoplamiento «muelle» (`acoplamiento`, `Games/Acoplamiento/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| Módulo (poliominó quiral de 4–7 bloques, ≤ 4×4) | E | gira 0–180° (45° o 15°), espejo = escala x −1; la luz fija va aparte | bloque 47–78 u, pieza 187–312 u; textura 384 px | no |
| Hueco del puerto (Hole + Edge) | E | borde teal / lima (encajó) / celeste | misma escala | no |
| Panel del puerto, 4 chevrones, botones «Encaja» y «Espejo» con íconos | I | atenuado 45 %, hundido, insignia ✓/✗ | 285–474×137–228 u | `AnswerMark` |
| Anillos de la estación, casilleros (8 por anillo, hasta 40) | D / progreso | vacío / celeste / lima | 816×228 u; 89–108 u | no |

Riesgos: **la silueta ES la respuesta** (módulo y hueco salen del mismo SDF, radio de esquina 0,16 bloque): cualquier bisel o inflado que cambie una concavidad de 1 bloque (≈ 47 u) cambia la respuesta; hay ~83 siluetas quirales distintas: no se prerenderizan, hace falta un **bloque de plasticina con 16 variantes de vecindad (autotile)** compuesto en tiempo de ejecución; la luz fija en pantalla (el render no puede traerla horneada: gira con la pieza y delata el espejo); textura isótropa; remaches omitidos a K = 0,6; juntas que no formen «+»; la aspa de la insignia en diagonal.

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

### 15. Lluvia de meteoros (`meteoros`, `Games/Meteoros/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| Roca-meteoro | E (soporta la palabra y se toca) | 4 formas × 5 tintes (lila, celeste, coral al azar; dorada = palabra real rara 1 de 12, con halo y aro; polvo gris con grietas = inventada tocada); brasa abajo a la derecha, nunca rota | ancho 410–960 u, alto 374 (432 mayores); **se estira sin conservar proporción** (1,1:1 a 2,6:1) | solo este |
| Placa crema con la palabra (Atkinson Bold 26 dp) | E | normal, tachada (barra coral de 10 u) | 240–590×117 u | `RoundedRect` + `ClayFrame` |
| Estela de calor | D | con o sin chispas; larga con la racha | 430–620 u | solo este |
| Estrella de la constelación (5, discos redondos) | I | encendida / apagada | 78 u | solo este |
| Cúpula del observatorio, superficie lunar, atmósfera | D | — | 330 u; 1080×326 | `LunarSurface` (compartida con Disparate) |

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

**Ficha de arte (Tarea 97, hecha el 14-oct):** la placa, los botones VERDAD (lima) y DISPARATE (coral) y el panel del final salen de las losas del kit; la antena (mismo lienzo ±1,2: `AntennaTip`, `AntennaDish` y `AntennaFloor` valen igual), los íconos ✓ y onda rota (churros de tinta) y las barras del medidor (nine-slice vertical) son renders propios. Se quedan procedurales la cinta de luz, las auroras, las ondas, el resplandor de la punta, la estática y las barras de señal y de tiempo. Medidas y detalle: [`diseno-verdad-o-disparate.md`](diseno-verdad-o-disparate.md) §11.

### 17. La estrella intrusa (`intrusa`, `Games/Intrusa/`)

| Objeto | Clase | Estados | Tamaño | Común |
|---|---|---|---|---|
| Estrella-palabra ×5 (4 anclas + intrusa): disco crema + halo + destello redondo + aro | E | normal, latiendo, tocada por error (aro coral + ✗), intrusa revelada (aro sol que late), cae como estrella fugaz | núcleo 21 u, halo 132, destello 72, aro 102 | `Disc`, `Glow`, `Ring`, `AnswerMark` |
| Placa de palabra ×5 (Atkinson 17 sp) | E | crema | 156–414×90 u | `RoundedRect` |
| Opciones «¿Qué las une?» ×3 y botón «Seguir» | I | elegida correcta (lima + ✓), errónea (coral + ✗), la correcta no elegida se tiñe lima (sin marca) | 328–900×192 u | `RoundedRect` |
| Figura de la recompensa (77 figuras: líneas en 3 pasadas, estrellas menores, grabado dorado con sombreado) | D (recompensa) | espejada al azar y girada ±12° | hasta ~890×830 u | **no son sprites** (puntos y aristas por ronda) |
| Cometa del tiempo, línea punteada de trampa | I / efecto | — | — | `Disc` |

Riesgos: **77 figuras no se modelan**: el grabado se queda procedural (la plasticina, como mucho, para la lámina del atlas); el espejado del 50 % de las rondas invertiría la luz horneada; el núcleo de 21 u es un punto luminoso con halo (la textura no se ve) y debe resaltar sobre las ~230 estrellas del fondo; las placas no pueden crecer (3 dp de aire); la correcta no elegida solo se tiñe (agregar marca); nunca puntas ni cruz de luz. A revisar por silueta si algún día se modelan: Las Tijeras (aspa), El Martillo (T), La Llave, La Bicicleta, El Arcoíris (media luna), La Paloma, El Velero, El Planeta con Anillos.

## Peso al APK

Meta: **menos de ~1,5 MB por juego** sumados al APK. El atlas va sin compresión de textura (la plasticina tiene degradés suaves); entre paréntesis, lo que pesaría con ASTC 6×6. Cómo se mide y se baja: [`tools/juegos/README.md`](../tools/juegos/README.md) § Peso. `PlasticArtTests` falla si un grupo pasa del tope.

| Grupo | Sprites | Atlas | PNG en disco | En el APK (estimado) | Con ASTC 6×6 |
|---|---:|---|---:|---:|---:|
| `comun` (losas del kit y «noche», marcas ✓ ✗, anillo de la cuenta): se paga UNA vez para los 17 juegos | 28 | 896×2292 | 815 KB | 1,05 MB | 0,87 MB |
| `nubi` (la Nubi 3D de los tutoriales) | 1 | 768×552 | 196 KB | 0,24 MB | 0,18 MB |
| `radar` (Rescate relámpago) | 16 | 1024×1776 | 790 KB | **1,11 MB** | 0,77 MB |
| `piloto` (Piloto Estelar: 15 señales a 192 y 96 px, nave) | 31 | 1664×492 | 472 KB | **0,58 MB** | 0,35 MB |
| `freno` (Freno de Emergencia: 2 botones, 3 cohetes, plataforma, ALTO) | 7 | 1152×664 | 369 KB | **0,50 MB** | 0,33 MB |
| `calculo` (Carga exacta: aro y núcleo del reactor) | 2 | 1152×556 | 242 KB | **0,32 MB** | 0,27 MB |
| `disparate` (¿Verdad o disparate?: antena, 2 íconos, 2 barras del medidor) | 5 | 512×574 | 125 KB | **0,16 MB** | 0,13 MB |
| `satelites` (Satélites: satélite, planeta, sobre, nube de polvo) | 4 | 1408×516 | 193 KB | **0,23 MB** | 0,31 MB |
| `rastro` (Rastro de luz: los 9 luceros) | 9 | 1792×196 | 208 KB | **0,39 MB** | 0,15 MB |

Lo nuevo en el APK con Rescate, Piloto, Freno, Carga exacta, ¿Verdad o disparate?, Satélites y Rastro de luz (radar + piloto + freno + calculo + disparate + satelites + rastro + comun + nubi) son unos 4,7 MB sin comprimir la textura y unos 3,4 MB con ASTC; cada juego que se migre suma solo su atlas (≤ 1,5 MB).
