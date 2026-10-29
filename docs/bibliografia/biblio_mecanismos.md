> **De dónde salió este archivo:** copia de `auditoria-fable/bibliografia-opus-28oct/biblio_mecanismos.md` (la hizo Opus con el conector de PubMed el 28-oct-2026; Ricardo aprobó esa propuesta el mismo día). En la app (Tarea 186, `docs/en-que-se-basa-nubi.md`) entran SOLO las filas cuya última columna empieza con «sí» (incluye «sí, con matiz»); las marcadas «solo interno» NO van en la app. `FuentesTest` compara este archivo con lo que muestra la pantalla.

# Bibliografía para los mecanismos de diseño de Nubi

Fuente: PubMed (conector PubMed). La búsqueda se hizo el 28-oct-2026 y no se usó búsqueda web.
Cada referencia de las tablas se confirmó con los metadatos de PubMed (PMID, DOI, primer autor, año y revista).
No se repiten las fuentes que la app ya cita: Levitt 1971, Kaernbach 1991, Salthouse 1996 y Simons y otros 2016.

Convenciones:
- **Qué sustenta**: una frase en español llano, de 20 palabras o menos, sin promesas y sin las palabras vetadas. Puede ir en la app solo si la última columna dice **sí**.
- **Año**: es el año del volumen impreso. Si PubMed muestra antes la versión en línea, se anota «en línea».
- **ECA** = ensayo controlado aleatorizado. **RS** = revisión sistemática. **MA** = metaanálisis.

---

## 1. Dificultad adaptativa (cerca de 80-85 % de aciertos)

| Cita corta | Título original | Revista | PMID | DOI | Tipo | Qué sustenta | ¿Para la app? |
|---|---|---|---|---|---|---|---|
| Wilson y otros, 2019 | The Eighty Five Percent Rule for optimal learning | Nat Commun 10(1):4646 | 31690723 | [10.1038/s41467-019-12552-4](https://doi.org/10.1038/s41467-019-12552-4) | Teórico y de simulación (algoritmos, no personas) | En modelos de aprendizaje, el punto más provechoso está cerca de 85 aciertos de cada 100. | **Sí, con matiz.** Hay que decir «en modelos de aprendizaje». No se probó con personas. |
| Brehmer y otros, 2012 | Working-memory training in younger and older adults: training gains, transfer, and maintenance | Front Hum Neurosci 6:63 | 22470330 | [10.3389/fnhum.2012.00063](https://doi.org/10.3389/fnhum.2012.00063) | ECA (55 de 20-30 años y 45 de 60-70 años) | Ajustar la dificultad a cada persona dio más avance en lo practicado que un nivel fijo fácil. | **Solo interno.** Afirma transferencia lejana y eso choca con los metaanálisis de la sección 11. |
| von Bastian y Eschen, 2016 (en línea 2015) | Does working memory training have to be adaptive? | Psychol Res 80(2):181-94 | 25716189 | [10.1007/s00426-015-0655-z](https://doi.org/10.1007/s00426-015-0655-z) | ECA (130 adultos jóvenes) | Variar la dificultad bastó: ajustarla a cada persona no rindió más que variarla al azar. | **Solo interno.** Va en contra de que la adaptación sea necesaria. Ver Cautelas. |

Nota técnica: con la regla clásica «n correctas seguidas suben, 1 error baja», la escalera se estabiliza donde p^n = 0,5. Eso da 70,7 % con n = 2, 79,4 % con n = 3 y 84,1 % con n = 4 (Levitt 1971, ya citado). Para llegar a ~85 % se necesita n = 4 o pasos con peso distinto (Kaernbach 1991). Conviene revisar qué regla usa cada juego.

---

## 2. Meta más fácil y ritmo más lento para 65+

| Cita corta | Título original | Revista | PMID | DOI | Tipo | Qué sustenta | ¿Para la app? |
|---|---|---|---|---|---|---|---|
| Verhaeghen y Salthouse, 1997 | Meta-analyses of age-cognition relations in adulthood: estimates of linear and nonlinear age effects and structural models | Psychol Bull 122(3):231-49 | 9354147 | [10.1037/0033-2909.122.3.231](https://doi.org/10.1037/0033-2909.122.3.231) | MA (91 estudios) | La rapidez para responder baja con la edad, y baja más después de los 50 años. | **Sí.** Describe un cambio normal. Úsese para explicar por qué se da más tiempo, no para etiquetar a nadie. |
| Starns y Ratcliff, 2010 | The effects of aging on the speed-accuracy compromise: Boundary optimality in the diffusion model | Psychol Aging 25(2):377-90 | 20545422 | [10.1037/a0018022](https://doi.org/10.1037/a0018022) | Experimental | Las personas mayores prefieren no equivocarse aunque tarden más. Los jóvenes equilibran rapidez y precisión. | **Sí.** Justifica un ritmo más pausado para 65+. |
| Starns y Ratcliff, 2012 | Age-related differences in diffusion model boundary optimality with both trial-limited and time-limited tasks | Psychon Bull Rev 19(1):139-45 | 22144142 | [10.3758/s13423-011-0189-3](https://doi.org/10.3758/s13423-011-0189-3) | Experimental | Aun con tiempo límite, las personas mayores siguen cuidando más la precisión que la rapidez. | **Solo interno.** Es clave para el Contrarreloj. Ver Cautelas. |

---

## 3. Sesiones cortas diarias (~2 min por juego) y meta semanal de N días

| Cita corta | Título original | Revista | PMID | DOI | Tipo | Qué sustenta | ¿Para la app? |
|---|---|---|---|---|---|---|---|
| Cepeda y otros, 2006 | Distributed practice in verbal recall tasks: A review and quantitative synthesis | Psychol Bull 132(3):354-80 | 16719566 | [10.1037/0033-2909.132.3.354](https://doi.org/10.1037/0033-2909.132.3.354) | MA (317 experimentos) | Repartir la práctica en varios días suele dejar mejor recuerdo que concentrarla en una sola sesión. | **Sí, con matiz.** El metaanálisis trata de recordar material verbal, no de juegos. |
| Kornell y otros, 2010 | Spacing as the friend of both memory and induction in young and older adults | Psychol Aging 25(2):498-503 | 20545435 | [10.1037/a0017807](https://doi.org/10.1037/a0017807) | Experimental (jóvenes y mayores) | Espaciar la práctica sirvió a jóvenes y a mayores, aunque ellos creían lo contrario. | **Sí.** |
| Simone y otros, 2013 (en línea 2012) | Diminished but not forgotten: effects of aging on magnitude of spacing effect benefits | J Gerontol B Psychol Sci Soc Sci 68(5):674-80 | 23103383 | [10.1093/geronb/gbs096](https://doi.org/10.1093/geronb/gbs096) | Experimental (51 jóvenes y 54 mayores) | Repartir la práctica en días distintos también ayudó a personas mayores, aunque algo menos que a jóvenes. | **Sí.** |

No se encontró en PubMed ninguna fuente que respalde específicamente sesiones de ~2 minutos. Lampit y otros 2014 (sección 11) apunta en otra dirección. Ver Cautelas.

---

## 4. Retroalimentación inmediata y sin culpa

| Cita corta | Título original | Revista | PMID | DOI | Tipo | Qué sustenta | ¿Para la app? |
|---|---|---|---|---|---|---|---|
| Wisniewski y otros, 2020 (vol. 2019) | The Power of Feedback Revisited: A Meta-Analysis of Educational Feedback Research | Front Psychol 10:3087 | 32038429 | [10.3389/fpsyg.2019.03087](https://doi.org/10.3389/fpsyg.2019.03087) | MA (435 estudios) | Recibir información sobre lo hecho ayuda a aprender, sobre todo si dice algo útil de la tarea. | **Sí.** Es de contexto escolar. |
| Wulf y otros, 2012 (en línea 2011) | Altering mindset can enhance motor learning in older adults | Psychol Aging 27(1):14-21 | 21988153 | [10.1037/a0025718](https://doi.org/10.1037/a0025718) | ECA (mujeres de 64-71 años) | En personas mayores, esperar que les irá bien redujo los nervios y mejoró lo aprendido en equilibrio. | **Solo interno.** Usó un dato falso («estás sobre el promedio»), cosa que Nubi no hará. Además la tarea era de movimiento. |
| Cyr y Anderson, 2015 (en línea 2014) | Mistakes as stepping stones: Effects of errors on episodic memory among younger and older adults | J Exp Psychol Learn Mem Cogn 41(3):841-50 | 25347615 | [10.1037/xlm0000073](https://doi.org/10.1037/xlm0000073) | Experimental | Equivocarse no siempre perjudica: en jóvenes y mayores, algunos errores sirvieron de paso hacia la respuesta. | **Sí.** Respalda tratar el error como parte del juego. |
| Scheper y otros, 2021 (en línea 2020) | Prevention is better than cure: effects of errors on memory performance during spatial learning in healthy aging | Aging Clin Exp Res 33(4):997-1003 | 32474856 | [10.1007/s40520-020-01603-2](https://doi.org/10.1007/s40520-020-01603-2) | Experimental (68 mayores y 60 jóvenes) | Aprender sin errores ayudó a jóvenes y mayores, pero la cantidad de errores no cambió el recuerdo. | **Solo interno.** La evidencia sobre los errores es mixta. |

No se encontró en PubMed ninguna fuente sobre culpa o tono emocional de la retroalimentación en adultos mayores con tareas digitales. Las fuentes de esta tabla tratan del error y de las expectativas, no del tono.

---

## 5. Racha diaria que perdona un día de descanso y meta semanal

| Cita corta | Título original | Revista | PMID | DOI | Tipo | Qué sustenta | ¿Para la app? |
|---|---|---|---|---|---|---|---|
| Gardner y otros, 2012 | Making health habitual: the psychology of 'habit-formation' and general practice | Br J Gen Pract 62(605):664-6 | 23211256 | [10.3399/bjgp12X659466](https://doi.org/10.3399/bjgp12X659466) | Artículo narrativo (sin resumen en PubMed) | Repetir una acción en el mismo momento y lugar la vuelve más automática con el tiempo. | **Sí.** |
| Lally y otros, 2011 | Experiences of habit formation: a qualitative study | Psychol Health Med 16(4):484-9 | 21749245 | [10.1080/13548506.2011.555774](https://doi.org/10.1080/13548506.2011.555774) | Cualitativo (10 personas) | Fines de semana y vacaciones cortaron la rutina por un tiempo, pero después se retomó. | **Sí, con matiz.** Es un estudio muy pequeño. Respalda el día de descanso. |
| Singh y otros, 2024 | Time to Form a Habit: A Systematic Review and Meta-Analysis of Health Behaviour Habit Formation and Its Determinants | Healthcare (Basel) 12(23) | 39685110 | [10.3390/healthcare12232488](https://doi.org/10.3390/healthcare12232488) | RS y MA (20 estudios) | Un hábito tarda unos dos meses en formarse, con grandes diferencias entre una persona y otra. | **Sí, con matiz.** La mayoría de los estudios tiene alto riesgo de sesgo. |
| Ingalls y otros, 2026 | The dark side of streaking: Examining the backfire potential of run streaking in recreational runners who broke a long-term streak | PLoS One 21(5):e0317254 | 42172224 | [10.1371/journal.pone.0317254](https://doi.org/10.1371/journal.pone.0317254) | Cualitativo (17 corredores) | Al cortar una racha larga, algunas personas sintieron tristeza, y algunas corrían lesionadas para no perderla. | **Solo interno.** Respalda la racha que perdona. Ver Cautelas. |
| Epton y otros, 2017 | Unique effects of setting goals on behavior change: Systematic review and meta-analysis | J Consult Clin Psychol 85(12):1182-1198 | 29189034 | [10.1037/ccp0000260](https://doi.org/10.1037/ccp0000260) | RS y MA (141 artículos) | Ponerse metas ayuda algo a cambiar conductas, y más si la meta es exigente o compartida. | **Sí, con matiz.** La meta de Nubi es personal y modesta, así que el efecto esperable es menor. |
| Locke y Latham, 2002 | Building a practically useful theory of goal setting and task motivation. A 35-year odyssey | Am Psychol 57(9):705-17 | 12237980 | [10.1037//0003-066x.57.9.705](https://doi.org/10.1037/0003-066x.57.9.705) | Revisión teórica (fundacional) | Las metas claras y concretas orientan el esfuerzo mejor que un «haz lo que puedas». | **Sí.** |

---

## 6. Elementos de juego y motivación (incluye personas mayores)

| Cita corta | Título original | Revista | PMID | DOI | Tipo | Qué sustenta | ¿Para la app? |
|---|---|---|---|---|---|---|---|
| Lumsden y otros, 2016 | Gamification of Cognitive Assessment and Cognitive Training: A Systematic Review of Applications and Efficacy | JMIR Serious Games 4(2):e11 | 27421244 | [10.2196/games.5888](https://doi.org/10.2196/games.5888) | RS (33 estudios) | Los elementos de juego suelen aumentar las ganas de participar. Su efecto en el desempeño fue mixto. | **Sí.** |
| Vermeir y otros, 2020 | The Effects of Gamification on Computerized Cognitive Training: Systematic Review and Meta-Analysis | JMIR Serious Games 8(3):e18644 | 32773374 | [10.2196/18644](https://doi.org/10.2196/18644) | RS y MA (49 estudios, 9 ECA) | Las tareas con elementos de juego resultaron más motivadoras y también más exigentes, sin cambio en el desempeño. | **Sí.** |
| Koivisto y Malik, 2021 | Gamification for Older Adults: A Systematic Literature Review | Gerontologist 61(7):e360-e372 | 32530026 | [10.1093/geront/gnaa047](https://doi.org/10.1093/geront/gnaa047) | RS (12 estudios, 55 años o más) | En personas mayores, los juegos muestran señales positivas, pero la evidencia todavía es débil. | **Sí.** |
| Peeters y otros, 2023 (en línea 2022) | Behaviour Change Techniques in Computerized Cognitive Training for Cognitively Healthy Older Adults: A Systematic Review | Neuropsychol Rev 33(1):238-254 | 35157209 | [10.1007/s11065-022-09537-4](https://doi.org/10.1007/s11065-022-09537-4) | RS con metarregresión (90 estudios) | Los premios genéricos se asociaron a menos efecto. Registrar la propia práctica se asoció a más constancia. | **Solo interno.** Va en contra de dos elementos de Nubi. Ver Cautelas. |

---

## 7. Autonomía y competencia (teoría de la autodeterminación)

| Cita corta | Título original | Revista | PMID | DOI | Tipo | Qué sustenta | ¿Para la app? |
|---|---|---|---|---|---|---|---|
| Ryan y Deci, 2000 | Self-determination theory and the facilitation of intrinsic motivation, social development, and well-being | Am Psychol 55(1):68-78 | 11392867 | [10.1037//0003-066x.55.1.68](https://doi.org/10.1037/0003-066x.55.1.68) | Revisión teórica (fundacional) | Sentirse capaz, con libertad de elegir y acompañado sostiene las ganas propias de hacer algo. | **Sí.** |
| Ng y otros, 2012 | Self-Determination Theory Applied to Health Contexts: A Meta-Analysis | Perspect Psychol Sci 7(4):325-40 | 26168470 | [10.1177/1745691612447309](https://doi.org/10.1177/1745691612447309) | MA (184 conjuntos de datos) | Respetar la libertad de elegir se asoció con una motivación más propia en conductas de salud. | **Sí, con matiz.** Los datos son de asociación. |
| Wulf y Lewthwaite, 2016 | Optimizing performance through intrinsic motivation and attention for learning: The OPTIMAL theory of motor learning | Psychon Bull Rev 23(5):1382-1414 | 26833314 | [10.3758/s13423-015-0999-9](https://doi.org/10.3758/s13423-015-0999-9) | Revisión teórica | Según esta teoría, poder elegir y esperar que irá bien favorecen el aprendizaje de movimientos. | **Solo interno.** Trata del movimiento e incluye mecanismos especulativos. |
| Gardner y Lally, 2013 (en línea 2012) | Does intrinsic motivation strengthen physical activity habit? Modeling relationships between self-determination, past behaviour, and habit strength | J Behav Med 36(5):488-97 | 22760451 | [10.1007/s10865-012-9442-0](https://doi.org/10.1007/s10865-012-9442-0) | Transversal (192 personas de 18-30 años) | Hacer algo por gusto propio se asoció con hábitos más firmes de actividad física. | **Solo interno.** Une las secciones 5 y 7. Es una muestra joven y de diseño transversal. |

---

## 8. Compararse solo con uno mismo y «es un resultado del juego, no una medida de capacidad»

| Cita corta | Título original | Revista | PMID | DOI | Tipo | Qué sustenta | ¿Para la app? |
|---|---|---|---|---|---|---|---|
| Lamont y otros, 2015 | A review and meta-analysis of age-based stereotype threat: negative stereotypes, not facts, do the damage | Psychol Aging 30(1):180-193 | 25621742 | [10.1037/a0038586](https://doi.org/10.1037/a0038586) | RS y MA (32 artículos, 82 efectos) | Recordar estereotipos negativos sobre la edad empeora el desempeño de personas mayores en tareas mentales. | **Sí.** Fe de erratas en 2018: d = 0,32, conclusiones iguales (PMID 30091626, [10.1037/pag0000269](https://doi.org/10.1037/pag0000269)). |
| Armstrong y otros, 2017 | Stereotype Threat Effects on Older Adults' Episodic and Working Memory: A Meta-Analysis | Gerontologist 57(suppl_2):S193-S205 | 28854607 | [10.1093/geront/gnx056](https://doi.org/10.1093/geront/gnx056) | MA (23 artículos) | La amenaza del estereotipo de edad bajó el recuerdo de personas mayores en una medida pequeña a moderada. | **Sí.** |
| Hess y otros, 2003 | The impact of stereotype threat on age differences in memory performance | J Gerontol B Psychol Sci Soc Sci 58(1):P3-11 | 12496296 | [10.1093/geronb/58.1.p3](https://doi.org/10.1093/geronb/58.1.p3) | Experimental (48 jóvenes y 48 mayores) | Bajo amenaza del estereotipo, las personas mayores recordaron menos, sobre todo quienes valoraban su memoria. | **Sí.** |
| Wilson y Ross, 2000 | The frequency of temporal-self and social comparisons in people's personal appraisals | J Pers Soc Psychol 78(5):928-42 | 10821199 | [10.1037//0022-3514.78.5.928](https://doi.org/10.1037/0022-3514.78.5.928) | Experimental (5 estudios) | Las personas se comparan con su propio pasado tanto como con otras, y eso suele sentar mejor. | **Sí.** |
| Lewthwaite y Wulf, 2010 (en línea 2009) | Social-comparative feedback affects motor skill learning | Q J Exp Psychol 63(4):738-49 | 19691002 | [10.1080/17470210903111839](https://doi.org/10.1080/17470210903111839) | Experimental (adultos jóvenes) | Decir «vas sobre el promedio» ayudó a aprender una tarea de equilibrio, aunque el dato era falso. | **Solo interno.** Matiza «nunca comparar». Ver Cautelas. |

Para la frase «es un resultado del juego, no una medida de capacidad», ver también Hedge y otros 2018 y Calamia y otros 2012 en la sección 9.

---

## 9. Contrarreloj con pista fija, igual para todos: efecto de práctica y estabilidad

| Cita corta | Título original | Revista | PMID | DOI | Tipo | Qué sustenta | ¿Para la app? |
|---|---|---|---|---|---|---|---|
| Calamia y otros, 2012 | Scoring higher the second time around: meta-analyses of practice effects in neuropsychological assessment | Clin Neuropsychol 26(4):543-70 | 22540222 | [10.1080/13854046.2012.680913](https://doi.org/10.1080/13854046.2012.680913) | MA (~1.600 efectos) | Repetir la misma prueba sube el puntaje por pura práctica, y eso hay que considerarlo al leer cambios. | **Sí.** Respalda que la marca es un resultado del juego. |
| Hedge y otros, 2018 | The reliability paradox: Why robust cognitive tasks do not produce reliable individual differences | Behav Res Methods 50(3):1166-1186 | 28726177 | [10.3758/s13428-017-0935-1](https://doi.org/10.3758/s13428-017-0935-1) | 3 estudios de repetición (7 tareas clásicas) | En tareas clásicas de reacción, el puntaje de una misma persona cambia bastante de una vez a otra. | **Sí.** Respalda «no es una medida de capacidad». |
| Sliwinski y otros, 2018 (en línea 2016) | Reliability and Validity of Ambulatory Cognitive Assessments | Assessment 25(1):14-30 | 27084835 | [10.1177/1073191116643164](https://doi.org/10.1177/1073191116643164) | Validación (219 adultos) | Promediar varias pruebas breves en el teléfono da un resultado tan estable como en laboratorio. | **Solo interno.** Una sola partida es poco estable y el promedio de varias lo es más. Ver Cautelas. |

---

## 10. Accesibilidad para personas mayores

| Cita corta | Título original | Revista | PMID | DOI | Tipo | Qué sustenta | ¿Para la app? |
|---|---|---|---|---|---|---|---|
| Wildenbos y otros, 2018 | Aging barriers influencing mobile health usability for older adults: A literature based framework (MOLD-US) | Int J Med Inform 114:66-75 | 29673606 | [10.1016/j.ijmedinf.2018.03.012](https://doi.org/10.1016/j.ijmedinf.2018.03.012) | Revisión de alcance | La vista, el tacto, la memoria y la motivación cambian con la edad y afectan el uso de apps. | **Sí.** |
| Owsley, 2011 (en línea 2010) | Aging and vision | Vision Res 51(13):1610-22 | 20974168 | [10.1016/j.visres.2010.10.020](https://doi.org/10.1016/j.visres.2010.10.020) | Revisión narrativa | Con la edad baja la capacidad de distinguir contrastes y de ver con poca luz. | **Sí.** Respalda el alto contraste. |
| Yeh, 2020 | Impact of button position and touchscreen font size on healthcare device operation by older adults | Heliyon 6(6):e04147 | 32637675 | [10.1016/j.heliyon.2020.e04147](https://doi.org/10.1016/j.heliyon.2020.e04147) | Experimental (32 jóvenes y 32 de 65+) | Con letra de 22 puntos, las personas de 65 años o más operaron la pantalla tan bien como jóvenes. | **Sí, con matiz.** Es un solo estudio, hecho en Taiwán con aparatos de salud. |
| Yeh, 2025 | Exploring Touchscreen Interface Design for Home Health Management Products: Effects of Button Size and Position on Elderly Users | Percept Mot Skills 132(6):1256-1275 | 40440033 | [10.1177/00315125251345594](https://doi.org/10.1177/00315125251345594) | Experimental (110 jóvenes y 110 de 65+) | Las personas de 65 años o más rindieron mejor con botones de 16 milímetros. | **Sí, con matiz.** Ver Cautelas sobre los 88 dp. |
| Staab y otros, 2017 | Diagnostic criteria for persistent postural-perceptual dizziness (PPPD): Consensus document of the committee for the Classification of Vestibular Disorders of the Bárány Society | J Vestib Res 27(4):191-208 | 29036855 | [10.3233/VES-170622](https://doi.org/10.3233/VES-170622) | Consenso diagnóstico internacional | En ciertos mareos crónicos, las imágenes en movimiento o recargadas empeoran los síntomas. | **Sí.** Respalda la opción de quitar animaciones. |

---

## 11. «Lo que no decimos»: transferencia cercana y lejana, y límites de la evidencia

| Cita corta | Título original | Revista | PMID | DOI | Tipo | Qué sustenta | ¿Para la app? |
|---|---|---|---|---|---|---|---|
| Melby-Lervåg y otros, 2016 | Working Memory Training Does Not Improve Performance on Measures of Intelligence or Other Measures of "Far Transfer": Evidence From a Meta-Analytic Review | Perspect Psychol Sci 11(4):512-34 | 27474138 | [10.1177/1745691616635612](https://doi.org/10.1177/1745691616635612) | MA (87 publicaciones) | Practicar tareas de memoria mejora esas tareas, pero no se vio mejora en habilidades distintas. | **Sí.** Es un límite honesto. |
| Sala y otros, 2018 (en línea 2017) | Video game training does not enhance cognitive ability: A comprehensive meta-analytic investigation | Psychol Bull 144(2):111-139 | 29239631 | [10.1037/bul0000139](https://doi.org/10.1037/bul0000139) | MA (3 modelos, más de 300 efectos cada uno) | Jugar videojuegos no mostró mejoras generales en habilidades mentales fuera del juego. | **Sí.** |
| Gobet y Sala, 2023 (en línea 2022) | Cognitive Training: A Field in Search of a Phenomenon | Perspect Psychol Sci 18(1):125-141 | 35939827 | [10.1177/17456916221091830](https://doi.org/10.1177/17456916221091830) | Revisión crítica de metaanálisis | Según estos autores, la mejora en tareas muy distintas a las practicadas es prácticamente nula. | **Solo interno.** Es una postura fuerte y discutida en el campo. |
| Lampit y otros, 2014 | Computerized cognitive training in cognitively healthy older adults: a systematic review and meta-analysis of effect modifiers | PLoS Med 11(11):e1001756 | 25405755 | [10.1371/journal.pmed.1001756](https://doi.org/10.1371/journal.pmed.1001756) | RS y MA (52 ECA, 4.885 personas) | En mayores sanos hubo efectos pequeños; en casa sin guía o más de tres veces por semana, no. | **Solo interno.** El efecto positivo se puede leer como promesa, y los moderadores van contra el diseño de Nubi. |
| Gates y otros, 2020 (Cochrane) | Computerised cognitive training for 12 or more weeks for maintaining cognitive function in cognitively healthy people in late life | Cochrane Database Syst Rev 2(2):CD012277 | 32104914 | [10.1002/14651858.CD012277.pub3](https://doi.org/10.1002/14651858.CD012277.pub3) | Revisión Cochrane (8 ECA, 1.183 personas, 65+) | Evidencia de baja calidad: beneficios pequeños e inciertos que no se mantuvieron al año. | **Sí, como límite.** Es la versión vigente; reemplaza a la pub2 de 2019 (PMID 30864187). |
| Rebok y otros, 2014 | Ten-year effects of the advanced cognitive training for independent and vital elderly cognitive training trial on cognition and everyday functioning in older adults | J Am Geriatr Soc 62(1):16-24 | 24417410 | [10.1111/jgs.12607](https://doi.org/10.1111/jgs.12607) | ECA con 10 años de seguimiento (2.832 personas) | Diez años después, lo practicado en rapidez y razonamiento se mantuvo, pero lo practicado en memoria no. | **Solo interno.** Es un resultado positivo con riesgo de leerse como promesa. Eran sesiones guiadas en grupo, no una app. |
| Edwards y otros, 2017 | Speed of processing training results in lower risk of dementia | Alzheimers Dement (N Y) 3(4):603-611 | 29201994 | [10.1016/j.trci.2017.09.002](https://doi.org/10.1016/j.trci.2017.09.002) | Análisis secundario de un ECA (ACTIVE) | Es un análisis secundario en el límite de la significación (p = 0,049) y no sirve como promesa. | **Solo interno, nunca en la app.** |

---

## 12. Mantenerse activo, mental y socialmente, como parte de envejecer bien

Todo lo de esta sección es de asociación, no de causa. Cualquier frase en la app debe evitar sugerir que jugar a Nubi protege la salud.

| Cita corta | Título original | Revista | PMID | DOI | Tipo | Qué sustenta | ¿Para la app? |
|---|---|---|---|---|---|---|---|
| Livingston y otros, 2024 | Dementia prevention, intervention, and care: 2024 report of the Lancet standing Commission | Lancet 404(10452):572-628 | 39096926 | [10.1016/S0140-6736(24)01296-0](https://doi.org/10.1016/S0140-6736(24)01296-0) | Informe de comisión de expertos | Este informe internacional enumera factores de riesgo modificables, entre ellos el aislamiento social. | **Solo interno.** PubMed no trae resumen, así que el contenido se describe según el informe y conviene verificarlo antes de citar. |
| Kuiper y otros, 2015 | Social relationships and risk of dementia: A systematic review and meta-analysis of longitudinal cohort studies | Ageing Res Rev 22:39-57 | 25956016 | [10.1016/j.arr.2015.04.006](https://doi.org/10.1016/j.arr.2015.04.006) | RS y MA (19 cohortes) | El poco contacto social y la soledad se asociaron con más demencia. Es asociación, no causa. | **Solo interno.** |
| Su y otros, 2022 | Leisure Activities and the Risk of Dementia: A Systematic Review and Meta-analysis | Neurology 99(15):e1651-e1663 | 35948447 | [10.1212/WNL.0000000000200929](https://doi.org/10.1212/WNL.0000000000200929) | RS y MA (38 estudios longitudinales) | Quienes hacían más actividades de ocio tuvieron menos demencia después; la relación podría ser inversa. | **Solo interno.** |

---

## No encontrado en PubMed o dudoso

**No encontrado en PubMed** (se buscó por cita, título y autor):
- **Lally y otros 2010**, «How are habits formed: Modelling habit formation in the real world» (Eur J Soc Psychol). No aparece en la lista de Lally P en PubMed entre 2008 y 2013. Su hallazgo de que saltarse una oportunidad no frena el hábito no se puede citar desde PubMed. Lo más cercano son Lally y otros 2011 (cualitativo) y Gardner y otros 2012.
- **Kluger y DeNisi 1996** (Psychol Bull), metaanálisis clásico sobre retroalimentación: no se encontró. Se usó Wisniewski y otros 2020 en su lugar.
- **Hattie y Timperley 2007** (Rev Educ Res): no está indexado. Lo reemplaza el mismo Wisniewski y otros 2020.
- **Ryan, Rigby y Przybylski 2006**, «The motivational pull of video games» (Motiv Emot): no se encontró.
- **Metaanálisis sobre práctica autocontrolada** (McKay y otros): no se encontró. Por eso no se puede contrastar aquí la teoría OPTIMAL.
- **Canestrari 1963**, sobre ritmo impuesto versus ritmo propio en mayores: no se encontró con la búsqueda hecha.
- **Dificultad adaptativa versus fija y cuánto se disfruta el juego** (ajuste dinámico de dificultad): no hay estudios sólidos en PubMed. Esa literatura está en ACM e IEEE.
- **Pautas de tamaño táctil** (WCAG, Material Design 48 dp, Jin y otros 2007): no están en PubMed. Solo se encontraron los dos estudios de Yeh.
- **Evidencia sobre rachas en apps** (tipo Duolingo): en PubMed solo apareció Ingalls y otros 2026, que trata de corredores.

**Dudoso o débil:**
- **Wilson y otros 2019**: es un resultado matemático para algoritmos de clasificación binaria y no se probó en personas.
- **Edwards y otros 2017**: p = 0,049 en un análisis secundario.
- **Brehmer y otros 2012**: la transferencia lejana que reporta incluye una autoevaluación.
- **Livingston y otros 2024**: no tiene resumen en PubMed.
- **Gardner y otros 2012**: es narrativo y no tiene resumen en PubMed.

---

## Cautelas

**Evidencia que va en contra o matiza un mecanismo de Nubi:**

1. **Sesiones diarias cortas en casa (sección 3).** Lampit y otros 2014 encontró que la práctica en casa sin guía no fue eficaz, que más de tres sesiones por semana tampoco lo fue y que hubo poca evidencia para sesiones de menos de 30 minutos. Esto afecta a Nubi solo si se dijera que el horario diario «hace efecto». Si el horario se justifica por hábito y disfrute, no hay contradicción. No hay que citar a Lampit como apoyo de las sesiones cortas.
2. **Dificultad adaptativa (sección 1).** von Bastian y Eschen 2016 halló que ajustar la dificultad a cada persona no rindió más que variarla al azar. Peeters y otros 2023 asoció las «tareas graduadas» a menos constancia, aunque es una correlación entre estudios. La adaptación queda bien defendida para evitar frustración y aburrimiento, pero no como algo necesario para avanzar.
3. **Premios y puntos (sección 6).** Peeters y otros 2023 encontró que el «premio no específico» se asoció a menos efecto. Los «+50» y premios genéricos pueden quedarse por diversión, pero no deben presentarse como parte de un beneficio. En el mismo estudio, registrar la propia práctica (el calendario semanal) se asoció a más constancia, lo que apoya a Nubi.
4. **«Nunca comparar con otros» (sección 8).** En Lewthwaite y Wulf 2010, adultos jóvenes aprendieron más con una comparación social que sin ninguna, incluso cuando la comparación era negativa. La regla de Nubi se sostiene por la amenaza del estereotipo y el bienestar (Lamont, Armstrong, Hess), no porque comparar siempre rinda menos. Esos estudios y Wulf 2012 usaron datos falsos, cosa que Nubi no debe imitar.
5. **Franjas de edad en el Contrarreloj.** Si el resultado muestra la franja de edad junto al puntaje, se activa justo lo que Lamont y otros 2015 describe: el efecto es mayor cuando la señal de edad aparece justo antes de la tarea. Además, Starns y Ratcliff 2010 y 2012 muestran que los mayores priorizan la precisión aun con tiempo límite. Por eso una pista igual para todos, con marca de aciertos menos errores en 2 minutos, favorece por diseño a los jóvenes. Comparar entre edades no sería justo, y comparar dentro de la misma franja exige no mostrar la franja como etiqueta.
6. **Una sola partida como «marca» (sección 9).** Hedge y otros 2018 y Sliwinski y otros 2018 muestran que una prueba breve aislada es poco estable, y que el promedio de varias sí lo es. Inferencia mía, no de los artículos: la mejor marca de muchas partidas tiende a subir por azar, además del efecto de práctica de Calamia y otros 2012. Esto refuerza la frase «es un resultado del juego».
7. **Errores (sección 4).** La evidencia es mixta: Scheper y otros 2021 apoya aprender sin errores y Cyr y Anderson 2015 muestra que algunos errores ayudan. Kessels y de Haan 2003 (MA, J Clin Exp Neuropsychol 25(6):805-14, PMID 13680458, [10.1076/jcen.25.6.805.16474](https://doi.org/10.1076/jcen.25.6.805.16474)) estudió a personas con amnesia y no debe generalizarse a Nubi.
8. **Tamaño de botones (sección 10).** Nubi usa 88 dp, que son unos 14 mm (1 dp = 1/160 pulgada). Yeh 2025 obtuvo el mejor rendimiento de los mayores con 16 mm, pero el resumen no dice qué otros tamaños se probaron. Vale la pena revisar el artículo completo antes de dar por buenos los 88 dp.
9. **Animaciones y edad.** Jochmann y otros 2025 (J Neuroeng Rehabil 22(1):143, PMID 40611269, [10.1186/s12984-025-01677-x](https://doi.org/10.1186/s12984-025-01677-x)) encontró que los mayores tuvieron menos mareo en realidad virtual que los jóvenes. La opción de quitar animaciones se justifica por los trastornos vestibulares (Staab y otros 2017), no por la edad en sí.
10. **Rachas (sección 5).** Ingalls y otros 2026 describe tristeza, sensación de duelo y gente que corría lesionada para no cortar la racha. Esto apoya el día de descanso que perdona. También sugiere no usar mensajes de pérdida («perdiste tu racha») cuando se corta.

**Fuentes que podrían leerse como promesa de salud (mantener solo internas):**
- Rebok y otros 2014, Edwards y otros 2017 y la parte positiva de Lampit y otros 2014: en ACTIVE eran sesiones guiadas y la reducción de demencia es marginal.
- Kuiper y otros 2015, Su y otros 2022 y Livingston y otros 2024: son asociaciones en cohortes y puede haber causalidad inversa.
- Brehmer y otros 2012 y Wulf y otros 2012: hablan de «mejoras» en contextos distintos al de Nubi.

Cualquier texto de la app basado en la sección 12 debería hablar de una vida activa en general (ver gente, moverse, hacer lo que gusta). No debe sugerir que jugar a Nubi protege la salud.
