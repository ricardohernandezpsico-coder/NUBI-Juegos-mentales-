# Medidas del final de los juegos estrella: qué miden y qué se puede decir

Revisión del 27-sep, pedida por Ricardo después de jugar Aterrizaje Lunar: que lo que dice el final de cada juego
aguante la mirada de alguien que conoce los estudios. Para cada medida: qué mide de verdad, cómo se calcula, qué
NO se puede decir con ella y en qué estudios se apoya.

Reglas para todas:

- **Nombrar lo que se mide, no un rasgo.** "Tu estimación: a 4% del blanco", no "tu precisión numérica".
- **Una partida es poco.** Son pocos ensayos (entre 6 y 40), así que no se sacan conclusiones de diferencias chicas.
  Al pie de las medidas va siempre: *"Medida de esta partida: cambia de un día a otro. Lo que vale es cómo
  evoluciona, no un resultado suelto. No es un diagnóstico."*
- **Comparar con los estudios solo si la condición es comparable.** Si no, no se compara.
- **Dar algo útil.** Si hay un consejo que respaldan los estudios, se dice.

---

## Aterrizaje Lunar: "Tu estimación" y "Tu línea"

**Qué es la tarea.** Estimación en la línea numérica con los dos extremos marcados (Siegler y Opfer, 2003).

**La pregunta de Ricardo: ¿es precisión numérica o visoespacial?** Las dos, y en adultos pesa mucho lo segundo.
- Con una regla que tiene sus dos extremos marcados, los adultos resuelven la tarea como un **juicio de proporción**:
  se apoyan en puntos de referencia (los extremos, la mitad, los cuartos) (Barth y Paladino, 2011). El seguimiento de
  la mirada muestra ese uso de referencias en adultos (Sullivan et al., 2011).
- En niños, la relación entre esta tarea y el rendimiento en matemáticas se explicó por completo con habilidades
  visomotoras y visoespaciales (Simms et al., 2016).
- La tarea sí se asocia con la competencia matemática: el metaanálisis de Schneider et al. (2018) da r ≈ .44, sobre
  todo en niños. Pero es una asociación, no una prueba de matemáticas.
- En nuestro juego se suma el pulso: hay que llevar el módulo con el dedo mientras cae.

**Qué decía antes y por qué estaba mal.** El juego leía un "sesgo" con signo ("agrandas los chicos, achicas los
grandes"). Tenía dos problemas:
1. **Es una trampa estadística.** Cerca del 0 solo se puede errar hacia la derecha, y cerca del final solo hacia la
   izquierda. Con pocos aterrizajes y algo de ruido, ese "sesgo" aparece aunque la persona no tenga ninguno.
2. **El patrón no aplica.** La curva logarítmica de Siegler y Opfer se describió en niños, y en adultos se discute que
   sea logarítmica (Barth y Paladino, 2011).

**Qué dice ahora.**
- "Tu estimación: a X% del blanco": la distancia media, en % del largo de la regla. Una línea explica que la tarea
  junta dos cosas: saber cuánto vale el número y calcular a ojo qué parte de la regla le toca.
- "Tu línea": el dibujo de cada blanco y dónde se posó.
- El tramo de la regla (inicio, centro o final) donde más se aleja del blanco, medido con distancia **sin signo**.
  Solo se nombra si hay al menos 2 aterrizajes por tramo y el peor tramo está al menos 3 puntos y 1,5 veces más lejos
  que el mejor.
- Un truco con respaldo: medir desde la referencia más cercana (la mitad, los cuartos, el final), que es la estrategia
  de quienes mejor estiman.

Código: `app/.../data/NumberLine.kt` (`reading`) con sus pruebas.

---

## Radar (Rescate relámpago): "Tu vistazo", "Tu captura", "Tu filtro" y "Tu radar"

Rediseño del 29-sep (la versión con una nave en el centro se parecía demasiado a una patente: ver
`docs/nombre-marca-y-riesgos.md`). La tarea es de **informe total** (Sperling, 1960) en la línea de la teoría de la
atención visual (TVA: Bundesen, 1990; revisión clínica de Habekost, 2015): varios astronautas en un destello
enmascarado, y se marcan todos los lugares donde se vieron (se sabe cuántos eran).

**Tu vistazo.** La duración de destello más breve con la que se rescatan casi todos (todos hasta 3; todos menos uno con
4 o más) de forma estable. La escalera apunta a ~80%, por eso el texto dice "unas 4 de cada 5 veces", y dice con
cuántos astronautas a la vez se logró (la cantidad sube con el nivel). Media geométrica de las duraciones REALES de
las últimas 12 rondas normales, sin las 4 primeras. No es la "velocidad de procesamiento C" de TVA (para estimarla hace
falta ajustar un modelo con muchas duraciones fijas): es un umbral práctico.

**Tu captura.** Promedio de las "lluvias de astronautas" (cada 5 rondas: 6 astronautas, 300 ms, fuera de la escalera):
rescatados menos balizas de más (poner balizas al azar no suma). Con destellos largos y máscara, el informe total se
acerca a la capacidad de la memoria visual de corto plazo (K en TVA), que en adultos ronda 3 a 4 elementos (Luck y
Vogel, 1997; Cowan, 2001): por eso el texto da esa referencia general. Se muestra solo con 2 o más lluvias.

**Tu filtro.** Robots tocados de los mostrados (desde el nivel 5: informe parcial, seleccionar lo relevante). Se
nombra solo con 6 o más robots en la partida. Tocar un robot también puede ser una baliza al azar: por eso el texto no
lo interpreta como "impulsividad" (además, no calculamos perfiles de ese tipo: ver la nota de patentes).

**Tu radar.** Rescatados por dirección (cuñas) y cerca / lejos del centro. Se nombra dónde más y dónde menos solo con
4 o más astronautas en cada dirección y 40 puntos de diferencia; cerca / lejos, con 6 o más en cada anillo y 25 puntos
(el campo visual útil se achica hacia la periferia cuando la tarea apura). Siempre "en esta partida... si se repite".

**Alerta.** El destello llega sin aviso (espera de 1,5 a 3,5 s): entrenar la alerta propia aumentó la velocidad de
procesamiento visual medida con TVA en mayores (Penning et al., Psychological Science, 2021). No se mide aparte.

**Qué NO se dice.** No se compara con normas clínicas (UFOV ni TVA): otro aparato, otra pantalla, otra distancia a los
ojos. No se usa la evidencia del ensayo ACTIVE para prometer efectos.

---

## Satélites: "Tu seguimiento"

**Qué mide.** Cuántos objetos se siguieron de verdad, descontando los aciertos por suerte. Se usa el modelo de
adivinación: aciertos = m + (k − m)² / (n − m).

**Qué estaba mal.** Se promedia por ronda, y el valor nunca puede superar la cantidad que había que seguir (k). Si la
persona solo llegó a rondas de 2 o 3 satélites, "2,6" no es su límite: es el techo que le puso el juego. Además, el
"alrededor de 4" depende mucho de la velocidad: a más velocidad se siguen menos (de 1 a 8, Alvarez y Franconeri,
2007).

**Qué dice ahora.**
- "Tu seguimiento: 2,6 **de 3** a la vez": el "de N" es cuántos había que seguir, en promedio. Viaja en
  `tracking_targets`.
- La referencia "a velocidad moderada, los adultos suelen seguir entre 3 y 4; más rápido, menos (Alvarez y
  Franconeri, 2007)" aparece solo si había que seguir 3,5 o más.

---

## Freno de Emergencia: "Tu freno"

**Qué mide.** El tiempo de reacción a la señal de alto (SSRT), estimado por el método de integración con reemplazo de
omisiones, como recomienda el consenso de Verbruggen et al. (2019).

**Qué cambió.** El consenso recomienda **no estimar** el SSRT individual si la proporción de veces que se respondió
ante un alto queda fuera de 0,25–0,75. Antes el juego aceptaba 0,15–0,85; ahora usa 0,25–0,75, con una prueba que lo
comprueba. Cuando no se puede estimar, el final lo explica y pide lanzar sin esperar al ALTO: esperar invalida la
medida, y esa también es una recomendación del consenso.

**Qué NO se dice.** Con 10 a 20 altos por partida la estimación es ruidosa. Por eso el texto dice "estimación" y
"varía bastante: mira cómo va en varias partidas".

---

## Acoplamiento: "Tu giro mental" y "Tu curva de giro"

**Qué mide.** La velocidad de rotación mental: la pendiente del tiempo de respuesta según el ángulo, calculada solo con
los aciertos. Los grados por segundo son la inversa de esa pendiente. Las piezas son 2D, así que la cita correcta es
Cooper y Shepard (1973); Shepard y Metzler (1971) usaron figuras 3D.

**Qué cambió.**
- Si se responde mucho al azar, la curva sale plana y parece un "giro rapidísimo". Ahora el giro no se calcula con
  menos de **70% de aciertos**, con una prueba que lo comprueba.
- En Reto, el combustible corta las respuestas lentas en los ángulos grandes, y eso aplana la curva. El texto sugiere
  Precisión para una medida más fiel.

---

## Piloto Estelar: "Costo de multitarea"

**Qué mide.** Cuánto baja la puntería con las señales (aciertos menos falsas alarmas, Snodgrass y Corwin, 1988) al
pasar de solo mirarlas (piloto automático) a pilotar y mirarlas a la vez. Es la idea de NeuroRacer (Anguera et al.,
2013).

**Límites.**
- El piloto automático dura 15 s: pocas señales.
- Siempre va primero, así que la práctica y el cansancio se mezclan con el costo.
- La dificultad de las señales sigue ajustándose entre las dos fases.

Por eso el texto describe lo que pasó en la partida y no lo compara con nadie. "Con práctica suele bajar" es lo que
mostró el estudio de NeuroRacer con la tarea entrenada.

---

## Tráfico Estelar: "Tu carga" y "Tu anticipación"

**Qué mide.**
- "Tu carga": cuántas cápsulas hubo en viaje a la vez sin ningún error entre ellas. Es descriptivo y depende del
  tráfico que el juego llegó a dar; el texto lo dice ("sube a medida que el juego te da más tráfico").
- "Tu anticipación": cuánto antes de que pase la cápsula quedó listo su desvío (la mediana de la partida).
- "Planificas / A último momento": qué parte de los desvíos se preparó con al menos 1 s de anticipación. Es una
  analogía con el control proactivo y reactivo (Braver, 2012), no una medida de ese modelo. El texto no lo presenta
  como tal.

---

## Bitácora de Misión: "Tu memoria a los X minutos", "retención", "la ruta"

**Qué mide.** Memoria episódica con recuerdo diferido: qué hallazgo había en cada planeta (asociación qué–dónde,
recordada con la pista del lugar) y en qué orden pasó la sonda (el "cuándo"). La demora es real: en la sesión diaria,
el informe llega después de los otros juegos (10 a 20 minutos); al jugar suelto, después de una patrulla de 45 s.

**Retención.** De lo acertado en el primer repaso, cuánto seguía en el informe. Separar aprender de retener es la
lógica de las pruebas de aprendizaje y recuerdo diferido (tipo RAVLT). Con 3 a 8 paradas por misión, una partida es
poco: por eso se muestra junto con la nota común, y lo valioso es la evolución.

**Qué NO se dice.** No se compara con normas clínicas: el material, la cantidad y la demora son propios del juego.
Elegir un hallazgo que no estaba se cuenta y se explica sin culpa: la memoria reconstruye y a veces completa huecos.

**Consejo.** Imaginar una escena que une el hallazgo con su planeta ayuda a recordar asociaciones (Bower, 1970).
Practicar el recuerdo afianza (Roediger y Karpicke, 2006).

---

## Rumbo a Casa: "Tu brújula interna" y "Tus llegadas"

**Qué mide.** Integración de trayecto: volver al punto de partida sin verlo, usando solo lo que se registró al moverse
(cuánto se giró y cuánto se avanzó). Es la tarea de completar el triángulo (Klatzky et al., 1990; Loomis et al., 1993),
que depende de las células de red de la corteza entorrinal (Hafting et al., 2005) y se usa en los estudios de
orientación en realidad virtual (Howett et al., 2019) y en Sea Hero Quest (Coutrot et al., 2018). La base
científica explica por qué vale la pena; en la app **no se nombran enfermedades** ni se sugiere que el juego detecte
algo.

**Cómo se calcula.** En cada vuelta se registra dónde quedó la nave en el marco de la vuelta justa (la base adelante,
a distancia 1). De ahí salen, sin suposiciones, el desvío del rumbo (grados) y la distancia recorrida (1 = justa).
"Tu brújula interna" es la distancia media a la base, en % de lo que había que volver.

**Qué se dice y con qué mínimos.**
- El desvío medio del rumbo, siempre.
- La distancia ("la calculas bien", "sueles quedarte corto", "sueles pasarte", "a veces corto y a veces largo") solo
  desde 4 viajes y con 70% de los viajes para el mismo lado. En los giros no hay bordes que sesguen el error, así que
  el signo sí se puede leer (a diferencia de la regla de Aterrizaje Lunar).
- Qué aleja más de casa (los costados = rumbo; antes/después = distancia) solo si una parte es 1,5 veces la otra y 5
  puntos mayor, con un truco concreto para esa parte.
- Con faro / sin faro solo con 3 viajes o más de cada tipo, y "te ayudó" solo con 8° de diferencia. El faro está en el
  infinito: da el rumbo, no la posición (los puntos de referencia lejanos ayudan a orientarse).

**Qué NO se dice.** No se compara con normas: el aparato, la escala y la demora no son los de ningún estudio. Se sabe
que en esta tarea las respuestas tienden hacia el promedio (giros grandes se quedan cortos, giros chicos se pasan;
Loomis et al., 1993), pero con 6-8 viajes por partida no se nombra ese patrón.

---

## Correo Estelar: "Tu memoria para lo pendiente"

**Qué mide.** Memoria prospectiva: acordarse de hacer algo en el momento justo mientras se está ocupado en otra cosa
(Rummel y Kvavilashvili, 2023). Es la base de muchos olvidos del día a día (tomar un remedio, hacer una llamada). La tarea
en curso es el vuelo de Piloto Estelar (mantenerse en la ruta y recoger sobres); los encargos se dan antes de salir y
NO se muestran durante el vuelo.
- **Por lugar** (evento): tocar los planetas de un color cuando pasan. Algo del entorno avisa (el planeta), pero hay
  que reconocerlo a tiempo. Pocos planetas son del encargo (~23%) y desde el nivel 3 algunos tienen un color parecido.
- **Por hora** (tiempo): tocar la radio cada 30 s (25 o 20 en niveles altos), con ±5 s de margen. Los dos tipos de
  encargo van desde el primer vuelo, y la tarea en curso se pone exigente: el vuelo se acelera en tres tramos y hay
  asteroides que esquivar (sin carga, los encargos serían fáciles de recordar y no medirían lo que pasa en el día). Nada avisa; el reloj
  va tapado y tocarlo lo destapa 1,6 s. Cuándo se mira es la estrategia: lo eficaz es mirar poco al principio y más
  cerca de la hora (el patrón clásico de las tareas por tiempo).

**Qué se dice y con qué mínimos.**
- "N de M encargos" y el detalle por lugar y por hora.
- Planetas tocados por error, sin culpa (y cuántos de color parecido).
- El reloj, solo con 2 o más horas de radio: sin mirarlo, mirando sobre todo justo antes (≥ 50% de las miradas en el
  último 30% del intervalo), mirando todo el rato (más de 3 veces por hora), o un truco.
- Lugar contra hora, solo con 3 o más de cada uno y 25 puntos de diferencia, con un consejo para el día: si se pasan los
  de hora, convertirlos en encargos de lugar (dejar el remedio junto al cepillo); si se pasan los de lugar, la intención
  de implementación ("cuando vea el planeta, lo toco"; metaanálisis de Chen et al., 2015). El mismo truco aparece en la
  hoja de ruta.

**Cuidado de la nave (escudo).** Cada asteroide rompe un segmento del escudo y la nave se ve dañada; volar limpio lo
repara. Tener algo propio que cuidar da motivo para anticiparse en vez de reaccionar (motivación y control proactivo:
Braver, 2012; Botvinick y Braver, 2015). A propósito, quedarse sin escudo NO termina el vuelo (solo una reparación de
emergencia de 3,5 s): si terminara, quien pilota peor tendría menos encargos y la medida de memoria dependería del
pilotaje; además, con un castigo fuerte en una de dos tareas la gente descuida la otra. Al final se dice "Nave intacta
el N% del vuelo" y cuántas reparaciones hubo; con menos de la mitad del vuelo intacta, un truco concreto (mirar más
arriba de la nave para ver venir los asteroides). No es una medida de memoria: es parte del juego.

**Qué NO se dice.** No se compara con normas ni se habla de "memoria prospectiva" como rasgo: con 150 s de vuelo y unos
7 encargos de cada tipo, la medida es de esta partida.

---

## Lluvia de meteoros: "Tu vocabulario", "Tu reconocimiento", "Tu filtro" y "Tu colección"

**Qué es la tarea.** Decisión léxica "ir / no ir": caen meteoros con una palabra y se TOCAN solo las que existen; las
inventadas se dejan pasar. Es una variante validada de la tarea clásica de decisión léxica (Meyer y Schvaneveldt, 1971):
conserva el efecto de frecuencia y sale más rápida, más precisa y menos exigente que la de sí/no (Perea, Rosa y Gómez,
2002). Por eso el juego no tiene botón "no es palabra".

**De dónde sale cada medida.**
- **Tu vocabulario.** Una cifra grande ("N de cada 10" poco frecuentes reconocidas) y tres barras: comunes, intermedias y raras
  (cada una junta dos bandas; antes eran seis columnas de estrellas, 1-oct). La banda sale de qué tan conocida es cada palabra (SPALEX: una recolección masiva en línea de qué palabras del español
  conoce la gente, Aguasvivas et al., 2018; se usa el MÍNIMO entre España y Latinoamérica, así no entran regionalismos).
  En cada grupo: % de palabras reconocidas MENOS el % de inventadas tocadas en toda la partida, como en LexTALE
  (aciertos menos falsas alarmas; Lemhöfer y Broersma, 2012; versión en español: Ferré y Brysbaert, 2017). Solo se
  muestra un grupo con al menos 6 palabras vistas; si no, "aún sin medir". La frase ("Reconoces casi todas las comunes, la
  mayoría de las intermedias y menos de la mitad de las raras") usa rangos fijos de % y junta los grupos vecinos iguales. No dice cuántas palabras
  conoces ni compara con otras personas.
- **Tu reconocimiento.** La mediana del tiempo de toque en palabras comunes (bandas 1-2) contra raras (bandas 5-6), con
  al menos 5 toques de cada una. Es el **efecto de frecuencia**: es normal que las raras tarden más.
- **Tu filtro.** Qué inventadas engañaron, por tipo: obvias (sílabas recombinadas), una letra cambiada y letras
  cambiadas de lugar. Estas últimas ("chocloate") se leen como palabra, sobre todo si el cambio es en el INTERIOR de
  la palabra y no al final (Perea y Lupker, 2003). El consejo ("mira el centro de la palabra") solo aparece si las
  traspuestas engañan más que los otros dos tipos y hay al menos 5 vistas de cada tipo.
- **Tu colección.** Las palabras raras (bandas 5-6) acertadas se guardan en el teléfono (`word_collection`).

**Qué NO se dice.** Nada de "tu vocabulario es de X palabras", "edad del vocabulario" ni comparaciones con otras
personas, hasta tener datos propios. Los meteoros de la Lluvia de estrellas (palabras comunes y rápidas) no entran en
ninguna medida. La marca para ver la evolución (`vocab`) es el promedio, ponderado por palabras vistas, del % reconocido
en las bandas 3 a 6 (lo que de verdad distingue un vocabulario amplio); "mejor" = más alto.

**Pendiente.** Licencia de SPALEX para uso comercial: se pidió permiso a los autores; mientras tanto el léxico se arma
con ella (`tools/lexico/meteoros.py`) y el plan B es una lista abierta de frecuencias.

Código: `Games/Meteoros/MeteorContract.cs` (reglas y medidas, con pruebas), `app/.../data/Vocabulary.kt` (lectura, con
pruebas) y `GameResultScreen`.

---

## ¿Verdad o disparate?: "Tu lectura con comprensión", "Qué te frena", "Tu precisión" y "Tu mejor racha"

**Qué es la tarea.** Verificación de frases: llegan frases cortas y se decide si son verdad o un disparate. Es la tarea de Collins
y Quillian (1969), usada en neuropsicología para evaluar la memoria semántica (Wilson y Baddeley, 1988, *Brain and Cognition*
8:31-46; Clare et al., 1993, *Neuropsychologia* 31:1225-41). Las frases las genera un programa a partir de una base de
conocimiento propia, sin un banco fijo que se memorice (Crossland, Legge y Dakin, 2008, *Behavioral and Brain Functions* 4:14).
La dificultad viene de la FORMA de la frase (negación, cláusula entre comas, cuantificadores, comparaciones), no del conocimiento.

**De dónde sale cada medida.**
- **Tu lectura con comprensión.** Mediana, en las frases bien respondidas, de palabras ÷ tiempo de respuesta, en palabras por
  minuto. El tiempo cuenta desde que la frase quedó completamente legible (después del efecto de llegada) hasta el toque. Se
  rotula "leyendo y decidiendo" porque incluye la decisión: no es una velocidad de lectura pura. Solo con 10 o más aciertos; si
  no, "juega un poco más para medir". Es la marca para ver su evolución (más alto = mejor), comparable solo entre partidas a ±1 nivel.
- **Qué te frena.** Tiempo medio de los aciertos por tipo de frase (corta, con complemento, negación, con pausa, todos / algunos /
  ningún, comparación), solo de los tipos con 4 o más aciertos. Las negaciones cuestan más (Clark y Chase, 1972; Carpenter y
  Just, 1975). Se marca la más lenta con texto ("la más lenta") además del color y, si se separa 0,3 s o más de las frases
  simples, se dice cuánto ("Las negaciones te toman 0,9 s más que las frases simples. Es normal.") con un truco por tipo:
  negación "lee la frase sin el «no» y después dala vuelta"; pausa "fíjate solo en lo que va después de la segunda coma";
  todos / algunos "busca un solo ejemplo que la rompa"; comparación "imagina las dos cosas una al lado de la otra".
- **Tu precisión.** Aciertos sobre el total y sobre los disparates SUTILES (los que se parecen a algo cierto) por separado. NO se
  calcula ningún perfil de sesgo (tender a decir verdad o disparate): regla de patentes (US 11,839,472).
- **Tu mejor racha.** La transmisión más larga sin error.

**Qué NO se dice.** Nada de "tu velocidad de lectura es de X" como rasgo, ni comparaciones con otras personas; las frases de
Ráfaga (3 palabras, muy rápidas) no entran en ninguna medida. Una frase que la persona marca como poco clara no cuenta.

Código: `Games/Disparate/DisparateContract.cs` y `DisparateDirector.cs` (reglas y medidas, con pruebas), `app/.../data/Reading.kt`
(lectura, con pruebas), frases en `tools/frases/`.

## Cosecha de palabras: "Tu cosecha", "Tu manera de buscar", "Tu ritmo" y "Tu palabra estrella"

**Qué es la tarea.** Fluidez verbal con un juego de letras fijo: siete letras giran alrededor de un planeta y se forman todas las
palabras posibles (3 letras o más) durante tres cosechas de 60 s. Pide PRODUCIR palabras desde la memoria bajo presión de tiempo, lo
que más cuesta con la edad (los otros juegos de Lenguaje reconocen o comprenden). La fluidez verbal es de las pruebas más usadas en
neuropsicología (Troyer, Moscovitch y Winocur, 1997).

**Nota de honestidad (Troyer).** El análisis de agrupar y saltar se validó con fluidez por categoría ("animales") y por letra inicial
("F"), NO con formar palabras con un juego de letras fijo. Por eso aquí se usa solo como DESCRIPCIÓN de cómo buscaste en esa partida
("racimos" o "saltos"): sin normas, sin percentiles y sin comparar con estudios ni con otras personas. La pantalla lo dice ("cómo
buscaste en esta partida").

**De dónde sale cada medida.**
- **Tu cosecha.** Palabras encontradas en las 3 cosechas (sin las ocultas, que se aceptan pero no se muestran ni cuentan) y, de las
  comunes (bandas 1-3 de SPALEX), cuántas se encontraron de las disponibles: "de las comunes, 21 de 48". Es la marca para ver su
  evolución: % de las comunes encontradas (más alto = mejor), comparable a ±1 nivel.
- **Tu manera de buscar.** Entre las palabras que tuvieron una anterior en su cosecha, el % que salió en RACIMO (comparte las 2
  primeras letras o la raíz con la anterior: casa → casas → caso) frente a SALTO (palabra nueva sin relación). Solo con 10 o más
  palabras. Frase con consejo: muchos racimos (≥ 70%) → "exprimes bien cada idea; prueba también saltar a otra letra inicial"; muchos
  saltos (≤ 30%) → "saltas rápido; cuando una funciona, busca sus parientes (plural, otra terminación)"; en medio, "combinas bien".
- **Tu ritmo.** Palabras en los primeros 20 s contra los últimos 20 s de cada cosecha (promedio de las cosechas). En las pruebas de
  fluidez la mayoría de las palabras salen al principio y después cuesta más: "arrancas fuerte y bajas al final: es lo normal" (con el
  consejo de cambiar de idea), "mantienes el ritmo" o "te soltaste al final". Es descriptivo.
- **Tu palabra estrella y también podías.** La palabra que usa las 7 letras (o la más larga si no hubo) y hasta 5 comunes que no se
  encontraron, de las más usadas y cortas (aprendizaje, sin culpa).

**Qué NO se dice.** Nada de "tu fluidez verbal es X" como rasgo, ni comparaciones con otras personas, ni diagnóstico. Las pistas que
dio Nubi se anotan ("Pistas de Nubi: N").

Código: `Games/Cosecha/CosechaContract.cs` y `CosechaSession.cs` (reglas y medidas, con pruebas), `app/.../data/Harvest.kt` (lectura,
con pruebas), rondas en `tools/cosecha/`.

## Rastro de luz: "Tu rastro" y "Por modo"

**Qué es la tarea.** Amplitud visoespacial con luz y sonido: una chispa recorre luceros de cristal (cada uno con su nota) y la persona repite el
orden con el dedo. Es la familia de las tareas de bloques de Corsi (orden de lugares: Kessels et al., 2000). Sobre ella hay tres variantes: **al revés**
(reordenar en la cabeza), **el cielo gira** (recordar el orden de los luceros aunque el tablero cambie de lugar; variante propia, no validada) y
**en marcha** (la chispa recorre más luces de las que se piden y solo se repiten las últimas N: memoria «en marcha» o *running span*, que mide capacidad
de memoria de trabajo y se relaciona con el razonamiento: Broadway y Engle, 2010).

**De dónde sale cada medida.**
- **Tu rastro.** Las luces más largas que se repitieron BIEN en el rastro simple de la partida (el mayor largo de una ronda acertada). Es la marca para ver su
  evolución (más alto = mejor, comparable a ±1 nivel porque el largo depende del nivel de la escalera). Si no salió ningún rastro simple completo, no hay cifra:
  se dice con calma y con un consejo («toca una luz a la vez»), sin culpa.
- **Por modo.** Para al revés, el cielo gira y en marcha: aciertos de rondas por modo, solo con **3 rondas o más** (con menos no se dice nada). El de menor
  tasa se marca con el TEXTO «el que más te costó» (además del color coral) y solo si hay dos o más modos y no empatan todos. Bajo las filas, SOLO la línea del
  modo marcado (qué pide: «Al revés pide reordenar en la cabeza: suele costar un poco más») y su truco concreto; sin modo marcado, ninguna.
- **Lectura.** «Tu rastro mide tu memoria de trabajo: lo que sostienes en la cabeza mientras lo usas» (una oración). Y, si hubo, «Desbloqueaste un modo nuevo: …» (un dato, una sola vez).

**Qué NO se dice.** Ningún percentil ni comparación con otras personas o con estudios (la condición no es comparable: luces, giro y sonido propios). No se
dice que mida «tu memoria» como rasgo ni que entrene nada. Con pocas rondas es una pista, no un resultado: la nota común del pie lo recuerda. El límite
real de la memoria a corto plazo ronda los 4 elementos (Cowan, 2001), por eso la escalera no pasa de 8 luces. La ronda guiada del tutorial no cuenta. Tampoco cuenta la «confusión de modo» (primer toque de una ronda al revés o en marcha en el lucero del modo equivocado: se avisa y se repite la
misma muestra, una vez por modo y partida): así el error de entender la consigna no se confunde con un fallo de memoria (`ras_mode_confusions`).

**El ojo de la originalidad.** Lumosity tiene «Rotation Matrix» (una cuadrícula con un patrón que gira 90°). El nuestro es distinto (el orden de un camino, no un
patrón; ángulos libres; luceros que suenan) y no se dice que «ninguna app gira». Lista del abogado: revisarlo antes de publicar.

Código: `Games/Secuencia/RastroContract.cs`, `RastroSession.cs` (`RastroTally`) y `RastroBoard.cs` (reglas y medidas, con pruebas), `app/.../data/Trail.kt`
(lectura, con pruebas), pantalla en `GameResultScreen`. Ficha técnica: `docs/diseno-rastro-de-luz.md`.

## Tinta o Palabra («Dos orillas»): "Cuánto te frenó la palabra" y "Cambiar de orilla te costó"

Ficha completa: [diseno-tinta-o-palabra.md](diseno-tinta-o-palabra.md). Dos diferencias de tiempo EN ESTA PARTIDA, medidas sobre los aciertos:

- **Interferencia («Cuánto te frenó la palabra»)**: tiempo medio de los aciertos con palabra que CHOCA con su tinta menos el de los que COINCIDEN (`interference_ms`; MacLeod, 1991; Stroop, 1935).
  Hace falta tener al menos 3 aciertos de cada tipo; con menos, −1 («sin datos») y la app no muestra nada. Un resultado negativo se lleva a 0 («casi nada», menos de 0,05 s) para que −1
  siga siendo solo «sin datos». Por eso en el juego una parte de las palabras (25–40 %) coincide con su tinta.
- **Costo de cambio («Cambiar de orilla te costó»)**: tiempo medio de los aciertos justo después de un cambio de orilla menos el de los que repiten orilla (`switch_cost_ms`; Monsell, 2003).
  Solo existe desde el nivel 3 (en los niveles 1 y 2 solo hay orilla de la TINTA) y también pide 3 aciertos de cada tipo. La regla se ve en tres señales a la vez (orilla, ícono y cinta)
  justamente porque una señal clara de la regla reduce ese costo.
- **Cómo leerlo**: es cuánto más tardaste en promedio en esta partida, no un rasgo tuyo ni un test clínico: cambia de un día a otro y con pocos aciertos es poco fiable. No se compara con
  otras personas ni se habla de «control ejecutivo» o de la concentración en la vida diaria.
- **Consejo** (solo con una diferencia de 0,3 s o más, para «¿qué te sirve más ver primero?»): «mira primero de qué orilla llega y recién después la palabra» (interferencia) y «cuando cambie
  la orilla, dite por dentro "ahora, tinta" o "ahora, palabra" antes de tocar» (costo de cambio). Son sugerencias de estrategia, no promesas.
- **Paleta**: 4 tintas para todos (ROJO C93C3C, AZUL 4A86E8, AMARILLO F2CC1D, BLANCO F4F4F4) verificadas con `tools/paleta_daltonismo.py` (diferencia ≥ 20 en visión típica, protanopía y
  deuteranopía; Machado et al., 2009). Una sola paleta para todos; no depende de `color_vision`.
- Telemetría en `StroopSessionMetrics`; lectura en `data/DosOrillas.kt` (con pruebas).

## En la punta de la lengua (id `anagramas`): "Encontraste X de N por tu cuenta" y "Tu cielo de palabras"

**Qué mide, de verdad:** cuántas de las palabras de la partida encontraste SIN ayuda (luceros dorados), con el desglose de las que salieron con 1-2 ayudas, con las
letras justas o mostradas por Nubi, y el tiempo medio hasta «¡La tengo!» de las que salieron solas. No es «tu vocabulario» ni «tu memoria»: es esta partida, con
estas palabras y estas definiciones. Se guarda como medida propia (`punta`, % de palabras encontradas solas) para ver su evolución, comparable solo entre partidas
a tu medida y de nivel parecido.

**Cómo se lee:** la cifra grande es «X de N». La lista deja ver qué palabras costaron (el nombre del lucero va en texto, nunca solo color). El tiempo solo se dice con
3 palabras solas o más (con menos sería ruido). Si alguna palabra necesitó ayuda o la mostró Nubi, el consejo es normalizar y dar el truco: «Si una no sale, piensa en
cómo empieza o en otra parecida: suele destrabarla.» Nunca se dice que el juego previene el deterioro ni que «recupera la memoria».

**Por qué:** el estado de «la tengo en la punta de la lengua» es normal, crece con la edad aunque el vocabulario se mantenga, y las pistas clásicas para destrabar la palabra
son el largo y la primera letra (los dos primeros peldaños de la escalera): Brown, 1991, *Psychol Bull* 109:204-23, doi:10.1037/0033-2909.109.2.204 (PMID 2034750);
Shafto y otros, 2007, *J Cogn Neurosci* 19:2060-70, doi:10.1162/jocn.2007.19.12.2060 (PMID 17892392).

**Las azules vuelven:** las palabras que Nubi tuvo que mostrar se guardan en la app (preferencias `punta_words`, que SÍ van en el respaldo; hasta 20, las más viejas se sueltan)
y se mandan a Unity (`punta_pending` de la config) para que vuelvan en otra partida (Precisión: hasta 2 y nunca la primera; Reto: una cada 4 palabras). Dejan de esperar
cuando se encuentran solas o con 1-2 ayudas. Es para aprender, no un castigo.

## Carga exacta (id `calculo`, reemplaza a Cálculo Sereno el 4-oct): "Lograste X de N sin pista" y "Tu reactor"

**Qué mide, de verdad:** cuántas de las cargas de la partida lograste SIN la pista de Nubi (cualquier camino vale), con el desglose de las que salieron con pista o quedaron para otra vez,
el tiempo medio de las que salieron sin pista y cuántas fueron por el «camino corto» (los pasos del camino más corto que encuentra el solucionador). No es «tu cálculo mental» ni «tu inteligencia»:
es esta partida, con estas cargas y estas celdas. Se guarda como medida propia (`carga`, % de cargas sin pista) para ver su evolución, comparable solo entre partidas a tu medida y de nivel parecido.

**Cómo se lee:** la cifra grande es «X de N». El tiempo y los caminos cortos solo se dicen con 3 cargas sin pista o más (con menos sería ruido). La pista cuenta como medio acierto para la dificultad
(con pista el nivel no baja de golpe), y nada cae ni apura dentro de una carga: pensar con calma no se penaliza (el motor no usa tiempo de reacción). Si alguna carga necesitó pista, el consejo es
concreto: desde el nivel 3, «mira primero si multiplicar dos celdas te deja cerca de la carga; después ajusta sumando o restando»; antes, «mira cuánto le falta a la celda más grande y busca otra que lo complete».
Nunca se dice que el juego previene el deterioro ni que «mejora la inteligencia».

**Por qué:** la tarea de llegar a un número juntando otros con operaciones aritméticas mezcla memoria de trabajo, planificación y fluidez con los números; que haya MUCHOS caminos (y que el
tiempo no apure) permite medir la estrategia sin que la velocidad tape la comprensión. La medida no se compara con estudios: no hay una referencia con esta misma condición.

## Referencias

- Alvarez, G. A., y Franconeri, S. L. (2007). How many objects can you track? *Journal of Vision*, 7(13):14.
- Anguera, J. A., et al. (2013). Video game training enhances cognitive control in older adults. *Nature*, 501, 97–101.
- Ball, K., et al. (2002). Effects of cognitive training interventions with older adults. *JAMA*, 288, 2271–2281.
- Barth, H. C., y Paladino, A. M. (2011). The development of numerical estimation: evidence against a representational
  shift. *Developmental Science*, 14, 125–135.
- Botvinick, M., y Braver, T. (2015). Motivation and cognitive control: from behavior to neural mechanism. *Annual
  Review of Psychology*, 66, 83–113.
- Bower, G. H. (1970). Imagery as a relational organizer in associative learning. *Journal of Verbal Learning and
  Verbal Behavior*, 9, 529–533.
- Braver, T. S. (2012). The variable nature of cognitive control: a dual mechanisms framework. *Trends in Cognitive
  Sciences*, 16, 106–113.
- Chen, X.-J., Wang, Y., Liu, L.-L., Cui, J.-F., Gan, M.-Y., Shum, D. H. K., y Chan, R. C. K. (2015). The effect of
  implementation intention on prospective memory: a systematic and meta-analytic review. *Psychiatry Research*, 226,
  14–22.
- Coutrot, A., et al. (2018). Global determinants of navigation ability. *Current Biology*, 28, 2861–2866.
- Cooper, L. A., y Shepard, R. N. (1973). Chronometric studies of the rotation of mental images. En W. G. Chase (Ed.),
  *Visual Information Processing*.
- Edwards, J. D., et al. (2005). Reliability and validity of Useful Field of View test scores as administered by
  personal computer. *Journal of Clinical and Experimental Neuropsychology*, 27, 529–543.
- Hafting, T., Fyhn, M., Molden, S., Moser, M.-B., y Moser, E. I. (2005). Microstructure of a spatial map in the
  entorhinal cortex. *Nature*, 436, 801–806.
- Howett, D., et al. (2019). Differentiation of mild cognitive impairment using an entorhinal cortex-based test of
  virtual reality navigation. *Brain*, 142, 1751–1766.
- Klatzky, R. L., Loomis, J. M., Golledge, R. G., et al. (1990). Acquisition of route and survey knowledge in the
  absence of vision. *Journal of Motor Behavior*, 22, 19–43.
- Loomis, J. M., Klatzky, R. L., Golledge, R. G., et al. (1993). Nonvisual navigation by blind and sighted: assessment
  of path integration ability. *Journal of Experimental Psychology: General*, 122, 73–91.
- Aguasvivas, J. A., Carreiras, M., Brysbaert, M., Mandera, P., Keuleers, E., y Duñabeitia, J. A. (2018). SPALEX: a
  Spanish lexical decision database from a massive online data collection. *Frontiers in Psychology*, 9, 2156.
- Ferré, P., y Brysbaert, M. (2017). Can Lextale-Esp discriminate between groups of highly proficient Catalan-Spanish
  bilinguals with different language dominances? *Behavior Research Methods*, 49, 717–723.
- Lemhöfer, K., y Broersma, M. (2012). Introducing LexTALE: a quick and valid lexical test for advanced learners of
  English. *Behavior Research Methods*, 44, 325–343.
- Meyer, D. E., y Schvaneveldt, R. W. (1971). Facilitation in recognizing pairs of words: evidence of a dependence
  between retrieval operations. *Journal of Experimental Psychology*, 90, 227–234.
- Perea, M., y Lupker, S. J. (2003). Transposed-letter confusability effects in masked form priming. *Memory & Cognition*,
  31, 829–841.
- Perea, M., Rosa, E., y Gómez, C. (2002). Is the go/no-go lexical decision task an alternative to the yes/no lexical
  decision task? *Memory & Cognition*, 30, 34–45.
- Roediger, H. L., y Karpicke, J. D. (2006). Test-enhanced learning: taking memory tests improves long-term
  retention. *Psychological Science*, 17, 249–255.
- Rummel, J., y Kvavilashvili, L. (2023). Current theories of prospective memory and new directions for theory
  development. *Nature Reviews Psychology*, 2, 40–54.
- Schneider, M., et al. (2018). Associations of number line estimation with mathematical competence: a meta-analysis.
  *Child Development*, 89, 1467–1484.
- Siegler, R. S., y Opfer, J. E. (2003). The development of numerical estimation. *Psychological Science*, 14, 237–243.
- Simms, V., Clayton, S., Cragg, L., Gilmore, C., y Johnson, S. (2016). Explaining the relationship between number line
  estimation and mathematical achievement: the role of visuomotor integration and visuospatial skills. *Journal of
  Experimental Child Psychology*, 145, 22–33.
- Snodgrass, J. G., y Corwin, J. (1988). Pragmatics of measuring recognition memory. *Journal of Experimental
  Psychology: General*, 117, 34–50.
- Sullivan, J. L., Juhasz, B. J., Slattery, T. J., y Barth, H. C. (2011). Adults' number-line estimation strategies:
  evidence from eye movements. *Psychonomic Bulletin & Review*, 18, 557–563.
- Verbruggen, F., et al. (2019). A consensus guide to capturing the ability to inhibit actions and impulsive behaviors
  in the stop-signal task. *eLife*, 8, e46323.
- Carpenter, P. A., y Just, M. A. (1975). Sentence comprehension: a psycholinguistic processing model of verification.
  *Psychological Review*, 82, 45–73.
- Clare, L., et al. (1993). *Neuropsychologia*, 31, 1225–1241 (doi:10.1016/0028-3932(93)90070-g; título por completar).
- Clark, H. H., y Chase, W. G. (1972). On the process of comparing sentences against pictures. *Cognitive Psychology*, 3, 472–517.
- Collins, A. M., y Quillian, M. R. (1969). Retrieval time from semantic memory. *Journal of Verbal Learning and Verbal
  Behavior*, 8, 240–247.
- Crossland, M. D., Legge, G. E., y Dakin, S. C. (2008). The development of an automated sentence generator for the assessment of
  reading speed. *Behavioral and Brain Functions*, 4, 14.
- Wilson, B., y Baddeley, A. (1988). Semantic, episodic, and autobiographical memory in a postmeningitic amnesic patient.
  *Brain and Cognition*, 8, 31–46.
- Troyer, A. K., Moscovitch, M., y Winocur, G. (1997). Clustering and switching as two components of verbal fluency: evidence from
  younger and older healthy adults. *Neuropsychology*, 11, 138–146.
- Kessels, R. P. C., van Zandvoort, M. J. E., Postma, A., Kappelle, L. J., y de Haan, E. H. F. (2000). The Corsi Block-Tapping Task: standardization and
  normative data. *Applied Neuropsychology*, 7, 252–258. doi:10.1207/S15324826AN0704_8
- Parmentier, F. B. R., Elford, G., y Maybery, M. T. (2005). Transitional information in spatial serial memory: path characteristics affect recall
  performance. *Journal of Experimental Psychology: Learning, Memory, and Cognition*, 31, 412–427. doi:10.1037/0278-7393.31.3.412
- Broadway, J. M., y Engle, R. W. (2010). Validating running memory span: measurement of working memory capacity and links with fluid intelligence.
  *Behavior Research Methods*, 42, 563–570. doi:10.3758/BRM.42.2.563
- MacLeod, C. M. (1991). Half a century of research on the Stroop effect: an integrative review. *Psychological Bulletin*, 109, 163–203. doi:10.1037/0033-2909.109.2.163
- Monsell, S. (2003). Task switching. *Trends in Cognitive Sciences*, 7, 134–140. doi:10.1016/S1364-6613(03)00028-7
- Stroop, J. R. (1935). Studies of interference in serial verbal reactions. *Journal of Experimental Psychology*, 18, 643–662. doi:10.1037/h0054651
- Machado, G. M., Oliveira, M. M., y Fernandes, L. A. F. (2009). A physiologically-based model for simulation of color vision deficiency. *IEEE Transactions on Visualization and Computer Graphics*, 15, 1291–1298. doi:10.1109/TVCG.2009.113
- Cowan, N. (2001). The magical number 4 in short-term memory: a reconsideration of mental storage capacity. *Behavioral and Brain Sciences*, 24, 87–114.
  doi:10.1017/s0140525x01003922
