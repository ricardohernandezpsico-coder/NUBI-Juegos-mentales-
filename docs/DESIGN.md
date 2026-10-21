# Nubi — Style Reference

> Noche y plasticina: un cielo índigo casi negro con luceros, y encima objetos de plasticina de verdad (renderizados), nunca imitados.

Theme: dark · Fuente de verdad: el código (ver §11) · Para: Sonnet, la landing y cualquier herramienta que lea Markdown. Escrito a mano desde el código el 18-oct-2026 (Tarea 131); `DesignMdTest` lo vigila en lo medible.

## 0 · Cómo usar este archivo (3 líneas)

1. Dice cómo se ve y cómo suena Nubi; manda el código (§11): si un color, una duración o un radio cambia allá, cambia aquí, y `DesignMdTest` falla si no coinciden.
2. Lo medible va en tablas con el token en acento grave (`Clay.Sun`, `NubiMotion.TAB_MS`, `kit.json losas.md.radio_dp`); lo demás son reglas (§8, Do / Don't) y roles.
3. Una pantalla nueva se arma con lo de §9 y respeta §8 y §10; la web, con §12. Nada de aquí autoriza a copiar arte, nombres ni sonidos de otras marcas.

## 1 · Voz y vocabulario

- **Tuteo, español cercano, frases cortas.** Nubi dice lo que hace, no lo que promete: «Al terminar ves tu cifra de hoy», no «Mejora tu memoria en 30 días».
- **El vocabulario de la app:** juego, partida, camino, tu avance, áreas, mente activa. En los juegos, «seguidos» (el contador de aciertos); «racha» es solo la de días, y la práctica de los tutoriales es «Práctica (sin puntos)».
- prohibidas: cognitivo, entrenamiento, cerebro, mejora tu, previene, percentiles, dominio
- **Sin culpa ni pérdida (A7):** Nubi no usa el miedo a perder para que vuelvas. La racha perdona (un día de descanso por semana no la corta) y espera. Lo que sí se dice: «Descansaste el martes: tu racha sigue», «no pierdes nada si dudas». La guardia vive en `VisibleTextsGuardTest` y `ReminderContentTest` (lista en `NoLossGuard`).
- **Sin promesas de salud ni diagnóstico:** lo que mide una partida es de esa partida; no se comparan personas ni se muestran percentiles. El aviso de salud va completo en Ajustes y en «Licencias y créditos».
- **Un solo nombre por juego:** el de `GameRegistry` (el mismo en la app, en Unity y en los avisos).
- Una exclamación por pantalla, como mucho. Sin mayúsculas sostenidas.

## 2 · Colores

Todos los colores de la app salen de `Clay` (los del kit) o de `NubiTokens` (el resto); no hay una tercera fuente. Contrastes medidos con la fórmula WCAG.

| Nombre | Hex | Token | Rol | Dónde NO |
|---|---|---|---|---|
| Tinta | #1A1240 | `Clay.Ink` | borde grueso de lo tocable; letra sobre crema y sobre sol (14,8:1 y 11,4:1) | letra sobre el cielo |
| Coral | #FF6B4A | `Clay.Coral` | acento cálido, losa coral | texto fino |
| Sol | #FFC93C | `Clay.Sun` | acción principal, hitos, churro bajo los títulos, farola encendida, texto sobre el cielo (12,2:1) | texto sobre crema (1,3:1) |
| Cielo | #4CC9F0 | `Clay.Sky` | acción principal alternativa, losa celeste | texto sobre crema |
| Uva (lila) | #B8A4FF | `Clay.Grape` | botones secundarios (R3) | acción principal |
| Lima | #9BE564 | `Clay.Lime` | acierto, «hecho», acción principal alternativa | texto sobre crema |
| Blanco | #FFFFFF | `Clay.Cream` | superficie clara y texto de los números sobre el cielo | borde |
| Tinta suave | #6B6790 | `Clay.InkSoft` | rótulos que acompañan, sobre crema (4,5:1: el mínimo) | texto de cuerpo |
| Tinta apagada | #514B78 | `Clay.InkMuted` | texto secundario sobre crema (6,8:1) | sobre el cielo |
| Sobre el cielo | #EAF0FF | `NubiTokens.OnNight` | títulos y cuerpo sobre el cielo (16,4:1) | sobre crema |
| Sobre el cielo, secundario | #C7D0FF | `NubiTokens.OnNightDim` | texto secundario sobre el cielo (12,4:1); un solo tono en toda la app | sobre crema |
| Memoria | #3B82F6 | `NubiTokens.Memoria` | color del área | texto sobre el cielo (se usa `NubiTokens.onSky`, aclarado 45 %) |
| Atención | #F59E0B | `NubiTokens.Atencion` | color del área | texto sobre el cielo (ídem) |
| Razonamiento | #8B5CF6 | `NubiTokens.Razonamiento` | color del área | texto sobre el cielo (ídem) |
| Lenguaje | #10B981 | `NubiTokens.Lenguaje` | color del área | texto sobre el cielo (ídem) |
| Marca | #5B9BFF | `NubiTokens.Brand` | azul de la marca, gráficos y Ajustes | losas del kit |
| Acierto | #10B981 | `NubiTokens.Success` | mejora, acierto (siempre con forma o texto) | señal única |
| Neutro | #475569 | `NubiTokens.Neutral` | «sin cambio» | acentos |
| Lámina del fondo | #0E0D33 | `BoardNavy` | el material del cielo «Espacio profundo» | superficie de lectura |

Losas del kit (son imágenes renderizadas, no tintes; los colores de `kit.json`):

| Nombre | Hex | Token | Rol | Dónde NO |
|---|---|---|---|---|
| Losa sol | #FFCB45 | `kit.json colores.sol` | botón principal, píldoras | texto fino |
| Losa crema | #F5EBDB | `kit.json colores.crema` | información: tarjetas, fichas, resultados, globo de Nubi | letra en sol |
| Losa lima | #9BE564 | `kit.json colores.lima` | acierto, «hecho» | — |
| Losa cielo | #4CC9F0 | `kit.json colores.cielo` | acción alternativa | — |
| Losa coral | #FF6B4A | `kit.json colores.coral` | acento cálido | — |
| Losa lila | #B8A4FF | `kit.json colores.lila` | secundarios | — |

El cielo: tres tonos en vertical, `#07071F` arriba, `#100F3A` al medio y `#1C1B52` abajo (`CosmosBackground`; en Unity, `NeuroStyle.NightTop/NightMid/NightBottom`). Mediana medida: `#151539`.

## 3 · Tipografía

- **Dos familias, elegidas por el peso (`AppFamily`):** Fredoka 600-700 para títulos, botones y números; Nunito 400-500 para el cuerpo. Una palabra nunca se parte: si no cabe, pasa a 2 líneas, se achica el ícono o se usan menos columnas.
- **Escala en sp:** 13 `labelSmall` (la etiqueta más chica) · 14 solo rótulos de gráficos y la escala de las 5 etapas · **16 el secundario (el mínimo para informar)** · 17-18 cuerpo · 21-22 títulos de tarjeta · 26-28 títulos de pantalla · 34 el único número grande (la semana de Avance).
- **Unity:** nada informativo bajo 16 dp (48 unidades del lienzo de 1080), en el código (`HudRulesTests`) y en pantalla (la guardia de textos del smoke). Marcador de dos filas: título a 18 dp, consigna y fase a 16-17 dp, chips a 16 dp; «×N seguidos» solo desde 2; el nivel no se ve en partida.
- Con la letra del sistema al 130 % y al 200 % nada se corta ni se solapa (§10).

## 4 · Losas, piezas y medidas

La plasticina es de Blender (`tools/isla/`, la misma luz y material en toda la app): losas por tamaño, discos y piezas sueltas. Al apoyar el dedo la losa se hunde (imagen presionada): el contenido baja y el borde casi no se mueve. Todo objeto tocable tiene su estado presionado, también en Unity (`PlasticTouch`).

| Medida | Token | Valor | Rol |
|---|---|---|---|
| Losa chica, radio | `kit.json losas.ch.radio_dp` | 14 dp | chips, casillas, píldoras |
| Losa mediana, radio | `kit.json losas.md.radio_dp` | 22 dp | botones y tarjetas |
| Losa grande, radio | `kit.json losas.gr.radio_dp` | 30 dp | láminas y paneles |
| Presionada: el contenido baja | `kit.json losas.md.presionada.texto_baja_dp` | 2 dp | respuesta al dedo |
| Disco (cerrar, atrás, íconos) | `kit.json discos.cerrar.cuerpo_dp.ancho` | 48 dp | el toque mínimo, en un objeto |

Otras medidas que no vienen de `kit.json`: botón principal ≥ 56 dp de alto; **todo lo tocable ≥ 48 dp** (en Unity, 48 dp = 144 unidades; también en la práctica de los tutoriales; guardia `ScanTouchTargets` y `TouchTargetTests`); la farola de Ajustes mide 32 dp en una caja de 46 dp con el toque mínimo de Material.

## 5 · Movimiento

Una sola escala (`NubiMotion`): rápido para lo que responde al dedo, medio para cambiar de lugar y largo solo para celebrar. Todo tiempo nuevo sale de aquí. **«Quitar animaciones» = `snap()`:** cada cosa va a su estado final de una vez y la lámina aparece en su sitio con un fundido corto.

| Clase | Token | Valor | Dónde |
|---|---|---|---|
| Al apretar | `NubiMotion.PRESS_MS` | 80 ms | losas y discos del kit |
| Al soltar | `NubiMotion.RELEASE_MS` | 140 ms | losas y discos del kit (más lento que al apretar) |
| Estado, rápido | `NubiMotion.STATE_FAST_MS` | 150 ms | cubo de la barra, interruptor |
| Estado | `NubiMotion.STATE_MS` | 200 ms | velo de la lámina, farola que se enciende |
| Pestaña | `NubiMotion.TAB_MS` | 300 ms | pasar de una pestaña a otra, pasos del Primer vuelo |
| Lámina | `NubiMotion.SHEET_MS` | 400 ms | la lámina de un lugar, sin espera |
| Celebrar | `NubiMotion.CELEBRATE_MS` | 600 ms | pulso de farola |
| Celebrar, largo | `NubiMotion.CELEBRATE_LONG_MS` | 900 ms | el saltito de Nubi al completar la ruta |
| Movimiento reducido | `NubiMotion.REDUCED_FADE_MS` | 150 ms | el fundido que reemplaza a todo movimiento de lugar |

**El «juice» de los juegos (Tarea 130; Unity, decorativo, sin sonido nuevo y sin cambiar medidas ni tiempos de ensayo):** Carga exacta, el aro de 12 luces se llena en cascada de 0,15 s por luz y la celda que recibe late con rebote en 0,30 s; Tinta o Palabra, la moneda ✓ / ✗ entra en 0,30 s sobre el botón tocado (nunca se tiñe la losa); En la punta de la lengua, la tarjeta se hace a un lado en 0,30 s; Rastro de luz, cada tramo del hilo se tiende en 0,20 s. Con «quitar animaciones» cada cosa va a su estado final de una vez y con la misma duración.

## 6 · Sonido

- **Paleta «Madera cálida»** (`Games/Shared/WoodSounds.cs`): kalimba y marimba sintetizadas, una sola sala (la de Rescate), un evento = un sonido, siempre el mismo en cualquier juego; lo que sube con una cuenta sube por la pentatónica de do. Cuatro familias de volumen: micro, respuesta, hito y cierre. Volumen general +3 dB en un solo lugar (`WoodKit.MasterGainDb`).
- **Eventos comunes (`GameFeel`):** acierto, error, nivel, tic del último tramo del reloj, cuenta regresiva y «¡Ya!», pausa, aviso, toque del kit y UN solo final. Ningún juego suena un final propio ni suma el aviso o el toque del kit a un sonido suyo (`SoundRulesTests`).
- **En la app:** `Record` (récord, en el final con sentido) y `Nuevo` (una pieza de la isla). Los sonidos que llevan información del juego (la señal de Freno, las notas de Rastro) no se cambian: solo su timbre.
- Sonido y vibración se pueden apagar y la app lo respeta siempre. El «juice» no suma sonidos.

## 7 · Fondo y capas

- **«Espacio profundo»:** lámina `BoardNavy` con una nebulosa suave, viñeta y 46 luceros fijos; un solo cielo en toda la app (también en Unity, la cortina, la pausa y la carga).
- **Los luceros van siempre DETRÁS** del contenido (`BoardLayersTest`); los textos sueltos sobre el cielo usan `OnBoardText`.
- **La barra de pestañas es una capa fija** encima de las páginas (cada pestaña deja su franja con `LocalBarInset`); las ventanas y láminas la cubren; las páginas no se desvanecen.
- **Las pantallas principales no usan recuadros para informar:** texto suelto, objetos y líneas finas; las tarjetas son para lo que se toca y para los diálogos (el globo de Nubi es el único recuadro de Avance).

## 8 · Do / Don't

1. **Do** un cielo de noche y, encima, plasticina renderizada. **Don't** imitar la plasticina con degradados o sombras de código.
2. **Don't** estrellas con puntas, medias lunas ni cruces: luceros redondos, hexágono, gota, ola (sin estrellas con puntas).
3. **Don't** emojis como íconos: cada ícono es una pieza del kit.
4. **Do** decir el acierto y el error con forma o texto además del color (nada solo por color).
5. **Don't** poner caras que reaccionen a cómo juegas (sin caras que reaccionen; Nubi no cambia de gesto según el puntaje).
6. **Do** informar con texto suelto y objetos (sin recuadros para informar en las pantallas principales); la tarjeta es para lo que se toca. La lectura LARGA (Licencias y créditos, el aviso de salud de Ajustes) es la excepción: va sobre UNA losa crema (`ClayPanel`) con la letra en tinta, nunca suelta sobre el cielo, donde los luceros fijos quedan detrás de las palabras y se leen como puntuación (Tarea 145).
7. **Do** la letra de lectura a 16 o más (letra ≥ 16 en la app y en los juegos; el único 14 son los rótulos de gráficos).
8. **Do** objetos tocables de 48 o más (toque ≥ 48 en todo, también en la práctica de los tutoriales).
9. **Don't** partir una palabra por la mitad: si no cabe, 2 líneas.
10. **Do** contraste de 4,5:1 o más; **Don't** sol sobre crema (1,3:1): en la losa crema los títulos van en tinta.
11. **Do** respetar «quitar animaciones», el sonido y la vibración apagados.
12. **Don't** culpa ni pérdida: nada de «¡No pierdas tu racha!», «Última oportunidad» ni «Solo por hoy»; Nubi acompaña y la racha perdona.

Kit sobre el cielo navy en los juegos de Unity (reglas R1-R4, Tarea 129): **R1** noche solo para las casillas de respuesta con un símbolo calibrado sobre oscuro; **R2** la información en crema con la letra en tinta (`NeuroStyle.Ink`, rótulos `InkMuted` #514B78, acento `PanelAccent` #2B57B8); **R3** los botones secundarios en lila (el principal, cielo, sol o lima; «apagada» solo para lo que de verdad no se puede); **R4** un símbolo calibrado sobre oscuro dentro de una tarjeta crema va en su ventanilla noche.

## 9 · Componentes

App (Compose, `ui/components/` y `ui/theme/`):
- `ClaySlab`: la losa de plasticina (chica, mediana, grande) con su imagen presionada; todo lo demás se apoya en ella.
- `KitButton` / `ClayButton`: el botón de la losa mediana (sol por defecto; lila para secundarios), ≥ 56 dp de alto.
- `ClayDisc` y `KitCloseButton`: el disco de 48 dp (cerrar, atrás, íconos de churro).
- `ClayPill`: pastilla chica que solo informa (rachas, etiquetas); no se toca.
- `ClaySwitch` y `ClayTextField`: los controles de Ajustes; `WeeklyGoalLamps`: la meta semanal en 7 farolas con su número, la elegida en un chip sol.
- `NubiBubble` y `NubiClay`: el globo de Nubi (losa crema con cola) y Nubi 3D de frente; Nubi comenta, no manda.
- `FarolaImage` y `WeekLampRendered`: la farola del kit, encendida o apagada (la semana de Hoy y de Avance).
- `ClayPanel`: la losa grande crema para la lectura larga y las tarjetas de Ajustes (letra en tinta; Licencias y créditos y el aviso de salud van sobre una).
- `KitBar`: el canal y el churro de las barras de avance; `FamilyMedal`: las medallas de conducta, también la de la celebración y la de la tarjeta de compartir (la vectorial solo queda para «Medallas anteriores»); `GameEmblem`: el emblema de un juego; `PedestalImage` y los edificios: los lugares de la isla; `Island`: la isla de Hoy y, al cerrar el Primer vuelo, la que se verá mañana.
- `OnBoardText`, `BoardBackground` y `CosmosBackground`: el texto sobre el cielo y el cielo mismo.

Unity (`Games/Shared/`): `PlasticSlab` y `PlasticTouch` (la losa y su estado presionado), `GameHud` (el marcador común de dos filas y la fila de consigna), `GuidedTutorial` (el tutorial fijo con Nubi), `UiKit` (el movimiento común), `NeuroStyle` (la paleta), `GameFeel` (sonido y háptica).

## 10 · Accesibilidad

- **Contraste** ≥ 4,5:1 en todo texto (§2 trae los medidos); nada solo por color (forma o texto).
- **Toque** ≥ 48 dp, con la separación suficiente entre objetos; los botones principales, ≥ 56 dp.
- **Lector de pantalla:** cada pieza tiene su descripción; los grupos se leen como grupo («3 de 4 días de tu meta», «4 días por semana» en la farola elegida); los botones de radio llevan `Role.RadioButton`.
- **Letra grande:** se prueba con la letra del sistema al 130 % y al 200 %, también a 360 dp de ancho (`AvanceJuegosLetraGrandeTest`, `WeeklyGoalLampsTest`).
- **Reducir movimiento:** «quitar animaciones» deja cada cosa en su estado final (§5).
- **Los juegos** no dependen del color para jugar (forma, lugar o texto; nunca solo color). El ajuste «¿Te cuesta distinguir algunos colores?» de Ajustes SOLO se guarda: hoy no cambia nada en los juegos, y desde el 19-oct el Primer vuelo ya no lo pregunta.

## 11 · Fuentes de verdad

`ui/theme/Clay.kt`, `ui/theme/NubiTokens.kt`, `ui/theme/Type.kt`, `ui/components/ClayKit.kt`, `res/raw/kit.json`, `ui/components/NubiMotion.kt`, `ui/components/BoardBackground.kt` y `CosmosBackground.kt`, `docs/sonido-madera-plan.md`, `docs/diseno-isla.md`, `docs/plasticina-juegos.md`, `docs/movimiento-reducido.md`, `docs/arquitectura-app.md` y `CLAUDE.md` (las reglas que no se rompen). La prueba es `app/src/test/java/com/example/DesignMdTest.kt`.

## 12 · Web (landing, dirección B)

Solo documentación: la landing NO se construye ahora (se diseña ahora y se publica en la etapa de Google; spec en `auditoria-fable/spec-adopcion-A1-A7.md`, §D.2 y §D.4; boceto `landing-boceto-v2.html`). **Dirección B, elegida por Ricardo el 18-oct:** cielo + losas crema, el mismo patrón de la app (el cielo en los márgenes y en los extremos; la lectura sobre papel crema).

| Token web | Valor | Rol | Dónde NO |
|---|---|---|---|
| `--navy` | #0E0D33 | el cielo (con los luceros y el degradado de §2) | superficie de lectura larga |
| `--cream` | #F5EBDB | la losa de cada idea (radio 30 px, canto del kit) | borde de lado a lado de la página |
| `--ink` | #1A1240 | títulos y cuerpo sobre crema | sobre el cielo |
| `--ink-muted` | #514B78 | texto secundario sobre crema | sobre el cielo |
| `--sun` | #FFC93C | el botón único, el churro bajo cada título y los títulos del hero y del cierre | título sobre crema (1,3:1) |
| `--lilac` | #B8A4FF | solo el segundo botón («Ver los juegos», que baja a la idea 2) | otro llamado a la acción |
| `--on-night` / `--on-night-dim` | #EAF0FF / #C7D0FF | texto sobre el cielo | sobre crema |

- **Estructura:** «una pantalla, una idea». Hero y cierre sobre el cielo; siete ideas, cada una en UNA losa crema con su escena de Nubi (el elenco es solo Nubi y los objetos de los juegos; las manos de plasticina, después). Unas 180 palabras de cuerpo; título de 2 a 4 palabras en minúscula; cuerpo de 2 a 3 líneas, ≤ 30 palabras.
- **Losas, no fondo:** márgenes de cielo de 16 px a los lados y 32-48 px entre losas; ancho máximo 1040 px; radio 30 px (`--r-gr`) en las losas y 22 px (`--r-md`) en los botones.
- **Tipografía en px:** base 18 px (`html`); **cuerpo ≥ 18 px** (1,15 rem ≈ 20,7 px, interlineado 1,5, 34 caracteres de ancho máximo); títulos en Fredoka 700: `h2` de 1,9 a 2,8 rem y `h1` de 2 a 3,2 rem; botones de 56 px de alto como mínimo (48 px en la barra fija). Las fuentes (Fredoka y Nunito, OFL) se alojan en el mismo sitio, sin Google Fonts.
- **El título sobre crema** va en tinta con un churro sol debajo (72 × 9 px, sombra de 3 px en `#C9951A`); **sobre el cielo** va en sol. Un solo color fuerte: el sol, en el botón, el churro y los títulos de los extremos.
- **El botón único:** hoy «Avísame cuando esté lista»; en la prueba cerrada «Quiero probarla»; publicada, «Descargar gratis» (la insignia de Google Play al lado, no en lugar del botón). Siempre sol con letra tinta, el mismo texto en la barra fija, en el hero y en el cierre.
- **Do:** una idea por pantalla; el título cuenta la idea y la escena la repite; frases cortas en segunda persona; números solo cuando cambian algo («17 juegos», «unos 5 minutos»). **Don't:** listas de funciones, cifras de salud, comparaciones, capturas sueltas, mayúsculas sostenidas, más de una exclamación, dos llamados que compitan.
- **Accesibilidad web:** contraste medido (tinta/crema 14,8:1; tinta/sol 11,4:1; `#C7D0FF` sobre `#0E0D33` 12,4:1), foco visible, `prefers-reduced-motion`, letra del navegador al 130 %, lector de pantalla sobre el hero y los botones, Lighthouse de accesibilidad ≥ 95.
- **Voz:** la de §1. Dice lo que hace, no lo que promete; seguro sin gritar; cálido sin infantilizar.
