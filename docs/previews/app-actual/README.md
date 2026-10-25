# La app de hoy, en capturas reales (24-oct-2026, hasta la Tarea 165)

Capturas REALES de Roborazzi (`AppActualScreenshotTest`) con unos 20 días de uso de ejemplo, a 393 dp de ancho (1080 px) y 873 dp de alto (las pantallas que se deslizan, enteras en una sola imagen alta). Se regeneran con
`./gradlew.bat :app:recordRoborazziDebug --tests "*AppActualScreenshotTest*"` y `python tools/previews/app_actual.py`. Las pantallas principales también con la letra del sistema al 130 % (`-letra13`). Los juegos de Unity están en
[`../capturas/`](../capturas/README.md) y la vista general de todo, en [`../estado-19-oct.png`](../estado-19-oct.png).

| Lo que se ve | Archivos |
|---|---|
| Lámina resumen de TODO lo de abajo | `lamina.png` |
| Hoy con UNA sola tarjeta (Tarea 165): el saludo, la semana, la tarjeta de la ruta con Nubi y el botón, la losa «Todos los juegos» y la barra; con letra 1,3 y 2,0 | `hoy.png`, `hoy-letra13.png`, `hoy-letra20.png` |
| La isla de Tú sin piezas y con las 10 («Tu isla crece»; desde la Tarea 165 la isla y la revelación de las piezas viven en Tú) | `tu-isla-0.png`, `tu-isla-10.png`, `tu-isla-10-letra13.png` |
| «Todos los juegos» (Tarea 158; antes la pestaña Juegos): sin filtro, con «Memoria» y con letra 1,3 | `todos-los-juegos.png`, `todos-los-juegos-memoria.png`, `todos-los-juegos-letra13.png` |
| La ficha del juego (Tarea 140): plegada y desplegada | `ficha.png`, `ficha-desplegada.png`, `ficha-letra13.png`, `ficha-desplegada-letra13.png` |
| Avance con puntos por área (Tarea 159): arriba, entero y su pie | `avance.png`, `avance-letra13.png`, `avance-completa.png`, `avance-abajo.png` |
| La página de un área en Avance (Tarea 159; antes la lámina de la Tarea 155) | `avance-pagina.png`, `avance-pagina-letra13.png`, `avance-pagina-completa.png` |
| Tú (Tarea 158; antes el panel «Perfil»): arriba, entera, sin partidas y editando el nombre | `tu.png`, `tu-letra13.png`, `tu-completa.png`, `tu-vacio.png`, `tu-vacio-letra13.png`, `tu-editando.png` |
| Ajustes (con las farolas de la meta) y Licencias | `ajustes.png`, `ajustes-letra13.png`, `ajustes-completa.png`, `licencias.png`, `licencias-completa.png` |
| El final con sentido de una partida: sin récord y con récord | `resultado.png` (Rastro de luz), `resultado-letra13.png`, `resultado-sin-record.png`, `resultado-con-record.png` y sus `-completa` |
| La celebración de una medalla, con y sin pieza de la isla | `medalla-con-pieza.png`, `medalla-sin-pieza.png` |
| Primer vuelo: Hola, juego 1, edad, metas, punto de partida y camino | `primer-vuelo-1.png`, `primer-vuelo-juego.png`, `primer-vuelo-edad.png`, `primer-vuelo-2.png`, `primer-vuelo-punto.png`, `primer-vuelo-3.png`, `primer-vuelo-1-letra13.png` |
| La barra de pestañas (Hoy · Avance · Tú; cada una elegida, con letra 1,0 y 1,3) | `barra.png`, `barra-letra13.png`, `barra-avance.png`, `barra-avance-letra13.png`, `barra-tu.png`, `barra-tu-letra13.png` |

Cómo se arman las tomas: la barra de pestañas se mide como en `MainActivity` (con la letra al 130 % es más alta que con la normal) y se esconde con la ventana de un área abierta, igual que en la app.

## Lo que se vio al revisarlas (sin arreglar: solo anotado)

(Hoy con letra 1,3 ya cabe entero sin deslizar desde la Tarea 165, y la lámina de un área de Hoy se borró.)

- **Detalle de un área en Avance:** la burbuja de Nubi de arriba dice «Esta semana Memoria avanzó» y parece cortarse (¿falta «de 46 a 52»?), con letra 1,0 y 1,3. Con letra 1,3, bajo «Tus juegos de memoria» no se ve ningún juego: la zona fija de abajo («¿Le damos un empujón a tu memoria?», «¡Sí, vamos!» y la frase de Nubi) ocupa casi todo el alto.
- **Ajustes:** dos títulos seguidos, «Opciones» (barra de arriba) y «Ajustes y Preferencias» (cabecera).
- **Primer vuelo:** las preguntas de edad y de metas dicen «Paso 2 de 6 · Memoria» (Memoria es el área del juego anterior, no de la pregunta).
- **Resultado de una partida:** «Tu promedio —» y «Tu mejor —» quedan vacíos en el ejemplo (no hay partidas anteriores del juego); en las tomas altas se ve algún lucero del fondo pegado al texto (es del marco de la toma: en el teléfono el fondo es fijo y el texto se desliza).

### En los juegos de Unity (`../capturas/`, a primera vista)

Sin errores de consola en los 17 (corrida del 21-oct, Tarea 154: 310 capturas; el script ya no incluye Rumbo a Casa ni Cosecha de palabras, retirados de la app). Los cuatro puntos que esta nota traía de la corrida del 20-oct **ya estaban resueltos desde la Tarea 144** (Fable había leído esta nota vieja); se verificaron con las capturas nuevas:

- **Acoplamiento:** «Módulo 2 · 8 a bordo» y el chip «×8 seguidos» van en filas aparte: no se pisan.
- **Bodega de carga:** el chip «×N seguidos» baja bajo la consigna (`AvoidForStreak`) y no tapa el aviso.
- **En la punta de la lengua:** la pregunta va pegada a la tarjeta y las casillas vacías están desde el primer cuadro: ya no hay un hueco.
- **Freno de emergencia:** el conjunto cohete-plataforma-botón subió (0,22 del alto) y creció; la mitad de arriba la ocupa la señal ALTO durante la partida (en la primera pantalla solo está «Tu freno»: es lo esperado).
- **Lo que cambió con la Tarea 154:** las tarjetas «Nuevo» y el resto de los textos que dibuja Unity hablan como la app (caja normal, «seguidos», «¡Un paso más!», «Encargos» en Correo) y llevan la losa chica «Seguir»; hoja `docs/previews/tarea-154.png`.
