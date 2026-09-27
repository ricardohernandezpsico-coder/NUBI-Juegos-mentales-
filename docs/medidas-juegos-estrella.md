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
- Cooper, L. A., y Shepard, R. N. (1973). Chronometric studies of the rotation of mental images. En W. G. Chase (Ed.),
  *Visual Information Processing*.
- Edwards, J. D., et al. (2005). Reliability and validity of Useful Field of View test scores as administered by
  personal computer. *Journal of Clinical and Experimental Neuropsychology*, 27, 529–543.
- Roediger, H. L., y Karpicke, J. D. (2006). Test-enhanced learning: taking memory tests improves long-term
  retention. *Psychological Science*, 17, 249–255.
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
