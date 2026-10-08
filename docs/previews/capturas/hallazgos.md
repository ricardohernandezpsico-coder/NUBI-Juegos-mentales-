# Hallazgos de las capturas reales (8-oct-2026)

Revisión a primera vista de las 19 láminas de esta carpeta (commit de la Tarea 53), mirando lo que busca esta revisión: textos fuera de lugar, cosas tapadas, fondos que no cubren y elementos detrás de otros. **No se arregló nada** (la tarea no toca la lógica de ningún juego): es la lista para decidir. «Confirmado» = se amplió la toma a tamaño completo; «a verificar» = se ve en la lámina pero puede ser un instante de animación o una decisión de diseño.

| Juego | Qué se ve | Dónde |
|---|---|---|
| Constelaciones (`parejas`) | Limpio. | |
| Rastro de luz (`secuencia`) | **Confirmado**: en el paso 3 del tutorial, la luz dorada resaltada lleva detrás un cuadrado oscuro (el halo se dibuja en un rectángulo que no se funde con el cielo). | `secuencia/08-tutorial-3` |
| Bodega de carga (`bodega`) | A verificar: la pausa no ofrece «Cómo se juega» aunque el juego tiene tutorial (Rastro, Constelaciones, Tinta, Carga exacta, Aterrizaje, Freno, Meteoros, En la punta, Engranajes y Correo sí lo ofrecen). | `bodega/05-pausa` |
| Rumbo a Casa (`rumbo`) | A verificar: la tarjeta «Recuerda cada giro» llega pegada al borde superior y tapa el título y el nivel; después queda mucho espacio vacío. | `rumbo/01-primera` |
| La estación de correo (`correo`) | Limpio (ya revisado en detalle el 8-oct). A mirar: en el paso 3 del tutorial el globo de Nubi queda sobre los buzones Coralia y Celesta (no son zona protegida del paso). | `correo/08-tutorial-3` |
| Tinta o Palabra (`stroop`) | **Confirmado**: la fila de 16 puntos de avance se sale por el borde derecho (se ven 13 y el último, cortado). | `stroop/01-primera` |
| Piloto Estelar (`piloto`) | **Confirmado**: los puntos de la ruta se dibujan encima del panel de abajo y de su texto «Desliza aquí para guiar la nave» (taparían sus flechas ‹ ›); el panel de la misión y el de deslizar se pisan. | `piloto/01-primera` |
| Freno de Emergencia (`freno`) | **Confirmado**: «¡FRENO PERFECTO!» pisa el aro verde de la señal ALTO. A verificar: el aviso «¡Nuevo límite!» queda pegado al borde superior y tapa título, nivel y barra del límite (sale igual en la toma de la pausa: puede ser un instante de animación). | `freno/04-t40`, `freno/05-pausa` |
| Satélites (`satelites`) | Limpio. | |
| Radar (`radar`) | Limpio. | |
| Acoplamiento (`acoplamiento`) | A verificar: el aviso «Con calma / Gira la pieza en tu mente» aparece cortado por el borde superior y tapa el encabezado (sale igual en la pausa: puede ser un instante de animación). | `acoplamiento/04-t40` |
| Carga exacta (`calculo`) | Limpio (en el tutorial el «13» queda en un recuadro más claro: es el foco). | |
| Aterrizaje Lunar (`aterrizaje`) | **Confirmado**: en los pasos 1 y 3 del tutorial el marco de foco corta los rótulos «0» y «10» de la regla (se leen «0» y «1(»). | `aterrizaje/07-tutorial-1`, `08-tutorial-3` |
| Engranajes (`engranajes`) | **Confirmado**: en el paso 1 del tutorial el marco de foco queda cortado por el borde derecho; el cartel «Turbina / Antena» llega a 5 px del borde en todas las tomas. | `engranajes/07-tutorial-1`, `01-primera` |
| En la punta de la lengua (`anagramas`) | **Confirmado**: en el paso 1 del tutorial la tarjeta de la definición está VACÍA mientras Nubi dice «Lee la definición y toca la tarjeta» (¿aún no se escribió el texto?). En la primera pantalla la definición sale a medias («pastand») por la animación de escritura: no es un error. | `anagramas/07-tutorial-1` |
| Lluvia de meteoros (`meteoros`) | A verificar: el aviso «se fue: fondo» es muy chico (parece bajo 14 dp); el rastro del primer meteoro pasa por encima del título del encabezado. | `meteoros/04-t40`, `01-primera` |
| ¿Verdad o disparate? (`disparate`) | Limpio. | |
| Cosecha de palabras (`cosecha`) | Limpio. | |
| La estrella intrusa (`intrusa`) | Limpio. | |

**En general**: en la pantalla alta (20:9) Radar, Satélites, Rumbo, La estrella intrusa y Bodega dejan casi un tercio de abajo vacío. Donde no hay piloto automático (todos menos Engranajes, Bodega, Constelaciones y Correo) las tomas de los 8, 20 y 40 s repiten la primera situación del juego: hay que jugarlos a mano para ver más.
