# ¿Verdad o disparate? — muestra de 100 frases para revisar (1-oct)

**Marca las que estén mal, sean dudosas o suenen raras.** La ambigüedad es el mayor riesgo de este juego: una frase
dudosa frustra. Cada frase trae su respuesta (✔ verdad / ✘ disparate) y, en los disparates, su clase y la corrección
que se muestra al errar. Salen al azar con semilla fija de `docs/frases-muestra-disparate.md` (generador:
`python tools/frases/disparate.py`); las frases salen de una base propia en `tools/frases/conocimiento.py`.

Tipos: 1 corta · 2 con complemento o adjetivo · 3 negación · 4 frase con pausa («, que …,») · 5 todos / algunos / ningún · 6 comparación y orden.

## Tipo 1 · corta

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 1 | Las flautas suenan | ✔ verdad | — | — |
| 2 | Los elefantes ladran | ✘ disparate | evidente | Los elefantes no ladran |
| 3 | Los canarios vuelan | ✔ verdad | — | — |
| 4 | Los pinceles pintan | ✔ verdad | — | — |
| 5 | Los ventiladores comen | ✘ disparate | evidente | Los ventiladores no comen |
| 6 | Los canguros saltan | ✔ verdad | — | — |
| 7 | Las gallinas cacarean | ✔ verdad | — | — |
| 8 | Los leopardos ladran | ✘ disparate | evidente | Los leopardos no ladran |
| 9 | Los chimpancés saltan | ✔ verdad | — | — |
| 10 | Las mesas lloran | ✘ disparate | evidente | Las mesas no lloran |
| 11 | Las gaviotas ladran | ✘ disparate | evidente | Las gaviotas no ladran |
| 12 | Los clavos hablan | ✘ disparate | evidente | Los clavos no hablan |
| 13 | Las zanahorias duermen | ✘ disparate | evidente | Las zanahorias no duermen |
| 14 | Los lobos aúllan | ✔ verdad | — | — |
| 15 | Los canarios cantan | ✔ verdad | — | — |
| 16 | Los helicópteros vuelan | ✔ verdad | — | — |
| 17 | Los leones vuelan | ✘ disparate | evidente | Los leones no vuelan |

## Tipo 2 · con complemento

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 18 | La sal es salada | ✔ verdad | — | — |
| 19 | Los ladrillos sirven para construir | ✔ verdad | — | — |
| 20 | Los pulpos viven en el mar | ✔ verdad | — | — |
| 21 | Las nubes flotan en el cielo | ✔ verdad | — | — |
| 22 | Las cigüeñas tienen plumas | ✔ verdad | — | — |
| 23 | Los cisnes trabajan en una oficina | ✘ disparate | evidente | Los cisnes no trabajan en una oficina |
| 24 | Los aviones tienen hambre | ✘ disparate | evidente | Los aviones no tienen hambre |
| 25 | La nariz sirve para ver | ✘ disparate | sutil | La nariz no sirve para ver |
| 26 | Las vacas comen hierba | ✔ verdad | — | — |
| 27 | Los murciélagos ponen huevos | ✘ disparate | sutil | Los murciélagos no ponen huevos |
| 28 | Las palomas escriben cartas | ✘ disparate | evidente | Las palomas no escriben cartas |
| 29 | La sopa tiene sueño | ✘ disparate | evidente | La sopa no tiene sueño |
| 30 | Las orejas sirven para ver | ✘ disparate | sutil | Las orejas no sirven para ver |
| 31 | La sopa se toma con cuchara | ✔ verdad | — | — |
| 32 | Las sierras tienen hambre | ✘ disparate | evidente | Las sierras no tienen hambre |
| 33 | Las anguilas viven en el agua | ✔ verdad | — | — |
| 34 | El jugo se bebe | ✔ verdad | — | — |

## Tipo 3 · negación

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 35 | Los helicópteros no duermen | ✔ verdad | — | — |
| 36 | Las ovejas no respiran | ✘ disparate | evidente | Las ovejas sí respiran |
| 37 | Las serpientes no pagan cuentas | ✔ verdad | — | — |
| 38 | Las cuerdas no tienen sueño | ✔ verdad | — | — |
| 39 | Las bombillas no alumbran | ✘ disparate | sutil | Las bombillas sí alumbran |
| 40 | Las tijeras no escriben | ✔ verdad | — | — |
| 41 | Los delfines no tienen aletas | ✘ disparate | sutil | Los delfines sí tienen aletas |
| 42 | Los vasos no ríen | ✔ verdad | — | — |
| 43 | Los gallos no leen libros | ✔ verdad | — | — |
| 44 | Los grillos no crecen | ✘ disparate | evidente | Los grillos sí crecen |
| 45 | Los grillos no pilotan aviones | ✔ verdad | — | — |
| 46 | Las mariposas no revolotean | ✘ disparate | sutil | Las mariposas sí revolotean |
| 47 | Los zorros no tienen cola | ✘ disparate | sutil | Los zorros sí tienen cola |
| 48 | La sal no es dulce | ✔ verdad | — | — |
| 49 | Los pianos no suenan | ✘ disparate | sutil | Los pianos sí suenan |
| 50 | Los barcos no tienen hambre | ✔ verdad | — | — |
| 51 | Los ladrillos no sirven para construir | ✘ disparate | sutil | Los ladrillos sí sirven para construir |

## Tipo 4 · frase con pausa (, que …,)

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 52 | Las cigüeñas, que tienen plumas, cocinan la cena | ✘ disparate | evidente | Las cigüeñas, que tienen plumas, no cocinan la cena |
| 53 | Los gansos, que tienen plumas, ladran | ✘ disparate | evidente | Los gansos, que tienen plumas, no ladran |
| 54 | Las águilas, que tienen alas, cazan | ✔ verdad | — | — |
| 55 | Los trenes, que tienen vagones, ríen | ✘ disparate | evidente | Los trenes, que tienen vagones, no ríen |
| 56 | Los paraguas, que protegen de la lluvia, lloran | ✘ disparate | evidente | Los paraguas, que protegen de la lluvia, no lloran |
| 57 | Los chimpancés, que tienen manos, trepan | ✔ verdad | — | — |
| 58 | Las ballenas, que son enormes, viven en el agua | ✔ verdad | — | — |
| 59 | Los caballos, que tienen cola, galopan | ✔ verdad | — | — |
| 60 | Los leones, que tienen garras, rugen | ✔ verdad | — | — |
| 61 | Los gansos, que tienen plumas, graznan | ✔ verdad | — | — |
| 62 | Los leones, que tienen garras, cazan | ✔ verdad | — | — |
| 63 | Las ovejas, que tienen lana, pagan cuentas | ✘ disparate | evidente | Las ovejas, que tienen lana, no pagan cuentas |
| 64 | Las uvas, que crecen en racimos, sueñan | ✘ disparate | evidente | Las uvas, que crecen en racimos, no sueñan |
| 65 | Los peces, que viven en el agua, tienen aletas | ✔ verdad | — | — |
| 66 | Los limones, que crecen en los árboles, son ácidos | ✔ verdad | — | — |
| 67 | Las vacas, que tienen cuatro patas, vuelan | ✘ disparate | evidente | Las vacas, que tienen cuatro patas, no vuelan |
| 68 | Los leopardos, que tienen cuatro patas, vuelan | ✘ disparate | evidente | Los leopardos, que tienen cuatro patas, no vuelan |

## Tipo 5 · todos / algunos / ningún

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 69 | Todos los reptiles tienen pelo | ✘ disparate | evidente | Ningún reptil tiene pelo |
| 70 | Todos los círculos tienen lados rectos | ✘ disparate | evidente | Ningún círculo tiene lados rectos |
| 71 | Todos los árboles ladran | ✘ disparate | evidente | Ningún árbol ladra |
| 72 | Algunos peces son rojos | ✔ verdad | — | — |
| 73 | Todos los reptiles tienen caparazón | ✘ disparate | sutil | Solo algunos reptiles tienen caparazón |
| 74 | Algunos reptiles tienen pelo | ✘ disparate | evidente | Ningún reptil tiene pelo |
| 75 | Todas las flores son blancas | ✘ disparate | sutil | Solo algunas flores son blancas |
| 76 | Algunos muebles sirven para sentarse | ✔ verdad | — | — |
| 77 | Algunos animales tienen cola | ✔ verdad | — | — |
| 78 | Ninguna persona tiene cola | ✔ verdad | — | — |
| 79 | Ningún reptil da leche | ✔ verdad | — | — |
| 80 | Algunas verduras son verdes | ✔ verdad | — | — |
| 81 | Algunos peces tienen pelo | ✘ disparate | evidente | Ningún pez tiene pelo |
| 82 | Ningún pájaro es azul | ✘ disparate | sutil | Solo algunos pájaros son azules |
| 83 | Ningún vehículo ladra | ✔ verdad | — | — |
| 84 | Algunos instrumentos musicales tienen cuerdas | ✔ verdad | — | — |

## Tipo 6 · comparación y orden

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 85 | Una semana dura menos que un segundo | ✘ disparate | evidente | Un segundo dura menos que una semana |
| 86 | La bicicleta es más rápida que el guepardo | ✘ disparate | sutil | El guepardo es más rápido que la bicicleta |
| 87 | Una manzana es más pequeña que un perro | ✔ verdad | — | — |
| 88 | Un edificio pesa menos que un conejo | ✘ disparate | evidente | Un conejo pesa menos que un edificio |
| 89 | El guepardo es más lento que la bicicleta | ✘ disparate | sutil | La bicicleta es más lenta que el guepardo |
| 90 | Un mes dura más que un año | ✘ disparate | sutil | Un año dura más que un mes |
| 91 | El cohete es más rápido que el caballo | ✔ verdad | — | — |
| 92 | Una taza pesa menos que un elefante | ✔ verdad | — | — |
| 93 | El cohete es más rápido que la bicicleta | ✔ verdad | — | — |
| 94 | El helado es más frío que el sol | ✔ verdad | — | — |
| 95 | El caracol es más lento que el guepardo | ✔ verdad | — | — |
| 96 | El caracol es más rápido que el caballo | ✘ disparate | evidente | El caballo es más rápido que el caracol |
| 97 | El caballo es más rápido que el avión | ✘ disparate | evidente | El avión es más rápido que el caballo |
| 98 | El fuego es más frío que el helado | ✘ disparate | evidente | El helado es más frío que el fuego |
| 99 | Una semana dura menos que un siglo | ✔ verdad | — | — |
| 100 | Un barco pesa más que un conejo | ✔ verdad | — | — |
