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

## Radar: "Tu vistazo" y "Tu radar"

**Qué mide.** La duración de destello más corta con la que se acierta de forma estable, en una tarea tipo campo visual
útil (UFOV). El UFOV original usa una escalera hacia el 75% de aciertos, con duraciones de 17 a 500 ms. Nuestra
escalera apunta a ~80%, por eso el texto dice "aciertas unas 4 de cada 5 veces". La duración se mide en cuadros reales
de la pantalla.

**Qué NO se dice.** No se compara con las normas clínicas del UFOV: otro aparato, otro tamaño de pantalla y otra
distancia a los ojos.

**Radar por dirección.** Con unos 3 destellos por dirección, las diferencias son azar. Ahora solo se nombra dónde se
rescató más y dónde menos si hay **4 o más destellos en cada dirección** y **40 puntos de diferencia**. Aun así se dice
"en esta partida" y "si se repite". Antes bastaban 2 destellos y 20 puntos.

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

**Qué NO se dice.** No se compara con normas ni se habla de "memoria prospectiva" como rasgo: con 150 s de vuelo y unos
7 encargos de cada tipo, la medida es de esta partida.

---

## Referencias

- Alvarez, G. A., y Franconeri, S. L. (2007). How many objects can you track? *Journal of Vision*, 7(13):14.
- Anguera, J. A., et al. (2013). Video game training enhances cognitive control in older adults. *Nature*, 501, 97–101.
- Ball, K., et al. (2002). Effects of cognitive training interventions with older adults. *JAMA*, 288, 2271–2281.
- Barth, H. C., y Paladino, A. M. (2011). The development of numerical estimation: evidence against a representational
  shift. *Developmental Science*, 14, 125–135.
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
