# Rastro de luz — Secuencia Lumínica rehecha desde cero (aprobado por Ricardo el 3-oct)

Reemplaza a Secuencia Lumínica (id interno `secuencia`, que SE MANTIENE para no perder avance, marcas ni historial).
Juego estrella de **Memoria** y juego de Memoria del inicio nuevo (`docs/diseno-inicio.md`).
Boceto jugable aprobado: `docs/previews/rastro-de-luz-boceto.html` (https://claude.ai/artifact/JDeHESmN6KmzPF4rq7nXGc).
Ricardo (3-oct): «me parece bien la mecánica y los cambios dentro de una misma partida»; le pareció «un poco difícil,
pero es parte de la experiencia». En el boceto no hay DDA: cada acierto suma una luz. En el juego, la dificultad la lleva
el motor común y arranca más suave (ver §4).

## 1. La idea en una línea

Una chispa de luz vuela entre 9 luceros de cristal; cada lucero suena con su nota, así que el camino se ve y se oye como
una melodía. Lo repites deslizando el dedo (o tocando). Al subir de nivel, Nubi desbloquea giros: **al revés**, **el cielo
gira** y **en marcha**.

## 2. Ciencia (PubMed, 3-oct)

- Amplitud visoespacial tipo Corsi (orden de lugares): Kessels y otros, 2000, *Appl Neuropsychol* 7:252-8,
  [doi:10.1207/S15324826AN0704_8](https://doi.org/10.1207/S15324826AN0704_8).
- La forma del camino cambia lo que se recuerda (cruces, largo y ángulos lo hacen más difícil): Parmentier, Elford y
  Maybery, 2005, *J Exp Psychol Learn Mem Cogn* 31:412-27, [doi:10.1037/0278-7393.31.3.412](https://doi.org/10.1037/0278-7393.31.3.412).
  Es la palanca de dificultad «curvas y cruces».
- «Memoria en marcha» (running span): mide capacidad de memoria de trabajo y se relaciona con el razonamiento: Broadway y
  Engle, 2010, *Behav Res Methods* 42:563-70, [doi:10.3758/BRM.42.2.563](https://doi.org/10.3758/BRM.42.2.563).
- El límite real de la memoria de corto plazo ronda los 4 elementos: Cowan, 2001, *Behav Brain Sci* 24:87-114,
  [doi:10.1017/s0140525x01003922](https://doi.org/10.1017/s0140525x01003922). Por eso las escaleras no pasan de 8.
- «El cielo gira» (recordar el orden sobre objetos que se movieron) es una variante propia; se presenta como juego, sin
  afirmar que mida algo validado. Ojo de originalidad: Lumosity tiene un juego donde una cuadrícula con un patrón gira 90°
  («Rotation Matrix»). El nuestro es distinto (orden de un camino, no un patrón; ángulos libres; luceros que suenan), pero
  no decir que «ninguna app gira»; nombre, arte y sonido propios.
- Sin patentes conocidas sobre amplitud de secuencias con luz y sonido (el juego «Simon» de 1978: patente vencida y nombre
  ajeno, no usar). Lista del abogado: revisar «Rotation Matrix» de Lumosity antes de publicar.

## 3. Cómo se juega

- **Escena** (`GameWorld` nuevo, p. ej. `CieloDeCristal`): fondo C con nebulosas lila/celeste/coral tenues, ~240 estrellas
  fijas y ~30 que titilan; un disco punteado muy tenue marca el tablero. Cada modo tiñe suavemente el cielo (radial del
  centro, alfa ~0,12): rastro celeste `76,201,240` · al revés violeta `184,164,255` · gira verde agua `120,240,200` · en marcha
  coral `255,159,122`.
- **Luceros**: 9 esferas de cristal (degradé radial blanco → color propio → translúcido, borde blanco fino, brillo
  especular arriba a la izquierda, halo del color) en anillo irregular + centro, como el boceto. Redondos, SIN puntas ni
  símbolos. Diámetro ≥ 56 dp (mayores 64), zona de toque ≥ 72 dp, separación ≥ 12 dp. Respiración muy suave en reposo.
  Colores: `#7FD8FF #B8A4FF #FFC94A #8EE07A #FF9F7A #9FF5D6 #F7A8E0 #FFE27A #A9C7FF`; notas: pentatónica
  `392 440 523,25 587,33 659,25 783,99 880 1046,5 1174,66` Hz (una por lucero, siempre la misma).
- **Muestra**: la chispa (halo del color del modo + núcleo blanco) vuela de lucero en lucero por curvas (Bézier
  cuadrática, alternando el lado de la curva), deja un rastro que se angosta y se apaga en ~1,5 s y suelta partículas. Al
  llegar a cada lucero: florece (anillo que se expande 650 ms), suena su nota y salen chispas de su color.
- **Respuesta**: deslizar el dedo dibuja una cinta de luz crema que se apaga en 0,7 s; al pasar a ≤ 34 dp de un lucero
  se engancha (no cuenta el mismo lucero dos veces seguidas). También vale tocar uno por uno. Cada enganche correcto:
  florece, suena su nota y queda un tramo fijo del color del modo entre los luceros acertados.
- **Acierto de ronda**: el camino completo brilla más, acorde de 4 notas, y las chispas vuelan al contador «luces
  recordadas» (arriba a la derecha). Vibración corta.
- **Error**: el lucero tocado lleva un aro coral con una ✗ dibujada y el correcto un aro sol que late. Texto: «Esa no era:
  la correcta tiene el aro amarillo». Sonido suave y vibración doble. Se pierde una vida (3) y sigue otra ronda.
- **Modos (familias)**:
  - **El rastro** (→): repetir en el mismo orden.
  - **Al revés** (⟲): de la última a la primera. Rótulo «Al revés: de la última a la primera».
  - **El cielo gira** (↻): tras la muestra, el tablero gira (sonido de aire) y se repite sobre los mismos luceros.
  - **En marcha** (≫): la chispa recorre más luceros de los que se piden (sin aviso de cuántos); al final se piden «las
    últimas N».
  Cada modo: ícono dibujado (no depender de glifos de la fuente), nombre y tinte. Nunca solo color.
- **Desbloqueo**: la PRIMERA VEZ (de por vida, guardado) que se llega a un modo, pantalla «¡NUEVO!» con el ícono grande, el
  nombre y una línea («De la última luz a la primera» / «El cielo da un giro: sigue a tus luceros» / «Repite solo las
  últimas luces»), dos campanas, 2,3 s o toque para seguir.
- **HUD**: píldora con ícono + modo, píldora «N luces» (o «Últimas N»), 3 vidas (puntos crema), contador grande «luces
  recordadas». Textos ≥ 14 sp (18 en las instrucciones).
- **Partida**: Reto 90 s (con el reloj común) o Precisión 14 rondas; en ambos, 3 vidas. Nunca más de 2 rondas seguidas
  del mismo modo nuevo; el rastro simple aparece siempre en la mezcla.

## 4. Dificultad (motor común, 16 niveles; misma escala que la Secuencia anterior)

Ensayo = ronda completa (acierto si se repite entero bien). Sin tiempo de reacción (`useReaction:false`, como hoy).
Cada nivel define: largo por familia, velocidad de la chispa (px/s), espera en cada lucero (ms), giro (grados), cuántos
de más en «en marcha» y si se permiten cruces. Propuesta (Sonnet puede afinar manteniendo el espíritu):

| Nivel | Familias disponibles | Largo (rastro / revés / gira / marcha: últimas) | Chispa | Giro | Cruces |
|---|---|---|---|---|---|
| 1 | rastro | 2 | 200 px/s, espera 320 ms | – | no |
| 2 | rastro | 3 | 220 / 300 | – | no |
| 3 | rastro | 3 | 260 / 260 | – | no |
| 4 | rastro | 4 | 260 / 260 | – | no |
| 5 | + al revés | 4 / 3 | 280 / 240 | – | no |
| 6 | | 4 / 4 | 280 / 240 | – | no |
| 7 | + gira | 5 / 4 / 3 | 300 / 220 | 60-90° | no |
| 8 | | 5 / 4 / 4 | 300 / 220 | 60-120° | no |
| 9 | + en marcha | 5 / 4 / 4 / 2 (de 3-5) | 300 / 220 | 90-150° | no |
| 10 | | 5 / 5 / 4 / 3 (de 4-6) | 320 / 200 | 90-150° | 1 |
| 11 | | 6 / 5 / 5 / 3 | 320 / 200 | 90-180° | 1 |
| 12 | | 6 / 5 / 5 / 3 (de 5-7) | 340 / 190 | 90-180° | 2 |
| 13 | | 6 / 6 / 5 / 4 | 340 / 190 | libre | 2 |
| 14 | | 7 / 6 / 6 / 4 | 360 / 180 | libre | 2 |
| 15 | | 7 / 6 / 6 / 4 (de 6-8) | 380 / 170 | libre | sí |
| 16 | | 8 / 7 / 6 / 5 | 400 / 160 | libre | sí |

- Mayores (`Senior`): objetivo 0,85 del motor común con un `stepUp` que no choque con el tope de bajada por error
  (`MaxDropPerError`; con la escala de 16 niveles el de Secuencia actual converge a ~0,82: usar ≈ 0,17 para Senior), chispa
  −15 % y luceros de 64 dp.
- «Primera vez» y evaluación inicial: arrancan en nivel 2 (adultos) o 1 (mayores), solo el rastro simple.

## 5. Tutorial dentro del juego (primer uso del formato de `docs/diseno-inicio.md`)

- Sale si la persona nunca jugó este juego (la app lo sabe por el historial y manda `show_tutorial=true`).
- Tarjeta de entrada: Nubi maestra + «Mira el camino de la chispa y repítelo con el dedo».
- Ronda guiada de 2 luces: rótulo «Práctica: no cuenta», «Saltar tutorial» visible, el siguiente lucero correcto con un aro
  sol punteado; si hay error: «Casi: esa no era. Mira otra vez el camino» y se repite. Al acertar: «¡Así se juega! Ahora
  sin ayuda» y empieza la partida. No suma puntos ni ensayos al DDA ni se guarda.
- Construirlo como pieza común reutilizable (`Games/Shared/`: tarjeta de Nubi maestra, rótulo de práctica, botón saltar),
  porque los otros 22 juegos la usarán.

## 6. Medida al final (juego estrella)

1. **Tu rastro** (cifra grande): las luces más largas que repetiste bien en el rastro simple.
2. **Por modo**: al revés / con giro / en marcha, cada uno solo si tuvo ≥ 3 rondas; aciertos por modo; el de menor tasa
   marcado con TEXTO «el que más te costó».
3. Lectura en palabras (Nubi científica): qué es la memoria de trabajo y una línea por modo («Al revés pide reordenar en la
   cabeza: suele costar un poco más»). Sin percentiles. Nota común al pie.
Agregar la medida a `docs/medidas-juegos-estrella.md` y a `StarMeasures`.

## 7. Sonido y efectos

- Pentatónica por lucero (campana cristalina: fundamental + parcial 2,76× + 5,4×), acorde de acierto, error grave suave,
  soplo de aire al girar, dos campanas en «¡NUEVO!». Nada arcade. Respeta sonido apagado.
- Vibración solo en acierto de ronda, error y desbloqueo.
- «Quitar animaciones»: el giro SE QUEDA (es la tarea) pero lineal; sin partículas, sin respiración, sin vuelo de chispas
  al contador; el rastro y la cinta como tramos rectos con fundido de 300 ms; misma duración de las esperas
  (`Motion.Hold`).
