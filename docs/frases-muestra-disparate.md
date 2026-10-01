# ¿Verdad o disparate? — muestra de 100 frases para revisar (1-oct)

**Marca las que estén mal, sean dudosas o suenen raras.** La ambigüedad es el mayor riesgo de este juego: una frase
dudosa frustra. Cada frase trae su respuesta (✔ verdad / ✘ disparate) y, en los disparates, su clase y la corrección
que se muestra al errar. Salen al azar con semilla fija de `docs/frases-muestra-disparate.md` (generador:
`python tools/frases/disparate.py`); las frases salen de una base propia en `tools/frases/conocimiento.py`.

Tipos: 1 corta · 2 con complemento o adjetivo · 3 negación · 4 con «que» · 5 todos / algunos / ningún · 6 comparación y orden.

## Tipo 1 · corta

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 1 | Los loros respiran | ✔ verdad | — | — |
| 2 | Los leones vuelan | ✘ disparate | evidente | Los leones no vuelan |
| 3 | Las abejas crecen | ✔ verdad | — | — |
| 4 | Las velas alumbran | ✔ verdad | — | — |
| 5 | Los peces maúllan | ✘ disparate | evidente | Los peces no maúllan |
| 6 | Los cerdos gruñen | ✔ verdad | — | — |
| 7 | Las ovejas respiran | ✔ verdad | — | — |
| 8 | El pan ríe | ✘ disparate | evidente | El pan no ríe |
| 9 | Los pulpos crecen | ✔ verdad | — | — |
| 10 | El fuego moja | ✘ disparate | evidente | El fuego no moja |
| 11 | La leche duerme | ✘ disparate | evidente | La leche no duerme |
| 12 | Los pájaros ladran | ✘ disparate | evidente | Los pájaros no ladran |
| 13 | Las almohadas ríen | ✘ disparate | evidente | Las almohadas no ríen |
| 14 | Las moscas comen | ✔ verdad | — | — |
| 15 | Los peces nadan | ✔ verdad | — | — |
| 16 | Las gallinas crecen | ✔ verdad | — | — |
| 17 | La miel piensa | ✘ disparate | evidente | La miel no piensa |

## Tipo 2 · con complemento

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 18 | El hielo es caliente | ✘ disparate | sutil | El hielo es frío |
| 19 | Los huevos tienen hambre | ✘ disparate | evidente | Los huevos no tienen hambre |
| 20 | La sopa se toma con tenedor | ✘ disparate | sutil | La sopa no se toma con tenedor |
| 21 | Las naranjas crecen en los árboles | ✔ verdad | — | — |
| 22 | Las manzanas crecen en los árboles | ✔ verdad | — | — |
| 23 | Las manzanas tienen semillas | ✔ verdad | — | — |
| 24 | Los loros tienen plumas | ✔ verdad | — | — |
| 25 | Los murciélagos tienen alas | ✔ verdad | — | — |
| 26 | La miel tiene sueño | ✘ disparate | evidente | La miel no tiene sueño |
| 27 | Los perros tienen cola | ✔ verdad | — | — |
| 28 | Los gatos trabajan en una oficina | ✘ disparate | evidente | Los gatos no trabajan en una oficina |
| 29 | Los camiones tienen motor | ✔ verdad | — | — |
| 30 | Las mariposas tienen alas | ✔ verdad | — | — |
| 31 | Los gatos leen libros | ✘ disparate | evidente | Los gatos no leen libros |
| 32 | Las ardillas ponen huevos | ✘ disparate | evidente | Las ardillas no ponen huevos |
| 33 | Los libros tienen páginas | ✔ verdad | — | — |
| 34 | Las manos sirven para ver | ✘ disparate | evidente | Las manos no sirven para ver |

## Tipo 3 · negación

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 35 | Las sillas no lloran | ✔ verdad | — | — |
| 36 | Los tiburones no vuelan | ✔ verdad | — | — |
| 37 | Las mesas no sirven para apoyar cosas | ✘ disparate | sutil | Las mesas sí sirven para apoyar cosas |
| 38 | Las manos no sirven para ver | ✔ verdad | — | — |
| 39 | Los pájaros no tienen pelo | ✔ verdad | — | — |
| 40 | Los árboles no tienen raíces | ✘ disparate | sutil | Los árboles sí tienen raíces |
| 41 | El pan no es un alimento | ✘ disparate | sutil | El pan sí es un alimento |
| 42 | Los camiones no tienen motor | ✘ disparate | sutil | Los camiones sí tienen motor |
| 43 | Los dientes no sirven para masticar | ✘ disparate | sutil | Los dientes sí sirven para masticar |
| 44 | Los peines no ríen | ✔ verdad | — | — |
| 45 | Los martillos no piensan | ✔ verdad | — | — |
| 46 | Las arañas no tejen telarañas | ✘ disparate | sutil | Las arañas sí tejen telarañas |
| 47 | Las serpientes no tienen plumas | ✔ verdad | — | — |
| 48 | Los pájaros no tienen aletas | ✔ verdad | — | — |
| 49 | Las plantas no crecen | ✘ disparate | sutil | Las plantas sí crecen |
| 50 | Los zapatos no lloran | ✔ verdad | — | — |
| 51 | Los ríos no llevan agua | ✘ disparate | sutil | Los ríos sí llevan agua |

## Tipo 4 · con «que»

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 52 | Los delfines que tienen aletas nadan | ✔ verdad | — | — |
| 53 | Las almohadas que sirven para apoyar la cabeza piensan | ✘ disparate | evidente | Las almohadas que sirven para apoyar la cabeza no piensan |
| 54 | Las mariposas que tienen alas tienen seis patas | ✔ verdad | — | — |
| 55 | Las gallinas que tienen alas cacarean | ✔ verdad | — | — |
| 56 | Los huevos que se rompen tienen sueño | ✘ disparate | evidente | Los huevos que se rompen no tienen sueño |
| 57 | Las hormigas que viven en grupos tienen ocho patas | ✘ disparate | sutil | Las hormigas que viven en grupos no tienen ocho patas |
| 58 | Las alfombras que cubren el suelo tienen hambre | ✘ disparate | evidente | Las alfombras que cubren el suelo no tienen hambre |
| 59 | Las monedas que se usan para pagar piensan | ✘ disparate | evidente | Las monedas que se usan para pagar no piensan |
| 60 | Los loros que tienen pico tienen plumas | ✔ verdad | — | — |
| 61 | Las ollas que sirven para cocinar comen | ✘ disparate | evidente | Las ollas que sirven para cocinar no comen |
| 62 | Los aviones que vuelan tienen alas | ✔ verdad | — | — |
| 63 | Los gatos que tienen cuatro patas tienen bigotes | ✔ verdad | — | — |
| 64 | El fuego que da calor quema | ✔ verdad | — | — |
| 65 | Los caballos que tienen cola tienen cuatro patas | ✔ verdad | — | — |
| 66 | Los gatos que tienen cola tienen plumas | ✘ disparate | evidente | Los gatos que tienen cola no tienen plumas |
| 67 | Las vacas que comen hierba cocinan la cena | ✘ disparate | evidente | Las vacas que comen hierba no cocinan la cena |
| 68 | Las flores que crecen tienen pétalos | ✔ verdad | — | — |

## Tipo 5 · todos / algunos / ningún

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 69 | Ningún cuadrado es redondo | ✔ verdad | — | — |
| 70 | Todos los peces son rojos | ✘ disparate | sutil | Solo algunos peces son rojos |
| 71 | Algunos animales viven en el agua | ✔ verdad | — | — |
| 72 | Todos los insectos pican | ✘ disparate | sutil | Solo algunos insectos pican |
| 73 | Ningún mueble tiene ruedas | ✘ disparate | sutil | Solo algunos muebles tienen ruedas |
| 74 | Algunas prendas de vestir comen | ✘ disparate | evidente | Ninguna prenda de vestir come |
| 75 | Algunas flores son amarillas | ✔ verdad | — | — |
| 76 | Ninguna persona tiene cola | ✔ verdad | — | — |
| 77 | Todas las prendas de vestir se ponen en el cuerpo | ✔ verdad | — | — |
| 78 | Ninguna flor sale de una planta | ✘ disparate | evidente | Todas las flores salen de una planta |
| 79 | Todos los mamíferos dan leche a sus crías | ✔ verdad | — | — |
| 80 | Todas las herramientas son animales | ✘ disparate | evidente | Ninguna herramienta es un animal |
| 81 | Ningún animal es de piedra | ✔ verdad | — | — |
| 82 | Ningún pez tiene plumas | ✔ verdad | — | — |
| 83 | Algunos planetas son animales | ✘ disparate | evidente | Ningún planeta es un animal |
| 84 | Todos los instrumentos musicales se tocan con las manos | ✘ disparate | sutil | Solo algunos instrumentos musicales se tocan con las manos |

## Tipo 6 · comparación y orden

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 85 | La bicicleta es más lenta que el cohete | ✔ verdad | — | — |
| 86 | La letra P viene antes que la letra J | ✘ disparate | sutil | La letra J viene antes que la letra P |
| 87 | Un mes dura más que un siglo | ✘ disparate | sutil | Un siglo dura más que un mes |
| 88 | La letra C viene después de la letra M | ✘ disparate | evidente | La letra M viene después de la letra C |
| 89 | Un caballo pesa menos que un edificio | ✔ verdad | — | — |
| 90 | El cohete es más lento que el guepardo | ✘ disparate | evidente | El guepardo es más lento que el cohete |
| 91 | Un zapato pesa menos que un barco | ✔ verdad | — | — |
| 92 | El fuego es más frío que el hielo | ✘ disparate | evidente | El hielo es más frío que el fuego |
| 93 | Un minuto dura menos que un segundo | ✘ disparate | sutil | Un segundo dura menos que un minuto |
| 94 | Una moneda pesa menos que un elefante | ✔ verdad | — | — |
| 95 | El cohete es más rápido que el caballo | ✔ verdad | — | — |
| 96 | La letra D viene después de la letra X | ✘ disparate | evidente | La letra X viene después de la letra D |
| 97 | La letra L viene después de la letra F | ✔ verdad | — | — |
| 98 | El fuego es más caliente que la nieve | ✔ verdad | — | — |
| 99 | La tortuga es más lenta que el cohete | ✔ verdad | — | — |
| 100 | La letra Q viene después de la letra Y | ✘ disparate | sutil | La letra Y viene después de la letra Q |
