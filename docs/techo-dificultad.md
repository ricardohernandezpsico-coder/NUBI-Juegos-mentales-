# Techo de la dificultad (informe del 4-nov-2026)

Pedido de Opus: tres de las cinco personas que probaron la app juegan otros juegos cerca de una hora al día y dijeron que los dejan porque «se vuelven muy fáciles». ¿Pasa lo mismo en Nubilus? Este informe es solo análisis: no cambia nada de la app ni de Unity. La simulación está en [`tools/techo_dificultad.py`](../tools/techo_dificultad.py) (Python, 25 s; con `--json` deja los números crudos). La sección 8 (la liga de la semana con 10 divisiones) se calcula con [`tools/techo_contrarreloj_liga.py`](../tools/techo_contrarreloj_liga.py).

## 0. En pocas líneas

- **Sí, en varios juegos el techo llega en la primera o segunda semana.** Una persona adulta que acierta el 95 % en todos los niveles toca el último nivel en **1 a 2 partidas** de «A tu medida» en Carga exacta, En la punta de la lengua, Satélites, Tinta o Palabra, Engranajes, Acoplamiento, Piloto Estelar y La estrella intrusa; y **empieza ya arriba** (la partida arranca en el último nivel) entre la 2.ª y la 5.ª partida. Con 65 años o más tarda 0 a 3 partidas más.
- **Las escaleras cortas (5 niveles) son lo más urgente.** Tinta o Palabra, Carga exacta y En la punta de la lengua tienen 5 niveles y un paso de 0,2 a 0,4 por acierto: el último nivel se toca en la 1.ª o 2.ª partida. Además, en esos tres, «Maestro» (el 80 % del avance) **ES** el nivel 5: llegar al último nivel y llegar a «Maestro» es lo mismo.
- **En el Contrarreloj pasa a mitad de la partida.** Tinta o Palabra llega al último paso (el 5.º, a los 32 aciertos) hacia el **segundo 47 a 56** de los 120 en una persona de 18 a 29 años del percentil 50 al 90, y el resto de la partida repite el último paso. ¿Verdad o disparate? (paso 12 a los 33 aciertos) y Acoplamiento (paso 12 a los 22 aciertos) lo alcanzan hacia el **segundo 79 a 98**. Rescate relámpago no llega nunca (le faltarían unos 260 segundos).
- **Los puntos no son el problema de hoy, pero lo serán si se alarga una escalera sin cuidado**: los puntos del juego salen de la posición en la escalera (0 a 1). Si se pasa, por ejemplo, de 5 a 8 niveles y se conserva el nivel de cada persona, **todos bajan un 37,5 %** (§6). Hay que alargar y a la vez anclar los puntos (propuesta en §6).
- **Los tres juegos de memoria de escalera larga (Rastro de luz, Constelaciones, Bodega de carga) no son el problema**: su último nivel está cerca del límite humano de memoria de trabajo y tardan 5 a 11 partidas en tocarse.

Lo que este informe NO prueba: nadie acierta el 95 % en todos los niveles de un juego de capacidad (arriba, la precisión baja y el motor se equilibra); es el peor caso que pidió Opus, para saber qué tan rápido se agota la escalera de alguien muy bueno. La prueba de verdad es la de §7.

## 1. Cómo se midió

- **El motor** es el de `Games/AdaptiveDifficulty.cs` ([`DDA-comun.md`](DDA-comun.md) §3), portado a Python: sube `δ · calibración` por acierto (`δ` = paso del juego, ×0,85 en mayores; ×1,5 en los 6 primeros ensayos de cada partida), baja `δ · p/(1−p)` por error con tope de 1,0 (p = 0,80; 0,85 en mayores), `+0,20` cada 6 aciertos seguidos, `−0,15` extra al segundo error seguido, y el rating nunca pasa de `MaxLevel + 0,99`. Se ignora el tiempo de reacción (solo lo usan Freno y Tinta con reloj, y a favor de la persona rápida: ignorarlo es lo prudente).
- **Entre partidas** (`Skill.nextRating`, `blendDdaRating`): lo guardado es 60 % de cómo terminó la partida y 40 % de lo anterior, y la siguiente parte de ahí. Por eso «tocar» el último nivel dentro de una partida (columna «toca») llega antes que «empezar arriba» (columna «arriba desde»).
- **La persona nueva** parte del nivel 1 (`getEffectiveLevelForGame` da 1 sin historial) y su primera partida no tiene la subida guiada, porque ya parte en lo más fácil. Se simulan 3.000 personas por celda; se informa la mediana.
- **Una partida «A tu medida»** es la SIN reloj (el camino diario va siempre sin reloj y los 4 juegos del Contrarreloj ya no ofrecen «¿Con reloj?»). Ensayos por partida (cada uno según lo que el juego le da al motor, `_dda.Register`): Constelaciones 24 (6 cielos de unas 3 a 12 oportunidades: cifra media), Rastro 14, Bodega 6, Correo 10 (4 días de 1 a 5 encargos), Tinta 16, Piloto 24 señales y unas 40 ventanas de pilotaje (estimado), Freno 28 (los de ir: 40 con 30 % de altos), Satélites 8, Rescate 12, Carga 8, Engranajes 10, Acoplamiento 24, Aterrizaje 15, Punta 8, Meteoros 40, Disparate 30, Intrusa 20. Son las constantes de cada contrato (`PrecisionTrials`, `PrecisionRounds`…); las estimadas están marcadas.
- **Sensibilidad**: también 90 % de aciertos (tarda más en las escaleras largas) y partiendo de la mitad de la escalera (el caso de quien ya jugó semanas).

## 2. Una tabla por juego

«Toca» = ensayos y partida en que el rating llega al último nivel dentro de una partida; «arriba desde» = la primera partida que ya EMPIEZA en el último nivel. Personas que aciertan el 95 % en todos los niveles (mediana de 3.000). Partidas contadas desde la primera. «Fácil» = se vuelve fácil: sí (ya pasa en ≤ 3 partidas), en riesgo (3 a 5 partidas o último nivel de conocimiento que una persona con estudios domina), no (≥ 5 partidas y el último nivel está cerca de un límite humano).

| Juego (área) | Escalera | Qué pide el último nivel | Adulto: toca · arriba desde | 65+: toca · arriba desde | Adulto 95 % desde la mitad | ¿Fácil? |
|---|---|---|---|---|---|---|
| Constelaciones (Memoria) | 18 niveles, paso 0,20 | cielos de 24 luces con gemelos, parejas y tríos | 114 ens.: partida 5 · desde la 7 | partida 7 · desde la 9 | partida 3 · desde la 5 | **no** (límite de memoria) |
| Rastro de luz (Memoria) | 16, paso 0,25 (0,17 mayores) | repite 8 luces seguidas y 7 al revés | 80 ens.: partida 6 · desde la 8 | partida 7 · desde la 10 | partida 3 · desde la 5 | **no** (límite de memoria) |
| Bodega de carga (Memoria) | 12, paso 0,20 | recuerda 7 objetos en 10 escotillas, con la bodega girando y 2 cajas que cambian | 65 ens.: partida 11 · desde la 13 | partida 14 · desde la 16 | partida 5 · desde la 7 | **no** (límite de memoria) |
| Correo Estelar (Memoria) | 10, paso 0,30 | ordena cartas en 4 buzones y cumple 5 encargos al día | 33 ens.: partida 4 · desde la 6 | partida 4 · desde la 7 | partida 1 · desde la 4 | en riesgo |
| Tinta o Palabra (Atención) | **5**, paso 0,20 | alterna tinta y palabra sin aviso (≈ 50 % de cambios, 85 % de palabras que chocan, la tarjeta llega en 0,24 s) | 19 ens.: partida 2 · desde la 3 | partida 2 · desde la 4 | partida 1 · desde la 3 | **sí** |
| Piloto Estelar (Atención) | 9, señales 0,25 y pilotaje 0,15 | atrapa señales parecidas de 0,65 s mientras pilota | señales 30 ens.: partida 2 · desde la 4; pilotaje 55 ens.: partida 2 · desde la 4 | partida 2 · desde la 4 | partida 1 · desde la 3 | **sí** |
| Freno de Emergencia (Atención) | 12, paso 0,15 | elige entre 4 plataformas y lanza en 0,8 s (con reloj) | 83 ens.: partida 3 · desde la 6 | partida 4 · desde la 7 | partida 2 · desde la 4 | en riesgo |
| Satélites (Atención) | 12, **paso 1,0** | sigue 5 satélites a la vez, entre 13 | 8 ens.: partida 1 · desde la 2 | partida 2 · desde la 3 | partida 1 · desde la 3 | **sí** en el motor; el último nivel es duro (ver §5) |
| Rescate relámpago (Atención) | 12, paso 0,30 | rescata 3 de 4 cápsulas con un destello de 0,1 segundos | 42 ens.: partida 4 · desde la 6 | partida 5 · desde la 7 | partida 2 · desde la 4 | en riesgo |
| Carga exacta (Razonamiento) | **5**, paso 0,40 | llega a la carga con las cuatro operaciones y más números | 7 ens.: **partida 1** · desde la 2 | partida 2 · desde la 3 | partida 1 · desde la 3 | **sí** |
| Engranajes (Razonamiento) | 12, paso 0,50 | arregla máquinas de 3 piezas y 4 correas | 19 ens.: partida 2 · desde la 5 | partida 3 · desde la 5 | partida 1 · desde la 3 | **sí** |
| Acoplamiento (Razonamiento) | 12, paso 0,30 | gira piezas de 7 bloques, en giros finos (hasta media vuelta) | 35 ens.: partida 2 · desde la 4 | partida 2 · desde la 5 | partida 1 · desde la 3 | **sí** (y el Contrarreloj lo toca) |
| Aterrizaje Lunar (Razonamiento) | 12, paso 0,30 | ubica números negativos entre menos 50 y 50 | 39 ens.: partida 3 · desde la 5 | partida 4 · desde la 6 | partida 1 · desde la 4 | en riesgo |
| En la punta de la lengua (Lenguaje) | **5**, paso 0,40 | encuentra palabras de 8 letras o más | 7 ens.: **partida 1** · desde la 2 | partida 2 · desde la 3 | partida 1 · desde la 3 | **sí** |
| Lluvia de meteoros (Lenguaje) | 12, paso 0,15 | reconoce palabras raras (cuánto se reconocen según SPALEX, no por uso) | 72 ens.: partida 2 · desde la 5 | partida 3 · desde la 5 | partida 1 · desde la 4 | en riesgo |
| ¿Verdad o disparate? (Lenguaje) | 12, paso 0,15 | distingue verdades de disparates en frases que comparan dos cosas | 84 ens.: partida 3 · desde la 5 | partida 4 · desde la 6 | partida 2 · desde la 4 | en riesgo (y el Contrarreloj lo toca) |
| La estrella intrusa (Lenguaje) | 12, paso 0,40 | detecta intrusas con trampa en reglas menos obvias | 25 ens.: partida 2 · desde la 4 | partida 2 · desde la 4 | partida 1 · desde la 3 | **sí** en el motor |

Con 90 % de aciertos tarda casi igual en las escaleras cortas y entre un tercio y la mitad más en las largas (Tinta, Carga y Punta siguen tocando el techo en la 1.ª o 2.ª partida; Engranajes y Acoplamiento en la 2.ª o 3.ª; Rastro en la 9.ª, Constelaciones en la 8.ª, Bodega en la 16.ª): los números completos salen de `python -I tools/techo_dificultad.py`.

## 3. Qué pasa al llegar arriba

- **El juego se queda igual.** El nivel que se presenta no pasa de `MaxLevel` (`Level = min(MaxLevel, floor(Rating))`) y el rating se queda en `MaxLevel + 0,99`: lo que viene es otra tanda del último nivel, con contenido al azar. Los modos «Desafío» («un paso más») y «Al límite» («dos pasos más») **tampoco** pasan del último nivel: su piso se acota al techo de la escalera.
- **El bono contra el aburrimiento** (`+0,20` cada 6 aciertos seguidos) ya no mueve nada arriba: el rating está en el tope. Debajo del techo SÍ acelera la subida: una persona del 95 % tiene una racha de 6 casi cada 4 ensayos.
- **Los puntos del juego quedan en el máximo.** El avance guardado tiende a `(MaxLevel + 0,99 − 1) / MaxLevel` = 0,998 (5 niveles), 0,999 (9 y 10), 0,9992 (12), 0,9994 (16 y 18). `Skill.progress` suma además el desfase de la edad (0 en adultos, positivo en mayores, que se acota a 1), así que el juego aporta 249 a 250 de 250 puntos (199 a 200 de 200 en Atención). No hay nada más que ganar: ningún número se mueve.
- **Etapa «Maestro»** (el 80 % de los puntos de un juego: `GamePoints.stage`, quintos de su máximo): el avance 0,8 equivale a la posición `1 + 0,8 · MaxLevel`. En las escaleras de 5 niveles es el nivel 5, o sea el último; en las de 12, el nivel 10,6.

## 4. El Contrarreloj (las 4 pistas)

La pista es la misma para todas las personas, parte siempre del paso 1 y **se queda en el último paso** una vez que llega (`TimeTrial.LevelFor`: `min(MaxLevel, 1 + aciertos / cada_N)`): sí, el último paso se repite hasta el segundo 120, con contenido nuevo al azar. Las tablas de la referencia estimada (`app/src/main/assets/referencia/*.json`) dicen cuántas marcas (aciertos menos errores, la mejor de 3 partidas) hace cada franja; con eso se estima en qué segundo cae el último paso, **suponiendo un ritmo parejo** (segundo ≈ 120 · aciertos_del_último_paso / (marca + 2 errores)). Es una cota inferior: en Acoplamiento y Disparate los pasos altos son más lentos y el último llega algo más tarde.

| Pista | Último paso | Marca p50 / p90 (18 a 29) | Segundo del último paso (p50 / p90) | Franjas que lo alcanzan con su marca p50 | Qué pasa después |
|---|---|---|---|---|---|
| Tinta o Palabra | paso 5, a los 32 aciertos (uno cada 8) | 67 / 80 | **56 / 47** (65 a 74 años: 78 / 62; 75+: 89 / 69) | las seis; el 86 % de 75+ ya en su marca | se repite el paso 5 entre 64 y 73 segundos |
| ¿Verdad o disparate? | paso 12, a los 33 (uno cada 3) | 41 / 48 | **92 / 79** (55 a 64: 107 / 88; 65 a 74: pasa de 120 en p50; 97 en p90) | 18 a 64 años; el 92 % de 18 a 29 | se repite el paso 12 entre 28 y 41 segundos |
| Acoplamiento | paso 12, a los 22 (uno cada 2) | 25 / 30 | **98 / 82** (45 a 54: 106 / 88; 65 a 74: 132 / 102) | 18 a 54 años; el 84 % de 18 a 29 | se repite el paso 12 entre 22 y 38 segundos |
| Rescate relámpago | paso 12, a las 33 RONDAS logradas (una cada 3) | 28 / 33 cápsulas | **no llega**: una ronda dura unos 8 s y caben unas 14 en 120: paso 5 como mucho | ninguna | — |

Así, **el Contrarreloj tiene techo en tres de las cuatro pistas para una persona buena de 18 a 54 años** (en Tinta, para casi cualquiera). Con la marca neta, lo que sigue premiando es ser más rápido y no errar, no pasar de paso: la persona muy buena ve el mismo paso durante el último tercio o la última mitad de la partida.

## 5. ¿El último nivel está cerca del límite humano?

Fuentes verificadas en PubMed (cada una con su DOI): Cowan 2001, la capacidad de la memoria de corto plazo se resume en unos 4 elementos (3 a 5), [10.1017/s0140525x01003922](https://doi.org/10.1017/s0140525x01003922); Luck y Vogel 1997, la memoria de trabajo visual retiene unos 4 elementos, [10.1038/36846](https://doi.org/10.1038/36846); Alvarez y Franconeri 2007, seguir objetos que se mueven: unos 4 en la literatura clásica, hasta 8 si van lentos y 1 si van muy rápido, [10.1167/7.13.14](https://doi.org/10.1167/7.13.14); Kessels y cols. 2000, datos normativos de las tareas de bloques de Corsi en adultos sanos, [10.1207/S15324826AN0704_8](https://doi.org/10.1207/S15324826AN0704_8); Kiesel y cols. 2010, el costo de cambiar de tarea persiste aun con preparación, [10.1037/a0019842](https://doi.org/10.1037/a0019842). Lo demás es criterio de diseño y se dice así.

| Juego | Veredicto | Por qué |
|---|---|---|
| Rastro de luz | **cerca del límite** | 8 luces seguidas y 7 al revés exceden lo que la memoria de trabajo retiene sin trucos (Cowan 2001; los bloques de Corsi, Kessels 2000). Una persona buena sube de nivel y el motor la equilibra por precisión: no se vuelve fácil. |
| Bodega de carga | **cerca del límite** | 7 objetos en 10 escotillas con la bodega girando: arriba de los ~4 elementos de Luck y Vogel 1997. |
| Constelaciones | **cerca del límite** (criterio de diseño) | 24 luces con gemelos, parejas y tríos: carga alta, pero las parejas y los tríos se pueden agrupar como trozos, así que el límite es más flexible que el de Rastro o Bodega. |
| Satélites | **cerca del límite** | 5 satélites entre 13 a la velocidad del juego: el tope clásico es ~4 y baja si van rápido (Alvarez y Franconeri 2007). El motor, en cambio, sube 1 nivel por ronda perfecta (paso 1,0): se recorren 12 niveles en 8 a 10 rondas, mucho antes de lo que justifica la capacidad. Es más un problema del paso que del último nivel. |
| Tinta o Palabra | **no** | Alternar tinta y palabra al azar cuesta tiempo (Kiesel 2010) y eso no desaparece, pero el nivel 5 no es un límite de capacidad: es el final de una escalera corta (5 niveles con pasos grandes entre uno y otro). Criterio de diseño: sin un dato de cuántos aciertos hace una persona entrenada en el nivel 5, no puedo decir que esté cerca del máximo. |
| Rescate relámpago | **no demostrado** | 3 de 4 cápsulas en 0,1 s está cerca de lo que ve el ojo sin movimiento, pero no tengo fuente verificada para el valor; criterio de diseño. |
| Carga exacta, En la punta de la lengua, Aterrizaje Lunar, ¿Verdad o disparate?, La estrella intrusa, Lluvia de meteoros | **no** | Dependen del conocimiento y no de un límite de capacidad: aritmética, vocabulario, números en la recta, comprensión de frases, categorías. El último nivel no es difícil para una persona con estudios que juega mucho; el techo sube con más contenido, no con una capacidad más grande. |
| Piloto, Freno, Acoplamiento, Engranajes, Correo | **no demostrado** | Velocidad y coordinación (Piloto, Freno), rotación mental (Acoplamiento), razonamiento mecánico (Engranajes), organización (Correo). Sin fuente verificada de un «máximo humano»; criterio de diseño. |

## 6. Cómo alargar la escalera sin cambiar la tarea, y cuidado con los puntos

### 6.1 Qué agregar arriba (la tarea se queda igual)

Mismo juego, más exigente: se suman niveles al final con parámetros que ya existen en cada contrato.

| Juego | Hoy | Niveles nuevos (misma tarea) |
|---|---|---|
| Tinta o Palabra (5) | tarjeta de 360 a 240 ms, 60 a 85 % de palabras que chocan, cambios hasta 50 % | 3 niveles más: tarjeta de 220 a 180 ms, 90 % de choques, cambios al 60 %; o una 3.ª tinta que se alterna |
| Carga exacta (5) | sumas → las 4 operaciones y más números | 3 más: más operandos, números mayores, un paso intermedio que se arrastra |
| En la punta de la lengua (5) | palabras de 4 a 8+ letras | 3 más: palabras de 9 a 11 letras, más raras (menos frecuentes) y con letras repetidas |
| Acoplamiento (12) | piezas de 4 a 7 bloques, hasta media vuelta | piezas de 8 y 9 bloques; giros espejo; plazos más cortos |
| Engranajes (12) | hasta 3 piezas y 4 correas | 5 y 6 correas; máquinas encadenadas |
| ¿Verdad o disparate? (12) | frases cortas → que comparan dos cosas | frases más largas, con doble negación o con datos |
| La estrella intrusa (12) | reglas obvias → «con trampa» | reglas más abstractas y grupos más grandes |
| Satélites (12, paso 1,0) | 5 entre 13 | bajar el paso (0,5 o 0,3, como los otros) antes que alargar; y ver si 5 entre 13 ya es el límite |
| Aterrizaje, Meteoros, Piloto, Freno, Rescate, Correo | 9 a 12 niveles | alargar solo si la persona real los agota: hoy tardan 3 a 5 partidas |
| Rastro, Constelaciones, Bodega | 12 a 18 niveles, cerca del límite | **no alargar** |

### 6.2 El efecto en los puntos (importa más que el contenido)

Los puntos de un juego salen de `Skill.progress = rating_guardado + desfase_de_edad / niveles`, y el rating guardado es `(rating − 1) / MaxLevel`, es decir, **la posición en la escalera como fracción** (`LevelLines.levelOf` usa lo mismo). Si `MaxLevel` crece sin tocar lo guardado, pasan dos cosas, las dos malas:

1. **Sin migrar** lo guardado: la misma fracción apunta a otro nivel. Una persona en la fracción 0,60 de una escalera de 5 estaba en el nivel 4 y, con 8 niveles, cae en el nivel 5,8: de golpe, más difícil de lo que juega; y quien estaba arriba (≈ 1,0) queda en el último nivel NUEVO, el más difícil, sin haberlo visto.
2. **Migrando para conservar el nivel** (`r' = r · L_viejo / L_nuevo`, como se hizo con Constelaciones 12 → 13, `translateOldRating`, y con Correo 13 → 14): el nivel se conserva, **pero el avance baja** en la proporción `L_viejo / L_nuevo`. De 5 a 8 niveles: **−37,5 % de los puntos de todos** (quien tenía 250 de 250 pasa a 156); de 12 a 16: −25 %; de 9 a 12: −25 %. Quien estaba en «Maestro» baja dos etapas.

**Propuesta (no implementada), en orden de preferencia:**

- **A. Anclar los puntos a la escalera ORIGINAL y mostrar lo nuevo aparte (recomendada).** Cada juego guarda la versión de su escalera (`ladderVersion`) y la migración conserva el nivel (`r'` arriba). Los puntos se calculan con el largo de la escalera *de la versión 1*: `avance = min(1, posición_en_niveles / L_v1)`. Así nadie baja nunca (el último nivel de la v1 sigue valiendo el máximo) y los niveles nuevos se dicen aparte («Más allá de Maestro: 3 niveles»), sin sumar puntos ni cambiar la escala de 0 a 1000 de las áreas. La escalera larga solo alarga el juego, no el puntaje.
- **B. Repartir el avance: los niveles nuevos valen una parte del máximo y los viejos, el resto.** Por ejemplo, los 5 niveles viejos valen hasta 0,625 del juego y los 3 nuevos, 0,375 más: se migra `r'` y se compensa con un piso (`puntos = max(puntos_antes_de_migrar, puntos_nuevos)`) hasta que el avance nuevo lo alcance. Evita que baje la cifra, pero la persona de arriba queda 8 a 12 semanas sin que se mueva nada hasta pasar su piso: se siente parada.
- **C. Alargar sin tocar el techo (más niveles adentro).** Subdividir niveles existentes (pasos más finos entre dos niveles). No cambia el tope, así que no resuelve «se vuelve fácil»: solo sirve para escaleras que suben demasiado rápido (Satélites).
- **No hacer:** alargar y migrar con `r · L_viejo / L_nuevo` sin más. Es lo que se hizo con Constelaciones y Correo cuando los puntos todavía no existían (Tarea 159); hoy cada persona vería una caída de puntos de 25 a 40 % sin haber hecho nada.

## 7. Lista ordenada: qué juegos urge alargar

1. **Carga exacta** — 5 niveles, el techo llega en la 1.ª partida y «Maestro» es el nivel 5. Lo más corto de la app.
2. **Tinta o Palabra** — 5 niveles y además el Contrarreloj llega al último paso al segundo 47 a 56. Dos techos juntos.
3. **En la punta de la lengua** — 5 niveles, el techo llega en la 1.ª partida.
4. **Acoplamiento** — techo en 2 a 4 partidas y el Contrarreloj lo alcanza hacia el segundo 82 a 98.
5. **Engranajes** — toca el techo en la 2.ª partida (paso 0,5: dos aciertos seguidos suben un nivel).
6. **¿Verdad o disparate?** — 3 a 5 partidas y el Contrarreloj lo alcanza hacia el segundo 79 a 92; el último nivel es de conocimiento y lo domina una persona con estudios.
7. **La estrella intrusa** — 2 a 4 partidas en el motor; el contenido de las reglas difíciles es fácil de ampliar.
8. **Satélites** — no alargar: **bajar el paso de 1,0**; con 1,0 se recorren los 12 niveles en 8 rondas perfectas.
9. **Lluvia de meteoros**, 10. **Aterrizaje Lunar**, 11. **Piloto Estelar** — techo en 3 a 5 partidas; ampliar solo si la prueba real (abajo) lo confirma.
12. **Freno de Emergencia**, 13. **Rescate relámpago**, 14. **Correo Estelar** — techo en 4 a 6 partidas; no urgen.
15. **Rastro de luz**, 16. **Constelaciones**, 17. **Bodega de carga** — no alargar: techo cerca del límite humano y 5 a 11 partidas.

**Antes de implementar nada**, lo que falta saber y no sale de una simulación: (1) cuánto aciertan DE VERDAD las 3 personas que se aburren en el último nivel de cada juego (la bitácora `progress_log` y el historial lo dicen: ver cuántas partidas llevan al techo y qué porcentaje aciertan ahí); (2) si «se vuelve fácil» es por el techo de la escalera, o por la falta de novedad (la sensación de repetir lo mismo): las dos cosas parecen iguales en una entrevista y se arreglan distinto; (3) el factor edad: con 65 años o más el techo llega 1 a 3 partidas más tarde, así que para esa persona el problema aparece más tarde.

## 8. La liga de la semana: ¿alcanza la pista para separar bien arriba?

Ricardo decidió que la liga de la semana tendrá **10 divisiones**, con líneas cada 10 % del «superas al N %» promedio de la semana (0, 10, 20 … 90). Esta sección dice, por juego del Contrarreloj y por franja de edad, **a qué marca corresponde cada línea** (con la regla exacta de la app: `Comparison.percentile` y `firstMarkReaching`, sobre los tramos sin suavizar, hacia abajo y con tope 99, y la referencia estimada de `app/src/main/assets/referencia/*.json`), **a qué paso de la pista llega esa marca** y **cuánto tiempo se queda la persona en el último paso**. Se calcula con [`tools/techo_contrarreloj_liga.py`](../tools/techo_contrarreloj_liga.py) (aciertos = marca + 2 errores; el segundo del último paso es proporcional: cota inferior, ver §4; en Rescate la marca son cápsulas y se usan 2,7 por ronda).

### 8.1 Conclusiones

1. **Las 10 divisiones caben**: las nueve líneas de 10 a 90 caen en **marcas distintas en los 4 juegos y las 6 franjas** (no queda ninguna división vacía), y el 99 % cae dentro de la pista (Tinta 93, Disparate 55, Acoplamiento 34 y Rescate 39 en 18 a 29 años, contra el borde de 100, 80, 40 y 48).
2. **Lo que separa arriba es rapidez y no errar, no el paso de la escalera.** En **Tinta o Palabra** la línea del 10 % de las franjas de 18 a 64 años ya está en el último paso (el 5.º, a los 32 aciertos): desde el 10 % hasta el 99 % la dificultad es la misma y la persona se queda en el paso 5 entre 71 y 80 segundos de los 120 en las líneas del 80 al 99 % (18 a 29 años). En **¿Verdad o disparate?** y **Acoplamiento** el 80, el 90 y el 99 % ya están en el paso 12 de 12: llegan a él entre el segundo 69 y el 91 de los 120 en 18 a 54 años y se quedan pegados entre 29 y 51 segundos (el 99 % de 18 a 29 años: segundo 69 y 73, y 51 y 47 segundos en el último paso). **Rescate relámpago NO se pega nunca**: aun el 99 % llega solo al paso 5 o 6 de 12.
3. **Las líneas de 80 y 90 % quedan muy juntas en Acoplamiento y Rescate**: entre una y otra hay **1 a 2 aciertos** (18 a 29 años: Acoplamiento 30 y 31; Rescate 33 y 35 cápsulas). Un solo error o una sola cápsula cambia la división de la persona, y en la regla de la marca un error cuesta 2 (el que no se suma y el que se resta). Tinta separa bien (80→90 = 4 a 6 aciertos; 90→99 = 11 a 14) y Disparate bastante bien (2 a 4 y 5 a 9).
4. **Las franjas de más edad se pegan más tarde, pero se pegan**: en 65 a 74 años, el 80 % llega al último paso de Disparate en el segundo 99 y al de Acoplamiento en el 106; en 75 o más, en el 110 y el 115 (quedan 10 y 5 segundos): ahí casi no hay «pegado», pero tampoco hay lugar para ir más arriba.

### 8.2 Qué implica para la liga (solo análisis, sin tocar nada)

- **Tinta o Palabra y ¿Verdad o disparate? separan bien arriba por marca**; Acoplamiento y Rescate separan **mal** el 80 y el 90 %: las dos líneas distan 1 a 2 aciertos, lo que el ruido de una partida mueve solo. Conviene que la liga use **el promedio de varios juegos y de la semana** (como dice la regla de Ricardo) y no la marca de un solo juego, y que la línea de una persona no cambie de división por una sola partida (una regla de «sube al promediar 3 partidas» o un margen de histéresis de ±1 acierto en esos dos juegos).
- **Pegado al último paso arriba = menos sensación de avance.** Para alguien del 80 % o más en Tinta, Disparate o Acoplamiento, los últimos 40 a 70 segundos de la partida son siempre el mismo paso; una escalera más larga (§6) podría separar más (un paso más difícil probablemente enlentece a los muy buenos, pero eso solo se sabe midiéndolo), y antes hay que anclar los puntos antes (§6.2) y **versionar la pista del Contrarreloj** (`TimeTrial.Track.Version`): cambiar la escalera del Contrarreloj reinicia los récords y la referencia estimada hay que recalcularla (`bash tools/verificar-todo.sh --referencia`).
- **Una pregunta de definición para la spec**: la referencia es la **mejor marca de 3 partidas por semana** de cada robot. Si el «superas al N % promedio de la semana» promedia el percentil de TODAS las partidas de la persona (no su mejor), queda subestimado frente a esa referencia (se compara un promedio contra una mejor-de-3): una persona que juega 3 veces y saca 70, 60 y 50 compara su 60 de promedio contra los mejores de 3 de los robots. Mejor: el promedio de los percentiles de la MEJOR marca de la semana de cada juego que jugó (así se compara una mejor-de-N contra una mejor-de-3), o decir en la liga «tu mejor de la semana».

### 8.3 Las tablas

Marca (aciertos menos errores; en Rescate, cápsulas) con la que se alcanza cada línea, por franja de edad. «Paso» es el paso de la pista de esa marca (aciertos = marca + 2). «Último paso (s)» es el segundo en que se llega al último paso y, entre paréntesis, cuántos segundos quedan ahí: «no llega» si la marca no alcanza el último paso. Las franjas son las 6 de la referencia; 14 a 17 años usa la de 18 a 29.

#### Tinta o Palabra — 5 pasos, el último a los 32 aciertos (uno cada 8); tramos de a 5, borde de la pista en 100

| Franja | 10 % | 20 % | 30 % | 40 % | 50 % | 60 % | 70 % | 80 % | 90 % | 99 % | paso en 80 % · 90 % · 99 % | último paso (s) en 80 % · 90 % · 99 % |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 18-29 | 55 | 59 | 62 | 65 | 68 | 70 | 73 | 76 | 82 | 93 | 5 de 5 · 5 de 5 · 5 de 5 | 49 (quedan 71) · 46 (quedan 74) · 40 (quedan 80) |
| 30-44 | 53 | 57 | 60 | 63 | 65 | 68 | 70 | 74 | 78 | 90 | 5 de 5 · 5 de 5 · 5 de 5 | 51 (quedan 69) · 48 (quedan 72) · 42 (quedan 78) |
| 45-54 | 50 | 54 | 56 | 59 | 61 | 64 | 67 | 70 | 75 | 87 | 5 de 5 · 5 de 5 · 5 de 5 | 53 (quedan 67) · 50 (quedan 70) · 43 (quedan 77) |
| 55-64 | 44 | 48 | 51 | 54 | 56 | 59 | 62 | 65 | 70 | 82 | 5 de 5 · 5 de 5 · 5 de 5 | 57 (quedan 63) · 53 (quedan 67) · 46 (quedan 74) |
| 65-74 | 37 | 41 | 43 | 46 | 48 | 51 | 54 | 57 | 61 | 73 | 5 de 5 · 5 de 5 · 5 de 5 | 65 (quedan 55) · 61 (quedan 59) · 51 (quedan 69) |
| 75+ | 31 | 34 | 37 | 40 | 42 | 44 | 47 | 50 | 55 | 69 | 5 de 5 · 5 de 5 · 5 de 5 | 74 (quedan 46) · 67 (quedan 53) · 54 (quedan 66) |

Separación en aciertos entre líneas: 18-29: 80→90 = 6, 90→99 = 11 · 30-44: 80→90 = 4, 90→99 = 12 · 45-54: 80→90 = 5, 90→99 = 12 · 55-64: 80→90 = 5, 90→99 = 12 · 65-74: 80→90 = 4, 90→99 = 12 · 75+: 80→90 = 5, 90→99 = 14

Franjas con dos líneas de 10 a 90 en la MISMA marca: ninguna

#### ¿Verdad o disparate? — 12 pasos, el último a los 33 aciertos (uno cada 3); tramos de a 5, borde de la pista en 80

| Franja | 10 % | 20 % | 30 % | 40 % | 50 % | 60 % | 70 % | 80 % | 90 % | 99 % | paso en 80 % · 90 % · 99 % | último paso (s) en 80 % · 90 % · 99 % |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 18-29 | 35 | 37 | 39 | 41 | 42 | 44 | 45 | 47 | 50 | 55 | 12 de 12 · 12 de 12 · 12 de 12 | 81 (quedan 39) · 76 (quedan 44) · 69 (quedan 51) |
| 30-44 | 32 | 36 | 37 | 39 | 40 | 42 | 44 | 45 | 49 | 55 | 12 de 12 · 12 de 12 · 12 de 12 | 84 (quedan 36) · 78 (quedan 42) · 69 (quedan 51) |
| 45-54 | 31 | 33 | 35 | 37 | 39 | 40 | 42 | 44 | 47 | 53 | 12 de 12 · 12 de 12 · 12 de 12 | 86 (quedan 34) · 81 (quedan 39) · 72 (quedan 48) |
| 55-64 | 28 | 31 | 33 | 34 | 36 | 38 | 39 | 42 | 44 | 51 | 12 de 12 · 12 de 12 · 12 de 12 | 90 (quedan 30) · 86 (quedan 34) · 75 (quedan 45) |
| 65-74 | 23 | 26 | 28 | 30 | 32 | 34 | 35 | 38 | 40 | 49 | 12 de 12 · 12 de 12 · 12 de 12 | 99 (quedan 21) · 94 (quedan 26) · 78 (quedan 42) |
| 75+ | 20 | 22 | 25 | 26 | 28 | 29 | 31 | 34 | 37 | 45 | 12 de 12 · 12 de 12 · 12 de 12 | 110 (quedan 10) · 102 (quedan 18) · 84 (quedan 36) |

Separación en aciertos entre líneas: 18-29: 80→90 = 3, 90→99 = 5 · 30-44: 80→90 = 4, 90→99 = 6 · 45-54: 80→90 = 3, 90→99 = 6 · 55-64: 80→90 = 2, 90→99 = 7 · 65-74: 80→90 = 2, 90→99 = 9 · 75+: 80→90 = 3, 90→99 = 8

Franjas con dos líneas de 10 a 90 en la MISMA marca: ninguna

#### Acoplamiento — 12 pasos, el último a los 22 aciertos (uno cada 2); tramos de a 2, borde de la pista en 40

| Franja | 10 % | 20 % | 30 % | 40 % | 50 % | 60 % | 70 % | 80 % | 90 % | 99 % | paso en 80 % · 90 % · 99 % | último paso (s) en 80 % · 90 % · 99 % |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 18-29 | 21 | 23 | 24 | 25 | 26 | 27 | 28 | 30 | 31 | 34 | 12 de 12 · 12 de 12 · 12 de 12 | 82 (quedan 38) · 80 (quedan 40) · 73 (quedan 47) |
| 30-44 | 20 | 22 | 23 | 24 | 25 | 26 | 28 | 29 | 31 | 33 | 12 de 12 · 12 de 12 · 12 de 12 | 85 (quedan 35) · 80 (quedan 40) · 75 (quedan 45) |
| 45-54 | 18 | 20 | 21 | 22 | 23 | 25 | 26 | 27 | 29 | 32 | 12 de 12 · 12 de 12 · 12 de 12 | 91 (quedan 29) · 85 (quedan 35) · 78 (quedan 42) |
| 55-64 | 16 | 18 | 19 | 21 | 22 | 23 | 24 | 26 | 28 | 32 | 12 de 12 · 12 de 12 · 12 de 12 | 94 (quedan 26) · 88 (quedan 32) · 78 (quedan 42) |
| 65-74 | 13 | 15 | 17 | 18 | 19 | 20 | 22 | 23 | 25 | 30 | 12 de 12 · 12 de 12 · 12 de 12 | 106 (quedan 14) · 98 (quedan 22) · 82 (quedan 38) |
| 75+ | 9 | 11 | 13 | 15 | 16 | 18 | 19 | 21 | 23 | 29 | 12 de 12 · 12 de 12 · 12 de 12 | 115 (quedan 5) · 106 (quedan 14) · 85 (quedan 35) |

Separación en aciertos entre líneas: 18-29: 80→90 = 1, 90→99 = 3 · 30-44: 80→90 = 2, 90→99 = 2 · 45-54: 80→90 = 2, 90→99 = 3 · 55-64: 80→90 = 2, 90→99 = 4 · 65-74: 80→90 = 2, 90→99 = 5 · 75+: 80→90 = 2, 90→99 = 6

Franjas con dos líneas de 10 a 90 en la MISMA marca: ninguna

#### Rescate relámpago — 12 pasos, el último a los 33 rondas (uno cada 3); tramos de a 3, borde de la pista en 48

| Franja | 10 % | 20 % | 30 % | 40 % | 50 % | 60 % | 70 % | 80 % | 90 % | 99 % | paso en 80 % · 90 % · 99 % | último paso (s) en 80 % · 90 % · 99 % |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 18-29 | 23 | 25 | 27 | 28 | 29 | 30 | 31 | 33 | 35 | 39 | 5 de 12 · 5 de 12 · 6 de 12 | no llega · no llega · no llega |
| 30-44 | 23 | 25 | 26 | 27 | 28 | 29 | 30 | 32 | 33 | 38 | 5 de 12 · 5 de 12 · 5 de 12 | no llega · no llega · no llega |
| 45-54 | 22 | 23 | 25 | 26 | 27 | 28 | 29 | 31 | 33 | 38 | 5 de 12 · 5 de 12 · 5 de 12 | no llega · no llega · no llega |
| 55-64 | 20 | 22 | 23 | 25 | 26 | 27 | 28 | 30 | 32 | 36 | 4 de 12 · 5 de 12 · 5 de 12 | no llega · no llega · no llega |
| 65-74 | 17 | 19 | 21 | 22 | 24 | 25 | 26 | 28 | 30 | 35 | 4 de 12 · 4 de 12 · 5 de 12 | no llega · no llega · no llega |
| 75+ | 14 | 16 | 18 | 20 | 21 | 22 | 24 | 25 | 27 | 33 | 4 de 12 · 4 de 12 · 5 de 12 | no llega · no llega · no llega |

Separación en aciertos entre líneas: 18-29: 80→90 = 2, 90→99 = 4 · 30-44: 80→90 = 1, 90→99 = 5 · 45-54: 80→90 = 2, 90→99 = 5 · 55-64: 80→90 = 2, 90→99 = 4 · 65-74: 80→90 = 2, 90→99 = 5 · 75+: 80→90 = 2, 90→99 = 6

Franjas con dos líneas de 10 a 90 en la MISMA marca: ninguna

## 9. Límites de este análisis

- El ensayo de cada juego es el que le entrega al motor; en Constelaciones, Correo y Piloto el número por partida es una estimación (marcada arriba), y el resultado cambia poco con ±30 % (se mueve 1 partida).
- Una persona real no acierta el mismo 95 % en todos los niveles: en los juegos de capacidad (Rastro, Constelaciones, Bodega, Satélites) el motor la equilibra antes. Ese caso se acerca al 90 % de la tabla de sensibilidad, que tampoco es realista arriba.
- El segundo del último paso del Contrarreloj es una estimación proporcional (cota inferior); para el valor exacto habría que registrar el segundo en el que cada robot cambia de paso (`AgeReference`), algo que cabe en la próxima corrida de `ReferenceTableRunner` si Opus lo pide.
- Las fuentes de §5 son las que se pudieron verificar en PubMed. Donde dice «sin fuente verificada», es criterio de diseño.
