# Dificultad, edad y avance: la matriz de Nubi

Aprobada por Ricardo el 28-sep e **implementada** ese día (lógica en `app/.../data/Skill.kt`, Unity en
`AdaptiveDifficulty.ConfigureMode`, pestaña Juegos con cartas). Decisiones: Experto se abre al superar un Desafío; el
reloj se elige en Ajustes, no antes de jugar; sin modo de dificultad en Ajustes. Pendiente: las fortalezas por
aspecto de la sección 8 (hoy la carta muestra la constancia) y la calibración de la sección 10. Documento original: Nace de la pestaña Juegos: la carta
de cada juego muestra "tu avance" y, antes de jugar, se elige Suave / A tu medida / Desafío / Experto. Ricardo
pidió revisar a fondo el razonamiento para que la dificultad, la edad y lo que se le muestra a la persona formen
una sola matriz lógica. Este documento revisa lo que hay hoy, propone la regla común y deja escritos los supuestos.

## 0. En una página

1. **Una sola regla para todas las edades**: cada juego tiene su **escalera** de niveles (lo que cambia la tarea:
   más ítems, menos tiempo de exposición, más distractores). La escalera es la misma para todos.
2. **Tu avance** (0-100%) = **el punto de la escalera donde aciertas 8 de cada 10**. Se lee igual a los 15, a los
   40 y a los 75 años. Por eso se puede comparar contigo mismo en el tiempo y entre juegos.
3. **La edad no cambia la regla, cambia cómo se entrena**: cuántos aciertos busca el juego mientras juegas, qué
   tan rápido sube, cuánto pesa la rapidez, los tiempos y tamaños, y desde dónde parte.
4. **Los modos se definen por cuántos aciertos esperas, no por niveles**: Suave (casi todo), A tu medida
   (8 de 10; 8 a 9 de 10 en mayores), Desafío (7 de 10), Experto (6 de 10). El nivel donde parte cada modo sale
   de ahí, y por eso es distinto para cada persona y cada edad. Esto es lo que igualamos entre edades: **cómo se
   siente**, no el número de nivel.
5. **Dos familias de números que no se mezclan**:
   - **Cómo te va**: tu avance, tu etapa y tu marca. Solo cambian con partidas comparables.
   - **Cuánto juegas**: tu planeta, tu racha, tu liga y tus trofeos. Todo suma aquí.

## 1. Lo que encontré hoy (diagnóstico)

| # | Hallazgo | Consecuencia |
|---|---|---|
| 1 | Hay **cuatro "dificultades" distintas**: (a) el rating del DDA común (0..1 por juego, el que de verdad usa Unity); (b) un "nivel 1-5" antiguo que sube si el puntaje es ≥ 85 y baja si es ≤ 45, con nombres Principiante…Experto (`LevelTier`, se ve en la pantalla de carga y en Liga); (c) el modo de Ajustes (Auto-adaptativa / Principiante / Intermedio / Avanzado / Personalizada); (d) Suave / Equilibrado / Desafío en la ventana antes de jugar (±1 nivel). | (c) y (d) **no hacen nada** desde la segunda partida: si el juego tiene rating guardado, Unity lo usa e ignora el nivel. El (b) casi no se mueve en los juegos adaptativos, porque el puntaje de la partida es el porcentaje de aciertos, el DDA lo mantiene cerca de 80% y rara vez llega a 85. La persona ve un "Nivel 3 · Intermedio" que no dice dónde está. |
| 2 | El punto de partida estimado (si se salta la evaluación) escribe un rating en **todos** los juegos, jugados o no. | Un juego nunca jugado mostraría "tu avance 35%". Hay que separar "desde dónde parte" de "lo que se midió". |
| 3 | La edad cambia la tasa de aciertos buscada (80% o 85%). | El rating de una persona mayor queda ~⅓ de nivel más abajo que si se midiera a 80%: su "45%" no significaría lo mismo que el de un adulto. Es chico, pero hay que corregirlo para que la regla sea una sola. |
| 4 | Ruta del Tesoro busca 70% de aciertos (perder la ruta cuesta una vida). | Su rating está medido con otra vara: se corrige con la misma fórmula. |
| 5 | En un juego adaptativo **los aciertos se mantienen cerca de 8 de 10 por diseño**. | El porcentaje de aciertos NO sirve para mostrar avance ni fortalezas: lo que avanza es el nivel. |
| 6 | Varias marcas de los juegos estrella dependen del nivel jugado (Aterrizaje: tipo de regla; Rumbo: cantidad de tramos; Bitácora: paradas; Correo: encargos; en parte Acoplamiento y Satélites). | Si subes de nivel, la marca puede "empeorar" aunque mejores. La evolución de la marca debe compararse a nivel parecido. Las marcas tipo umbral (tu vistazo, tu freno, tu carga, costo de multitarea) no tienen este problema. |
| 7 | (Resuelto el 3-oct: ver DDA-comun.md §6.) Secuencia (16 niveles) y Parejas (10 niveles) tenían motores propios y su avance salía del nivel 1-5 antiguo. | Ya usan el DDA común: su escalera normalizada a 0..1, como los demás. |
| 8 | Con reloj / sin reloj cambia el tiempo disponible en algunos juegos (Acoplamiento 12 s sin reloj, Aterrizaje 10 s). | El mismo nivel es más fácil sin reloj. Ver decisión 3. |

## 2. Principios

1. **Una vara por juego, igual para todos.** La escalera es la tarea; el avance es tu lugar en ella a 8 de cada 10.
2. **La edad ajusta el entrenamiento, no la vara.** Es lo que la literatura respalda: aprendizaje con pocos
   errores en mayores (Baddeley y Wilson, 1994), enlentecimiento del procesamiento con la edad (Salthouse, 1996),
   aprendizaje óptimo con ~85% de aciertos (Wilson et al., 2019).
3. **Solo se compara lo comparable.** El avance y las marcas cambian con partidas jugadas en condiciones parecidas
   (regla que ya usamos en las medidas del final: `docs/medidas-juegos-estrella.md`).
4. **Pocos números, cada uno con un solo significado** y dicho en palabras simples.
5. **Nada se pierde de golpe**: las etapas logradas quedan con su fecha; el avance puede bajar, pero despacio.

## 3. La regla común: leer todo a "8 de 10"

El DDA común ya funciona como una escalera ponderada (Kaernbach, 1991): converge al nivel donde la persona
acierta la proporción buscada `p`. Si el desempeño en función del nivel sigue una curva logística con pendiente
`s` (cuánto caen los aciertos por nivel), el nivel donde se acierta `p` es:

`nivel(p) = θ − logit(p) / s`, con `logit(p) = ln(p / (1 − p))`.

De ahí sale todo:

- **Avance a 8 de 10** = `nivel(0,80) = nivel(p_buscado) + (logit(p_buscado) − logit(0,80)) / s`, llevado a 0-100%
  sobre la escalera del juego.
  - Adulto (busca 80%): sin corrección.
  - Mayor (busca 85%): + 0,35 / s niveles.
  - Ruta del Tesoro (busca 70%): − 0,54 / s niveles.
- **Dónde parte cada modo** = `nivel(p_modo) = nivel(p_buscado) + (logit(p_buscado) − logit(p_modo)) / s`.

`s` es la pendiente de cada juego. Valor inicial: 1 por nivel en las escaleras de 9 a 16 niveles y 2 en las de 5
a 7 (en esas cada nivel es un salto más grande). **Es un supuesto** y se calibra con datos (sección 10).

## 4. La matriz por edad

### 4.A Cómo entrena la edad (ya existe hoy, se mantiene)

| | Menos de 18 | 18 a 64 | 65 o más |
|---|---|---|---|
| Aciertos buscados "a tu medida" | 80% | 80% | 85% |
| Velocidad al subir (paso por acierto) | × 1,1 | × 1 | × 0,85 |
| Peso de la rapidez en el ajuste | 0,40 | 0,35 | 0,15 |
| Tiempos y tamaños (Parejas, Secuencia) | exposición ≥ 800 ms, blancos 17 mm, sin límites estrictos | ≥ 500 ms, 15 mm | ≥ 1500 ms, 20 mm, sin límites estrictos |
| Punto de partida estimado (sin evaluación) | referencia − 5 | referencia | referencia − 10 |

### 4.B Los modos, en aciertos esperados (lo que se iguala entre edades)

| Modo | Menos de 18 | 18 a 64 | 65 o más | Cómo funciona |
|---|---|---|---|---|
| **Suave** | 90% | 90% | 92% | El juego se ajusta como siempre, pero con **techo**: nunca pasa del nivel donde acertarías eso. |
| **A tu medida** | 80% | 80% | 85% | Libre: es el ajuste normal. |
| **Desafío** | 70% | 70% | 75% | Con **piso**: parte más arriba y no baja de ahí; si rindes más, sube. |
| **Experto** | 60% | 60% | 65% | Piso más alto. Se abre al **superar un Desafío** en ese juego. |

Los mayores tienen todo 5 puntos más arriba: la misma lógica de "pocos errores" que ya usa A tu medida. Así Desafío
se siente desafiante pero posible a cualquier edad.

### 4.C Dónde parte cada modo, en niveles (derivado de 4.B con `s` = 1)

| Modo | Menos de 18 y 18 a 64 | 65 o más |
|---|---|---|
| Suave (techo) | − 0,8 niveles | − 0,7 niveles |
| A tu medida | 0 | 0 |
| Desafío (piso) | + 0,5 niveles | + 0,6 niveles |
| Experto (piso) | + 1,0 nivel | + 1,1 niveles |

Se ve algo que parece raro y es correcto: al mayor su Desafío le sube un poco **más** de nivel que al adulto, aunque
espera más aciertos (75% contra 70%). Pasa porque parte desde 85%, más lejos del borde. Lo que se iguala es la
experiencia (cuántos aciertos), y los niveles son la consecuencia.

Un solo mecanismo en Unity: A tu medida con **piso** o **techo**. No hay que tocar la escalera de cada juego.

## 5. Qué cuenta para qué

| Modo | Tu avance | Tu marca (juegos estrella) | Planeta, racha, liga, logros |
|---|---|---|---|
| A tu medida (y la sesión diaria) | sí | sí | sí |
| Suave | no (nunca lo baja) | no | sí |
| Desafío o Experto **superado** | sí: sube | no | sí |
| Desafío o Experto no superado | no (nunca lo baja) | no | sí |
| Evaluación inicial | la fija (es su propósito) | no | no |

- **Superado** = después del calentamiento, acertaste al menos lo que buscas "a tu medida" (80%; 85% en mayores),
  con un mínimo de 12 ensayos (6 rondas en los juegos de rondas largas: Rumbo, Satélites, Bitácora, Correo). Si lo
  lograste con el piso puesto, tu nivel de 8 de 10 está sobre el piso: tu avance sube.
- **Cómo se mueve el avance**: ya se suaviza (60% la partida, 40% lo anterior). Se agrega un tope de bajada de
  5 puntos por partida: un mal día se nota, pero no borra semanas.
- **Las marcas** se guardan con el nivel y el reloj de la partida, y su evolución compara partidas de nivel parecido
  (± 1 nivel) y con el mismo reloj.

## 6. Lo que ve la persona

| Número | Qué es | Cómo se dice | Cuándo aparece |
|---|---|---|---|
| **Tu avance** | Lugar en la escalera del juego donde aciertas 8 de 10 (sección 3) | "45%" | Desde la primera partida A tu medida. Antes: "Sin medir aún". |
| **Etapa** | Quintos del avance: Inicio 0-19, Aprendiz 20-39, Hábil 40-59, Experto 60-79, Maestro 80-100 | "Hábil · a 5 puntos de Experto" | Con el avance. La etapa lograda queda con su fecha aunque el avance baje. |
| **Avance del área** | Promedio del avance de los juegos **medidos** del área | "Memoria · 51% · 4 de 6 juegos explorados" | En el selector de área. |
| **Tu marca** | La medida propia del juego estrella (`StarMeasures`) | "a 18% de casa" + últimas 6 a nivel parecido | Con 1 partida; la evolución, con 3. |
| **Tus fortalezas aquí** | Ver sección 8 | puntos 1 a 5 o una frase | Con 3 partidas A tu medida. |
| **Modo de hoy** | Sección 4.B | "Desafío · aciertas cerca de 7 de 10" | Botón de la carta y ventana antes de jugar. |
| Planeta, racha, liga, trofeos | Cuánto juegas | Como hoy | Siempre: todo suma. |

Qué **no** se muestra como avance: el porcentaje de aciertos de la partida (por diseño ronda 8 de 10) ni el puntaje
de la partida (sirve para la liga, no para el nivel).

Los nombres de las etapas (Inicio…Maestro) reemplazan a Principiante…Experto (`LevelTier`) en toda la app, para que
no haya dos escalas de nombres. La liga conserva sus metales (Bronce…Maestro), que miden otra cosa.

## 7. Por tipo de juego

| Tipo | Juegos | Cómo se aplica |
|---|---|---|
| DDA común, una escalera | Tinta o Palabra, Series, Cálculo, Anagramas, Radar, Freno, Aterrizaje, Acoplamiento, Tráfico, Satélites, Rumbo | Directo: piso o techo sobre el rating. |
| DDA común, objetivo propio | Ruta del Tesoro (70%) | Igual, con la corrección de la sección 3. |
| Dos escaleras | Piloto (pilotaje y señales), Correo (encargos y pilotaje) | El avance es el de la tarea que se mide: Piloto, el promedio de las dos (como hoy); Correo, la de encargos. El piso o techo se aplica a las dos. |
| (Ya no hay motores propios desde el 3-oct) | Secuencia (16 niveles), Parejas (10 niveles) | Pasaron al DDA común: directo, piso o techo sobre el rating, igual que los demás. |
| Sesión diaria | Bitácora de Misión | Solo A tu medida (su espera y su informe dependen de la sesión). |

## 8. Fortalezas

Como los aciertos son estables por diseño, las fortalezas no pueden salir de "cuánto aciertas". Propuesta:

- **Juegos estrella**: dos aspectos que el juego ya mide, en una escala fija de 1 a 5 puntos con cortes escritos
  en `docs/medidas-juegos-estrella.md`. Ejemplos: Rumbo (rumbo: error de dirección; distancia: error de largo),
  Radar (centro y periferia), Freno (rapidez al lanzar y freno), Aterrizaje (inicio, centro y final de la regla).
  Se calculan con las últimas 3 a 5 partidas A tu medida.
- **Todos los juegos**: **Constancia** = partidas en los últimos 14 días (1 punto cada 2, hasta 5).
- **Juegos clásicos**: en vez de puntos inventados, "Tu mejor etapa" y la constancia.

## 9. Ejemplo con números (Radar, 12 niveles, `s` = 1)

- **Diego, 30 años.** Su rating A tu medida (80%) queda en 5,8 → avance (5,8 − 1) / 12 = **40% · Hábil**.
  Desafío parte en 6,3 (acierta ~7 de 10). Suave nunca pasa de 5,0.
- **Rosa, 70 años.** Su rating A tu medida (85%) queda en 4,2 → leído a 8 de 10: 4,2 + 0,35 = 4,55 → avance
  **30% · Aprendiz**. Desafío parte en 4,8 (acierta ~7 u 8 de 10). Suave nunca pasa de 3,5 (acierta ~9 de 10).
- Rosa entrena con menos errores y una subida más pausada, y su 30% significa exactamente lo mismo que el de
  cualquier otra persona: el punto de Radar donde acierta 8 de 10.

## 10. Supuestos y cómo se calibran

| Supuesto | Valor inicial | Cómo se calibra |
|---|---|---|
| Pendiente `s` por juego | 1 por nivel (9-16 niveles), 2 (5-7) | Unity informa, por partida, ensayos y aciertos por nivel presentado; con muchas partidas se ajusta la curva de cada juego. |
| Aciertos de cada modo por edad | tabla 4.B | Revisar que Desafío se supere entre 30% y 60% de las veces; si no, mover 5 puntos. |
| Tope de bajada del avance | 5 puntos por partida | Mirar si genera avances "pegados" arriba. |
| Mínimo para "superado" | 12 ensayos (6 rondas) | Con datos, el menor número con resultado estable. |
| Grupo "menos de 18" | uno solo | Más adelante, separar niños (menos de 12) si la app se usa con ellos. |

Nada de esto es un diagnóstico ni está validado clínicamente. Son reglas de entrenamiento razonadas y escritas
para poder revisarlas.

## 11. Implementación por pasos (cada uno probable por separado)

1. **Lógica pura con pruebas** (`data/Skill.kt`): avance a 8 de 10, etapas, "a N puntos de", piso o techo por modo
   y edad, regla de "superado", tope de bajada, avance del área, "sin medir". Separar el punto de partida del
   avance medido.
2. **Unity**: la configuración trae `mode` + piso o techo (0..1); `AdaptiveDifficulty` los respeta; la telemetría
   dice si se superó y los ensayos por nivel. Adaptadores para Secuencia y Parejas. Cambiar `StyleStamp`. Requiere
   reexportar en el PC de Ricardo.
3. **Pestaña Juegos**: selector de área (flechas y deslizar) + cartas + ventana "¿Cómo quieres jugar hoy?".
4. **Limpieza**: sacar el modo de dificultad de Ajustes y la ventana antigua; el nivel 1-5 se deriva del avance;
   nombres de etapa en toda la app (pantalla de carga, Liga, resultado).
5. **Marcas**: guardar modo, nivel y reloj con cada marca; evolución a nivel parecido.

## 12. Decisiones para Ricardo

1. ¿Te cierra la tabla 4.B (aciertos esperados por modo y edad)?
2. ¿Experto se abre al superar un Desafío en ese juego (recomendado: justo para cualquier edad) o siempre?
3. Reloj: **recomiendo sacarlo de la ventana antes de jugar** y dejarlo en Ajustes como hoy. Hay menos decisiones
   para la persona y el avance y las marcas se miden siempre igual. La alternativa es dejarlo en la ventana y
   separar las marcas por reloj.
4. ¿Sacamos el modo de dificultad de Ajustes (Principiante, Intermedio…), que hoy casi no tiene efecto?

## Referencias

- Baddeley, A. y Wilson, B. A. (1994). When implicit learning fails: amnesia and the problem of error elimination.
  *Neuropsychologia*, 32, 53-68.
- Kaernbach, C. (1991). Simple adaptive testing with the weighted up-down method. *Perception & Psychophysics*,
  49, 227-229.
- Levitt, H. (1971). Transformed up-down methods in psychoacoustics. *JASA*, 49, 467-477.
- Salthouse, T. A. (1996). The processing-speed theory of adult age differences in cognition. *Psychological
  Review*, 103, 403-428.
- Wilson, R. C., Shenhav, A., Straccia, M. y Cohen, J. D. (2019). The eighty five percent rule for optimal
  learning. *Nature Communications*, 10, 4646.
- Ryan, R. M. y Deci, E. L. (2000). Self-determination theory and the facilitation of intrinsic motivation.
  *American Psychologist*, 55, 68-78 (elegir cómo jugar sostiene la motivación).
- Detalle del DDA común: [`docs/DDA-comun.md`](DDA-comun.md). Medidas de los juegos estrella:
  [`docs/medidas-juegos-estrella.md`](medidas-juegos-estrella.md).
