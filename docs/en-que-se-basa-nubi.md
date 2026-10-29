# «En qué se basa Nubi»: las fuentes, sus DOI y cómo se comprobaron

Tarea 185, 28-oct-2026, y Tarea 186 (la ampliada, el mismo día). Ficha técnica de la pantalla que abre la losa «En qué se basa Nubi» de Tú (`ui/screens/FuentesScreen.kt`; datos en `data/Fuentes.kt` y `data/FuentesMas.kt`). El diseño es de Fable (`auditoria-fable/tu-pestana.md` §3); el nombre lo eligió Ricardo el 27-oct (no «La ciencia de Nubi»: sugiere que Nubi está probada, y no tiene estudios propios). La bibliografía de la T186 la armó Opus con el conector de PubMed (101 referencias confirmadas) y Ricardo aprobó la propuesta el 28-oct; las dos tablas de origen están copiadas en [`docs/bibliografia/`](bibliografia/).

## Qué es y qué no es

De qué estudio sale la TAREA de cada juego y en qué se apoyan las decisiones de la app, en una línea y con el estudio a un toque. **No** es una prueba de que Nubi funcione y no promete mejorar nada: la pantalla lo dice arriba («Nubi no promete mejorar nada: es un juego.») y al final («Lo que no decimos»: que jugar mejore la memoria o la atención fuera del juego no está demostrado; Simons y otros, 2016, y desde la T186 Melby-Lervåg y otros, 2016, Sala y otros, 2018, y Gates y otros, 2020; más el aviso de salud de siempre, `HealthNotice`).

## Qué muestra la pantalla (T186)

1. **Por área, una losa por juego** (como en la T185): emblema, nombre, UNA línea de «qué tomamos» y «Autor, año · Ver el estudio ›»; tocarla abre el estudio principal. **Bajo cada losa, una fila «Más estudios (N)»** (botón lila de 48 dp, PLEGADO por defecto): al tocarla se despliegan, en el mismo lugar, los estudios extra de ese juego, cada uno en su fila tocable (≥ 48 dp): «Autor, año», su línea «qué sustenta» y «Ver el estudio ›», que abre el DOI. 37 estudios extra en los 17 juegos (de 1 a 3 por juego).
2. **«Cómo está hecha Nubi»**, entre los juegos y «Las palabras»: diez grupos, cada uno con su título y UNA frase en simple de lo que hace Nubi; las fuentes van PLEGADAS en un botón lila **«Estudios (N)»** (igual que «Más estudios»; Opus, 28-oct: «lo simple primero», 13 pantallas de deslizar no lo eran), y al desplegarlo cada fuente tiene el mismo formato de fila (32 fuentes en total): «La dificultad se acomoda a ti» (absorbe lo que era «Cómo se ajusta la dificultad»: Levitt y Kaernbach, más Wilson 2019, que va SIEMPRE «en modelos de aprendizaje»), «Más tiempo desde los 65» (Salthouse 1996, que antes estaba en el método, va primero), «Partidas cortas, varios días», «Tu meta y tu racha», «Saber cómo te fue», «Tú eliges», «Solo contigo», «Una partida es una partida», «Fácil de ver y de tocar» y «Con elementos de juego».
3. **Las palabras** (SPALEX y EsPal viven en «Licencias y créditos»), como antes.
4. **«Lo que no decimos»**: la frase de siempre, cuatro revisiones (Simons 2016, Melby-Lervåg 2016, Sala 2018 y Gates 2020, cada una con su línea) y el aviso de salud al final.

Reglas que se mantienen: **solo autor, año, la línea en español y el enlace** (ni el título en inglés ni la revista: `VisibleTextsGuardTest` prohíbe «cognitiv…», «entren…», «cerebro»… en cualquier texto del código y varios títulos y revistas los traen; la página del DOI los muestra a un toque); **la app sigue SIN permiso de internet** (abrir un enlace lo hace el navegador con `Intent(ACTION_VIEW)`; sin navegador, una línea plana lo dice; no hace falta `<queries>`). Entran SOLO las filas de las tablas cuya última columna empieza con «sí» (incluye «sí, con matiz»): **69 de 101**. Las 29 «solo interno» y las 3 de la sección 12 de `biblio_mecanismos.md` (envejecimiento activo y demencia) NO van en la app.

- **Las 17 líneas de «qué tomamos» de las losas principales las APROBÓ Ricardo el 28-oct** (con un cambio: la de Constelaciones dice «…lo que más cambia con los años»). **Las 69 líneas nuevas son BORRADORES**: salieron de la columna «Qué sustenta» de las tablas (subagentes, con revisión de Opus), y 41 de ellas se corrigieron (poco claras, un matiz que faltaba, una palabra prohibida o un tono que podía leerse como promesa): la lista «antes → después» está en [`bibliografia/t186-lineas-y-revisiones.md`](bibliografia/t186-lineas-y-revisiones.md). Las aprueba Ricardo.

## Cómo se comprobó cada DOI (28-oct-2026)

Con `https://doi.org/api/handles/<DOI>` (`responseCode 1` = el DOI existe y a dónde redirige) y con Crossref (`https://api.crossref.org/works/<DOI>`: primer autor y año contra la cita). **T185: los 24 responden y los 24 coinciden en autor y año** (Levitt, Kaernbach, Salthouse, Sweller y EsPal, que Fable había marcado «por confirmar», quedaron confirmados; Sweller 1988 y EsPal/SPALEX no entran en la pantalla —Sweller es de diseño y EsPal/SPALEX van en «Licencias»— y Cooper y Shepard (1973) no tiene DOI: es un capítulo de libro; Acoplamiento cita a Uttal y otros, 2013). **T186: los 69 nuevos responden en doi.org y 68 coinciden con Crossref en autor y año**; el que falta, **Facal y otros, 2012** (10.3275/8586), no está en Crossref porque lo registra otra agencia: el DOI responde, y su ficha (vía negociación de contenido de doi.org: David Facal y otros, 2012, 133 participantes) coincide con la cita. Se usa siempre la forma canónica del DOI, sin barra doble (`10.1037/0278-7393.26.5.1170`, no `10.1037//…`; las dos responden). Años: los de la cita corta de las tablas (año del volumen impreso; «en línea» y «vol.» se quitan).

## Las tablas

### Las 17 fuentes principales (T185) y los estudios sueltos

| Área | Juego | Cita (la misma de «Sobre este juego») | DOI | Línea de «qué tomamos» (aprobada el 28-oct) |
|---|---|---|---|---|
| Memoria | Constelaciones | Old y Naveh-Benjamin, 2008 | 10.1037/0882-7974.23.1.104 | Recordar qué va con qué es lo que más cambia con los años. |
| Memoria | Rastro de luz | Baddeley, 2003 | 10.1038/nrn1201 | Guardar un orden un rato mientras lo usamos: la memoria de trabajo. |
| Memoria | Bodega de carga | Postma, Kessels y van Asselen, 2008 | 10.1016/j.neubiorev.2008.05.001 | Recordar dónde quedó cada cosa: la cosa, el lugar y lo que las une. |
| Memoria | Correo Estelar | Hering y otros, 2014 | 10.1007/s00426-014-0566-4 | Acordarse de hacer algo más tarde sostiene el día a día. |
| Atención | Tinta o Palabra | Diamond, 2013 | 10.1146/annurev-psych-113011-143750 | Frenar la lectura automática y cambiar de regla. |
| Atención | Piloto Estelar | Beurskens y Bock, 2012 | 10.1155/2012/131608 | Hacer dos cosas a la vez se vuelve más difícil con los años. |
| Atención | Freno de Emergencia | Verbruggen y Logan, 2008 | 10.1016/j.tics.2008.07.005 | Frenar una acción que ya empezó, justo a tiempo. |
| Atención | Satélites | Trick, Perl y Sethi, 2005 | 10.1093/geronb/60.2.p102 | Seguir varios objetos que se mueven a la vez. |
| Atención | Rescate relámpago | Owsley, 2013 | 10.1016/j.visres.2012.11.014 | Qué tan rápido vemos y reconocemos algo que aparece un instante. |
| Razonamiento | Carga exacta | Reyna y otros, 2009 | 10.1037/a0017327 | Entender las cantidades ayuda a comprender riesgos y a decidir. |
| Razonamiento | Engranajes | Hegarty, 2004 | 10.1016/j.tics.2004.04.001 | Imaginar cómo se mueven las piezas de una máquina antes de moverlas. |
| Razonamiento | Acoplamiento | Uttal y otros, 2013 | 10.1037/a0028446 | Girar formas en la mente: una habilidad espacial. |
| Razonamiento | Aterrizaje Lunar | Schley y Peters, 2014 | 10.1177/0956797613515485 | Ubicar números a ojo guía cómo valoramos montos. |
| Lenguaje | En la punta de la lengua | Shafto y otros, 2007 | 10.1162/jocn.2007.19.12.2060 | Tener una palabra en la punta de la lengua es normal y crece con los años. |
| Lenguaje | Lluvia de meteoros | Verhaeghen, 2003 | 10.1037/0882-7974.18.2.332 | El vocabulario se mantiene, y hasta crece, con los años. |
| Lenguaje | ¿Verdad o disparate? | Xiang, Kramer y Nordmeyer, 2020 | 10.1037/xlm0000851 | Las frases con «no» piden más esfuerzo a todas las personas. |
| Lenguaje | La estrella intrusa | Mirman, Landrigan y Britt, 2017 | 10.1037/bul0000092 | Ordenamos lo que sabemos por tipo de cosa y por lo que va junto. |
| Palabras | SPALEX (Aguasvivas y otros, 2018) y EsPal (Duchon y otros, 2013) | — | 10.3389/fpsyg.2018.02156 · 10.3758/s13428-013-0326-1 | (ya en «Licencias y créditos»; no entran en esta pantalla) |

### Más estudios, por juego (T186: 37)

| Juego | Cita | DOI | PMID | Línea en la app (borrador) |
|---|---|---|---|---|
| Constelaciones | Naveh-Benjamin, 2000 | 10.1037/0278-7393.26.5.1170 | 11009251 | Con la edad, recordar qué iba con qué cuesta más que recordar cada elemento suelto. |
| Constelaciones | Spencer y Raz, 1995 | 10.1037/0882-7974.10.4.527 | 8749580 | En 46 estudios, recordar dónde o cuándo pasó algo cambió más con los años que recordar qué pasó. |
| Constelaciones | Bender y otros, 2010 | 10.1037/a0020595 | 20822256 | En 278 adultos de distintas edades, reconocer parejas de palabras costó más con los años. |
| Rastro de luz | Berch y otros, 1998 | 10.1006/brcg.1998.1039 | 9841789 | Revisa 25 años de la tarea de Corsi: repetir en orden una secuencia de posiciones. |
| Rastro de luz | Kessels y otros, 2008 | 10.1177/1073191108315611 | 18483192 | En 246 adultos de 50 a 92 años, repetir posiciones en orden y repetir números resultaron tareas distintas. |
| Bodega de carga | Kessels y otros, 2007 | 10.1080/00207450600910218 | 17454244 | En una tarea en computador, unir cada objeto con su lugar costó más a mayores que a jóvenes. |
| Correo Estelar | Henry y otros, 2004 | 10.1037/0882-7974.19.1.27 | 15065929 | En el laboratorio, acordarse de hacer algo ante una señal cuesta más con los años; en la vida diaria, los mayores rinden mejor. |
| Correo Estelar | Einstein y McDaniel, 1990 | 10.1037/0278-7393.16.4.717 | 2142956 | Presentó la tarea de hacer algo cuando aparece una señal y la probó con jóvenes y mayores. |
| Correo Estelar | Kliegel y otros, 2016 | 10.1159/000443698 | 26950339 | Resume cómo cambia con la edad recordar hacer algo más tarde, y su peso en la vida diaria. |
| Tinta o Palabra | MacLeod, 1991 | 10.1037/0033-2909.109.2.163 | 2034749 | Revisa 50 años del efecto Stroop: decir el color de la tinta cuando la palabra dice otro. |
| Tinta o Palabra | Verhaeghen y De Meersman, 1998 | 10.1037/0882-7974.13.1.120 | 9533194 | En 20 estudios, el estorbo de la palabra al nombrar el color fue parecido en jóvenes y mayores, más allá de la rapidez general. |
| Tinta o Palabra | Wasylyshyn y otros, 2011 | 10.1037/a0020912 | 21261411 | Alternar entre dos reglas tiene un costo, y tener las dos reglas en juego pesa más con los años. |
| Piloto Estelar | Verhaeghen y otros, 2003 | 10.1037/0882-7974.18.3.443 | 14518807 | Hacer dos tareas a la vez toma más tiempo a los mayores que a los jóvenes; los aciertos cambian poco. |
| Piloto Estelar | Pashler, 1994 | 10.1037/0033-2909.116.2.220 | 7972591 | Incluso dos tareas simples se estorban al hacerlas juntas, sobre todo al elegir qué responder. |
| Freno de Emergencia | Rey-Mermet y Gade, 2018 | 10.3758/s13423-017-1384-7 | 29019064 | En 176 estudios, la edad cambió poco varias tareas de frenar; en la de la señal de alto, sí. |
| Freno de Emergencia | Williams y otros, 1999 | 10.1037/0012-1649.35.1.205 | 9923475 | En 275 personas de 6 a 81 años, frenar cambió poco en la adultez; responder se hizo más lento. |
| Satélites | Pylyshyn y Storm, 1988 | 10.1163/156856888x00122 | 3153671 | Las personas pueden seguir hasta 5 objetos entre 10 idénticos que se mueven al azar. |
| Satélites | Sekuler y otros, 2008 | 10.1068/p5923 | 18686706 | Seguir varios objetos en movimiento costó más a mayores, sobre todo con más velocidad o más tiempo. |
| Satélites | Meyerhoff y otros, 2017 | 10.3758/s13414-017-1338-1 | 28584953 | Revisa 30 años de la tarea de seguir varios objetos idénticos y cómo se diseña. |
| Rescate relámpago | Waszak y otros, 2008 | 10.1007/s00426-008-0139-5 | 18309516 | En 226 personas de 6 a 88 años, reconocer símbolos que aparecen un instante y se tapan sube hasta la juventud y luego baja. |
| Rescate relámpago | Enns y Di Lollo, 2000 | 10.1016/s1364-6613(00)01520-5 | 10962616 | Una imagen breve puede volverse invisible si otra la sigue, y la atención influye en cuánto se tapa. |
| Carga exacta | Duverne y Lemaire, 2004 | 10.1093/geronb/59.3.p135 | 15118017 | En 138 adultos de 20 a 80 años, el cálculo de sumas se mantuvo; cambiaron estrategia y rapidez. |
| Carga exacta | Lipkus y otros, 2001 | 10.1177/0272989X0102100105 | 11206945 | Incluso con estudios superiores, a muchos adultos les cuestan las preguntas simples con números. |
| Engranajes | Hegarty, 1992 | 10.1037/0278-7393.18.5.1084 | 1402712 | Para saber cómo se mueve una máquina dibujada, las personas la animan mentalmente pieza por pieza. |
| Engranajes | Hegarty y Sims, 1994 | 10.3758/bf03200867 | 7934947 | Verificar el movimiento de una pieza cuesta más cuantas más piezas hay que animar en la cadena. |
| Acoplamiento | Shepard y Metzler, 1971 | 10.1126/science.171.3972.701 | 5540314 | Decidir si dos figuras son la misma tarda más cuanto más hay que girarlas. |
| Acoplamiento | Techentin y otros, 2014 | 10.1080/0361073X.2014.926773 | 25054640 | En 80 muestras, las tareas espaciales como girar figuras cambiaron con los años, más en el tiempo que en los aciertos. |
| Aterrizaje Lunar | Siegler y Opfer, 2003 | 10.1111/1467-9280.02438 | 12741747 | Ubicar números en una recta se vuelve más proporcional con la edad y la experiencia con números. |
| En la punta de la lengua | Brown, 1991 | 10.1037/0033-2909.109.2.204 | 2034750 | Tener una palabra en la punta de la lengua le pasa a casi todo el mundo, más o menos cada semana, y es más común con los años. |
| En la punta de la lengua | Gollan y Brown, 2006 | 10.1037/0096-3445.135.3.462 | 16846276 | Con los años aumentan los bloqueos con palabras difíciles, no con las fáciles. |
| En la punta de la lengua | Facal y otros, 2012 | 10.3275/8586 | 22960259 | En 133 adultos, los bloqueos con las palabras aumentaron con los años y se ligaron a la rapidez, no al vocabulario. |
| Lluvia de meteoros | Ratcliff y otros, 2004 | 10.1037/0882-7974.19.2.278 | 15222821 | Al decidir si algo es una palabra, personas de 60 a 75 años fueron más lentas, pero más precisas. |
| Lluvia de meteoros | Brysbaert y otros, 2016 | 10.3389/fpsyg.2016.01116 | 27524974 | Entre los 20 y 60 años, una persona aprende unas 6.000 palabras nuevas (datos en inglés). |
| ¿Verdad o disparate? | Nieuwland y Kuperberg, 2008 | 10.1111/j.1467-9280.2008.02226.x | 19121125 | Una frase con «no» que cae bien en su contexto se juzga verdadera o falsa sin trabarse. |
| ¿Verdad o disparate? | Dale y Duran, 2011 | 10.1111/j.1551-6709.2010.01164.x | 21463359 | Verificar frases con «no» es más trabado, pero un contexto adecuado reduce esa dificultad. |
| La estrella intrusa | Lin y Murphy, 2001 | 10.1037/0096-3445.130.1.3 | 11293459 | Los adultos también agrupan por relaciones de escena (torta y velas), no solo por tipo de cosa. |
| La estrella intrusa | Minto-García y otros, 2020 | 10.1007/s10936-020-09708-5 | 32519228 | En 120 mayores hispanohablantes, las palabras que se asocian mezclan el tipo de cosa y el contexto (cuna y bebé). |

### «Cómo está hecha Nubi» (T186: 32 fuentes en 10 grupos; Levitt, Kaernbach y Salthouse ya estaban desde la T185)

| Grupo | Cita | DOI | PMID | Línea en la app (borrador) |
|---|---|---|---|---|
| La dificultad se acomoda a ti | Levitt, 1971 | 10.1121/1.1912375 | — | La escalera «sube y baja»: el nivel sube con los aciertos y baja con los errores hasta quedar cerca de un nivel de aciertos fijo. |
| La dificultad se acomoda a ti | Kaernbach, 1991 | 10.3758/BF03214307 | — | La escalera con pasos de distinto peso permite quedarse cerca de 8 aciertos de cada 10. |
| La dificultad se acomoda a ti | Wilson y otros, 2019 | 10.1038/s41467-019-12552-4 | 31690723 | En modelos de aprendizaje, el punto más provechoso está cerca de 85 aciertos de cada 100. |
| Más tiempo desde los 65 | Salthouse, 1996 | 10.1037/0033-295x.103.3.403 | — | Con los años cambia la rapidez para procesar información, y eso explica parte de las diferencias entre edades. |
| Más tiempo desde los 65 | Verhaeghen y Salthouse, 1997 | 10.1037/0033-2909.122.3.231 | 9354147 | En 91 estudios, la rapidez para responder baja con la edad, y baja más después de los 50 años. |
| Más tiempo desde los 65 | Starns y Ratcliff, 2010 | 10.1037/a0018022 | 20545422 | Las personas mayores prefieren no equivocarse aunque tarden más; las personas jóvenes equilibran rapidez y aciertos. |
| Partidas cortas, varios días | Cepeda y otros, 2006 | 10.1037/0033-2909.132.3.354 | 16719566 | Al recordar palabras, repartir la práctica en varios días suele dejar mejor recuerdo que juntarla en un solo día. |
| Partidas cortas, varios días | Kornell y otros, 2010 | 10.1037/a0017807 | 20545435 | Espaciar la práctica sirvió a jóvenes y a mayores, aunque los participantes creían que juntarla rendía más. |
| Partidas cortas, varios días | Simone y otros, 2013 | 10.1093/geronb/gbs096 | 23103383 | Repartir la práctica en días distintos también ayudó a personas mayores, aunque algo menos que a jóvenes. |
| Tu meta y tu racha | Gardner y otros, 2012 | 10.3399/bjgp12X659466 | 23211256 | Repetir una acción en el mismo momento y lugar la vuelve más automática con el tiempo. |
| Tu meta y tu racha | Lally y otros, 2011 | 10.1080/13548506.2011.555774 | 21749245 | En un estudio pequeño, de 10 personas, los fines de semana y las vacaciones cortaron la rutina, pero después se retomó. |
| Tu meta y tu racha | Singh y otros, 2024 | 10.3390/healthcare12232488 | 39685110 | En 20 estudios, un hábito tardó unos dos meses en formarse, con grandes diferencias entre personas. |
| Tu meta y tu racha | Epton y otros, 2017 | 10.1037/ccp0000260 | 29189034 | En 141 artículos, ponerse una meta ayudó un poco a cambiar conductas, más si era exigente o compartida. |
| Tu meta y tu racha | Locke y Latham, 2002 | 10.1037/0003-066x.57.9.705 | 12237980 | Las metas claras y concretas orientan el esfuerzo mejor que un «haz lo que puedas». |
| Saber cómo te fue | Wisniewski y otros, 2020 | 10.3389/fpsyg.2019.03087 | 32038429 | En 435 estudios escolares, recibir información sobre lo hecho ayudó a aprender, sobre todo si decía algo útil de la tarea. |
| Saber cómo te fue | Cyr y Anderson, 2015 | 10.1037/xlm0000073 | 25347615 | Equivocarse no siempre perjudica: en jóvenes y mayores, algunos errores sirvieron de paso hacia la respuesta. |
| Tú eliges | Ryan y Deci, 2000 | 10.1037/0003-066x.55.1.68 | 11392867 | Sentirse capaz, con libertad de elegir y acompañado sostiene las ganas propias de hacer algo. |
| Tú eliges | Ng y otros, 2012 | 10.1177/1745691612447309 | 26168470 | En 184 conjuntos de datos, sentir libertad de elegir se asoció con más ganas propias de mantener un hábito; es una asociación. |
| Solo contigo | Lamont y otros, 2015 | 10.1037/a0038586 | 25621742 | En 32 artículos, recordar estereotipos negativos sobre la edad empeoró el desempeño de personas mayores en tareas mentales. |
| Solo contigo | Armstrong y otros, 2017 | 10.1093/geront/gnx056 | 28854607 | En 23 artículos, tener presente el estereotipo de la edad bajó el recuerdo de las personas mayores, de forma pequeña a moderada. |
| Solo contigo | Hess y otros, 2003 | 10.1093/geronb/58.1.p3 | 12496296 | Con el estereotipo de la edad presente, las personas mayores recordaron menos, sobre todo quienes valoraban su memoria. |
| Solo contigo | Wilson y Ross, 2000 | 10.1037/0022-3514.78.5.928 | 10821199 | Las personas se comparan con su propio pasado tanto como con otras, y eso suele sentar mejor. |
| Una partida es una partida | Calamia y otros, 2012 | 10.1080/13854046.2012.680913 | 22540222 | En unos 1.600 resultados, repetir la misma prueba subió el puntaje por pura práctica, algo que hay que tener en cuenta. |
| Una partida es una partida | Hedge y otros, 2018 | 10.3758/s13428-017-0935-1 | 28726177 | En tareas clásicas de reacción, el puntaje de una misma persona cambia bastante de una vez a otra. |
| Fácil de ver y de tocar | Wildenbos y otros, 2018 | 10.1016/j.ijmedinf.2018.03.012 | 29673606 | La vista, el tacto, la memoria y la motivación cambian con la edad y afectan el uso de apps. |
| Fácil de ver y de tocar | Owsley, 2011 | 10.1016/j.visres.2010.10.020 | 20974168 | Con la edad baja la capacidad de distinguir contrastes y de ver con poca luz. |
| Fácil de ver y de tocar | Yeh, 2020 | 10.1016/j.heliyon.2020.e04147 | 32637675 | En un estudio con aparatos de salud, con letra de 22 puntos, las personas de 65 años o más usaron la pantalla tan bien como las personas jóvenes. |
| Fácil de ver y de tocar | Yeh, 2025 | 10.1177/00315125251345594 | 40440033 | En 110 personas de 65 años o más, los botones de 16 milímetros rindieron mejor. |
| Fácil de ver y de tocar | Staab y otros, 2017 | 10.3233/VES-170622 | 29036855 | En ciertos mareos crónicos, las imágenes en movimiento o muy cargadas empeoran los síntomas. |
| Con elementos de juego | Lumsden y otros, 2016 | 10.2196/games.5888 | 27421244 | En 33 estudios, los elementos de juego solían dar más ganas de participar; su efecto en el desempeño fue mixto. |
| Con elementos de juego | Vermeir y otros, 2020 | 10.2196/18644 | 32773374 | En 49 estudios, las tareas con elementos de juego resultaron más motivadoras y más exigentes, sin cambio en el desempeño. |
| Con elementos de juego | Koivisto y Malik, 2021 | 10.1093/geront/gnaa047 | 32530026 | En personas de 55 años o más, los elementos de juego muestran señales positivas, pero la evidencia todavía es débil. |

### «Lo que no decimos» (T186: Simons 2016 y tres más)

| Cita | DOI | PMID | Línea en la app (borrador) |
|---|---|---|---|
| Simons y otros, 2016 | 10.1177/1529100616661983 | — | Revisó los estudios de los programas de juegos mentales: lo practicado mejora, pero no hay evidencia suficiente de que mejore lo demás. |
| Melby-Lervåg y otros, 2016 | 10.1177/1745691616635612 | 27474138 | En 87 publicaciones, practicar tareas de memoria hizo mejor esas mismas tareas, pero no otras habilidades. |
| Sala y otros, 2018 | 10.1037/bul0000139 | 29239631 | En más de 300 resultados, jugar videojuegos no mostró mejoras en habilidades mentales fuera del juego. |
| Gates y otros, 2020 | 10.1002/14651858.CD012277.pub3 | 32104914 | En 8 ensayos con personas de 65 años o más, los programas de ejercicios en pantalla mostraron beneficios pequeños e inciertos, que no se mantuvieron al año. |

### Lo que NO entra (32 filas: 29 «solo interno» y las 3 de la sección 12 de `biblio_mecanismos.md`)

Están en las tablas de `docs/bibliografia/` con su razón (diferencias por sexo, neuroimagen, lecturas que suenan a promesa, resultados que contradicen un mecanismo, etc.). La sección 12 (envejecimiento activo y demencia) solo trae asociaciones y nunca puede leerse como «jugar protege». `FuentesTest` compara las tablas con la pantalla: lo que se ve es EXACTAMENTE las 69 filas «sí» más lo que ya estaba desde la T185.

| Cita | DOI | Razón (resumida) |
|---|---|---|
| Claessen y otros, 2014 | 10.1080/23279095.2014.892488 | solo interno: advierte que la versión digital no equivale a la física; 40 universitarios; título con «cognitiv |
| Kessels y otros, 1999 | 10.3758/bf03200721 | solo interno: es metodología, sin datos de personas. |
| Voyer y otros, 2007 | 10.3758/bf03194024 | solo interno: su tema son las diferencias por sexo. |
| Al-Yahya y otros, 2011 | 10.1016/j.neubiorev.2010.08.008 | solo interno: es marcha, no conducir; roza temas de salud; título con «Cognitive». |
| Verbruggen y otros, 2019 | 10.7554/eLife.46323 | solo interno: guía técnica para revisar el diseño del juego; el resumen habla de trastornos. |
| Salthouse, 1996 | 10.1037/0033-295x.103.3.403 | solo interno: teoría general; título con «cognition». |
| Peters y otros, 2006 | 10.1111/j.1467-9280.2006.01720.x | solo interno: se puede leer como «jugar mejora tus decisiones». |
| Battaglia y otros, 2013 | 10.1073/pnas.1306572110 | solo interno: usa torres de bloques, no engranajes. |
| Zacks, 2008 | 10.1162/jocn.2008.20013 | solo interno: es de neuroimagen. |
| Schneider y otros, 2018 | 10.1111/cdev.13068 | solo interno: muestra de 4 a 14 años. |
| Eyler y otros, 2018 | 10.1186/s12911-018-0618-6 | solo interno: el título habla de «mejorar». |
| Aguasvivas y otros, 2018 | 10.3389/fpsyg.2018.02156 | solo interno: útil para elegir palabras y seudopalabras; PubMed no trae resumen. |
| Wingfield y Grossman, 2006 | 10.1152/jn.00628.2006 | solo interno: es de neuroimagen y habla de «compensación». |
| Hebart y otros, 2020 | 10.1038/s41562-020-00951-3 | solo interno: el resumen de PubMed no nombra la tarea de «el intruso» (ver Cautelas). |
| Brehmer y otros, 2012 | 10.3389/fnhum.2012.00063 | Solo interno. Afirma transferencia lejana y eso choca con los metaanálisis de la sección 11. |
| von Bastian y Eschen, 2016 | 10.1007/s00426-015-0655-z | Solo interno. Va en contra de que la adaptación sea necesaria. Ver Cautelas. |
| Starns y Ratcliff, 2012 | 10.3758/s13423-011-0189-3 | Solo interno. Es clave para el Contrarreloj. Ver Cautelas. |
| Wulf y otros, 2012 | 10.1037/a0025718 | Solo interno. Usó un dato falso («estás sobre el promedio»), cosa que Nubi no hará. Además la tarea era de mov |
| Scheper y otros, 2021 | 10.1007/s40520-020-01603-2 | Solo interno. La evidencia sobre los errores es mixta. |
| Ingalls y otros, 2026 | 10.1371/journal.pone.0317254 | Solo interno. Respalda la racha que perdona. Ver Cautelas. |
| Peeters y otros, 2023 | 10.1007/s11065-022-09537-4 | Solo interno. Va en contra de dos elementos de Nubi. Ver Cautelas. |
| Wulf y Lewthwaite, 2016 | 10.3758/s13423-015-0999-9 | Solo interno. Trata del movimiento e incluye mecanismos especulativos. |
| Gardner y Lally, 2013 | 10.1007/s10865-012-9442-0 | Solo interno. Une las secciones 5 y 7. Es una muestra joven y de diseño transversal. |
| Lewthwaite y Wulf, 2010 | 10.1080/17470210903111839 | Solo interno. Matiza «nunca comparar». Ver Cautelas. |
| Sliwinski y otros, 2018 | 10.1177/1073191116643164 | Solo interno. Una sola partida es poco estable y el promedio de varias lo es más. Ver Cautelas. |
| Gobet y Sala, 2023 | 10.1177/17456916221091830 | Solo interno. Es una postura fuerte y discutida en el campo. |
| Lampit y otros, 2014 | 10.1371/journal.pmed.1001756 | Solo interno. El efecto positivo se puede leer como promesa, y los moderadores van contra el diseño de Nubi. |
| Rebok y otros, 2014 | 10.1111/jgs.12607 | Solo interno. Es un resultado positivo con riesgo de leerse como promesa. Eran sesiones guiadas en grupo, no u |
| Edwards y otros, 2017 | 10.1016/j.trci.2017.09.002 | Solo interno, nunca en la app. |
| Livingston y otros, 2024 | 10.1016/S0140-6736(24)01296-0 | Solo interno. PubMed no trae resumen, así que el contenido se describe según el informe y conviene verificarlo |
| Kuiper y otros, 2015 | 10.1016/j.arr.2015.04.006 | Solo interno. |
| Su y otros, 2022 | 10.1212/WNL.0000000000200929 | Solo interno. |

## Pruebas

`FuentesTest` (los 17 juegos de la app y solo ellos; la cita de cada uno es la misma de «Sobre este juego»; 90 DOI con forma `10.xxxx/…`, sin barra doble y sin repetirse; ninguna cita trae título en inglés ni revista; cada línea extra es una frase corta en español sin palabras prohibidas ni promesas; los 37 «Más estudios» sin repetir el estudio principal; los diez grupos con su título, su frase y sus fuentes —Wilson 2019 «en modelos de aprendizaje»—; «Lo que no decimos» con sus cuatro revisiones; Salthouse 1996 abre «Más tiempo desde los 65»; las líneas de Ng y de Gates son las que aprobó Opus; **la pantalla muestra EXACTAMENTE las 69 filas «sí» de `docs/bibliografia/` y ninguna «solo interno»**; el encuadre; el navegador y la ausencia de permiso de internet) y `FuentesYContactoTest` (la pantalla: las cuatro áreas y las 17 losas se alcanzan deslizando, cada una abre el SUYO; «Más estudios» plegado por defecto, se despliega en el mismo lugar y se pliega de nuevo, el botón y las filas miden 48 dp o más con letra 1,0 y 2,0 y no se salen del ancho, los 37 estudios extra y las 32 fuentes de los diez grupos abren cada una su DOI; «Cómo está hecha Nubi» va entre los juegos y «Las palabras» con los diez grupos PLEGADOS («Estudios (N)»: cada grupo se despliega bajo su frase, el botón mide 48 dp o más y las 32 fuentes abren cada una su DOI); sin navegador lo dice; «Lo que no decimos» con las cuatro revisiones y el aviso de salud al final). Las capturas de la lámina salen de `Tarea186CapturasTest` (solo al grabar).

## Medidas (T186, letra 1,0 / 1,3 / 2,0; `tools/previews/tarea_186_medidas.txt`)

**Con los grupos plegados (lo que quedó):** «En qué se basa Nubi» mide **7 009 dp con letra 1,0** (8 698 con 1,3 y 13 315 con 2,0; en la T185 medía 3 917) y **«Cómo está hecha Nubi» sola mide 1 781 dp con 1,0** (2 121 con 1,3 y 3 324 con 2,0). Con los 17 «Más estudios» desplegados, 12 871 dp con 1,0 (16 290 con 1,3); con los diez grupos desplegados la sección mide 6 965 dp. Cada fila de estudio mide entre 121 y 164 dp con 1,0 (171 a 225 con 1,3); «Más estudios» y «Estudios» miden 48 dp (50 con 2,0). **Antes de plegar los grupos** (la lámina `tarea-186.png`): 11 571 dp la pantalla y 6 343 dp la sección con 1,0 (14 636 y 8 086 con 1,3).

## Pendiente (hoja de ruta, Etapa 4)

Que Ricardo apruebe las 69 líneas nuevas (borradores; Opus ya aprobó los cambios de Ng, Gates y Salthouse y las dos revisiones, que se aplicaron: la racha en cero en Hoy y el aviso, y el rastro de esta partida con su pie propio: `bibliografia/t186-lineas-y-revisiones.md`); el correo de Nubi (`NubiContact.EMAIL`, hoy `null`: la losa dice «Correo por definir»); la política de privacidad (Google Play la pide para todas las apps; no hay página ni enlace todavía); y la declaración de salud de Google Play («Mental Acuity»).
