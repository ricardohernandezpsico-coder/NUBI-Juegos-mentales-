# La app de hoy, en capturas reales (21-oct-2026, hasta la Tarea 158)

Capturas REALES de Roborazzi (`AppActualScreenshotTest`) con unos 20 días de uso de ejemplo, a 393 dp de ancho (1080 px) y 873 dp de alto (las pantallas que se deslizan, enteras en una sola imagen alta). Se regeneran con
`./gradlew.bat :app:recordRoborazziDebug --tests "*AppActualScreenshotTest*"` y `python tools/previews/app_actual.py`. Las pantallas principales también con la letra del sistema al 130 % (`-letra13`). Los juegos de Unity están en
[`../capturas/`](../capturas/README.md) y la vista general de todo, en [`../estado-19-oct.png`](../estado-19-oct.png).

| Lo que se ve | Archivos |
|---|---|
| Lámina resumen de TODO lo de abajo | `lamina.png` |
| Hoy: la isla, la ruta, la losa «Todos los juegos» y la barra | `hoy.png`, `hoy-letra13.png` |
| Hoy con el globo de Nubi, una sola vez (Tarea 158: «Los juegos ahora están en la isla…») | `hoy-globo.png`, `hoy-globo-letra13.png` |
| La isla sin piezas y con las 10 piezas («Tu isla crece») | `hoy-isla-0.png`, `hoy-isla-10.png`, `hoy-isla-10-letra13.png` |
| La lámina del lugar (al tocar un edificio; en Hoy, una sola línea de avance, Tarea 158) | `hoy-lamina.png`, `hoy-lamina-letra13.png` |
| «Todos los juegos» (Tarea 158; antes la pestaña Juegos): sin filtro, con «Memoria» y con letra 1,3 | `todos-los-juegos.png`, `todos-los-juegos-memoria.png`, `todos-los-juegos-letra13.png` |
| La ficha del juego (Tarea 140): plegada y desplegada | `ficha.png`, `ficha-desplegada.png`, `ficha-letra13.png`, `ficha-desplegada-letra13.png` |
| Avance: arriba (con Nubi), entero y su pie | `avance.png`, `avance-letra13.png`, `avance-completa.png`, `avance-abajo.png` |
| La lámina de un área en Avance, con el gráfico de las 4 semanas (Tarea 155) | `avance-lamina.png`, `avance-lamina-letra13.png` |
| Tú (Tarea 158; antes el panel «Perfil»): arriba, entera, sin partidas y editando el nombre | `tu.png`, `tu-letra13.png`, `tu-completa.png`, `tu-vacio.png`, `tu-vacio-letra13.png`, `tu-editando.png` |
| Ajustes (con las farolas de la meta) y Licencias | `ajustes.png`, `ajustes-letra13.png`, `ajustes-completa.png`, `licencias.png`, `licencias-completa.png` |
| El final con sentido de una partida: sin récord y con récord | `resultado.png` (Rastro de luz), `resultado-letra13.png`, `resultado-sin-record.png`, `resultado-con-record.png` y sus `-completa` |
| La celebración de una medalla, con y sin pieza de la isla | `medalla-con-pieza.png`, `medalla-sin-pieza.png` |
| Primer vuelo: Hola, juego 1, edad, metas, punto de partida y camino | `primer-vuelo-1.png`, `primer-vuelo-juego.png`, `primer-vuelo-edad.png`, `primer-vuelo-2.png`, `primer-vuelo-punto.png`, `primer-vuelo-3.png`, `primer-vuelo-1-letra13.png` |
| La barra de pestañas (Hoy · Avance · Tú; cada una elegida, con letra 1,0 y 1,3) | `barra.png`, `barra-letra13.png`, `barra-avance.png`, `barra-avance-letra13.png`, `barra-tu.png`, `barra-tu-letra13.png` |

Cómo se arman las tomas: la barra de pestañas se mide como en `MainActivity` (con la letra al 130 % es más alta que con la normal) y se esconde con la ventana de un área abierta, igual que en la app.

## Lo que se vio al revisarlas (sin arreglar: solo anotado)

- **Hoy con letra 1,3:** el botón grande «Seguir la ruta» queda bajo el pliegue y la tercera parada asoma tapada por la barra (hay que deslizar; la regla de Hoy lo permite, pero el botón principal no se ve).
- **Lámina de un área, con letra 1,3:** la cuadrícula de los números de la semana pasa a 2 × 2 y la segunda fila («37» y «Esta semana») queda a medio ver bajo el botón fijo «Jugar en el Observatorio». La burbuja de Nubi cubre el saludo y la fecha (con las dos letras).
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
